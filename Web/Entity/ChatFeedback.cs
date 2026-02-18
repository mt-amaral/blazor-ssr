using Web.Entity.Identity;

public class ChatFeedback
{
    public long Id { get; set; }

    public long MessageId { get; set; }
    public ChatMessage Message { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;

    public FeedbackRating Rating { get; set; }  // Good/Bad
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum FeedbackRating : short
{
    Bad = 0,
    Good = 1
}