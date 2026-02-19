namespace Web.Dto.Chat;

public record MsgOutDto(
    long Id,
    Guid ThreadId,
    TypeMsg Type,              
    string Content,
    DateTimeOffset CreatedAt
);

