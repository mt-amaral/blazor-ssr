using Web.Entity;
using Web.Entity.Identity;

public class ChatMessage
{
    public long Id { get; set; }

    public long ThreadId { get; set; }
    public ChatThread Thread { get; set; } = default!;

    // quem enviou (null para assistant/system)
    public string? UserId { get; set; }
    public User? User { get; set; }

    public ChatRole Role { get; set; }          // User/Assistant/System/Tool
    public string Content { get; set; } = "";   // texto final (persistido)

    // status da mensagem (para streaming / "Generating…")
    public ChatMessageStatus Status { get; set; } = ChatMessageStatus.Completed;

    // metadados úteis p/ IA
    public string? Model { get; set; }          // ex: "gpt-4o-mini"
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public decimal? CostUsd { get; set; }       // se você rastrear custo

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // feedback do usuário nessa msg do assistant (like/dislike)
    public ChatFeedback? Feedback { get; set; }

    // anexos opcionais
    public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
}

public enum ChatRole : short
{
    User = 1,
    Assistant = 2,
    System = 3,
    Tool = 4
}

public enum ChatMessageStatus : short
{
    Draft = 1,
    Streaming = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}