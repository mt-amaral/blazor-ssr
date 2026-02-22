namespace Web.Dto.Chat;

public record MsgOutDto(
    long Id,
    long ThreadId,
    TypeMsg Type,              
    string Content,
    DateTimeOffset CreatedAt
);

