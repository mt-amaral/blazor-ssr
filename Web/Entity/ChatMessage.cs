using Web.Entity;
using Web.Entity.Identity;

public enum TypeMsg : byte
{
    Bot = 1,
    User = 2,
}

public class ChatMessage
{
    public ChatMessage()
    {
        
    }

    public ChatMessage(Guid threadId, string content, long userId, TypeMsg type)
    {
        UserId = userId;
        ThreadId = threadId;
        Content = content;
        TypeMsg = type;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public long Id { get; init; }


    public Guid ThreadId { get; private set; }


    public ChatThread Thread { get; private set; }

    public TypeMsg  TypeMsg { get; private set; }
    public long UserId { get; private set; }
    
    public User User { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public string Content { get; private set; } = string.Empty;
    
    
    public void SetContent(string content)
    {
        Content = content ?? "";
    }
    

}
