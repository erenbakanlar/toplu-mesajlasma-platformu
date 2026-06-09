namespace MessagingPlatform.Models;

public class GroupMessage
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public MessageGroup Group { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}
