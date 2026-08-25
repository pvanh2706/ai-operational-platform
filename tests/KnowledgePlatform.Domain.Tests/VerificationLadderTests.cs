using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// `V1` — thang xác minh KHÔNG phải một đường thẳng.
///
/// Bốn mức đầu là thang đơn điệu tăng. CONFLICTING và INVALIDATED nằm NGOÀI thang,
/// và đó không phải chi tiết kỹ thuật: xếp chúng như "một mức tin trung bình" sẽ
/// làm chỗ tranh chấp biến mất khỏi mắt người duyệt — đúng thứ `V1` cảnh báo và
/// đúng thứ `S8` dựa vào để bản nháp gom có giá trị.
///
/// Cái bẫy cụ thể: cả sáu giá trị đều là enum có số, nên `a.Verification >= b`
/// LUÔN biên dịch được và luôn cho ra một câu trả lời trông hợp lý. Trình biên
/// dịch không bao giờ chặn. Chỉ có test chặn được.
/// </summary>
public sealed class VerificationLadderTests
{
    [Theory]
    [InlineData(VerificationLevel.Speculative)]
    [InlineData(VerificationLevel.Plausible)]
    [InlineData(VerificationLevel.Supported)]
    [InlineData(VerificationLevel.Verified)]
    public void Bon_muc_dau_nam_tren_thang(VerificationLevel level)
    {
        Assert.True(level.IsOnLadder());
    }

    /// <summary>
    /// "Bằng chứng chỉ hai hướng" KHÔNG phải "hơi tin". "Từng tin, nay bị bác"
    /// KHÔNG phải "rất không tin". Hai giá trị này trả lời một câu hỏi khác hẳn
    /// với bốn giá trị kia.
    /// </summary>
    [Theory]
    [InlineData(VerificationLevel.Conflicting)]
    [InlineData(VerificationLevel.Invalidated)]
    public void Conflicting_va_Invalidated_nam_ngoai_thang(VerificationLevel level)
    {
        Assert.False(level.IsOnLadder());
    }

    /// <summary>
    /// Bốn mức trên thang phải so sánh được với nhau theo đúng thứ tự đã chốt.
    /// Đây là vế "đơn điệu tăng" của `V1` — vế duy nhất của thang mà việc so sánh
    /// bằng toán tử là HỢP LỆ.
    /// </summary>
    [Fact]
    public void Thang_don_dieu_tang()
    {
        Assert.True(VerificationLevel.Speculative < VerificationLevel.Plausible);
        Assert.True(VerificationLevel.Plausible < VerificationLevel.Supported);
        Assert.True(VerificationLevel.Supported < VerificationLevel.Verified);
    }

    /// <summary>
    /// Hai giá trị ngoài thang phải nằm HẲN ngoài dải số của thang, không xen kẽ.
    /// Nếu ai đó đánh số Conflicting = 3 thì `>= Supported` bỗng đúng với nó, và
    /// mọi bộ lọc "chỉ lấy tri thức đủ tin" sẽ âm thầm nhận thêm chỗ tranh chấp.
    /// Khoảng cách số ở đây là một hàng rào, không phải ngẫu nhiên.
    /// </summary>
    [Fact]
    public void Gia_tri_ngoai_thang_khong_xen_ke_voi_thang()
    {
        var tranCuaThang = new[]
        {
            VerificationLevel.Speculative,
            VerificationLevel.Plausible,
            VerificationLevel.Supported,
            VerificationLevel.Verified,
        }.Max(x => (int)x);

        Assert.True((int)VerificationLevel.Conflicting > tranCuaThang);
        Assert.True((int)VerificationLevel.Invalidated > tranCuaThang);
    }

    /// <summary>
    /// Mọi giá trị của enum phải được phân loại TƯỜNG MINH về một trong hai phía.
    ///
    /// Đây là test đắt giá nhất trong file: thêm một mức xác minh mới mà quên nói
    /// nó nằm trong hay ngoài thang là chuyện xảy ra được trong ba mươi giây, và
    /// `IsOnLadder` viết bằng `is ... or ...` sẽ lặng lẽ trả false cho nó — tức là
    /// mặc định coi nó như chỗ tranh chấp. Test này biến sự im lặng đó thành màu đỏ.
    /// </summary>
    [Fact]
    public void Moi_gia_tri_deu_duoc_phan_loai_tuong_minh()
    {
        var ngoaiThangDaBiet = new[]
        {
            VerificationLevel.Conflicting,
            VerificationLevel.Invalidated,
        };

        var chuaPhanLoai = Enum.GetValues<VerificationLevel>()
            .Where(level => !level.IsOnLadder() && !ngoaiThangDaBiet.Contains(level))
            .ToArray();

        Assert.True(chuaPhanLoai.Length == 0,
            $"VerificationLevel có giá trị mới chưa được phân loại: {string.Join(", ", chuaPhanLoai)}. " +
            "V1 nói thang xác minh không phải đường thẳng — mỗi giá trị phải nằm rõ ràng " +
            "TRÊN thang (so sánh được) hoặc NGOÀI thang (không so sánh được). Sửa " +
            "VerificationLevelExtensions.IsOnLadder, rồi thêm giá trị mới vào test này.");

        // Chiều ngược lại: không giá trị nào được nằm ở CẢ HAI phía. Một giá trị vừa
        // trên thang vừa ngoài thang thì mọi bộ lọc dựa vào IsOnLadder đều mập mờ.
        var phanLoaiHaiChieu = ngoaiThangDaBiet.Where(level => level.IsOnLadder()).ToArray();

        Assert.True(phanLoaiHaiChieu.Length == 0,
            $"IsOnLadder nhận cả giá trị NGOÀI thang: {string.Join(", ", phanLoaiHaiChieu)}. " +
            "Đó là cách chỗ tranh chấp lọt vào tập 'đã đủ tin' mà không ai thấy — đúng thứ " +
            "V1 cảnh báo và S8 dựa vào để bản nháp gom còn giá trị.");
    }
}
