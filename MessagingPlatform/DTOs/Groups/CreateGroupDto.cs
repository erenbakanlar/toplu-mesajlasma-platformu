using System.ComponentModel.DataAnnotations;

namespace MessagingPlatform.DTOs.Groups;

public class CreateGroupDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<string> MemberIds { get; set; } = new();
}
