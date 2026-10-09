using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Process;

/// <summary>
/// Một quy trình đã (hoặc sắp) được duyệt cho MỘT loại công việc — ô `D` của sơ đồ luồng tín hiệu.
/// Thiết kế: `docs/05` §5 (PR1–PR4). Đưa vào phạm vi 2026-10-10 bằng việc mở lại `I1` (`docs/18`).
///
/// 🛑 **TUYẾN TÍNH, KHÔNG NHÁNH (PR2).** SOP thật là CÂY (`docs/11`, `IM-30`), nhưng nhánh
/// "thấy X thì là nguyên nhân Y" là TRI THỨC (Recognition), không phải bước. Quy trình chỉ trả lời
/// *"làm gì tiếp"*; tri thức trả lời *"thấy gì thì kết luận gì"*. Tách vậy thì PR2 đứng nguyên mà SOP
/// cây vẫn dùng được (`IM-49` lựa chọn 1).
///
/// ⚠ **Dùng CHUNG nhân với Knowledge (`05` §5, S4):** <see cref="StoredLifecycleState"/>,
/// <see cref="Knowledge.VisibilityScope"/>, <see cref="Origin"/>, <see cref="KnowledgeApproval"/>. Không đẻ
/// từ vựng mới — bệnh "từ vựng song song" đã tái phát ba lần trong một workstream.
///
/// ⚠ **`N5-1` (PROPOSED):** DRAFT sửa được bước; ACTIVE BẤT BIẾN — muốn đổi thì tạo phiên bản mới.
/// **`N6-1` (PROPOSED):** duyệt phiên bản mới thì bản cũ thành DEPRECATED, nhưng
/// <see cref="ProcessRun"/> đang chạy vẫn trỏ đúng bản cũ.
/// </summary>
public sealed class ProcessDefinition : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>
    /// Loại công việc mà quy trình này dành cho — hôm nay là TÊN NHÓM định tuyến (`IM-49` lựa chọn 3).
    /// Chuỗi tự do, cùng lý do nhãn nhóm của tri thức là assertion chứ không phải khoá ngoại: taxonomy
    /// đang lung lay (`AR-p`).
    /// </summary>
    public required string Nhom { get; init; }

    public required string Ten { get; init; }

    /// <summary>Tăng dần trong một nhóm. Một nhóm có tối đa MỘT bản ACTIVE (unique index có điều kiện).</summary>
    public required int PhienBan { get; init; }

    public StoredLifecycleState Lifecycle { get; private set; } = StoredLifecycleState.Draft;

    public VisibilityScope? VisibilityScope { get; private set; }

    /// <summary>Hình thành bằng cách nào. Sinh từ bản nháp SOP = <see cref="Origin.AiInference"/>, giữ vĩnh viễn.</summary>
    public required Origin Origin { get; init; }

    /// <summary>Ai tạo. Lời khai, như mọi tên người trong hệ thống hôm nay.</summary>
    public string? ActorLabel { get; init; }

    /// <summary>Bản nháp SOP sinh ra quy trình này, nếu có. Unique — một bản nháp sinh đúng MỘT quy trình.</summary>
    public Guid? SopDraftGocId { get; init; }

    public KnowledgeApproval? LastApproval { get; private set; }

    public List<ProcessStep> Buoc { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Thay TOÀN BỘ danh sách bước — chỉ khi còn DRAFT (`N5-1`).
    ///
    /// ⚠ Thay cả danh sách chứ không sửa từng bước: thứ tự là một phần của định nghĩa (PR1 — bước hiện
    /// tại là bước chưa xong ĐẦU TIÊN), và sửa lẻ từng bước là cách thứ tự lệch đi mà không ai thấy.
    /// </summary>
    public void ThayBuoc(IReadOnlyList<ProcessStep> buocMoi)
    {
        ArgumentNullException.ThrowIfNull(buocMoi);

        if (Lifecycle != StoredLifecycleState.Draft)
        {
            throw new InvalidOperationException(
                $"Quy trình {Id} đang {Lifecycle} — chỉ DRAFT mới sửa được bước (N5-1). " +
                "Muốn đổi một quy trình đã duyệt thì tạo phiên bản mới.");
        }

        KiemBuoc(buocMoi);
        Buoc.Clear();
        Buoc.AddRange(buocMoi);
    }

    /// <summary>
    /// Duyệt: nội dung + quyền xem trong MỘT hành động (S7), cùng lời khai dữ liệu thương mại (`AR-n`) —
    /// đúng khuôn <see cref="KnowledgeRecord.Approve"/>, vì duyệt quy trình cũng là XUẤT BẢN.
    /// </summary>
    public void Approve(string nguoiDuyet, VisibilityScope phamViXem, bool xacNhanDuLieuThuongMai, string? lyDo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nguoiDuyet);

        if (!xacNhanDuLieuThuongMai)
        {
            throw new InvalidOperationException(
                "AR-n: không duyệt được khi người duyệt chưa xác nhận đã soát dữ liệu thương mại.");
        }

        if (!Enum.IsDefined(phamViXem))
        {
            throw new ArgumentOutOfRangeException(nameof(phamViXem), phamViXem,
                $"Phạm vi xem không nằm trong tập đã chốt. Nhận: {VisibilityScopes.Allowed}.");
        }

        if (Lifecycle != StoredLifecycleState.Draft)
        {
            throw new InvalidOperationException(
                $"Quy trình {Id} đang {Lifecycle} — chỉ duyệt được bản DRAFT (N5-1: bản ACTIVE bất biến).");
        }

        if (Buoc.Count == 0)
        {
            throw new InvalidOperationException("Quy trình không có bước nào — không có gì để duyệt.");
        }

        LastApproval = new KnowledgeApproval
        {
            ApprovedByActor = nguoiDuyet,
            ApprovedAt = DateTimeOffset.UtcNow,
            VisibilityScopeBefore = VisibilityScope,
            VisibilityScopeAfter = phamViXem,
            Reason = lyDo,
            CommercialDataConfirmed = true,
        };

        VisibilityScope = phamViXem;
        Lifecycle = StoredLifecycleState.Active;
    }

    /// <summary>Rút khỏi lưu hành — khi một phiên bản mới của cùng nhóm được duyệt (`N6-1`).</summary>
    public void Deprecate() => Lifecycle = StoredLifecycleState.Deprecated;

    /// <summary>Mã bước duy nhất, thứ tự liền mạch từ 1, nội dung không rỗng.</summary>
    public static void KiemBuoc(IReadOnlyList<ProcessStep> buoc)
    {
        var trung = buoc.GroupBy(b => b.Ma, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (trung.Count > 0)
        {
            throw new ArgumentException($"Mã bước bị trùng: {string.Join(", ", trung)}. Lời xác nhận trỏ tới bước bằng mã.");
        }

        if (buoc.Any(b => string.IsNullOrWhiteSpace(b.Ma) || string.IsNullOrWhiteSpace(b.NoiDung)))
        {
            throw new ArgumentException("Mỗi bước phải có mã và nội dung.");
        }

        if (!buoc.Select(b => b.ThuTu).Order().SequenceEqual(Enumerable.Range(1, buoc.Count)))
        {
            throw new ArgumentException("Thứ tự bước phải liền mạch từ 1 — PR1: bước hiện tại là bước chưa xong ĐẦU TIÊN.");
        }
    }
}

