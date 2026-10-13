using PhuThuongStickyRice.Blazor.Modules.Files.Components;
using PhuThuongStickyRice.Blazor.Modules.Files.Models;
using PhuThuongStickyRice.Blazor.Modules.Files.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Files.Pages
{
    public partial class Edit
    {
        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public FileService FileService { get; set; }

        [Parameter]
        public Guid Id { get; set; }

        public FileEntryModel File { get; set; } = new FileEntryModel();

        protected AuditLogsDialog AuditLogsDialog { get; set; }

        protected override async Task OnInitializedAsync()
        {
            File = await FileService.GetFileByIdAsync(Id);
        }

        protected async Task ViewAuditLogs()
        {
            var logs = await FileService.GetAuditLogsAsync(File.Id);
            AuditLogsDialog.Show(logs);
        }

        protected async Task HandleValidSubmit()
        {
            await FileService.UpdateFileAsync(File.Id, File);
            NavManager.NavigateTo("/files");
        }
    }
}
