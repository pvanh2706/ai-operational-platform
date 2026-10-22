using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>Tên nhà cung cấp model — chuỗi đi qua cấu hình, request và biên lai (`Q1-1`, `Q6-1`).</summary>
public static class NhaCungCap
{
    public const string Anthropic = "anthropic";
    public const string OpenAI = "openai";

    public const string DanhSach = "anthropic · openai";

    /// <summary>Chuẩn hoá (chữ thường, bỏ khoảng trắng). <c>false</c> khi không phải một trong hai tên.</summary>
    public static bool TryChuanHoa(string? s, out string ncc)
    {
        ncc = (s ?? string.Empty).Trim().ToLowerInvariant();
        return ncc is Anthropic or OpenAI;
    }

    /// <summary>Giá trị options ĐÃ được kiểm lúc khởi động — trống → Anthropic.</summary>
    public static string TuCauHinhDaChuan(string? giaTri) =>
        TryChuanHoa(giaTri, out var ncc) ? ncc : Anthropic;

    /// <summary>
    /// Đọc một khoá cấu hình chọn nhà cung cấp. Trống → Anthropic (giữ hành vi trước `Q1-1`). Sai tên → NÉM lúc khởi động:
    /// một lỗi chính tả ở đây mà âm thầm rơi về mặc định là tiêu tiền ở nhà cung cấp người vận hành không chọn.
    /// </summary>
    public static string TuCauHinh(IConfiguration cfg, string khoa)
    {
        var giaTri = cfg[khoa];
        if (string.IsNullOrWhiteSpace(giaTri)) return Anthropic;

        return TryChuanHoa(giaTri, out var ncc)
            ? ncc
            : throw new InvalidOperationException(
                $"{khoa} = \"{giaTri}\" không phải nhà cung cấp nào. Nhận: {DanhSach}. Để trống = {Anthropic}.");
    }
}

/// <summary>Trạng thái MỘT nhà cung cấp — KHÔNG BAO GIỜ chứa khoá.</summary>
/// <param name="DungDuoc">Có khoá VÀ (với OpenAI) có model. CHƯA nói khoá hợp lệ — đó là việc của "Thử khoá".</param>
public sealed record TrangThaiNhaCungCap(string Ten, bool CoKhoa, string? Model, bool DungDuoc, string? LyDo);

/// <summary>Một bước có AI, phía HOST: nhà cung cấp mặc định và trần.</summary>
public sealed record TrangThaiBuocAi(string NhaCungCap, bool DungDuoc, int TranThang);

/// <summary>Thân của <c>GET /internal/ai</c> — đủ để màn Cấu hình (`Q6-1`) vẽ, không thứ gì để lộ.</summary>
/// <param name="DinhTuyenGoiModel"><c>DinhTuyen:GoiModel</c> (`N7-1`). Tắt thì định tuyến KHÔNG gọi model dù có khoá.</param>
public sealed record TrangThaiAi(
    TrangThaiNhaCungCap Anthropic,
    TrangThaiNhaCungCap OpenAI,
    TrangThaiBuocAi DinhTuyen,
    bool DinhTuyenGoiModel,
    double DinhTuyenNguongTinCay,
    TrangThaiBuocAi SoanNhap,
    TrangThaiBuocAi HoiDap);

/// <summary>
/// Sổ đăng ký nhà cung cấp model — `Q1-1` + `Q6-1` (2026-10-22). Đọc khoá ĐÚNG MỘT LẦN cho mỗi nhà cung cấp, rồi dựng
/// bản hiện thực theo tên.
///
/// 🎯 Hai loại bước có AI chọn nhà cung cấp theo hai cách khác nhau, và đó là cố ý:
/// <list type="bullet">
///   <item><b>định tuyến</b> chạy TỰ ĐỘNG trên mọi webhook và mọi <c>/hoi</c> — không có ai đứng đó chọn, nên nhà cung cấp
///     là cấu hình của host (<c>DinhTuyen:NhaCungCap</c>). Màn Cấu hình của console chỉ HIỆN nó;</item>
///   <item><b>soạn nháp</b> (và hỏi đáp, `Q3-1`) có người bấm — request mang <c>nhaCungCap</c> được, không mang thì dùng
///     mặc định của host (<c>SoanNhap:NhaCungCap</c>).</item>
/// </list>
/// Đây là hệ quả `docs/18` `Q6` đã nói trước: lựa chọn của console không thể chạm tới một lượt định tuyến không đi qua
/// console.
/// </summary>
public sealed class CacModel
{
    private readonly IHttpClientFactory _http;
    private readonly string? _khoaOpenAi;

    public CacModel(IConfiguration cfg, IHttpClientFactory http, CongChe congChe)
    {
        _http = http;
        QuaCongChe = congChe.DaQuaCongChe;

        if (KhoaAnthropic.Doc(cfg) is { } khoa) AnthropicClient = new Anthropic.AnthropicClient { ApiKey = khoa };

        _khoaOpenAi = KhoaOpenAI.Doc(cfg);
        ModelOpenAi = KhoaOpenAI.Model(cfg);
    }

