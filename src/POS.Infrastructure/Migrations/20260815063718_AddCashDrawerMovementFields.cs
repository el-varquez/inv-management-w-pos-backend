using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDrawerMovementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "CashDrawerMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "CashDrawerMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "CashDrawerMovements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "CashDrawerMovements",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "CashDrawerMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedBy",
                table: "CashDrawerMovements",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "CashDrawerMovements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CashDrawerMovements");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "CashDrawerMovements");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "CashDrawerMovements");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "CashDrawerMovements");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "CashDrawerMovements");
        }
    }
}
