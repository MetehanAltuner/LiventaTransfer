using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LiventaTransfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiplePassengersPerStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobStops_Passengers_PassengerId",
                table: "JobStops");

            migrationBuilder.DropIndex(
                name: "IX_JobStops_PassengerId",
                table: "JobStops");

            migrationBuilder.CreateTable(
                name: "JobStopPassengers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobStopId = table.Column<long>(type: "bigint", nullable: false),
                    PassengerId = table.Column<long>(type: "bigint", nullable: false),
                    InfoSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStopPassengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStopPassengers_JobStops_JobStopId",
                        column: x => x.JobStopId,
                        principalTable: "JobStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobStopPassengers_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobStopPassengers_JobStopId_PassengerId",
                table: "JobStopPassengers",
                columns: new[] { "JobStopId", "PassengerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobStopPassengers_PassengerId",
                table: "JobStopPassengers",
                column: "PassengerId");

            // Mevcut tek-yolcu verisini yeni ara tabloya taşı (PassengerId dolu olan duraklar).
            // Yolcu bazındaki InfoSentAt, durağın eski InfoSentAt değerinden devralınır.
            migrationBuilder.Sql(@"
                INSERT INTO ""JobStopPassengers"" (""JobStopId"", ""PassengerId"", ""InfoSentAt"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                SELECT ""Id"", ""PassengerId"", ""InfoSentAt"", ""CreatedAt"", ""UpdatedAt"", false
                FROM ""JobStops""
                WHERE ""PassengerId"" IS NOT NULL;");

            // Veri taşındıktan sonra eski kolonları kaldır.
            migrationBuilder.DropColumn(
                name: "InfoSentAt",
                table: "JobStops");

            migrationBuilder.DropColumn(
                name: "PassengerCount",
                table: "JobStops");

            migrationBuilder.DropColumn(
                name: "PassengerId",
                table: "JobStops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InfoSentAt",
                table: "JobStops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCount",
                table: "JobStops",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "PassengerId",
                table: "JobStops",
                type: "bigint",
                nullable: true);

            // Geri taşıma: her durak için (varsa) ilk yolcuyu eski tek-yolcu kolonlarına yaz.
            // Çoklu yolcu içeren duraklarda yalnızca ilk yolcu korunur.
            migrationBuilder.Sql(@"
                UPDATE ""JobStops"" js
                SET ""PassengerId"" = sub.""PassengerId"",
                    ""InfoSentAt"" = sub.""InfoSentAt""
                FROM (
                    SELECT DISTINCT ON (""JobStopId"") ""JobStopId"", ""PassengerId"", ""InfoSentAt""
                    FROM ""JobStopPassengers""
                    ORDER BY ""JobStopId"", ""Id""
                ) sub
                WHERE js.""Id"" = sub.""JobStopId"";");

            migrationBuilder.DropTable(
                name: "JobStopPassengers");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_PassengerId",
                table: "JobStops",
                column: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobStops_Passengers_PassengerId",
                table: "JobStops",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
