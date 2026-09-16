using KnowledgePlatform.Domain.Redaction;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `AR-n` nhánh B2a — che dữ liệu BÊN THỨ BA ở khâu nạp.
///
/// 🛑 **MỌI GIÁ TRỊ TRONG FILE NÀY LÀ GIẢ.** Đừng ai chép giá trị thật từ corpus vào đây —
/// ngày 2026-09-11 đã có bốn credential thật bị dán vào đúng loại file này, và
/// `scripts/quet_credential_repo.py` ra đời vì chuyện đó. Hình dạng thì lấy từ payload thật
/// (`ES-346481#comment-802977`); nội dung thì bịa.
/// </summary>
public sealed class ThirdPartyRedactorTests
{
    /// <summary>Hình dạng thật của payload hoá đơn, giá trị bịa hoàn toàn.</summary>
    private const string Payload = """
        <DSHDon>
          <HDon>
            <DLHDon>
              <TTChung>
                <NLap>2026-09-01</NLap>
                <DVTTe>VND</DVTTe>
                <HTTToan>TM, CK</HTTToan>
              </TTChung>
              <NDHDon>
                <NMua>
                  <Ten>Cong ty Trach nhiem Huu han Mot Thanh Vien ABC</Ten>
                  <MST>0100000001</MST>
                  <DChi>12 Duong Khong Co That, Phuong Ao, Tinh Bia</DChi>
                  <SDThoai>
                  </SDThoai>
                  <STKNHang />
                  <CCCDan />
                </NMua>
                <DSHHDVu>
                  <HHDVu>
                    <STT>1</STT>
                    <THHDVu>Tien phong ngay 01/09</THHDVu>
                    <SLuong>2</SLuong>
                    <DGia>1500000</DGia>
                    <ThTien>3000000</ThTien>
                    <TSuat>8%</TSuat>
                  </HHDVu>
                </DSHHDVu>
                <TToan>
                  <TgTCThue>3000000</TgTCThue>
                  <TgTThue>240000</TgTThue>
                  <TgTTTBSo>3240000</TgTTTBSo>
                </TToan>
              </NDHDon>
            </DLHDon>
          </HDon>
        </DSHDon>
        """;

    [Fact]
    public void Che_moi_gia_tri_dinh_danh_trong_khoi_ben_mua()
    {
        var kq = ThirdPartyRedactor.Redact(Payload);

        Assert.Equal(3, kq.RedactedCount);
        Assert.DoesNotContain("Cong ty Trach nhiem Huu han Mot Thanh Vien ABC", kq.Text);
        Assert.DoesNotContain("0100000001", kq.Text);
        Assert.DoesNotContain("Duong Khong Co That", kq.Text);
        Assert.Contains(ThirdPartyRedactor.Marker, kq.Text);
    }

    /// <summary>
    /// 🛑 **PHÉP THỬ QUAN TRỌNG NHẤT CỦA FILE NÀY.** Số tiền nằm NGOÀI khối bên mua và
    /// KHÔNG được đụng tới.
    ///
    /// Nhóm SOP dày nhất trong 19 nhóm là *"Lệch cấu hình tiền/thuế"* (8 case, 63 mẩu), và nó
    /// chẩn đoán bằng cách mở cùng một hoá đơn ở ba nơi rồi **so từng cột tiền**. Che số tiền
    /// cho "an toàn" là vứt đúng bằng chứng của nhóm dày nhất — lần thứ BA một bộ lọc nghe rất
    /// hợp lý vứt đúng phần có giá trị, sau *"chỉ lấy case đã đóng"* và *"chỉ lấy phần khách
    /// đọc được"*.
    /// </summary>
    [Fact]
    public void KHONG_dung_toi_so_tien_va_dong_hang_ngoai_khoi_ben_mua()
    {
        var kq = ThirdPartyRedactor.Redact(Payload);

        Assert.Contains("<DGia>1500000</DGia>", kq.Text);
        Assert.Contains("<ThTien>3000000</ThTien>", kq.Text);
        Assert.Contains("<TgTTTBSo>3240000</TgTTTBSo>", kq.Text);
        Assert.Contains("<THHDVu>Tien phong ngay 01/09</THHDVu>", kq.Text);
        Assert.Contains("<TSuat>8%</TSuat>", kq.Text);
    }

