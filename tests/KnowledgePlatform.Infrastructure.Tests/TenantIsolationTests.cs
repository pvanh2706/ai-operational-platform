using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Ranh giới giữa các công ty khách hàng, kiểm trên PostgreSQL thật.
///
/// `G7` gọi tenant boundary là NỀN TẢNG. Trước những test này, bằng chứng duy
/// nhất cho nền tảng đó là "SQL sinh ra trông đúng" — và `IM-5` tồn tại chính vì
/// RLS có một cái bẫy làm nó bật mà không chặn gì, không báo lỗi. Đọc SQL không
/// phát hiện được loại lỗi đó; chỉ chạy thật mới phát hiện được.
///
/// Mỗi test dùng TenantId riêng nên không test nào nhìn thấy dữ liệu của test
/// khác. Cả file cố ý nằm trong MỘT class: test tắt RLS bên dưới có đổi schema
/// tạm thời, và xUnit chỉ chạy song song GIỮA các class.
/// </summary>
public sealed class TenantIsolationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _db;

    public TenantIsolationTests(TestDatabaseFixture db) => _db = db;

    private static CanonicalCase NewCase(Guid tenantId, string subject) => new()
    {
        TenantId = tenantId,
        Subject = subject,
        SourceReference = $"test:{Guid.CreateVersion7()}",
    };

    // =====================================================================
    //  Test số 0 — kiểm chính BỘ TEST này có ý nghĩa hay không
    // =====================================================================

    /// <summary>
    /// Nếu test chạy bằng superuser thì MỌI test còn lại trong file PASS GIẢ:
    /// superuser đi vòng qua row-level security, kể cả khi có FORCE. Test này
    /// đứng đầu để cái bẫy đó không im lặng.
    ///
    /// Nó còn khẳng định role đang chạy LÀ chủ sở hữu bảng — tức là các test
    /// dưới đang kiểm đúng ca mà `IM-5` nhắm tới, không phải ca dễ.
    /// </summary>
    [Fact]
    public async Task Role_chay_test_phai_bi_RLS_rang_buoc()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        var privileged = await db.Database
            .SqlQueryRaw<bool>(
                "SELECT (rolsuper OR rolbypassrls) AS \"Value\" " +
                "FROM pg_roles WHERE rolname = current_user")
            .FirstAsync();

        Assert.False(privileged,
            "Role đang chạy test là superuser hoặc có BYPASSRLS → row-level security " +
            "không có tác dụng và mọi test cách ly tenant trong file này vô nghĩa. " +
            "Xem scripts/dev-db-setup.sql.");

        var isOwner = await db.Database
            .SqlQueryRaw<bool>(
                "SELECT (pg_get_userbyid(c.relowner) = current_user) AS \"Value\" " +
                "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace " +
                "WHERE n.nspname = 'kp' AND c.relname = 'canonical_case'")
            .FirstAsync();

        Assert.True(isOwner,
            "Role đang chạy test không phải chủ sở hữu bảng, nên bộ test này KHÔNG " +
            "kiểm được điều mà IM-5 nhắm tới: chủ sở hữu bảng vẫn phải bị policy chặn.");
    }

    // =====================================================================
    //  Cách ly giữa hai khách hàng
    // =====================================================================

    [Fact]
    public async Task Moi_tenant_chi_doc_duoc_du_lieu_cua_chinh_no()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            dbA.Cases.Add(NewCase(a, "Case của khách A"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _db.NewContext(new FixedTenantContext(b)))
        {
            dbB.Cases.Add(NewCase(b, "Case của khách B"));
            await dbB.SaveChangesAsync();
        }

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            var subjects = await dbA.Cases.Select(c => c.Subject).ToListAsync();
            Assert.Equal(["Case của khách A"], subjects);
        }

        await using (var dbB = _db.NewContext(new FixedTenantContext(b)))
        {
            var subjects = await dbB.Cases.Select(c => c.Subject).ToListAsync();
            Assert.Equal(["Case của khách B"], subjects);
        }
    }

    /// <summary>
    /// Test QUAN TRỌNG NHẤT của file. Nó mô phỏng đúng lỗi mà `AR2` nói global
    /// query filter không đỡ được: một câu SQL thô, quên điều kiện tenant.
    ///
    /// Nếu ranh giới tenant chỉ là filter của EF Core thì câu này đọc được dữ
    /// liệu của cả hai khách hàng và KHÔNG có gì báo. RLS làm nó không xảy ra được.
    /// </summary>
    [Fact]
    public async Task SQL_tho_quen_dieu_kien_tenant_van_bi_RLS_chan()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            dbA.Cases.AddRange(NewCase(a, "A-1"), NewCase(a, "A-2"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _db.NewContext(new FixedTenantContext(b)))
        {
            dbB.Cases.AddRange(NewCase(b, "B-1"), NewCase(b, "B-2"), NewCase(b, "B-3"));
            await dbB.SaveChangesAsync();
        }

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            Assert.Equal(2, await TestDatabaseFixture.CountCasesWithoutTenantFilterAsync(dbA));
        }

        await using (var dbB = _db.NewContext(new FixedTenantContext(b)))
        {
            Assert.Equal(3, await TestDatabaseFixture.CountCasesWithoutTenantFilterAsync(dbB));
        }
    }

    /// <summary>`WITH CHECK` của policy: không ghi được dữ liệu mang tenant của người khác.</summary>
    [Fact]
    public async Task Ghi_du_lieu_mang_TenantId_cua_khach_khac_bi_tu_choi()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();

        await using var dbA = _db.NewContext(new FixedTenantContext(a));

        // Cố ý bỏ qua mọi kiểm tra ở tầng C#: gán thẳng TenantId của khách khác.
        dbA.Cases.Add(NewCase(b, "Ghi lậu sang khách B"));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("42501", pg.SqlState);  // insufficient_privilege
        Assert.Contains("row-level security", pg.MessageText);
    }

    // =====================================================================
    //  Mặc định khi KHÔNG có tenant — phải nghiêng về phía không thấy gì
    // =====================================================================

    /// <summary>
    /// `IM-6`: quên đặt tenant thì thấy 0 dòng, KHÔNG phải thấy hết.
    ///
    /// Dùng SQL thô vì global query filter của EF ném trước khi tới được database
    /// (đọc <c>TenantId</c> lúc chưa xác định là ném — đúng thiết kế). Đường thật
    /// cần chặn là đường đi vòng qua filter đó.
    /// </summary>
    [Fact]
    public async Task Chua_xac_dinh_duoc_tenant_thi_thay_0_dong_khong_phai_thay_het()
    {
        var a = Guid.CreateVersion7();

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            dbA.Cases.Add(NewCase(a, "Dữ liệu của A"));
            await dbA.SaveChangesAsync();
        }

        await using var db = _db.NewContext(new UnresolvedTenantContext());
        Assert.Equal(0, await TestDatabaseFixture.CountCasesWithoutTenantFilterAsync(db));
    }

    /// <summary>
    /// Hồi quy cho `IM-9`.
    ///
    /// Policy đầu tiên viết <c>current_setting('app.current_tenant', true)::uuid</c>.
    /// Khi biến session là CHUỖI RỖNG — chuyện xảy ra ngay sau một <c>RESET</c>,
    /// tức là chuyện connection pool làm — thì <c>''::uuid</c> NÉM LỖI ép kiểu
    /// thay vì trả 0 dòng. Không rò rỉ, nhưng thông báo lỗi không hề nhắc tới
    /// tenant nên rất tốn thời gian truy.
    ///
    /// Test này đặt chuỗi rỗng tường minh và đòi 0 dòng. Bỏ <c>nullif</c> khỏi
    /// policy là test này đỏ ngay.
    /// </summary>
    [Fact]
    public async Task Tenant_la_chuoi_rong_thi_thay_0_dong_chu_khong_nem_loi_ep_kieu()
    {
        var a = Guid.CreateVersion7();

        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            dbA.Cases.Add(NewCase(a, "Dữ liệu của A"));
            await dbA.SaveChangesAsync();
        }

        await using var db = _db.NewContext(new FixedTenantContext(a));
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET app.current_tenant = ''");

        Assert.Equal(0, await TestDatabaseFixture.CountCasesWithoutTenantFilterAsync(db));
    }

    /// <summary>
    /// Connection lấy lại từ pool KHÔNG được thừa hưởng tenant của lượt dùng
    /// trước. Đây là lý do <see cref="TenantConnectionInterceptor"/> ghi biến
    /// session TRONG MỌI TRƯỜNG HỢP, kể cả khi chưa xác định được tenant.
    /// </summary>
    [Fact]
    public async Task Connection_lay_lai_tu_pool_khong_thua_huong_tenant_cua_luot_truoc()
    {
        var a = Guid.CreateVersion7();

        // Lượt 1: mở, ghi, đóng → connection về pool sau khi đã đặt tenant A.
        await using (var dbA = _db.NewContext(new FixedTenantContext(a)))
        {
            dbA.Cases.Add(NewCase(a, "Dữ liệu của A"));
            await dbA.SaveChangesAsync();
        }

        // Lượt 2: cùng chuỗi kết nối nên rất có thể lấy lại đúng connection đó,
        // nhưng lần này không có tenant. Lặp vài lần để tăng khả năng trúng lại
        // connection cũ trong pool.
        for (var i = 0; i < 5; i++)
        {
            await using var db = _db.NewContext(new UnresolvedTenantContext());
            Assert.Equal(0, await TestDatabaseFixture.CountCasesWithoutTenantFilterAsync(db));
        }
    }

    // =====================================================================
    //  RlsGuard — cơ chế chặn "thêm bảng mà quên bảo mật tenant"
    // =====================================================================

    [Fact]
    public async Task RlsGuard_khong_nem_khi_moi_bang_tenant_scoped_deu_duoc_bao_ve()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        Assert.Equal(
            ["assertion", "assertion_evidence", "canonical_case", "evidence_item", "knowledge_record"],
            db.TenantScopedTables);

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// `IM-7` chỉ có giá trị nếu nó THẬT SỰ ném. Test này tắt RLS của một bảng,
    /// đòi guard ném, và đòi thông báo lỗi chỉ rõ TÊN BẢNG — người đọc lỗi lúc
    /// 2 giờ sáng cần biết sửa ở đâu, không chỉ cần biết có gì đó sai.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_va_chi_ro_bang_nao_khi_mot_bang_bi_tat_RLS()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync("ALTER TABLE kp.evidence_item DISABLE ROW LEVEL SECURITY");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));
            Assert.Contains("evidence_item", ex.Message);
            Assert.DoesNotContain("canonical_case", ex.Message);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE kp.evidence_item ENABLE ROW LEVEL SECURITY");
        }

        // Trả về nguyên trạng rồi kiểm lại — để test này không để lại schema hỏng
        // cho các test sau, và để chứng minh nó đã dọn xong.
        await RlsGuard.VerifyAsync(db);
    }

    // =====================================================================
    //  RlsGuard — năm cách ranh giới tenant mất mà bản guard CŨ vẫn báo xanh
    //
    //  Cả năm đều là fail-open: dữ liệu rò hoặc không được bảo vệ, mà cơ chế
    //  canh gác lại nói "ổn". Bản cũ chỉ hỏi "có tồn tại policy nào không", nên
    //  nó mù với cả năm. Xem chú thích đầu RlsGuard.cs.
    // =====================================================================

    /// <summary>
    /// ⚠ CA QUAN TRỌNG NHẤT — đã đo trên PostgreSQL 18 trước khi viết test này:
    /// thêm một policy thứ hai <c>USING (true)</c> thì khách A đọc được dòng của
    /// khách B, trong khi guard bản cũ vẫn báo XANH.
    ///
    /// Lý do: policy trong PostgreSQL mặc định là PERMISSIVE, tức gộp bằng <b>OR</b>.
    /// Policy thứ hai chỉ NỚI ra, không siết vào — nên ranh giới bằng policy lỏng
    /// nhất, không phải chặt nhất.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_khi_co_policy_thu_hai_permissive()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync(
            "CREATE POLICY mo_toang ON kp.canonical_case USING (true)");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));

            Assert.Contains("canonical_case", ex.Message);
            Assert.Contains("2 policy", ex.Message);
            // Thông báo phải nói VÌ SAO hai policy là hỏng, không chỉ nói là hỏng.
            Assert.Contains("OR", ex.Message);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP POLICY mo_toang ON kp.canonical_case");
        }

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// `IM-9` tái phát: một bảng mới chép policy từ migration ĐẦU TIÊN — bản chưa có
    /// <c>nullif</c>. Lúc đó biến session đã từng được đặt rồi <c>RESET</c> (đúng việc
    /// connection pool làm) sẽ là chuỗi RỖNG, và <c>''::uuid</c> ném một lỗi KHÔNG
    /// nhắc gì tới tenant. Người gặp nó sẽ đi tìm bug ép kiểu uuid.
    ///
    /// Guard cũ mù với ca này vì nó không đọc biểu thức policy.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_khi_bieu_thuc_policy_la_ban_truoc_IM9()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync("""
            DROP POLICY tenant_isolation ON kp.evidence_item;
            CREATE POLICY tenant_isolation ON kp.evidence_item
                USING ("TenantId" = current_setting('app.current_tenant', true)::uuid)
                WITH CHECK ("TenantId" = current_setting('app.current_tenant', true)::uuid);
            """);
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));

            Assert.Contains("evidence_item", ex.Message);
            Assert.Contains("USING", ex.Message);
            // Phải in ra CẢ hai biểu thức để người đọc thấy ngay chỗ lệch.
            Assert.Contains("NULLIF", ex.Message);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"""
                DROP POLICY tenant_isolation ON kp.evidence_item;
                CREATE POLICY tenant_isolation ON kp.evidence_item
                    USING {RlsGuard.PolicyExpression}
                    WITH CHECK {RlsGuard.PolicyExpression};
                """);
        }

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// `IM-5`: thiếu <c>FORCE</c> thì CHỦ SỞ HỮU bảng được miễn policy — mà app chạy
    /// bằng chính chủ sở hữu (<c>kp_app</c> sở hữu database, xem dev-db-setup.sql).
    /// RLS bật, mà không chặn gì. Guard cũ không kiểm cột này.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_khi_thieu_FORCE_ROW_LEVEL_SECURITY()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync("ALTER TABLE kp.assertion NO FORCE ROW LEVEL SECURITY");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));

            Assert.Contains("assertion", ex.Message);
            Assert.Contains("FORCE", ex.Message);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE kp.assertion FORCE ROW LEVEL SECURITY");
        }

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// `AR-d` — cái lỗ mà guard cũ sinh ra để bịt nhưng lại không bịt: nó kiểm MỘT
    /// CHIỀU ("bảng đã khai có RLS chưa"), không kiểm chiều ngược ("bảng đang tồn tại
    /// đã được khai chưa"). Quên cài <c>ITenantScoped</c> ở một entity mới thì bảng
    /// đó rơi khỏi danh sách và guard bỏ qua — tức mặc định là ALLOW, trái `G7`.
    ///
    /// Test này cũng ghim luôn RANH GIỚI của phép kiểm nông: nó CỐ Ý không bắt ca này.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_khi_co_bang_la_chua_duoc_khai_hoac_mien_tru()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync("CREATE TABLE kp.bang_ai_do_quen (id int)");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));

            Assert.Contains("bang_ai_do_quen", ex.Message);
            // Thông báo phải chỉ ra CẢ HAI đường sửa, vì chọn sai đường là tạo lỗ mới.
            Assert.Contains("ITenantScoped", ex.Message);
            Assert.Contains("TenantExemptRelations", ex.Message);

            // ⚠ Và phép kiểm NÔNG cố ý KHÔNG bắt — đó là lý do /health/ready dùng nó,
            // để deploy cuốn chiếu không rút cả đội tiến trình khoẻ ra khỏi luồng.
            await RlsGuard.VerifyAsync(db, RlsScanDepth.DeclaredTablesOnly);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TABLE kp.bang_ai_do_quen");
        }

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// Materialized view KHÔNG nhận policy RLS được — Postgres không hỗ trợ. Nên một
    /// MV bắc qua bảng tenant-scoped là một BẢN SAO không có ranh giới nào, và
    /// <c>REFRESH</c> chạy dưới tenant nào thì nó đông cứng dữ liệu của tenant đó cho
    /// mọi người đọc.
    ///
    /// Guard cũ lọc <c>relkind = 'r'</c> nên vĩnh viễn không thấy MV. Đây là đường rò
    /// mạnh hơn cả bảng lạ, và nó sẽ tới đúng lúc làm full-text search.
    /// </summary>
    [Fact]
    public async Task RlsGuard_nem_khi_co_materialized_view_trong_schema()
    {
        await using var db = _db.NewContext(new FixedTenantContext(Guid.CreateVersion7()));

        await db.Database.ExecuteSqlRawAsync(
            "CREATE MATERIALIZED VIEW kp.mv_khong_co_rls AS SELECT 1 AS x");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RlsGuard.VerifyAsync(db));

            Assert.Contains("mv_khong_co_rls", ex.Message);
            Assert.Contains("materialized view", ex.Message);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW kp.mv_khong_co_rls");
        }

        await RlsGuard.VerifyAsync(db);
    }

    /// <summary>
    /// Miễn trừ phải KÈM LÝ DO. Kiểu <c>Dictionary</c> thay vì <c>HashSet</c> tồn tại
    /// chỉ để ép điều này: thêm một dòng vào danh sách miễn trừ buộc phải viết ra vì
    /// sao, và lý do đó hiện trong diff khi có người review.
    ///
    /// Test này đỏ nếu ai đó thêm một miễn trừ mà để lý do trống.
    /// </summary>
    [Fact]
    public void Moi_mien_tru_deu_phai_co_ly_do()
    {
        Assert.NotEmpty(AppDbContext.TenantExemptRelations);
        Assert.All(
            AppDbContext.TenantExemptRelations,
            kv => Assert.False(string.IsNullOrWhiteSpace(kv.Value), $"Miễn trừ \"{kv.Key}\" không ghi lý do."));

        // Hôm nay đúng MỘT ngoại lệ hợp lệ: bảng danh bạ tenant (`IM-14`).
        Assert.Equal(["tenant"], AppDbContext.TenantExemptRelations.Keys.OrderBy(k => k));
    }
}
