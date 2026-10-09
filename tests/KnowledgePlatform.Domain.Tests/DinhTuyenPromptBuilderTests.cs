using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>Payload định tuyến bằng model (P8) — ĐƯỜNG RA mới tới bên thứ ba, nên nó phải qua cổng che như mọi đường ra.</summary>
public sealed class DinhTuyenPromptBuilderTests
{
    private static readonly NhomTaxonomy[] Nhom =
    [
        new("Lệch cấu hình kết nối", "mã lỗi E00xx trong nhật ký"),
        new("Thiếu quyền ký hiệu", null),
    ];

    [Fact]
    public void Payload_co_ten_va_ranh_gioi_moi_nhom_cung_tieu_de_mo_ta()
    {
        var p = DinhTuyenPromptBuilder.Build("Không phát hành được", "Báo E0042", Nhom);

        Assert.Contains("Lệch cấu hình kết nối: mã lỗi E00xx trong nhật ký", p.UserText);
        Assert.Contains("Thiếu quyền ký hiệu", p.UserText);
        Assert.Contains("Báo E0042", p.UserText);
    }

    /// <summary>`AR-o`: bí mật trong mô tả ticket KHÔNG được đi ra model — mặc định là CÓ che.</summary>
    [Fact]
    public void Mac_dinh_CHE_bi_mat_trong_mo_ta()
    {
        var p = DinhTuyenPromptBuilder.Build("x", "anh cho em ultraview, Mật khẩu: 13579", Nhom);

        Assert.DoesNotContain("13579", p.UserText);
        Assert.Contains(EgressRedactor.Marker, p.UserText);
        Assert.True(p.RedactedCount > 0);
    }

    [Fact]
    public void Co_N1_2_bat_thi_payload_NGUYEN_VAN_va_dem_0()
    {
        var p = DinhTuyenPromptBuilder.Build("x", "Mật khẩu: 13579", Nhom, quaCongChe: false);

        Assert.Contains("13579", p.UserText);
        Assert.Equal(0, p.RedactedCount);
    }

    [Fact]
    public void Taxonomy_rong_thi_tu_choi() =>
        Assert.Throws<ArgumentException>(() => DinhTuyenPromptBuilder.Build("x", "y", []));
}
