namespace Web.Dto.Chat;

public record MsgListInDto(
    long ThreadId,
    int Limit = 50,
    string? BeforeCursor = null 
);