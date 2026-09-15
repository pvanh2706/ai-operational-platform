using KnowledgePlatform.Domain.Knowledge;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Chuyển một <see cref="SopDraft"/> (bản nháp cho MỘT NHÓM) thành các
/// <see cref="KnowledgeRecord"/> (mỗi cái là MỘT NGUYÊN NHÂN — `T1`).
///
/// 🛑 VÌ SAO ĐÂY KHÔNG PHẢI PHÉP MAP 1-1, và vì sao chủ dự án phải chốt hạt:
/// một `SopDraft` là bản nháp của một NHÓM, mà `docs/11` đã đo: nhóm dày nhất có BỐN
/// nguyên nhân với triệu chứng khách kể trùng nhau. `T1` thì nói identity của record là
/// NGUYÊN NHÂN. Nên một nháp sinh ra NHIỀU record.
/// → Chủ dự án chốt 2026-09-13 (form): **mỗi LÁ của cây = 1 record.**
///
/// ⚠ "Đường từ gốc tới lá" KHÔNG liệt kê được — cây thật CÓ CHU TRÌNH. Đo trên
/// `docs/ket-qua-phan-tich/may-sinh-phan-quyen-ky-hieu.json`: `K2 -> K3` và `K3 -> K2`.
/// Liệt kê đường đi sẽ lặp vô hạn. Nên "lá" ở đây hiện thực là **bước sửa**
/// (<see cref="SopFixStep"/>): mỗi bước sửa là một nguyên nhân phân biệt, và các NHÁNH
/// trỏ tới nó là các cách nhận ra nó. Giữ đúng ý chủ dự án chốt, và chạy được trên cây
/// có chu trình.
///
/// ⚠ `AP4` khớp chính xác ở đây: *"một cách nhận ra = MỘT assertion"*. Một nhánh của cây
/// LÀ một cách nhận ra. Nên nhánh -> <see cref="AssertionKind.Recognition"/>, không gộp.
///
/// 🛑 `D4` — MỌI assertion sinh ra ở đây đều mang <see cref="Origin.AiInference"/>, kể cả
/// khi nhánh tự khai `evidence-noi-ro`. Nhãn `chungCu` nói bản nháp CÓ ticket chống lưng
/// hay không; nó KHÔNG biến bản nháp thành lời của người. Record vì thế ra đời ở
/// <c>Draft</c> và chỉ có người gọi <c>Approve()</c> mới đổi được.
/// </summary>
public static class SopDraftToKnowledge
{
    /// <summary>Mã đích KHÔNG phải nguyên nhân — nhánh trỏ tới đây thì không sinh record.</summary>
    private static readonly HashSet<string> DichKhongPhaiNguyenNhan =
        new(StringComparer.Ordinal) { "BANG", "K-XN", "NGOAI-PHAM-VI" };

    /// <summary>
    /// Tiền tố của assertion nói record này thuộc nhóm nào — cầu nối giữa ĐỊNH TUYẾN
    /// (trả về tên nhóm) và KHO TRI THỨC (chứa record theo nguyên nhân).
    ///
    /// 🛑 VÌ SAO LÀ ASSERTION CHỨ KHÔNG PHẢI MỘT CỘT: `AP1` nói thẳng *"applicability là
    /// assertion, không phải field có cấu trúc"*, và ở đây nó còn có lý do thứ hai, nặng
    /// hơn — `docs/13` §3.3 đã đo: **4/5 lỗi định tuyến nằm ở SEAM giữa hai nhóm kề nhau**,
    /// và `AR-p` đang mở đúng câu "vẽ lại các nhóm quanh nhà cung cấp thì sao". Đóng một
    /// taxonomy CHƯA ỔN ĐỊNH thành khoá ngoại trong schema là cam kết vào thứ dự án vừa
    /// đo được là đang lung lay. Assertion thì sửa được bằng một bản mới (`M2`), cột thì phải migration.
    /// </summary>
    public const string NhanNhomPrefix = "Thuộc nhóm nguyên nhân: ";

    /// <summary>Trần của cột `cause_name` trong schema — xem `AppDbContext`.</summary>
    private const int TranDoDaiTenNguyenNhan = 512;

    /// <summary>Nhãn nhóm dùng cho cả lúc GHI và lúc TRA. Một chỗ duy nhất biết cách ghép.</summary>
    public static string NhanNhom(string nhom) => NhanNhomPrefix + nhom;

