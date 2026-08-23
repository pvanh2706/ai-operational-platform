using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPathASchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kp");

            migrationBuilder.CreateTable(
                name: "canonical_case",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SourceCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourceResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_canonical_case", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_item",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ObservedInCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineReadability = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_item", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_record",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CauseName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Lifecycle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    approved_by_actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    visibility_before = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    visibility_after = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    approval_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    VisibilityScope = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_record", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assertion",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Verification = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReplacedByAssertionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assertion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assertion_knowledge_record_KnowledgeRecordId",
                        column: x => x.KnowledgeRecordId,
                        principalSchema: "kp",
                        principalTable: "knowledge_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assertion_evidence",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssertionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Relation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assertion_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assertion_evidence_assertion_AssertionId",
                        column: x => x.AssertionId,
                        principalSchema: "kp",
                        principalTable: "assertion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assertion_KnowledgeRecordId",
                schema: "kp",
                table: "assertion",
                column: "KnowledgeRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_assertion_TenantId_KnowledgeRecordId_ReplacedByAssertionId",
                schema: "kp",
                table: "assertion",
                columns: new[] { "TenantId", "KnowledgeRecordId", "ReplacedByAssertionId" });

            migrationBuilder.CreateIndex(
                name: "IX_assertion_evidence_AssertionId_EvidenceItemId",
                schema: "kp",
                table: "assertion_evidence",
                columns: new[] { "AssertionId", "EvidenceItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_canonical_case_TenantId_SourceReference",
                schema: "kp",
                table: "canonical_case",
                columns: new[] { "TenantId", "SourceReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_canonical_case_TenantId_SourceResolvedAt",
                schema: "kp",
                table: "canonical_case",
                columns: new[] { "TenantId", "SourceResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_item_TenantId_ObservedInCaseId",
                schema: "kp",
                table: "evidence_item",
                columns: new[] { "TenantId", "ObservedInCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_record_TenantId_Lifecycle",
                schema: "kp",
                table: "knowledge_record",
                columns: new[] { "TenantId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_ExternalKey",
                schema: "kp",
                table: "tenant",
                column: "ExternalKey",
                unique: true);

            // =================================================================
            //  ROW-LEVEL SECURITY — AR2 / G7
            //
            //  Đây là NGUỒN QUYỀN LỰC của ranh giới tenant, không phải global
            //  query filter của EF. Một câu WHERE thiếu điều kiện tenant, ở một
            //  truy vấn, một lần, sẽ làm tri thức của khách A hiện ra cho khách B
            //  — và KHÔNG có gì báo lỗi, nó chỉ trả về dữ liệu. RLS làm điều đó
            //  không thể xảy ra, kể cả khi lập trình viên quên.
            //
            //  Đặt trong CHÍNH migration đầu tiên, cố ý: không được có khoảnh
            //  khắc nào các bảng này tồn tại mà chưa được bảo vệ.
            //
            //  G13: policy đọc tenant từ biến session `app.current_tenant`, do
            //  lớp ứng dụng đặt theo NGỮ CẢNH REQUEST. Ở bản deploy riêng cho
            //  một khách, cùng policy này chạy y nguyên — database chỉ tình cờ
            //  chứa một tenant. Không có gì phải sửa.
            //
            //  `RlsGuard.VerifyAsync` đối chiếu danh sách bảng tenant-scoped suy
            //  ra TỪ MODEL với pg_policies lúc khởi động, nên thêm entity mới mà
            //  quên RLS sẽ ném lỗi lúc start, không rò rỉ lúc chạy.
            // =================================================================

            foreach (var table in new[]
            {
                "canonical_case",
                "evidence_item",
                "knowledge_record",
                "assertion",
                "assertion_evidence",
            })
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE kp."{table}" ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE kp."{table}" FORCE ROW LEVEL SECURITY;

                    CREATE POLICY tenant_isolation ON kp."{table}"
                        USING ("TenantId" = current_setting('app.current_tenant', true)::uuid)
                        WITH CHECK ("TenantId" = current_setting('app.current_tenant', true)::uuid);
                    """);
            }

            // FORCE ROW LEVEL SECURITY ở trên là cố ý: nếu không có nó, chủ sở hữu
            // bảng (thường là chính user mà app dùng để migrate) được MIỄN policy —
            // tức RLS bật mà không chặn gì. Đây là cái bẫy phổ biến nhất của RLS
            // trong PostgreSQL, và nó thất bại IM LẶNG.

            // `current_setting(..., true)` trả NULL khi biến chưa được đặt, và
            // `NULL = uuid` là NULL → policy từ chối. Nghĩa là: quên đặt tenant
            // thì KHÔNG THẤY GÌ, chứ không phải thấy HẾT. Mặc định đúng hướng an toàn.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assertion_evidence",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "canonical_case",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "evidence_item",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "assertion",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "knowledge_record",
                schema: "kp");
        }
    }
}
