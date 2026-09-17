using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DinhTuyenDaNap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dinh_tuyen_da_nap",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KhoaNguon = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Nguon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GiaiThich = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    NguoiNap = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NapLuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dinh_tuyen_da_nap", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dinh_tuyen_da_nap_TenantId_KhoaNguon",
                schema: "kp",
                table: "dinh_tuyen_da_nap",
                columns: new[] { "TenantId", "KhoaNguon" },
                unique: true);

            // RLS cho bảng mới — `G7`, `AR2`. Biểu thức chép nguyên và khớp TỪNG CHỮ với
            // RlsGuard.PolicyExpression; danh sách viết tay vì migration là ảnh chụp của một
            // thời điểm (xem HardenTenantPolicyAgainstEmptySetting).
            migrationBuilder.Sql("""
                ALTER TABLE kp."dinh_tuyen_da_nap" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."dinh_tuyen_da_nap" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."dinh_tuyen_da_nap"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dinh_tuyen_da_nap",
                schema: "kp");
        }
    }
}