    /// <summary>
    /// Thẻ RỖNG để yên. Che một thẻ vốn đã rỗng là biến *"không có"* thành *"có nhưng bị
    /// giấu"* — và chính sự rỗng đó là thứ nhóm 2 (*NCC từ chối payload*) cần nhìn thấy để
    /// chẩn đoán: nhà cung cấp từ chối vì trường nào THIẾU.
    /// </summary>
    [Fact]
    public void The_rong_KHONG_bi_che()
    {
        var kq = ThirdPartyRedactor.Redact(Payload);

        Assert.Contains("<STKNHang />", kq.Text);
        Assert.Contains("<CCCDan />", kq.Text);
        Assert.DoesNotContain($"<STKNHang>{ThirdPartyRedactor.Marker}", kq.Text);

        // <SDThoai> chỉ chứa khoảng trắng — cũng là rỗng.
        Assert.DoesNotContain($"<SDThoai>{ThirdPartyRedactor.Marker}", kq.Text);
    }

    /// <summary>
    /// 🛑 Chuỗi `MST` trần KHÔNG bị đụng. Đo trên fixture: nó xuất hiện ở **10/345** mẩu, và
    /// phần lớn là **MST của chính khách sạn** — một định danh CÔNG KHAI mà `make_fixture.py`
    /// cố ý giữ lại. Một luật bắt theo chữ "MST" sẽ che mất đúng dữ liệu cần giữ; đó là lý do
    /// luật này chỉ nhìn VỊ TRÍ trong cây XML.
    /// </summary>
    [Fact]
    public void MST_tran_ngoai_khoi_ben_mua_KHONG_bi_dung()
    {
        const string van = "Khach bao hoa don cua khach san MST 0100000009 khong phat hanh duoc";

        var kq = ThirdPartyRedactor.Redact(van);

        Assert.Equal(0, kq.RedactedCount);
        Assert.Equal(van, kq.Text);
    }

    [Fact]
    public void Khong_co_khoi_ben_mua_thi_giu_nguyen_van()
    {
        const string van = "Da kiem Nhat ky ket noi, thay ma loi E0042";

        var kq = ThirdPartyRedactor.Redact(van);

        Assert.Equal(0, kq.RedactedCount);
        Assert.Equal(van, kq.Text);
        Assert.False(kq.AnythingRedacted);
    }

    /// <summary>
    /// Bản xuất của nhà cung cấp không đảm bảo hoa thường, và một khối lọt lưới vì viết
    /// <c>&lt;nmua&gt;</c> là một pháp nhân bị nêu tên.
    /// </summary>
    [Fact]
    public void Khop_khong_phan_biet_hoa_thuong()
    {
        const string van = "<nmua><ten>Cong ty Gia Dinh</ten></nmua>";

        var kq = ThirdPartyRedactor.Redact(van);

        Assert.Equal(1, kq.RedactedCount);
        Assert.DoesNotContain("Cong ty Gia Dinh", kq.Text);
    }

    /// <summary>
    /// Che hai lần thì lần hai KHÔNG đếm thêm và KHÔNG lồng dấu che. Đường nạp idempotent nên
    /// cùng một mẩu đi qua nhiều lần là chuyện bình thường; nếu mỗi lượt lại che chồng thì con
    /// số báo ra phồng lên và dấu che thành `[ĐÃ CHE: [ĐÃ CHE: ...]]`.
    /// </summary>
    [Fact]
    public void Che_lan_hai_khong_dem_them_va_khong_long_dau()
    {
        var lan1 = ThirdPartyRedactor.Redact(Payload);
        var lan2 = ThirdPartyRedactor.Redact(lan1.Text);

        Assert.Equal(0, lan2.RedactedCount);
        Assert.Equal(lan1.Text, lan2.Text);
        Assert.DoesNotContain($"{ThirdPartyRedactor.Marker}{ThirdPartyRedactor.Marker}", lan2.Text);
    }

