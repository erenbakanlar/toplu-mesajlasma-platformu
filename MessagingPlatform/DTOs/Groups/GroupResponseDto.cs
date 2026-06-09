namespace MessagingPlatform.DTOs.Groups;

public class GroupResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public List<GroupMemberDto> Members { get; set; } = new();
}

public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
