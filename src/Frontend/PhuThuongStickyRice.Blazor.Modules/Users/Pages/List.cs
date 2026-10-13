using PhuThuongStickyRice.Blazor.Modules.Core.Components;
using PhuThuongStickyRice.Blazor.Modules.Users.Models;
using PhuThuongStickyRice.Blazor.Modules.Users.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Users.Pages
{
    public partial class List
    {
        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public UserService UserService { get; set; }

        public List<UserModel> Users { get; set; } = new List<UserModel>();

        public UserModel DeletingUser { get; private set; }

        protected ConfirmDialog DeleteDialog { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Users = await UserService.GetUsersAsync();
        }

        protected void DeleteUser(UserModel user)
        {
            DeletingUser = user;
            DeleteDialog.Show();
        }

        public async void ConfirmedDeleteUser()
        {
            await UserService.DeleteUserAsync(DeletingUser.Id);
            DeleteDialog.Close();
            Users = await UserService.GetUsersAsync();
            StateHasChanged();
        }
    }
}
