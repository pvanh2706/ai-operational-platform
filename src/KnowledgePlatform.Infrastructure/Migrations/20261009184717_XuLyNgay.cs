using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class XuLyNgay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KhoaKhuTrung",
                schema: "kp",
                table: "webhook_delivery",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuKien",
                schema: "kp",
                table: "webhook_delivery",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_TenantId_KhoaKhuTrung",
                schema: "kp",
                table: "webhook_delivery",
                columns: new[] { "TenantId", "KhoaKhuTrung" },
                unique: true,
                filter: "\"KhoaKhuTrung\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_webhook_delivery_TenantId_KhoaKhuTrung",
                schema: "kp",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "KhoaKhuTrung",
                schema: "kp",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "SuKien",
                schema: "kp",
                table: "webhook_delivery");
        }
    }
}
