using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EvidenceCheBenThuBa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `N1-2`: dấu "mẩu này đã qua cổng che bên thứ ba lúc nạp chưa". CỐ Ý không điền bù:
            // NULL = nạp trước khi có ô này, KHÁC `true`. Điền bù `true` cho dòng cũ là để ô này
            // khẳng định thay cho dữ liệu nó chưa từng thấy — cùng bài học `subTenantCode` (`AR-l`).
            migrationBuilder.AddColumn<bool>(
                name: "DaCheBenThuBa",
                schema: "kp",
                table: "evidence_item",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaCheBenThuBa",
                schema: "kp",
                table: "evidence_item");
        }
    }
}
