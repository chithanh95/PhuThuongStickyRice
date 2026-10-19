using System.Text;
using System.Text.Json;

namespace PhuThuongStickyRice.Domain.Infrastructure.MessageBrokers
{
    public class Message<T>
    {
        public MessageMetadata Metadata { get; set; }

        public T Data { get; set; }

        public string SerializeObject()
        {
            return JsonSerializer.Serialize(this);
        }

        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(SerializeObject());
        }
    }
}
