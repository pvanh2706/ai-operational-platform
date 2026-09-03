using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

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
        });

        // --- KnowledgeRecord + cụm assertion (§3C.5) ---
        b.Entity<KnowledgeRecord>(e =>
        {
            e.ToTable("knowledge_record");
            e.HasKey(x => x.Id);
            e.Property(x => x.CauseName).HasMaxLength(512).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Lifecycle).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.VisibilityScope).HasMaxLength(128);

            e.HasMany(x => x.Assertions)
                .WithOne()
                .HasForeignKey(a => a.KnowledgeRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // S7 chỗ chứa thứ ba: ai mở quyền xem, khi nào, từ đâu tới đâu.
            e.OwnsOne(x => x.LastApproval, a =>
            {
                a.Property(p => p.ApprovedByActor).HasColumnName("approved_by_actor").HasMaxLength(256);
                a.Property(p => p.ApprovedAt).HasColumnName("approved_at");
                a.Property(p => p.VisibilityScopeBefore).HasColumnName("visibility_before").HasMaxLength(128);
                a.Property(p => p.VisibilityScopeAfter).HasColumnName("visibility_after").HasMaxLength(128);
                a.Property(p => p.Reason).HasColumnName("approval_reason").HasMaxLength(1024);
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

        base.OnModelCreating(b);
    }
}
