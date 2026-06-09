using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MessagingPlatform.Hubs;

[Authorize]
public class ChatHub : Hub
{
    // Kullanıcı bağlandığında kendi özel grubuna katılır (userId bazlı)
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnConnectedAsync();
    }

    // Mesaj grubu odasına katıl
    public async Task JoinGroup(int groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    // Mesaj grubu odasından ayrıl
    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }
}
