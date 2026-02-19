namespace Web.Dto.Chat;

public record SendMsgInDto(
    Guid ThreadId,
    string Content
);