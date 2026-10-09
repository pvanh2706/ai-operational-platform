using System.Text.Json;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Bảng định tuyến đã đo — chỗ khớp khoá.
///
/// 🛑 Vì sao file này có: tới 2026-10-10 bảng chỉ khớp CHÍNH XÁC, trong khi bảng thật lưu mã trần
/// (<c>ES-…</c>) còn case vào qua connector mang tiền tố nguồn (<c>jira:ES-…</c>). Mọi lượt webhook
/// vì thế ra KHÔNG BIẾT cho cả 150 ticket đã đo, và không test nào đỏ.
/// </summary>
public sealed class DinhTuyenTraSanTests : IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private DinhTuyenTraSan Bang(Dictionary<string, string> maVaNhom)
    {
        var duong = Path.Combine(Path.GetTempPath(), $"dt-{Guid.CreateVersion7():N}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new
        {
            ketQua = maVaNhom.ToDictionary(
                x => x.Key,
                x => new { nhom = x.Value, nguon = "noi-ro-trong-mo-ta", vietTat = "test" }),
        }));
        _fileTam.Add(duong);
        return new DinhTuyenTraSan(duong);
    }

    [Theory]
    [InlineData("jira:ES-345990", "ES-345990")]
    [InlineData("jira:ES-345990#comment-7", "ES-345990")]
    [InlineData("crm:deal/4471", "deal/4471")]     // KHÔNG gắn cứng tên connector nào (G1)
    [InlineData("ES-345990", null)]                 // không có gì để bỏ
    [InlineData("jira:", null)]                     // bỏ xong còn rỗng thì không phải một mã
    public void Bo_tien_to_nguon(string khoa, string? mongDoi) =>
        Assert.Equal(mongDoi, DinhTuyenTraSan.BoTienToNguon(khoa));

    [Fact]
    public async Task Khoa_nguon_co_tien_to_van_khop_bang_luu_ma_tran()
    {
        var bang = Bang(new() { ["ES-1"] = "nhom-a" });

        var kq = await bang.DinhTuyenAsync(new YeuCauDinhTuyen("jira:ES-1", null, null));

        Assert.True(kq.BietNhom);
        Assert.Equal("nhom-a", kq.Nhom);
    }

    /// <summary>
    /// Khớp chính xác THẮNG. Bảng có cả hai dạng thì dạng đầy đủ là thứ ai đó cố ý ghi vào, còn
    /// dạng trần chỉ là đường lùi — đảo thứ tự là để đường lùi đè lên ý muốn của người ghi.
    /// </summary>
    [Fact]
    public async Task Khop_chinh_xac_duoc_uu_tien_hon_khoa_da_bo_tien_to()
    {
        var bang = Bang(new() { ["jira:ES-1"] = "nhom-day-du", ["ES-1"] = "nhom-tran" });

        var kq = await bang.DinhTuyenAsync(new YeuCauDinhTuyen("jira:ES-1", null, null));

        Assert.Equal("nhom-day-du", kq.Nhom);
    }

    [Fact]
    public async Task Khong_co_trong_bang_ke_ca_sau_khi_bo_tien_to_thi_van_KHONG_BIET()
    {
        var bang = Bang(new() { ["ES-1"] = "nhom-a" });

        var kq = await bang.DinhTuyenAsync(new YeuCauDinhTuyen("jira:ES-2", null, null));

        Assert.False(kq.BietNhom);
        Assert.Equal("tra-san-khong-co-trong-bang", kq.Nguon);
    }
}
