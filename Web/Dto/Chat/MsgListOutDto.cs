namespace Web.Dto.Chat;

public record MsgListOutDto(
    Guid ThreadId,
    IReadOnlyList<MsgOutDto> Items,
    string? NextCursor,       
    bool HasMore
);