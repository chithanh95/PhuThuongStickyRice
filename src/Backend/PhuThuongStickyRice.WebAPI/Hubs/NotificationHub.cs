using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PhuThuongStickyRice.WebAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
