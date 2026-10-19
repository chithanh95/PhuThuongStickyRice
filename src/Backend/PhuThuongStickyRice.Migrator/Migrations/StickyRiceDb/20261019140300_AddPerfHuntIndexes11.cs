using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019140300_AddPerfHuntIndexes11")]
    public partial class AddPerfHuntIndexes11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 13:55):
            // DiscountAppService.GetUsableCodeAsync (public, voucher đơn đầu + mã SN:
            // OwnerPhone == phone AND Code LIKE 'PREFIX%') và GetPhonesWithUsableCodeAsync
            // (batch: OwnerPhone IN (...) AND Code LIKE 'PREFIX%') chỉ có unique index
            // trên Code nên phải range-scan toàn bộ mã cùng prefix; mã BDAY/SN sinh theo
            // từng khách nên tích lũy vô hạn. Composite (OwnerPhone, IsActive, Code):
            // seek equality trên OwnerPhone (+IsActive) rồi range trên Code.
            // Non-unique, lookup-only, không đổi nghiệp vụ.
            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_OwnerPhone_IsActive_Code",
                table: "DiscountCodes",
                columns: new[] { "OwnerPhone", "IsActive", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscountCodes_OwnerPhone_IsActive_Code",
                table: "DiscountCodes");
        }
    }
}
