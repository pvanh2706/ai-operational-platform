using System.Net;
using System.Text;
using Anthropic;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// "Khoá có dùng được không" (`IM-57`) — đi qua SDK THẬT bằng HttpMessageHandler giả: không mạng, không tiền.
///
/// 🛑 Test quan trọng nhất là <see cref="Thu_khoa_goi_DUONG_MIEN_PHI_khong_goi_messages"/>: cả giá trị của nút này nằm ở
/// chỗ nó KHÔNG tốn tiền. Một lần sửa vô tình đổi sang gọi /v1/messages sẽ vẫn trả lời đúng câu "khoá có hợp lệ không"
/// — và tính tiền mỗi lần bấm.
/// </summary>
public sealed class ThuKhoaModelTests
{
    private const string KhoaGia = "khoa-gia-KHONG-DUOC-LOT-RA";

    private sealed class MayChu(HttpStatusCode ma, string than) : HttpMessageHandler
    {
        public List<string> DaGoi { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            DaGoi.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
            return Task.FromResult(new HttpResponseMessage(ma) { Content = new StringContent(than, Encoding.UTF8, "application/json") });
        }
    }

    private static (AnthropicClient Client, MayChu May) Dung(HttpStatusCode ma, string than)
    {
        var may = new MayChu(ma, than);
        return (new AnthropicClient { ApiKey = KhoaGia, MaxRetries = 0, HttpClient = new HttpClient(may) }, may);
    }

    private static string Loi(string loai, string thongDiep) =>
        $$"""{"type":"error","error":{"type":"{{loai}}","message":"{{thongDiep}}"},"request_id":"req_gia"}""";

    private const string ModelThat =
        """{"type":"model","id":"claude-opus-5","display_name":"Claude Opus 5","created_at":"2026-01-01T00:00:00Z"}""";

    [Fact]
    public async Task Thu_khoa_goi_DUONG_MIEN_PHI_khong_goi_messages()
    {
        var (client, may) = Dung(HttpStatusCode.OK, ModelThat);

        await ThuKhoaModel.ThuAsync(client, "claude-opus-5");

        Assert.Equal(["GET /v1/models/claude-opus-5"], may.DaGoi);
    }

    [Fact]
    public async Task Khoa_duoc_nhan_va_co_model_thi_hop_le_va_noi_ro_khong_kiem_duoc_credit()
    {
        var (client, _) = Dung(HttpStatusCode.OK, ModelThat);

        var kq = await ThuKhoaModel.ThuAsync(client, "claude-opus-5");

        Assert.True(kq.HopLe);
        Assert.Null(kq.LyDo);
        Assert.Equal("Claude Opus 5", kq.TenModelHienThi);
        Assert.Contains("KHÔNG kiểm được số dư credit", kq.GiaiThich);   // "hợp lệ" ≠ "gọi được"
    }

    [Fact]
    public async Task Khoa_bi_tu_choi_thi_noi_dung_ly_do_va_KHONG_in_khoa()
    {
        var (client, _) = Dung(HttpStatusCode.Unauthorized, Loi("authentication_error", "invalid x-api-key"));

        var kq = await ThuKhoaModel.ThuAsync(client, "claude-opus-5");

        Assert.False(kq.HopLe);
        Assert.Equal(ModelTuChoiException.KhoaBiTuChoi, kq.LyDo);
        Assert.DoesNotContain(KhoaGia, kq.GiaiThich);
    }

    [Fact]
    public async Task Khong_co_model_thi_model_khong_ton_tai()
    {
        var (client, _) = Dung(HttpStatusCode.NotFound, Loi("not_found_error", "model: claude-opus-5"));

        var kq = await ThuKhoaModel.ThuAsync(client, "claude-opus-5");

        Assert.False(kq.HopLe);
        Assert.Equal(ModelTuChoiException.ModelKhongTonTai, kq.LyDo);
    }

    /// <summary>5xx / mạng KHÔNG nói gì về khoá — <c>hopLe: null</c>, không được báo "khoá sai".</summary>
    [Fact]
    public async Task Loi_may_chu_thi_KHONG_ket_luan_ve_khoa()
    {
        var (client, _) = Dung(HttpStatusCode.InternalServerError, Loi("api_error", "boom"));

        var kq = await ThuKhoaModel.ThuAsync(client, "claude-opus-5");

        Assert.Null(kq.HopLe);
        Assert.Equal(ThuKhoaModel.LoiKhac, kq.LyDo);
    }

    [Fact]
    public async Task Khong_co_khoa_thi_tra_loi_ngay_khong_goi_gi()
    {
        var kq = await ThuKhoaModel.ThuAsync(null, "claude-opus-5");

        Assert.False(kq.CoKhoa);
        Assert.Equal(ThuKhoaModel.KhongCoKhoa, kq.LyDo);
    }
}
