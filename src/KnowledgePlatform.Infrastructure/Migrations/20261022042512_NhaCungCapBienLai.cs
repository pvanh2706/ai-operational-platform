using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NhaCungCapBienLai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Model",
                schema: "kp",
                table: "luot_soan_nhap",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhaCungCap",
                schema: "kp",
                table: "luot_soan_nhap",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                schema: "kp",
                table: "luot_dinh_tuyen_model",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhaCungCap",
                schema: "kp",
                table: "luot_dinh_tuyen_model",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Model",
                schema: "kp",
                table: "luot_soan_nhap");

            migrationBuilder.DropColumn(
                name: "NhaCungCap",
                schema: "kp",
                table: "luot_soan_nhap");

            migrationBuilder.DropColumn(
                name: "Model",
                schema: "kp",
                table: "luot_dinh_tuyen_model");

            migrationBuilder.DropColumn(
                name: "NhaCungCap",
                schema: "kp",
                table: "luot_dinh_tuyen_model");
        }
    }
}
