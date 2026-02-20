using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Web.Plugins;

/// <summary>
/// Exemplo de plugin para demonstrar escalabilidade e padrão.
/// Você pode criar quantos plugins quiser implementando IPlugin.
///
/// Para usar:
/// 1. Criar a classe implementando IPlugin
/// 2. Adicionar métodos com [KernelFunction]
/// 3. O Program.cs automaticamente vai descobrir e registrar
/// </summary>
public class TaskPlugin : IPlugin
{
    private readonly ILogger<TaskPlugin> _logger;

    public string PluginName => "TaskTools";
    public string Description => "Ferramentas para gerenciar tarefas e projetos.";

    public TaskPlugin(ILogger<TaskPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("create_task")]
    [Description("Cria uma nova tarefa com titulo e descricao.")]
    public async Task<string> CreateTaskAsync(
        [Description("Titulo da tarefa")] string title,
        [Description("Descricao detalhada")] string description,
        [Description("Prioridade: alta, media, baixa")] string priority = "media",
        CancellationToken ct = default)
    {
        _logger.LogInformation("Criando tarefa: {Title}", title);

        // Aqui você implementaria lógica real, por enquanto é um exemplo
        if (string.IsNullOrWhiteSpace(title))
            return "ERR: Titulo da tarefa é obrigatório.";

        // Simulando criação bem-sucedida
        return $"OK: Tarefa '{title}' criada com sucesso com prioridade {priority}.";
    }

    [KernelFunction("list_tasks")]
    [Description("Lista tarefas com filtro por status.")]
    public async Task<string> ListTasksAsync(
        [Description("Status: pending, done, cancelled (opcional)")] string? status = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Listando tarefas com status: {Status}", status ?? "todas");

        // Simulando resposta
        return status switch
        {
            "pending" => "OK: Tarefas pendentes: Tarefa 1, Tarefa 2, Tarefa 3",
            "done" => "OK: Tarefas concluidas: Tarefa 4, Tarefa 5",
            _ => "OK: Todas as tarefas: Tarefa 1 (pendente), Tarefa 2 (pendente), Tarefa 4 (concluida)"
        };
    }

    [KernelFunction("complete_task")]
    [Description("Marca uma tarefa como concluida.")]
    public async Task<string> CompleteTaskAsync(
        [Description("ID da tarefa")] int taskId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Marcando tarefa {TaskId} como concluida", taskId);

        if (taskId <= 0)
            return "ERR: ID da tarefa inválido.";

        return $"OK: Tarefa #{taskId} marcada como concluida.";
    }
}
