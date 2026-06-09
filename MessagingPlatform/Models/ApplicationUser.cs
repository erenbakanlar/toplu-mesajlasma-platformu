using Microsoft.AspNetCore.Identity;

namespace MessagingPlatform.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<MessageGroupMember> GroupMemberships { get; set; } = new List<MessageGroupMember>();
    public ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();
    public ICollection<MessageGroup> CreatedGroups { get; set; } = new List<MessageGroup>();
}
