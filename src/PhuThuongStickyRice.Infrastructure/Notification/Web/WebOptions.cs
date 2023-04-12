using PhuThuongStickyRice.Infrastructure.Notification.Web.SignalR;

namespace PhuThuongStickyRice.Infrastructure.Notification.Web
{
    public class WebOptions
    {
        public string Provider { get; set; }

        public SignalROptions SignalR { get; set; }

        public bool UsedFake()
        {
            return Provider == "Fake";
        }

        public bool UsedSignalR()
        {
            return Provider == "SignalR";
        }
    }
}
