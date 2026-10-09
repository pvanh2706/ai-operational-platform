using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DanChungVaSopDraftGoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_evidence_item_TenantId_Id",
                schema: "kp",
                table: "evidence_item",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "sop_draft_goc",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    NapLuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sop_draft_goc", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assertion_evidence_TenantId_EvidenceItemId",
                schema: "kp",
                table: "assertion_evidence",
                columns: new[] { "TenantId", "EvidenceItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_sop_draft_goc_TenantId_Hash",
                schema: "kp",
                table: "sop_draft_goc",
                columns: new[] { "TenantId", "Hash" },
                unique: true);

            // 🛑 Khoá ngoại GHÉP cả TenantId — RLS `WITH CHECK` chỉ kiểm dòng đang ghi, không kiểm dòng
            //   nó trỏ tới. Xem chú thích ở AppDbContext (AssertionEvidence).
            migrationBuilder.AddForeignKey(
                name: "FK_assertion_evidence_evidence_item_TenantId_EvidenceItemId",
                schema: "kp",
                table: "assertion_evidence",
                columns: new[] { "TenantId", "EvidenceItemId" },
                principalSchema: "kp",
                principalTable: "evidence_item",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            // =================================================================
            //  RLS cho bảng mới — TRONG CHÍNH migration tạo bảng, không có khoảnh khắc nào bảng
            //  tồn tại mà chưa được bảo vệ. Biểu thức chép NGUYÊN từ WebhookOutbox; nó phải khớp
            //  từng chữ với RlsGuard.PolicyExpression, nếu không app từ chối khởi động.
            // =================================================================
            migrationBuilder.Sql("""
                ALTER TABLE kp."sop_draft_goc" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."sop_draft_goc" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."sop_draft_goc"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assertion_evidence_evidence_item_TenantId_EvidenceItemId",
                schema: "kp",
                table: "assertion_evidence");

            migrationBuilder.DropTable(
                name: "sop_draft_goc",
                schema: "kp");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_evidence_item_TenantId_Id",
                schema: "kp",
                table: "evidence_item");

            migrationBuilder.DropIndex(
                name: "IX_assertion_evidence_TenantId_EvidenceItemId",
                schema: "kp",
                table: "assertion_evidence");
        }
    }
}
