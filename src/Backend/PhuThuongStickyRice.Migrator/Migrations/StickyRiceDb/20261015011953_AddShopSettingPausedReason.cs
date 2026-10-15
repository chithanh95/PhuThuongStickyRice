using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class AddShopSettingPausedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: the ShopSettings.PausedReason column is already added by
            // the earlier SeedUpsellProducts migration (20261015011700).
            // Kept as an empty migration so the migration history stays
            // consistent; do NOT re-add the column here (would fail with
            // "column already exists" when the user runs the Migrator).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op (see Up). The column is owned by SeedUpsellProducts.
        }
    }
}
