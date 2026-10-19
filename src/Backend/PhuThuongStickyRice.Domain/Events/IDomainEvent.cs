using System;
using System.Collections.Generic;
using System.Text;

namespace PhuThuongStickyRice.Domain.Events
{
    /// <summary>
    /// Marker interface cho mọi sự kiện domain: "đã có chuyện gì đó xảy ra"
    /// trong nghiệp vụ (đơn được tạo, điểm được cộng...). Handler đăng ký
    /// qua <see cref="IDomainEventHandler{T}"/>.
    /// </summary>
    public interface IDomainEvent
    {
    }
}
