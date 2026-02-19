using Web.Entity;
using Web.Entity.Identity;

public class ChatMessage
{
    public ChatMessage()
    {
        
    }
    public long Id { get; init; }


    public Guid ThreadId { get; private set; }


    public ChatThread Thread { get; private set; }


    public long UserId { get; private set; }
    
    public User User { get; private set; }

    public string Content { get; private set; } = string.Empty;

}
