using MessagingPlatform.Models;

namespace MessagingPlatform.Services;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
