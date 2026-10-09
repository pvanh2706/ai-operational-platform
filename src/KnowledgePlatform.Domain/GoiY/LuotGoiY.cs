using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.GoiY;

/// <summary>
/// Năm nấc của một lượt gợi ý — từ vựng ĐÃ KHOÁ `L2` (`docs/04` §3B.2:706-716), không đặt tên mới.
///
/// ⚠ Ba nấc đầu HỆ THỐNG tự ghi; hai nấc cuối là LỜI KHAI qua API phản hồi (`N3-1`, đóng `L2-a`).
/// "Không nhận / không dùng" KHÔNG phải nấc thứ sáu — nó là nấc đó với <c>DatDuoc = false</c>.
/// </summary>
public enum NacGoiY
{
    /// <summary>Hệ thống đã TRA kho cho lượt này. <c>DatDuoc = false</c> nghĩa là cổng KHÔNG BIẾT đã chặn.</summary>
    Retrieved = 1,

    /// <summary>
    /// Hệ thống đã GIAO tới bên nhận (webhook trả 2xx, hoặc <c>/hoi</c> đã trả response).
    /// ⚠ KHÔNG có nghĩa là một NGƯỜI đã nhìn thấy — hệ thống không biết điều đó.
    /// </summary>
    Shown = 2,

    /// <summary>Có người mở link xem đầy đủ (<c>/xem/{token}</c>). Ghi mỗi lần mở.</summary>
    Opened = 3,

    /// <summary>LỜI KHAI: người nhận đồng ý với gợi ý.</summary>
    Accepted = 4,

    /// <summary>LỜI KHAI: gợi ý được dùng để xử lý case. Không gì nối nó với kết luận B5 của case.</summary>
    Used = 5,
}

/// <summary>
/// Một lượt gợi ý — ô `J` của sơ đồ: *"ghi lại đã gợi ý gì, người có dùng không"*. Thêm 2026-10-10 (P6).
///
/// 🛑 <see cref="NoiDung"/> là ẢNH CHỤP thứ ĐÃ thực sự trả ra / gửi đi, không phải thứ sẽ dựng lại. Đây là
/// chỗ khác hẳn <c>WebhookDelivery</c> (cố ý KHÔNG lưu payload, để lượt thử lại mang câu trả lời mới nhất):
/// hai câu hỏi khác nhau. Hàng đợi hỏi *"gửi gì bây giờ"*; bảng này trả lời *"hôm ấy đã gửi gì"* — mà kho tri
/// thức đổi liên tục, nên câu thứ hai không dựng lại được từ kho (`G5`).
///
/// ⚠ Mỗi lượt THỬ gửi webhook là một lượt gợi ý: lượt hỏng chỉ có nấc RETRIEVED, lượt giao được có thêm SHOWN.
/// </summary>
public sealed class LuotGoiY : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary><c>hoi</c> hoặc <c>webhook</c>.</summary>
    public required string KenhRa { get; init; }

    public Guid? CaseId { get; init; }

    public string? KhoaNguonCase { get; init; }

    public string? Nhom { get; init; }

    public required string NguonDinhTuyen { get; init; }

    public required bool BietTraLoi { get; init; }

    public string? LyDoKhongBiet { get; init; }

    public Guid? ProcessDefinitionId { get; init; }

    public Guid? ProcessRunId { get; init; }

    public string? MaBuoc { get; init; }

    public List<Guid> KnowledgeRecordIds { get; init; } = [];

    /// <summary>JSON của thứ đã trả ra / gửi đi. Đặt NGAY trước khi giao — xem phần tóm tắt.</summary>
    public string NoiDung { get; set; } = "{}";

    public DateTimeOffset Luc { get; init; } = DateTimeOffset.UtcNow;

    public List<LuotGoiYSuKien> SuKien { get; init; } = [];

    /// <summary>Thêm một nấc. CHỈ THÊM — không sửa, không xoá (`K-B3`: không ghi đè quá khứ).</summary>
    public LuotGoiYSuKien Ghi(NacGoiY nac, bool datDuoc, DateTimeOffset luc,
        string? nguoiKhai = null, string? ghiChu = null, string? nhomDung = null)
    {
        var sk = new LuotGoiYSuKien
        {
            TenantId = TenantId, LuotGoiYId = Id, Nac = nac, DatDuoc = datDuoc, Luc = luc,
            NguoiKhai = nguoiKhai, GhiChu = ghiChu, NhomDung = nhomDung,
        };

        SuKien.Add(sk);
        return sk;
    }
}

/// <summary>Một nấc đã xảy ra. Append-only.</summary>
public sealed class LuotGoiYSuKien : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid LuotGoiYId { get; init; }

    public required NacGoiY Nac { get; init; }

    public required bool DatDuoc { get; init; }

    public DateTimeOffset Luc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Ai khai — CHỈ ở hai nấc lời khai. LỜI KHAI, như mọi tên người trong hệ thống hôm nay.</summary>
    public string? NguoiKhai { get; init; }

    public string? GhiChu { get; init; }

    /// <summary>
    /// `E2-1`: người dùng nói nhóm định tuyến SAI và nhóm đúng là gì. Lưu ở đây cả khi nó đã được áp vào kho
    /// định tuyến — để bộ eval đọc được cả "định tuyến nói gì" lẫn "người sửa thành gì".
    /// </summary>
    public string? NhomDung { get; init; }
}
