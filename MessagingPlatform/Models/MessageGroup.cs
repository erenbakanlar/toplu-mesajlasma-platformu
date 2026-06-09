namespace MessagingPlatform.Models;

public class MessageGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedById { get; set; } = string.Empty;

    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<MessageGroupMember> Members { get; set; } = new List<MessageGroupMember>();
    public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
}
