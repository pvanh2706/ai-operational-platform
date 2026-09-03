using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// Độ sâu của phép kiểm. Hai chỗ gọi <see cref="RlsGuard"/> hỏi hai câu khác nhau,
/// nên chúng không được dùng chung một độ sâu.
/// </summary>
public enum RlsScanDepth
{
    /// <summary>
    /// Chỉ kiểm những bảng mà MODEL khai là tenant-scoped.
    ///
    /// Dùng cho <c>/health/ready</c>. Câu hỏi ở đó là *"tôi phục vụ được không"*, và
    /// nó phải trả lời được về những bảng tiến trình NÀY thật sự đọc ghi.
    ///
    /// ⚠ Vì sao readiness KHÔNG dùng <see cref="IncludingUndeclaredRelations"/>: lúc
    /// deploy cuốn chiếu, bản N+1 chạy migration tạo bảng mới trong khi các tiến trình
    /// bản N cũ vẫn đang phục vụ. Model của bản cũ không biết bảng đó, nên phép kiểm
    /// sâu sẽ coi nó là "relation lạ" và trả 503 — rút CẢ ĐỘI tiến trình đang khoẻ ra
    /// khỏi luồng, vì một bảng mà chúng không hề đụng tới. Đó là mất dịch vụ do một
    /// cơ chế an toàn gây ra, không phải do sự cố thật.
    /// </summary>
    DeclaredTablesOnly,

    /// <summary>
    /// Kiểm thêm chiều ngược: MỌI relation trong schema đều phải được khai là
    /// tenant-scoped, hoặc được miễn trừ TƯỜNG MINH.
    ///
    /// Dùng lúc KHỞI ĐỘNG. Câu hỏi ở đó khác hẳn: *"bản build này có khớp với database
    /// này không"* — và đó đúng là lúc phải bắt được "thêm entity mà quên cài
    /// <c>ITenantScoped</c>", vì lúc đó bản build và schema được nhìn cùng một lần.
    /// </summary>
    IncludingUndeclaredRelations,
}

/// <summary>
/// Kiểm lúc KHỞI ĐỘNG rằng ranh giới tenant trên database thật còn nguyên vẹn.
/// Thiếu là ném ngay — không phải lỗ rò lúc chạy.
///
/// Vì sao cần thứ này: `G7` nói tenant boundary là NỀN TẢNG. Nền tảng nghĩa là không
/// dựa vào việc không ai quên. Một entity mới cài <c>ITenantScoped</c> mà migration
/// quên bật RLS sẽ chạy hoàn toàn bình thường trong dev (global query filter che mất),
/// rồi rò rỉ dữ liệu giữa hai công ty khách hàng trên production ở đúng chỗ nào đó
/// dùng SQL thô.
///
/// <para><b>⚠ 2026-09-01 — BẢN NÀY VIẾT LẠI SAU KHI ĐO ĐƯỢC MỘT LỖ FAIL-OPEN THẬT.</b>
/// Bản trước chỉ hỏi <i>"bảng này có tồn tại policy nào không"</i> (<c>EXISTS pg_policy</c>).
/// Policy trong PostgreSQL mặc định là PERMISSIVE, tức là <b>OR</b> với nhau — nên chỉ
/// cần ai đó thêm một policy thứ hai <c>USING (true)</c> là ranh giới biến mất, trong
/// khi guard vẫn báo xanh. Đã đo trên PostgreSQL 18, role <c>kp_app</c>, schema thật:</para>
/// <code>
/// Khách A thấy                  | cua khach A + cua khach B   ← RÒ
/// Câu SQL của guard bản cũ      | XANH — "đã được bảo vệ"
/// Số policy trên bảng           | 2
/// </code>
/// <para>Đó chính là loại thất bại im lặng mà cả dự án được dựng để chặn, nằm bên trong
/// cơ chế được giao việc chặn nó. Nên bản này không hỏi <i>"có policy không"</i> nữa;
/// nó đối chiếu <b>BIỂU THỨC</b> của policy với đúng một chuỗi đã biết.</para>
///
/// Bốn lỗ mà bản này bịt:
/// <list type="number">
/// <item>policy thứ hai <c>USING (true)</c> — đã đo, ở trên</item>
/// <item>bảng mới chép policy từ migration đầu (bản CHƯA có <c>nullif</c>) → tái phát
///       đúng `IM-9`, và thông báo lỗi lúc đó không nhắc gì tới tenant</item>
/// <item>view và materialized view trong schema — bản cũ lọc <c>relkind = 'r'</c> nên
///       không bao giờ thấy chúng. Materialized view KHÔNG nhận policy RLS được, nên
///       một MV bắc qua bảng tenant-scoped là một bản sao không có ranh giới nào</item>
/// <item><c>relforcerowsecurity</c> — bản cũ không kiểm. Thiếu <c>FORCE</c> thì chủ sở
///       hữu bảng được miễn policy, mà <c>kp_app</c> chính là chủ sở hữu (`IM-5`)</item>
/// </list>
///
/// Danh sách bảng cần kiểm vẫn suy ra TỪ MODEL (<see cref="AppDbContext.TenantScopedTables"/>),
/// không phải danh sách viết tay — nên nó không thể lệch với model.
/// </summary>
public static class RlsGuard
{
    /// <summary>Tên policy duy nhất được chấp nhận. Migration nào cũng phải dùng đúng tên này.</summary>
    public const string PolicyName = "tenant_isolation";

