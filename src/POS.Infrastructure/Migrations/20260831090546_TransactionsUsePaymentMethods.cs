using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TransactionsUsePaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentMethodId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Transactions" SET "PaymentMethodId" = CASE "PaymentType"
                    WHEN 0 THEN '00000000-0000-0000-0000-000000000001'::uuid
                    WHEN 1 THEN '00000000-0000-0000-0000-000000000002'::uuid
                    WHEN 2 THEN '00000000-0000-0000-0000-000000000002'::uuid
                    ELSE '00000000-0000-0000-0000-000000000003'::uuid
                END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentMethodId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentMethodId",
                table: "Transactions",
                column: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentMethods_PaymentMethodId",
                table: "Transactions",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentMethods_PaymentMethodId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentMethodId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Transactions");

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
