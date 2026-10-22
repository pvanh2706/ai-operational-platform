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
    /// `N7-1` (PROPOSED, 2026-10-10): BẬT định tuyến ticket MỚI bằng model. Mặc định TẮT — có khoá API thôi thì CHƯA
    /// gọi. Lý do nó là một cờ riêng: định tuyến chạy TỰ ĐỘNG trên mọi webhook và mọi <c>/hoi</c>, một loại chi phí
    /// mà trần của `E3-1` (người bấm soạn nháp) chưa bao giờ phủ.
    /// </summary>
    public bool GoiModel { get; set; }

    /// <summary>
    /// Model tự chấm dưới ngưỡng này thì hệ thống IM LẶNG — KHÔNG BIẾT (`E1-1`: thà im lặng hơn thà đoán).
    /// ⚠ 0,9 là PROPOSED, CHƯA ĐO: chưa có lượt gọi thật nào để biết model tự chấm lệch tới đâu.
    /// </summary>
    public double NguongTinCay { get; set; } = 0.9;

    /// <summary>Trần lượt gọi model định tuyến mỗi tháng, mỗi khách. Biên lai ghi TRƯỚC khi gọi.</summary>
    public int TranThang { get; set; } = 500;

    /// <summary>
    /// `Q6-1` (2026-10-22): nhà cung cấp gọi khi định tuyến bằng model — <c>anthropic</c> (mặc định) hoặc <c>openai</c>.
    /// Là cấu hình HOST chứ không là lựa chọn của console, vì định tuyến chạy tự động trên mọi webhook — xem CacModel.
    /// Sai tên thì host TỪ CHỐI khởi động.
    /// </summary>
    public string? NhaCungCap { get; set; }

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
