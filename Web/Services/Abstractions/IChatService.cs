using Web.Dto;
using Web.Dto.Account;
using Web.Dto.Chat;

namespace Web.Services.Abstractions;

public interface IChatService
{
    Task<(Response<ChatThreadOutDto?>, short)> CreateThreadAsync(string? title = "New Chat",
        CancellationToken ct = default);

    Task<(Response<List<ChatThreadOutDto?>>, short)> GetThreadByUserAsync();
    Task<(Response<MsgListOutDto?>, short)> ListMsgsAsync(MsgListInDto input, CancellationToken ct = default);
    Task<(Response<SendMsgOutDto?>, short)> SendMsgAsync(
        SendMsgInDto input,
        CancellationToken ct = default
    );
}