using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEWalletToXRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_EWalletCashIn",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Snapshot_EWalletCashInCount",
                table: "Shifts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_EWalletCashOut",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Snapshot_EWalletCashOutCount",
                table: "Shifts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletCashIn",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletCashInCount",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletCashOut",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletCashOutCount",
                table: "Shifts");
        }
    }
}
