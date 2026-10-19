using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Đánh dấu entity được theo dõi thay đổi: giữ dấu thời gian tạo/sửa
    /// (audit thủ công của project) và token <c>RowVersion</c> cho kiểm soát
    /// đồng thời lạc quan (optimistic concurrency) ở tầng EF Core.
    /// </summary>
    public interface ITrackable
    {
        /// <summary>
        /// Token đồng thời: EF Core tự tăng mỗi lần ghi, chống ghi đè
        /// khi 2 luồng cùng sửa 1 bản ghi.
        /// </summary>
        byte[] RowVersion { get; set; }

        /// <summary>Thời điểm bản ghi được tạo (UTC).</summary>
        DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>Thời điểm sửa lần cuối (UTC); null nếu chưa từng sửa.</summary>
        DateTimeOffset? UpdatedDateTime { get; set; }
    }
}
