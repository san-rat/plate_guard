using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncConflicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    LocalSyncId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CloudSyncId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VehicleNumberRaw = table.Column<string>(type: "TEXT", nullable: true),
                    PromotionName = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncConflicts");
        }
    }
}
