namespace Web.Dto.Chat;

public record SendMsgInDto(
    long ThreadId,
    string Content
);