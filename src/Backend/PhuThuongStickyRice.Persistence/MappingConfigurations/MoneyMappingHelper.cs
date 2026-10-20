using PhuThuongStickyRice.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq.Expressions;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Helper dùng chung cho các <see cref="IEntityTypeConfiguration{T}"/>:
    /// cấu hình 1 cột tiền (<see cref="Money"/> complex type) — cột Amount
    /// precision 18,2 bắt buộc, cột Currency tối đa 3 ký tự bắt buộc.
    /// Gom các khối ComplexProperty giống hệt nhau ở Order/OrderLine/Product
    /// về 1 chỗ để không lặp code (trước đây mỗi file tự copy 1 bản).
    /// </summary>
    internal static class MoneyMappingHelper
    {
        /// <summary>
        /// Cấu hình 1 navigation <see cref="Money"/> thành 2 cột DB với tên
        /// cột cho trước. Mapping giữ nguyên như các bản copy trước đây.
        /// </summary>
        /// <typeparam name="TEntity">Entity chứa cột tiền.</typeparam>
        /// <param name="builder">EntityTypeBuilder của entity.</param>
        /// <param name="navigation">Expression trỏ tới property Money.</param>
        /// <param name="amountColumnName">Tên cột DB cho số tiền.</param>
        /// <param name="currencyColumnName">Tên cột DB cho mã tiền tệ.</param>
        public static void ConfigureMoneyColumn<TEntity>(
            EntityTypeBuilder<TEntity> builder,
            Expression<Func<TEntity, Money>> navigation,
            string amountColumnName,
            string currencyColumnName)
            where TEntity : class
        {
            builder.ComplexProperty(navigation, moneyBuilder =>
            {
                moneyBuilder.Property(x => x.Amount)
                    .HasColumnName(amountColumnName)
                    .HasPrecision(18, 2)
                    .IsRequired();
                moneyBuilder.Property(x => x.Currency)
                    .HasColumnName(currencyColumnName)
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
