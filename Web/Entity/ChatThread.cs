using Web.Entity.Identity;

namespace Web.Entity;

public class ChatThread
{
    public long Id { get; set; }

    // dono do thread
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    // título
    public string Title { get; set; } = "New chat";

    // "soft delete"/arquivar (opcional)
    public bool IsArchived { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }

    // timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // navegação
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    // concorrência (opcional)
    public byte[] RowVersion { get; set; } = default!;
}
