using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `AR-a` — tập giá trị của nhãn QUYỀN XEM, chốt 2026-09-15 (phiếu `docs/14` §A2).
///
/// 🛑 Test quan trọng nhất file này là <see cref="Dang_luu_la_KEBAB_va_GHIM_CUNG"/>, và nó
/// bảo vệ một thứ mà không test nào khác chạm tới: **dạng lưu xuống database**.
/// `kp_dev` đã có 7 dòng mang `noi-bo` từ đợt build 2026-09-13. Nếu ai đó đổi tên enum, hoặc
/// đổi sang `HasConversion&lt;string&gt;()` cho "gọn", thì dạng lưu thành `NoiBo` và 7 dòng cũ
/// **không đọc lại được** — hỏng ở lúc ĐỌC, tức xa chỗ gây ra, và không test nào khác đỏ vì
/// chúng đều ghi rồi đọc bằng cùng một hàm.
/// </summary>
public sealed class VisibilityScopeTests
{
    /// <summary>
    /// Ghim CỨNG từng chuỗi. Cố ý KHÔNG sinh chuỗi mong đợi bằng `ToWire()` — làm vậy thì
    /// test chỉ khẳng định "hàm bằng chính nó", và sẽ xanh sau MỌI thay đổi.
    /// </summary>
    [Theory]
    [InlineData(VisibilityScope.RiengTu, "rieng-tu")]
    [InlineData(VisibilityScope.NoiBo, "noi-bo")]
    [InlineData(VisibilityScope.ToanCongTy, "toan-cong-ty")]
    public void Dang_luu_la_KEBAB_va_GHIM_CUNG(VisibilityScope value, string mongDoi)
    {
        Assert.Equal(mongDoi, value.ToWire());
    }

    /// <summary>
    /// `noi-bo` — giá trị 7 record trong `kp_dev` đang mang — phải đọc lại được.
    /// Đây là ca thật, không phải ca giả định.
    /// </summary>
    [Fact]
    public void Gia_tri_da_co_trong_kho_doc_lai_duoc()
    {
        Assert.True(VisibilityScopes.TryParse("noi-bo", out var v));
        Assert.Equal(VisibilityScope.NoiBo, v);
    }

    [Theory]
    [InlineData("rieng-tu", VisibilityScope.RiengTu)]
    [InlineData("NOI-BO", VisibilityScope.NoiBo)]
    [InlineData("  toan-cong-ty  ", VisibilityScope.ToanCongTy)]
    public void Doc_duoc_bat_ke_hoa_thuong_va_khoang_trang_thua(string raw, VisibilityScope mongDoi)
    {
        Assert.True(VisibilityScopes.TryParse(raw, out var v));
        Assert.Equal(mongDoi, v);
    }

    /// <summary>
    /// ⚠ `NoiBo` (tên C#) KHÔNG phải dạng lưu, nên KHÔNG được đọc ra. Nếu ca này đỏ thì có
    /// người vừa đổi sang `HasConversion&lt;string&gt;()` — và 7 dòng cũ vừa thành rác.
    /// </summary>
    [Theory]
    [InlineData("NoiBo")]
    [InlineData("noibo")]
    [InlineData("noi_bo")]
    [InlineData("team:ops")]
    [InlineData("company")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("2")]
    public void Chuoi_ngoai_tap_da_chot_thi_KHONG_doc_ra(string? raw)
    {
        Assert.False(VisibilityScopes.TryParse(raw, out _));
    }

    /// <summary>
    /// `S7` nói mặc định là HẸP NHẤT. Câu đó chỉ có nghĩa khi tập giá trị CÓ THỨ TỰ — đây là
    /// chỗ ghim thứ tự ấy, và là lý do chọn ba mức chứ không phải hai.
    /// </summary>
    [Fact]
    public void Thu_tu_tang_dan_la_pham_vi_rong_dan()
    {
        Assert.True(VisibilityScope.RiengTu < VisibilityScope.NoiBo);
        Assert.True(VisibilityScope.NoiBo < VisibilityScope.ToanCongTy);
    }

    /// <summary>
    /// Bảng ánh xạ phải phủ HẾT enum. Thêm một giá trị vào enum mà quên thêm vào bảng thì
    /// `ToWire()` ném `InvalidOperationException` ở lúc chạy — test này bắt sớm hơn.
    /// </summary>
    [Fact]
    public void Moi_gia_tri_cua_enum_deu_co_dang_luu()
    {
        foreach (var v in Enum.GetValues<VisibilityScope>())
        {
            var wire = v.ToWire();
            Assert.False(string.IsNullOrWhiteSpace(wire));
            Assert.True(VisibilityScopes.TryParse(wire, out var vong), $"{v} không đi vòng lại được");
            Assert.Equal(v, vong);
        }
    }

    /// <summary>Danh sách báo lỗi phải liệt kê ĐỦ ba, kẻo bên tích hợp đoán mò.</summary>
    [Fact]
    public void Danh_sach_bao_loi_liet_ke_du_ba_gia_tri()
    {
        var allowed = VisibilityScopes.Allowed;

        Assert.Contains("rieng-tu", allowed);
        Assert.Contains("noi-bo", allowed);
        Assert.Contains("toan-cong-ty", allowed);
    }
}