    /// <summary>
    /// Dựng record + assertion từ một bản nháp. KHÔNG chạm hạ tầng, KHÔNG ghi DB —
    /// cùng lý do với `IM-18`: luật domain phải test được mà không cần PostgreSQL.
    /// </summary>
    public static SopMappingResult Map(SopDraft draft, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(draft);

        // Gom các nhánh trỏ tới từng bước sửa. Khoá = mã bước sửa.
        var nhanhToiBuocSua = new Dictionary<string, List<RecognitionSource>>(StringComparer.Ordinal);
        foreach (var buoc in draft.BuocKiem)
        {
            foreach (var nhanh in buoc.Nhanh)
            {
                if (DichKhongPhaiNguyenNhan.Contains(nhanh.DiToi))
                {
                    continue;
                }

                // Nhánh trỏ sang bước kiểm khác là bước TRUNG GIAN, không phải lá.
                if (draft.BuocKiem.Any(b => string.Equals(b.Ma, nhanh.DiToi, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (!nhanhToiBuocSua.TryGetValue(nhanh.DiToi, out var ds))
                {
                    ds = [];
                    nhanhToiBuocSua[nhanh.DiToi] = ds;
                }

                ds.Add(new RecognitionSource(buoc, nhanh));
            }
        }

        var records = new List<KnowledgeRecord>();
        var buocSuaMoCoi = new List<string>();
        var nhanhTroLacLoi = new List<string>();

        foreach (var buocSua in draft.BuocSua)
        {
            nhanhToiBuocSua.TryGetValue(buocSua.Ma, out var nguon);
            nguon ??= [];

            if (nguon.Count == 0)
            {
                // ⚠ PHÁT HIỆN THẬT, không phải ca giả định: trong nháp nhóm 1 do máy sinh,
                // `S8` không có nhánh nào trỏ tới. Một bước sửa không cách nào tới được là
                // một bước sửa KHÔNG BAO GIỜ được dùng. Không nuốt im lặng — báo ra.
                buocSuaMoCoi.Add(buocSua.Ma);
            }

            records.Add(DungRecord(draft, buocSua, nguon, tenantId));
        }

        // Nhánh trỏ tới mã không tồn tại ở đâu cả — bịa đích. `G6`/`AP3`.
        foreach (var (dich, _) in nhanhToiBuocSua)
        {
            if (!draft.BuocSua.Any(s => string.Equals(s.Ma, dich, StringComparison.Ordinal)))
            {
                nhanhTroLacLoi.Add(dich);
            }
        }

        return new SopMappingResult(records, buocSuaMoCoi, nhanhTroLacLoi);
    }

    private static KnowledgeRecord DungRecord(
        SopDraft draft,
        SopFixStep buocSua,
        List<RecognitionSource> nguon,
        Guid tenantId)
    {
        var record = new KnowledgeRecord
        {
            TenantId = tenantId,
            CauseName = DatTenNguyenNhan(draft, buocSua, nguon),
            Type = KnowledgeType.Diagnostic,
        };

        // --- CauseExists: identity của record (T1) ---
        record.Assertions.Add(new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.CauseExists,
            Content = record.CauseName,
            Origin = Origin.AiInference,
            Verification = MucKiemChung(nguon),
        });

        // --- Recognition: MỘT assertion cho MỖI nhánh (AP4) ---
        foreach (var src in nguon)
        {
            record.Assertions.Add(new Assertion
            {
                TenantId = tenantId,
                KnowledgeRecordId = record.Id,
                Kind = AssertionKind.Recognition,
                Content = $"{src.Buoc.CauHoi} — nơi xem: {src.Buoc.NoiXem} — quan sát: {src.Nhanh.QuanSat}",
                Origin = Origin.AiInference,
                Verification = MucKiemChungCuaNhanh(src.Nhanh),
            });
        }

        // --- Handling: T2, hành động đơn lẻ nằm trong record ---
        record.Assertions.Add(new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.Handling,
            Content = buocSua.Viec,
            Origin = Origin.AiInference,
            Verification = buocSua.Case.Count > 0
                ? VerificationLevel.Supported
                : VerificationLevel.Speculative,
        });

        // --- Applicability: record này thuộc NHÓM nào (cầu nối với định tuyến) ---
        record.Assertions.Add(new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.Applicability,
            Content = NhanNhom(draft.Nhom),
            Origin = Origin.AiInference,
            Verification = VerificationLevel.Speculative,
        });

        // --- Applicability: AP1 — applicability là assertion, không phải field ---
        // Quyền cần có để LÀM được bước kiểm là một điều kiện áp dụng thật: `docs/11` §9.1
        // đã đo ca nguyên nhân nhóm 1 chặn bước kiểm nhóm 2.
        foreach (var quyen in nguon
            .Select(s => s.Buoc.QuyenCan)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.Ordinal))
        {
            record.Assertions.Add(new Assertion
            {
                TenantId = tenantId,
                KnowledgeRecordId = record.Id,
                Kind = AssertionKind.Applicability,
                Content = $"Cần quyền: {quyen}",
                Origin = Origin.AiInference,
                Verification = VerificationLevel.Speculative,
            });
        }

