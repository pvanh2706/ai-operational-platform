using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Ném khi chưa có credit để gọi model. Chỗ gọi bắt nó và đổi thành một câu trả lời đọc được.
/// </summary>
public sealed class ChuaCoCreditException(string message) : Exception(message);

/// <summary>
/// Bản <see cref="ISopDrafter"/> cắm khi CHƯA CÓ khoá API — tức mặc định hôm nay.
///
/// 🛑 **Vì sao là một bản hiện thực NÉM chứ không phải để trống DI.** Để trống thì host không
/// khởi động được, và cả sản phẩm chết vì một tính năng chưa dùng tới. Trả một bản nháp rỗng
/// thì tệ hơn nữa: nó đi thẳng vào kho ở trạng thái `Draft` và trông như một kết quả thật.
///
/// Bản này **nói ra chính xác việc phải làm thay**: vòng không-credit
/// (<c>--xuat-soan-nhap</c> → dán cho một ngữ cảnh AI → <c>POST /knowledge/sop-draft</c>),
/// đúng đường mà `IM-29` đã đi thật ngày 2026-09-10.
///
/// ⚠ Đổi sang bản gọi model thật là đặt `Anthropic:ApiKey` — KHÔNG sửa dòng code nào.
/// Đó là toàn bộ lý do `AR3` giữ interface này mỏng.
/// </summary>
public sealed class SopDrafterChuaCoCredit : ISopDrafter
{
    public Task<SopDraftResult> DraftAsync(SopDraftRequest request, CancellationToken ct = default) =>
        throw new ChuaCoCreditException(
            """
            Chưa cấu hình Anthropic:ApiKey nên không gọi model được.

            Vòng thay thế, KHÔNG tốn credit và đã chạy thật (IM-29):
              1. dotnet run --project src/KnowledgePlatform.Api -- \
                   --xuat-soan-nhap <mã-khách> "<tên nhóm>" <file-ra>
              2. Dán toàn bộ file cho một ngữ cảnh AI sạch, lưu JSON nó trả về.
              3. POST /knowledge/sop-draft với chính JSON đó.

            ⚠ Bước 3 đi qua đúng cổng kiểm mà đường này đi qua, nên không mất phép kiểm nào.
            """);
}
