using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpenedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedLate = table.Column<bool>(type: "boolean", nullable: false),
                    Snapshot_NetSales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_TransactionCount = table.Column<int>(type: "integer", nullable: true),
                    Snapshot_CashSales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_GcashSales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_MayaSales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_DrawerMovementsNet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_CountedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_CashVariance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Snapshot_ShiftCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDays_Number",
                table: "BusinessDays",
                column: "Number",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "BusinessDays" ("Id", "Number", "Status", "OpenedAt", "OpenedBy", "ClosedAt", "ClosedBy", "ClosedLate", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), s."Number", s."Status", s."OpenedAt", s."OpenedBy", s."ClosedAt", s."ClosedBy", s."ClosedLate", s."CreatedAt", s."UpdatedAt"
                FROM "Shifts" s;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessDayId",
                table: "Shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Shifts" s SET "BusinessDayId" = d."Id"
                FROM "BusinessDays" d WHERE d."Number" = s."Number";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessDayId",
                table: "Shifts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_BusinessDayId",
                table: "Shifts",
                column: "BusinessDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_BusinessDays_BusinessDayId",
                table: "Shifts",
                column: "BusinessDayId",
                principalTable: "BusinessDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "ClosedLate",
                table: "Shifts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_BusinessDays_BusinessDayId",
                table: "Shifts");

            migrationBuilder.DropTable(
                name: "BusinessDays");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_BusinessDayId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "BusinessDayId",
                table: "Shifts");

            migrationBuilder.AddColumn<bool>(
                name: "ClosedLate",
                table: "Shifts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
