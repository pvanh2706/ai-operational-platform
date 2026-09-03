using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgePlatform.Infrastructure.Migrations
{
    /// <summary>
    /// Chống trùng cho evidence — cùng cơ chế và cùng lý do với canonical_case.
    ///
    /// `AR-f` mở đường nạp evidence, và đường đó nhận tín hiệu từ phần mềm của khách:
    /// webhook gửi lại, job đồng bộ chạy lại, bên gửi retry. Không có index này thì
    /// một comment Jira gửi mười lần thành mười dòng.
    ///
    /// Vì sao nó KHÔNG chỉ là chuyện gọn gàng: `S8` nói giá trị của bản nháp gom nằm ở
    /// PHÂN BỐ — *"bước kiểm room mapping: 14/20 case đã làm"*. Evidence trùng làm sai
    /// đúng con số đó, và sai theo hướng không ai nhìn ra: bản nháp vẫn đọc trôi chảy,
    /// chỉ có tỉ lệ là bịa. Đây là thất bại im lặng, đúng loại dự án này đang chặn.
    ///
    /// Có TenantId trong khoá: hai khách hàng được phép dùng cùng một SourceReference
    /// mà không đè lên nhau — giống hệt canonical_case.
    /// </summary>
    public partial class EvidenceSourceReferenceUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_evidence_item_TenantId_SourceReference",
                schema: "kp",
                table: "evidence_item",
                columns: new[] { "TenantId", "SourceReference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_evidence_item_TenantId_SourceReference",
                schema: "kp",
                table: "evidence_item");
        }
    }
}
