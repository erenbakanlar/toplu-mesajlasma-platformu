using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MessagingPlatform.DTOs;
using MessagingPlatform.Models;

namespace MessagingPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Tüm üyeleri listele (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = user.CreatedAt
            });
        }

        return Ok(result);
    }

    /// <summary>Sadece normal üyeleri listele (Admin mesaj göndermek için)</summary>
    [HttpGet("members")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetMembers()
    {
        var users = await _userManager.GetUsersInRoleAsync("User");
        var result = users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = "User",
            CreatedAt = u.CreatedAt
        });
        return Ok(result);
    }

    /// <summary>Yöneticileri listele (üyelerin mesajlaşması için - tüm oturumlular erişebilir)</summary>
    [HttpGet("admins")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAdmins()
    {
        var users = await _userManager.GetUsersInRoleAsync("Admin");
        var result = users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = "Admin",
            CreatedAt = u.CreatedAt
        });
        return Ok(result);
    }

    /// <summary>Kullanıcı profili</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "User",
            CreatedAt = user.CreatedAt
        });
    }
}
