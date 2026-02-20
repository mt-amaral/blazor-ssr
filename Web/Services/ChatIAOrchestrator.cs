using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI; // OpenAIPromptExecutionSettings / FunctionChoiceBehavior
using Web.Context;
using Web.Dto;
using Web.Services.Abstractions;

namespace Web.Services;

public class ChatIAOrchestrator : IChatIAOrchestrator
{
    private const string PluginName = "UserTools";
    private const string CreateUserFunction = "create_user";

    private readonly Kernel _kernel;
    private readonly UserPlugin _userPlugin;
    private readonly ApplicationDbContext _context;
    private readonly IUserLoggedService _userLoggedService;
    private readonly IChatCompletionService _chat;

    public ChatIAOrchestrator(
        Kernel kernel,
        UserPlugin userPlugin,
        ApplicationDbContext context,
        IUserLoggedService userLoggedService,
        IChatCompletionService chat)
    {
        _kernel = kernel;
        _userPlugin = userPlugin;
        _context = context;
        _userLoggedService = userLoggedService;
        _chat = chat;

        // ✅ registra ferramentas (plugins) no kernel
        _kernel.ImportPluginFromObject(_userPlugin, PluginName);
    }

    public async Task<(Response<string?>, short)> AskAiAsync(
        Guid threadId,
        string text,
        CancellationToken ct = default)
    {
        try
        {
            if (threadId == Guid.Empty)
                return (new Response<string?>(null, "ThreadId inválido."), 400);

            if (string.IsNullOrWhiteSpace(text))
                return (new Response<string?>(null, "Mensagem vazia."), 400);

            var user = await _userLoggedService.GetUserLoggedAsync();

            var threadOk = await _context.ChatThread
                .AsNoTracking()
                .AnyAsync(t => t.Id == threadId && t.UserId == user.Id, ct);

            if (!threadOk)
                return (new Response<string?>(null, "Thread não encontrado."), 404);

            var historyMsgs = await _context.ChatMessage
                .AsNoTracking()
                .Where(m => m.ThreadId == threadId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(30)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);

            var history = new ChatHistory();

            // ✅ instrução para tooling (para Ollama: força JSON estrito para você conseguir executar manualmente)
            history.AddSystemMessage("""
                                     Você é um assistente.

                                     Regras:
                                     - Você pode conversar normalmente com o usuário.
                                     - SOMENTE quando o usuário pedir para criar usuário/conta, responda APENAS com JSON no formato:
                                       {"name":"UserTools.create_user","parameters":{"name":"...","email":"...","password":"..."}}

                                     - Se o usuário quiser criar usuário mas faltar name/email/password, responda normalmente pedindo exatamente os campos que faltam.
                                     - Nunca invente dados.
                                     - Não repita a senha na resposta.
                                     """);

            foreach (var m in historyMsgs)
            {
                if (m.TypeMsg == TypeMsg.User)
                    history.AddUserMessage(m.Content);
                else
                    history.AddAssistantMessage(m.Content);
            }

            history.AddUserMessage(text.Trim());

            // ✅ settings continuam ok; com Ollama ele pode não auto-invocar, mas mantemos
            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var result = await _chat.GetChatMessageContentAsync(
                history,
                executionSettings: settings,
                kernel: _kernel,
                cancellationToken: ct);

            var content = result?.Content ?? "";

            // ✅ Fallback para Ollama: se vier JSON de tool-call em texto, executa a tool manualmente
            if (TryParseToolJson(content, out var fullToolName, out var toolArgs, out var missing))
            {
                // Se o modelo disse que faltam campos, apenas retorne uma mensagem humana
                if (missing is { Count: > 0 })
                {
                    var faltando = string.Join(", ", missing);
                    return (new Response<string?>($"Faltam dados para criar o usuário: {faltando}.", ""), 200);
                }

                // Esperado: "UserTools.create_user"
                var (pluginName, functionName) = SplitToolName(fullToolName);

                if (!string.Equals(pluginName, PluginName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(functionName, CreateUserFunction, StringComparison.OrdinalIgnoreCase))
                {
                    // segurança: não executa ferramentas fora do allowlist
                    return (new Response<string?>("Ferramenta solicitada não é permitida.", ""), 400);
                }

                toolArgs.TryGetValue("name", out var name);
                toolArgs.TryGetValue("email", out var email);
                toolArgs.TryGetValue("password", out var password);

                // validação mínima (não confia no modelo)
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    return (new Response<string?>("Faltam dados para criar o usuário. Envie nome, email e senha.", ""), 200);
                }

                var kernelArgs = new KernelArguments
                {
                    ["name"] = name!,
                    ["email"] = email!,
                    ["password"] = password!
                };

                var toolResult = await _kernel.InvokeAsync(pluginName, functionName, kernelArgs, ct);
                var toolText = toolResult?.GetValue<string>() ?? "";

                // sua regra de verdade: só confirma sucesso se vier OK:
                if (toolText.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                    return (new Response<string?>("Usuário criado com sucesso.", ""), 200);

                if (toolText.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
                    return (new Response<string?>(toolText, ""), 400);

                return (new Response<string?>(toolText, ""), 200);
            }

            // resposta normal (sem tool)
            return (new Response<string?>(content, ""), 200);
        }
        catch (Exception ex)
        {
            return (new Response<string?>(null, "Tool/LLM error: " + ex.Message), 500);
        }
    }

    private static (string pluginName, string functionName) SplitToolName(string fullToolName)
    {
        if (string.IsNullOrWhiteSpace(fullToolName))
            return ("", "");

        var parts = fullToolName.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
            return (parts[0], parts[1]);

        // se vier apenas "create_user"
        return (PluginName, fullToolName.Trim());
    }

    private static bool TryParseToolJson(
        string content,
        out string toolName,
        out Dictionary<string, string?> toolArgs,
        out List<string>? missing)
    {
        toolName = "";
        toolArgs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        missing = null;

        content = (content ?? "").Trim();
        if (!(content.StartsWith("{") && content.EndsWith("}")))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Caso: {"name":null,"missing":[...]}
            if (root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.Null)
            {
                if (root.TryGetProperty("missing", out var missingProp) && missingProp.ValueKind == JsonValueKind.Array)
                {
                    missing = missingProp.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    return true;
                }

                missing = new List<string> { "name", "email", "password" };
                return true;
            }

            if (!root.TryGetProperty("name", out var nameOk))
                return false;

            toolName = nameOk.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(toolName))
                return false;

            if (root.TryGetProperty("parameters", out var p) && p.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in p.EnumerateObject())
                {
                    toolArgs[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}