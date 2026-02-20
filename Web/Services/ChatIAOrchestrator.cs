using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Web.Context;
using Web.Dto;
using Web.Services.Abstractions;

namespace Web.Services;

public class ChatIAOrchestrator : IChatIAOrchestrator
{
    private readonly Kernel _kernel;
    private readonly ApplicationDbContext _context;
    private readonly IUserLoggedService _userLoggedService;
    private readonly IChatCompletionService _chat;
    private readonly ILogger<ChatIAOrchestrator> _logger;

    // Segurança anti-loop
    private const int MaxToolIterations = 2;

    public ChatIAOrchestrator(
        Kernel kernel,
        ApplicationDbContext context,
        IUserLoggedService userLoggedService,
        IChatCompletionService chat,
        ILogger<ChatIAOrchestrator> logger)
    {
        _kernel = kernel;
        _context = context;
        _userLoggedService = userLoggedService;
        _chat = chat;
        _logger = logger;
    }

    public async Task<(Response<string?>, short)> AskAiAsync(
        Guid threadId,
        string text,
        CancellationToken ct = default)
    {
        try
        {
            if (threadId == Guid.Empty) return (new Response<string?>(null, "ThreadId inválido."), 400);
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

            // 1) Monta histórico base (sem prompt agressivo)
            var baseHistory = new ChatHistory();
            baseHistory.AddSystemMessage(BuildBaseSystemPrompt());

            foreach (var m in historyMsgs)
            {
                if (m.TypeMsg == TypeMsg.User) baseHistory.AddUserMessage(m.Content);
                else baseHistory.AddAssistantMessage(m.Content);
            }

            var userText = text.Trim();

            // 2) Loop de roteamento (no máximo 2 chamadas de tool)
            var availableToolsDescription = BuildAvailableToolsDescription();

            var workingHistory = CloneHistory(baseHistory);
            workingHistory.AddUserMessage(userText);

            for (var i = 0; i < MaxToolIterations; i++)
            {
                // ROUTER: sempre devolve JSON (interno)
                var routerHistory = new ChatHistory();
                routerHistory.AddSystemMessage(BuildRouterPrompt(availableToolsDescription));
                CopyConversation(routerHistory, workingHistory);

                var routerSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    TopP = 1,
                    // Não depende de function calling do provider
                    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                };

                var routerMsg = await _chat.GetChatMessageContentAsync(
                    routerHistory,
                    executionSettings: routerSettings,
                    kernel: _kernel,
                    cancellationToken: ct);

                var routerRaw = (routerMsg?.Content ?? "").Trim();
                _logger.LogInformation("🧭 Router raw: {Content}", routerRaw);

                if (!TryParseRouterJson(routerRaw, out var decision, out var parseErr))
                {
                    _logger.LogWarning("❌ Router JSON inválido: {Err}. Raw: {Raw}", parseErr, routerRaw);

                    // Fallback: se não parseou, responde “normal” com a própria saída do modelo
                    // (melhor do que quebrar a conversa)
                    return (new Response<string?>(routerRaw, ""), 200);
                }

                if (decision.Type == "final")
                {
                    return (new Response<string?>(decision.Content ?? "", ""), 200);
                }

                if (decision.Type == "tool")
                {
                    var plugin = decision.Plugin ?? "";
                    var function = decision.Function ?? "";

                    if (string.IsNullOrWhiteSpace(plugin) || string.IsNullOrWhiteSpace(function))
                        return (new Response<string?>(null, "Router pediu tool mas não informou plugin/function."), 400);

                    try
                    {
                        var args = new KernelArguments();
                        if (decision.Parameters != null)
                        {
                            foreach (var kv in decision.Parameters)
                                args[kv.Key] = kv.Value;
                        }

                        _logger.LogInformation("🔧 Executando tool: {Plugin}.{Function} args={Args}",
                            plugin, function, JsonSerializer.Serialize(args));

                        var toolResult = await _kernel.InvokeAsync(plugin, function, args, ct);
                        var toolText = toolResult?.GetValue<string>() ?? "";

                        _logger.LogInformation("🔧 Tool result: {Result}", toolText);

                        if (toolText.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
                            return (new Response<string?>(toolText, ""), 400);

                        // Agora a parte importante: transformar tool result em resposta humana BOA.
                        var finalHistory = CloneHistory(workingHistory);
                        finalHistory.AddSystemMessage(BuildFinalizerPrompt());
                        finalHistory.AddAssistantMessage($"[TOOL_RESULT {plugin}.{function}] {toolText}");

                        var finalSettings = new OpenAIPromptExecutionSettings
                        {
                            Temperature = 0.7,
                            TopP = 0.95,
                            // trava ferramentas aqui pra evitar loop
                            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
                        };

                        var finalMsg = await _chat.GetChatMessageContentAsync(
                            finalHistory,
                            executionSettings: finalSettings,
                            kernel: _kernel,
                            cancellationToken: ct);

                        var finalText = (finalMsg?.Content ?? "").Trim();
                        return (new Response<string?>(finalText, ""), 200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao executar {Plugin}.{Function}", plugin, function);
                        return (new Response<string?>($"Falha ao executar {plugin}.{function}: {ex.Message}", ""), 400);
                    }
                }

                // tipo desconhecido
                return (new Response<string?>(null, $"Tipo de decisão desconhecido: {decision.Type}"), 400);
            }

            // Se estourou iterações, responde normal
            return (new Response<string?>("Não consegui completar a ação com ferramentas. Pode reformular?", ""), 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na IA ou Orquestração");
            return (new Response<string?>(null, "Erro na IA ou Orquestração: " + ex.Message), 500);
        }
    }

    // =========================
    // PROMPTS
    // =========================

    private string BuildBaseSystemPrompt()
    {
        return
            "Você é um assistente conversável, objetivo e útil. " +
            "Responda naturalmente, com clareza, e faça perguntas curtas só quando realmente necessário.";
    }

    private string BuildRouterPrompt(string availableToolsDescription)
    {
        // Router NÃO conversa com usuário. Só decide ação.
        // IMPORTANTÍSSIMO: JSON válido => aspas DUPLAS.
        return $@"
Você é um roteador (router) para um chatbot com ferramentas.

TAREFA:
- Analise a conversa e a última mensagem do usuário.
- Decida UMA das opções:
  1) Responder direto (sem ferramenta)
  2) Chamar UMA ferramenta específica

SAÍDA: Retorne APENAS um JSON válido em UMA LINHA (sem markdown, sem texto extra).

SCHEMAS:

1) Resposta direta:
{{""type"":""final"",""content"":""texto da resposta""}}

2) Chamar ferramenta:
{{""type"":""tool"",""plugin"":""NomePlugin"",""function"":""NomeFuncao"",""parameters"":{{ ... }}}}

