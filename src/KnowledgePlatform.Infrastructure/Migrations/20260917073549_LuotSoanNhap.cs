using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LuotSoanNhap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "luot_soan_nhap",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nhom = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NguoiYeuCau = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SoCase = table.Column<int>(type: "integer", nullable: false),
                    SoChoDaChe = table.Column<int>(type: "integer", nullable: false),
                    ThanhCong = table.Column<bool>(type: "boolean", nullable: false),
                    Loi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Luc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_luot_soan_nhap", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_luot_soan_nhap_TenantId_Luc",
                schema: "kp",
                table: "luot_soan_nhap",
                columns: new[] { "TenantId", "Luc" });

            // RLS — `G7`, `AR2`. Biểu thức khớp TỪNG CHỮ với RlsGuard.PolicyExpression.
            migrationBuilder.Sql("""
                ALTER TABLE kp."luot_soan_nhap" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE kp."luot_soan_nhap" FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON kp."luot_soan_nhap"
                    USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                    WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "luot_soan_nhap",
                schema: "kp");
        }
    }
}
