namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Cấu hình cho bộ định tuyến.
///
/// ⚠ Bản đang cắm là bản TRA SẴN (`DinhTuyenTraSan`) — không gọi API, không tốn credit, và
/// **không định tuyến gì cả**: nó tra một bảng đã có đáp án. Đó là quyết định chốt
/// 2026-09-13 ("build sao cho chạy được không cần credit"), không phải một thiếu sót.
///
/// Đổi sang bản gọi model thật = đổi một dòng đăng ký DI. Nơi gọi không đổi (`AR3`).
/// </summary>
public sealed class DinhTuyenOptions
{
    public const string SectionName = "DinhTuyen";

    /// <summary>
    /// Đường dẫn tới bảng kết quả đã đo. Để trống thì tự tìm từ gốc repo — tiện cho máy dev
    /// và cho test, nhưng bản deploy thật nên khai tường minh.
    /// </summary>
    public string? DuongDanBangTraSan { get; set; }

    /// <summary>
    /// Tìm gốc repo bằng thư mục <c>.git</c>, cùng cách <c>SoanNhapRunner</c> đã dùng.
    /// </summary>
    public static string DuongDanMacDinh()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, ".git")))
        {
            d = d.Parent;
        }

        var goc = d?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(goc, "docs", "ket-qua-phan-tich", "dinh-tuyen-150-case.json");
    }
}
