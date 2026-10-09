using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Knowledge;

public sealed class SoanNhapOptions
{
    public const string SectionName = "SoanNhap";

    /// <summary>
    /// Trần số lượt gọi model trong MỘT THÁNG DƯƠNG LỊCH, cho mỗi khách hàng (`E3-1`).
    ///
    /// ⚠ Đếm theo LƯỢT chứ không theo tiền: sản phẩm không biết giá, và một con số tiền
    /// đoán ra sẽ sai theo cách không ai kiểm được. Lượt thì đếm chính xác.
    /// </summary>
    public int TranThang { get; set; } = 20;

    /// <summary>Số case tối đa đưa vào một lượt soạn nháp.</summary>
    public int ToiDaCaseMoiLuot { get; set; } = 30;
}

/// <param name="LyDo">
/// <c>chua-co-credit</c> · <c>vuot-tran-thang</c> · <c>nhom-khong-co-case</c> ·
/// <c>cong-che-chan</c> — mỗi lý do đòi một việc khác nhau, nên đừng gộp vào một nhánh.
/// </param>
public sealed record KetQuaSoanNhap(
    bool Thanh,
    string? LyDo,
    string? GiaiThich,
    int SoCase,
    int SoChoDaChe,
    SopDraftIngestResult? DaNap);

