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

    /// <summary>Tiền tố của nhãn chủ đề THEO BƯỚC KIỂM — xem <see cref="NhanChuDe"/>.</summary>
    public const string NhanChuDePrefix = "Áp dụng ở bước kiểm: ";

    /// <summary>
    /// Nhãn chủ đề của MỘT bước kiểm (L1, P4 2026-10-10) — để bước quy trình tra tri thức theo CHỦ ĐỀ chứ
    /// không trỏ tới record cụ thể. Record nào có nhánh đi ra từ bước <paramref name="maBuoc"/> thì mang nhãn
    /// này; bước đó của quy trình tra đúng nhãn này.
    ///
    /// ⚠ Là assertion chứ không phải cột — cùng lý do với <see cref="NhanNhom"/> (`AP1`, taxonomy lung lay).
    /// Một chỗ ghép duy nhất cho cả lúc ghi (ở đây) lẫn lúc tra (<c>SopDraftToProcess</c>).
    /// </summary>
    public static string NhanChuDe(string nhom, string maBuoc) => $"{NhanChuDePrefix}{nhom} · {maBuoc}";

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
        var trichDan = new List<TrichDanCase>();
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

            records.Add(DungRecord(draft, buocSua, nguon, tenantId, trichDan));
        }

        // Nhánh trỏ tới mã không tồn tại ở đâu cả — bịa đích. `G6`/`AP3`.
        foreach (var (dich, _) in nhanhToiBuocSua)
        {
            if (!draft.BuocSua.Any(s => string.Equals(s.Ma, dich, StringComparison.Ordinal)))
            {
                nhanhTroLacLoi.Add(dich);
            }
        }

        return new SopMappingResult(records, buocSuaMoCoi, nhanhTroLacLoi, trichDan);
    }

    /// <summary>
    /// Ghi lại "phát biểu này dựa trên ticket nào" — thứ bản nháp CÓ mà trước 2026-10-10 bị vứt.
    ///
    /// 🛑 Vì sao đây là lỗi chứ không phải chi tiết: <c>assertion_evidence</c> có 0 dòng vì đúng
    /// chỗ này. Bản nháp mang danh sách <c>case</c> ở mọi nhánh và mọi bước sửa, nhưng phép map
    /// cũ chỉ dùng nó để chấm mức kiểm chứng rồi bỏ. Hệ quả dây chuyền: `/hoi` không trỏ được về
    /// nguồn, lời khai `xacNhanDuLieuThuongMai` không đối chiếu được, V3 không biết mẩu nguồn bị
    /// rút. Bản nháp gốc cũng không được lưu, nên 8 record cũ ở `kp_dev` KHÔNG điền bù được.
    ///
    /// ⚠ Hàm này chỉ ghi MÃ CASE. Mã case → mẩu evidence nào là việc của tầng có database
    /// (<c>SopDraftIngestHandler</c>): Domain không biết case nào tồn tại.
    /// </summary>
    private static void GhiTrichDan(
        List<TrichDanCase> dich,
        SopDraft draft,
        Assertion assertion,
        IReadOnlyList<string> cases,
        string? chungCu,
        string nguon)
    {
        foreach (var ma in cases.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()))
        {
            // Cùng một phát biểu có thể trích một case qua hai nhánh — giữ MỘT dòng.
            if (dich.Any(t => ReferenceEquals(t.Assertion, assertion) && t.MaCase == ma)) continue;

            var loai = chungCu is null ? "bước sửa" : $"chungCu={chungCu}";
            var ghiChu = $"ticket {ma} · {nguon} · {loai} · {cases.Count}/{draft.SoCaseDungSau} case của nhóm";

            dich.Add(new TrichDanCase(assertion, ma, chungCu, ghiChu));
        }
    }

    private static KnowledgeRecord DungRecord(
        SopDraft draft,
        SopFixStep buocSua,
        List<RecognitionSource> nguon,
        Guid tenantId,
        List<TrichDanCase> trichDan)
    {
        var record = new KnowledgeRecord
        {
            TenantId = tenantId,
            CauseName = DatTenNguyenNhan(draft, buocSua, nguon),
            Type = KnowledgeType.Diagnostic,
        };

        // --- CauseExists: identity của record (T1) ---
        var tonTai = new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.CauseExists,
            Content = record.CauseName,
            Origin = Origin.AiInference,
            Verification = MucKiemChung(nguon),
        };
        record.Assertions.Add(tonTai);

        // Nguyên nhân tồn tại = mọi ticket mà một NHÁNH dẫn tới nó trích ra.
        foreach (var src in nguon)
        {
            GhiTrichDan(trichDan, draft, tonTai, src.Nhanh.Case, src.Nhanh.ChungCu,
                $"nhánh {src.Buoc.Ma}→{buocSua.Ma}");
        }

        // --- Recognition: MỘT assertion cho MỖI nhánh (AP4) ---
        foreach (var src in nguon)
        {
            var nhanRa = new Assertion
            {
                TenantId = tenantId,
                KnowledgeRecordId = record.Id,
                Kind = AssertionKind.Recognition,
                Content = $"{src.Buoc.CauHoi} — nơi xem: {src.Buoc.NoiXem} — quan sát: {src.Nhanh.QuanSat}",
                Origin = Origin.AiInference,
                Verification = MucKiemChungCuaNhanh(src.Nhanh),
            };
            record.Assertions.Add(nhanRa);

            GhiTrichDan(trichDan, draft, nhanRa, src.Nhanh.Case, src.Nhanh.ChungCu,
                $"nhánh {src.Buoc.Ma}→{buocSua.Ma}");
        }

        // --- Handling: T2, hành động đơn lẻ nằm trong record ---
        var xuLy = new Assertion
        {
            TenantId = tenantId,
            KnowledgeRecordId = record.Id,
            Kind = AssertionKind.Handling,
            Content = buocSua.Viec,
            Origin = Origin.AiInference,
            Verification = buocSua.Case.Count > 0
                ? VerificationLevel.Supported
                : VerificationLevel.Speculative,
        };
        record.Assertions.Add(xuLy);

        GhiTrichDan(trichDan, draft, xuLy, buocSua.Case, chungCu: null, $"bước sửa {buocSua.Ma}");

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

        // --- Applicability: chủ đề THEO BƯỚC KIỂM (L1, P4) — bước nào của quy trình nhận ra được nguyên nhân này ---
        foreach (var ma in nguon.Select(s => s.Buoc.Ma).Distinct(StringComparer.Ordinal))
        {
            record.Assertions.Add(new Assertion
            {
                TenantId = tenantId,
                KnowledgeRecordId = record.Id,
                Kind = AssertionKind.Applicability,
                Content = NhanChuDe(draft.Nhom, ma),
                Origin = Origin.AiInference,
                Verification = VerificationLevel.Speculative,
            });
        }

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
/// <param name="TrichDan">
/// Phát biểu nào dựa trên TICKET nào — thứ bản nháp có sẵn mà phép map cũ vứt đi. Đây mới là
/// MÃ CASE; nối sang mẩu evidence cụ thể là việc của tầng có database.
/// </param>
public sealed record SopMappingResult(
    IReadOnlyList<KnowledgeRecord> Records,
    IReadOnlyList<string> BuocSuaMoCoi,
    IReadOnlyList<string> NhanhTroLacLoi,
    IReadOnlyList<TrichDanCase> TrichDan)
{
    public bool CoCanhBao => BuocSuaMoCoi.Count > 0 || NhanhTroLacLoi.Count > 0;
}

/// <summary>Một phát biểu trích một ticket.</summary>
/// <param name="MaCase">Mã đúng như bản nháp viết — có thể TRẦN (<c>ES-9001</c>) hoặc có tiền tố nguồn.</param>
/// <param name="ChungCu">
/// <c>evidence-noi-ro</c> / <c>toi-suy-ra</c> của nhánh, hoặc <c>null</c> với bước sửa. Đây là LỜI
/// MODEL TỰ KHAI, nên nó chỉ quyết định QUAN HỆ của liên kết (Support hay ContextFor), không bao giờ
/// nâng mức kiểm chứng lên quá Supported.
/// </param>
public sealed record TrichDanCase(Assertion Assertion, string MaCase, string? ChungCu, string GhiChu);
