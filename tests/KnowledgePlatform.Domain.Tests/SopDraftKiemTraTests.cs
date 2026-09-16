using System.Text.Json;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Bộ kiểm bản nháp SOP ở cửa vào (`IM-41`).
///
/// 🛑 **Mọi test ở đây đi qua JSON, không dựng object bằng C#** — và đó chính là điểm. Dựng
/// bằng object initializer thì trình biên dịch ép đủ trường `required`, nên ca hỏng thật
/// KHÔNG TẠO RA ĐƯỢC. Đúng chỗ mù mà `IM-38` đã đo một lần: bộ test tự cấp vật liệu cho mình
/// chỉ kiểm được những hình dạng người viết test nghĩ ra.
/// </summary>
public sealed class SopDraftKiemTraTests
{
    private static SopDraft Doc(string json) =>
        JsonSerializer.Deserialize<SopDraft>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    /// <summary>Bản nháp tối thiểu ĐÚNG — mốc để so.</summary>
    private const string Dung = """
        {
          "nhom": "nhom-thu",
          "soCaseDungSau": 2,
          "trieuChungVao": [{ "moTa": "Không phát hành được hoá đơn", "case": ["ES-1"] }],
          "buocKiem": [{
            "ma": "K1", "cauHoi": "Ô ký hiệu có giá trị không?", "noiXem": "Màn phát hành",
            "nhanh": [{ "quanSat": "Ô trống", "diToi": "S1", "case": ["ES-1"], "chungCu": "evidence-noi-ro" }]
          }],
          "buocSua": [{ "ma": "S1", "viec": "Chọn ký hiệu rồi phát hành lại", "case": ["ES-1"] }],
          "phanBoBuocKiemDuocGhiLai": {
            "ghi-ro-buoc-kiem": { "so": 2, "case": ["ES-1", "ES-2"] },
            "suy-ra-duoc-nhung-khong-ghi": { "so": 0, "case": [] },
            "chi-ghi-ket-luan": { "so": 0, "case": [] },
            "buoc-kiem-ngoai-ticket": { "so": 0, "case": [] },
            "khong-phai-chan-doan": { "so": 0, "case": [] }
          }
        }
        """;

    [Fact]
    public void Ban_nhap_dung_thi_khong_co_cho_sai()
    {
        Assert.Null(SopDraftKiemTra.KiemTra(Doc(Dung)));
    }

    /// <summary>
    /// 🛑 **CA ĐÃ LÀM ENDPOINT TRẢ 500**, tìm ra bằng `scripts/kiem_hai_khach.py`.
    ///
    /// Payload gõ `dieuKien` thay cho `quanSat`. JSON hợp lệ, object dựng được, `required`
    /// không chặn (nó chỉ ràng buộc mã C# khởi tạo) — và nó chạy tiếp tới tận
    /// <c>SopDraftToKnowledge.DatTenNguyenNhan</c> rồi nổ bằng NullReferenceException.
    ///
    /// Khác biệt 400 vs 500 không phải thẩm mỹ: 400 nói bên tích hợp gõ sai chỗ nào, 500 nói
    /// với họ rằng lỗi ở phía mình — và họ sẽ đi tìm sai chỗ, rất lâu.
    /// </summary>
    [Fact]
    public void Go_sai_TEN_truong_thi_bao_400_chu_khong_no()
    {
        var json = Dung.Replace("\"quanSat\"", "\"dieuKien\"");

        var sai = SopDraftKiemTra.KiemTra(Doc(json));

        Assert.NotNull(sai);
        Assert.Contains("quanSat", sai);   // nói ĐÚNG tên trường trong JSON, không phải tên C#
        Assert.Contains("K1", sai);        // và nói ĐÚNG bước nào
    }

    /// <summary>
    /// Nhóm trường nằm trong **positional record** — thiếu thì `System.Text.Json` IM LẶNG cho
    /// `null`. Đây mới là nhóm mà <see cref="SopDraftKiemTra"/> thật sự canh.
    /// </summary>
    [Theory]
    [InlineData("\"quanSat\"", "\"quanSatSai\"", "quanSat")]
    [InlineData("\"diToi\"", "\"diToiSai\"", "diToi")]
    [InlineData("\"moTa\"", "\"moTaSai\"", "moTa")]
    [InlineData("\"viec\"", "\"viecSai\"", "viec")]
    public void Truong_cua_POSITIONAL_RECORD_thieu_thi_bi_bat(string tu, string thanh, string nhacTen)
    {
        var sai = SopDraftKiemTra.KiemTra(Doc(Dung.Replace(tu, thanh)));

        Assert.NotNull(sai);
        Assert.Contains(nhacTen, sai);
    }

    /// <summary>
    /// 🎯 **Phân biệt phải ghi lại, vì tôi đã đọc nhầm nó một lần.** Nhóm trường khai
    /// `required` thì `System.Text.Json` **CÓ** ép: thiếu là ném <c>JsonException</c>, và
    /// ASP.NET đổi nó thành 400 — kín sẵn, không nhờ bộ kiểm này.
    ///
    /// Test này ghim đúng ranh giới đó, để người sau không tưởng rằng
    /// <see cref="SopDraftKiemTra"/> là thứ đang giữ nhóm ấy — và cũng để biết ngay nếu một
    /// phiên bản .NET sau đổi hành vi.
    /// </summary>
    [Theory]
    [InlineData("\"nhom\"", "\"nhomSai\"")]
    [InlineData("\"trieuChungVao\"", "\"trieuChungSai\"")]
    [InlineData("\"buocSua\"", "\"buocSuaSai\"")]
    [InlineData("\"phanBoBuocKiemDuocGhiLai\"", "\"phanBoSai\"")]
    [InlineData("\"cauHoi\"", "\"cauHoiSai\"")]
    [InlineData("\"noiXem\"", "\"noiXemSai\"")]
    public void Truong_khai_required_thi_CHINH_JSON_da_nem(string tu, string thanh)
    {
        Assert.Throws<JsonException>(() => Doc(Dung.Replace(tu, thanh)));
    }

    [Fact]
    public void Than_request_rong_thi_bao_ra_chu_khong_no()
    {
        Assert.NotNull(SopDraftKiemTra.KiemTra(null));
    }

    /// <summary>
    /// Mảng `buocKiem` RỖNG là hợp lệ — có nhóm không có bước kiểm nào ghi lại được, và
    /// `docs/11` đo được rằng đó là ca THƯỜNG chứ không phải ngoại lệ. Chặn nó ở đây là đuổi
    /// đúng phần dữ liệu thật ra khỏi hệ thống.
    /// </summary>
    [Fact]
    public void Mang_buocKiem_RONG_van_hop_le()
    {
        // Cắt nguyên mảng buocKiem ra khỏi bản đúng, thay bằng mảng rỗng.
        var dau = Dung.IndexOf("\"buocKiem\"", StringComparison.Ordinal);
        var cuoi = Dung.IndexOf("\"buocSua\"", StringComparison.Ordinal);
        var json = Dung[..dau] + "\"buocKiem\": [],\n  " + Dung[cuoi..];

        Assert.Null(SopDraftKiemTra.KiemTra(Doc(json)));
    }
}
