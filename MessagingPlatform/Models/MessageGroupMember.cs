namespace MessagingPlatform.Models;

public class MessageGroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public MessageGroup Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
