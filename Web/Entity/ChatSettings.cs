using System.ComponentModel;
using Web.Entity.Identity;

namespace Web.Entity;


public class ChatSettings
{
    public ChatSettings()
    {
        
    }
    
    public ChatSettings(string content)
    {
        Content = content;
    }
    public int Id { get; init; }
    public string Content { get; private set; } = string.Empty;
    public void SetContent(string content) =>  Content = content;
}
