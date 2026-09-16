namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Kiểm một bản nháp SOP TRƯỚC khi nó chạm vào logic chuyển đổi.
///
/// 🛑 **Vì sao lớp này ra đời, và ranh giới của nó nằm ở một chi tiết rất dễ đọc nhầm.**
/// <c>System.Text.Json</c> xử lý HAI kiểu trường theo hai cách khác hẳn nhau:
/// <code>
///   required property     SopDraft.Nhom, SopCheckStep.CauHoi ...
///                         -> THIẾU LÀ NÉM JsonException, và ASP.NET đổi thành 400. Kín rồi.
///   tham số của POSITIONAL RECORD   SopBranch(QuanSat, DiToi, ...), SopSymptom, SopFixStep,
///                         SopDistribution ...
///                         -> THIẾU THÌ IM LẶNG THÀNH null. Không ai ném gì cả.
/// </code>
/// Chính nhóm thứ hai làm endpoint trả <b>500</b>: payload gõ `dieuKien` thay cho `quanSat`,
/// <c>SopBranch.QuanSat</c> thành `null`, và nó chạy tiếp tới tận
/// <see cref="SopDraftToKnowledge"/> mới nổ bằng <c>NullReferenceException</c>.
///
/// → Lớp này canh đúng nhóm thứ hai. Nhóm thứ nhất KHÔNG cần nó, và các phép kiểm ở đây cho
///   nhóm ấy chỉ là lưới thứ hai — vô hại, nhưng đừng đọc thành "nhờ nó mà kín".
///
/// ⚠ Và đây đúng là loại lỗi mà bộ test C# KHÔNG BAO GIỜ thấy được: nó luôn dựng
/// <c>SopDraft</c> bằng object initializer, nơi trình biên dịch ép đủ trường. Chỉ một payload
/// VIẾT TAY — tức đúng thứ bên tích hợp gửi — mới tạo ra được. Tìm ra bằng
/// `scripts/kiem_hai_khach.py`, cùng họ với `IM-38`.
///
/// Khác biệt giữa 400 và 500 ở đây không phải chuyện thẩm mỹ: **400 nói cho bên tích hợp
/// biết họ gõ sai chỗ nào; 500 nói với họ rằng lỗi ở phía mình.** Người tích hợp sẽ đi tìm
/// sai chỗ, và sẽ tìm rất lâu.
/// </summary>
public static class SopDraftKiemTra
{
    /// <summary>
    /// Trả về chỗ sai ĐẦU TIÊN, hoặc <c>null</c> nếu bản nháp dùng được.
    ///
    /// ⚠ Thông điệp cố ý nhắc TÊN TRƯỜNG trong JSON, không phải tên thuộc tính C#: người đọc
    /// nó đang cầm một gói JSON trong tay. Gõ sai `quanSat` thành `dieuKien` là ca đã xảy ra
    /// thật, và một câu báo lỗi nói "QuanSat null" thì không giúp họ tìm ra.
    /// </summary>
    public static string? KiemTra(SopDraft? draft)
    {
        if (draft is null) return "Thân request rỗng hoặc không đọc được thành JSON.";

        if (string.IsNullOrWhiteSpace(draft.Nhom)) return "Thiếu `nhom`.";

        if (draft.PhanBoBuocKiemDuocGhiLai is null)
        {
            return "Thiếu `phanBoBuocKiemDuocGhiLai` — năm ô phân bố của `AR-q`.";
        }

        if (draft.TrieuChungVao is null || draft.TrieuChungVao.Count == 0)
        {
            return "Thiếu `trieuChungVao` — phải có ít nhất một triệu chứng.";
        }

        foreach (var t in draft.TrieuChungVao)
        {
            if (t is null || string.IsNullOrWhiteSpace(t.MoTa))
            {
                return "Một phần tử của `trieuChungVao` thiếu `moTa`.";
            }
        }

        if (draft.BuocKiem is null) return "Thiếu `buocKiem` (được phép là mảng rỗng, nhưng phải có).";

        foreach (var k in draft.BuocKiem)
        {
            if (k is null) return "`buocKiem` chứa một phần tử rỗng.";
            if (string.IsNullOrWhiteSpace(k.Ma)) return "Một bước trong `buocKiem` thiếu `ma`.";
            if (string.IsNullOrWhiteSpace(k.CauHoi)) return $"Bước kiểm `{k.Ma}` thiếu `cauHoi`.";
            if (string.IsNullOrWhiteSpace(k.NoiXem)) return $"Bước kiểm `{k.Ma}` thiếu `noiXem`.";
            if (k.Nhanh is null) return $"Bước kiểm `{k.Ma}` thiếu `nhanh`.";

            foreach (var n in k.Nhanh)
            {
                if (n is null) return $"Bước kiểm `{k.Ma}` có một nhánh rỗng.";

                // ⚠ Đây CHÍNH LÀ ca đã làm endpoint trả 500: payload gõ `dieuKien` thay vì
                //   `quanSat`, nên QuanSat là null và nó nổ ở tận chỗ đặt tên nguyên nhân.
                if (string.IsNullOrWhiteSpace(n.QuanSat))
                {
                    return $"Một nhánh của bước `{k.Ma}` thiếu `quanSat` — câu mô tả điều QUAN SÁT ĐƯỢC.";
                }

                if (string.IsNullOrWhiteSpace(n.DiToi))
                {
                    return $"Nhánh \"{n.QuanSat}\" của bước `{k.Ma}` thiếu `diToi` — mã bước sửa mà nó dẫn tới.";
                }
            }
        }

        if (draft.BuocSua is null || draft.BuocSua.Count == 0)
        {
            return "Thiếu `buocSua` — mỗi bước sửa là một nguyên nhân (`T1`), không có thì không " +
                   "có gì để nạp.";
        }

        foreach (var s in draft.BuocSua)
        {
            if (s is null) return "`buocSua` chứa một phần tử rỗng.";
            if (string.IsNullOrWhiteSpace(s.Ma)) return "Một bước trong `buocSua` thiếu `ma`.";
            if (string.IsNullOrWhiteSpace(s.Viec)) return $"Bước sửa `{s.Ma}` thiếu `viec`.";
        }

        return null;
    }
}
