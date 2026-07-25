using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobContractor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContractorId",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ContractorId",
                table: "Jobs",
                column: "ContractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Contractors_ContractorId",
                table: "Jobs",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Contractors_ContractorId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_ContractorId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "Jobs");
        }
    }
}
