using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundsToXRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Snapshot_RefundCount",
                table: "Shifts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_Refunds",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Snapshot_RefundCount",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_Refunds",
                table: "Shifts");
        }
    }
}
