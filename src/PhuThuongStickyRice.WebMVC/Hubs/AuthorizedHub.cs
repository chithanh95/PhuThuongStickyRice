using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PhuThuongStickyRice.WebMVC.Hubs
{
    [Authorize]
    public class AuthorizedHub : Hub
    {
    }
}
