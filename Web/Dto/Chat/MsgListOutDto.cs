namespace Web.Dto.Chat;

public record MsgListOutDto(
    long ThreadId,
    IReadOnlyList<MsgOutDto> Items,
    string? NextCursor,       
    bool HasMore
);