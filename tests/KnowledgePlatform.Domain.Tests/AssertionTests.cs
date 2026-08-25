using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `M2` — giữ được CẢ bản AI và bản người sửa, và `AP3` — origin chỉ lưu ở MỘT mức.
///
/// `M2` không phải một tính năng cho vui: `diff(bản nháp AI, bản đã duyệt)` vừa là
/// thước đo tháng đầu, vừa là NHÃN cho bộ eval của flywheel `D6`. Ghi đè bản gốc là
/// phá cả hai cùng lúc, và phá theo cách không ai nhìn thấy — dữ liệu vẫn đẹp, chỉ
/// là thước đo trở thành bịa. Mất bản gốc thì không dựng lại được.
/// </summary>
public sealed class AssertionTests
{
    /// <summary>Assertion mới sinh ra là bản còn hiệu lực.</summary>
    [Fact]
    public void Assertion_moi_la_ban_con_hieu_luc()
    {
        var record = KnowledgeBuilder.NewRecord();
        var assertion = record.NewAssertion();

        Assert.True(assertion.IsCurrent);
        Assert.Null(assertion.ReplacedByAssertionId);
    }

    /// <summary>
    /// Người duyệt sửa: bản gốc KHÔNG bị ghi đè, nó chỉ thôi còn hiệu lực. Cả nội
    /// dung lẫn origin của bản gốc phải nguyên vẹn — đó chính là hai vế của
    /// `diff(A, B)` mà `M2` cần đọc lại về sau.
    /// </summary>
    [Fact]
    public void Nguoi_sua_thi_ban_goc_thoi_hieu_luc_chu_khong_bi_ghi_de()
    {
        var record = KnowledgeBuilder.NewRecord();

        var banNhapAi = record.NewAssertion(
            origin: Origin.AiInference,
            verification: VerificationLevel.Speculative,
            content: "Nghi do parser bỏ qua payload.");

        var banNguoiSua = record.NewAssertion(
            origin: Origin.UserConfirmed,
            verification: VerificationLevel.Verified,
            content: "Parser dưới 2.3 bỏ qua payload OTA dạng X, đã đối chiếu source.");

        banNhapAi.ReplacedByAssertionId = banNguoiSua.Id;

        Assert.False(banNhapAi.IsCurrent);
        Assert.True(banNguoiSua.IsCurrent);

        // Vế A của diff(A, B) vẫn đọc được nguyên vẹn.
        Assert.Equal(Origin.AiInference, banNhapAi.Origin);
        Assert.Equal("Nghi do parser bỏ qua payload.", banNhapAi.Content);
        Assert.Equal(VerificationLevel.Speculative, banNhapAi.Verification);

        // Và cả hai bản cùng tồn tại trong record — không bản nào bị gỡ đi.
        Assert.Equal(2, record.Assertions.Count);
    }

    /// <summary>
    /// `M2` chỉ đọc được nếu bản mới mang origin CỦA NGƯỜI SỬA, khác origin của bản
    /// AI. Hai bản cùng origin thì diff còn tồn tại nhưng mất nghĩa: không phân biệt
    /// được "AI viết rồi người sửa" với "người viết cả hai lần".
    /// </summary>
    [Fact]
    public void Ban_sua_mang_origin_cua_nguoi_sua_khong_phai_cua_AI()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banNhapAi = record.NewAssertion(origin: Origin.AiInference);
        var banNguoiSua = record.NewAssertion(origin: Origin.UserConfirmed);
        banNhapAi.ReplacedByAssertionId = banNguoiSua.Id;

        Assert.NotEqual(banNhapAi.Origin, banNguoiSua.Origin);
    }

    /// <summary>
    /// `AP3` / §6.9: "SUPERSEDES" là quan hệ Knowledge ↔ Knowledge của `L4`. Dùng lại
    /// đúng từ đó cho quan hệ Assertion ↔ Assertion sẽ tạo vocabulary song song —
    /// đúng cái bệnh Workstream 04 mất ba lần để chữa. Tên `ReplacedByAssertionId`
    /// được chọn CÓ CHỦ ĐÍCH để tránh nó.
    ///
    /// Test này canh cái tên. Nghe vụn vặt, nhưng §6.9 tái phát ba lần rồi và mỗi
    /// lần đều bắt đầu bằng việc ai đó dùng lại một từ nghe rất hợp.
    /// </summary>
    [Fact]
    public void Quan_he_giua_hai_assertion_khong_muon_tu_SUPERSEDES()
    {
        var tenNhamLan = typeof(Assertion).GetProperties()
            .Select(p => p.Name)
            .Where(name => name.Contains("Supersede", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(tenNhamLan.Length == 0,
            $"Assertion có thành viên mang từ SUPERSEDE: {string.Join(", ", tenNhamLan)}. " +
            "SUPERSEDES là quan hệ Knowledge ↔ Knowledge của L4. Dùng lại từ đó cho quan hệ " +
            "giữa hai assertion là tạo vocabulary song song (§6.9). Quan hệ ở mức assertion " +
            "tên là ReplacedByAssertionId.");
    }

    /// <summary>
    /// `L3`: mỗi liên kết bằng chứng mang quan hệ RIÊNG. Một dẫn chứng có thể CHỐNG
    /// LẠI chính phát biểu nó được gắn vào — đó không phải lỗi dữ liệu, đó là cách
    /// `S8` để chỗ tranh chấp hiện ra thay vì bị giấu đi.
    /// </summary>
    [Fact]
    public void Dan_chung_gan_vao_mot_phat_bieu_co_the_mang_quan_he_bac_bo()
    {
        var record = KnowledgeBuilder.NewRecord();
        var assertion = record.NewAssertion(verification: VerificationLevel.Conflicting);

        assertion.EvidenceLinks.Add(new AssertionEvidence
        {
            TenantId = record.TenantId,
            AssertionId = assertion.Id,
            EvidenceItemId = Guid.CreateVersion7(),
            Relation = EvidenceRelation.Support,
        });

        assertion.EvidenceLinks.Add(new AssertionEvidence
        {
            TenantId = record.TenantId,
            AssertionId = assertion.Id,
            EvidenceItemId = Guid.CreateVersion7(),
            Relation = EvidenceRelation.Refute,
            Note = "Case #4412 có đúng triệu chứng này nhưng parser đã là 2.5.",
        });

        Assert.Contains(assertion.EvidenceLinks, l => l.Relation == EvidenceRelation.Support);
        Assert.Contains(assertion.EvidenceLinks, l => l.Relation == EvidenceRelation.Refute);
    }
}