    /// <summary><c>null</c> = không có khoá Anthropic.</summary>
    public Anthropic.AnthropicClient? AnthropicClient { get; }

    public bool CoKhoaOpenAi => _khoaOpenAi is not null;

    public string? ModelOpenAi { get; }

    public bool QuaCongChe { get; }

    public bool DungDuoc(string ncc) => ncc == NhaCungCap.OpenAI
        ? CoKhoaOpenAi && ModelOpenAi is not null
        : AnthropicClient is not null;

    /// <summary>Client OpenAI, hoặc <c>null</c> khi thiếu khoá hoặc thiếu model (`Q2-1`).</summary>
    /// <param name="khongThuLai">Cho "Thử khoá": một lượt, không timeout dài.</param>
    public OpenAiChat? OpenAi(bool khongThuLai = false)
    {
        if (_khoaOpenAi is null || ModelOpenAi is null) return null;

        var http = _http.CreateClient(OpenAiChat.TenHttpClient);
        if (khongThuLai) http.Timeout = TimeSpan.FromSeconds(30);

        return new OpenAiChat(http, _khoaOpenAi, ModelOpenAi);
    }

    /// <summary>Tên model mà nhà cung cấp này đang dùng — ghi vào biên lai và trả ra response.</summary>
    public string? TenModel(string ncc) => ncc == NhaCungCap.OpenAI ? ModelOpenAi : SopDrafterOptions.ModelMacDinh;

    /// <summary>Bộ soạn nháp của <paramref name="ncc"/>. Thiếu cấu hình → bản NÉM kèm hướng dẫn, không bao giờ <c>null</c>.</summary>
    public ISopDrafter SoanNhap(string ncc)
    {
        var tuyChon = new SopDrafterOptions { QuaCongChe = QuaCongChe };

        if (ncc == NhaCungCap.OpenAI)
        {
            return OpenAi() is { } chat
                ? new OpenAiSopDrafter(chat, tuyChon)
                : new SopDrafterChuaCoCredit(ThieuOpenAi());
        }

        return AnthropicClient is { } client
            ? new AnthropicSopDrafter(client, tuyChon)
            : new SopDrafterChuaCoCredit();
    }

    /// <summary>Bản gọi model định tuyến của <paramref name="ncc"/>, hoặc <c>null</c> khi nhà cung cấp đó chưa dùng được.</summary>
    public IGoiModelDinhTuyen? DinhTuyen(string ncc)
    {
        if (ncc == NhaCungCap.OpenAI) return OpenAi() is { } chat ? new OpenAiDinhTuyen(chat) : null;

        return AnthropicClient is { } client ? new AnthropicDinhTuyen(client) : null;
    }

    /// <summary>Bản gọi model hỏi đáp (`Q3-1`) của <paramref name="ncc"/>, hoặc <c>null</c> khi chưa dùng được.</summary>
    public IGoiModelHoiDap? HoiDap(string ncc)
    {
        if (ncc == NhaCungCap.OpenAI) return OpenAi() is { } chat ? new OpenAiHoiDap(chat) : null;

        return AnthropicClient is { } client ? new AnthropicHoiDap(client) : null;
    }

    public TrangThaiNhaCungCap TrangThai(string ncc) => ncc == NhaCungCap.OpenAI
        ? new(NhaCungCap.OpenAI, CoKhoaOpenAi, ModelOpenAi, DungDuoc(ncc),
            !CoKhoaOpenAi ? "khong-co-khoa" : ModelOpenAi is null ? ThuKhoaModel.ThieuModel : null)
        : new(NhaCungCap.Anthropic, AnthropicClient is not null, SopDrafterOptions.ModelMacDinh, DungDuoc(ncc),
            AnthropicClient is null ? "khong-co-khoa" : null);

    /// <summary>Câu hướng dẫn khi OpenAI chưa dùng được — nói thiếu KHOÁ hay thiếu MODEL, vì hai việc phải làm khác nhau.</summary>
    public string ThieuOpenAi() => CoKhoaOpenAi
        ? $"Có khoá OpenAI nhưng CHƯA đặt {KhoaOpenAI.TenCauHinhModel} (Q2-1: không có mặc định — chủ dự án chọn model). " +
          "Đặt nó trong appsettings.Local.json (máy dev) hoặc biến môi trường OpenAI__Model, rồi khởi động lại app."
        : $"Chưa cấu hình khoá OpenAI ({KhoaOpenAI.TenCauHinh} hoặc {KhoaOpenAI.TenMoiTruong}) nên không gọi OpenAI được. " +
          "Máy dev: điền vào appsettings.Local.json rồi khởi động lại app. Vòng không cần khoá: chế độ File " +
          "(--xuat-soan-nhap → đưa cho Claude/Codex → POST /knowledge/sop-draft).";
}
