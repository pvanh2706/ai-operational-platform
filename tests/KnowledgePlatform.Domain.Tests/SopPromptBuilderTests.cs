using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `AR-o` tại chỗ nối: KHÔNG có cách nào dựng payload mà đi vòng qua cổng che.
///
/// Vì sao bộ test này quan trọng hơn bộ test của chính cổng che: cổng che có thể hoàn hảo
/// mà vẫn vô dụng nếu một đường gọi mới đi vòng qua nó. Đây là loại lỗ `AR-d`/`IM-22` đã
/// dạy hai lần — cơ chế canh gác báo xanh trong khi dữ liệu đi qua cửa khác.
/// </summary>
public sealed class SopPromptBuilderTests
{
    private static SopDraftRequest Yeu_cau(params string[] noiDungEvidence) => new()
    {
        GroupName = "Nhóm thử",
        Cases =
        [
            new CaseForDrafting("ES-100001", "không xuất được hoá đơn",
                noiDungEvidence
                    .Select((c, i) => new EvidenceForDrafting(
                        $"ES-100001#comment-{i}", c, DateTimeOffset.UnixEpoch.AddMinutes(i)))
                    .ToList())
        ]
    };

    /// <summary>Payload dựng ra phải qua được cổng che — tức không còn bí mật nào.</summary>
    [Fact]
    public void Payload_dung_ra_luon_qua_duoc_cong_che()
    {
        var prompt = SopPromptBuilder.Build(Yeu_cau(
            "anh cho em ultraview vào máy nhé",
            "11 222 333",
            "8642",
            "{\"account\": \"taikhoangia\"}"));

        EgressRedactor.EnsureSafeToSend(prompt.UserText);   // không ném
        Assert.True(prompt.RedactedCount >= 3);
        Assert.Contains(EgressRedactor.Marker, prompt.UserText);
    }

    /// <summary>Giá trị bí mật KHÔNG được có mặt trong payload, dù ở bất kỳ dạng nào.</summary>
    [Fact]
    public void Gia_tri_bi_mat_khong_con_trong_payload()
    {
        var prompt = SopPromptBuilder.Build(Yeu_cau("Mật khẩu: 13579", "ID của bạn: 246813579"));

        Assert.DoesNotContain("13579", prompt.UserText);
        Assert.DoesNotContain("246813579", prompt.UserText);
    }

    /// <summary>
    /// Bí mật trong TIÊU ĐỀ case cũng phải bị che. Chỗ này dễ quên vì tiêu đề trông vô hại
    /// — nhưng corpus thật có tiêu đề mang mã khách sạn và tên khách.
    /// </summary>
    [Fact]
    public void Bi_mat_trong_tieu_de_cung_bi_che()
    {
        var yeu_cau = new SopDraftRequest
        {
            GroupName = "Nhóm thử",
            Cases =
            [
                new CaseForDrafting("ES-100002", "hỗ trợ remote, Mật khẩu: 24680",
                    [new EvidenceForDrafting("ES-100002#description", "nội dung bình thường",
                        DateTimeOffset.UnixEpoch)])
            ]
        };

        var prompt = SopPromptBuilder.Build(yeu_cau);

        Assert.DoesNotContain("24680", prompt.UserText);
        Assert.Equal(1, prompt.RedactedCount);
    }

    /// <summary>
    /// 🛑 Phép kiểm quan trọng nhất của file này: GHÉP nhiều mẩu lại có thể tạo ra hình
    /// dạng MỚI mà che-từng-mẩu không thấy. Hình dạng 3 nhìn tới trước 6 dòng, nên một dãy
    /// số trần ở đầu mẩu sau có thể nằm trong cửa sổ của một nhãn ở cuối mẩu trước.
    /// Đó là lý do `Build` quét lại TOÀN BỘ payload chứ không chỉ tin vào từng lần che lẻ.
    /// </summary>
    [Fact]
    public void Ghep_nhieu_mau_khong_tao_ra_lo_moi()
    {
        // Mẩu 1 kết thúc bằng từ khoá, mẩu 2 bắt đầu bằng dãy số trần. Tách riêng thì mẩu 2
        // vô hại; ghép lại thì nó là nửa sau của một cặp đăng nhập.
        var prompt = SopPromptBuilder.Build(Yeu_cau(
            "anh gửi em ultraview với",
            "77 888 999"));

        EgressRedactor.EnsureSafeToSend(prompt.UserText);
        Assert.DoesNotContain("77 888 999", prompt.UserText);
    }

