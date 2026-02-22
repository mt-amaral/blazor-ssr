using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public ChatIAOrchestrator(
        Kernel kernel, // O Kernel já deve vir com TODOS os plugins registrados via DI
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

            // Gerar descrição dinâmica das ferramentas disponíveis
            var availableToolsDescription = BuildAvailableToolsDescription();

            // ✅ Prompt aprimorado para conversas humanizadas com suporte a ferramentas
            var systemPrompt = $@"Você é um assistente útil, amigável e conversável.

IMPORTANTE: Mantenha conversas NATURAIS e normais. Responda perguntas simples sem usar ferramentas.
Use ferramentas APENAS quando o usuário EXPLICITAMENTE PEDIR uma ação específica (criar, deletar, listar, etc).

EXEMPLOS DE QUANDO USAR FERRAMENTAS:
- 'Cria um usuário chamado João' → use create_user
- 'Lista todos os usuários' → use list_users
- 'Delete o usuário 123' → use delete_user
- 'Cria uma tarefa' → use create_task

EXEMPLOS DE QUANDO NÃO USAR FERRAMENTAS:
- 'Oi, como você vai?' → just respond
- 'O que você consegue fazer?' → describe capabilities without calling tools
- 'Quanto é 2+2?' → just answer
- Small talk, perguntas gerais → respond naturally

FERRAMENTAS DISPONÍVEIS:
{availableToolsDescription}

Quando o usuário EXPLICITAMENTE PEDIR para usar uma ferramenta, retorne APENAS JSON neste formato:
JSON: {{""plugin"":""NomeDaFerramenta"",""function"":""nome_funcao"",""parameters"":{{""chave"":""valor""}}}}

Nunca retorne JSON com verbos ou textos - apenas o JSON da ferramenta.
Se não tiver certeza se deve chamar uma ferramenta, RESPONDA EM TEXTO primeiro e pergunte o que o usuário quer.";

            // Adicionar o prompt dinâmico ao histórico
            history.AddSystemMessage(systemPrompt);

            foreach (var m in historyMsgs)
            {
                if (m.TypeMsg == TypeMsg.User) history.AddUserMessage(m.Content as string);
                else history.AddAssistantMessage(m.Content);
            }

            history.AddUserMessage(text.Trim());

            var settings = new OpenAIPromptExecutionSettings
            {
                // ⚠️ IMPORTANTE: None() faz a IA NUNCA chamar ferramentas automaticamente
                // A IA só retorna JSON de ferramentas quando explicitamente instruída pelo usuário
                FunctionChoiceBehavior = FunctionChoiceBehavior.None()
            };

            var result = await _chat.GetChatMessageContentAsync(history, executionSettings: settings, kernel: _kernel, cancellationToken: ct);
            var content = result?.Content ?? "";

            // ✅ Execução Dinâmica de Ferramentas (Desacoplado)
            if (TryParseToolJson(content, out var pluginName, out var functionName, out var kernelArgs))
            {
                try
                {
                    // Deixa o Semantic Kernel encontrar o plugin e executar
                    var toolResult = await _kernel.InvokeAsync(pluginName, functionName, kernelArgs, ct);
                    var toolText = toolResult?.GetValue<string>() ?? "";

                    // Padronização simples de retorno
                    if (toolText.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
                        return (new Response<string?>(toolText, ""), 400);

                    return (new Response<string?>(toolText, ""), 200);
                }
                catch (Exception ex)
                {
                    // Pode capturar exceções específicas do SK caso a função não exista
                    return (new Response<string?>($"Falha ao executar {pluginName}.{functionName}: {ex.Message}", ""), 400);
                }
            }

            // Resposta normal sem ferramentas
            return (new Response<string?>(content, ""), 200);
        }
        catch (Exception ex)
        {
            return (new Response<string?>(null, "Erro na IA ou Orquestração: " + ex.Message), 500);
        }
    }

    /// <summary>
    /// Constrói uma descrição textual das ferramentas disponíveis no kernel
    /// </summary>
    private string BuildAvailableToolsDescription()
    {
        try
        {
            var plugins = _kernel.Plugins;
            if (!plugins.Any())
                return "Nenhuma ferramenta disponível no momento.";

            var sb = new System.Text.StringBuilder();

            foreach (var plugin in plugins)
            {
                var pluginName = plugin.Name ?? "Unknown";
                sb.AppendLine($"\n📌 {pluginName}:");

                foreach (var function in plugin)
                {
                    var funcName = function.Name;
                    var funcDesc = function.Description ?? "Sem descrição";
                    sb.AppendLine($"   • {funcName}: {funcDesc}");
                }
            }

            return sb.ToString();
        }
        catch
        {
            return "Ferramentas disponíveis no kernel.";
        }
    }

    /// <summary>
    /// Faz o parse de qualquer chamada de ferramenta no formato {"plugin":"X","function":"Y","parameters":{...}}
    /// </summary>
    private static bool TryParseToolJson(
        string content,
        out string pluginName,
        out string functionName,
        out KernelArguments kernelArgs)
    {
        pluginName = string.Empty;
        functionName = string.Empty;
        kernelArgs = new KernelArguments();

        content = (content ?? "").Trim();
        if (!content.StartsWith("{") || !content.EndsWith("}"))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("plugin", out var pluginElement) || 
                !root.TryGetProperty("function", out var functionElement))
            {
                // Se estiver usando formato antigo do OpenAI {"name":"plugin.funcao"}, tenta adaptar:
                if (root.TryGetProperty("name", out var nameElement))
                {
                    var parts = nameElement.GetString()?.Split('.', 2);
                    if (parts?.Length == 2)
                    {
                        pluginName = parts[0];
                        functionName = parts[1];
                    }
                    else return false;
                }
                else return false;
            }
            else
            {
                pluginName = pluginElement.GetString() ?? "";
                functionName = functionElement.GetString() ?? "";
            }

            if (string.IsNullOrWhiteSpace(pluginName) || string.IsNullOrWhiteSpace(functionName))
                return false;

            // Extrai parâmetros dinamicamente
            if (root.TryGetProperty("parameters", out var p) && p.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in p.EnumerateObject())
                {
                    kernelArgs[prop.Name] = prop.Value.ValueKind == JsonValueKind.String 
                        ? prop.Value.GetString() 
                        : prop.Value.GetRawText();
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