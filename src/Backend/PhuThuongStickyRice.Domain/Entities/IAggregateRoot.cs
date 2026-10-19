namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Đánh dấu entity là gốc tổng hợp (aggregate root theo DDD): điểm vào
    /// duy nhất để repository và tầng application thao tác với cụm entity.
    /// Interface rỗng (marker) — nghiệp vụ cụ thể nằm ở từng entity.
    /// </summary>
    public interface IAggregateRoot
    {
    }
}
