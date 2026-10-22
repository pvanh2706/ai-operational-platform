using KnowledgePlatform.Api.QuyTrinh;
using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Domain.Documents;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Chọn bản gọi model hỏi đáp theo tên nhà cung cấp — bọc lại để test tiêm được bản giả, cùng lý do
/// <see cref="ModelDinhTuyen"/>. Bản thật hỏi <see cref="CacModel.HoiDap"/>.
/// </summary>
public sealed class ModelHoiDap(Func<string, IGoiModelHoiDap?> chon)
{
    public IGoiModelHoiDap? Chon(string nhaCungCap) => chon(nhaCungCap);
}

public sealed class HoiDapOptions
{
    public const string SectionName = "HoiDap";

    /// <summary>Trần lượt gọi model hỏi đáp mỗi tháng, mỗi khách. Đếm theo LƯỢT, như `E3-1`.</summary>
    public int TranThang { get; set; } = 200;

    /// <summary>Trần tổng số ký tự nguồn gửi trong MỘT lượt. Quá thì cắt và nói ra.</summary>
    public int TranKyTu { get; set; } = 60_000;

    /// <summary>Độ dài một đoạn khi chia chữ của tài liệu thành mẩu có mã.</summary>
    public int DoDaiDoan { get; set; } = 1_500;

    /// <summary><c>anthropic</c> (mặc định) · <c>openai</c> — khi request không khai <c>nhaCungCap</c>.</summary>
    public string? NhaCungCap { get; set; }
}

/// <summary>Thân của <c>POST /internal/hoi-dap</c> và hai đường chế độ File. Đúng MỘT trong hai nguồn.</summary>
/// <param name="NguoiHoi">LỜI KHAI của người bấm nút tốn tiền — bắt buộc ở chế độ API, như <c>nguoiYeuCau</c> của `E3-1`.</param>
public sealed record YeuCauHoiDap(
    string? KhoaNguon,
    Guid? TaiLieuId,
    string? CauHoi,
    string? NguoiHoi = null,
    string? NhaCungCap = null,
    System.Text.Json.JsonElement? KetQua = null);

/// <summary>Một mẩu đã gửi, ở dạng ĐÃ che — để người đọc đối chiếu phát biểu với đúng chữ model đã thấy.</summary>
public sealed record NguonTraVe(string Ma, string Nhan, string NoiDung);

/// <param name="LoiVanHanh">
/// <c>true</c> = KHÔNG hỏi được vì vận hành (trần, khoá, cổng che) → 409. <c>false</c> mà <see cref="BietTraLoi"/> =
/// <c>false</c> là KHÔNG BIẾT thật — một câu trả lời đúng, 200.
/// </param>
/// <param name="CheDo"><c>api</c> · <c>file</c>.</param>
public sealed record KetQuaHoiDap(
    bool BietTraLoi,
    bool LoiVanHanh,
    string? LyDoKhongBiet,
    string? GiaiThich,
    IReadOnlyList<PhatBieuHoiDap> PhatBieu,
    int SoPhatBieuBiBo,
    string? GhiChu,
    IReadOnlyList<NguonTraVe> Nguon,
    bool NguonBiCat,
    int SoChoDaChe,
    string CheDo,
    string? NhaCungCap,
    string? Model)
{
    /// <summary>🛑 `Q3-1`: MỌI câu trả lời của đường này mang nhãn này — kể cả khi trông đúng.</summary>
    public string TrangThai => "CHUA-DUYET";
}

/// <summary>Payload chế độ File: đưa cho Claude Code / Codex đọc, nó ghi JSON theo <see cref="Schema"/>.</summary>
public sealed record PromptHoiDap(
    string SystemText,
    string UserText,
    string Schema,
    IReadOnlyList<NguonTraVe> Nguon,
    bool NguonBiCat,
    int SoChoDaChe,
    string HuongDan)
{
    public string TrangThai => "CHUA-DUYET";
}

