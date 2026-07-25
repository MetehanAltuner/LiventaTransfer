using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignContractorFieldsWithCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactPerson",
                table: "Contractors",
                newName: "TaxOffice");

            migrationBuilder.AddColumn<int>(
                name: "CustomerType",
                table: "Contractors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Contractors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "Contractors",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_TaxNumber",
                table: "Contractors",
                column: "TaxNumber",
                unique: true,
                filter: "\"TaxNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contractors_TaxNumber",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Contractors");

            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "Contractors");

            migrationBuilder.RenameColumn(
                name: "TaxOffice",
                table: "Contractors",
                newName: "ContactPerson");
        }
    }
}
