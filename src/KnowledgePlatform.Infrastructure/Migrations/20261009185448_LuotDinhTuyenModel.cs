using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LuotDinhTuyenModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "luot_dinh_tuyen_model",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KhoaNguon = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HashDauVao = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SoChoDaChe = table.Column<int>(type: "integer", nullable: false),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Nguon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GiaiThich = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DoTinCay = table.Column<double>(type: "double precision", nullable: true),
                    ThanhCong = table.Column<bool>(type: "boolean", nullable: false),
                    Loi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Luc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_dinh_tuyen_model", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_luot_dinh_tuyen_model_TenantId_HashDauVao",
                schema: "kp",
                table: "luot_dinh_tuyen_model",
                columns: new[] { "TenantId", "HashDauVao" });

            migrationBuilder.CreateIndex(
                name: "IX_luot_dinh_tuyen_model_TenantId_KhoaNguon",
                schema: "kp",
                table: "luot_dinh_tuyen_model",
                columns: new[] { "TenantId", "KhoaNguon" });

            migrationBuilder.CreateIndex(
                name: "IX_luot_dinh_tuyen_model_TenantId_Luc",
                schema: "kp",
                table: "luot_dinh_tuyen_model",
                columns: new[] { "TenantId", "Luc" });

            // RLS TRONG CHÍNH migration tạo bảng (khuôn WebhookOutbox). Biểu thức khớp từng chữ với RlsGuard.
            migrationBuilder.Sql("""
                ALTER TABLE kp."luot_dinh_tuyen_model" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."luot_dinh_tuyen_model" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."luot_dinh_tuyen_model"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "luot_dinh_tuyen_model",
                schema: "kp");
        }
    }
}
