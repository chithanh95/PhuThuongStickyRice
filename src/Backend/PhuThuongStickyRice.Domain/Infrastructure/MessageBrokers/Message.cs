using System.Text;
using System.Text.Json;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    /// <summary>
    /// Thông điệp gửi qua message broker: gói payload <c>Data</c> kèm
    /// <see cref="MessageMetadata"/> (tracing/correlation) để consumer
    /// xử lý bất đồng bộ.
    /// </summary>
    public class Message<T>
    {
        public MessageMetadata Metadata { get; set; }

        public T Data { get; set; }

        /// <summary>Serialize cả message (metadata + data) thành JSON để gửi qua broker.</summary>
        public string SerializeObject()
        {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>Encode JSON của message thành byte UTF-8 để publish lên broker.</summary>
        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(SerializeObject());
        }
    }
}
