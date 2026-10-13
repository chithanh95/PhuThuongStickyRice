using PhuThuongStickyRice.Blazor.Modules.Files.Models;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Blazor.Modules.Files.Components
{
    public partial class AuditLogsDialog
    {
        public bool ShowDialog { get; set; }

        public List<FileEntryAuditLogModel> AuditLogs { get; set; }

        public void Show(List<FileEntryAuditLogModel> auditLogs)
        {
            AuditLogs = auditLogs;
            ShowDialog = true;
            StateHasChanged();
        }

        public void Close()
        {
            ShowDialog = false;
            StateHasChanged();
        }
    }
}
