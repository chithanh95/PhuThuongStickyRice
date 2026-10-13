using PhuThuongStickyRice.Blazor.Modules.Users.Models;
using PhuThuongStickyRice.Blazor.Modules.Users.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Users.Components
{
    public partial class AddEdit
    {
        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public UserService UserService { get; set; }


        [Parameter]
        public UserModel User { get; set; } = new UserModel();

        protected async Task HandleValidSubmit()
        {
            var user = await (User.Id == Guid.Empty ? UserService.CreateUserAsync(User) : UserService.UpdateUserAsync(User.Id, User));
            NavManager.NavigateTo($"/users/{user.Id}");
        }
    }
}
