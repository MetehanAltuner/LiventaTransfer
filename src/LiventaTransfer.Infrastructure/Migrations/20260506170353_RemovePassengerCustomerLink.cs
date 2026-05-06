using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePassengerCustomerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Customers_CustomerId",
                table: "Passengers");

            migrationBuilder.DropIndex(
                name: "IX_Passengers_CustomerId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Passengers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CustomerId",
                table: "Passengers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_CustomerId",
                table: "Passengers",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Customers_CustomerId",
                table: "Passengers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
