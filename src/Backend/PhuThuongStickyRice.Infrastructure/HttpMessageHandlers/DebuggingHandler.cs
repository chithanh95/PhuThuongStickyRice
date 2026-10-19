using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.HttpMessageHandlers
{
    public class DebuggingHandler : DelegatingHandler
    {
        public DebuggingHandler()
        {
        }

        // Không cần async/await ở đây (chỉ pass-through): tránh tạo async
        // state machine vô ích cho mỗi request đi qua handler.
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => base.SendAsync(request, cancellationToken);
    }
}
