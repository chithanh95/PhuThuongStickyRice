using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019200250_AddOrderSpinPrizeCode")]
    public partial class AddOrderSpinPrizeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // bug-hunt 2026-10-20: lưu mã voucher trúng từ vòng quay may mắn
            // lên đơn (Order.SpinPrizeCode) để khi đơn bị hủy thì thu hồi
            // voucher — chống farm: quay trên đơn Pending rồi hủy đơn vẫn
            // giữ voucher thật. Nullable: đơn chưa quay / quay trúng ô "Miss"
            // thì để null. Không cần index: chỉ đọc từ entity đã load, không
            // lọc theo cột này.
            migrationBuilder.AddColumn<string>(
                name: "SpinPrizeCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpinPrizeCode",
                table: "Orders");
        }
    }
}
