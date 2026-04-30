using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JobStopsAndMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Customers_CustomerId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Locations_DropoffLocationId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Locations_PickupLocationId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Passengers_PassengerId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_CustomerId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_DropoffLocationId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_PassengerId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DropoffAddress",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DropoffLocationId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FlightCode",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PassengerCount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PassengerId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "PickupLocationId",
                table: "Jobs",
                newName: "MergedIntoJobId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_PickupLocationId",
                table: "Jobs",
                newName: "IX_Jobs_MergedIntoJobId");

            migrationBuilder.CreateTable(
                name: "JobStops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    PassengerId = table.Column<long>(type: "bigint", nullable: true),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PickupLocationId = table.Column<long>(type: "bigint", nullable: true),
                    DropoffLocationId = table.Column<long>(type: "bigint", nullable: true),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FlightCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStops_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobStops_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobStops_Locations_DropoffLocationId",
                        column: x => x.DropoffLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobStops_Locations_PickupLocationId",
                        column: x => x.PickupLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobStops_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_CustomerId",
                table: "JobStops",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_DropoffLocationId",
                table: "JobStops",
                column: "DropoffLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_JobId_Sequence",
                table: "JobStops",
                columns: new[] { "JobId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_PassengerId",
                table: "JobStops",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_PickupLocationId",
                table: "JobStops",
                column: "PickupLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_MergedIntoJobId",
                table: "Jobs",
                column: "MergedIntoJobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_MergedIntoJobId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "JobStops");

            migrationBuilder.RenameColumn(
                name: "MergedIntoJobId",
                table: "Jobs",
                newName: "PickupLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_MergedIntoJobId",
                table: "Jobs",
                newName: "IX_Jobs_PickupLocationId");

            migrationBuilder.AddColumn<long>(
                name: "CustomerId",
                table: "Jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "DropoffAddress",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DropoffLocationId",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightCode",
                table: "Jobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCount",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "PassengerId",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Jobs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CustomerId",
                table: "Jobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DropoffLocationId",
                table: "Jobs",
                column: "DropoffLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PassengerId",
                table: "Jobs",
                column: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Customers_CustomerId",
                table: "Jobs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Locations_DropoffLocationId",
                table: "Jobs",
                column: "DropoffLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Locations_PickupLocationId",
                table: "Jobs",
                column: "PickupLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Passengers_PassengerId",
                table: "Jobs",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
