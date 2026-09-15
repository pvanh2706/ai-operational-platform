using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `M2` — vế B: người sửa thì bản A phải đọc lại được nguyên vẹn.
///
/// `AssertionTests` đã kiểm rằng CƠ CHẾ (`ReplacedByAssertionId` / `IsCurrent`) làm được
/// việc đó. File này kiểm `KnowledgeRecord.ReviseAssertion` — cái hàm buộc mọi nơi gọi phải
/// dùng cơ chế ấy CHO ĐÚNG.
///
/// ⚠ Vì sao cần cả hai: trước hàm này, chỗ duy nhất thực hiện phép sửa trong cả repo là
/// chính `AssertionTests`, và nó **gán tay** `ReplacedByAssertionId`. Một cơ chế mà chỉ bộ
/// test biết dùng đúng thì mã sản phẩm sẽ dùng sai — bốn bất biến dưới đây độc lập với
/// nhau, nên quên một cái không làm hỏng ba cái kia, tức quên là quên IM LẶNG.
/// </summary>
public sealed class ReviseAssertionTests
{
    /// <summary>Bất biến 1 — bản gốc thôi hiệu lực, KHÔNG bị ghi đè. Vế A của diff phải còn.</summary>
    [Fact]
    public void Sua_thi_ban_goc_thoi_hieu_luc_chu_khong_bi_ghi_de()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(
            kind: AssertionKind.Recognition,
            origin: Origin.AiInference,
            verification: VerificationLevel.Speculative,
            content: "Nghi do parser bỏ qua payload.");

        var banB = record.ReviseAssertion(
            banA.Id,
            "Parser dưới 2.3 bỏ qua payload OTA dạng X.",
            Origin.HumanAssessment,
            VerificationLevel.Verified,
            "chị Lan (support)");

        Assert.False(banA.IsCurrent);
        Assert.Equal(banB.Id, banA.ReplacedByAssertionId);
        Assert.True(banB.IsCurrent);

        // Vế A còn nguyên từng trường — đây là thứ M2 đọc lại.
        Assert.Equal("Nghi do parser bỏ qua payload.", banA.Content);
        Assert.Equal(Origin.AiInference, banA.Origin);
        Assert.Equal(VerificationLevel.Speculative, banA.Verification);

