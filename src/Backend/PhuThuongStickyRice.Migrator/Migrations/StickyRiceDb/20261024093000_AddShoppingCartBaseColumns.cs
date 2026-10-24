using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Bổ sung 3 cột gốc (CreatedDateTime / UpdatedDateTime / RowVersion) cho
    /// bảng ShoppingCarts. Migration gốc 20261019070000_AddShoppingCart viết
    /// tay đã QUÊN 3 cột này (mọi entity đều kế thừa Entity&lt;TKey&gt; có 3
    /// thuộc tính trên, các bảng khác đều có đủ từ migration scaffold) —
    /// EF luôn SELECT/INSERT cả 3 cột nên mọi thao tác giỏ hàng server của
    /// khách đã đăng nhập đều nổ SqlException "Invalid column name
    /// 'CreatedDateTime' / 'RowVersion' / 'UpdatedDateTime'" (user gặp khi
    /// chọn món + bấm thanh toán, 2026-10-24). Raw SQL có guard
    /// COL_LENGTH để chạy lại an toàn trên DB đã vá tay.
    /// </summary>
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261024093000_AddShoppingCartBaseColumns")]
    public partial class AddShoppingCartBaseColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'CreatedDateTime') IS NULL " +
                "ALTER TABLE [ShoppingCarts] ADD [CreatedDateTime] datetimeoffset NOT NULL " +
                "CONSTRAINT [DF_ShoppingCarts_CreatedDateTime] DEFAULT SYSDATETIMEOFFSET();");

            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'UpdatedDateTime') IS NULL " +
                "ALTER TABLE [ShoppingCarts] ADD [UpdatedDateTime] datetimeoffset NULL;");

            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'RowVersion') IS NULL " +
                "ALTER TABLE [ShoppingCarts] ADD [RowVersion] rowversion NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'RowVersion') IS NOT NULL " +
                "ALTER TABLE [ShoppingCarts] DROP COLUMN [RowVersion];");

            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'UpdatedDateTime') IS NOT NULL " +
                "ALTER TABLE [ShoppingCarts] DROP COLUMN [UpdatedDateTime];");

            migrationBuilder.Sql(
                "IF COL_LENGTH('ShoppingCarts', 'CreatedDateTime') IS NOT NULL " +
                "BEGIN " +
                "ALTER TABLE [ShoppingCarts] DROP CONSTRAINT [DF_ShoppingCarts_CreatedDateTime]; " +
                "ALTER TABLE [ShoppingCarts] DROP COLUMN [CreatedDateTime]; " +
                "END");
        }
    }
}
