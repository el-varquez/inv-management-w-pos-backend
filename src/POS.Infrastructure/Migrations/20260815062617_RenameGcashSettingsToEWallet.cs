using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameGcashSettingsToEWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrackGcashWallet",
                table: "StoreSettings",
                newName: "TrackEWalletFloat");

            migrationBuilder.RenameColumn(
                name: "GcashFeeItemId",
                table: "StoreSettings",
                newName: "EWalletFeeItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrackEWalletFloat",
                table: "StoreSettings",
                newName: "TrackGcashWallet");

            migrationBuilder.RenameColumn(
                name: "EWalletFeeItemId",
                table: "StoreSettings",
                newName: "GcashFeeItemId");
        }
    }
}