    /// <summary>
    /// Biểu thức policy DUY NHẤT được chấp nhận, ở đúng dạng <c>pg_get_expr()</c> trả về
    /// (PostgreSQL tự chuẩn hoá, nên chuỗi này ổn định giữa các lần chạy).
    ///
    /// ⚠ Đây là bản sao thứ hai của biểu thức trong migration, và <b>trùng lặp đó là cố ý</b>.
    /// Migration là ảnh chụp một thời điểm; hằng số này là điều kiện HIỆN HÀNH. Nếu suy
    /// hằng số này ra từ migration thì đúng cái sai cần bắt — một migration cũ viết sai —
    /// sẽ tự hợp lệ hoá chính nó.
    ///
    /// Đổi biểu thức policy thì phải đổi ở HAI chỗ, và lệch một chỗ là app không khởi
    /// động được. Đó là hành vi mong muốn, không phải phiền toái.
    /// </summary>
    public const string PolicyExpression =
        """("TenantId" = (NULLIF(current_setting('app.current_tenant'::text, true), ''::text))::uuid)""";

    /// <summary>
    /// Gọi ở startup, sau khi migration đã chạy. Mặc định là phép kiểm SÂU —
    /// default DENY, đúng `G7`. Chỗ nào cần nông hơn thì phải nói tường minh.
    /// </summary>
    public static Task VerifyAsync(AppDbContext db, CancellationToken ct = default) =>
        VerifyAsync(db, RlsScanDepth.IncludingUndeclaredRelations, ct);

    /// <summary>
    /// Ném <see cref="InvalidOperationException"/> kèm tên relation và lý do cụ thể
    /// nếu ranh giới tenant không còn nguyên.
    /// </summary>
    public static async Task VerifyAsync(AppDbContext db, RlsScanDepth depth, CancellationToken ct = default)
    {
        var declared = db.TenantScopedTables;
        if (declared.Count == 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy bảng tenant-scoped nào. Hoặc model sai, hoặc ITenantScoped " +
                "đã bị bỏ khỏi các entity — cả hai đều nghĩa là ranh giới tenant không được thực thi.");
        }

        var schema = db.Model.GetDefaultSchema() ?? "public";
        var relations = await ReadAsync(db, schema, ct);

        var problems = new List<string>();

        // --- Chiều 1: mọi bảng ĐÃ KHAI đều phải được bảo vệ ĐÚNG CÁCH ---
        foreach (var table in declared)
        {
            if (!relations.TryGetValue(table, out var r))
            {
                problems.Add($"kp.{table}: model khai là tenant-scoped nhưng bảng không tồn tại trong schema \"{schema}\". Migration chưa chạy?");
                continue;
            }

            problems.AddRange(Fault(r).Select(f => $"kp.{table}: {f}"));
        }

