namespace Web.Dto.Chat;

public record SendMsgOutDto(
    Guid ThreadId,
    MsgOutDto UserMessage,
    MsgOutDto? BotMessage // pode vir null se você ainda não quer criar placeholder
);