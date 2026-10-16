using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Pluggable SMS sender. Implementations: <see cref="LoggingSmsSender"/>
    /// (logs only — for dev/test), and future real providers (Viettel/VNPT/
    /// SpeedSMS) resolved by <see cref="SmsOptions.Provider"/>.
    /// </summary>
    public interface ISmsSender
    {
        Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
    }
}
