using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LuotGoiY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LuotGoiYId",
                schema: "kp",
                table: "knowledge_link_token",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "luot_goi_y",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KenhRa = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    KhoaNguonCase = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NguonDinhTuyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BietTraLoi = table.Column<bool>(type: "boolean", nullable: false),
                    LyDoKhongBiet = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProcessDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaBuoc = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    KnowledgeRecordIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    Luc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_goi_y", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "luot_goi_y_su_kien",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LuotGoiYId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nac = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DatDuoc = table.Column<bool>(type: "boolean", nullable: false),
                    Luc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiKhai = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    NhomDung = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_goi_y_su_kien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_luot_goi_y_su_kien_luot_goi_y_LuotGoiYId",
                        column: x => x.LuotGoiYId,
                        principalSchema: "kp",
                        principalTable: "luot_goi_y",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_luot_goi_y_TenantId_CaseId",
                schema: "kp",
                table: "luot_goi_y",
                columns: new[] { "TenantId", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_luot_goi_y_TenantId_Luc",
                schema: "kp",
                table: "luot_goi_y",
                columns: new[] { "TenantId", "Luc" });

            migrationBuilder.CreateIndex(
                name: "IX_luot_goi_y_su_kien_LuotGoiYId",
                schema: "kp",
                table: "luot_goi_y_su_kien",
                column: "LuotGoiYId");

            migrationBuilder.CreateIndex(
                name: "IX_luot_goi_y_su_kien_TenantId_Nac",
                schema: "kp",
                table: "luot_goi_y_su_kien",
                columns: new[] { "TenantId", "Nac" });

            // =================================================================
            //  RLS cho hai bảng mới — TRONG CHÍNH migration tạo bảng (khuôn WebhookOutbox). Biểu thức phải
            //  khớp từng chữ với RlsGuard.PolicyExpression, nếu không app từ chối khởi động.
            //
            //  ⚠ Cột mới của knowledge_link_token KHÔNG cần policy: bảng đó MIỄN RLS có ký tên
            //    (TenantExemptRelations) vì chính token quyết định tenant. Cột thêm vào không đổi lý do đó.
            // =================================================================
            foreach (var table in new[] { "luot_goi_y", "luot_goi_y_su_kien" })
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
            migrationBuilder.DropTable(
                name: "luot_goi_y_su_kien",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "luot_goi_y",
                schema: "kp");

            migrationBuilder.DropColumn(
                name: "LuotGoiYId",
                schema: "kp",
                table: "knowledge_link_token");
        }
    }
}
