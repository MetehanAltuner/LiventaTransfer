using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePassengerIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Passengers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Passengers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
