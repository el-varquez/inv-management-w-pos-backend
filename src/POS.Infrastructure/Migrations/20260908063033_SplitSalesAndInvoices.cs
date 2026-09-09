using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitSalesAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Sales");

            migrationBuilder.RenameTable(
                name: "TransactionItems",
                newName: "SaleItems");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "SaleItems",
                newName: "SaleId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ReceiptNumber",
                table: "Sales",
                newName: "IX_Sales_ReceiptNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_PaymentMethodId",
                table: "Sales",
                newName: "IX_Sales_PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ShiftId",
                table: "Sales",
                newName: "IX_Sales_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionItems_ItemId",
                table: "SaleItems",
                newName: "IX_SaleItems_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionItems_TransactionId",
                table: "SaleItems",
                newName: "IX_SaleItems_SaleId");

            migrationBuilder.Sql("""
                ALTER TABLE "Sales" RENAME CONSTRAINT "PK_Transactions" TO "PK_Sales";
                ALTER TABLE "Sales" RENAME CONSTRAINT "FK_Transactions_PaymentMethods_PaymentMethodId" TO "FK_Sales_PaymentMethods_PaymentMethodId";
                ALTER TABLE "Sales" RENAME CONSTRAINT "FK_Transactions_Shifts_ShiftId" TO "FK_Sales_Shifts_ShiftId";
                ALTER TABLE "SaleItems" RENAME CONSTRAINT "PK_TransactionItems" TO "PK_SaleItems";
                ALTER TABLE "SaleItems" RENAME CONSTRAINT "FK_TransactionItems_Items_ItemId" TO "FK_SaleItems_Items_ItemId";
                ALTER TABLE "SaleItems" RENAME CONSTRAINT "FK_TransactionItems_Transactions_TransactionId" TO "FK_SaleItems_Sales_SaleId";
                """);

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MarkupTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Sukis_SukiId",
                        column: x => x.SukiId,
                        principalTable: "Sukis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EditedFrom = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Sukis_SukiId",
                        column: x => x.SukiId,
                        principalTable: "Sukis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ItemId",
                table: "InvoiceItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ShiftId",
                table: "Invoices",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SukiId",
                table: "Invoices",
                column: "SukiId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SukiId",
                table: "Payments",
                column: "SukiId");

            migrationBuilder.Sql("""
                INSERT INTO "Invoices" ("Id", "InvoiceNumber", "SukiId", "ShiftId", "Subtotal", "DiscountAmount", "MarkupTotal", "Total", "IsVoided", "VoidedAt", "VoidedBy", "CreatedBy", "CreatedAt", "UpdatedAt")
                SELECT s."Id", s."ReceiptNumber", c."SukiId", s."ShiftId", s."Subtotal", s."DiscountAmount", c."Markup", s."Total", c."IsVoided", c."VoidedAt", c."VoidedBy", s."CreatedBy", s."CreatedAt", s."UpdatedAt"
                FROM "Sales" s
                JOIN "UtangCharges" c ON c."TransactionId" = s."Id"
                WHERE s."PaymentMethodId" = '00000000-0000-0000-0000-000000000003' AND s."RefundedFromId" IS NULL;

                INSERT INTO "InvoiceItems" ("Id", "InvoiceId", "ItemId", "ItemName", "UnitPrice", "CostPrice", "Quantity", "Discount", "Total", "CreatedAt", "UpdatedAt")
                SELECT si."Id", si."SaleId", si."ItemId", si."ItemName", si."UnitPrice", si."CostPrice", si."Quantity", si."Discount", si."Total", si."CreatedAt", si."UpdatedAt"
                FROM "SaleItems" si
                JOIN "Invoices" i ON i."Id" = si."SaleId";

                INSERT INTO "Payments" ("Id", "SukiId", "Amount", "Note", "EditedFrom", "IsVoided", "VoidedAt", "VoidedBy", "CreatedBy", "CreatedAt", "UpdatedAt")
                SELECT p."Id", p."SukiId", p."Amount", p."Note", p."EditedFrom", p."IsVoided", p."VoidedAt", p."VoidedBy", p."CreatedBy", p."CreatedAt", p."UpdatedAt"
                FROM "UtangPayments" p;
                """);

            migrationBuilder.DropTable(
                name: "UtangAdjustments");

            migrationBuilder.DropTable(
                name: "UtangCharges");

            migrationBuilder.DropTable(
                name: "UtangPayments");

            migrationBuilder.Sql("""
                DELETE FROM "SaleItems" WHERE "SaleId" IN (SELECT "Id" FROM "Sales" WHERE "PaymentMethodId" = '00000000-0000-0000-0000-000000000003');
                DELETE FROM "Sales" WHERE "PaymentMethodId" = '00000000-0000-0000-0000-000000000003';
                """);

            migrationBuilder.DropIndex(
                name: "IX_Transactions_SukiId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SukiId",
                table: "Sales");

            migrationBuilder.Sql("""
                UPDATE "PaymentMethods" SET "Name" = 'GCash'
                WHERE "Id" = '00000000-0000-0000-0000-000000000002'
                  AND NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Name" = 'GCash' AND "Id" <> '00000000-0000-0000-0000-000000000002');
                DELETE FROM "PaymentMethods" WHERE "Id" = '00000000-0000-0000-0000-000000000003';
                """);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PaymentMethods");

            migrationBuilder.Sql("""
                INSERT INTO "PaymentMethods" ("Id", "Name", "RequiresReference", "IsActive", "IsSystem", "CreatedAt", "UpdatedAt")
                SELECT '00000000-0000-0000-0000-000000000004', 'Maya', TRUE, TRUE, TRUE, NOW(), NULL
                WHERE NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Id" = '00000000-0000-0000-0000-000000000004' OR "Name" = 'Maya');
                """);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptUtang",
                table: "StoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UtangReminderDays",
                table: "StoreSettings",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.Sql("""
                UPDATE "StoreSettings" SET "AcceptUtang" = EXISTS (SELECT 1 FROM "Sukis");
                """);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropColumn(
                name: "AcceptUtang",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "UtangReminderDays",
                table: "StoreSettings");

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

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PaymentMethods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameTable(
                name: "Sales",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "SaleItems",
                newName: "TransactionItems");

            migrationBuilder.RenameColumn(
                name: "SaleId",
                table: "TransactionItems",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_ReceiptNumber",
                table: "Transactions",
                newName: "IX_Transactions_ReceiptNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_PaymentMethodId",
                table: "Transactions",
                newName: "IX_Transactions_PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_ShiftId",
                table: "Transactions",
                newName: "IX_Transactions_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_ItemId",
                table: "TransactionItems",
                newName: "IX_TransactionItems_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_SaleId",
                table: "TransactionItems",
                newName: "IX_TransactionItems_TransactionId");

            migrationBuilder.Sql("""
                ALTER TABLE "Transactions" RENAME CONSTRAINT "PK_Sales" TO "PK_Transactions";
                ALTER TABLE "Transactions" RENAME CONSTRAINT "FK_Sales_PaymentMethods_PaymentMethodId" TO "FK_Transactions_PaymentMethods_PaymentMethodId";
                ALTER TABLE "Transactions" RENAME CONSTRAINT "FK_Sales_Shifts_ShiftId" TO "FK_Transactions_Shifts_ShiftId";
                ALTER TABLE "TransactionItems" RENAME CONSTRAINT "PK_SaleItems" TO "PK_TransactionItems";
                ALTER TABLE "TransactionItems" RENAME CONSTRAINT "FK_SaleItems_Items_ItemId" TO "FK_TransactionItems_Items_ItemId";
                ALTER TABLE "TransactionItems" RENAME CONSTRAINT "FK_SaleItems_Sales_SaleId" TO "FK_TransactionItems_Transactions_TransactionId";
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "SukiId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SukiId",
                table: "Transactions",
                column: "SukiId");

            migrationBuilder.CreateTable(
                name: "UtangAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtangAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtangAdjustments_Sukis_SukiId",
                        column: x => x.SukiId,
                        principalTable: "Sukis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtangCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    Markup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtangCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtangCharges_Sukis_SukiId",
                        column: x => x.SukiId,
                        principalTable: "Sukis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtangCharges_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtangPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SukiId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    EditedFrom = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_UtangAdjustments_SukiId",
                table: "UtangAdjustments",
                column: "SukiId");

            migrationBuilder.CreateIndex(
                name: "IX_UtangCharges_SukiId",
                table: "UtangCharges",
                column: "SukiId");

            migrationBuilder.CreateIndex(
                name: "IX_UtangCharges_TransactionId",
                table: "UtangCharges",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_UtangPayments_SukiId",
                table: "UtangPayments",
                column: "SukiId");

            migrationBuilder.CreateIndex(
                name: "IX_UtangPayments_TransactionId",
                table: "UtangPayments",
                column: "TransactionId");
        }
    }
}
