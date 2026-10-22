using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>Payload hỏi đáp (`Q3-1`) — luật cắt và luật che, kiểm không cần database.</summary>
public sealed class HoiDapPromptBuilderTests
{
    [Fact]
    public void Nguon_qua_tran_thi_cat_va_NOI_RA()
    {
        var nguon = new[] { new NguonHoiDap("N1", "a", new string('x', 80)), new NguonHoiDap("N2", "b", "PHAN-SAU-KHONG-DUOC-GUI") };

        var p = HoiDapPromptBuilder.Build("hỏi", "case thử", nguon, tranKyTu: 50);

        Assert.True(p.BiCat);
        Assert.Single(p.NguonDaChe);
        Assert.DoesNotContain("PHAN-SAU", p.UserText);
        Assert.Contains("CHỈ phần đầu", p.UserText);   // model được báo — "không nói" ≠ "không được gửi"
    }

    [Fact]
    public void Che_ca_cau_hoi_lan_nguon()
    {
        var p = HoiDapPromptBuilder.Build("Mật khẩu: 13579 có đúng không?", "case thử",
            [new NguonHoiDap("N1", "a", "pass: 987654321")], tranKyTu: 10_000);

        Assert.DoesNotContain("13579", p.UserText);
        Assert.DoesNotContain("987654321", p.UserText);
        Assert.Equal(2, p.RedactedCount);
    }

    [Fact]
    public void Doi_chieu_bo_phat_bieu_dan_ma_khong_co_that()
    {
        var kq = new KetQuaModelHoiDap(true,
            [new PhatBieuModel("có nguồn", ["[N1]"]), new PhatBieuModel("bịa", ["N7"])], null);

        var dc = HoiDapDoiChieu.DoiChieu(kq, new HashSet<string> { "N1" });

        Assert.True(dc.BietTraLoi);
        Assert.Equal(["N1"], dc.PhatBieu.Single().Nguon);   // `[N1]` model hay viết kèm ngoặc vẫn được nhận
        Assert.Equal(1, dc.SoPhatBieuBiBo);
    }
}
