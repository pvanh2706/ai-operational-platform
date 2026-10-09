using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// BẢN NHÁP SOP GỐC, đúng như lúc nạp — để phép map có lỗi lần sau còn chạy lại được.
///
/// 🛑 Vì sao bảng này phải có (2026-10-10, P3): tới hôm nay host chỉ lưu record ĐÃ MAP, không
/// lưu bản nháp. Phép map cũ vứt danh sách <c>case</c> của mọi nhánh, nên khi sửa được phép map
/// thì 8 record cũ ở <c>kp_dev</c> **không điền bù dẫn chứng được** — vật liệu đã mất từ lúc nạp.
/// Lưu bản gốc là để lần sửa phép map sau KHÔNG phải nói câu đó thêm một lần nữa.
///
/// ⚠ Nội dung là dạng <b>serialize chuẩn</b> của <see cref="SopDraft"/>, KHÔNG phải chuỗi byte
/// bên gửi đã gửi: minimal API dựng object trước khi handler thấy được thân request. Hệ quả: trường
/// lạ bên gửi tự thêm (không có trong <see cref="SopDraft"/>) không được giữ.
///
/// ⚠ <see cref="Hash"/> là SHA-256 của chính chuỗi đó, unique theo tenant — nạp lại cùng một bản
/// nháp không sinh dòng thứ hai. Đây cũng là khoá mà P4 dùng để sinh quy trình DRAFT đúng một lần.
/// </summary>
public sealed class SopDraftGoc : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required string Nhom { get; init; }

    /// <summary>SHA-256 hex thường của <see cref="NoiDung"/>.</summary>
    public required string Hash { get; init; }

    /// <summary>JSON chuẩn của bản nháp.</summary>
    public required string NoiDung { get; init; }

    public DateTimeOffset NapLuc { get; init; } = DateTimeOffset.UtcNow;
}
