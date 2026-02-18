public class ChatAttachment
{
    public long Id { get; set; }

    public long MessageId { get; set; }
    public ChatMessage Message { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }

    // onde está salvo (blob/s3/disk)
    public string StorageKey { get; set; } = default!; // ex: path, blob key etc.
    public string? Url { get; set; } // opcional (se público)

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}