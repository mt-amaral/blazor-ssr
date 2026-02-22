using Web.Dto;
using Web.Dto.Account;
using Web.Entity.Enum;

namespace Web.Services.Abstractions;

public interface IChatIAOrchestrator
{

    Task<(Response<string?>, short)> AskAiAsync(
        long threadId,
        string text,
        Models model,
        CancellationToken ct = default);

}