using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveSalePriceToJobAndAddContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Yeni iş seviyesi satış fiyatı kolonu.
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Jobs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // 2) Mevcut durak fiyatlarını iş seviyesine kayıpsız taşı (iş başına topla).
            //    Bu, DropColumn'dan ÖNCE çalışmalıdır.
            migrationBuilder.Sql(
                """
                UPDATE "Jobs" j
                SET "SalePrice" = s.total
                FROM (
                    SELECT "JobId", SUM("SalePrice") AS total
                    FROM "JobStops"
                    WHERE "SalePrice" IS NOT NULL
                    GROUP BY "JobId"
                ) s
                WHERE j."Id" = s."JobId";
                """);

            // 3) Durak seviyesi fiyat kolonunu kaldır.
            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "JobStops");

            migrationBuilder.AddColumn<long>(
                name: "ContractorId",
                table: "Customers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ContractorId",
                table: "Customers",
                column: "ContractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Contractors_ContractorId",
                table: "Customers",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Contractors_ContractorId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "Contractors");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ContractorId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "Customers");

            // Durak seviyesi fiyat kolonunu geri ekle.
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "JobStops",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Best-effort: iş seviyesi fiyatı her işin ilk durağına (en küçük Sequence) geri yaz.
            // Toplama geri açılamayacağından bu, kısmi bir geri yüklemedir.
            migrationBuilder.Sql(
                """
                UPDATE "JobStops" st
                SET "SalePrice" = j."SalePrice"
                FROM "Jobs" j
                WHERE st."JobId" = j."Id"
                  AND j."SalePrice" IS NOT NULL
                  AND st."Sequence" = (
                      SELECT MIN(s2."Sequence") FROM "JobStops" s2 WHERE s2."JobId" = j."Id"
                  );
                """);

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Jobs");
        }
    }
}