/// <summary>
/// Soạn nháp SOP **từ trong host** — mảnh còn thiếu của bước 4.
///
/// 🎯 **Chỗ nối đáng nói: nhóm lấy từ KHO ĐỊNH TUYẾN, không từ file taxonomy.** Vòng
/// không-credit của bước 3 (`IM-45`) nạp *ticket nào thuộc nhóm nào* vào
/// <see cref="DinhTuyenDaNap"/>; vòng này đọc chính nó ra để biết gom case nào. Hai vòng
/// nối vào nhau, và cả hai đều chạy được khi chưa có credit.
///
/// `SoanNhapRunner` (công cụ rời) đọc corpus TRÊN ĐĨA — đúng cho lúc chưa có đường nạp,
/// nhưng nó không biết gì về tenant. Bản này đọc database của chính khách hàng đang gọi.
///
/// 🛑 **Cổng che `AR-o` chạy TRƯỚC khi gọi model**, trong <c>SopPromptBuilder.Build</c>. Nó
/// ném thì KHÔNG GỬI GÌ CẢ — fail closed, không phải "che được bao nhiêu thì gửi bấy nhiêu".
/// </summary>
public sealed class SoanNhapHandler(
    AppDbContext db,
    ITenantContext tenant,
    ISopDrafter drafter,
    SopDraftIngestHandler nap,
    TimeProvider clock,
    IOptions<SoanNhapOptions> options,
    Startup.CongChe congChe)
{
    private readonly SoanNhapOptions _options = options.Value;

    /// <summary>Gom case của một nhóm từ kho định tuyến. Dùng chung với đường xuất offline.</summary>
    public async Task<SopDraftRequest?> GomAsync(string nhom, CancellationToken ct = default)
    {
        var khoa = await db.Set<DinhTuyenDaNap>()
            .Where(x => x.Nhom == nhom)
            .Select(x => x.KhoaNguon)
            .Take(_options.ToiDaCaseMoiLuot)
            .ToListAsync(ct);

        if (khoa.Count == 0) return null;

        var cases = await db.Cases
            .Where(c => khoa.Contains(c.SourceReference))
            .Select(c => new { c.Id, c.SourceReference, c.Subject })
            .ToListAsync(ct);

        var ids = cases.Select(c => c.Id).ToList();

        // Mẩu người gửi ĐÃ RÚT LẠI thì bỏ (`AR-m`). Một đoạn đã bị rút mà vẫn đi vào bản
        // nháp là đúng thứ `AR-m` sinh ra để chặn.
        var evidence = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId != null && ids.Contains(e.ObservedInCaseId!.Value))
            .Where(e => e.RetractedAt == null)
            .Select(e => new { CaseId = e.ObservedInCaseId!.Value, e.SourceReference, e.Content, e.ObservedAt })
            .ToListAsync(ct);

        var theoCase = evidence
            .GroupBy(e => e.CaseId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<EvidenceForDrafting>)
                    [.. g.OrderBy(x => x.ObservedAt)
                        .Select(x => new EvidenceForDrafting(
                            x.SourceReference, x.Content, x.ObservedAt ?? DateTimeOffset.MinValue))]);

        return new SopDraftRequest
        {
            GroupName = nhom,
            Cases =
            [
                .. cases.Select(c => new CaseForDrafting(
                    c.SourceReference,
                    c.Subject,
                    theoCase.TryGetValue(c.Id, out var ds) ? ds : []))
            ],
        };
    }

    public async Task<KetQuaSoanNhap> SoanAsync(string nhom, string nguoiYeuCau, CancellationToken ct = default)
    {
        var bayGio = clock.GetUtcNow();

        // --- Trần tháng (E3-1) ---
        var dauThang = new DateTimeOffset(bayGio.Year, bayGio.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var daDung = await db.Set<LuotSoanNhap>().CountAsync(x => x.Luc >= dauThang, ct);

        if (daDung >= _options.TranThang)
        {
            return new KetQuaSoanNhap(false, "vuot-tran-thang",
                $"Tháng này đã dùng {daDung}/{_options.TranThang} lượt gọi model. " +
                "Trần đặt ở SoanNhap:TranThang.", 0, 0, null);
        }

        var yeuCau = await GomAsync(nhom, ct);

        if (yeuCau is null)
        {
            return new KetQuaSoanNhap(false, "nhom-khong-co-case",
                $"Chưa có case nào được định tuyến vào nhóm \"{nhom}\". " +
                "Nạp định tuyến trước: --xuat-dinh-tuyen / --nap-dinh-tuyen.", 0, 0, null);
        }

        // --- Cổng che AR-o, TRƯỚC khi gửi bất cứ byte nào ---
        SopPrompt prompt;
        try
        {
            prompt = SopPromptBuilder.Build(yeuCau, congChe.DaQuaCongChe);
        }
        catch (EgressBlockedException e)
        {
            return new KetQuaSoanNhap(false, "cong-che-chan",
                $"Cổng che ở khâu gửi ra vẫn bắt được thứ nghi là bí mật sau khi đã che: {e.Message}",
                yeuCau.Cases.Count, 0, null);
        }

        // Ghi biên lai TRƯỚC khi gọi — lượt hỏng vẫn có thể đã tính tiền. Xem LuotSoanNhap.
        var luot = new LuotSoanNhap
        {
            TenantId = tenant.TenantId,
            Nhom = nhom,
            NguoiYeuCau = nguoiYeuCau,
            SoCase = yeuCau.Cases.Count,
            SoChoDaChe = prompt.RedactedCount,
            Luc = bayGio,
        };

        db.Set<LuotSoanNhap>().Add(luot);
        await db.SaveChangesAsync(ct);

        try
        {
            var ketQua = await drafter.DraftAsync(yeuCau, ct);

            var daNap = await nap.HandleAsync(ketQua.Draft, ct);

            luot.ThanhCong = true;
            await db.SaveChangesAsync(ct);

            return new KetQuaSoanNhap(true, null, null, yeuCau.Cases.Count, prompt.RedactedCount, daNap);
        }
        catch (ChuaCoCreditException e)
        {
            luot.Loi = "chua-co-credit";
            await db.SaveChangesAsync(ct);

            return new KetQuaSoanNhap(false, "chua-co-credit", e.Message,
                yeuCau.Cases.Count, prompt.RedactedCount, null);
        }
        catch (Exception e)
        {
            luot.Loi = $"{e.GetType().Name}: {e.Message}";
            await db.SaveChangesAsync(ct);
            throw;
        }
    }
}
