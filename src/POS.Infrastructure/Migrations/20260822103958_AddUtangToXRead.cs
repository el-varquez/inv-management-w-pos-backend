using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUtangToXRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_UtangCharged",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Snapshot_UtangChargedCount",
                table: "Shifts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_UtangCollections",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_UtangMarkup",
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
                name: "Snapshot_UtangCharged",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_UtangChargedCount",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_UtangCollections",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_UtangMarkup",
                table: "Shifts");
        }
    }
}
