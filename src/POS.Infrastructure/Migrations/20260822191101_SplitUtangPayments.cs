using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitUtangPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtangPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EditedFrom = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtangPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtangPayments_Sukis_SukiId",
                        column: x => x.SukiId,
                        principalTable: "Sukis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtangPayments_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UtangPayments_SukiId",
                table: "UtangPayments",
                column: "SukiId");

            migrationBuilder.CreateIndex(
                name: "IX_UtangPayments_TransactionId",
                table: "UtangPayments",
                column: "TransactionId");

            migrationBuilder.Sql("""
                INSERT INTO "UtangPayments"
                    ("Id", "SukiId", "Amount", "TransactionId", "ShiftId", "IsVoided",
                     "VoidedAt", "VoidedBy", "EditedFrom", "CreatedBy", "CreatedAt", "UpdatedAt")
                SELECT "Id", "SukiId", "Amount", "TransactionId", "ShiftId", "IsVoided",
                       "VoidedAt", "VoidedBy", "EditedFrom", "CreatedBy", "CreatedAt", "UpdatedAt"
                FROM "UtangEntries"
                WHERE "Type" = 1;
                """);

            migrationBuilder.Sql("""DELETE FROM "UtangEntries" WHERE "Type" = 1;""");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UtangEntries");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "UtangEntries");

            migrationBuilder.DropColumn(
                name: "EditedFrom",
                table: "UtangEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                table: "UtangEntries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.RenameTable(
                name: "UtangEntries",
                newName: "UtangCharges");

            migrationBuilder.RenameIndex(
                name: "IX_UtangEntries_SukiId",
                table: "UtangCharges",
                newName: "IX_UtangCharges_SukiId");

            migrationBuilder.RenameIndex(
                name: "IX_UtangEntries_TransactionId",
                table: "UtangCharges",
                newName: "IX_UtangCharges_TransactionId");

            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "PK_UtangEntries" TO "PK_UtangCharges";""");

            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "FK_UtangEntries_Sukis_SukiId" TO "FK_UtangCharges_Sukis_SukiId";""");

            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "FK_UtangEntries_Transactions_TransactionId" TO "FK_UtangCharges_Transactions_TransactionId";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "FK_UtangCharges_Transactions_TransactionId" TO "FK_UtangEntries_Transactions_TransactionId";""");

            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "FK_UtangCharges_Sukis_SukiId" TO "FK_UtangEntries_Sukis_SukiId";""");

            migrationBuilder.Sql("""ALTER TABLE "UtangCharges" RENAME CONSTRAINT "PK_UtangCharges" TO "PK_UtangEntries";""");

            migrationBuilder.RenameIndex(
                name: "IX_UtangCharges_TransactionId",
                table: "UtangCharges",
                newName: "IX_UtangEntries_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_UtangCharges_SukiId",
                table: "UtangCharges",
                newName: "IX_UtangEntries_SukiId");

            migrationBuilder.RenameTable(
                name: "UtangCharges",
                newName: "UtangEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                table: "UtangEntries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UtangEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "UtangEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EditedFrom",
                table: "UtangEntries",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "UtangEntries"
                    ("Id", "SukiId", "Type", "Amount", "Markup", "TransactionId", "ShiftId", "Note",
                     "IsVoided", "VoidedAt", "VoidedBy", "EditedFrom", "CreatedBy", "CreatedAt", "UpdatedAt")
                SELECT "Id", "SukiId", 1, "Amount", 0, "TransactionId", "ShiftId",
                       CASE WHEN "TransactionId" IS NULL THEN 'Payment received' ELSE 'Down payment' END,
                       "IsVoided", "VoidedAt", "VoidedBy", "EditedFrom", "CreatedBy", "CreatedAt", "UpdatedAt"
                FROM "UtangPayments";
                """);

            migrationBuilder.DropTable(
                name: "UtangPayments");
        }
    }
}