/// <summary>
/// Một bước của quy trình. Ba phần theo `05`:200-203: nội dung · bằng chứng cần · tra tri thức.
/// </summary>
public sealed class ProcessStep : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid ProcessDefinitionId { get; init; }

    public required int ThuTu { get; init; }

    /// <summary>Mã bước (<c>K1</c>, <c>XL</c>…). Lời xác nhận "đã làm bước K1" trỏ tới đây.</summary>
    public required string Ma { get; init; }

    public required string NoiDung { get; init; }

    public string? NoiXem { get; init; }

    /// <summary>
    /// "Bằng chứng cần" (PR1) ở dạng ĐO ĐƯỢC: các cụm từ mà một mẩu evidence của case chứa thì coi như bước
    /// đã làm (`N2-1`). So không phân biệt hoa thường, không dấu.
    ///
    /// ⚠ Rỗng là HỢP LỆ — bước sinh từ bản nháp SOP không có dấu hiệu nào (<c>SopDraft</c> không có trường
    /// đó), nên lúc đầu chúng chỉ xong được bằng LỜI XÁC NHẬN. Người duyệt thêm dấu hiệu khi còn DRAFT.
    /// </summary>
    public List<string> DauHieu { get; init; } = [];

    /// <summary>
    /// Chủ đề tri thức mà bước này tra (L1) — khớp nguyên văn với một assertion <c>Applicability</c>.
    /// Tra theo CHỦ ĐỀ chứ không trỏ tới record cụ thể: thêm nguyên nhân thứ 11 không phải sửa quy trình.
    /// </summary>
    public required string ChuDe { get; init; }
}

/// <summary>
/// Một lần chạy một quy trình trên MỘT case (`05` §5). CHỈ có: case · định nghĩa · lúc bắt đầu · bị bỏ chưa.
///
/// 🛑 **KHÔNG CÓ CỘT TIẾN ĐỘ, và đừng thêm** (PR1, `06` §10 ràng buộc 4, `AGENT.md`). Đang ở bước nào là
/// một PHÉP CHIẾU từ evidence (<see cref="TienDoQuyTrinh"/>), không phải dữ liệu. Lưu cờ thì cờ và evidence
/// sẽ lệch nhau — và không ai biết bên nào đúng.
/// </summary>
public sealed class ProcessRun : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required Guid CaseId { get; init; }

    /// <summary>Đúng phiên bản lúc bắt đầu (`N6-1`). Duyệt phiên bản mới KHÔNG kéo run này theo.</summary>
    public required Guid ProcessDefinitionId { get; init; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? AbandonedAt { get; private set; }

    public string? AbandonedBy { get; private set; }

    public string? AbandonReason { get; private set; }

    public bool DaBo => AbandonedAt is not null;

    /// <summary>
    /// Bỏ lượt chạy — CHỈ NGƯỜI (`05`:103: "ABANDONED do người đặt, giống DEPRECATED"). Khớp nhầm quy trình là
    /// chuyện có thật; bỏ rồi thì hệ thống KHÔNG tự dựng lại run cho đúng cặp (case, quy trình) đó.
    /// </summary>
    public void Bo(string nguoiBo, string lyDo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nguoiBo);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyDo);

        if (DaBo)
        {
            throw new InvalidOperationException(
                $"Lượt chạy này đã bị bỏ lúc {AbandonedAt:O} bởi \"{AbandonedBy}\". Bỏ lần hai sẽ xoá tác giả lần đầu.");
        }

        AbandonedAt = DateTimeOffset.UtcNow;
        AbandonedBy = nguoiBo.Trim();
        AbandonReason = lyDo.Trim();
    }
}
