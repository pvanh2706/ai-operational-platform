using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Kenh2TrichChu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuTrichChu",
                schema: "kp",
                table: "document",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiDungChu",
                schema: "kp",
                table: "document",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiTrichChu",
                schema: "kp",
                table: "document",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChuTrichChu",
                schema: "kp",
                table: "document");

            migrationBuilder.DropColumn(
                name: "NoiDungChu",
                schema: "kp",
                table: "document");

            migrationBuilder.DropColumn(
                name: "TrangThaiTrichChu",
                schema: "kp",
                table: "document");
        }
    }
}
