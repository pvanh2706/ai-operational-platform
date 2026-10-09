using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuyTrinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                schema: "kp",
                table: "evidence_item",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "process_definition",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Ten = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PhienBan = table.Column<int>(type: "integer", nullable: false),
                    Lifecycle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    VisibilityScope = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SopDraftGocId = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    visibility_before = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    visibility_after = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    approval_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    commercial_data_confirmed = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_definition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "process_run",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AbandonedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AbandonedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AbandonReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_run", x => x.Id);
                    table.ForeignKey(
                        name: "FK_process_run_canonical_case_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "kp",
                        principalTable: "canonical_case",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_run_process_definition_ProcessDefinitionId",
                        column: x => x.ProcessDefinitionId,
                        principalSchema: "kp",
                        principalTable: "process_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_step",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    Ma = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    NoiXem = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DauHieu = table.Column<List<string>>(type: "text[]", nullable: false),
                    ChuDe = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_step", x => x.Id);
                    table.ForeignKey(
                        name: "FK_process_step_process_definition_ProcessDefinitionId",
                        column: x => x.ProcessDefinitionId,
                        principalSchema: "kp",
                        principalTable: "process_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_item_XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item",
                column: "XacNhanProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_process_definition_TenantId_Nhom",
                schema: "kp",
                table: "process_definition",
                columns: new[] { "TenantId", "Nhom" },
                unique: true,
                filter: "\"Lifecycle\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_process_definition_TenantId_Nhom_PhienBan",
                schema: "kp",
                table: "process_definition",
                columns: new[] { "TenantId", "Nhom", "PhienBan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_definition_TenantId_SopDraftGocId",
                schema: "kp",
                table: "process_definition",
                columns: new[] { "TenantId", "SopDraftGocId" },
                unique: true,
                filter: "\"SopDraftGocId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_CaseId",
                schema: "kp",
                table: "process_run",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_ProcessDefinitionId",
                schema: "kp",
                table: "process_run",
                column: "ProcessDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_TenantId_CaseId",
                schema: "kp",
                table: "process_run",
                columns: new[] { "TenantId", "CaseId" },
                unique: true,
                filter: "\"AbandonedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_process_run_TenantId_CaseId_ProcessDefinitionId",
                schema: "kp",
                table: "process_run",
                columns: new[] { "TenantId", "CaseId", "ProcessDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_process_step_ProcessDefinitionId_Ma",
                schema: "kp",
                table: "process_step",
                columns: new[] { "ProcessDefinitionId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_step_ProcessDefinitionId_ThuTu",
                schema: "kp",
                table: "process_step",
                columns: new[] { "ProcessDefinitionId", "ThuTu" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_item_process_step_XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item",
                column: "XacNhanProcessStepId",
                principalSchema: "kp",
                principalTable: "process_step",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // =================================================================
            //  RLS cho ba bảng mới — TRONG CHÍNH migration tạo bảng (khuôn WebhookOutbox). Biểu thức phải
            //  khớp từng chữ với RlsGuard.PolicyExpression, nếu không app từ chối khởi động.
            // =================================================================
            foreach (var table in new[] { "process_definition", "process_step", "process_run" })
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE kp."{table}" ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE kp."{table}" FORCE ROW LEVEL SECURITY;

                    CREATE POLICY tenant_isolation ON kp."{table}"
                        USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                        WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evidence_item_process_step_XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropTable(
                name: "process_run",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "process_step",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "process_definition",
                schema: "kp");

            migrationBuilder.DropIndex(
                name: "IX_evidence_item_XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "Origin",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropColumn(
                name: "XacNhanProcessStepId",
                schema: "kp",
                table: "evidence_item");
        }
    }
}
