using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LuotHoiDap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "luot_hoi_dap",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoaiNguon = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaNguon = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NguoiHoi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SoNguon = table.Column<int>(type: "integer", nullable: false),
                    SoChoDaChe = table.Column<int>(type: "integer", nullable: false),
                    NhaCungCap = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ThanhCong = table.Column<bool>(type: "boolean", nullable: false),
                    Loi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Luc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_hoi_dap", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_luot_hoi_dap_TenantId_Luc",
                schema: "kp",
                table: "luot_hoi_dap",
                columns: new[] { "TenantId", "Luc" });

            // RLS TRONG CHÍNH migration tạo bảng (khuôn WebhookOutbox). Biểu thức khớp từng chữ với RlsGuard.
            migrationBuilder.Sql("""
                ALTER TABLE kp."luot_hoi_dap" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."luot_hoi_dap" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."luot_hoi_dap"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "luot_hoi_dap",
                schema: "kp");
        }
    }
}
