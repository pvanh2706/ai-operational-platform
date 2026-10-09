using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Domain.Documents;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.GoiY;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// AR1: PostgreSQL. AR2: ranh giới tenant thực thi ở tầng DB bằng row-level security.
///
/// HAI TẦNG PHÒNG NGỰ, và thứ tự quan trọng:
///   1. ROW-LEVEL SECURITY trong PostgreSQL  ←  ĐÂY LÀ NGUỒN QUYỀN LỰC (G7)
///      Database tự chặn, kể cả khi lập trình viên quên một câu WHERE.
///   2. Global query filter của EF Core       ←  chỉ là tiện lợi + phát hiện sớm
///      Nó KHÔNG phải ranh giới bảo mật: một câu SQL thô, một view, hay một
///      IgnoreQueryFilters() là đi vòng qua nó ngay.
///
/// Nếu chỉ có tầng 2 thì ranh giới giữa các công ty khách hàng phụ thuộc vào
/// việc KHÔNG LẬP TRÌNH VIÊN NÀO quên — trái G7 ("nền tảng", không được mềm).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<CanonicalCase> Cases => Set<CanonicalCase>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<KnowledgeRecord> KnowledgeRecords => Set<KnowledgeRecord>();
    public DbSet<Assertion> Assertions => Set<Assertion>();
    public DbSet<AssertionEvidence> AssertionEvidenceLinks => Set<AssertionEvidence>();

    /// <summary>
    /// Relation trong schema <c>kp</c> được MIỄN TRỪ khỏi luật "phải có ranh giới tenant".
    ///
    /// ⚠ Đây là danh sách VIẾT TAY, và đó là chủ đích — ngược với
    /// <see cref="TenantScopedTables"/> vốn suy ra từ model. Lý do: cái được bảo vệ thì
    /// nên tự động (quên là bị bắt), còn cái được MIỄN thì phải là một hành động có
    /// người ký tên. Một danh sách miễn trừ tự suy ra là một danh sách tự nới lỏng.
    ///
    /// Kiểu <c>Dictionary</c> chứ không phải <c>HashSet</c> cũng vì thế: thêm một dòng
    /// vào đây buộc phải viết LÝ DO, và lý do đó hiện ra trong diff khi có người review.
    ///
    /// ⚠ Miễn trừ theo TÊN RELATION, không theo kiểu C#. Cố ý: thứ cần miễn trừ có thể
    /// không có entity type nào cả — một view, một materialized view, hay một bảng tạo
    /// bằng SQL thô trong migration. Khoá bằng <c>Type</c> thì đúng những thứ đó lại
    /// không diễn đạt được.
    /// </summary>
    public static IReadOnlyDictionary<string, string> TenantExemptRelations { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tenant"] = "Danh bạ khách hàng — nó LÀ tenant, không thuộc tenant nào. " +
                         "Phải đọc được TRƯỚC khi biết tenant của request, nên không thể tự bảo vệ bằng RLS (`IM-14`).",

            ["knowledge_link_token"] = "Token mở tri thức bằng LINK (`D5-3`). ⚠ CÙNG lý do với api_key, " +
                          "KHÔNG phải lý do của tenant: người bấm link không mang theo thứ gì khác, nên chính " +
                          "token này là thứ quyết định tenant của request. Bù lại: không có DbSet, và đọc nó " +
                          "đi qua đúng một cửa (LinkTokenDirectory).",

            ["api_key"] = "Khoá API của khách (`AR-e`). ⚠ LÝ DO KHÁC hàng trên, đừng đọc gộp: dòng này CÓ " +
                          "thuộc một tenant, nhưng chính nó là thứ QUYẾT ĐỊNH tenant của request — nên nó " +
                          "không thể nằm sau cánh cửa mà nó đang mở. Bù lại: không có DbSet, không endpoint " +
                          "nào trả dòng của bảng này ra, và đọc nó đi qua đúng một cửa (ApiKeyDirectory).",
        };

    /// <summary>
    /// Tên các bảng phải có RLS. Suy ra TỪ MODEL, không phải danh sách viết tay —
    /// một entity mới cài <see cref="ITenantScoped"/> tự động vào danh sách này,
    /// nên không có đường nào thêm entity mà quên RLS.
    /// <see cref="RlsGuard"/> đối chiếu danh sách này với pg_policies lúc khởi động.
    /// </summary>
    public IReadOnlyList<string> TenantScopedTables =>
        Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType))
            .Select(e => e.GetTableName()!)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

    /// <summary>
    /// Mọi <see cref="DateTimeOffset"/> được đưa về UTC TRƯỚC KHI ghi xuống database.
    ///
    /// ⚠ Vì sao cần: Npgsql TỪ CHỐI ghi <c>DateTimeOffset</c> có offset khác 0 vào cột
    /// <c>timestamptz</c> — nó ném <c>ArgumentException</c>, và vì lỗi bật ra từ tận
    /// <c>SaveChangesAsync</c> nên endpoint trả <b>500</b> chứ không phải 400. Tức là
    /// một đầu vào HỢP LỆ theo ISO 8601 bị báo thành lỗi máy chủ.
    ///
    /// <para><b>Bug này chỉ lộ ra khi nạp dữ liệu THẬT (2026-09-04, `IM-24`).</b> Cả 103
    /// test đều xanh vì mọi mốc thời gian trong test đều do chính test dựng ra, và tay
    /// người viết test thì luôn viết UTC. Jira Server thì trả <c>+07:00</c>. Đây là loại
    /// lỗ mà một bộ test tự cấp vật liệu cho mình không bao giờ nhìn thấy.</para>
    ///
    /// Đặt ở đây chứ không ở từng handler, và đó là điểm chính: <c>ConfigureConventions</c>
    /// áp cho MỌI thuộc tính <c>DateTimeOffset</c> của MỌI entity, kể cả entity chưa ai
    /// viết. Sửa ở handler thì mỗi chỗ ghi mới lại phải nhớ một lần nữa — cùng lý do
    /// <see cref="TenantScopedTables"/> suy ra từ model thay vì viết tay.
    ///
    /// KHÔNG mất thông tin: <c>2026-09-01T10:00:00+07:00</c> và <c>2026-09-01T03:00:00Z</c>
    /// là cùng một thời điểm. Thứ mất đi là "múi giờ nào đã ghi nhận" — mà cột
    /// <c>timestamptz</c> của PostgreSQL vốn đã không lưu được điều đó. Nếu sau này có
    /// lúc cần biết múi giờ gốc thì nó phải là một CỘT RIÊNG, không phải một hy vọng
    /// rằng offset sống sót qua tầng lưu trữ.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        b.Properties<DateTimeOffset?>().HaveConversion<UtcDateTimeOffsetConverter>();
        base.ConfigureConventions(b);
    }

    /// <summary>
    /// `VisibilityScope` lưu ở DB dưới DẠNG KEBAB (`noi-bo`), KHÔNG phải tên C# (`NoiBo`).
    ///
    /// 🛑 Vì sao không dùng `HasConversion&lt;string&gt;()` như các enum khác: nó lưu TÊN C#.
    /// Cột `visibility_scope` đã có **7 dòng mang `noi-bo`** từ đợt build 2026-09-13, và
    /// tập giá trị chốt ở `AR-a` cũng viết dạng kebab. Dùng converter mặc định là làm 7
    /// dòng đó không đọc lại được — và nó sẽ hỏng ở lúc ĐỌC, tức xa chỗ gây ra.
    /// </summary>
    private static readonly ValueConverter<VisibilityScope, string> PhamViXemConverter =
        new(v => v.ToWire(), s => DocPhamViXem(s));

    /// <summary>
    /// ⚠ NÉM khi gặp giá trị lạ, KHÔNG âm thầm về một mặc định nào. Một nhãn quyền xem đọc
    /// sai là một tri thức hiện ra cho nhầm người — fail loud là lựa chọn duy nhất đúng ở đây.
    /// </summary>
    private static VisibilityScope DocPhamViXem(string raw) =>
        VisibilityScopes.TryParse(raw, out var v)
            ? v
            : throw new InvalidOperationException(
                $"Cột phạm vi xem chứa giá trị không nằm trong tập đã chốt: \"{raw}\". " +
                $"Nhận: {VisibilityScopes.Allowed}. Dữ liệu này có trước khi AR-a khoá tập giá trị?");

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("kp");

        // --- Tenant: bảng gốc, KHÔNG tenant-scoped (nó LÀ tenant) ---
        b.Entity<Tenant>(e =>
        {
            e.ToTable("tenant");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.ExternalKey).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.ExternalKey).IsUnique();
        });

        // --- ApiKey: CÓ TenantId nhưng KHÔNG cài ITenantScoped, và đó là chủ đích ---
        // Xem TenantExemptRelations["api_key"] cho lý do đầy đủ. Cố ý KHÔNG có DbSet:
        // entity vẫn nằm trong model (nên migration sinh ra bình thường và RlsGuard vẫn
        // thấy bảng), nhưng không có `db.ApiKeys` để ai đó lỡ tay truy vấn — một bảng
        // miễn RLS mà lại tiện query là cách tạo ra đường rò không ai cố ý viết.
        b.Entity<ApiKey>(e =>
        {
            e.ToTable("api_key");
            e.HasKey(x => x.Id);
            e.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
            e.Property(x => x.SecretHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.Label).HasMaxLength(256).IsRequired();

            // Tra theo KeyId là đường NÓNG của mọi request có xác thực -> phải là index,
            // và duy nhất vì KeyId là thứ định danh dòng.
            e.HasIndex(x => x.KeyId).IsUnique();

            // Xoá tenant thì khoá của nó đi theo. Không có đường nào để một khoá mồ côi
            // trỏ vào một tenant không còn tồn tại.
            e.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- KnowledgeLinkToken: CÓ TenantId, KHÔNG cài ITenantScoped — xem TenantExemptRelations ---
        b.Entity<KnowledgeLinkToken>(e =>
        {
            e.ToTable("knowledge_link_token");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenId).HasMaxLength(64).IsRequired();
            e.Property(x => x.SecretHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();

            e.HasIndex(x => x.TokenId).IsUnique();

            // Thu hồi theo record (khi bản tri thức bị sửa) tra bằng cột này.
            e.HasIndex(x => x.KnowledgeRecordId);

            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        // --- Định tuyến do NGƯỜI nạp (vòng không-credit) — tenant-scoped, CÓ RLS ---
        b.Entity<DinhTuyenDaNap>(e =>
        {
            e.ToTable("dinh_tuyen_da_nap");
            e.HasKey(x => x.Id);
            e.Property(x => x.KhoaNguon).HasMaxLength(512).IsRequired();
            e.Property(x => x.Nhom).HasMaxLength(512).IsRequired();
            e.Property(x => x.Nguon).HasMaxLength(128).IsRequired();
            e.Property(x => x.GiaiThich).HasMaxLength(2048);
            e.Property(x => x.NguoiNap).HasMaxLength(256).IsRequired();

            // Một khoá nguồn có ĐÚNG một kết quả trong mỗi tenant. Nạp lại thì ĐÈ, cố ý:
            // người nạp lại là người đang sửa một kết quả họ thấy sai — khác hẳn `K-B3`
            // (không ghi đè QUÁ KHỨ của tri thức đã duyệt). Đây là bộ nhớ thao tác, không
            // phải tri thức.
            e.HasIndex(x => new { x.TenantId, x.KhoaNguon }).IsUnique();
        });

        // --- Bản nháp SOP GỐC (P3, 2026-10-10) — tenant-scoped, CÓ RLS. Xem SopDraftGoc ---
        b.Entity<SopDraftGoc>(e =>
        {
            e.ToTable("sop_draft_goc");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nhom).HasMaxLength(512).IsRequired();
            e.Property(x => x.Hash).HasMaxLength(64).IsRequired();
            e.Property(x => x.NoiDung).IsRequired();

            // Nạp lại cùng một bản nháp KHÔNG sinh dòng thứ hai — cùng luật `IM-20`.
            e.HasIndex(x => new { x.TenantId, x.Hash }).IsUnique();
        });

        // --- P8: biên lai + bộ nhớ đệm của định tuyến bằng model (N7-1) — tenant-scoped, CÓ RLS ---
        b.Entity<LuotDinhTuyenModel>(e =>
        {
            e.ToTable("luot_dinh_tuyen_model");
            e.HasKey(x => x.Id);
            e.Property(x => x.KhoaNguon).HasMaxLength(512);
            e.Property(x => x.HashDauVao).HasMaxLength(64).IsRequired();
            e.Property(x => x.Nhom).HasMaxLength(512);
            e.Property(x => x.Nguon).HasMaxLength(64);
            e.Property(x => x.GiaiThich).HasMaxLength(2048);
            e.Property(x => x.Loi).HasMaxLength(1000);

            e.HasIndex(x => new { x.TenantId, x.Luc });          // đếm trần tháng
            e.HasIndex(x => new { x.TenantId, x.KhoaNguon });    // đệm theo case
            e.HasIndex(x => new { x.TenantId, x.HashDauVao });   // đệm theo nội dung
        });

        // --- Biên lai mỗi lượt gọi model (E3-1) — tenant-scoped, CÓ RLS ---
        b.Entity<LuotSoanNhap>(e =>
        {
            e.ToTable("luot_soan_nhap");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nhom).HasMaxLength(512).IsRequired();
            e.Property(x => x.NguoiYeuCau).HasMaxLength(256).IsRequired();
            e.Property(x => x.Loi).HasMaxLength(1000);

            // Đường NÓNG: đếm lượt của tháng này để áp trần.
            e.HasIndex(x => new { x.TenantId, x.Luc });
        });

        // --- Kênh 2: siêu dữ liệu tài liệu. Nội dung nằm ở kho file (AR5) ---
        b.Entity<Document>(e =>
        {
            e.ToTable("document");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenKhaiBao).HasMaxLength(512).IsRequired();
            e.Property(x => x.LoaiNoiDung).HasMaxLength(256).IsRequired();
            e.Property(x => x.MaKho).HasMaxLength(64).IsRequired();
            e.Property(x => x.Hash).HasMaxLength(64).IsRequired();
            e.Property(x => x.SubTenantCode).HasMaxLength(SubTenantCodes.MaxLength).IsRequired();
            e.Property(x => x.NguoiNap).HasMaxLength(256).IsRequired();

            // Nạp lại cùng một file KHÔNG sinh bản thứ hai — cùng luật `IM-20` ở đường tín hiệu.
            e.HasIndex(x => new { x.TenantId, x.Hash }).IsUnique();
        });

        // --- Webhook: CÓ RLS (tenant-scoped) — khác hai bảng trên, xem WebhookEndpoint ---
        b.Entity<WebhookEndpoint>(e =>
        {
            e.ToTable("webhook_endpoint");
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.SigningSecret).HasMaxLength(256).IsRequired();

            // Một khách MỘT đích gửi, cho tới khi có nhu cầu đo được là cần nhiều.
            e.HasIndex(x => x.TenantId).IsUnique();
        });

        b.Entity<WebhookDelivery>(e =>
        {
            e.ToTable("webhook_delivery");
            e.HasKey(x => x.Id);
            e.Property(x => x.CaseSourceReference).HasMaxLength(512).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(1024).IsRequired();
            e.Property(x => x.LastError).HasMaxLength(1000);
            e.Property(x => x.State).HasConversion<int>();
            e.Property(x => x.SuKien).HasMaxLength(32);
            e.Property(x => x.KhoaKhuTrung).HasMaxLength(128);

            // P7: một lần đổi bước = TỐI ĐA một thông báo, kể cả khi hai lô cùng làm đổi bước đó cùng lúc.
            e.HasIndex(x => new { x.TenantId, x.KhoaKhuTrung }).IsUnique().HasFilter("\"KhoaKhuTrung\" IS NOT NULL");

            // Đường NÓNG của vòng quét: "còn chờ, và đã tới hạn".
            e.HasIndex(x => new { x.TenantId, x.State, x.NextAttemptAt });
        });

        // --- Case: bản MỎNG cố ý, xem CanonicalCase ---
        b.Entity<CanonicalCase>(e =>
        {
            e.ToTable("canonical_case");
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(1024).IsRequired();
            e.Property(x => x.SourceReference).HasMaxLength(512).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.SourceReference }).IsUnique();

            // Path A cần "tìm N case liên quan" → index để tìm theo chủ đề.
            // AR4: Postgres full-text search trước, pgvector khi ĐO ĐƯỢC là không đủ.
            e.HasIndex(x => new { x.TenantId, x.SourceResolvedAt });
        });

        // --- Evidence ---
        b.Entity<EvidenceItem>(e =>
        {
            e.ToTable("evidence_item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.SourceReference).HasMaxLength(512).IsRequired();
            e.Property(x => x.MachineReadability).HasConversion<string>().HasMaxLength(16);

            // K-B9: ObservedInCaseId NULL là HỢP LỆ và quan trọng — một email của
            // senior hay tin Zalo không thuộc case nào. Không đặt required ở đây.
            e.HasIndex(x => new { x.TenantId, x.ObservedInCaseId });

            // Chống trùng, GIỐNG HỆT canonical_case và vì cùng một lý do: webhook gửi
            // lại, job đồng bộ chạy lại, bên gửi retry. Thiếu index này thì cùng một
            // comment Jira gửi mười lần ra mười dòng, và bản nháp gom của Path A sẽ
            // đếm một quan sát thành mười — hỏng đúng chỗ `S8` cần chính xác nhất
            // ("14/20 case đã làm bước này").
            e.HasIndex(x => new { x.TenantId, x.SourceReference }).IsUnique();

            // --- AR-l: mã khách sạn do BÊN GỬI khai ---
            //
            // ⚠ Cột NULL được, dù hợp đồng nạp BẮT BUỘC khai. Không phải nới lỏng: NULL ở
            // đây là một GIÁ TRỊ CÓ NGHĨA — "mẩu này nạp trước khi có luật", khác hẳn
            // `khong-xac-dinh` ("đã được hỏi, chưa biết"). Backfill một sentinel cho 128 mẩu
            // cũ sẽ xoá đúng khác biệt đó, và tạo lại đúng hình dạng lỗi mà `Mã khách sạn =
            // -1.0` (32/32) đã dạy. Lý do đầy đủ ở `EvidenceItem.SubTenantCode`.
            e.Property(x => x.SubTenantCode).HasMaxLength(SubTenantCodes.MaxLength);

            // Index để trả lời ĐIỀU KIỆN MỞ LẠI của `AR-l` ("≥2 giá trị PHÂN BIỆT") mà không
            // quét cả bảng. Câu hỏi đó phải chạy được, không phải là một dòng trong lịch.
            e.HasIndex(x => new { x.TenantId, x.SubTenantCode });

            // --- AR-m: dấu ĐÃ BỊ NGƯỜI GỬI RÚT LẠI ---
            //
            // Ba cột cùng NULL hoặc cùng có giá trị — `EvidenceItem.RutLai` là đường DUY NHẤT
            // đặt chúng, và nó đặt cả ba một lượt. Không đặt ràng buộc CHECK ở DB cho quan hệ
            // đó: luật đã nằm ở domain, và một luật ở hai chỗ là hai chỗ phải sửa (đúng dư
            // chấn mà `IM-22` ghi lại về biểu thức policy).
            e.Property(x => x.RetractedReason).HasMaxLength(1024);
            e.Property(x => x.RetractedBy).HasMaxLength(256);

            // --- P4: origin của mẩu + lời xác nhận bước quy trình (N2-1) ---
            //
            // ⚠ NULL được ở CẢ HAI cột, và NULL có nghĩa riêng: mẩu nạp trước luật / mẩu không phải lời
            //   xác nhận. Không điền bù — cùng bài học subTenantCode.
            e.Property(x => x.Origin).HasConversion<string>().HasMaxLength(32);

            // Restrict: bước đã có người xác nhận thì không xoá được — bước của quy trình ACTIVE vốn bất
            // biến (N5-1), nên chỉ đụng tới khi ai đó cố xoá tay dưới database.
            e.HasOne<ProcessStep>()
                .WithMany()
                .HasForeignKey(x => x.XacNhanProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- P4 (2026-10-10, mở lại I1): quy trình · bước · lượt chạy — cả ba tenant-scoped, CÓ RLS ---
        b.Entity<ProcessDefinition>(e =>
        {
            e.ToTable("process_definition");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nhom).HasMaxLength(512).IsRequired();
            e.Property(x => x.Ten).HasMaxLength(512).IsRequired();
            e.Property(x => x.Lifecycle).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.VisibilityScope).HasConversion(PhamViXemConverter).HasMaxLength(128);
            e.Property(x => x.Origin).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.ActorLabel).HasMaxLength(256);

            // Cùng hình dạng lượt duyệt với knowledge_record — `05` §5: Process dùng CHUNG nhân.
            e.OwnsOne(x => x.LastApproval, a =>
            {
                a.Property(p => p.ApprovedByActor).HasColumnName("approved_by_actor").HasMaxLength(256);
                a.Property(p => p.ApprovedAt).HasColumnName("approved_at");
                a.Property(p => p.VisibilityScopeBefore).HasConversion(PhamViXemConverter).HasColumnName("visibility_before").HasMaxLength(128);
                a.Property(p => p.VisibilityScopeAfter).HasConversion(PhamViXemConverter).HasColumnName("visibility_after").HasMaxLength(128);
                a.Property(p => p.Reason).HasColumnName("approval_reason").HasMaxLength(1024);
                a.Property(p => p.CommercialDataConfirmed).HasColumnName("commercial_data_confirmed");
            });

            e.HasMany(x => x.Buoc)
                .WithOne()
                .HasForeignKey(s => s.ProcessDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.TenantId, x.Nhom, x.PhienBan }).IsUnique();

            // 🛑 Một nhóm có TỐI ĐA MỘT quy trình ACTIVE. Chốt ở database chứ không chỉ ở handler: duyệt hai
            //   phiên bản cùng lúc là kiểm-rồi-ghi không nguyên tử, và hai bản ACTIVE thì ô D không biết khớp bản nào.
            e.HasIndex(x => new { x.TenantId, x.Nhom }).IsUnique().HasFilter("\"Lifecycle\" = 'Active'");

            // Một bản nháp SOP sinh ĐÚNG MỘT quy trình — nạp lại bản nháp không đẻ phiên bản mới.
            e.HasIndex(x => new { x.TenantId, x.SopDraftGocId }).IsUnique().HasFilter("\"SopDraftGocId\" IS NOT NULL");
        });

        b.Entity<ProcessStep>(e =>
        {
            e.ToTable("process_step");
            e.HasKey(x => x.Id);
            e.Property(x => x.Ma).HasMaxLength(64).IsRequired();
            e.Property(x => x.NoiDung).IsRequired();
            e.Property(x => x.NoiXem).HasMaxLength(1024);
            e.Property(x => x.ChuDe).HasMaxLength(1024).IsRequired();

            e.HasIndex(x => new { x.ProcessDefinitionId, x.ThuTu }).IsUnique();
            e.HasIndex(x => new { x.ProcessDefinitionId, x.Ma }).IsUnique();
        });

        b.Entity<ProcessRun>(e =>
        {
            e.ToTable("process_run");
            e.HasKey(x => x.Id);
            e.Property(x => x.AbandonedBy).HasMaxLength(256);
            e.Property(x => x.AbandonReason).HasMaxLength(1024);

            e.HasOne<CanonicalCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProcessDefinition>().WithMany().HasForeignKey(x => x.ProcessDefinitionId).OnDelete(DeleteBehavior.Restrict);

            // 🛑 Một case có TỐI ĐA MỘT lượt chạy CHƯA BỎ. Hai lượt mở cùng lúc thì "đang ở bước nào" có hai đáp án.
            e.HasIndex(x => new { x.TenantId, x.CaseId }).IsUnique().HasFilter("\"AbandonedAt\" IS NULL");
            e.HasIndex(x => new { x.TenantId, x.CaseId, x.ProcessDefinitionId });
        });

        // --- P6 (2026-10-10): ô J — lượt gợi ý + các nấc L2. Tenant-scoped, CÓ RLS ---
        b.Entity<LuotGoiY>(e =>
        {
            e.ToTable("luot_goi_y");
            e.HasKey(x => x.Id);
            e.Property(x => x.KenhRa).HasMaxLength(16).IsRequired();
            e.Property(x => x.KhoaNguonCase).HasMaxLength(512);
            e.Property(x => x.Nhom).HasMaxLength(512);
            e.Property(x => x.NguonDinhTuyen).HasMaxLength(256).IsRequired();
            e.Property(x => x.LyDoKhongBiet).HasMaxLength(128);
            e.Property(x => x.MaBuoc).HasMaxLength(64);
            e.Property(x => x.NoiDung).IsRequired();

            e.HasMany(x => x.SuKien)
                .WithOne()
                .HasForeignKey(s => s.LuotGoiYId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.TenantId, x.Luc });
            e.HasIndex(x => new { x.TenantId, x.CaseId });
        });

        b.Entity<LuotGoiYSuKien>(e =>
        {
            e.ToTable("luot_goi_y_su_kien");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nac).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.NguoiKhai).HasMaxLength(256);
            e.Property(x => x.GhiChu).HasMaxLength(2048);
            e.Property(x => x.NhomDung).HasMaxLength(512);

            e.HasIndex(x => new { x.TenantId, x.Nac });
        });

        // --- KnowledgeRecord + cụm assertion (§3C.5) ---
        b.Entity<KnowledgeRecord>(e =>
        {
            e.ToTable("knowledge_record");
            e.HasKey(x => x.Id);
            e.Property(x => x.CauseName).HasMaxLength(512).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Lifecycle).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.VisibilityScope).HasConversion(PhamViXemConverter).HasMaxLength(128);

            e.HasMany(x => x.Assertions)
                .WithOne()
                .HasForeignKey(a => a.KnowledgeRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // S7 chỗ chứa thứ ba: ai mở quyền xem, khi nào, từ đâu tới đâu.
            e.OwnsOne(x => x.LastApproval, a =>
            {
                a.Property(p => p.ApprovedByActor).HasColumnName("approved_by_actor").HasMaxLength(256);
                a.Property(p => p.ApprovedAt).HasColumnName("approved_at");
                a.Property(p => p.VisibilityScopeBefore).HasConversion(PhamViXemConverter).HasColumnName("visibility_before").HasMaxLength(128);
                a.Property(p => p.VisibilityScopeAfter).HasConversion(PhamViXemConverter).HasColumnName("visibility_after").HasMaxLength(128);
                a.Property(p => p.Reason).HasColumnName("approval_reason").HasMaxLength(1024);

                // AR-n (B2b-1). Cột NULL được: NULL = duyệt TRƯỚC khi có luật này, khác hẳn
                // false. Backfill `true` cho 8 bản ghi cũ sẽ là bịa ra một lời khai mà không
                // ai từng nói — đúng thứ AP3 cấm.
                a.Property(p => p.CommercialDataConfirmed).HasColumnName("commercial_data_confirmed");
            });

            e.HasIndex(x => new { x.TenantId, x.Lifecycle });
        });

        // --- Assertion: chỗ AP3 + T4 sống. Xem cảnh báo trong Assertion.cs ---
        b.Entity<Assertion>(e =>
        {
            e.ToTable("assertion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Content).IsRequired();

            // AP3: origin BẮT BUỘC, và giữ vĩnh viễn (K-B5, v0.2 §7.4).
            // Không có giá trị mặc định — buộc nơi tạo phải nói rõ nó đến từ đâu.
            e.Property(x => x.Origin).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.ActorLabel).HasMaxLength(256);

            // T4: verification riêng từng assertion, BẮT BUỘC.
            e.Property(x => x.Verification).HasConversion<string>().HasMaxLength(16).IsRequired();

            e.HasMany(x => x.EvidenceLinks)
                .WithOne()
                .HasForeignKey(l => l.AssertionId)
                .OnDelete(DeleteBehavior.Cascade);

            // M2: bản gốc của AI không bị ghi đè — nó bị trỏ là "đã bị thay".
            e.HasIndex(x => new { x.TenantId, x.KnowledgeRecordId, x.ReplacedByAssertionId });
        });

        // --- L3: liên kết CÓ THUỘC TÍNH, nên là entity chứ không phải quan hệ trơn ---
        b.Entity<AssertionEvidence>(e =>
        {
            e.ToTable("assertion_evidence");
            e.HasKey(x => x.Id);
            e.Property(x => x.Relation).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.Note).HasMaxLength(1024);
            e.HasIndex(x => new { x.AssertionId, x.EvidenceItemId }).IsUnique();

            // 🛑 KHOÁ NGOẠI GHÉP (TenantId, EvidenceItemId) → evidence_item (TenantId, Id), có từ
            //   2026-10-10 (P3). Hai lý do, lý do thứ hai mới là lý do thật:
            //   1. Trước hôm nay bảng này KHÔNG có khoá ngoại sang evidence_item. Thực hành "xoá
            //      evidence rồi nạp lại" (`IM-36`) chỉ an toàn vì có 0 liên kết — có liên kết rồi
            //      thì xoá là để lại dòng mồ côi, im lặng. Restrict biến chuyện đó thành một lỗi.
            //   2. RLS `WITH CHECK` chỉ kiểm TenantId của CHÍNH dòng đang ghi, không kiểm dòng nó
            //      TRỎ TỚI. Khoá ngoại một cột thì một liên kết của khách A trỏ được vào mẩu của
            //      khách B. Ghép cả TenantId vào khoá thì database tự chặn.
            e.HasOne(x => x.EvidenceItem)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.EvidenceItemId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================================
        //  Tầng phòng ngự 2 — global query filter.
        //  KHÔNG phải ranh giới bảo mật (xem chú thích đầu class). RLS là nguồn
        //  quyền lực; cái này bắt lỗi sớm trong dev và giảm lặp code.
        // =====================================================================
        b.Entity<CanonicalCase>().HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
        b.Entity<EvidenceItem>().HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
        b.Entity<KnowledgeRecord>().HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
        b.Entity<Assertion>().HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
        b.Entity<AssertionEvidence>().HasQueryFilter(x => x.TenantId == tenantContext.TenantId);

        // =====================================================================
        //  KHOÁ CHÍNH DO CODE SINH, KHÔNG PHẢI DATABASE — và EF phải được NÓI điều đó.
        //
        //  🛑 Mọi entity ở đây đặt `Id` ngay trong bộ khởi tạo (`Guid.CreateVersion7()`).
        //  Nhưng theo QUY ƯỚC của EF, khoá chính kiểu Guid là `ValueGenerated.OnAdd` — và
        //  với khoá OnAdd đã có giá trị khác mặc định, EF suy ra entity đó ĐÃ TỒN TẠI.
        //
        //  Hệ quả đã đo (2026-09-13, bộ test API bắt): thêm một Assertion vào một
        //  KnowledgeRecord ĐANG ĐƯỢC THEO DÕI thì EF gắn nó là `Modified` chứ không phải
        //  `Added`, rồi sinh một câu UPDATE vào một dòng KHÔNG TỒN TẠI →
        //  `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`.
        //
        //  ⚠ Vì sao nó không lộ ra sớm hơn: đường nạp bản nháp `Add()` NGUYÊN CẢ ĐỒ THỊ một
        //  lần, và khi đó cả cụm được gắn `Added` nên không ai thấy gì. Lỗi chỉ hiện ra ở
        //  chỗ ĐẦU TIÊN thêm con vào một cha đã nạp — tức nó nằm chờ sẵn cho mọi đường
        //  tương lai, không riêng đường sửa. Thông điệp lỗi lại nói về "concurrency",
        //  trỏ hoàn toàn sai chỗ.
        //
        //  ⚠ Luật SUY TỪ MODEL, không phải danh sách viết tay — cùng lý do `RlsGuard` suy
        //  danh sách bảng từ model (`IM-7`): thêm entity mới mà quên thêm vào danh sách thì
        //  cái bẫy này lặng lẽ quay lại.
        // =====================================================================
        foreach (var khoa in b.Model.GetEntityTypes()
                     .Select(t => t.FindPrimaryKey())
                     .Where(k => k is not null)
                     .SelectMany(k => k!.Properties)
                     .Where(p => p.ClrType == typeof(Guid)))
        {
            khoa.ValueGenerated = ValueGenerated.Never;
        }

        base.OnModelCreating(b);
    }
}
