using System.ComponentModel.DataAnnotations;

namespace MessagingPlatform.DTOs.Groups;

public class AddMembersDto
{
    [Required]
    public List<string> UserIds { get; set; } = new();
}
