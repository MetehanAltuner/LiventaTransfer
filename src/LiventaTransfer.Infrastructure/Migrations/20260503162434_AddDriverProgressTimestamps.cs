using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverProgressTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DroppedOffAt",
                table: "JobStops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "JobStops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContactedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DroppedOffAt",
                table: "JobStops");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "JobStops");

            migrationBuilder.DropColumn(
                name: "ContactedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DepartedAt",
                table: "Jobs");
        }
    }
}
