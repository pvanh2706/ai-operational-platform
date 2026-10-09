using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Process;

/// <summary>
/// Bản nháp SOP → một quy trình ở trạng thái DRAFT (`S4` quy tắc 1, `05`:197-198: Path A sinh ra nó ở nháp).
///
/// Bước = các bước KIỂM theo đúng thứ tự bản nháp viết → một bước "xử lý theo nguyên nhân đã nhận ra" →
/// bước xác nhận (nếu bản nháp có). Nhánh của từng bước kiểm KHÔNG thành bước: chúng là tri thức
/// (Recognition) và đã vào kho qua <see cref="SopDraftToKnowledge"/> (`IM-49` lựa chọn 1, PR2).
///
/// 🛑 Mọi quy trình sinh ở đây mang <see cref="Origin.AiInference"/> và nằm ở DRAFT — `D4`: AI đề xuất, NGƯỜI
/// công nhận. Bước sinh ra KHÔNG có dấu hiệu nào (<see cref="SopDraft"/> không có trường đó), nên tới khi người
/// duyệt thêm dấu hiệu thì chúng chỉ xong được bằng lời xác nhận.
///
/// ⚠ Thứ tự các bước kiểm lấy theo bản nháp, và `docs/11`:61-64 đã ghi: thứ tự kiểm CHƯA TỪNG được quan sát
/// trên dữ liệu thật. Người duyệt phải đọc nó như một đề xuất.
/// </summary>
public static class SopDraftToProcess
{
    /// <summary>Mã của bước xử lý cuối — không đụng mã bước kiểm nào của bản nháp (chúng là K1, K2…).</summary>
    public const string MaBuocXuLy = "XL";

    /// <summary>Mã của bước xác nhận.</summary>
    public const string MaBuocXacNhan = "XN";

    public static ProcessDefinition Dung(SopDraft draft, Guid tenantId, Guid? sopDraftGocId, int phienBan)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var dinhNghia = new ProcessDefinition
        {
            TenantId = tenantId,
            Nhom = draft.Nhom,
            Ten = $"Quy trình cho nhóm: {draft.Nhom}",
            PhienBan = phienBan,
            Origin = Origin.AiInference,
            ActorLabel = "sinh từ bản nháp SOP",
            SopDraftGocId = sopDraftGocId,
        };

        var thuTu = 0;

        foreach (var k in draft.BuocKiem)
        {
            dinhNghia.Buoc.Add(new ProcessStep
            {
                TenantId = tenantId,
                ProcessDefinitionId = dinhNghia.Id,
                ThuTu = ++thuTu,
                Ma = k.Ma,
                NoiDung = k.CauHoi,
                NoiXem = k.NoiXem,
                ChuDe = SopDraftToKnowledge.NhanChuDe(draft.Nhom, k.Ma),
            });
        }

        dinhNghia.Buoc.Add(new ProcessStep
        {
            TenantId = tenantId,
            ProcessDefinitionId = dinhNghia.Id,
            ThuTu = ++thuTu,
            Ma = MaBuocXuLy,
            NoiDung = "Xử lý theo nguyên nhân đã nhận ra ở các bước kiểm",
            ChuDe = SopDraftToKnowledge.NhanNhom(draft.Nhom),
        });

        if (draft.BuocXacNhan is { } xn && !string.IsNullOrWhiteSpace(xn.Viec))
        {
            dinhNghia.Buoc.Add(new ProcessStep
            {
                TenantId = tenantId,
                ProcessDefinitionId = dinhNghia.Id,
                ThuTu = ++thuTu,
                Ma = MaBuocXacNhan,
                NoiDung = xn.Viec,
                ChuDe = SopDraftToKnowledge.NhanNhom(draft.Nhom),
            });
        }

        ProcessDefinition.KiemBuoc(dinhNghia.Buoc);
        return dinhNghia;
    }
}
