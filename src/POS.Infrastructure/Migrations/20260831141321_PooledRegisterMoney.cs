using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PooledRegisterMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EWalletFeeItemId",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "TrackEWalletFloat",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "Snapshot_CountedEWalletBalance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletVariance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_ExpectedEWalletBalance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "StartingEWalletBalance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Snapshot_CountedEWalletBalance",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "Snapshot_EWalletVariance",
                table: "BusinessDays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EWalletFeeItemId",
                table: "StoreSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackEWalletFloat",
                table: "StoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_CountedEWalletBalance",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_EWalletVariance",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_ExpectedEWalletBalance",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StartingEWalletBalance",
                table: "Shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_CountedEWalletBalance",
                table: "BusinessDays",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Snapshot_EWalletVariance",
                table: "BusinessDays",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
