using KnowledgePlatform.Domain.Evidence;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Luật thuần của `AR-l` và `AR-m` — KHÔNG chạm PostgreSQL (`IM-18`).
///
/// Bộ test API đã kiểm hai đường này qua HTTP thật. Chỗ đứng của file này khác: nó kiểm
/// những nhánh mà đi qua HTTP thì **không diễn đạt được** — ví dụ gọi <c>RutLai</c> hai lần
/// trên cùng một đối tượng, hay ép một giá trị dài 65 ký tự vào bộ chuẩn hoá.
/// </summary>
public sealed class RutLaiVaMaKhachSanTests
{
    private static EvidenceItem Mau() => new()
    {
        TenantId = Guid.CreateVersion7(),
        Content = "Khach bao o ky hieu trong khi phat hanh",
        SourceReference = "jira:ES-346661#description",
    };

    // =====================================================================
    //  AR-m — dấu rút lại
    // =====================================================================

    [Fact]
    public void Mau_moi_nap_thi_chua_bi_rut_lai()
    {
        var mau = Mau();

        Assert.False(mau.DaRutLai);
        Assert.Null(mau.RetractedAt);
        Assert.Null(mau.RetractedBy);
        Assert.Null(mau.RetractedReason);
    }

    [Fact]
    public void Rut_lai_ghi_du_ba_truong_mot_luot()
    {
        var mau = Mau();

        mau.RutLai("anh.phamviet", "Nguoi gui noi gui nham noi dung");

        Assert.True(mau.DaRutLai);
        Assert.NotNull(mau.RetractedAt);
        Assert.Equal("anh.phamviet", mau.RetractedBy);
        Assert.Equal("Nguoi gui noi gui nham noi dung", mau.RetractedReason);
    }

    /// <summary>
    /// 🛑 `B1-1` chốt CHỈ NGƯỜI điền tay, nên "người nào" là bắt buộc. Một dấu vết không có
    /// tác giả thì không phân biệt được với một dấu vết do script đặt — mà *script không được
    /// đặt* chính là toàn bộ nội dung của quyết định này.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Khong_khai_nguoi_danh_dau_thi_nem(string nguoi)
    {
        var mau = Mau();

        Assert.Throws<ArgumentException>(() => mau.RutLai(nguoi, "Ly do that"));
        Assert.False(mau.DaRutLai);
    }

    /// <summary>
    /// Lý do cũng bắt buộc: ca thật của `ES-346661` là *"người gửi nói gửi nhầm nội dung,
    /// đoạn này là của case khác"* — một câu mà không bộ mã dựng sẵn nào đoán trước được, và
    /// một dấu rút lại không có lý do thì người duyệt sau không biết có tin được không.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Khong_khai_ly_do_thi_nem(string lyDo)
    {
        var mau = Mau();

        Assert.Throws<ArgumentException>(() => mau.RutLai("anh.phamviet", lyDo));
        Assert.False(mau.DaRutLai);
    }

    /// <summary>
    /// 🛑 Rút lần hai thì NÉM, và lần đầu còn nguyên. Cho ghi đè là để lần sau âm thầm xoá
    /// mất tác giả + lý do của lần đầu — tức mất đúng thứ ba cột này sinh ra để giữ.
    /// </summary>
    [Fact]
    public void Rut_lai_lan_hai_thi_nem_va_GIU_nguyen_lan_dau()
    {
        var mau = Mau();
        mau.RutLai("nguoi.thu.nhat", "Ly do that");
        var luc = mau.RetractedAt;

        Assert.Throws<InvalidOperationException>(
            () => mau.RutLai("nguoi.thu.hai", "Ly do khac han"));

        Assert.Equal("nguoi.thu.nhat", mau.RetractedBy);
        Assert.Equal("Ly do that", mau.RetractedReason);
        Assert.Equal(luc, mau.RetractedAt);
    }

    /// <summary>Khoảng trắng thừa bị cắt — để hai lần đánh dấu của cùng một người không thành hai người.</summary>
    [Fact]
    public void Ten_nguoi_va_ly_do_duoc_cat_khoang_trang()
    {
        var mau = Mau();

        mau.RutLai("  anh.phamviet  ", "  Gui nham  ");

        Assert.Equal("anh.phamviet", mau.RetractedBy);
        Assert.Equal("Gui nham", mau.RetractedReason);
    }

    // =====================================================================
    //  AR-l — mã khách sạn
    // =====================================================================

    /// <summary>
    /// 🛑 Bỏ trống KHÔNG được tự hiểu thành <c>khong-xac-dinh</c>. Đây là luật trung tâm của
    /// `AR-l` ở tầng giá trị: *"bên gửi đã cân nhắc và chưa biết"* khác *"bên gửi chưa đọc
    /// tài liệu"*, và gộp hai cái đó thì về sau không cột nào tách chúng ra nữa.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bo_trong_thi_KHONG_chuan_hoa_duoc(string? raw)
    {
        Assert.False(SubTenantCodes.TryChuanHoa(raw, out var ma));
        Assert.Equal(string.Empty, ma);
    }

    [Fact]
    public void Ma_that_giu_nguyen_van()
    {
        Assert.True(SubTenantCodes.TryChuanHoa("  MIRAH-01  ", out var ma));
        Assert.Equal("MIRAH-01", ma);
    }

    /// <summary>
    /// Sentinel viết hoa thế nào cũng về đúng một dạng kebab. Hai cách viết của CÙNG một
    /// sentinel mà nằm thành hai dòng khác nhau là tự thổi phép đếm giá trị phân biệt —
    /// đúng con số quyết định việc gỡ một cổng bảo mật.
    /// </summary>
    [Theory]
    [InlineData("KHONG-XAC-DINH", "khong-xac-dinh")]
    [InlineData("Khong-Xac-Dinh", "khong-xac-dinh")]
    [InlineData("KHONG-THUOC-KHACH-SAN", "khong-thuoc-khach-san")]
    public void Gia_tri_danh_rieng_ve_dang_kebab(string raw, string mong)
    {
        Assert.True(SubTenantCodes.TryChuanHoa(raw, out var ma));
        Assert.Equal(mong, ma);
    }

    [Fact]
    public void Dai_qua_tran_thi_KHONG_chuan_hoa_duoc()
    {
        var qua = new string('x', SubTenantCodes.MaxLength + 1);

        Assert.False(SubTenantCodes.TryChuanHoa(qua, out _));
        Assert.True(SubTenantCodes.TryChuanHoa(new string('x', SubTenantCodes.MaxLength), out _));
    }

    /// <summary>
    /// 🛑 Phép phân loại mà điều kiện mở lại của `AR-l` đứng lên: hai sentinel KHÔNG phải mã
    /// khách sạn. Bỏ luật này thì đổi từ sentinel này sang sentinel kia là "đạt ≥2 giá trị
    /// phân biệt", trong khi không khách sạn nào được phân biệt với khách sạn nào.
    /// </summary>
    [Theory]
    [InlineData("khong-xac-dinh", true)]
    [InlineData("khong-thuoc-khach-san", true)]
    [InlineData("KHONG-XAC-DINH", true)]
    [InlineData("17468", false)]
    [InlineData("MIRAH-01", false)]
    [InlineData(null, false)]
    public void Phan_biet_duoc_sentinel_voi_ma_that(string? ma, bool laDanhRieng)
    {
        Assert.Equal(laDanhRieng, SubTenantCodes.LaGiaTriDanhRieng(ma));
    }
}
