using Web.Entity;

public class ChatRun
{
    public long Id { get; set; }

    public long ThreadId { get; set; }
    public ChatThread Thread { get; set; } = default!;

    public long? UserMessageId { get; set; }        // msg do user que disparou
    public long? AssistantMessageId { get; set; }   // msg do assistant gerada

    public string Model { get; set; } = default!;
    public ChatRunStatus Status { get; set; } = ChatRunStatus.Completed;

    public string? Error { get; set; }
    public int? HttpStatus { get; set; }

    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}

public enum ChatRunStatus : short
{
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}