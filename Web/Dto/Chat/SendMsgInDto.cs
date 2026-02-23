using Web.Entity.Enum;

namespace Web.Dto.Chat;

public record SendMsgInDto(
    long ThreadId,
    string Content,
    Models Model
);