/// <summary>
/// Hỏi TỰ DO về một case hoặc một tài liệu đã nạp — `Q3-1` (2026-10-22), đường NỘI BỘ của console.
///
/// 🛑 Đây là đường ĐẦU TIÊN cho model sinh chữ từ vật liệu CHƯA DUYỆT rồi đưa cho người đọc. Nó cố ý KHÁC <c>/hoi</c>:
/// <c>/hoi</c> chỉ trả tri thức ĐÃ DUYỆT của bước hiện tại, và là MỘT cửa hỏi của sản phẩm (`F2-1` giữ nguyên). Đường này
/// trả lời bằng evidence/tài liệu thô, nên:
/// <list type="bullet">
///   <item>chỉ sống ở <c>/internal/</c>, console dùng — ai nối nó ra phần mềm khách là đang làm `Q3-2`, phải hỏi lại;</item>
///   <item>mọi câu trả lời mang <c>trangThai: CHUA-DUYET</c>;</item>
///   <item>KHÔNG ghi câu trả lời vào kho tri thức — biên lai chỉ đếm lượt;</item>
///   <item>phát biểu không trỏ được về một mẩu có thật bị bỏ (<see cref="HoiDapDoiChieu"/>).</item>
/// </list>
/// ⚠ Mẩu đã RÚT LẠI (`AR-m`) không được đưa vào — cùng luật với soạn nháp. Tài liệu chưa trích được chữ (PDF, ảnh — `N4-1`)
///   thì nói thẳng là chưa đọc được, không gửi gì.
/// </summary>
public sealed class HoiDapHandler(
    AppDbContext db,
    ITenantContext tenant,
    CacModel cacModel,
    ModelHoiDap chonModel,
    TimeProvider clock,
    IOptions<HoiDapOptions> options,
    Startup.CongChe congChe,
    ILogger<HoiDapHandler> logger)
{
    private readonly HoiDapOptions _options = options.Value;

    private sealed record NguonDaGom(string Loai, string Ma, string MoTa, IReadOnlyList<NguonHoiDap> Manh);

    /// <summary>Kiểm thân request; <c>null</c> = hợp lệ.</summary>
    public static string? KiemYeuCau(YeuCauHoiDap? y) => y switch
    {
        null => "Thiếu thân request.",
        { CauHoi: null } or { CauHoi: "" } => "cauHoi không được để trống.",
        _ when string.IsNullOrWhiteSpace(y.CauHoi) => "cauHoi không được để trống.",
        _ when y.CauHoi!.Length > 4000 => "cauHoi dài quá 4000 ký tự.",
        _ when string.IsNullOrWhiteSpace(y.KhoaNguon) == (y.TaiLieuId is null) =>
            "Khai ĐÚNG MỘT nguồn: khoaNguon (một case) HOẶC taiLieuId (một tài liệu đã nạp qua POST /documents).",
        _ => null,
    };

    /// <summary>Chế độ API — gọi model, TỐN TIỀN.</summary>
    public async Task<KetQua<KetQuaHoiDap>> HoiAsync(YeuCauHoiDap y, string? nhaCungCap, CancellationToken ct = default)
    {
        if (KiemYeuCau(y) is { } loi) return KetQua<KetQuaHoiDap>.Sai(loi);
        if (string.IsNullOrWhiteSpace(y.NguoiHoi))
        {
            return KetQua<KetQuaHoiDap>.Sai(
                "nguoiHoi không được để trống — lượt này TỐN TIỀN, và nguoiHoi là LỜI KHAI của người bấm (như E3-1).");
        }

        var ncc = nhaCungCap ?? NhaCungCap.TuCauHinhDaChuan(_options.NhaCungCap);
        var tenModel = cacModel.TenModel(ncc);

        var gom = await GomAsync(y, ct);
        if (gom.KetCuc != KetCuc.Xong) return new KetQua<KetQuaHoiDap>(null, gom.Loi, gom.KetCuc);
        var nguon = gom.GiaTri!;

        // --- Trần tháng — đếm cả lượt hỏng, như mọi biên lai khác ---
        var bayGio = clock.GetUtcNow();
        var dauThang = new DateTimeOffset(bayGio.Year, bayGio.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var daDung = await db.Set<LuotHoiDap>().CountAsync(x => x.Luc >= dauThang, ct);
        if (daDung >= _options.TranThang)
        {
            return Xong(VanHanh("vuot-tran-thang",
                $"Tháng này đã dùng {daDung}/{_options.TranThang} lượt hỏi đáp bằng model (HoiDap:TranThang).", ncc, tenModel));
        }

        HoiDapPrompt prompt;
        try
        {
            prompt = HoiDapPromptBuilder.Build(y.CauHoi!, nguon.MoTa, nguon.Manh, _options.TranKyTu, congChe.DaQuaCongChe);
        }
        catch (EgressBlockedException e)
        {
            return Xong(VanHanh("cong-che-chan",
                $"Cổng che ở khâu gửi ra vẫn bắt được thứ nghi là bí mật sau khi đã che: {e.Message}", ncc, tenModel));
        }

        var model = chonModel.Chon(ncc);
        if (model is null)
        {
            return Xong(VanHanh("chua-co-credit",
                ncc == NhaCungCap.OpenAI
                    ? cacModel.ThieuOpenAi()
                    : "Chưa cấu hình khoá Anthropic (Anthropic:ApiKey hoặc ANTHROPIC_API_KEY). Dùng chế độ File: " +
                      "POST /internal/hoi-dap/xuat-prompt → đưa cho Claude Code/Codex → POST /internal/hoi-dap/nap-ket-qua.",
                ncc, tenModel, prompt));
        }

        // Biên lai TRƯỚC khi gọi — lượt hỏng vẫn có thể đã tính tiền.
        var luot = new LuotHoiDap
        {
            TenantId = tenant.TenantId,
            LoaiNguon = nguon.Loai,
            MaNguon = nguon.Ma,
            NguoiHoi = y.NguoiHoi!.Trim(),
            SoNguon = prompt.NguonDaChe.Count,
            SoChoDaChe = prompt.RedactedCount,
            NhaCungCap = ncc,
            Model = tenModel,
            Luc = bayGio,
        };
        db.Set<LuotHoiDap>().Add(luot);
        await db.SaveChangesAsync(ct);

        try
        {
            var kq = await model.HoiAsync(prompt, ct);

            luot.ThanhCong = true;
            await db.SaveChangesAsync(ct);

            return Xong(TuDoiChieu(kq, prompt, "api", ncc, tenModel));
        }
        catch (ModelTuChoiException e)
        {
            luot.Loi = e.LyDo;
            await db.SaveChangesAsync(ct);
            return Xong(VanHanh(e.LyDo, e.Message, ncc, tenModel, prompt));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            luot.Loi = Cat($"{e.GetType().Name}: {e.Message}", 1000);
            await db.SaveChangesAsync(ct);
            logger.LogWarning(e, "Hỏi đáp bằng model thất bại — trả lỗi vận hành, không đoán.");
            return Xong(VanHanh("loi-goi-model", $"Gọi model thất bại (biên lai giữ lỗi): {e.Message}", ncc, tenModel, prompt));
        }
    }

    /// <summary>Chế độ File, bước 1 — dựng prompt, KHÔNG gọi gì, KHÔNG ghi gì.</summary>
    public async Task<KetQua<PromptHoiDap>> XuatPromptAsync(YeuCauHoiDap y, CancellationToken ct = default)
    {
        if (KiemYeuCau(y) is { } loi) return KetQua<PromptHoiDap>.Sai(loi);

        var gom = await GomAsync(y, ct);
        if (gom.KetCuc != KetCuc.Xong) return new KetQua<PromptHoiDap>(null, gom.Loi, gom.KetCuc);

        HoiDapPrompt prompt;
        try
        {
            prompt = HoiDapPromptBuilder.Build(y.CauHoi!, gom.GiaTri!.MoTa, gom.GiaTri.Manh, _options.TranKyTu, congChe.DaQuaCongChe);
        }
        catch (EgressBlockedException e)
        {
            return KetQua<PromptHoiDap>.XungDot($"Cổng che chặn: {e.Message}");
        }

        return KetQua<PromptHoiDap>.Xong(new PromptHoiDap(
            prompt.SystemText, prompt.UserText, HoiDapPromptBuilder.SchemaJson, TraVe(prompt), prompt.BiCat, prompt.RedactedCount,
            "Đưa systemText + userText cho Claude Code / Codex. Bảo nó trả ĐÚNG MỘT object JSON theo `schema`, không chữ nào " +
            "khác. Rồi gửi object đó vào POST /internal/hoi-dap/nap-ket-qua cùng khoaNguon/taiLieuId và cauHoi Y HỆT lúc xuất."));
    }

    /// <summary>
    /// Chế độ File, bước 2 — đọc câu trả lời AI viết và đối chiếu mã dẫn bằng ĐÚNG luật của chế độ API. Không ghi gì.
    /// ⚠ Nguồn được dựng LẠI ở đây, không tin danh sách mã mà file mang theo: mẩu bị rút lại giữa hai bước thì mã dẫn tới
    ///   nó cũng bị bỏ.
    /// </summary>
    public async Task<KetQua<KetQuaHoiDap>> NapKetQuaAsync(YeuCauHoiDap y, CancellationToken ct = default)
    {
        if (KiemYeuCau(y) is { } loi) return KetQua<KetQuaHoiDap>.Sai(loi);

        var json = y.KetQua switch
        {
            null => null,
            { ValueKind: System.Text.Json.JsonValueKind.String } v => v.GetString(),
            { } v => v.GetRawText(),
        };

        var kq = HoiDapDoiChieu.DocTuFile(json);
        if (kq is null)
        {
            return KetQua<KetQuaHoiDap>.Sai(
                "ketQua không đọc được thành {bietTraLoi, phatBieu[{noiDung, nguon[]}], ghiChu}. Dán ĐÚNG object JSON AI trả " +
                "(hàng rào ``` được gỡ hộ). Không sửa hộ — sửa hộ là đoán ý AI.");
        }

        var gom = await GomAsync(y, ct);
        if (gom.KetCuc != KetCuc.Xong) return new KetQua<KetQuaHoiDap>(null, gom.Loi, gom.KetCuc);

        HoiDapPrompt prompt;
        try
        {
            prompt = HoiDapPromptBuilder.Build(y.CauHoi!, gom.GiaTri!.MoTa, gom.GiaTri.Manh, _options.TranKyTu, congChe.DaQuaCongChe);
        }
        catch (EgressBlockedException e)
        {
            return KetQua<KetQuaHoiDap>.XungDot($"Cổng che chặn: {e.Message}");
        }

        return Xong(TuDoiChieu(kq, prompt, "file", null, null));
    }

    // ---------------------------------------------------------------------------------------------------------------

    private async Task<KetQua<NguonDaGom>> GomAsync(YeuCauHoiDap y, CancellationToken ct)
    {
        if (y.TaiLieuId is { } id)
        {
            var tl = await db.Set<Document>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (tl is null) return new(null, $"Không có tài liệu {id} trong phạm vi khách hàng này.", KetCuc.KhongThay);

            if (tl.TrangThaiTrichChu != TrichChu.DaTrich || string.IsNullOrWhiteSpace(tl.NoiDungChu))
            {
                return KetQua<NguonDaGom>.XungDot(
                    $"Tài liệu \"{tl.TenKhaiBao}\" CHƯA đọc được chữ (trạng thái: {tl.TrangThaiTrichChu ?? "chưa trích"}" +
                    $"{(tl.GhiChuTrichChu is { } g ? $" — {g}" : "")}). N4-1: PDF/ảnh chưa đọc được; không có chữ thì không gửi gì.");
            }

            var doan = ChiaDoan(tl.NoiDungChu, _options.DoDaiDoan)
                .Select((d, i) => new NguonHoiDap($"N{i + 1}", $"tài liệu \"{tl.TenKhaiBao}\" · đoạn {i + 1}", d))
                .ToList();

            return KetQua<NguonDaGom>.Xong(new(LuotHoiDap.LoaiTaiLieu, tl.Id.ToString(), $"tài liệu \"{tl.TenKhaiBao}\"", doan));
        }

        var c = await TraCaseTheoMa.TimMotAsync(db, y.KhoaNguon!, ct);
        if (c is null)
        {
            return new(null, $"Không thấy case nào mang mã \"{y.KhoaNguon}\" (hoặc mã trần khớp hơn một case — mơ hồ thì không " +
                             "tự chọn). Nạp nó trước: màn Nạp & hỏi, hoặc POST /signals/case-observed.", KetCuc.KhongThay);
        }

        var evidence = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId == c.Id && e.RetractedAt == null)
            .OrderBy(e => e.ObservedAt)
            .Select(e => new { e.SourceReference, e.Content })
            .ToListAsync(ct);

        var manh = new List<NguonHoiDap> { new("N1", "tiêu đề case", c.Subject) };
        manh.AddRange(evidence.Select((e, i) => new NguonHoiDap($"N{i + 2}", e.SourceReference, e.Content)));

        return KetQua<NguonDaGom>.Xong(new(LuotHoiDap.LoaiCase, c.SourceReference,
            $"case {c.SourceReference} — tiêu đề + {evidence.Count} mẩu evidence (mẩu đã rút lại không có ở đây)", manh));
    }

    /// <summary>Chia theo đoạn văn, gom tới gần <paramref name="doDai"/> ký tự; đoạn quá dài thì cắt cứng.</summary>
    internal static IEnumerable<string> ChiaDoan(string chu, int doDai)
    {
        var buf = new System.Text.StringBuilder();
        foreach (var dong in chu.Replace("\r\n", "\n").Split('\n'))
        {
            var d = dong;
            while (d.Length > doDai)
            {
                if (buf.Length > 0) { yield return buf.ToString().Trim(); buf.Clear(); }
                yield return d[..doDai];
                d = d[doDai..];
            }

            if (buf.Length + d.Length + 1 > doDai && buf.Length > 0)
            {
                yield return buf.ToString().Trim();
                buf.Clear();
            }

            buf.AppendLine(d);
        }

        if (!string.IsNullOrWhiteSpace(buf.ToString())) yield return buf.ToString().Trim();
    }

    private static KetQuaHoiDap TuDoiChieu(KetQuaModelHoiDap kq, HoiDapPrompt prompt, string cheDo, string? ncc, string? model)
    {
        var dc = HoiDapDoiChieu.DoiChieu(kq, prompt.NguonDaChe.Select(n => n.Ma).ToHashSet(StringComparer.Ordinal));

        string? lyDo = null;
        string? giaiThich = null;
        if (!dc.BietTraLoi)
        {
            (lyDo, giaiThich) = kq.BietTraLoi
                ? ("khong-co-nguon-dan-chung",
                   $"AI nói biết, nhưng cả {dc.SoPhatBieuBiBo} phát biểu đều không dẫn được mẩu có thật — bỏ hết, trả KHÔNG BIẾT.")
                : ("nguon-khong-noi", "AI đọc nguồn và nói nguồn KHÔNG trả lời được câu hỏi này.");
        }

        if (prompt.BiCat)
        {
            giaiThich = (giaiThich is null ? "" : giaiThich + " ") +
                        "⚠ Nguồn dài quá trần nên CHỈ phần đầu được gửi (HoiDap:TranKyTu) — \"không nói\" có thể là \"không được gửi\".";
        }

        return new KetQuaHoiDap(dc.BietTraLoi, false, lyDo, giaiThich, dc.PhatBieu, dc.SoPhatBieuBiBo, dc.GhiChu,
            TraVe(prompt), prompt.BiCat, prompt.RedactedCount, cheDo, ncc, model);
    }

    private static KetQuaHoiDap VanHanh(string lyDo, string giaiThich, string ncc, string? model, HoiDapPrompt? prompt = null) =>
        new(false, true, lyDo, giaiThich, [], 0, null, prompt is null ? [] : TraVe(prompt), prompt?.BiCat ?? false,
            prompt?.RedactedCount ?? 0, "api", ncc, model);

    private static List<NguonTraVe> TraVe(HoiDapPrompt p) => [.. p.NguonDaChe.Select(n => new NguonTraVe(n.Ma, n.Nhan, n.NoiDung))];

    private static KetQua<KetQuaHoiDap> Xong(KetQuaHoiDap v) => KetQua<KetQuaHoiDap>.Xong(v);

    private static string Cat(string s, int n) => s.Length <= n ? s : s[..n];
}
