using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Mọi thứ ở đây chạy TRƯỚC khi ứng dụng nhận request đầu tiên, và thất bại nào
/// cũng là **không khởi động được**.
///
/// Vì sao không dùng health check để báo: health check báo SAU khi đã start, tức
/// là có một khoảng thời gian ứng dụng đang chạy ở trạng thái sai. Với ranh giới
/// tenant (`G7` gọi là nền tảng) thì khoảng thời gian đó không được tồn tại.
///
/// Đây là `RlsGuard` mở rộng ra tầng cấu hình: cùng một nguyên tắc — **quên là
/// không start được, không phải rò rỉ lúc chạy**.
/// </summary>
public static class StartupChecks
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var config = sp.GetRequiredService<IConfiguration>();
        var tenancy = sp.GetRequiredService<IOptions<TenancyOptions>>().Value;

        var auth = sp.GetRequiredService<IOptions<AuthOptions>>().Value;

        RequireConnectionString(config);
        RequireApiAuthDecision(tenancy, auth);
        RequireCongCheDecision(tenancy, sp.GetRequiredService<IOptions<CongCheOptions>>().Value);
        CanhBaoDiaChiLink(sp.GetRequiredService<IOptions<LinkXemOptions>>().Value);
        await ResolveTenancyAsync(sp, tenancy, ct);

        // Kiểm RLS trên database THẬT. Danh sách bảng suy ra từ model (`IM-7`),
        // nên thêm entity tenant-scoped mà migration quên bật RLS là ném ở đây.
        var db = sp.GetRequiredService<AppDbContext>();
        await RlsGuard.VerifyAsync(db, ct);
    }

    /// <summary>
    /// Mọi endpoint phải có mô tả trong <see cref="OpenApiMoTa.Bang"/>. Thiếu là **không khởi
    /// động được**.
    ///
    /// 🛑 Vì sao đây là một cổng cứng chứ không phải một lời dặn: `IM-38` đã ĐO được cái giá
    /// của việc không có nó — bộ Postman đứng ở 5/13 endpoint suốt hơn hai tuần mà không ai
    /// nhận ra, vì không có gì đối chiếu nó với thực tế. Một hợp đồng tích hợp thiếu ba
    /// endpoint thì tệ hơn không có hợp đồng: bên tích hợp tin vào nó.
    ///
    /// ⚠ Phải gọi SAU mọi lệnh <c>Map*</c>, và phải nhận <c>IEndpointRouteBuilder</c> chứ
    /// KHÔNG phải <c>IServiceProvider</c> — **bản đầu đã sai đúng chỗ đó**. Lấy
    /// <c>EndpointDataSource</c> từ DI thì lúc này nó còn RỖNG: lệnh <c>Map*</c> của minimal
    /// API đi vào <c>WebApplication.DataSources</c>, và chỉ được nối vào nguồn toàn cục khi
    /// pipeline dựng xong — tức SAU lệnh gọi này. Hậu quả: cổng duyệt một danh sách rỗng,
    /// không bao giờ ném, và **trông hệt như đang canh**. Test của nó vẫn xanh vì test gọi
    /// thẳng <see cref="OpenApiMoTa.ThieuMoTa"/>, không đi qua đường nối. Tìm ra bằng một
    /// phép ĐỘT BIẾN chạy thật, không bằng đọc lại code.
    ///
    /// → Vì thế phép kiểm này đi CẢ HAI CHIỀU: danh sách route rỗng làm mọi mô tả thành
    ///   thừa, nên cổng tự tố cáo khi nó bị ngắt lần nữa.
    /// </summary>
    public static void RequireMoTaEndpoint(IEndpointRouteBuilder app)
    {
        var routes = app.DataSources.SelectMany(d => d.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(e =>
                (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(m => (Method: m, Route: e.RoutePattern.RawText ?? string.Empty)))
            // Tài liệu OpenAPI tự nó không nằm trong hợp đồng của sản phẩm.
            .Where(r => !r.Route.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase));

        var danhSach = routes.ToList();
        var thieu = OpenApiMoTa.ThieuMoTa(danhSach);
        var thua = OpenApiMoTa.MoTaThua(danhSach);

        if (thieu.Count == 0 && thua.Count == 0) return;

        var phan = new List<string>();

        if (thieu.Count > 0)
        {
            phan.Add(
                $"{thieu.Count} endpoint CHƯA CÓ mô tả:{Environment.NewLine}    " +
                string.Join($"{Environment.NewLine}    ", thieu));
        }

        if (thua.Count > 0)
        {
            phan.Add(
                $"{thua.Count} mô tả KHÔNG ỨNG với endpoint nào:{Environment.NewLine}    " +
                string.Join($"{Environment.NewLine}    ", thua));
        }

        throw new InvalidOperationException(
            $"""
             Hợp đồng tích hợp lệch với thực tế.

             {string.Join($"{Environment.NewLine}{Environment.NewLine}", phan)}

             Sửa OpenApiMoTa.Bang. Mỗi dòng ở đó là thứ BÊN NGOÀI đọc để cắm vào sản phẩm này —
             thiếu một dòng nghĩa là bên tích hợp phải đoán, hoặc phải đọc code.

             ⚠ Đây là cổng cứng vì IM-38 đã đo được cái giá của việc không có nó: bộ Postman
               đứng ở 5/13 endpoint suốt hơn hai tuần mà không ai nhận ra.
             ⚠ Nếu MỌI mô tả đều báo thừa thì danh sách route đang rỗng — cổng này vừa bị
               ngắt khỏi chỗ nó phải canh. Xem phần tóm tắt của hàm.
             """);
    }

    /// <summary>
    /// Cảnh báo — KHÔNG ném — khi địa chỉ gốc của link trỏ vào chính máy này.
    ///
    /// 🛑 Đây là một thất bại IM LẶNG có hình dạng rất đặc biệt: **nó chạy đúng với đúng
    /// người đi kiểm nó.** Người deploy tự bấm link trên máy chủ thì mở được; người support
    /// bấm từ máy của họ thì chết. Không có phép kiểm tự động nào bắt được, vì máy chạy phép
    /// kiểm cũng chính là máy đó.
    ///
    /// ⚠ Cảnh báo chứ không ném, vì trên máy dev thì `localhost` LÀ đúng. Cái ném được là
    /// việc thiếu hẳn cấu hình (đã chặn ở <see cref="Knowledge.PhatLinkHandler"/>).
    /// Muốn soi kỹ trước khi deploy: `--kiem-cau-hinh`.
    /// </summary>
    private static void CanhBaoDiaChiLink(LinkXemOptions link)
    {
        if (string.IsNullOrWhiteSpace(link.PublicBaseUrl)) return;
        if (!SoiCauHinh.LaLoopback(link.PublicBaseUrl)) return;

        Console.Error.WriteLine(
            $"⚠ Link:PublicBaseUrl = {link.PublicBaseUrl} — trỏ vào CHÍNH MÁY NÀY. " +
            "Link phát ra sẽ mở được khi bạn tự bấm, và CHẾT khi gửi cho người khác. " +
            "Deploy thật: đặt bằng tên máy trong LAN/VPN.");
    }

    private static void RequireConnectionString(IConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.GetConnectionString("Default"))) return;

        throw new InvalidOperationException(
            """
            Thiếu chuỗi kết nối "ConnectionStrings:Default".

            Máy dev:   đã có sẵn trong appsettings.Development.json
            Deploy:    đặt bằng biến môi trường, KHÔNG commit mật khẩu
                       ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...

            ⚠ Đừng dùng role superuser. Superuser đi vòng qua row-level security,
              kể cả khi bảng có FORCE — xem scripts/dev-db-setup.sql.
            """);
    }

    /// <summary>
    /// `AR-e`: người gọi phải chứng minh được mình là khách hàng nào.
    ///
    /// Hai nhánh, và chúng KHÔNG đối xứng — chỗ này đáng đọc kỹ:
    /// <list type="bullet">
    ///   <item><b>dedicated</b>: tenant đến từ cấu hình, nên chạy không khoá vẫn có nghĩa
    ///         (một bản deploy, một khách). Cho phép, nhưng phải THỪA NHẬN tường minh —
    ///         nguyên tắc cũ của `IM-13`: sơ suất phải thành quyết định.</item>
    ///   <item><b>shared</b>: khoá API là thứ DUY NHẤT nói được request thuộc khách nào.
    ///         Bật cờ thừa nhận ở đây không phải là "chấp nhận rủi ro" mà là vô nghĩa —
    ///         không còn đường nào xác định tenant. Nên cờ bị TỪ CHỐI, không phải cảnh báo.</item>
    /// </list>
    /// </summary>
    private static void RequireApiAuthDecision(TenancyOptions tenancy, AuthOptions auth)
    {
        if (tenancy.Mode == TenancyMode.SharedMultiTenant && auth.AcknowledgeUnauthenticatedApi)
        {
            throw new InvalidOperationException(
                """
                Auth:AcknowledgeUnauthenticatedApi = true KHÔNG dùng được ở chế độ SharedMultiTenant.

                Ở chế độ này, khoá API là thứ DUY NHẤT nói được request thuộc khách hàng nào
                (AR-e). Bỏ xác thực đi thì không còn gì để xác định tenant — không phải "kém
                an toàn hơn", mà là KHÔNG CHẠY ĐƯỢC.

                Muốn chạy không khoá thì dùng Tenancy:Mode=DedicatedSingleTenant, nơi tenant
                đến từ cấu hình.
                """);
        }

        if (!auth.AcknowledgeUnauthenticatedApi) return;

        // Tới đây: dedicated + đã thừa nhận. Hợp lệ, không ném. Cảnh báo để nó không im lặng.
        Console.Error.WriteLine(
            "⚠ Auth:AcknowledgeUnauthenticatedApi = true — API đang chạy KHÔNG cần khoá. " +
            "Chỉ dùng cho máy dev và bộ test.");
    }

    /// <summary>
    /// `N1-2` — cờ TẮT TẠM mọi cổng che.
    ///
    /// 🛑 Chế độ <b>shared</b> TỪ CHỐI KHỞI ĐỘNG khi cờ bật. Lý do không phải "kém an toàn hơn"
    /// mà là PHẠM VI của quyết định: chủ dự án chấp nhận rủi ro cho dữ liệu của chính khách mình
    /// trên một bản deploy riêng. Một bản shared mang dữ liệu của NHIỀU công ty — không ai trong
    /// số họ đã chấp nhận điều đó, và khâu nạp là cửa MỘT CHIỀU (xem <see cref="CongChe"/>).
    ///
    /// ⚠ Ở dedicated thì chạy được, nhưng in cảnh báo MỖI LẦN khởi động — cùng khuôn với
    /// <c>Auth:AcknowledgeUnauthenticatedApi</c>: sơ suất phải thành quyết định, và quyết định
    /// phải không im lặng.
    /// </summary>
    private static void RequireCongCheDecision(TenancyOptions tenancy, CongCheOptions congChe)
    {
        if (!congChe.TatTamVaChapNhanRuiRo) return;

        if (tenancy.Mode == TenancyMode.SharedMultiTenant)
        {
            throw new InvalidOperationException(
                """
                CongChe:TatTamVaChapNhanRuiRo = true KHÔNG dùng được ở chế độ SharedMultiTenant.

                Cờ này tắt mọi cổng che dữ liệu (N1-2). Một bản shared mang dữ liệu của NHIỀU công ty,
                và không công ty nào trong số đó đã chấp nhận để mật khẩu/token của họ đi nguyên văn
                tới model và vào hệ thống bên ngoài.

                ⚠ Khâu nạp là cửa MỘT CHIỀU: mẩu vào lúc cờ bật nằm trong kho KHÔNG CHE, và tắt cờ đi
                  cũng không che lại được.
                """);
        }

        Console.Error.WriteLine(
            "🛑 CongChe:TatTamVaChapNhanRuiRo = true — MỌI CỔNG CHE ĐANG TẮT (N1-2). " +
            "Mật khẩu/token trong dữ liệu sẽ đi NGUYÊN VĂN tới model, webhook, trang /xem, và vào kho. " +
            "Mẩu nạp lúc này mang dấu DaCheBenThuBa = false.");
    }

    private static async Task ResolveTenancyAsync(
        IServiceProvider sp, TenancyOptions tenancy, CancellationToken ct)
    {
        var holder = sp.GetRequiredService<DedicatedTenant>();

        switch (tenancy.Mode)
        {
            case TenancyMode.DedicatedSingleTenant:
                if (string.IsNullOrWhiteSpace(tenancy.TenantExternalKey))
                {
                    throw new InvalidOperationException(
                        "Chế độ Tenancy:Mode=DedicatedSingleTenant đòi Tenancy:TenantExternalKey — " +
                        "khoá ngoài của công ty khách hàng mà bản deploy này phục vụ.");
                }

                var directory = sp.GetRequiredService<TenantDirectory>();
                var id = await directory.FindIdByExternalKeyAsync(tenancy.TenantExternalKey, ct);

                // Khoá cấu hình sai mà vẫn start được là ca xấu nhất: ứng dụng chạy
                // bình thường, không thấy dữ liệu nào, và không ai biết vì sao.
                holder.Assign(id ?? throw new InvalidOperationException(
                    $"""
                     Không có tenant nào trong kp.tenant với ExternalKey = "{tenancy.TenantExternalKey}".

                     Bản deploy dedicated này sẽ không thấy dữ liệu nào — nên nó từ chối khởi động
                     thay vì chạy im lặng ở trạng thái sai.
                     """));
                break;

            case TenancyMode.SharedMultiTenant:
                // ✅ Không còn cổng thừa nhận ở đây. `AR-e` đã thành code: tenant đến từ khoá
                //    API đã kiểm, nên không còn trạng thái "chạy mà chưa xác thực" để thừa nhận.
                //    Cờ cũ (`Tenancy:AcknowledgeUnauthenticatedTenantHeader`) đã bị xoá — xem
                //    ghi chú trong TenancyOptions.
                holder.Assign(null);
                break;

            case TenancyMode.Unspecified:
            default:
                throw new InvalidOperationException(
                    """
                    Thiếu cấu hình "Tenancy:Mode". Không có giá trị mặc định, cố ý (G13).

                    DedicatedSingleTenant   một bản deploy cho một khách hàng.
                                            Cần thêm Tenancy:TenantExternalKey.
                    SharedMultiTenant       một bản deploy phục vụ nhiều khách hàng,
                                            tenant đến từ header của request.

                    Cùng codebase, cùng schema, cùng policy RLS — khác một lớp cài đặt.
                    Đoán hộ chế độ deploy là đúng loại giả định mà G13 cấm.
                    """);
        }
    }
}
