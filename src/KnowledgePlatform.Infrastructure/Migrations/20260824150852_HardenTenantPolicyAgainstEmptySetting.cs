using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <summary>
    /// Không đổi schema. Chỉ sửa BIỂU THỨC của policy tenant_isolation.
    ///
    /// Vì sao có migration này: `IM-6` nói "quên đặt tenant thì KHÔNG THẤY GÌ".
    /// Chạy thật trên PostgreSQL 18 cho thấy điều đó chỉ đúng khi biến session
    /// CHƯA BAO GIỜ được đặt. Nếu nó đã từng được đặt rồi `RESET` — đúng việc mà
    /// connection pool làm — giá trị trở thành chuỗi RỖNG, không phải NULL, và
    /// `''::uuid` ném lỗi:
    ///
    ///     ERROR: invalid input syntax for type uuid: ""
    ///
    /// Không rò rỉ dữ liệu (vẫn nghiêng về hướng an toàn), nhưng thông báo lỗi
    /// đó KHÔNG nói gì về tenant. Người gặp nó sẽ đi tìm bug ép kiểu uuid, không
    /// đi tìm biến session. Đó là chi phí debug vô ích ở đúng chỗ nhạy cảm nhất.
    ///
    /// `nullif(..., '')` đưa chuỗi rỗng về NULL, và `TenantId = NULL` là NULL
    /// → policy từ chối → quay lại đúng hành vi mà `IM-6` mô tả: 0 dòng.
    ///
    /// Xem `07 §3 IM-9`.
    /// </summary>
    public partial class HardenTenantPolicyAgainstEmptySetting : Migration
    {
        // Danh sách này lặp lại migration đầu một cách CỐ Ý. Migration là ảnh
        // chụp của một thời điểm — nếu nó đọc danh sách động từ model thì việc
        // chạy lại lịch sử migration sẽ cho kết quả khác nhau theo thời gian.
        // Bảng mới thêm sau này sẽ có policy đúng ngay từ migration của nó.
        private static readonly string[] TenantScopedTables =
        [
            "canonical_case",
            "evidence_item",
            "knowledge_record",
            "assertion",
            "assertion_evidence",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                // DROP + CREATE, không ALTER: ALTER POLICY ... USING không thay
                // được WITH CHECK trong cùng câu lệnh một cách rõ ràng, và ta
                // muốn cả hai nhánh đọc luôn khớp nhau.
                migrationBuilder.Sql($"""
                    DROP POLICY tenant_isolation ON kp."{table}";

                    CREATE POLICY tenant_isolation ON kp."{table}"
                        USING ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid)
                        WITH CHECK ("TenantId" = nullif(current_setting('app.current_tenant', true), '')::uuid);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    DROP POLICY tenant_isolation ON kp."{table}";

                    CREATE POLICY tenant_isolation ON kp."{table}"
                        USING ("TenantId" = current_setting('app.current_tenant', true)::uuid)
                        WITH CHECK ("TenantId" = current_setting('app.current_tenant', true)::uuid);
                    """);
            }
        }
    }
}
