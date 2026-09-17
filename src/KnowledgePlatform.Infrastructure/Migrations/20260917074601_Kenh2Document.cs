using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Kenh2Document : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenKhaiBao = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LoaiNoiDung = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    MaKho = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubTenantCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NguoiNap = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NapLuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_TenantId_Hash",
                schema: "kp",
                table: "document",
                columns: new[] { "TenantId", "Hash" },
                unique: true);

            // RLS — `G7`, `AR2`. Biểu thức khớp TỪNG CHỮ với RlsGuard.PolicyExpression.
            //
            // ⚠ RLS che phần SIÊU DỮ LIỆU. Phần NỘI DUNG nằm ở kho file và KHÔNG có RLS nào —
            //   tầng đó tự đứng vững bằng thư mục theo tenant + mã kho do sản phẩm sinh.
            //   Xem KhoFileDia.
            migrationBuilder.Sql("""
                ALTER TABLE kp."document" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."document" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."document"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document",
                schema: "kp");
        }
    }
}
