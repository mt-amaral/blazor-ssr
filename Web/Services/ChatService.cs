using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Context;
using Web.Dto;
using Web.Dto.Account;
using Web.Dto.Chat;
using Web.Entity;
using Web.Entity.Identity;
using Web.Services.Abstractions;

namespace Web.Services;

public class ChatService(ApplicationDbContext context, IUserLoggedService userLoggedService, IChatIAOrchestrator chatIAOrchestrator) :  IChatService
{
    public async Task<(Response<ChatThreadOutDto?>, short)> CreateThreadAsync( string? title = "New Chat", CancellationToken ct = default)
    {
        try
        {
            var user = await userLoggedService.GetUserLoggedAsync();
            var chat = new ChatThread(user.Id,  title);
            await context.ChatThread.AddAsync(chat, ct);
            await context.SaveChangesAsync(ct);
            var response = new ChatThreadOutDto(chat.Id, chat.Title, chat.UpdatedAt.ToString());
            
            return (new Response<ChatThreadOutDto?>(response, null), 200);
        }
        catch(Exception ex)
        {
            return (new Response<ChatThreadOutDto?>(null, "Erro ao criar chat"), 500);
        }
    }
    
    
    public async Task<(Response<List<ChatThreadOutDto?>>, short)> GetThreadByUserAsync( )
    {
        try
        {
            var response = new List<ChatThreadOutDto?>();
            
            var user = await userLoggedService.GetUserLoggedAsync();

            var chat = context.ChatThread.AsNoTracking().Where(c => c.UserId == user.Id).ToList();
            foreach (var chatThread in chat)
            {
                response.Add(new (chatThread.Id, chatThread.Title, chatThread.UpdatedAt.ToString()));
            }
            
            
            return (new Response<List<ChatThreadOutDto?>>(response, null), 200);
        }
        catch
        {
            return (new Response<List<ChatThreadOutDto?>>(null, ""), 500);
        }
    }
    
    
    public async Task<(Response<MsgListOutDto?>, short)> ListMsgsAsync(MsgListInDto input, CancellationToken ct = default)
    {
        try
        {
            var user = await userLoggedService.GetUserLoggedAsync();

            // 1) garante que o thread é do user
            var threadExists = await context.ChatThread
                .AsNoTracking()
                .AnyAsync(t => t.Id == input.ThreadId && t.UserId == user.Id, ct);

            if (!threadExists)
                return (new Response<MsgListOutDto?>(null, "Thread não encontrado."), 404);

            // 2) base query
            var q = context.ChatMessage
                .AsNoTracking()
                .Where(m => m.ThreadId == input.ThreadId);

            // 3) aplica cursor (buscar mensagens MAIS ANTIGAS que o cursor)
            if (!string.IsNullOrWhiteSpace(input.BeforeCursor) && TryParseCursor(input.BeforeCursor, out var cursorAtUtc, out var cursorId))
            {
                q = q.Where(m =>
                    m.CreatedAt < cursorAtUtc ||
                    (m.CreatedAt == cursorAtUtc && m.Id < cursorId));
            }

            var take = input.Limit <= 0 ? 50 : Math.Min(input.Limit, 200);

            // 4) pega "limit + 1" para saber se tem mais
            var rows = await q
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Take(take + 1)
                .Select(m => new
                {
                    m.Id,
                    m.ThreadId,
                    m.Content,
                    m.CreatedAt,
                    Role = m.TypeMsg // se não tiver Role, troque aqui pelo seu campo
                })
                .ToListAsync(ct);

            var hasMore = rows.Count > take;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            // 5) UI normalmente espera antigo -> novo
            rows.Reverse();

            var items = rows.Select(m => new MsgOutDto(
                m.Id,
                m.ThreadId,
                m.Role,
                m.Content,
                m.CreatedAt
            )).ToList();

            // 6) próximo cursor = item mais antigo do bloco retornado (pra buscar mais antigas depois)
            string? nextCursor = null;
            if (hasMore && items.Count > 0)
            {
                var oldest = items.First(); // depois do Reverse, o primeiro é o mais antigo do lote
                nextCursor = BuildCursor(oldest.CreatedAt, oldest.Id);
            }

            var outDto = new MsgListOutDto(input.ThreadId, items, nextCursor, hasMore);
            return (new Response<MsgListOutDto?>(outDto, ""), 200);
        }
        catch (Exception ex)
        {
            return (new Response<MsgListOutDto?>(null, ex.Message), 500);
        }
    }

    public async Task<(Response<SendMsgOutDto?>, short)> SendMsgAsync(SendMsgInDto input, CancellationToken ct = default)
    {
        try
        {
            var user = await userLoggedService.GetUserLoggedAsync();

            // 1) valida thread do user
            var threadOk = await context.ChatThread
                .AnyAsync(t => t.Id == input.ThreadId && t.UserId == user.Id, ct);

            if (!threadOk)
                return (new Response<SendMsgOutDto?>(null, "Thread não encontrado."), 404);

            // 2) cria msg do user + placeholder bot (vazio)
            var userMsg = new ChatMessage(input.ThreadId, input.Content.Trim(), user.Id, TypeMsg.User);
            var botMsg = new ChatMessage(input.ThreadId, "", user.Id, TypeMsg.Bot);

            context.ChatMessage.Add(userMsg);
            context.ChatMessage.Add(botMsg);    

            await context.SaveChangesAsync(ct); // gera Ids

            // 3) chama IA (ollama)
            var (answer, code) = await chatIAOrchestrator.AskAiAsync(input.ThreadId, userMsg.Content, ct);

            // 4) atualiza placeholder bot
            botMsg.SetContent(answer.Data!); // se não tiver, mude Content setter ou crie método
            await context.SaveChangesAsync(ct);

            // 5) retorna DTO pronto pra UI
            var outDto = new SendMsgOutDto(
                input.ThreadId,
                new MsgOutDto(userMsg.Id, userMsg.ThreadId, userMsg.TypeMsg, userMsg.Content, userMsg.CreatedAt),
                new MsgOutDto(botMsg.Id, botMsg.ThreadId, botMsg.TypeMsg, botMsg.Content, botMsg.CreatedAt)
            );

            return (new Response<SendMsgOutDto?>(outDto, ""), 200);
        }
        catch (Exception ex)
        {
            return (new Response<SendMsgOutDto?>(null, ex.Message), 500);
        }
    }

    
    
    
    //-----------------------

    
    private static bool TryParseCursor(string cursor, out DateTimeOffset createdAtUtc, out long id)
    {
        createdAtUtc = default;
        id = default;

        // formato: "ticks:id"
        var parts = cursor.Split(':', 2);
        if (parts.Length != 2) return false;

        if (!long.TryParse(parts[0], out var ticks)) return false;
        if (!long.TryParse(parts[1], out id)) return false;

        // ticks sempre em UTC
        createdAtUtc = new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
        return true;
    }

    private static string BuildCursor(DateTimeOffset createdAt, long id)
    {
        // garante ticks UTC
        var utc = createdAt.ToUniversalTime();
        return $"{utc.UtcTicks}:{id}";
    }
    
}
