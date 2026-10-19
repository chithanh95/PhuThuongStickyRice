using System;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Identity;

namespace PhuThuongStickyRice.Application.Products.EventHandlers
{
    /// <summary>
    /// Dựng <see cref="OutboxEvent"/> cho các sự kiện vòng đời món ăn
    /// (tạo/sửa/xóa). Gom bản copy-paste trước đây nằm trong 3 handler
    /// <see cref="ProductCreatedEventHandler"/>,
    /// <see cref="ProductUpdatedEventHandler"/>,
    /// <see cref="ProductDeletedEventHandler"/> — cùng khởi tạo 6 field
    /// giống hệt nhau, chỉ khác EventType.
    /// </summary>
    // [codequality-smell 2026-10-20] Gộp khối khởi tạo OutboxEvent trùng nhau ở 3 handler.
    internal static class ProductOutboxFactory
    {
        /// <summary>
        /// Dựng outbox event "chưa publish" cho món: worker nền đọc dần để
        /// đẩy sang hệ thống ngoài mà không chặn luồng ghi DB của món.
        /// </summary>
        /// <param name="eventType">Loại sự kiện (vd "PRODUCT_CREATED").</param>
        /// <param name="currentUser">Người thao tác hiện tại (làm TriggeredById).</param>
        /// <param name="entity">Món ăn (làm ObjectId + snapshot Message).</param>
        /// <param name="eventDateTime">Thời điểm sự kiện xảy ra.</param>
        internal static OutboxEvent Create(
            string eventType,
            ICurrentUser currentUser,
            Product entity,
            DateTime eventDateTime)
        {
            return new OutboxEvent
            {
                EventType = eventType,
                TriggeredById = currentUser.UserId,
                CreatedDateTime = eventDateTime,
                ObjectId = entity.Id.ToString(),
                Message = entity.AsJsonString(),
                Published = false,
            };
        }
    }
}
