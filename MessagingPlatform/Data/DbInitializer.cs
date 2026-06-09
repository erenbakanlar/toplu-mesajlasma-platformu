using Microsoft.AspNetCore.Identity;
using MessagingPlatform.Models;

namespace MessagingPlatform.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Rolleri oluştur
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Admin kullanıcısı oluştur
        const string adminEmail = "admin@messaging.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Örnek kullanıcılar oluştur
        var sampleUsers = new[]
        {
            new { Email = "ali@example.com", First = "Ali", Last = "Yılmaz" },
            new { Email = "ayse@example.com", First = "Ayşe", Last = "Kaya" },
            new { Email = "mehmet@example.com", First = "Mehmet", Last = "Demir" }
        };

        foreach (var u in sampleUsers)
        {
            if (await userManager.FindByEmailAsync(u.Email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    FirstName = u.First,
                    LastName = u.Last,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, "User123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, "User");
            }
        }
    }
}
