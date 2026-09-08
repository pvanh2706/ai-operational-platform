using KnowledgePlatform.Domain.Redaction;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `AR-o` — cổng che ở ranh giới GỬI RA. Xem `docs/07` §5.
///
/// ⚠ MỌI GIÁ TRỊ TRONG FILE NÀY LÀ GIẢ, chỉ GIỮ HÌNH DẠNG của bốn hình dạng đã đo trên
/// corpus thật. Không có credential thật nào ở đây, và đừng ai thêm vào: file này đi theo
/// git, và git history của repo này đã một lần chứa credential sống (`docs/10` §0).
/// Muốn đo recall trên giá trị thật thì dùng `check_corpus.py` trên corpus tại máy —
/// corpus KHÔNG theo git, cố ý.
///
/// Vì sao bộ test này nằm ở tầng Domain: `IM-18` — luật domain phải kiểm được trên một
/// máy chưa cài PostgreSQL. Luật che là một luật, không phải một cuộc gọi mạng.
/// </summary>
public sealed class EgressRedactionTests
{
    // ── Bốn hình dạng phải BẮT ĐƯỢC ───────────────────────────────────────────────

    /// <summary>Hình dạng 1: nhãn và giá trị CÙNG DÒNG.</summary>
    [Theory]
    [InlineData("ID của bạn: 123456789")]
    [InlineData("Mật khẩu: 12345")]
    [InlineData("mat khau 98765")]
    [InlineData("matkhau=45678")]
    [InlineData("pass: 987654321")]
    [InlineData("PW 5555")]
    public void Hinh_dang_1_nhan_va_so_cung_dong(string dong)
    {
        var ra = EgressRedactor.Redact(dong);

        Assert.Equal(1, ra.RedactedCount);
        Assert.Contains(EgressRedactor.Marker, ra.Text);
    }

    /// <summary>
    /// Chữ "khẩu" phải bắt được. Bản đầu của luật thiếu đúng chữ 'ẩ' và vì thế trượt
    /// một credential thật — giữ test này để không ai "dọn" bộ ký tự tiếng Việt đó.
    /// </summary>
    [Fact]
    public void Chu_khau_co_dau_van_bat_duoc()
    {
        Assert.Equal(1, EgressRedactor.Redact("Mật khẩu: 24680").RedactedCount);
    }

    /// <summary>Hình dạng 2: tên công cụ remote rồi NHIỀU giá trị trên cùng dòng.</summary>
    [Fact]
    public void Hinh_dang_2_cong_cu_va_nhieu_so_cung_dong()
    {
        var ra = EgressRedactor.Redact("Ultraview 12 345 678 // 4321");

        Assert.Equal(2, ra.RedactedCount);
        Assert.DoesNotContain("12 345 678", ra.Text);
        Assert.DoesNotContain("4321", ra.Text);
    }

    /// <summary>
    /// Hình dạng 3 — hình dạng làm luật "cùng dòng" chỉ bắt được 11%: khách gõ ID ở một
    /// tin nhắn rồi mật khẩu ở tin nhắn kế tiếp, và dòng thứ hai KHÔNG có từ khoá nào.
    /// </summary>
    [Fact]
    public void Hinh_dang_3_so_tran_o_dong_ke_tiep()
    {
        var doan = string.Join('\n',
            "anh cho em ultraview vào máy kiểm tra ạ",
            "11 222 333",
            "8642");

        var ra = EgressRedactor.Redact(doan);

        Assert.True(ra.RedactedCount >= 2, $"chỉ che được {ra.RedactedCount} chỗ");
        Assert.DoesNotContain("11 222 333", ra.Text);
        Assert.DoesNotContain("8642", ra.Text);
    }

    /// <summary>
    /// Hình dạng 4: key JSON — và key KHÔNG phải password. Bản đầu chỉ bắt password nên
    /// để lọt cả hai nửa của một cặp đăng nhập. Nửa nào bị rò cũng là rò.
    /// </summary>
    [Theory]
    [InlineData("{\"Account\": \"hoadon_gia\"}")]
    [InlineData("{\"username\": \"nguoidung_gia\"}")]
    [InlineData("{\"token\": \"eyJhbGciOiJIUzI1NiJ9giavaykhongthat\"}")]
    [InlineData("{\"api_key\": \"sk-gia-khong-that-0000\"}")]
    [InlineData("{\"acpass\": \"matkhaugia\"}")]
    public void Hinh_dang_4_key_JSON(string json)
    {
        var ra = EgressRedactor.Redact(json);

        Assert.True(ra.RedactedCount >= 1);
        Assert.Contains(EgressRedactor.Marker, ra.Text);
    }

    // ── Chỗ phải KHÔNG ăn nhầm ────────────────────────────────────────────────────

    /// <summary>
    /// False positive ĐÃ ĐO THẬT trên corpus 12 tháng: `id=2469` trong tham số URL bị bắt
    /// vì nhãn `ID`. Che nó đi thì mất đúng đường dẫn người duyệt cần để mở lại màn hình
    /// đang lỗi — tức luật che làm hỏng chính thứ nó phải bảo vệ.
    /// </summary>
    [Fact]
    public void URL_co_tham_so_id_KHONG_bi_che()
    {
        const string dong = "mở https://pms.example.net/invoice?table_id=23&id=2469&area=4 để xem";

        var ra = EgressRedactor.Redact(dong);

        Assert.Equal(0, ra.RedactedCount);
        Assert.Equal(dong, ra.Text);
    }