        Assert.Equal(2, record.Assertions.Count);
    }

    /// <summary>
    /// Bất biến 2 — bản mới giữ NGUYÊN `Kind`. Sửa một "cách nhận ra" mà ra một "cách xử lý"
    /// thì `diff(A,B)` không còn so được cái gì với cái gì.
    /// </summary>
    [Fact]
    public void Ban_sua_giu_nguyen_Kind_cua_ban_goc()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(kind: AssertionKind.Handling, origin: Origin.AiInference);

        var banB = record.ReviseAssertion(
            banA.Id, "cách xử lý đã sửa", Origin.HumanAssessment, VerificationLevel.Supported);

        Assert.Equal(AssertionKind.Handling, banB.Kind);
    }

    /// <summary>
    /// Bất biến 3 — `AiInference` bị TỪ CHỐI. Hai bản cùng origin thì diff còn tồn tại nhưng
    /// mất nghĩa: không phân biệt được "AI viết rồi người sửa" với "người viết cả hai lần".
    /// </summary>
    [Fact]
    public void Ban_sua_mang_origin_AiInference_thi_bi_tu_choi()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);

        var ex = Assert.Throws<ArgumentException>(() => record.ReviseAssertion(
            banA.Id, "máy tự sửa mình", Origin.AiInference, VerificationLevel.Supported));

        Assert.Contains("M2", ex.Message);
        Assert.True(banA.IsCurrent);
        Assert.Single(record.Assertions);
    }

    /// <summary>
    /// Bất biến 4 — chỉ sửa được bản CÒN HIỆU LỰC. Sửa một bản đã bị thay là rẽ nhánh lịch
    /// sử, và `diff(A,B)` lúc đó không biết B là bản nào.
    /// </summary>
    [Fact]
    public void Sua_mot_ban_da_bi_thay_thi_bi_tu_choi()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);
        record.ReviseAssertion(banA.Id, "bản B", Origin.HumanAssessment, VerificationLevel.Supported);

        Assert.Throws<InvalidOperationException>(() => record.ReviseAssertion(
            banA.Id, "bản C rẽ nhánh", Origin.HumanAssessment, VerificationLevel.Supported));

        Assert.Equal(2, record.Assertions.Count);
    }

    /// <summary>Sửa NỐI TIẾP thì được — B sửa thành C là một chuỗi, không phải rẽ nhánh.</summary>
    [Fact]
    public void Sua_noi_tiep_ban_con_hieu_luc_thi_duoc()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);
        var banB = record.ReviseAssertion(banA.Id, "bản B", Origin.HumanAssessment, VerificationLevel.Supported);
        var banC = record.ReviseAssertion(banB.Id, "bản C", Origin.UserConfirmed, VerificationLevel.Verified);

        Assert.False(banA.IsCurrent);
        Assert.False(banB.IsCurrent);
        Assert.True(banC.IsCurrent);
        Assert.Equal(3, record.Assertions.Count);
    }

    [Fact]
    public void Sua_assertion_khong_thuoc_record_thi_bi_tu_choi()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(origin: Origin.AiInference);

        Assert.Throws<ArgumentException>(() => record.ReviseAssertion(
            Guid.CreateVersion7(), "nội dung", Origin.HumanAssessment, VerificationLevel.Supported));
    }

    [Fact]
    public void Noi_dung_moi_rong_thi_bi_tu_choi()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);

        Assert.Throws<ArgumentException>(() => record.ReviseAssertion(
            banA.Id, "   ", Origin.HumanAssessment, VerificationLevel.Supported));

        Assert.True(banA.IsCurrent);
    }

    /// <summary>
    /// 🛑 `V3` — sửa SAU KHI duyệt thì `NeedsReview` bật lên, và đó là ĐÚNG chứ không phải
    /// tác dụng phụ. Tri thức KHÔNG bị rút khỏi đường trả lời (V3: gắn cờ chứ không rút),
    /// nhưng ai hỏi cũng thấy ngay là nó đã đổi kể từ lần duyệt cuối.
    /// </summary>
    [Fact]
    public void Sua_sau_khi_duyet_thi_record_can_duyet_lai()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);

        record.Approve("chị Lan", VisibilityScope.NoiBo);
        Assert.Equal("ACTIVE", record.DisplayState);

        record.ReviseAssertion(banA.Id, "đã sửa", Origin.HumanAssessment, VerificationLevel.Verified);

        Assert.True(record.NeedsReview);
        Assert.Equal("NEEDS_REVIEW", record.DisplayState);

        // Vẫn ACTIVE ở tầng lưu trữ — nên nó KHÔNG biến mất khỏi đường trả lời.
        Assert.Equal(StoredLifecycleState.Active, record.Lifecycle);
    }

    /// <summary>Duyệt lại thì sạch cờ — và chính lần duyệt lại đó là vế "số nháp được duyệt" của `M2`.</summary>
    [Fact]
    public void Duyet_lai_sau_khi_sua_thi_sach_co()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);
        record.Approve("chị Lan", VisibilityScope.NoiBo);
        record.ReviseAssertion(banA.Id, "đã sửa", Origin.HumanAssessment, VerificationLevel.Verified);

        record.Approve("chị Lan", VisibilityScope.NoiBo, "duyệt lại sau khi sửa");

        Assert.False(record.NeedsReview);
        Assert.Equal("ACTIVE", record.DisplayState);
    }

    /// <summary>
    /// `T1` — `ReviseAssertion` KHÔNG đụng tới `CauseName`. Tên nguyên nhân là IDENTITY của
    /// record; đổi nó là đổi record đang nói về cái gì, không phải sửa một phát biểu.
    /// </summary>
    [Fact]
    public void Sua_assertion_KHONG_doi_ten_nguyen_nhan()
    {
        var record = KnowledgeBuilder.NewRecord();
        var truoc = record.CauseName;
        var banA = record.NewAssertion(kind: AssertionKind.CauseExists, origin: Origin.AiInference);

        record.ReviseAssertion(banA.Id, "phát biểu về nguyên nhân, đã sửa",
            Origin.HumanAssessment, VerificationLevel.Verified);

        Assert.Equal(truoc, record.CauseName);
    }

    /// <summary>Người sửa được ghi vào bản mới, để `M2` tra ngược được ai đã sửa gì.</summary>
    [Fact]
    public void Nguoi_sua_duoc_ghi_vao_ban_moi()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banA = record.NewAssertion(origin: Origin.AiInference);

        var banB = record.ReviseAssertion(
            banA.Id, "đã sửa", Origin.HumanAssessment, VerificationLevel.Verified, "chị Lan (support)");

        Assert.Equal("chị Lan (support)", banB.ActorLabel);
        Assert.Null(banA.ActorLabel);
    }
}
