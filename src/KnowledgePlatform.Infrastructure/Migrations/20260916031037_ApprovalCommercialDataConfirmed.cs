using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalCommercialDataConfirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "commercial_data_confirmed",
                schema: "kp",
                table: "knowledge_record",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commercial_data_confirmed",
                schema: "kp",
                table: "knowledge_record");
        }
    }
}
