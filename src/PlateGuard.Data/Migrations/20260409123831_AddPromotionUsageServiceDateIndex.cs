using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateGuard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionUsageServiceDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_ServiceDate",
                table: "PromotionUsages",
                column: "ServiceDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_ServiceDate",
                table: "PromotionUsages");
        }
    }
}
