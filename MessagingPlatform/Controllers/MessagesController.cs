using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MessagingPlatform.Data;
using MessagingPlatform.DTOs.Messages;
using MessagingPlatform.Hubs;
using MessagingPlatform.Models;

namespace MessagingPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    /// <summary>Mesaj gönder. Admin herkese, üye yalnızca yöneticilere gönderebilir.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<MessageResponseDto>> SendMessage([FromBody] SendMessageDto dto)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var receiver = await _userManager.FindByIdAsync(dto.ReceiverId);
        if (receiver == null)
            return NotFound(new { message = "Alıcı kullanıcı bulunamadı." });

        // Normal üyeler yalnızca yöneticilere yanıt verebilir
        if (!User.IsInRole("Admin"))
        {
            var receiverIsAdmin = await _userManager.IsInRoleAsync(receiver, "Admin");
            if (!receiverIsAdmin)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Üyeler yalnızca yöneticilere mesaj gönderebilir." });
        }

        var sender = await _userManager.FindByIdAsync(senderId);

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var response = new MessageResponseDto
        {
            Id = message.Id,
            SenderId = senderId,
            SenderName = $"{sender!.FirstName} {sender.LastName}",
            ReceiverId = dto.ReceiverId,
            ReceiverName = $"{receiver.FirstName} {receiver.LastName}",
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = false
        };

        // SignalR ile alıcıya gerçek zamanlı bildirim gönder
        await _hubContext.Clients
            .Group($"user_{dto.ReceiverId}")
            .SendAsync("ReceiveMessage", response);

        return Ok(response);
    }

    /// <summary>İki kullanıcı arasındaki konuşmayı getir (oturum açan kullanıcı ile verilen kullanıcı)</summary>
    [HttpGet("conversation/{userId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetConversation(string userId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == adminId && m.ReceiverId == userId) ||
                (m.SenderId == userId && m.ReceiverId == adminId))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                ReceiverId = m.ReceiverId,
                ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}",
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            })
            .ToListAsync();

        // Okunmamış mesajları okundu olarak işaretle
        var unreadMessages = await _context.Messages
            .Where(m => m.ReceiverId == adminId && m.SenderId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
            msg.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok(messages);
    }

    /// <summary>Kullanıcının kendi mesajlarını görüntülemesi</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetMyMessages()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new MessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                ReceiverId = m.ReceiverId,
                ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}",
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            })
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>Admin: tüm mesajları listele</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetAllMessages()
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new MessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                ReceiverId = m.ReceiverId,
                ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}",
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            })
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>Birebir mesaj sil. Mesajı gönderen veya Admin silebilir.</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return NotFound(new { message = "Mesaj bulunamadı." });

        // Yalnızca mesajı gönderen ya da yönetici silebilir
        if (message.SenderId != userId && !User.IsInRole("Admin"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bu mesajı silme yetkiniz yok." });

        var receiverId = message.ReceiverId;
        var senderId = message.SenderId;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        // SignalR ile her iki tarafa silindi bildirimi
        await _hubContext.Clients.Group($"user_{receiverId}").SendAsync("MessageDeleted", id);
        await _hubContext.Clients.Group($"user_{senderId}").SendAsync("MessageDeleted", id);

        return NoContent();
    }
}
