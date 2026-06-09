using System.ComponentModel.DataAnnotations;

namespace MessagingPlatform.DTOs.Groups;

public class SendGroupMessageDto
{
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
