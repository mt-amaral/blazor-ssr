using Web.Dto;
using Web.Dto.Account;

namespace Web.Services.Abstractions;

public interface IChatIAOrchestrator
{

    Task<(Response<string?>, short)> AskAiAsync(
        long threadId,
        string text,
        CancellationToken ct = default);

}