    /// <summary>Đếm được theo từng thẻ — để ĐO, không để log giá trị.</summary>
    [Fact]
    public void Bao_ra_the_nao_bat_duoc_bao_nhieu_cho()
    {
        var kq = ThirdPartyRedactor.Redact(Payload);

        Assert.Equal(1, kq.Shapes["NMua/Ten"]);
        Assert.Equal(1, kq.Shapes["NMua/MST"]);
        Assert.Equal(1, kq.Shapes["NMua/DChi"]);
        Assert.False(kq.Shapes.ContainsKey("NMua/STKNHang"));
    }

    /// <summary>
    /// Dấu che của cổng này KHÁC dấu của <see cref="EgressRedactor"/>, có chủ đích: hai cổng
    /// che vì hai lý do khác nhau, và người đọc bản nháp cần phân biệt được "đây là bí mật kỹ
    /// thuật" với "đây là người ngoài không liên quan".
    /// </summary>
    [Fact]
    public void Dau_che_khac_dau_cua_cong_gui_ra()
    {
        Assert.NotEqual(EgressRedactor.Marker, ThirdPartyRedactor.Marker);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rong_thi_khong_no(string? van)
    {
        var kq = ThirdPartyRedactor.Redact(van);

        Assert.Equal(0, kq.RedactedCount);
        Assert.Equal(string.Empty, kq.Text);
    }

    /// <summary>
    /// 🛑 **PHÉP THỬ NÀY RA ĐỜI TỪ MỘT LỖI MÀ CHẠY THẬT MỚI BẮT ĐƯỢC (2026-09-16).**
    ///
    /// Bản đầu dùng <c>string.IsNullOrWhiteSpace</c> để nhận ra thẻ rỗng, và mọi test đều
    /// xanh — vì test viết thẻ rỗng bằng **xuống dòng thật**. Nhưng bản xuất Jira mang xuống
    /// dòng ở dạng **chuỗi thoát nguyên văn**: thẻ rỗng thật trong corpus trông như
    /// <c>&lt;SDThoai&gt;\\r\\n   &lt;/SDThoai&gt;</c> với <c>\\r\\n</c> là BỐN ký tự
    /// (gạch chéo, r, gạch chéo, n). <c>IsNullOrWhiteSpace</c> thấy có gạch chéo và chữ nên
    /// nói "không rỗng" → một thẻ vốn trống bị dán dấu che.
    ///
    /// Nạp `kp_dev` xong mở ra xem thì thấy <c>&lt;SDThoai&gt;</c>, <c>&lt;DCTDTu&gt;</c> và
    /// <c>&lt;HVTNMHang&gt;</c> đều mang dấu che dù cả ba vốn trống.
    ///
    /// → Cùng một hình dạng lỗi với `IM-24` (`+07:00` làm endpoint trả 500, mà 103 test cũ
    ///   không thấy vì tay người viết test luôn viết UTC). **Test tự dựng vật liệu cho mình
    ///   chỉ kiểm được những hình dạng người viết NGHĨ RA.**
    /// </summary>
    [Fact]
    public void The_rong_kieu_CORPUS_THAT_khong_bi_che()
    {
        // Xuống dòng ở dạng chuỗi thoát nguyên văn — đúng như nó nằm trong bản xuất Jira.
        const string thoat = "\\r" + "\\n";
        var van = "<NMua><Ten>Cong ty Bia</Ten><SDThoai>" + thoat + "   </SDThoai></NMua>";

        var kq = ThirdPartyRedactor.Redact(van);

        Assert.Equal(1, kq.RedactedCount);
        Assert.False(kq.Shapes.ContainsKey("NMua/SDThoai"));
        Assert.Contains("<SDThoai>" + thoat + "   </SDThoai>", kq.Text);
    }
}
