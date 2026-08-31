using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropEWalletTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EWalletTransactions");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "EWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    DrawerDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    Principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    WalletDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EWalletTransactions_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EWalletTransactions_Transactions_FeeTransactionId",
                        column: x => x.FeeTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EWalletTransactions_FeeTransactionId",
                table: "EWalletTransactions",
                column: "FeeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_EWalletTransactions_ShiftId",
                table: "EWalletTransactions",
                column: "ShiftId");
        }
    }
}