    /// <summary>
    /// Mã đặt phòng và mã số thuế là dữ liệu CẦN GIỮ — bước kiểm dựa vào chúng. Chúng chỉ
    /// bị che khi đứng cạnh một nhãn hoặc một tên công cụ remote.
    /// </summary>
    [Theory]
    [InlineData("xuất hoá đơn cho đặt phòng 80771 không được")]
    [InlineData("mã số thuế 0304746657 của khách")]
    [InlineData("Mã Pms : 16785, Mã Booking 2165")]
    public void Du_lieu_nghiep_vu_KHONG_bi_che(string dong)
    {
        Assert.Equal(0, EgressRedactor.Redact(dong).RedactedCount);
    }

    // ── Ba tính chất của cổng ─────────────────────────────────────────────────────

    /// <summary>Giữ DẤU (`G6`/`AP3`): che chứ không xoá lặng lẽ, và phần còn lại nguyên vẹn.</summary>
    [Fact]
    public void Che_thi_giu_DAU_va_giu_phan_con_lai_cua_mau()
    {
        var ra = EgressRedactor.Redact("anh cho em ultraview với, Mật khẩu: 13579 nhé ạ");

        Assert.Contains(EgressRedactor.Marker, ra.Text);
        Assert.Contains("anh cho em ultraview với", ra.Text);
        Assert.Contains("nhé ạ", ra.Text);
        Assert.DoesNotContain("13579", ra.Text);
    }

    /// <summary>Đếm được, và đếm THEO HÌNH DẠNG — luật che là cận trên, nên nó phải ra số.</summary>
    [Fact]
    public void Dem_duoc_theo_tung_hinh_dang()
    {
        var doan = string.Join('\n',
            "Mật khẩu: 11111",
            "{\"account\": \"taikhoangia\"}");

        var ra = EgressRedactor.Redact(doan);

        Assert.Equal(2, ra.RedactedCount);
        Assert.Equal(1, ra.Shapes["nhãn+số"]);
        Assert.Equal(1, ra.Shapes["key JSON"]);
        Assert.True(ra.AnythingRedacted);
    }

    /// <summary>
    /// FAIL CLOSED: một văn bản CHƯA che thì `EnsureSafeToSend` phải ném. Đây là phép kiểm
    /// canh chính cái cổng — nếu nó xanh với dữ liệu chưa che thì cả `AR-o` là trang trí.
    /// </summary>
    [Fact]
    public void Chua_che_thi_TU_CHOI_GUI()
    {
        var ex = Assert.Throws<EgressBlockedException>(
            () => EgressRedactor.EnsureSafeToSend("ID của bạn: 246813579"));

        Assert.Contains("KHÔNG GỬI", ex.Message);
        Assert.Contains("AR-o", ex.Message);
    }

    /// <summary>
    /// Thông điệp lỗi KHÔNG được mang giá trị bí mật. Log đi ra khỏi máy dễ hơn payload,
    /// nên một cổng che mà rò qua thông điệp lỗi thì rò ở đúng chỗ không ai canh.
    /// </summary>
    [Fact]
    public void Thong_diep_loi_KHONG_mang_gia_tri()
    {
        const string biMat = "192837465";

        var ex = Assert.Throws<EgressBlockedException>(
            () => EgressRedactor.EnsureSafeToSend($"ID của bạn: {biMat}"));

        Assert.DoesNotContain(biMat, ex.Message);
        Assert.Contains("nhãn+số", ex.Message);
    }

    /// <summary>Che xong thì phải TỰ QUÉT LẠI sạch — đó là điều kiện để `Redact` không ném.</summary>
    [Fact]
    public void Che_xong_quet_lai_thi_sach()
    {
        var doan = string.Join('\n',
            "cho em ultraview: 22 333 444",
            "1234",
            "{\"password\": \"matkhaugia\"}",
            "Mật khẩu: 55555");

        var ra = EgressRedactor.Redact(doan);

        EgressRedactor.EnsureSafeToSend(ra.Text);   // không ném
        Assert.True(ra.RedactedCount >= 4);
    }

    /// <summary>Không có gì đáng che thì không đổi một ký tự nào.</summary>
    [Fact]
    public void Khong_co_gi_thi_khong_doi_gi()
    {
        const string doan = "Anh chọn giúp em ký hiệu hoá đơn rồi phát hành lại nhé ạ";

        var ra = EgressRedactor.Redact(doan);

        Assert.Equal(doan, ra.Text);
        Assert.False(ra.AnythingRedacted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rong_hoac_null_thi_khong_no(string? doan)
    {
        var ra = EgressRedactor.Redact(doan);

        Assert.Equal(0, ra.RedactedCount);
        EgressRedactor.EnsureSafeToSend(ra.Text);
    }

    /// <summary>
    /// Vị trí che phải đúng kể cả khi văn bản có URL ở TRƯỚC chỗ bí mật. Đây là chỗ bản
    /// Python không cần lo và bản này thì có: nó thay URL bằng khoảng trắng CÙNG ĐỘ DÀI để
    /// mọi offset sau đó không lệch. Thay bằng `<URL>` như bản Python là lệch âm thầm.
    /// </summary>
    [Fact]
    public void URL_dai_o_truoc_khong_lam_lech_vi_tri_che()
    {
        var doan = "xem https://pms.example.net/a/very/long/path?x=1&y=2&z=3 rồi Mật khẩu: 97531";

        var ra = EgressRedactor.Redact(doan);

        Assert.Equal(1, ra.RedactedCount);
        Assert.DoesNotContain("97531", ra.Text);
        Assert.Contains("https://pms.example.net/a/very/long/path?x=1&y=2&z=3", ra.Text);
        Assert.Contains("rồi Mật khẩu: " + EgressRedactor.Marker, ra.Text);
    }
}
