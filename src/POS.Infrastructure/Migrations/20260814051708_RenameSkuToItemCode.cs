using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSkuToItemCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "Items",
                newName: "ItemCode");

            migrationBuilder.Sql("""
                UPDATE "Items" AS i
                SET "ItemCode" = CASE
                    WHEN (sub.rn + off.max_code) > 99999 THEN (sub.rn + off.max_code)::text
                    ELSE lpad((sub.rn + off.max_code)::text, 5, '0')
                END
                FROM (
                    SELECT "Id", row_number() OVER (ORDER BY "CreatedAt", "Id") AS rn
                    FROM "Items"
                    WHERE "ItemCode" IS NULL OR btrim("ItemCode") = ''
                ) AS sub,
                (
                    SELECT COALESCE(MAX("ItemCode"::int), 0) AS max_code
                    FROM "Items"
                    WHERE "ItemCode" ~ '^[0-9]{1,9}$'
                ) AS off
                WHERE i."Id" = sub."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "Items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                table: "Items",
                column: "ItemCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_ItemCode",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "Items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.RenameColumn(
                name: "ItemCode",
                table: "Items",
                newName: "Sku");
        }
    }
}
