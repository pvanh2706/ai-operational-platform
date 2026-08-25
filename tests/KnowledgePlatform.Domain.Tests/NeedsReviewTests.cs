using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `V3` — NEEDS_REVIEW được KÍCH HOẠT, không phải ai đó tự chọn.
///
/// Câu đó là một lời hứa với người dùng: "một tri thức có assertion vừa bị bác bỏ
/// sẽ KHÔNG nằm im ở Active cho tới khi có người để ý". Lời hứa đó nằm trọn trong
/// một biểu thức bool duy nhất, không có SQL, không có gì để nhìn từ bên ngoài —
/// nếu nó sai thì hệ thống không crash, không báo, chỉ im lặng KHÔNG gắn cờ. Đó là
/// đúng loại hỏng mà `AGENT.md` gọi là nguy hiểm nhất: hỏng mà vẫn trông như chạy.
///
/// ⚠ Slice này mới có 3/5 trigger của `V3`. Hai trigger còn lại (quan hệ
/// CONTRADICTS qua `L4`, và nguồn chống lưng bị đổi/xoá) chưa có vật liệu để kiểm.
/// Không test giả chúng ở đây — xem <see cref="Chua_du_nam_trigger_cua_V3"/>.
/// </summary>
public sealed class NeedsReviewTests
{
    // =========================================================================
    //  Điều kiện cổng: chỉ Active mới gắn cờ được
    // =========================================================================

