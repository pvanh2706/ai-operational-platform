using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WebhookOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_delivery",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseSourceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_delivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_endpoint",
                schema: "kp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SigningSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_endpoint", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_TenantId_State_NextAttemptAt",
                schema: "kp",
                table: "webhook_delivery",
                columns: new[] { "TenantId", "State", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_endpoint_TenantId",
                schema: "kp",
                table: "webhook_endpoint",
                column: "TenantId",
                unique: true);

            // =================================================================
            //  ROW-LEVEL SECURITY cho hai bảng mới — `G7`, `AR2`
            //
            //  ⚠ Danh sách viết TAY và biểu thức chép nguyên, CỐ Ý: migration là ảnh chụp
            //    của một thời điểm. Suy từ model thì chạy lại lịch sử migration sẽ cho kết
            //    quả khác nhau theo thời gian — xem ghi chú cùng ý ở migration
            //    HardenTenantPolicyAgainstEmptySetting.
            //
            //  ⚠ Biểu thức phải KHỚP TỪNG CHỮ với RlsGuard.PolicyExpression, nếu không app
            //    từ chối khởi động. Trùng lặp là có chủ ý (`IM-22`): suy policy từ migration
            //    thì một migration sai sẽ tự hợp lệ hoá chính nó.
            // =================================================================

            foreach (var table in new[] { "webhook_endpoint", "webhook_delivery" })
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
                name: "webhook_delivery",
                schema: "kp");

            migrationBuilder.DropTable(
                name: "webhook_endpoint",
                schema: "kp");
        }
    }
}
