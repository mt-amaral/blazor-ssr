using Microsoft.SemanticKernel;

namespace Web.Plugins;

/// <summary>
/// Interface base para plugins do Semantic Kernel.
/// Cada plugin deve implementar esta interface para ser registrado automaticamente.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Nome único do plugin para identificação no kernel.
    /// Exemplo: "UserTools", "TaskTools", "EmailTools".
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// Descrição do plugin para ajudar a IA a decidir quando usar.
    /// </summary>
    string Description { get; }
}
