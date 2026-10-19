using System;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Toast thông báo nhẹ (vd "đã thêm vào giỏ"). Component hiển thị đăng ký
    /// sự kiện OnShow để nhận và render toast; gọi <see cref="Show"/> để bắn toast.
    /// </summary>
    public class ToastService
    {
        /// <summary>Sự kiện bắn ra mỗi khi có toast mới cần hiển thị.</summary>
        public event Action<ToastMessage> OnShow;

        /// <summary>Bắn một toast thông báo.</summary>
        /// <param name="message">Nội dung hiển thị.</param>
        /// <param name="linkText">Chữ của link đính kèm (null = không có link).</param>
        /// <param name="linkUrl">URL của link đính kèm.</param>
        /// <param name="kind">Loại toast (mặc định Success — xác nhận xanh).</param>
        public void Show(string message, string linkText = null, string linkUrl = null, ToastKind kind = ToastKind.Success)
        {
            OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                LinkText = linkText,
                LinkUrl = linkUrl,
                Id = Guid.NewGuid(),
                Kind = kind
            });
        }
    }

    /// <summary>Loại toast — quyết định icon và ngữ nghĩa ARIA cho trình đọc màn hình.
    /// Success = xác nhận (check xanh, role="status"); Warning = cảnh báo/lỗi nghiệp vụ
    /// (icon tam giác + role="alert" để đọc ngay); Info = thông tin trung tính.</summary>
    public enum ToastKind
    {
        Success,
        Warning,
        Info
    }

    /// <summary>Một toast đơn lẻ: nội dung + link tùy chọn + loại hiển thị.</summary>
    public class ToastMessage
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public string LinkText { get; set; }

        public string LinkUrl { get; set; }

        /// <summary>Loại toast (mặc định Success để tương thích mọi caller cũ).</summary>
        public ToastKind Kind { get; set; } = ToastKind.Success;
    }
}
