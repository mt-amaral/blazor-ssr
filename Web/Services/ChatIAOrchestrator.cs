using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Web.Context;
using Web.Dto;
using Web.Entity.Enum;
using Web.Extensions;
using Web.Services.Abstractions;

namespace Web.Services;

public class ChatIAOrchestrator : IChatIAOrchestrator
{
    private readonly Kernel _kernel;
    private readonly ApplicationDbContext _context;
    private readonly IUserLoggedService _userLoggedService;
    private readonly IChatCompletionService _chat;

    public ChatIAOrchestrator(
        Kernel kernel,
        ApplicationDbContext context,
        IUserLoggedService userLoggedService,
        IChatCompletionService chat)
    {
        _kernel = kernel;
        _context = context;
        _userLoggedService = userLoggedService;
        _chat = chat;
    }

    public async Task<(Response<string?>, short)> AskAiAsync(
        long threadId,
        string text,
        Models model,
        CancellationToken ct = default)
    {
        try
        {
            if (threadId == 0) return (new Response<string?>(null, "ThreadId inválido."), 400);
            if (string.IsNullOrWhiteSpace(text)) return (new Response<string?>(null, "Mensagem vazia."), 400);

            var user = await _userLoggedService.GetUserLoggedAsync();

            var threadOk = await _context.ChatThread
                .AsNoTracking()
                .AnyAsync(t => t.Id == threadId && t.UserId == user.Id, ct);

            if (!threadOk) return (new Response<string?>(null, "Thread não encontrado."), 404);

            var historyMsgs = await _context.ChatMessage
                .AsNoTracking()
                .Where(m => m.ThreadId == threadId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(30)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);

            var history = new ChatHistory();
            var systemPrompt = _context.ChatSettings.AsNoTracking().FirstOrDefault()!.Content;

            history.AddSystemMessage(systemPrompt);

            foreach (var m in historyMsgs)
            {
                if (m.TypeMsg == TypeMsg.User) history.AddUserMessage(m.Content as string);
                else history.AddAssistantMessage(m.Content);
            }

            history.AddUserMessage(text.Trim());

            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var result = await _chat.GetChatMessageContentAsync(history, executionSettings: settings, kernel: _kernel, cancellationToken: ct);
            
            var content = result?.Content ?? "";

 
            return (new Response<string?>(content, ""), 200);
        }
        catch (Exception ex)
        {
            return (new Response<string?>(null, "Erro na IA ou Orquestração: " + ex.Message), 500);
        }
    }
}