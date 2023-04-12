using PhuThuongStickyRice.Blazor.Modules.Users.Models;
using PhuThuongStickyRice.Blazor.Modules.Users.Services;
using Microsoft.AspNetCore.Components;

namespace PhuThuongStickyRice.Blazor.Modules.Users.Pages
{
    public partial class Add
    {
        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public UserService UserService { get; set; }

        public UserModel User { get; set; } = new UserModel();
    }
}