REGRAS:
- Use sempre aspas DUPLAS (JSON válido).
- Se for ""tool"", escolha apenas UMA ferramenta por vez.
- ""parameters"" deve conter apenas os parâmetros necessários (strings simples sempre que possível).
- Se o usuário só quer conversar, use ""final"".

FERRAMENTAS DISPONÍVEIS:
{availableToolsDescription}
".Trim();
    }

    private string BuildFinalizerPrompt()
    {
        return @"
Você recebeu um resultado de ferramenta (TOOL_RESULT).
Use esse resultado para responder o usuário de forma natural, objetiva e útil.
- Não mostre JSON.
- Se o resultado vier vazio/estranho, peça uma informação mínima para prosseguir.
".Trim();
    }

    // =========================
    // TOOLS DESCRIPTION
    // =========================

    private string BuildAvailableToolsDescription()
    {
        try
        {
            var plugins = _kernel.Plugins;
            if (!plugins.Any())
                return "Nenhuma ferramenta disponível no momento.";

            var sb = new StringBuilder();

            foreach (var plugin in plugins)
            {
                var pluginName = plugin.Name ?? "Unknown";
                sb.AppendLine($"\nPlugin: {pluginName}");

                foreach (var function in plugin)
                {
                    var funcName = function.Name;
                    var funcDesc = function.Description ?? "Sem descrição";
                    sb.AppendLine($"- {funcName}: {funcDesc}");
                }
            }

            var result = sb.ToString();
            _logger.LogInformation("📋 Ferramentas disponíveis:\n{Tools}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao construir descrição de ferramentas");
            return "Ferramentas disponíveis no kernel.";
        }
    }

    // =========================
    // ROUTER JSON PARSE
    // =========================

    private sealed class RouterDecision
    {
        public string? Type { get; set; }           // "final" | "tool"
        public string? Content { get; set; }        // para final
        public string? Plugin { get; set; }         // para tool
        public string? Function { get; set; }       // para tool
        public Dictionary<string, object?>? Parameters { get; set; } // para tool
    }

    private bool TryParseRouterJson(string raw, out RouterDecision decision, out string error)
    {
        decision = new RouterDecision();
        error = "";

        raw = (raw ?? "").Trim();
        var json = ExtractJsonFromContent(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Nenhum JSON encontrado.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeEl))
            {
                error = "Campo 'type' ausente.";
                return false;
            }

            var type = (typeEl.GetString() ?? "").Trim().ToLowerInvariant();
            decision.Type = type;

            if (type == "final")
            {
                decision.Content = root.TryGetProperty("content", out var c) ? (c.GetString() ?? "") : "";
                return true;
            }

            if (type == "tool")
            {
                decision.Plugin = root.TryGetProperty("plugin", out var p) ? (p.GetString() ?? "") : "";
                decision.Function = root.TryGetProperty("function", out var f) ? (f.GetString() ?? "") : "";

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("parameters", out var prm) && prm.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in prm.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.Number => prop.Value.GetRawText(),  // mantém como string, SK lida bem
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Object => prop.Value.GetRawText(),
                            JsonValueKind.Array => prop.Value.GetRawText(),
                            _ => prop.Value.GetRawText()
                        };
                    }
                }

                decision.Parameters = dict;
                return true;
            }

            error = $"Tipo inválido: {type}";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string? ExtractJsonFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        int jsonStart = content.IndexOf('{');
        if (jsonStart < 0)
            return null;

        int braceCount = 0;
        for (int i = jsonStart; i < content.Length; i++)
        {
            if (content[i] == '{') braceCount++;
            if (content[i] == '}') braceCount--;

            if (braceCount == 0)
                return content.Substring(jsonStart, i - jsonStart + 1);
        }

        return null;
    }

    // =========================
    // CHAT HISTORY HELPERS
    // =========================

    private static ChatHistory CloneHistory(ChatHistory source)
    {
        var h = new ChatHistory();
        CopyConversation(h, source);
        return h;
    }

    private static void CopyConversation(ChatHistory target, ChatHistory source)
    {
        foreach (var msg in source)
        {
            var content = msg.Content ?? "";

            // SK normalmente usa AuthorRole: System/User/Assistant/Tool
            if (msg.Role == AuthorRole.System)
                target.AddSystemMessage(content);
            else if (msg.Role == AuthorRole.User)
                target.AddUserMessage(content);
            else if (msg.Role == AuthorRole.Assistant)
                target.AddAssistantMessage(content);
            else if (msg.Role == AuthorRole.Tool)
                target.Add(new ChatMessageContent(AuthorRole.Tool, content));
            else
                target.Add(new ChatMessageContent(msg.Role, content));
        }
    }
}