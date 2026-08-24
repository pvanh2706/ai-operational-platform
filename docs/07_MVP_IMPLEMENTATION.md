# 07 — MVP Implementation

## Nhật ký hiện thực · slice đầu: Path A

> **Bắt đầu:** 2026-08-23 · Slice đầu tiên = **Path A đi hết một đường**
> **Cập nhật:** 2026-08-24 — có PostgreSQL thật. `AR-c` ĐÓNG. Sinh `IM-9`..`IM-11`,
> test project đầu tiên, và `TenantConnectionInterceptor` (mắt xích C# ↔ RLS).
> **Cùng ngày, buổi 2:** có **project host**. Ranh giới tenant giờ sống được trong
> một request HTTP thật, ở CẢ HAI chế độ deploy của `G13`. Sinh `IM-12`..`IM-14`
> và `AR-e` (chế độ shared chưa có xác thực).
> **Cập nhật 2026-08-25:** có **Kênh 1** — đường nhận tín hiệu. Ô "tìm hoặc tạo
> Case" của sơ đồ luồng chạy được. Sinh `IM-15`..`IM-17`. 33 test.
> **File này CỐ Ý NGẮN.** §6.7 cảnh báo tốc độ sản xuất tài liệu vượt tốc độ sử dụng.
> Đây là **nhật ký quyết định phát sinh khi code**, không phải bản thiết kế.

---

# 1. Vì sao Path A trước

```text
§3     Ngày đầu khách KHÔNG có SOP nào để tìm (90% thiếu). Cap 1 và Cap 2
       không có gì để làm cho tới khi Path A tạo ra thứ đầu tiên.
M2     Success Metric CHÍNH của tháng đầu là "số nháp Path A được duyệt
       + mức sửa diff(A,B)". Build cái mà thước đo đầu tiên đo.
§8.2   Path A độc lập với §8.2 (nó quyết định Capability 1). Nên implementation
       KHÔNG bị chặn bởi con số chưa đếm.
```

---

# 2. Đã build

```text
src/KnowledgePlatform.slnx          (.slnx — định dạng solution của .NET 10)
  KnowledgePlatform.Domain/
    Knowledge/KnowledgeVocabulary.cs   từ vựng khóa (04 §3D.7) — enum, có ràng buộc
    Knowledge/KnowledgeRecord.cs        T1 identity=nguyên nhân · V2/V3 lifecycle · S7 duyệt
    Knowledge/Assertion.cs              AP3 + T4 + S8 + L3  ← chỗ quan trọng nhất
    Evidence/EvidenceItem.cs            v0.2 §9 · K-B9 (trỏ trực tiếp, không qua Case)
    Cases/CanonicalCase.cs              bản MỎNG cố ý
    Tenancy/ITenantScoped.cs            + Tenant
    Tenancy/ITenantContext.cs           G13 — tenant từ ngữ cảnh, không từ hằng số

  KnowledgePlatform.Infrastructure/
    Persistence/AppDbContext.cs         mapping + global query filter (tầng 2)
    Persistence/RlsGuard.cs             kiểm RLS lúc KHỞI ĐỘNG
    Persistence/TenantConnectionInterceptor.cs
                                        MẮT XÍCH C# ↔ RLS: đặt app.current_tenant
                                        trên MỌI connection mở ra  ← IM-10
    Persistence/DesignTimeDbContextFactory.cs
    Migrations/…_InitialPathASchema.cs  schema + RLS trong CÙNG migration đầu
    Migrations/…_HardenTenantPolicy…    sửa BIỂU THỨC policy, không đổi schema ← IM-9

  KnowledgePlatform.Api/               ← PROJECT HOST, 06 §1 "API nhận tín hiệu"
    Program.cs                          DI + 3 endpoint + thứ tự middleware
    Tenancy/TenancyOptions.cs           HAI chế độ deploy của G13, không mặc định
    Tenancy/RequestTenantContext.cs     ITenantContext cho MỘT request
    Tenancy/TenantResolutionMiddleware.cs
                                        dedicated → từ cấu hình
                                        shared    → từ header của request
    Tenancy/TenantDirectory.cs          ExternalKey → TenantId  ← IM-14
    Startup/StartupChecks.cs            cấu hình sai = KHÔNG START ĐƯỢC
    Signals/CaseObservedSignal.cs       hợp đồng tín hiệu — G1: không biết Jira là gì
    Signals/CaseSignalHandler.cs        tìm-hoặc-tạo Case, idempotent  ← IM-15
    Signals/SignalKeyEndpointFilter.cs  chốt tạm cho endpoint GHI      ← IM-17
    Signals/IngestOptions.cs

tests/KnowledgePlatform.Infrastructure.Tests/
    TestDatabase.cs                     fixture — chạy trên PostgreSQL THẬT
    TenantIsolationTests.cs             9 test cách ly tenant ở tầng DB

tests/KnowledgePlatform.Api.Tests/
    ApiFactory.cs                       dựng host thật, DB riêng kp_api_test
    TenantBoundaryThroughHttpTests.cs   11 test cách ly tenant qua HTTP THẬT
    CaseSignalTests.cs                  13 test Kênh 1
    AssemblyInfo.cs                     chạy tuần tự — lý do ghi trong file

scripts/dev-db-setup.sql                role kp_app + 3 database
```

## Trạng thái verify

```text
✅  dotnet build          toàn solution, 0 error 0 warning
✅  dotnet ef migrations   sinh được
✅  apply migration        PostgreSQL 18.6 local · kp_dev + kp_test · cả 2 migration
✅  RlsGuard chạy thật     PASS trên DB sống, bằng code C# thật (không phải SQL tay)
✅  cách ly tenant (DB)    9/9 test xanh, chạy bằng role KHÔNG phải superuser
✅  cách ly tenant (HTTP)  11/11 test xanh, qua host thật, cả hai chế độ G13
✅  Kênh 1 chạy thật       13/13 test xanh · curl: 3 tín hiệu → 3 Case,
                           gửi lại → 0 Case mới, khách khác không thấy gì
✅  bộ test có thể ĐỎ      gỡ FORCE khỏi một bảng     → 5 test đỏ
                           gỡ nullif khỏi policy       → 3 test đỏ
                           gỡ interceptor khỏi host    → 4 test API đỏ
                           đảo thứ tự hai filter       → 1 test đỏ
                           gỡ trần lô tín hiệu         → 1 test đỏ
```

⚠️ Một cơ chế KHÔNG đỏ được khi thử phá: tính idempotent của tín hiệu. Gỡ bước kiểm
trước khi ghi thì unique index vẫn bắt, nên test vẫn xanh. Đó là hai lớp bảo vệ làm
việc đúng như thiết kế, nhưng phải ghi rõ ở đây — nói "đã chứng minh mọi test biết
đỏ" là nói quá.

**`AR-c` ĐÓNG 2026-08-24.** "Đúng cú pháp" đã trở thành "chặn được thật", đo trên PostgreSQL 18.6.

Ba thứ chỉ chạy thật mới thấy, không đọc SQL nào thấy được:

```text
1  Policy văng lỗi ép kiểu khi biến session là chuỗi rỗng     → IM-9
2  Superuser đi vòng qua RLS, KỂ CẢ khi có FORCE              → scripts/dev-db-setup.sql
   → chạy app hay test bằng `postgres` là RLS bằng KHÔNG,
     và mọi test cách ly tenant PASS GIẢ
3  Consumer của Infrastructure nhận EF Core 10.0.4 chứ không
   phải 10.0.11 → không biên dịch được                        → IM-11
```

⚠️ Điểm 2 nguy hiểm hơn điểm 1: nó làm chính bộ đo bị hỏng. Vì thế test đầu tiên
trong `TenantIsolationTests` không kiểm sản phẩm mà kiểm **bộ test có ý nghĩa hay
không** — nó đỏ nếu ai đó trỏ test vào role superuser.

---

# 3. Quyết định phát sinh khi code — cần người dùng biết

Mười bảy quyết định dưới đây **suy ra từ** các quyết định domain đã chốt, không phát minh gì mới. Nhưng chúng là lựa chọn, nên ghi lại.

`IM-1`..`IM-8` viết khi chưa có PostgreSQL. `IM-9`..`IM-11` sinh ra từ việc **chạy thật** ngày 2026-08-24 — hai trong ba là thứ đọc SQL không phát hiện được. `IM-12`..`IM-14` sinh ra khi dựng project host cùng ngày. `IM-15`..`IM-17` khi dựng Kênh 1 ngày 2026-08-25.

## `IM-1` · Assertion là bất biến; sửa thì tạo bản mới

`M2` cần `diff(bản nháp AI, bản đã duyệt)` vì nó **vừa** là thước đo tháng đầu **vừa** là nhãn eval (D6 flywheel). Ghi đè là phá cả hai.

→ Người duyệt sửa một assertion thì bản gốc **không bị ghi đè**; nó được trỏ `ReplacedByAssertionId`, và bản mới mang `Origin` của người sửa.

⚠️ **Cố ý KHÔNG đặt tên `Supersedes`** — `SUPERSEDES` là quan hệ Knowledge ↔ Knowledge của `L4`. Dùng lại từ đó ở đây sẽ tạo đúng bệnh §6.9 mà workstream 04 mất **ba** lần để chữa.

## `IM-2` · `StoredLifecycleState` chỉ có BA giá trị

`V3` nói `NEEDS_REVIEW` và `SUPERSEDED` là **suy ra**. Nên enum lưu chỉ có `Draft` / `Active` / `Deprecated`.

→ Thêm hai giá trị kia vào enum là **không thể** mà không sửa enum trước — tức là vi phạm `V3` trở thành hành động tường minh, không phải sơ suất.

## `IM-3` · `VisibilityScope` để dạng chuỗi đục, KHÔNG phát minh enum

`S7` khoá **quy tắc** (hẹp nhất + mở rộng tường minh + log ai/khi nào) nhưng **không** khoá tập giá trị của scope.

→ Tự phát minh một enum visibility ở tầng code chính là cách §6.9 tái phát. Để dạng `string?` và ghi rõ trong comment rằng khoá tập giá trị này là **quyết định domain**, không phải quyết định implementation.

**→ Sinh ra `AR-a`, một Open Question mới cho tầng domain.** Xem §5.

## `IM-4` · `AssertionKind` KHÔNG phải vocabulary khóa

Bốn giá trị (`CauseExists` / `Recognition` / `Applicability` / `Handling`) là cách tổ chức nội dung, suy ra từ `T1`/`T2`/`AP1`/`AP4` và ví dụ ở §3C.5. Ghi rõ trong code là **sửa được** nếu có ca thật đòi — khác với `Origin`/`Verification` là khóa.

## `IM-5` · `FORCE ROW LEVEL SECURITY`, không chỉ `ENABLE`

Đây là cái bẫy phổ biến nhất của RLS trong PostgreSQL, và nó **thất bại im lặng**: nếu không có `FORCE`, **chủ sở hữu bảng được MIỄN policy**. Mà chủ sở hữu thường chính là user app dùng để migrate → RLS bật mà không chặn gì, và không có gì báo.

## `IM-6` · Quên đặt tenant thì KHÔNG THẤY GÌ, không phải thấy HẾT

Policy dùng `current_setting('app.current_tenant', true)`. Tham số `true` làm nó trả `NULL` khi biến chưa đặt, và `NULL = uuid` là `NULL` → policy **từ chối**.

→ Mặc định nghiêng về hướng an toàn. Bug thành "không thấy dữ liệu" (ồn, phát hiện ngay) thay vì "thấy dữ liệu của khách khác" (im lặng, phát hiện khi đã muộn).

## `IM-7` · `RlsGuard` suy danh sách bảng TỪ MODEL

`AppDbContext.TenantScopedTables` liệt kê mọi entity cài `ITenantScoped`, đối chiếu với `pg_policies` lúc khởi động.

→ Thêm một entity tenant-scoped mà migration quên bật RLS thì **ném lỗi lúc start**, không rò rỉ lúc chạy. Danh sách không thể lệch với model vì nó **là** model.

Đây là `G7` ("tenant boundary là nền tảng") được hiện thực thành một cơ chế, không phải một lời nhắc.

## `IM-8` · `CanonicalCase` mỏng có chủ đích

Chưa có: `CaseEvent`, `OwnershipSegment`, `WaitingSegment`, `CaseProblem`, `CaseClaim`, `Classification`, `CaseAction`, `CaseOutcome`, `CaseRelation`.

Path A chỉ cần **tìm và gom** case. Luật §6.7 áp vào implementation: vừa đủ để chạy.

## `IM-9` · Policy phải dùng `nullif`, không chỉ `current_setting`

Ba quyết định trước (`IM-5`, `IM-6`, `IM-7`) viết ra khi chưa có Postgres. Chạy thật
cho thấy `IM-6` **chỉ đúng một nửa**.

```text
Session CHƯA BAO GIỜ đặt tenant    current_setting → NULL   → thấy 0 dòng   ✅ đúng như IM-6
Session ĐÃ đặt rồi RESET           current_setting → ''     → '': :uuid NÉM LỖI
```

Vế thứ hai chính là chuyện **connection pool** làm mỗi lần trả connection về pool.

```
ERROR:  invalid input syntax for type uuid: ""
```

Không rò rỉ — vẫn nghiêng về hướng an toàn. Nhưng thông báo lỗi đó **không nhắc tới
tenant**. Người gặp nó đi tìm bug ép kiểu, không đi tìm biến session. Ở đúng chỗ nhạy
cảm nhất của hệ thống, một thông báo lỗi sai hướng là chi phí thật.

→ `nullif(current_setting('app.current_tenant', true), '')::uuid`. Chuỗi rỗng thành
NULL, `TenantId = NULL` là NULL, policy từ chối → về đúng hành vi `IM-6` mô tả.

→ Sửa bằng **migration thứ hai**, không sửa migration đầu. Migration đã apply ở đâu
đó là lịch sử, không phải bản nháp.

## `IM-10` · Interceptor ghi tenant TRONG MỌI TRƯỜNG HỢP, kể cả khi chưa xác định được

`TenantConnectionInterceptor` đặt `app.current_tenant` mỗi lần một connection được mở.
Câu hỏi là: khi `ITenantContext.IsResolved` **false** (job hệ thống, health check, host
app không gửi tenant) thì làm gì?

```text
Lựa chọn A  không ghi gì            → connection lấy từ pool có thể còn giữ tenant
                                      của REQUEST TRƯỚC → đọc dữ liệu khách khác,
                                      im lặng, không log
Lựa chọn B  ghi chuỗi rỗng          → policy hiểu là "không có tenant" → 0 dòng
```

→ Chọn B. Và chọn B **bắt buộc** phải có `IM-9`: không có `nullif` thì lựa chọn này
làm mọi truy vấn của job hệ thống văng lỗi ép kiểu. Hai quyết định này khớp nhau,
không độc lập — đã kiểm: gỡ `nullif` ra thì đúng 3 test đỏ, cả ba đều là ca "không
có tenant".

⚠️ Đặt ở **tầng connection**, không phải trong từng repository. Cùng lý do với `AR2`:
nếu việc đặt tenant nằm trong code truy vấn thì nó lại phụ thuộc vào việc không lập
trình viên nào quên — trái `G7`.

⚠️ **Npgsql multiplexing phải TẮT** (mặc định tắt). Multiplexing trộn lệnh của nhiều
nơi lên một connection vật lý, nên biến session không còn thuộc về ai. Bật nó lên là
cơ chế tenant phải đổi cách khác.

## `IM-11` · Infrastructure phải khai báo tường minh phiên bản EF Core

Không phải quyết định domain, nhưng là cái bẫy mất 20 phút của người tiếp theo.

```text
Design package có PrivateAssets=all  → EF Core 10.0.11 KHÔNG chảy sang project khác
Provider Npgsql 10.0.3 khai báo       → EF Core 10.0.4
Kết quả: Infrastructure biên dịch với 10.0.11, consumer nhận 10.0.4
         → CS1705 "uses a higher version than referenced assembly"
```

→ Thêm `Microsoft.EntityFrameworkCore.Relational` 10.0.11 **không private** vào
Infrastructure. Gặp thật khi thêm test project; project host sau này sẽ gặp y hệt.

## `IM-12` · Host KHÔNG đọc cấu hình trước khi `builder.Build()`

Nghe như chuyện kỹ thuật nhỏ, nhưng nó quyết định hai thứ lớn.

```text
1  G13 dùng MỘT đường code cho hai chế độ deploy
   Nếu chế độ được đọc trước khi build rồi rẽ nhánh đăng ký dịch vụ, thì hai
   chế độ thành hai đường code — và đường ít chạy hơn sẽ mục dần mà không ai
   biết, đúng loại lỗi G13 sinh ra để chặn.

2  Test tích hợp GHI ĐÈ ĐƯỢC cấu hình
   WebApplicationFactory chỉ chen được vào cấu hình khi build. Code đọc cấu
   hình trước đó nhìn thấy giá trị của máy dev, nên "test cấu hình" hoá ra là
   test cấu hình của người viết test. Ba trong 11 test API là ca CẤU HÌNH SAI
   PHẢI KHÔNG START ĐƯỢC — không ghi đè được thì không viết được test đó.
```

→ Mọi thứ đọc cấu hình đều nằm sau `Build()`: `AddDbContext` dùng bản
`(sp, options)`, `TenantDirectory` dựng bằng factory, và toàn bộ việc kiểm nằm
trong `StartupChecks` chạy trước `app.Run()`.

## `IM-13` · Chế độ shared TỪ CHỐI KHỞI ĐỘNG khi chưa có xác thực

Ở chế độ shared, tenant đến từ header `X-Tenant-Key`. Hiện **không có gì** kiểm
người gọi có quyền dùng khoá đó — biết khoá là đọc được dữ liệu.

```text
Lựa chọn A  tự phát minh một cơ chế xác thực ngay      → 06 §0 nói rõ danh sách
                                                         endpoint và cơ chế là
                                                         việc của workstream này,
                                                         nhưng XÁC THỰC chưa ai
                                                         quyết → đúng kiểu §6.9
Lựa chọn B  để đó, ghi TODO                            → deploy được do sơ suất
Lựa chọn C  chạy được, nhưng phải NÓI RA tường minh    ← chọn
```

→ `Tenancy:AcknowledgeUnauthenticatedTenantHeader` phải bằng `true`, không thì
ném lúc khởi động kèm giải thích đầy đủ. Cờ này **không bảo vệ gì** — cờ nào cũng
bật được. Nó chỉ biến việc deploy một API chưa xác thực từ **sơ suất** thành
**quyết định**. Cùng tinh thần `IM-2` và `IM-5`.

**→ Sinh `AR-e`.** Xem §5.

⚠️ Chế độ dedicated không có câu hỏi này: tenant đến từ cấu hình của chính bản
deploy, không từ người gọi. Đó là chế độ của khách hàng #0 (`D3`) — nên mảnh còn
thiếu **không chặn** khách hàng đầu tiên.

## `IM-14` · Danh bạ tenant nằm NGOÀI ranh giới tenant

`TenantDirectory` (đổi `ExternalKey` thành `TenantId`) **không** dùng
`AppDbContext`. Ban đầu tưởng là hạn chế của DI: `AppDbContext` cần
`ITenantContext`, mà lúc này `ITenantContext` đang đi tìm chính tenant của mình.

Nhưng vòng tròn đó nói lên một điều thật: **việc tra tenant không thể nằm trong
ranh giới tenant.** Nó cũng giải thích vì sao `kp.tenant` là bảng duy nhất không
có RLS — nó là danh bạ, không phải dữ liệu của một khách hàng nào.

→ Dùng `NpgsqlConnection` trực tiếp, một truy vấn khoá chính, KHÔNG cache (§6.7).
Cache sai ở đúng chỗ này nghĩa là **phục vụ sai khách hàng**, đắt hơn nhiều chỗ
nó tiết kiệm.

**Về endpoint `/internal/tenant-boundary`:** nó là endpoint HẠ TẦNG, không phải bề
mặt sản phẩm (`G11` — không tự phỏng to capability đã chốt). Nó trả lời đúng một
câu hỏi vận hành: *"trên bản deploy NÀY, ranh giới tenant có đang sống không?"* —
bằng một câu SQL thô cố ý không có điều kiện tenant. Hai khách hàng gọi cùng
endpoint đó phải thấy hai con số khác nhau; đó là `AR2` ở dạng đo được bằng `curl`.

## `IM-15` · Kênh 1 dừng ở ô "tìm hoặc tạo Case", và response nói đúng điều đó

`POST /signals/case-observed` nhận tín hiệu và tạo Case. Các ô sau của sơ đồ —
khớp quy trình đã duyệt, suy ra bước hiện tại, tra tri thức, trả gợi ý — **chưa
build**.

→ Response chỉ có ba trường: `received`, `created`, `results`. Cố ý KHÔNG có
`suggestions: []` hay `process: null`. Một trường rỗng làm bên gọi tưởng đường đó
đã tồn tại và chỉ đang không có gì trả về — đúng cột phải của `G11`. Có một test
khoá đúng ba trường này.

**Tín hiệu lặp lại không sinh Case trùng**, và được bảo vệ hai lớp:

```text
Lớp 1  kiểm trước khi ghi           bắt ca thường
Lớp 2  unique (TenantId, SourceRef) bắt ca hai tín hiệu tới CÙNG LÚC
       — index này có từ migration đầu, không phải thêm mới
```

⚠️ `TenantId` nằm TRONG unique index đó, và điều đó quan trọng: hai khách hàng đều
có `jira:ES-1234` mà là hai việc khác nhau. Bỏ `TenantId` ra khỏi index thì tín
hiệu của khách B sẽ **trả về Case của khách A** — rò rỉ qua một đường không ai nghĩ
tới. Có test riêng cho ca này.

**Nhận một MẢNG tín hiệu, không phải một tín hiệu.** Lô một phần tử là ca thường
gặp; lô lớn là đường nạp Case lịch sử. Tách hai endpoint là hai đường code làm cùng
một việc, và đường ít chạy hơn sẽ mục — cùng lý do `IM-12`.

## `IM-16` · Lô vượt trần thì TỪ CHỐI CẢ LÔ, không cắt bớt

Cắt bớt im lặng là kiểu thất bại tệ nhất ở đường nạp dữ liệu: bên gửi thấy `200`,
tưởng đã nạp hết, và phần thiếu chỉ lộ ra nhiều tuần sau khi có người hỏi *"sao
thiếu case"*.

→ Vượt `Ingest:MaxSignalsPerRequest` là `400` kèm nói rõ trần là bao nhiêu. Một tín
hiệu sai định dạng cũng làm cả lô bị từ chối — không ghi một nửa, vì "một nửa" là
trạng thái không ai truy được về sau.

## `IM-17` · Endpoint GHI có chốt riêng, và chốt đó KHÔNG phải câu trả lời cho `AR-e`

Endpoint tín hiệu là endpoint **ghi**, khác `/internal/tenant-boundary` (chỉ đọc).
Không xác thực nghĩa là bất kỳ ai cũng bơm được Case giả vào dữ liệu khách hàng —
không crash, không báo, chỉ làm sai kho tri thức và sai luôn `M2`.

→ `Ingest:SignalApiKey`, so sánh theo thời gian hằng số. Không có khoá và không
thừa nhận tường minh thì **không khởi động được**.

⚠️ Khoá dùng chung **không** giải quyết `AR-e`: nó không phân biệt khách A với khách
B ở chế độ shared, không thu hồi theo từng khách, không chống replay. Nó chỉ là cái
chốt trong lúc chờ, và được ghi rõ như vậy ở cả code lẫn thông báo lỗi.

**Thứ tự filter là một quyết định, không phải chi tiết:** xác thực chạy TRƯỚC khi
tra tenant. Ngược lại thì người không có khoá vẫn phân biệt được `400` ("khoá tenant
này không tồn tại") với `401` — tức là dò được danh sách khách hàng mà không cần
khoá nào. Có test khoá đúng thứ tự này, và nó đỏ khi đảo hai dòng.

---

# 4. Chưa build — phần còn lại của slice Path A

```text
· Hai loại tín hiệu còn lại của 06 §1      hiện chỉ có "có việc mới ở nguồn".
                                           Còn: người dùng đổi trạng thái ·
                                           người dùng hỏi về tài liệu
· Truy vấn "tìm N case cũ liên quan"       Q-C đã chốt là dependency của Cap 3
                                           AR4: Postgres FTS trước
· ISoạnNhápSOP → Anthropic SDK             AR3 interface mỏng · structured outputs
                                           · Batches API (S5: Path A không nhạy latency)
· Luồng duyệt (S7 một hành động)           gọi KnowledgeRecord.Approve
· Tính diff(A,B) cho M2
```

**Ranh giới tenant giờ đã ĐÓNG hết một vòng** — từ một request HTTP thật, qua
`ITenantContext`, qua interceptor, xuống policy của Postgres, và có 20 test giữ ở
hai tầng. Mọi thứ trong danh sách trên xây trên một nền đã được đo, không phải trên
một nền được cho là đúng.

Việc tiếp theo nên là **truy vấn "tìm N case cũ liên quan"** — nó là dependency đầu
tiên của Path A và giờ đã có chỗ để chạy: một host sống, một tenant thật, một
database có RLS đang làm việc.

---

# 5. Open Questions sinh ra từ implementation

```text
AR-a   Tập giá trị của visibility scope là gì?
       S7 khoá QUY TẮC nhưng không khoá TẬP GIÁ TRỊ. Code để dạng chuỗi đục
       thay vì phát minh enum (IM-3). Đây là câu hỏi DOMAIN, cần quyết ở tầng
       domain rồi mới siết ở code. Gắn với Q-D (còn OPEN, hoãn v2).

AR-b   Hai trigger còn lại của NEEDS_REVIEW (V3) chưa hiện thực được:
         · quan hệ CONTRADICTS tới record khác  → cần L4 (KnowledgeRelation)
         · nguồn chống lưng bị đổi/xoá          → cần theo dõi thay đổi nguồn
       Ba trigger còn lại đã hiện thực. Ghi rõ trong code để không ai tưởng đủ.

AR-c   ĐÓNG 2026-08-24. RLS đã kiểm trên PostgreSQL 18.6 thật, bằng code C#
       thật, với role KHÔNG phải superuser. 9/9 test xanh, và đã chứng minh bộ
       test biết ĐỎ (gỡ FORCE → 5 đỏ; gỡ nullif → 3 đỏ). Sinh ra IM-9 và IM-10.

AR-d   Chuỗi kết nối và mật khẩu DB lấy từ đâu ở deploy thật?
       ĐÃ CÓ HÌNH DẠNG, chưa chốt nguồn. Host đọc ConnectionStrings:Default từ
       IConfiguration, nên biến môi trường ConnectionStrings__Default hoặc bất kỳ
       secret provider nào của .NET đều cắm vào được, không sửa code. Thiếu nó là
       KHÔNG START ĐƯỢC. Còn phải quyết: dùng secret store nào ở deploy thật.

AR-e   Chế độ shared multi-tenant xác thực người gọi bằng cách gì?          ← MỚI
       Hiện tenant đến từ header X-Tenant-Key và KHÔNG có gì kiểm người gọi có
       quyền dùng khoá đó. Biết khoá là đọc được dữ liệu của khách hàng đó.
       Chế độ shared vì thế TỪ CHỐI KHỞI ĐỘNG trừ khi được thừa nhận tường minh
       (IM-13). Đây là câu hỏi cần quyết ở tầng sản phẩm, không phải tầng code:
       API key theo tenant? mTLS? chữ ký trên payload tín hiệu? Gắn với 06 §1
       ("phần mềm có sẵn của khách phát tín hiệu" — ai chứng minh mình là ai).

       ⚠ KHÔNG chặn khách hàng #0: D3 nói khách #0 là công ty của người dùng, và
         bản deploy dedicated lấy tenant từ cấu hình chứ không từ người gọi.
```

---

# 6. Nguyên tắc cốt lõi của slice này

> **Hai chỗ dễ sai IM LẶNG nhất (`AP3` provenance và `G13` tenant) phải được biến từ "nhớ thì làm" thành "không thể quên".**

```text
AP3   Origin/Verification là required, không có giá trị mặc định
      → nơi tạo assertion BUỘC phải nói nó đến từ đâu
G13   ITenantContext là interface được inject, không phải static
      → hai chế độ deploy dùng cùng codebase
G7    RlsGuard kiểm lúc khởi động, danh sách suy từ model
      → quên RLS = không start được, không phải rò rỉ
V3    enum lưu chỉ có 3 giá trị
      → vi phạm V3 thành hành động tường minh, không phải sơ suất
```

Bốn cơ chế trên là toàn bộ giá trị của slice nền móng này. Phần LLM là phần dễ.

---

# 7. Điều học được ngày 2026-08-24

> **Bốn cơ chế trên đúng về thiết kế. Nhưng "thiết kế đúng" không phải "chạy đúng",
> và với thất bại IM LẶNG thì khoảng cách giữa hai thứ đó là chỗ bug sống.**

```text
Đọc SQL thấy được       cú pháp · thiếu FORCE · thiếu policy
Đọc SQL KHÔNG thấy      '': :uuid văng lỗi          → IM-9
                        superuser bỏ qua cả FORCE   → bộ đo tự hỏng
                        connection pool giữ tenant cũ → IM-10
```

Nên từ đây, mỗi cơ chế chống-thất-bại-im-lặng cần một test **đã được chứng minh là
biết đỏ**. Test xanh mà không thể đỏ thì không phải bằng chứng, nó chỉ là sự yên tâm.

Áp luôn cho project host cùng ngày: gỡ `AddInterceptors` khỏi `Program.cs` → đúng 4
test API đỏ, và cả 4 đều là ca "hai khách hàng phải thấy hai bộ dữ liệu". Đó là bằng
chứng rằng 11 test kia đang đo mắt xích chứ không đo lại policy của Postgres.

---

# 8. Điều học được ngày 2026-08-25

> **Một cơ chế bảo vệ đúng vẫn có thể làm bộ test hỏng, nếu chính bộ test tạo ra
> trạng thái mà cơ chế đó chặn.**

Test `/health/ready` tắt row-level security vài chục milli-giây để chứng minh nó
biết báo `503`. Trong khoảng đó, mọi host khởi động đều bị `RlsGuard` chặn — đúng
như `IM-7` thiết kế. Kết quả: một test Kênh 1 đỏ **ngẫu nhiên** khi chạy cả bộ, xanh
khi chạy một mình, và thông báo lỗi trỏ vào RLS chứ không trỏ vào nguyên nhân thật.

```text
Cơ chế       ĐÚNG    RlsGuard chặn host khởi động trên DB có RLS bị tắt
Bộ test      SAI     tự tạo trạng thái đó rồi để test khác đi vào
```

→ Chạy tuần tự trong project test API, và ghi lý do ngay trong `AssemblyInfo.cs`.
Chỗ đó là chỗ người tiếp theo sẽ tìm khi họ định bật lại chạy song song.

**Điều đáng nhớ:** kiểu lỗi này chỉ hiện ra khi chạy CẢ BỘ. Chạy từng test một —
việc rất tự nhiên khi đang viết test — sẽ không bao giờ thấy nó.
