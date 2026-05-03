using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Jobs",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PublicId",
                table: "Jobs",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_PublicId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Jobs");
        }
    }
}