        if (depth == RlsScanDepth.IncludingUndeclaredRelations)
        {
            // --- Chiều 2: KHÔNG relation nào được nằm ngoài hai danh sách ---
            // Đây là chiều mà bản cũ hoàn toàn không có, và là chiều chặn được "quên"
            // (`AR-d`): không khai thì bị bỏ qua, tức mặc định là ALLOW — trái G7.
            var known = new HashSet<string>(declared, StringComparer.Ordinal);

            foreach (var r in relations.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (known.Contains(r.Name)) continue;
                if (AppDbContext.TenantExemptRelations.ContainsKey(r.Name)) continue;

                problems.Add(
                    $"kp.{r.Name} ({Describe(r.Kind)}): nằm trong schema nhưng KHÔNG được khai là tenant-scoped " +
                    "và KHÔNG được miễn trừ. Mặc định phải là TỪ CHỐI. " +
                    "Sửa bằng một trong hai cách, và cả hai đều phải là hành động có ý thức: " +
                    "cài ITenantScoped cho entity (rồi thêm RLS trong migration của nó), " +
                    $"hoặc thêm vào AppDbContext.TenantExemptRelations KÈM LÝ DO.");
            }
        }

        if (problems.Count == 0) return;

        throw new InvalidOperationException(
            $"Ranh giới tenant không nguyên vẹn — {problems.Count} vấn đề:{Environment.NewLine}" +
            string.Join(Environment.NewLine, problems.Select(p => "  · " + p)) +
            $"{Environment.NewLine}Đây là vi phạm G7 (tenant boundary là nền tảng) — không được khởi động.");
    }

    /// <summary>Mọi cách một bảng tenant-scoped có thể mất ranh giới. Trả về rỗng nghĩa là ổn.</summary>
    private static IEnumerable<string> Fault(Relation r)
    {
        if (r.Kind != 'r')
        {
            yield return $"phải là bảng thường nhưng đang là {Describe(r.Kind)} — {Describe(r.Kind)} không nhận policy RLS như bảng.";
            yield break;
        }

        if (!r.RowSecurity)
        {
            yield return "chưa ENABLE ROW LEVEL SECURITY.";
            yield break;
        }

        // FORCE là cái bẫy phổ biến nhất của RLS trong PostgreSQL, và nó hỏng IM LẶNG:
        // thiếu nó thì chủ sở hữu bảng được MIỄN policy — mà app chạy bằng chính chủ
        // sở hữu (IM-5). RLS bật, mà không chặn gì.
        if (!r.ForceRowSecurity)
        {
            yield return "thiếu FORCE ROW LEVEL SECURITY — chủ sở hữu bảng đang được miễn policy (IM-5).";
        }

        if (r.PolicyCount == 0)
        {
            yield return "có RLS nhưng KHÔNG có policy nào — Postgres sẽ chặn hết, tức là cấu hình sai chứ không phải an toàn.";
            yield break;
        }

        // ⚠ CHỖ QUAN TRỌNG NHẤT CỦA CẢ FILE.
        // Policy là PERMISSIVE, tức OR với nhau. Hai policy nghĩa là ranh giới bằng
        // policy LỎNG NHẤT, không phải chặt nhất. Đã đo: thêm một policy USING(true)
        // là khách A đọc được dữ liệu khách B, trong khi guard bản cũ vẫn xanh.
        if (r.PolicyCount != 1)
        {
            yield return
                $"có {r.PolicyCount} policy ({r.PolicyNames}) — chỉ được phép đúng MỘT. " +
                "Policy PERMISSIVE gộp bằng OR, nên policy thứ hai chỉ nới ra chứ không siết vào: " +
                "một policy USING (true) là mở toang ranh giới mà không có dấu hiệu nào.";
            yield break;
        }

        if (r.PolicyNames != PolicyName)
        {
            yield return $"policy tên \"{r.PolicyNames}\", phải là \"{PolicyName}\".";
        }

        if (!r.Permissive || r.Command != "*")
        {
            yield return $"policy phải là PERMISSIVE và áp cho MỌI lệnh (ALL); đang là permissive={r.Permissive}, cmd=\"{r.Command}\".";
        }

        // Kiểm CẢ HAI nhánh. Thiếu WITH CHECK thì đọc bị chặn mà GHI thì không —
        // tức là bơm được dữ liệu vào tenant khác.
        if (r.UsingExpression != PolicyExpression)
        {
            yield return $"biểu thức USING không khớp.{Environment.NewLine}      đang là: {r.UsingExpression}{Environment.NewLine}      phải là: {PolicyExpression}";
        }

        if (r.CheckExpression != PolicyExpression)
        {
            yield return $"biểu thức WITH CHECK không khớp (thiếu nó thì ĐỌC bị chặn mà GHI thì không).{Environment.NewLine}      đang là: {r.CheckExpression ?? "<không có>"}{Environment.NewLine}      phải là: {PolicyExpression}";
        }
    }

    private static string Describe(char relkind) => relkind switch
    {
        'r' => "bảng",
        'v' => "view",
        'm' => "materialized view",
        'p' => "bảng phân mảnh",
        'f' => "foreign table",
        _ => $"relkind '{relkind}'",
    };

    private sealed record Relation(
        string Name,
        char Kind,
        bool RowSecurity,
        bool ForceRowSecurity,
        int PolicyCount,
        string? PolicyNames,
        string? UsingExpression,
        string? CheckExpression,
        bool Permissive,
        string? Command);

    /// <summary>
    /// Đọc trạng thái RLS thật từ catalog. Chạy được bằng role KHÔNG superuser —
    /// <c>kp_app</c> là chủ sở hữu schema nên đọc <c>pg_class</c>/<c>pg_policy</c> bình thường.
    /// </summary>
    private static async Task<Dictionary<string, Relation>> ReadAsync(
        AppDbContext db, string schema, CancellationToken ct)
    {
        var result = new Dictionary<string, Relation>(StringComparer.Ordinal);

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        var opened = conn.State != System.Data.ConnectionState.Open;
        if (opened) await conn.OpenAsync(ct);

        try
        {
            await using var cmd = conn.CreateCommand();

            // relkind: r bảng · v view · m materialized view · p phân mảnh · f foreign.
            // Bản cũ chỉ lấy 'r', nên view và matview vô hình với nó — xem chú thích class.
            // Index ('i') và sequence ('S') cố ý không lấy: chúng không chứa dòng dữ liệu riêng.
            cmd.CommandText = """
                SELECT c.relname,
                       c.relkind::text                                    AS kind,
                       c.relrowsecurity,
                       c.relforcerowsecurity,
                       COALESCE(p.cnt, 0)                                 AS policy_count,
                       p.names,
                       p.using_expr,
                       p.check_expr,
                       COALESCE(p.permissive, true)                       AS permissive,
                       p.cmd
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                LEFT JOIN LATERAL (
                    SELECT count(*)::int AS cnt,
                           string_agg(pol.polname, ', ' ORDER BY pol.polname)                          AS names,
                           min(pg_get_expr(pol.polqual, pol.polrelid))                                 AS using_expr,
                           min(pg_get_expr(pol.polwithcheck, pol.polrelid))                            AS check_expr,
                           bool_and(pol.polpermissive)                                                 AS permissive,
                           min(pol.polcmd::text)                                                       AS cmd
                    FROM pg_policy pol
                    WHERE pol.polrelid = c.oid
                ) p ON true
                WHERE n.nspname = @schema
                  AND c.relkind IN ('r', 'v', 'm', 'p', 'f')
                """;

            var param = cmd.CreateParameter();
            param.ParameterName = "schema";
            param.Value = schema;
            cmd.Parameters.Add(param);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(0);
                result[name] = new Relation(
                    Name: name,
                    Kind: reader.GetString(1)[0],
                    RowSecurity: reader.GetBoolean(2),
                    ForceRowSecurity: reader.GetBoolean(3),
                    PolicyCount: reader.GetInt32(4),
                    PolicyNames: reader.IsDBNull(5) ? null : reader.GetString(5),
                    UsingExpression: reader.IsDBNull(6) ? null : reader.GetString(6),
                    CheckExpression: reader.IsDBNull(7) ? null : reader.GetString(7),
                    Permissive: reader.GetBoolean(8),
                    Command: reader.IsDBNull(9) ? null : reader.GetString(9));
            }
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }

        return result;
    }
}
