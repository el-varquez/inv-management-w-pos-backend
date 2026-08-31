using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodSalesRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DayMethodSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayMethodSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayMethodSales_BusinessDays_BusinessDayId",
                        column: x => x.BusinessDayId,
                        principalTable: "BusinessDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftMethodSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftMethodSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftMethodSales_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayMethodSales_BusinessDayId",
                table: "DayMethodSales",
                column: "BusinessDayId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftMethodSales_ShiftId",
                table: "ShiftMethodSales",
                column: "ShiftId");

            migrationBuilder.Sql("""
                INSERT INTO "ShiftMethodSales" ("Id", "ShiftId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), s."Id", '00000000-0000-0000-0000-000000000001'::uuid, 'Cash', s."Snapshot_CashSales", now()
                FROM "Shifts" s WHERE s."Snapshot_CashSales" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ShiftMethodSales" ("Id", "ShiftId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), s."Id", '00000000-0000-0000-0000-000000000002'::uuid, 'GCash', s."Snapshot_GcashSales", now()
                FROM "Shifts" s WHERE s."Snapshot_GcashSales" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ShiftMethodSales" ("Id", "ShiftId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), s."Id", '00000000-0000-0000-0000-000000000002'::uuid, 'Maya', s."Snapshot_MayaSales", now()
                FROM "Shifts" s WHERE s."Snapshot_MayaSales" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "DayMethodSales" ("Id", "BusinessDayId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), d."Id", '00000000-0000-0000-0000-000000000001'::uuid, 'Cash', d."Snapshot_CashSales", now()
                FROM "BusinessDays" d WHERE d."Snapshot_CashSales" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "DayMethodSales" ("Id", "BusinessDayId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), d."Id", '00000000-0000-0000-0000-000000000002'::uuid, 'GCash', d."Snapshot_GcashSales", now()
                FROM "BusinessDays" d WHERE d."Snapshot_GcashSales" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "DayMethodSales" ("Id", "BusinessDayId", "PaymentMethodId", "MethodName", "Amount", "CreatedAt")
                SELECT gen_random_uuid(), d."Id", '00000000-0000-0000-0000-000000000002'::uuid, 'Maya', d."Snapshot_MayaSales", now()
                FROM "BusinessDays" d WHERE d."Snapshot_MayaSales" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Snapshot_CashSales",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_GcashSales",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_MayaSales",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_CashSales",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "Snapshot_GcashSales",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "Snapshot_MayaSales",
                table: "BusinessDays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DayMethodSales");

            migrationBuilder.DropTable(
                name: "ShiftMethodSales");

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_CashSales",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_GcashSales",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_MayaSales",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_CashSales",
                table: "BusinessDays",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_GcashSales",
                table: "BusinessDays",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_MayaSales",
                table: "BusinessDays",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