    /// <summary>
    /// Draft chưa ai duyệt, nên chưa có gì để "xem lại". Gắn cờ NEEDS_REVIEW lên
    /// một bản nháp là làm nhiễu đúng cái hàng đợi mà người duyệt cần sạch.
    /// </summary>
    [Fact]
    public void Draft_khong_gan_co_du_co_assertion_bi_bac_bo()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Invalidated);

        Assert.False(record.NeedsReview);
        Assert.Equal("DRAFT", record.DisplayState);
    }

    /// <summary>
    /// Đã rút khỏi lưu hành thì không gọi người duyệt quay lại nữa. Ghim ca này vì
    /// nó là hệ quả dễ tuột nhất khi ai đó sửa biểu thức: bỏ điều kiện Lifecycle
    /// sẽ làm mọi record Deprecated có assertion cũ bỗng nhảy vào hàng đợi duyệt.
    /// </summary>
    [Fact]
    public void Rut_tri_thuc_thi_thoi_gan_co()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Invalidated);
        record.Approve("an.pham", "team:ops");
        Assert.True(record.NeedsReview);

        record.Deprecate();

        Assert.False(record.NeedsReview);
        Assert.Equal("DEPRECATED", record.DisplayState);
    }

    // =========================================================================
    //  Trigger 1 + 2 — assertion nằm NGOÀI thang xác minh
    // =========================================================================

    /// <summary>
    /// `V4` ca (b): từng tin, nay bị bác. Tri thức đang Active mà có phát biểu bị
    /// bác bỏ thì phải gọi người xem lại — đây là ca trung tâm của `V3`.
    /// </summary>
    [Fact]
    public void Assertion_bi_bac_bo_kich_hoat_xem_lai()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Invalidated);
        record.Approve("an.pham", "team:ops");

        Assert.True(record.NeedsReview);
    }

    /// <summary>
    /// `S8` làm CONFLICTING thành giá trị BẮT BUỘC: bản nháp gom từ N case luôn có
    /// chỗ các case không đồng ý, và chính chỗ đó là chỗ người duyệt cần nhìn.
    /// Nếu bằng chứng chỉ hai hướng mà hệ thống không gắn cờ, thì `S8` mất tác dụng
    /// ngay tại điểm nó được thiết kế để phát huy.
    /// </summary>
    [Fact]
    public void Assertion_co_bang_chung_hai_huong_kich_hoat_xem_lai()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Conflicting);
        record.Approve("an.pham", "team:ops");

        Assert.True(record.NeedsReview);
    }

    /// <summary>
    /// Bốn mức TRÊN thang thì không kích hoạt gì — kể cả Speculative, mức thấp nhất.
    /// Đây là ranh giới thật của `V1`: "thấp" khác "ngoài thang". Một phát biểu mới
    /// suy đoán vẫn là phát biểu bình thường; một phát biểu bị bác bỏ thì không.
    /// </summary>
    [Theory]
    [InlineData(VerificationLevel.Speculative)]
    [InlineData(VerificationLevel.Plausible)]
    [InlineData(VerificationLevel.Supported)]
    [InlineData(VerificationLevel.Verified)]
    public void Assertion_tren_thang_khong_kich_hoat_xem_lai(VerificationLevel level)
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: level);
        record.Approve("an.pham", "team:ops");

        Assert.False(record.NeedsReview);
        Assert.Equal("ACTIVE", record.DisplayState);
    }

    // =========================================================================
    //  Trigger 3 — có nội dung mới hơn lần duyệt gần nhất
    // =========================================================================

    /// <summary>
    /// Nội dung thêm vào SAU khi duyệt thì chưa ai duyệt nó. Không gắn cờ ở đây
    /// nghĩa là tri thức đang phát cho người dùng có phần chưa qua mắt người nào —
    /// trái `D4` (người công nhận) một cách âm thầm.
    /// </summary>
    [Fact]
    public void Assertion_them_sau_lan_duyet_kich_hoat_xem_lai()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion();
        record.Approve("an.pham", "team:ops");

        record.NewAssertion(
            kind: AssertionKind.Handling,
            createdAt: record.LastApproval!.ApprovedAt.AddMinutes(1));

        Assert.True(record.NeedsReview);
    }

    /// <summary>
    /// Và duyệt lại thì cờ tắt. Không có test này thì "gắn cờ được" chưa chứng minh
    /// được gì cả — một biểu thức luôn trả true cũng qua được mọi test phía trên.
    /// </summary>
    [Fact]
    public void Duyet_lai_thi_tat_co()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion();
        record.Approve("an.pham", "team:ops");

        var mocDuyetDau = record.LastApproval!.ApprovedAt;
        record.NewAssertion(kind: AssertionKind.Handling, createdAt: mocDuyetDau.AddTicks(1));
        Assert.True(record.NeedsReview);

        // Approve() đóng dấu bằng DateTimeOffset.UtcNow ở trong domain, test không
        // chọn hộ được. Nên phải để đồng hồ thật đi qua mốc trên rồi mới duyệt lại,
        // nếu không lần duyệt thứ hai có thể mang dấu thời gian CŨ HƠN assertion và
        // test thành ra kiểm nhầm chuyện khác.
        ChoDongHoNhichQua(mocDuyetDau.AddTicks(1));

        record.Approve("linh.tran", "team:ops", reason: "Đã đọc phần cách xử lý mới.");

        Assert.False(record.NeedsReview);
    }

    /// <summary>
    /// Quay cho tới khi đồng hồ hệ thống vượt mốc — không phải Sleep, nên không
    /// đoán độ phân giải của đồng hồ và không chớp tắt. Thường tốn dưới một mili giây.
    /// </summary>
    private static void ChoDongHoNhichQua(DateTimeOffset moc)
    {
        while (DateTimeOffset.UtcNow <= moc)
        {
            Thread.SpinWait(1);
        }
    }

    // =========================================================================
    //  Ranh giới: chỉ assertion CÒN HIỆU LỰC mới kích hoạt
    // =========================================================================

    /// <summary>
    /// `M2` giữ bản gốc thay vì ghi đè, nên trong record luôn có những assertion đã
    /// bị thay. Nếu chúng vẫn kích hoạt cờ thì mọi tri thức từng được người sửa một
    /// lần sẽ kẹt vĩnh viễn ở NEEDS_REVIEW — hàng đợi duyệt đầy rác và người dùng
    /// học được cách phớt lờ cái cờ. Đó là cách một tính năng đúng giết một tính
    /// năng khác.
    /// </summary>
    [Fact]
    public void Assertion_da_bi_thay_the_khong_con_kich_hoat_xem_lai()
    {
        var record = KnowledgeBuilder.NewRecord();
        var banGoc = record.NewAssertion(verification: VerificationLevel.Invalidated);
        var banSua = record.NewAssertion(
            verification: VerificationLevel.Verified,
            origin: Origin.UserConfirmed);

        banGoc.ReplacedByAssertionId = banSua.Id;
        record.Approve("an.pham", "team:ops");

        Assert.False(record.NeedsReview);
    }

    /// <summary>
    /// Record không có assertion nào thì không có gì kích hoạt. Ca rỗng, ghim để
    /// biểu thức không bao giờ được viết thành dạng ném khi danh sách rỗng.
    /// </summary>
    [Fact]
    public void Record_khong_co_assertion_thi_khong_gan_co()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.Approve("an.pham", "team:ops");

        Assert.False(record.NeedsReview);
    }

    /// <summary>
    /// MỘT assertion hỏng trong cả cụm là đủ để gọi người xem lại. `T4` gắn
    /// verification ở từng assertion chứ không phải một con số cho cả record, nên
    /// cờ phải là phép HOẶC trên cụm, không phải trung bình — lấy trung bình chính
    /// là cách làm chỗ tranh chấp biến mất khỏi mắt người duyệt mà `V1` cảnh báo.
    /// </summary>
    [Fact]
    public void Mot_assertion_hong_trong_ca_cum_la_du_de_gan_co()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(kind: AssertionKind.CauseExists, verification: VerificationLevel.Verified);
        record.NewAssertion(kind: AssertionKind.Recognition, verification: VerificationLevel.Verified);
        record.NewAssertion(kind: AssertionKind.Handling, verification: VerificationLevel.Invalidated);
        record.Approve("an.pham", "team:ops");

        Assert.True(record.NeedsReview);
    }

    // =========================================================================
    //  Điểm chưa xong — ghim để không ai tưởng là đã đủ
    // =========================================================================

    /// <summary>
    /// `V3` có NĂM trigger; slice này hiện thực BA. Hai cái còn lại cần thứ chưa
    /// tồn tại trong code: quan hệ CONTRADICTS (`L4` / KnowledgeRelation) và việc
    /// theo dõi nguồn chống lưng bị đổi/xoá.
    ///
    /// Test này không kiểm hành vi — nó ghim một KHOẢNG TRỐNG ĐÃ BIẾT vào bộ test,
    /// vì `docs/00_CURRENT_STATE.md` chép lại rằng mất tài liệu là failure mode đã
    /// xảy ra với dự án này. Comment thì bị bỏ qua, test thì được chạy và được đếm.
    /// Khi `L4` có mặt, chính test này phải đỏ và bắt người sửa quay lại đây.
    /// </summary>
    [Fact]
    public void Chua_du_nam_trigger_cua_V3()
    {
        var chuaCoKieuQuanHe = Type.GetType(
            "KnowledgePlatform.Domain.Knowledge.KnowledgeRelation, KnowledgePlatform.Domain") is null;

        Assert.True(chuaCoKieuQuanHe,
            "KnowledgeRelation (L4) đã xuất hiện. V3 có 5 trigger cho NEEDS_REVIEW và bộ test " +
            "này mới phủ 3 — trigger 'có quan hệ CONTRADICTS tới record khác' giờ đã có vật " +
            "liệu để kiểm. Viết nó, và cập nhật cả IsSuperseded (xem DisplayStateTests).");
    }
}