        return record;
    }

    /// <summary>
    /// Tên nguyên nhân lấy từ QUAN SÁT dẫn tới bước sửa, không lấy từ chính bước sửa —
    /// `T1` nói identity là nguyên nhân, mà `viec` mô tả CÁCH CHỮA chứ không mô tả BỆNH.
    /// Ưu tiên nhánh có ticket chống lưng.
    /// </summary>
    private static string DatTenNguyenNhan(SopDraft draft, SopFixStep buocSua, List<RecognitionSource> nguon)
    {
        var tot = nguon
            .OrderByDescending(s => string.Equals(s.Nhanh.ChungCu, "evidence-noi-ro", StringComparison.Ordinal))
            .ThenByDescending(s => s.Nhanh.Case.Count)
            .FirstOrDefault();

        // Bước sửa mồ côi: không quan sát nào dẫn tới nó. Ghi rõ trong chính tên, để ai đọc
        // kho tri thức cũng thấy — thay vì đặt một cái tên nghe bình thường.
        var ten = tot is null
            ? $"[{draft.Nhom} · {buocSua.Ma}] KHÔNG CÓ ĐƯỜNG NHẬN RA — {buocSua.Viec}"
            : tot.Nhanh.QuanSat;

        // ⚠ Cột `cause_name` có trần 512 (AppDbContext). Cắt Ở ĐÂY chứ không để DB văng
        // lỗi: `quanSat` do MODEL sinh, độ dài không ai kiểm soát được. Cắt có dấu "…" để
        // người đọc thấy là đã bị cắt — cắt im lặng là đúng loại thất bại im lặng dự án cấm.
        return ten.Length <= TranDoDaiTenNguyenNhan
            ? ten
            : string.Concat(ten.AsSpan(0, TranDoDaiTenNguyenNhan - 1), "…");
    }

    /// <summary>
    /// Mức kiểm chứng của record = mức CAO NHẤT trong các nhánh dẫn tới nó.
    /// Không có nhánh nào thì <see cref="VerificationLevel.Speculative"/>.
    /// </summary>
    private static VerificationLevel MucKiemChung(List<RecognitionSource> nguon) =>
        nguon.Count == 0
            ? VerificationLevel.Speculative
            : nguon.Max(s => MucKiemChungCuaNhanh(s.Nhanh));

    /// <summary>
    /// ⚠ Trần là <see cref="VerificationLevel.Supported"/>, KHÔNG BAO GIỜ là `Verified`.
    /// `chungCu` là thứ MODEL TỰ KHAI, và `SopDraft` đã ghi rõ điều đó. Một phát biểu do
    /// máy sinh, tự khai là có nguồn, mà được gắn `Verified` thì cái thang `V1` mất nghĩa.
    /// </summary>
    private static VerificationLevel MucKiemChungCuaNhanh(SopBranch nhanh) =>
        string.Equals(nhanh.ChungCu, "evidence-noi-ro", StringComparison.Ordinal) && nhanh.Case.Count > 0
            ? VerificationLevel.Supported
            : VerificationLevel.Speculative;

    private sealed record RecognitionSource(SopCheckStep Buoc, SopBranch Nhanh);
}

/// <summary>
/// Kết quả map, kèm những chỗ bản nháp KHÔNG nhất quán.
///
/// ⚠ Hai danh sách cảnh báo là PHẦN CỦA KẾT QUẢ, không phải log: chúng là thứ `--kiem-cay`
/// bắt được ở tầng Python, và đường nạp vào DB phải bắt được đúng như vậy — nếu không thì
/// một bản nháp hỏng vẫn vào kho êm ru.
/// </summary>
/// <param name="Records">Record đã dựng, còn ở <c>Draft</c>. Chưa ai duyệt.</param>
/// <param name="BuocSuaMoCoi">Mã bước sửa mà KHÔNG nhánh nào trỏ tới — không bao giờ dùng được.</param>
/// <param name="NhanhTroLacLoi">Mã đích mà nhánh trỏ tới nhưng không tồn tại trong bản nháp.</param>
public sealed record SopMappingResult(
    IReadOnlyList<KnowledgeRecord> Records,
    IReadOnlyList<string> BuocSuaMoCoi,
    IReadOnlyList<string> NhanhTroLacLoi)
{
    public bool CoCanhBao => BuocSuaMoCoi.Count > 0 || NhanhTroLacLoi.Count > 0;
}
