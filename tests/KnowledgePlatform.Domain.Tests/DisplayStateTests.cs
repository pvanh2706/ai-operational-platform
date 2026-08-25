using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `V3` — nhãn hiển thị gộp state LƯU và state SUY RA, và nguyên tắc
/// "NEEDS_REVIEW gắn cờ chứ KHÔNG rút khỏi retrieval".
///
/// Vì sao vế thứ hai quan trọng đến mức phải có test riêng: rút một tri thức đang
/// tranh chấp khỏi kết quả tìm kiếm là phản xạ tự nhiên của người viết code
/// ("chưa chắc đúng thì đừng trả"). Nhưng §6.3 nói đó chính là chỗ sản phẩm bán
/// được, và `G4` nói bày chỗ tranh chấp ra mới là điều người duyệt cần. Một dòng
/// `.Where(x => !x.NeedsReview)` viết ở tầng truy vấn sau này sẽ lặng lẽ đảo ngược
/// quyết định đó — test này giữ cho tầng domain không tự làm chuyện ấy trước.
/// </summary>
public sealed class DisplayStateTests
{
    /// <summary>Ba state được LƯU hiện đúng tên của chúng.</summary>
    [Fact]
    public void Ba_state_luu_hien_dung_ten()
    {
        var record = KnowledgeBuilder.NewRecord();
        Assert.Equal("DRAFT", record.DisplayState);

        record.Approve("an.pham", "team:ops");
        Assert.Equal("ACTIVE", record.DisplayState);

        record.Deprecate();
        Assert.Equal("DEPRECATED", record.DisplayState);
    }

    /// <summary>
    /// Gắn cờ, KHÔNG rút. Sau khi cờ bật, `Lifecycle` vẫn phải là Active — nghĩa là
    /// tri thức vẫn nằm trong tập được trả về, chỉ mang thêm nhãn cảnh báo.
    /// </summary>
    [Fact]
    public void Needs_review_gan_co_chu_khong_rut_tri_thuc_khoi_Active()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Conflicting);
        record.Approve("an.pham", "team:ops");

        Assert.Equal("NEEDS_REVIEW", record.DisplayState);
        Assert.Equal(StoredLifecycleState.Active, record.Lifecycle);
        Assert.Equal("team:ops", record.VisibilityScope);
    }

    /// <summary>
    /// `V3`: SUPERSEDED thắng NEEDS_REVIEW khi cả hai cùng đúng.
    ///
    /// ⚠ Slice này CHƯA có `L4` (KnowledgeRelation) nên `IsSuperseded` luôn false —
    /// đó là stub CÓ CHỦ ĐÍCH, đã ghi rõ trong comment của kiểu, không phải sót.
    /// Test này ghim cái stub đó lại: khi `L4` xuất hiện và `IsSuperseded` biết trả
    /// true, test sẽ đỏ và bắt người sửa quay lại kiểm luật ưu tiên thật.
    /// </summary>
    [Fact]
    public void Superseded_van_con_la_stub_trong_slice_nay()
    {
        var record = KnowledgeBuilder.NewRecord();
        record.NewAssertion(verification: VerificationLevel.Invalidated);
        record.Approve("an.pham", "team:ops");

        Assert.False(record.IsSuperseded,
            "IsSuperseded đã trả true — nghĩa là L4 (KnowledgeRelation) đã có mặt. " +
            "Giờ mới kiểm được luật ưu tiên thật của V3: SUPERSEDED thắng NEEDS_REVIEW " +
            "khi cả hai cùng đúng. Viết test đó thay cho test này.");

        // Cho tới lúc đó, đây là hành vi thật của slice: cờ xem lại vẫn thắng.
        Assert.Equal("NEEDS_REVIEW", record.DisplayState);
    }

    /// <summary>
    /// `V3` cấm lưu NEEDS_REVIEW / SUPERSEDED thành giá trị lifecycle. Thêm chúng vào
    /// enum là tái phát đúng bệnh §6.9 (vocabulary song song) mà Workstream 04 đã mất
    /// ba lần để chữa — và nó sẽ trông hoàn toàn vô hại lúc gõ.
    ///
    /// Nguyên tắc bị vi phạm: "nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó
    /// là PHÉP CHIẾU, không phải dữ liệu độc lập" (dùng lần thứ tư: `L4`→`AP3`→`V3`→`PR1`).
    /// </summary>
    [Fact]
    public void Chi_ba_gia_tri_duoc_luu_lam_lifecycle()
    {
        var tenGiaTri = Enum.GetNames<StoredLifecycleState>();

        Assert.Equal(3, tenGiaTri.Length);
        Assert.Equal(
            new[] { "Active", "Deprecated", "Draft" },
            tenGiaTri.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }
}
