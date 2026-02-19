namespace Web.Dto.Chat;

public record MsgListInDto(
    Guid ThreadId,
    int Limit = 50,
    string? BeforeCursor = null 
);