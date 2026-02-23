using Web.Entity.Identity;

namespace Web.Entity;

public class ChatThread
{
    public ChatThread()
    {
        
    }
    
    public ChatThread(long userId, string title)
    {
        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title) ? "New Chat" : title.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    
    public long Id { get; init; }

    public long UserId { get; private set; } 
    
    public User User { get; private set; } 
    
    public string Title { get; private set; }  = string.Empty;

    public DateTimeOffset CreatedAt { get;  private set; }

    public DateTimeOffset? UpdatedAt { get; private set; } = null;
    
    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();
    
    public void UpdateAt() =>  UpdatedAt = DateTimeOffset.Now;

    public void UpdateTitle(string title) => Title = title;
}
