using System.Security.Cryptography;
using System.Text;
using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Định tuyến bằng KHO KẾT QUẢ ĐÃ NẠP của khách hàng này, rơi về bảng đã đo nếu chưa có.
///
/// Đây là bản hiện thực của <see cref="IDinhTuyenNhom"/> cho **vòng không-credit**: người vận
/// hành xuất ticket ra, một ngữ cảnh AI khác đọc và trả nhóm, rồi nạp về. Xem
/// <see cref="Domain.Sop.DinhTuyenDaNap"/>.
///
/// 🛑 **Thứ tự tra là KHO TRƯỚC, BẢNG ĐO SAU — và đó là một quyết định, không phải tiện tay.**
/// Bảng `dinh-tuyen-150-case.json` là một PHÉP ĐO ngày 2026-09-10, không phải dữ liệu vận
/// hành. Nếu người vận hành nạp một kết quả cho cùng một mã ticket thì họ đang SỬA một thứ
/// họ thấy sai — ý muốn của người phải thắng ảnh chụp của phép đo.
///
/// ⚠ Và <c>Nguon</c> trả ra NÓI RÕ kết quả đến từ đâu (<c>nguoi-nap:</c>, <c>tra-san:</c>, <c>model:</c>).
/// Trộn nguồn vào một câu trả lời không phân biệt được là cách nhanh nhất để về sau không ai chấm lại được cái gì —
/// đúng chỗ mà `R-K4` đã phải hạ cấp một kết luận vì nó bị viết ở sai mức.
///
/// 🎯 **Từ 2026-10-10 (P8, `N7-1` PROPOSED) có tầng thứ ba — MODEL — cho ticket MỚI:** người nạp → bảng đo →
/// kết quả model đã lưu → gọi model. Chỉ gọi khi CÓ khoá VÀ <c>DinhTuyen:GoiModel = true</c>. Hai luật để tầng mới
/// không đè lên tầng cũ:
/// <list type="bullet">
///   <item>bảng đo nói <c>KHONG-BIET</c> cho một ticket ĐÃ ĐO thì đó là KẾT QUẢ, không phải chỗ trống — model không
///     được gọi để đoán lại;</item>
///   <item>model chỉ được nghe khi nó tự chấm ≥ ngưỡng (`E1-1`, mặc định 0,9 — CHƯA ĐO) VÀ nhóm nó trả có trong
///     taxonomy (`G6`). Mọi thứ khác → KHÔNG BIẾT, kèm lý do.</item>
/// </list>
/// ⚠ Lỗi gọi model (mất mạng, hết credit) KHÔNG làm vỡ <c>/hoi</c> hay webhook: định tuyến trả KHÔNG BIẾT, biên lai
/// giữ lỗi. Một bộ định tuyến ném lỗi là cả đường trả lời chết theo.
/// </summary>
public sealed class DinhTuyenKho(
    AppDbContext db,
    DinhTuyenTraSanNguon traSan,
    ModelDinhTuyen model,
    IOptions<DinhTuyenOptions> options,
    TaxonomyNhom taxonomy,
    CongChe congChe,
    ITenantContext tenant,
    TimeProvider clock,
    ILogger<DinhTuyenKho> logger) : IDinhTuyenNhom
{
    /// <summary>Tiền tố nguồn cho kết quả do người vận hành nạp.</summary>
    public const string TienToNguoiNap = "nguoi-nap:";

    /// <summary>Tiền tố nguồn cho kết quả lấy từ bảng đã đo.</summary>
    public const string TienToTraSan = "tra-san:";

    /// <summary>Tiền tố nguồn cho kết quả do model trả (P8). Luôn kèm "ngưỡng chưa đo" cho tới khi có phép đo.</summary>
    public const string TienToModel = "model:";

    public async Task<KetQuaDinhTuyen> DinhTuyenAsync(YeuCauDinhTuyen yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        var ma = yeuCau.MaTicket?.Trim();

        if (!string.IsNullOrEmpty(ma))
        {
            var daNap = await db.Set<DinhTuyenDaNap>()
                .FirstOrDefaultAsync(x => x.KhoaNguon == ma, ct);

            if (daNap is not null)
            {
                return daNap.BietNhom
                    ? new KetQuaDinhTuyen(daNap.Nhom, TienToNguoiNap + daNap.Nguon, daNap.GiaiThich)
                    : KetQuaDinhTuyen.KhongXacDinh(TienToNguoiNap + daNap.Nguon, daNap.GiaiThich);
            }
        }

        // Chưa nạp -> rơi về bảng đã đo. Nó tự trả KHÔNG BIẾT cho mọi mã ngoài 150 ticket,
        // và đó là câu trả lời trung thực duy nhất của một bảng tra.
        var cu = await traSan.Bang.DinhTuyenAsync(yeuCau, ct);
        var tuBang = cu with { Nguon = TienToTraSan + cu.Nguon };

        // Bảng biết, HOẶC bảng đã ĐO và nói không biết → đó là kết quả. Chỉ ticket NGOÀI bảng mới tới lượt model.
        var ngoaiBang = !cu.BietNhom && cu.Nguon is "tra-san-khong-co-trong-bang" or "tra-san-khong-co-ma-ticket";
        if (!ngoaiBang || !options.Value.GoiModel || model.Model is null) return tuBang;

        if (string.IsNullOrWhiteSpace(yeuCau.TieuDe) && string.IsNullOrWhiteSpace(yeuCau.MoTa)) return tuBang;

        return await QuaModelAsync(yeuCau, ma, ct) ?? tuBang;
    }

    /// <summary>Tầng model. <c>null</c> = không có gì để nói thêm, dùng kết quả của bảng.</summary>
    private async Task<KetQuaDinhTuyen?> QuaModelAsync(YeuCauDinhTuyen yeuCau, string? ma, CancellationToken ct)
    {
        if (taxonomy.Nhom is not { Count: > 0 } nhom)
        {
            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "khong-co-taxonomy",
                $"Không đọc được taxonomy ({taxonomy.DuongDan}) nên không hỏi model được — không có tập nhóm để chọn.");
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{yeuCau.TieuDe}\n{yeuCau.MoTa}")));

        // --- Bộ nhớ đệm: định tuyến chạy trên MỌI webhook và MỌI /hoi ---
        var daLuu = await db.Set<LuotDinhTuyenModel>()
            .Where(x => x.ThanhCong && (ma != null ? x.KhoaNguon == ma : x.HashDauVao == hash))
            .OrderByDescending(x => x.Luc)
            .FirstOrDefaultAsync(ct);

        if (daLuu is not null) return DanhGia(daLuu, nhom, "model-da-luu");

        // --- Trần tháng, biên lai TRƯỚC khi gọi (cùng khuôn SoanNhapHandler) ---
        var bayGio = clock.GetUtcNow();
        var dauThang = new DateTimeOffset(bayGio.Year, bayGio.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var daDung = await db.Set<LuotDinhTuyenModel>().CountAsync(x => x.Luc >= dauThang, ct);

        if (daDung >= options.Value.TranThang)
        {
            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "vuot-tran-thang",
                $"Tháng này đã dùng {daDung}/{options.Value.TranThang} lượt định tuyến bằng model (DinhTuyen:TranThang).");
        }

        DinhTuyenPrompt prompt;
        try
        {
            prompt = DinhTuyenPromptBuilder.Build(yeuCau.TieuDe, yeuCau.MoTa, nhom, congChe.DaQuaCongChe);
        }
        catch (Domain.Redaction.EgressBlockedException e)
        {
            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "cong-che-chan", e.Message);
        }

        var bienLai = new LuotDinhTuyenModel
        {
            TenantId = tenant.TenantId, KhoaNguon = ma, HashDauVao = hash, SoChoDaChe = prompt.RedactedCount, Luc = bayGio,
        };
        db.Set<LuotDinhTuyenModel>().Add(bienLai);
        await db.SaveChangesAsync(ct);

        try
        {
            var kq = await model.Model!.PhanLoaiAsync(prompt, ct);

            bienLai.Nhom = kq.Nhom;
            bienLai.Nguon = kq.Nguon;
            bienLai.GiaiThich = kq.GiaiThich;
            bienLai.DoTinCay = kq.DoTinCay;
            bienLai.ThanhCong = true;
            await db.SaveChangesAsync(ct);

            return DanhGia(bienLai, nhom, "model");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            bienLai.Loi = $"{e.GetType().Name}: {e.Message}";
            await db.SaveChangesAsync(ct);
            logger.LogWarning(e, "Định tuyến bằng model thất bại — trả KHÔNG BIẾT, không làm vỡ đường trả lời.");

            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "loi",
                "Gọi model định tuyến thất bại (xem biên lai). Trả KHÔNG BIẾT thay vì đoán.");
        }
    }

    /// <summary>
    /// Áp NGƯỠNG và TAXONOMY lên câu trả lời thô — mỗi lần đọc, kể cả từ bộ nhớ đệm: ngưỡng là cấu hình, đổi được.
    /// </summary>
    private KetQuaDinhTuyen DanhGia(LuotDinhTuyenModel b, IReadOnlyList<NhomTaxonomy> nhom, string tang)
    {
        var nguong = options.Value.NguongTinCay;
        var nguon = $"{TienToModel}{tang}:{b.Nguon} · ngưỡng {nguong:0.00} CHƯA ĐO";

        if (string.IsNullOrWhiteSpace(b.Nhom) || b.Nhom == KetQuaDinhTuyen.KhongBiet)
        {
            return KetQuaDinhTuyen.KhongXacDinh(nguon, b.GiaiThich);
        }

        if (!nhom.Any(n => string.Equals(n.Ten, b.Nhom, StringComparison.Ordinal)))
        {
            // `G6`: một tên nhóm bịa ra mà được "sửa cho giống" là biến một lỗi nhìn thấy được thành lỗi im lặng.
            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "nhom-ngoai-taxonomy",
                $"Model trả nhóm \"{b.Nhom}\" — KHÔNG có trong taxonomy. Không sửa hộ, không tìm nhóm gần nhất.");
        }

        if ((b.DoTinCay ?? 0) < nguong)
        {
            return KetQuaDinhTuyen.KhongXacDinh(TienToModel + "duoi-nguong",
                $"Model nghiêng về \"{b.Nhom}\" với độ tin {b.DoTinCay:0.00} < ngưỡng {nguong:0.00} (E1-1: thà im lặng). " +
                "⚠ Ngưỡng CHƯA ĐO.");
        }

        return new KetQuaDinhTuyen(b.Nhom, nguon, b.GiaiThich);
    }
}

/// <summary>
/// Bọc <see cref="DinhTuyenTraSan"/> để nó sống ở phạm vi singleton (đọc file một lần) trong
/// khi <see cref="DinhTuyenKho"/> phải là scoped (cần <c>AppDbContext</c> của request).
/// </summary>
public sealed class DinhTuyenTraSanNguon(DinhTuyenTraSan bang)
{
    public DinhTuyenTraSan Bang { get; } = bang;
}

/// <summary>
/// Bản gọi model định tuyến, hoặc <c>null</c> khi KHÔNG có khoá Anthropic (P8). Bọc lại để DI tiêm được "không có".
/// </summary>
public sealed class ModelDinhTuyen(IGoiModelDinhTuyen? model)
{
    public IGoiModelDinhTuyen? Model { get; } = model;
}
