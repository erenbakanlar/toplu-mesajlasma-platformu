using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MessagingPlatform.DTOs.Auth;
using MessagingPlatform.Models;
using MessagingPlatform.Services;

namespace MessagingPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    /// <summary>Kullanıcı girişi</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        var token = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = roles.FirstOrDefault() ?? "User",
            Expiration = DateTime.UtcNow.AddHours(8)
        });
    }

    /// <summary>Yeni kullanıcı kaydı</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null)
            return BadRequest(new { message = "Bu e-posta adresi zaten kullanımda." });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "User");

        var token = await _tokenService.GenerateTokenAsync(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = "User",
            Expiration = DateTime.UtcNow.AddHours(8)
        });
    }
}
