namespace MessagingPlatform.Models;

public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public ApplicationUser Sender { get; set; } = null!;
    public ApplicationUser Receiver { get; set; } = null!;
}
