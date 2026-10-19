namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Entity có khóa chính kiểu <typeparamref name="T"/>. Với <c>TKey = Guid</c>,
    /// <c>Repository.AddAsync</c> tự sinh <c>Guid.NewGuid()</c> khi <c>Id</c>
    /// để trống — ngăn lỗi trùng khóa <c>00000000-...</c> trên các bảng audit/log.
    /// </summary>
    public interface IHasKey<T>
    {
        /// <summary>Khóa chính của entity.</summary>
        T Id { get; set; }
    }
}
