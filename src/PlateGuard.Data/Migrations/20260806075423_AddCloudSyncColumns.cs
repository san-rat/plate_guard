using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSyncColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vehicles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "Vehicles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "PromotionUsages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PromotionUsages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "PromotionUsages",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "PromotionUsages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Promotions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Promotions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "Promotions",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Promotions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Vehicles SET SyncId = lower(
                  substr(hex(randomblob(4)),1,8) || '-' ||
                  substr(hex(randomblob(2)),1,4) || '-4' ||
                  substr(hex(randomblob(2)),2,3) || '-' ||
                  substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2,3) || '-' ||
                  substr(hex(randomblob(6)),1,12)
                ) WHERE SyncId IS NULL OR SyncId = '';
                """);

            migrationBuilder.Sql("""
                UPDATE Promotions SET SyncId = lower(
                  substr(hex(randomblob(4)),1,8) || '-' ||
                  substr(hex(randomblob(2)),1,4) || '-4' ||
                  substr(hex(randomblob(2)),2,3) || '-' ||
                  substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2,3) || '-' ||
                  substr(hex(randomblob(6)),1,12)
                ) WHERE SyncId IS NULL OR SyncId = '';
                """);

            migrationBuilder.Sql("""
                UPDATE PromotionUsages SET SyncId = lower(
                  substr(hex(randomblob(4)),1,8) || '-' ||
                  substr(hex(randomblob(2)),1,4) || '-4' ||
                  substr(hex(randomblob(2)),2,3) || '-' ||
                  substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2,3) || '-' ||
                  substr(hex(randomblob(6)),1,12)
                ) WHERE SyncId IS NULL OR SyncId = '';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SyncId",
                table: "Vehicles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SyncId",
                table: "Promotions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SyncId",
                table: "PromotionUsages",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_VehicleId_PromotionId",
                table: "PromotionUsages");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_SyncId",
                table: "Vehicles",
                column: "SyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_SyncId",
                table: "PromotionUsages",
                column: "SyncId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_VehicleId_PromotionId",
                table: "PromotionUsages",
                columns: new[] { "VehicleId", "PromotionId" },
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_SyncId",
                table: "Promotions",
                column: "SyncId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_SyncId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_SyncId",
                table: "PromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_VehicleId_PromotionId",
                table: "PromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_SyncId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Promotions");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_VehicleId_PromotionId",
                table: "PromotionUsages",
                columns: new[] { "VehicleId", "PromotionId" },
                unique: true);
        }
    }
}
