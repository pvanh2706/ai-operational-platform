using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EvidenceSubTenantAndRetraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetractedAt",
                schema: "kp",
                table: "evidence_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetractedBy",
                schema: "kp",
                table: "evidence_item",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetractedReason",
                schema: "kp",
                table: "evidence_item",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubTenantCode",
                schema: "kp",
                table: "evidence_item",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_item_TenantId_SubTenantCode",
                schema: "kp",
                table: "evidence_item",
                columns: new[] { "TenantId", "SubTenantCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_evidence_item_TenantId_SubTenantCode",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "RetractedAt",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "RetractedBy",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "RetractedReason",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "SubTenantCode",
                schema: "kp",
                table: "evidence_item");
        }
    }
}
