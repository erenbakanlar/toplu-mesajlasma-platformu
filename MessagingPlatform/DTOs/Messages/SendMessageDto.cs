using System.ComponentModel.DataAnnotations;

namespace MessagingPlatform.DTOs.Messages;

public class SendMessageDto
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
