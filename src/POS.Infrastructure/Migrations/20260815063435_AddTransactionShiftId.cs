using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionShiftId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ShiftId",
                table: "Transactions",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Shifts_ShiftId",
                table: "Transactions",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Shifts_ShiftId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ShiftId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Transactions");
        }
    }
}