    /// <summary>Không có gì đáng che thì đếm 0 — con số này đi vào bản ghi mỗi lượt gọi.</summary>
    [Fact]
    public void Khong_co_bi_mat_thi_dem_0()
    {
        var prompt = SopPromptBuilder.Build(Yeu_cau("Anh chọn giúp em ký hiệu hoá đơn nhé ạ"));

        Assert.Equal(0, prompt.RedactedCount);
        Assert.False(prompt.AnythingRedacted);
    }

    /// <summary>
    /// Payload phải liệt kê mã ticket được phép dùng. `G6`/`AP3`: model không được bịa
    /// nguồn, và cách rẻ nhất để nó không bịa là nói trước danh sách hợp lệ.
    /// </summary>
    [Fact]
    public void Payload_liet_ke_ma_ticket_duoc_phep()
    {
        var prompt = SopPromptBuilder.Build(Yeu_cau("nội dung"));

        Assert.Contains("KHÔNG dùng mã nào khác", prompt.UserText);
        Assert.Contains("ES-100001", prompt.UserText);
    }

    /// <summary>Evidence được xếp theo THỜI GIAN, vì bước kiểm là một trình tự.</summary>
    [Fact]
    public void Evidence_xep_theo_thoi_gian()
    {
        var yeu_cau = new SopDraftRequest
        {
            GroupName = "Nhóm thử",
            Cases =
            [
                new CaseForDrafting("ES-100003", "tiêu đề",
                [
                    new EvidenceForDrafting("muon", "MẨU SAU", DateTimeOffset.UnixEpoch.AddHours(2)),
                    new EvidenceForDrafting("som", "MẨU TRƯỚC", DateTimeOffset.UnixEpoch)
                ])
            ]
        };

        var text = SopPromptBuilder.Build(yeu_cau).UserText;

        Assert.True(text.IndexOf("MẨU TRƯỚC", StringComparison.Ordinal)
                    < text.IndexOf("MẨU SAU", StringComparison.Ordinal));
    }

    /// <summary>Không có case nào thì từ chối ngay, không gọi gì.</summary>
    [Fact]
    public void Khong_co_case_thi_tu_choi()
    {
        var yeu_cau = new SopDraftRequest { GroupName = "rỗng", Cases = [] };

        Assert.Throws<ArgumentException>(() => SopPromptBuilder.Build(yeu_cau));
    }

    /// <summary>
    /// `AllowedCaseKeys` là thứ eval dùng để kiểm model không bịa mã ticket. Nó phải suy
    /// từ chính danh sách case, không phải một trường nhập tay có thể lệch.
    /// </summary>
    [Fact]
    public void Danh_sach_ma_hop_le_suy_tu_case_khong_nhap_tay()
    {
        var yeu_cau = new SopDraftRequest
        {
            GroupName = "Nhóm thử",
            Cases =
            [
                new CaseForDrafting("ES-1", "a", []),
                new CaseForDrafting("ES-2", "b", [])
            ]
        };

        Assert.Equal(["ES-1", "ES-2"], yeu_cau.AllowedCaseKeys.OrderBy(k => k));
    }

    /// <summary>Chỉ dẫn phải mang đúng sáu luật — mỗi luật đến từ một chỗ đã đo.</summary>
    [Fact]
    public void Chi_dan_mang_du_sau_luat()
    {
        var s = SopPromptBuilder.SystemText;

        Assert.Contains("KHÔNG BAO GIỜ bịa mã ticket", s);
        Assert.Contains("evidence-noi-ro", s);
        Assert.Contains("toi-suy-ra", s);
        Assert.Contains("quyenCan", s);
        Assert.Contains("QUAN SÁT ĐƯỢC", s);
        Assert.Contains("bangTraMaLoi", s);
        Assert.Contains(EgressRedactor.Marker, s);   // model phải biết dấu che nghĩa là gì
    }
}
