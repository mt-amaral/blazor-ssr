using Web.Entity.Identity;

namespace Web.Entity;

public class ChatThread
{
    public ChatThread()
    {
        
    }
    
    public Guid Id { get; init; }


    public long UserId { get; private set; } 


    public User User { get; private set; } 


    public string Title { get; private set; }  = string.Empty;

    

    public DateTimeOffset CreatedAt { get;  private set; } 

    public DateTimeOffset UpdatedAt { get; private set; } 


    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

}
