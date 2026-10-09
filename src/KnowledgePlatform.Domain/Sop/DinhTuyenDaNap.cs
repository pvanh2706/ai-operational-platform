using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Một kết quả định tuyến do NGƯỜI VẬN HÀNH nạp vào, không phải do sản phẩm tự sinh.
///
/// 🛑 **Vì sao bảng này tồn tại, và nó là một cái NẠNG chứ không phải một tính năng.**
/// Bản định tuyến đang chạy (<see cref="IDinhTuyenNhom"/>) cần gọi model, mà tài khoản API
/// thì chưa có credit. Vòng thay thế: **xuất dữ liệu ra → một ngữ cảnh AI khác đọc → nạp kết
/// quả về**. Đúng đường mà `IM-29` đã đi cho bản nháp SOP ngày 2026-09-10, giờ áp cho định
/// tuyến.
///
/// ⚠ **Đừng đọc bảng này thành "sản phẩm đã định tuyến được".** Nó là bộ nhớ của một việc
/// NGƯỜI làm. Mỗi lô ticket mới lại cần một lượt xuất-dán-nạp nữa. Ngày có credit thì
/// <see cref="IDinhTuyenNhom"/> đổi bản hiện thực và bảng này thành dữ liệu lịch sử — cố ý
/// giữ <see cref="NguoiNap"/> và <see cref="NapLuc"/> để lúc đó còn tách được ra.
///
/// ⚠ **Và nó KHÔNG mang con số 84,1%.** Con số ấy đo trên một prompt và một model cụ thể ở
/// `docs/13`. Một ngữ cảnh chat khác, một model khác, một ngày khác — chất lượng có thể khác,
/// và **không ai biết khác bao nhiêu cho tới khi chấm lại** bằng
/// <c>scripts/jira-export/cham_routing.py</c>.
/// </summary>
public sealed class DinhTuyenDaNap : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>Khoá nguồn của case — cùng giá trị với <c>CanonicalCase.SourceReference</c>.</summary>
    public required string KhoaNguon { get; init; }

    /// <summary>
    /// Tên nhóm nguyên nhân, hoặc <see cref="KetQuaDinhTuyen.KhongBiet"/>.
    ///
    /// 🛑 Phải nằm trong taxonomy. Một tên nhóm bịa ra là cùng loại lỗi mà `G6`/`AP3` chặn ở
    /// bản nháp SOP, và `cham_routing.py` gọi nó là bẫy thứ ba — đường nạp PHẢI từ chối.
    /// </summary>
    public required string Nhom { get; set; }

    /// <summary>Vì sao ra kết quả này — <c>noi-ro-trong-mo-ta</c>, <c>suy-tu-trieu-chung</c>...</summary>
    public required string Nguon { get; set; }

    /// <summary>Câu giải thích ngắn, để người đọc PHẢN BÁC ĐƯỢC. `S8`/`G4`.</summary>
    public string? GiaiThich { get; set; }

    /// <summary>Ai nạp. Không phải để phân quyền — để lúc chấm lại biết lô nào của ai.</summary>
    /// <summary>
    /// Ai nạp lần GẦN NHẤT. Cho ghi đè từ 2026-10-10 (P6): phản hồi "nhóm đúng là X" của người dùng (`E2-1`) sửa
    /// một kết quả đã có, và để nguyên tên người nạp cũ là để kết quả mới mang tên người không viết ra nó.
    /// Lịch sử ai sửa gì nằm ở <c>luot_goi_y_su_kien</c>.
    /// </summary>
    public required string NguoiNap { get; set; }

    public DateTimeOffset NapLuc { get; init; } = DateTimeOffset.UtcNow;

    public bool BietNhom =>
        !string.Equals(Nhom, KetQuaDinhTuyen.KhongBiet, StringComparison.Ordinal);
}
