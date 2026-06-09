using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MessagingPlatform.Data;
using MessagingPlatform.DTOs.Groups;
using MessagingPlatform.Hubs;
using MessagingPlatform.Models;

namespace MessagingPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<ChatHub> _hubContext;

    public GroupsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    /// <summary>Yeni mesaj grubu oluştur (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var admin = await _userManager.FindByIdAsync(adminId);

        var group = new MessageGroup
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedById = adminId,
            CreatedAt = DateTime.UtcNow
        };

        _context.MessageGroups.Add(group);
        await _context.SaveChangesAsync();

        // Üyeleri ekle
        foreach (var userId in dto.MemberIds.Distinct())
        {
            var userExists = await _userManager.FindByIdAsync(userId) != null;
            if (userExists)
            {
                _context.MessageGroupMembers.Add(new MessageGroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGroup), new { id = group.Id },
            await BuildGroupResponse(group.Id));
    }

    /// <summary>Tüm grupları listele (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<GroupResponseDto>>> GetAllGroups()
    {
        var groups = await _context.MessageGroups
            .Include(g => g.CreatedBy)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .ToListAsync();

        var result = groups.Select(g => new GroupResponseDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            CreatedAt = g.CreatedAt,
            CreatedById = g.CreatedById,
            CreatedByName = $"{g.CreatedBy.FirstName} {g.CreatedBy.LastName}",
            MemberCount = g.Members.Count,
            Members = g.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email!,
                JoinedAt = m.JoinedAt
            }).ToList()
        });

        return Ok(result);
    }

    /// <summary>Grup detayı</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponseDto>> GetGroup(int id)
    {
        var response = await BuildGroupResponse(id);
        if (response == null)
            return NotFound();
        return Ok(response);
    }

    /// <summary>Gruba üye ekle (Admin)</summary>
    [HttpPost("{id}/members")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMembers(int id, [FromBody] AddMembersDto dto)
    {
        var group = await _context.MessageGroups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = "Grup bulunamadı." });

        var existingMemberIds = await _context.MessageGroupMembers
            .Where(m => m.GroupId == id)
            .Select(m => m.UserId)
            .ToListAsync();

        foreach (var userId in dto.UserIds.Distinct())
        {
            if (existingMemberIds.Contains(userId)) continue;

            var userExists = await _userManager.FindByIdAsync(userId) != null;
            if (userExists)
            {
                _context.MessageGroupMembers.Add(new MessageGroupMember
                {
                    GroupId = id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Üyeler eklendi." });
    }

    /// <summary>Gruptan üye çıkar (Admin)</summary>
    [HttpDelete("{id}/members/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        var member = await _context.MessageGroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

        if (member == null)
            return NotFound(new { message = "Üye grupta bulunamadı." });

        _context.MessageGroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Üye gruptan çıkarıldı." });
    }

    /// <summary>Gruba toplu mesaj gönder (Admin)</summary>
    [HttpPost("{id}/messages")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GroupMessageResponseDto>> SendGroupMessage(
        int id, [FromBody] SendGroupMessageDto dto)
    {
        var group = await _context.MessageGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return NotFound(new { message = "Grup bulunamadı." });

        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var sender = await _userManager.FindByIdAsync(senderId);

        var groupMessage = new GroupMessage
        {
            GroupId = id,
            SenderId = senderId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow
        };

        _context.GroupMessages.Add(groupMessage);
        await _context.SaveChangesAsync();

        var response = new GroupMessageResponseDto
        {
            Id = groupMessage.Id,
            GroupId = id,
            GroupName = group.Name,
            SenderId = senderId,
            SenderName = $"{sender!.FirstName} {sender.LastName}",
            Content = dto.Content,
            SentAt = groupMessage.SentAt
        };

        // SignalR ile grup üyelerine gerçek zamanlı bildirim
        await _hubContext.Clients
            .Group($"group_{id}")
            .SendAsync("ReceiveGroupMessage", response);

        // Ayrıca her üyenin kişisel kanalına da bildirim gönder
        foreach (var member in group.Members)
        {
            await _hubContext.Clients
                .Group($"user_{member.UserId}")
                .SendAsync("ReceiveGroupMessage", response);
        }

        return Ok(response);
    }

    /// <summary>Grup mesajlarını getir</summary>
    [HttpGet("{id}/messages")]
    public async Task<ActionResult<IEnumerable<GroupMessageResponseDto>>> GetGroupMessages(int id)
    {
        var group = await _context.MessageGroups.FindAsync(id);
        if (group == null)
            return NotFound();

        var messages = await _context.GroupMessages
            .Include(gm => gm.Sender)
            .Include(gm => gm.Group)
            .Where(gm => gm.GroupId == id)
            .OrderBy(gm => gm.SentAt)
            .Select(gm => new GroupMessageResponseDto
            {
                Id = gm.Id,
                GroupId = gm.GroupId,
                GroupName = gm.Group.Name,
                SenderId = gm.SenderId,
                SenderName = $"{gm.Sender.FirstName} {gm.Sender.LastName}",
                Content = gm.Content,
                SentAt = gm.SentAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>Grup mesajı sil. Mesajı gönderen veya Admin silebilir.</summary>
    [HttpDelete("{groupId}/messages/{messageId}")]
    [Authorize]
    public async Task<IActionResult> DeleteGroupMessage(int groupId, int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

        if (message == null)
            return NotFound(new { message = "Mesaj bulunamadı." });

        if (message.SenderId != userId && !User.IsInRole("Admin"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bu mesajı silme yetkiniz yok." });

        _context.GroupMessages.Remove(message);
        await _context.SaveChangesAsync();

        // SignalR ile grup odasına ve üyelere silindi bildirimi
        await _hubContext.Clients.Group($"group_{groupId}").SendAsync("GroupMessageDeleted", messageId);

        var members = await _context.MessageGroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();
        foreach (var memberId in members)
            await _hubContext.Clients.Group($"user_{memberId}").SendAsync("GroupMessageDeleted", messageId);

        return NoContent();
    }

    /// <summary>Grup sil (Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _context.MessageGroups.FindAsync(id);
        if (group == null)
            return NotFound();

        _context.MessageGroups.Remove(group);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<GroupResponseDto?> BuildGroupResponse(int groupId)
    {
        var group = await _context.MessageGroups
            .Include(g => g.CreatedBy)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return null;

        return new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedAt,
            CreatedById = group.CreatedById,
            CreatedByName = $"{group.CreatedBy.FirstName} {group.CreatedBy.LastName}",
            MemberCount = group.Members.Count,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email!,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }
}
