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
> **Cập nhật 2026-08-30:** có bộ test API **Postman** (`scripts/postman/`, 13 request).
> ⚠️ Và phát hiện một **lỗ trong kế hoạch**: `evidence_item` chưa có đường ghi nào,
> nên Path A không có nội dung để gom. Sinh `AR-f`. Thứ tự §4 đã sửa.
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
    Signals/CaseEvidenceSignal.cs       hợp đồng evidence — K-B9: link case TUỲ CHỌN
    Signals/CaseEvidenceHandler.cs      nạp nội dung của case, idempotent  ← AR-f

tests/KnowledgePlatform.Domain.Tests/    ← KHÔNG cần PostgreSQL, cố ý
    KnowledgeBuilder.cs                 vật liệu test
    KnowledgeRecordLifecycleTests.cs    V2 · V4(a) · S7 · D4
    NeedsReviewTests.cs                 V3 — 3/5 trigger, ghim rõ 2 cái còn thiếu
    DisplayStateTests.cs                V3 — gắn cờ chứ không rút · stub IsSuperseded
    VerificationLadderTests.cs          V1 — thang không phải đường thẳng
    AssertionTests.cs                   M2 · AP3 · L3

tests/KnowledgePlatform.Infrastructure.Tests/
    TestDatabase.cs                     fixture — chạy trên PostgreSQL THẬT
    TenantIsolationTests.cs             9 test cách ly tenant ở tầng DB

tests/KnowledgePlatform.Api.Tests/
    ApiFactory.cs                       dựng host thật, DB riêng kp_api_test
    TenantBoundaryThroughHttpTests.cs   11 test cách ly tenant qua HTTP THẬT
    CaseSignalTests.cs                  13 test Kênh 1
    CaseEvidenceTests.cs                16 test nạp evidence            ← AR-f
    AssemblyInfo.cs                     chạy tuần tự — lý do ghi trong file

scripts/dev-db-setup.sql                role kp_app + 3 database
scripts/postman/                        bộ test API dev — 13 request, 4 nhóm (A/B/C/D)
                                        đã gọi THẬT vào app đang chạy trước khi đóng gói
                                        ⚠ chưa commit tính tới 2026-08-30
```

## Trạng thái verify

```text
✅  dotnet build          toàn solution, 0 error 0 warning
✅  dotnet ef migrations   sinh được
✅  apply migration        PostgreSQL 18.6 local · kp_dev + kp_test · cả 2 migration
✅  RlsGuard chạy thật     PASS trên DB sống, bằng code C# thật (không phải SQL tay)
✅  luật domain thuần     48/48 test xanh trong 77ms, KHÔNG cần PostgreSQL   ← IM-18
✅  cách ly tenant (DB)    9/9 test xanh, chạy bằng role KHÔNG phải superuser
✅  cách ly tenant (HTTP)  11/11 test xanh, qua host thật, cả hai chế độ G13
✅  Kênh 1 chạy thật       13/13 test xanh · curl: 3 tín hiệu → 3 Case,
                           gửi lại → 0 Case mới, khách khác không thấy gì
✅  bộ test có thể ĐỎ      gỡ FORCE khỏi một bảng     → 5 test đỏ
                           gỡ nullif khỏi policy       → 3 test đỏ
                           gỡ interceptor khỏi host    → 4 test API đỏ
                           đảo thứ tự hai filter       → 1 test đỏ
                           gỡ trần lô tín hiệu         → 1 test đỏ
    bộ test domain, 5 phép đột biến — mỗi phép sửa src rồi khôi phục:
                           bỏ `Lifecycle == Active` khỏi NeedsReview  → 2 test đỏ
                           bỏ `a.IsCurrent` khỏi NeedsReview          → 1 test đỏ
                           Approve quên ghi phạm vi xem TRƯỚC (S7)    → 1 test đỏ
                           IsOnLadder nhận cả Conflicting             → 1 test đỏ
                           mở setter public cho VisibilityScope       → 1 test đỏ
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

## `IM-24` · Mốc thời gian có múi giờ làm endpoint trả 500 — chỉ dữ liệu THẬT mới lộ

**Ghi 2026-09-04.** Nạp fixture Jira thật vào `kp_dev` thì `POST /signals/case-observed`
trả **500** ngay tín hiệu đầu tiên:

```text
System.ArgumentException: Cannot write DateTimeOffset with Offset=07:00:00 to
PostgreSQL type 'timestamp with time zone', only offset 0 (UTC) is supported.
```

Npgsql từ chối mọi `DateTimeOffset` có offset khác 0. Jira Server trả `+07:00`. Và vì
lỗi bật ra từ tận `SaveChangesAsync` nên nó thành **500**, không phải 400 — một đầu vào
**hợp lệ theo ISO 8601** bị báo là lỗi máy chủ, và bên gửi không có cách nào biết phải
sửa gì.

**Điều đáng nhớ không phải cái bug, mà là VÌ SAO 103 test không thấy nó.**
Mọi mốc thời gian trong bộ test đều do chính bộ test dựng ra — và tay người viết test
thì luôn viết UTC, hoặc để `null` (xem tham số `observedAt` mặc định của hàm `Evidence()`
trong `CaseEvidenceTests`). Một bộ test tự cấp vật liệu cho mình chỉ kiểm được những
hình dạng mà người viết nghĩ ra. Đây là lý do thứ hai trong cùng một ngày để nói rằng
**fixture dữ liệu thật có giá trị mà dữ liệu bịa không có** — lý do thứ nhất là `AR-k`.

**Sửa ở đâu, và vì sao không sửa ở handler.** Đặt trong `AppDbContext.ConfigureConventions`
qua `UtcDateTimeOffsetConverter`: nó áp cho MỌI thuộc tính `DateTimeOffset` của MỌI
entity, **kể cả entity chưa ai viết**. Sửa ở handler thì mỗi chỗ ghi mới lại phải nhớ
một lần nữa — cùng lý do `TenantScopedTables` suy ra từ model thay vì viết tay.

**Không phải workaround, mà là đúng ngữ nghĩa.** `2026-09-01T10:00:00+07:00` và
`2026-09-01T03:00:00Z` là cùng một khoảnh khắc. Thứ mất đi là *"người ghi nhận ở múi
giờ nào"* — mà cột `timestamptz` chưa bao giờ lưu được điều đó, kể cả trước khi có lớp
này. Ngày nào cần biết múi giờ gốc thì nó phải là một **CỘT RIÊNG do bên gửi khai**
(đúng tinh thần `IM-19`), không phải một hy vọng rằng offset tự sống sót.

2 test mới, và đã chứng minh biết ĐỎ: gỡ hai dòng `HaveConversion` khỏi
`ConfigureConventions` → đúng 2 test đó đỏ, 40 test còn lại vẫn xanh.
Hai test kiểm CẢ HAI thứ: endpoint trả 200, **và** mốc thời gian lưu xuống là cùng một
thời điểm chứ không bị dịch đi 7 tiếng — một phép chuyển sai vẫn ghi được xuống DB, chỉ
là ghi sai giờ, và không ai phát hiện cho tới khi Path A xếp case theo thời gian.

---

## `IM-29` · Bản B ĐẦU TIÊN — sinh không cần credit, và nó phơi ra một trần cấu trúc của cách gom theo nhóm

Ngày 2026-09-10. Tài khoản API chờ tổ chức duyệt, `count_tokens` cũng bị cổng credit chặn
(chỉ `GET /v1/models` đi qua). Chủ dự án hỏi có cách test tạm nào không. Có, và nó trả lời
được **câu đắt hơn** câu mà lượt gọi thật trả lời.

```text
tools/SoanNhapRunner --xuat-payload <file>     xuất payload SẼ gửi, rồi dừng
  · đặt SAU cổng che AR-o        -> không nhánh nào xuất được payload chưa che
  · từ chối ghi vào chỗ git theo dõi (exit 7)  -> đã thử, KHÔNG tạo file
  · bắt truyền đường dẫn tường minh, không có mặc định tiện tay
payload nhóm 1: system 2 201 ký tự · user 19 891 ký tự · che 0 chỗ · 41 KB
```

⚠ `SopDraftSchema` đổi `internal` → `public` để xuất được schema cùng payload. Lý do ghi
trong chính file đó: `description` của từng trường **là một phần của prompt thật**, nên bản
xuất thiếu schema là bản xuất không trung thực.

**Cái bẫy đã xử lý TRƯỚC khi đo, và nó quan trọng hơn phép đo:** phiên đang chạy đã đọc
`docs/00`, nơi trích khá chi tiết bản A. Một bản B viết bởi ngữ cảnh đó đo **việc nhớ bản
A**, không đo sản phẩm — và nó sẽ ra con số nhỏ trông như tin tốt, đúng hố mà vòng duyệt 1
đã rơi vào. Nên bản B do một ngữ cảnh SẠCH sinh: chỉ đọc file payload, bị cấm `docs/11`,
`docs/00`, `docs/07`, hai file cây, `git log`, và **cấm cả bộ eval** — vì model qua API cũng
chỉ có một lượt và không thấy eval. Cấm eval là chỗ dễ bỏ sót nhất: cho nó tự kiểm rồi sửa
là biến phép đo một-lượt thành phép đo có-vòng-lặp.

✅ **`--kiem-cay` mã thoát 0.** Phép cộng case khớp, không case nào bị bỏ hay đếm hai lần.

```text
                buocKiem  buocSua  nhanh  co-nguon  trieuChung  khoangTrong
bản A (người)       5        6       13     7 (54%)      5            4
bản B (máy)         4        8       16     8 (50%)      8           13
```

🎯 **`diff(A,B)` KHÁC 0 — tín hiệu `M2` có nghĩa đầu tiên, và tốn $0 tiền API.** Vòng duyệt
1 cho ra 0 vô nghĩa (support trả về trắng). Đây là số đầu tiên đến từ hai bản thật sự độc
lập. ⚠ Nhưng ĐỪNG đọc nó thành "điểm M2": *cách tính* `diff(A,B)` vẫn là câu chưa quyết
(`docs/11` §8 mục 3 — mức chứng cứ đặt ở tầng nhánh hay tầng SOP, và diff có đọc theo
nhánh-không-nguồn hay không). Ở đây chỉ là so sánh mô tả.

**Bốn trong năm bước kiểm hội tụ độc lập.** B dựng lại K1→K4 của A gần như cùng nội dung:
form/mã đặt phòng → tài khoản + phân quyền trên trang quản trị → role PMS có quyền "Thêm
hoá đơn" → site/MST mới đã khai ký hiệu chưa. Prompt của `SopPromptBuilder` **đủ** để tới
đó, và đó là điều lượt gọi thật không cần chứng minh lại.

### 🛑 Phát hiện nặng nhất: luật chống-bịa-nguồn chính là thứ chặn B dựng được K5

B **thiếu hẳn K5 của A** — *"trình duyệt đang đăng nhập nhiều tài khoản cùng lúc không?"*.
Và K5 không phải nhánh yếu: nó là `evidence-noi-ro`, chống lưng bởi `ES-341290` **và
`ES-343036`**. Truy `ES-343036` thì ra lý do:

```text
ES-343036 KHÔNG thuộc nhóm này. Bản A cố ý mượn nó từ nhóm "xung đột phiên"
làm mốc cho một BƯỚC LOẠI TRỪ, và ghi rõ việc mượn ở khối _haiCaseLech.
```

Trong khi luật mà `G6`/`AP3` đòi và bộ eval thi hành là: **mã case model viết ra phải TỒN
TẠI trong nhóm.** Luật đó tồn tại để chặn model bịa nguồn, và nó làm đúng việc đó. Nhưng
cùng lúc nó **rào máy ở trong một nhóm**, nên máy không thể dựng bước loại trừ trỏ sang
nhóm khác — thứ người viết làm rất tự nhiên.

⚠ Và bộ eval **không phân biệt được** "không bịa" với "không vươn tới được": B đặt
`_haiCaseLech: null`, trích 9 mã đều trong nhóm, nên phép kiểm chống bịa nguồn **qua một
cách tầm thường**.

→ Đây là lần đo **thứ hai, từ chiều ngược lại**, của cùng một điều `docs/11` §9 đã ghi:
*bước kiểm đầu tiên của nhóm 2 bị chặn bởi đúng nguyên nhân của nhóm 1.* Lần đó là hai
nhóm phụ thuộc nhau khi THAO TÁC; lần này là một bước loại trừ phải MƯỢN case của nhóm
khác. Hai quan sát độc lập, cùng một kết luận: **ranh giới nhóm của taxonomy không phải
ranh giới hợp lệ của một SOP.** Đưa `ISoạnNhápSOP` mỗi lần một nhóm là có **trần cấu
trúc**, không phải hạn chế tạm thời của prompt.

Câu phải quyết, và nó chạm vào cả `G6` lẫn hình dạng đầu vào:

```text
cho phép trích case NGOÀI nhóm để dựng bước loại trừ, với điều kiện gì?
  · nếu KHÔNG: máy vĩnh viễn không dựng được bước loại trừ liên nhóm
  · nếu CÓ:    phải cấp corpus rộng hơn một nhóm, và phép kiểm chống
               bịa nguồn phải đổi từ "trong nhóm" sang "trong corpus"
               + bắt khai chỗ mượn (đúng như _haiCaseLech của bản A)
```

### Phát hiện thứ hai: bộ 5 ô cố định chốt được TÊN ô, chưa chốt được ĐỊNH NGHĨA ô

`IM-27` đặt bộ 5 ô cố định vì *"phân bố mà mỗi nhóm tự đặt tên là phân bố không so được
giữa các nhóm"*. Đúng, nhưng chưa đủ. Cùng bộ ô, hai người đọc xếp khác nhau ba ticket:

```text
                              bản A                        bản B
ES-341317   suy-ra-được                        ->  ghi-rõ-bước-kiểm
ES-343733   bước-kiểm-NGOÀI-ticket (remote)    ->  ghi-rõ-bước-kiểm
ES-346559   ghi-rõ-bước-kiểm                   ->  suy-ra-được
ô "ngoài ticket"        A: 1 case                  B: 0 case
```

🛑 Ô về 0 ở B là chỗ đáng lo nhất, vì đó là ô mang **phát hiện nền của cả dự án** — chẩn
đoán xảy ra trên remote/điện thoại và không để lại chữ nào. B **có thấy** hiện tượng (nó
viết ra: *"mọi kết luận đều nằm trong ticket, dù việc kiểm thật xảy ra qua zalo/cuộc
gọi/ultraview"*) nhưng đọc tên ô thành *"kết luận ngoài ticket"* rồi xếp sang ô khác.

→ Một ô mà hai người đọc hiểu khác nhau thì phân bố **vẫn không so được** — đúng thứ `S8`
sinh nó ra để tránh. Việc còn lại: viết **định nghĩa + ca biên** cho từng ô vào chính
`description` của schema, rồi đo lại bằng cách cho hai ngữ cảnh sạch xếp cùng một nhóm.
B tự nêu đúng chỗ này trong `khoangTrongPhaiBiet` (13 mục, so với 4 của A).

### Ba thứ phép thử này KHÔNG chứng minh — ghi kẻo đọc lại tưởng đã xong

```text
1  output_config.format có nhận "type": ["string","null"] hay không   VẪN LÀ ẨN SỐ
2  đường HTTP của AnthropicSopDrafter, số token, giá thật             CHƯA ĐO
3  structured outputs có RÀNG BUỘC được đầu ra hay không              CHƯA BIẾT
```

Điểm 3 tinh vi nhất và dễ nhầm nhất: một ngữ cảnh đọc file payload tuân thủ schema **tự
nguyện**; qua API thì bị **ràng buộc**. `--kiem-cay` xanh ở đây **không** chứng minh lượt
gọi thật sẽ xanh. Nó chứng minh prompt đủ và hình dạng đầu ra khả thi — hai thứ khác.

⚠ Sửa một lỗi phân tích tham số sẵn có trong lúc thêm cờ: `args.FirstOrDefault(a => !a
.StartsWith("--"))` lấy tên nhóm bằng "token đầu không phải cờ", nhưng đường dẫn đi sau
`--ra` cũng không bắt đầu bằng `--`. Gõ cờ trước tên nhóm là lấy đường dẫn làm tên nhóm
rồi báo "khớp 0 nhóm" — một thông điệp chỉ về sai hướng hoàn toàn. Chưa vấp vì mọi ví dụ
đều đặt tên nhóm trước.

---

## `IM-28` · Ẩn số thứ ba hoá ra tra được, và phép kiểm "còn credit không" là MIỄN PHÍ

Ngày 2026-09-10, trước khi định tiêu tiền cho lượt gọi thật, áp lại đúng luật của `IM-27`
(*trước khi gọi một API tốn tiền, hỏi phần nào kiểm được mà không cần gọi nó*) — lần này
lên chính cái danh sách ẩn số mà `IM-26`/`IM-27` để lại. Ra hai thứ.

**1 · `$defs`/`$ref` ĐƯỢC HỖ TRỢ — tài liệu nói, không cần đo.**

`IM-27` ghi việc bỏ `$ref` là *"cái giá đúng cho việc bỏ một ẩn số không đo được miễn phí"*.
Câu đó **sai ở vế cuối**: ẩn số đó đo được miễn phí, chỉ là bằng cách tra tài liệu chứ
không bằng cách gọi API. Tập schema mà structured outputs nhận có ghi rõ:

```text
Supported:  enum · const · anyOf · allOf · $ref/$def · additionalProperties: false
Not:        schema đệ quy · minimum/maximum · minLength/maxLength · ràng buộc mảng phức
```

⚠ **KHÔNG đổi `SopDraftSchema` về `$ref` vì phát hiện này.** Bản viết thẳng 5 lần đang
chạy, đã có test canh, và khớp với thứ `--kiem-cay` đọc được. Đổi để cho gọn là chấp nhận
rủi ro thật lấy cái đẹp. Điều cần sửa là **niềm tin**, không phải mã: *"chỉ một lượt gọi
tốn tiền mới trả lời được"* đã đúng với một câu và sai với hai câu khác trong cùng danh
sách. Trước khi ghi một thứ là "không đo được miễn phí", hãy tra tài liệu trước.

**2 · Ẩn số nullable KHÔNG cần đề phòng trước, vì lượt hỏi nó là lượt MIỄN PHÍ.**

`"type": ["string","null"]` (ở `quyenCan`) vẫn chưa xác nhận: tài liệu liệt kê `null` như
kiểu cơ bản và `anyOf` như dạng được đỡ, nhưng **không** khẳng định dạng union-array. Cám
dỗ là đổi sẵn sang `anyOf: [{"type":"string"},{"type":"null"}]` cho chắc. Đã KHÔNG làm, và
lý do là một phép đo:

```text
schema bị từ chối  ->  HTTP 400 lúc validate request  ->  KHÔNG TÍNH TIỀN
```

Một ẩn số mà câu trả lời sai của nó tốn **$0** thì không đáng đổi mã để đề phòng. Đổi mã
thì tốn một vòng review + một lần chạy lại bộ test; để nguyên thì lượt gọi đầu tiên tự nói
ra. **Cái giá của việc SAI mới là thứ quyết định có nên phòng thủ trước hay không** — không
phải cái giá của việc biết.

**3 · Và phép kiểm đáng mang đi nhất: "còn credit không" hỏi được miễn phí.**

```text
POST /v1/messages  ·  payload 577 byte  ·  KHÔNG có một byte dữ liệu khách nào
-> http=400  "Your credit balance is too low to access the Anthropic API."
   request_id: req_011CeuKWEQeqxmEkQVMh1iZd
```

Lượt này tách được ba nguyên nhân mà trước đây `IM-26` chỉ gộp làm một cục "chặn ở credit":
**khoá sai** (401) · **hết credit** (400, thông điệp credit) · **schema bị từ chối** (400,
thông điệp schema). Cả ba đều trả lời trước khi có token nào được sinh, nên cả ba **miễn
phí**. Khoá đọc được dài 108 ký tự và xác thực qua — vấn đề chưa bao giờ nằm ở khoá.

⚠ `AR-o` **không áp dụng** cho lượt gọi này, và đó là một lựa chọn chứ không phải sơ suất:
payload thăm dò do tôi viết, không có mẩu ticket nào. Cổng che tồn tại để canh dữ liệu
khách; chạy nó lên một chuỗi hằng số là làm loãng ý nghĩa con số nó in ra.

⚠ Một bẫy môi trường đã vấp trong lúc làm, ghi vì nó sẽ lặp: `open()` của Python trên
Windows mặc định **cp1252**, nên đọc `appsettings.Local.json` (có chú thích tiếng Việt)
chết bằng `UnicodeDecodeError` ở byte 0x81 — và thông điệp lỗi trỏ về *file*, trong khi lỗi
nằm ở *cách mở file*. Mọi chỗ đọc file của repo này phải `encoding='utf-8'`.
`SoanNhapRunner` không dính: C# mặc định UTF-8.

---

## `IM-27` · Xoá hai trong ba ẩn số MÀ KHÔNG GỌI API — và lỗ tìm được là lỗi kiểu dữ liệu

`IM-26` liệt ba thứ chưa biết. Hai trong ba **kiểm được miễn phí**, và hoá ra phải kiểm:

```text
(1) serialize có ra đúng hình dạng bộ eval đọc được?   ✅ kiểm được không cần model
(2) API có nhận schema có "type": [...,"null"] không?  ❌ chỉ một lượt gọi thật trả lời
(3) bản nháp có qua được --kiem-cay không?             ✅ kiểm được không cần model
```

**Cách kiểm, và nó rẻ đến mức đáng ngượng vì không làm sớm hơn:** cho chính cây dựng TAY
đi qua kiểu C# `SopDraft` (deserialize → serialize) rồi ném bản ghi lại vào `--kiem-cay`.

🛑 **Kết quả lần đầu: 3 PHÁT HIỆN CHẶN, mã thoát 1.** `SopDraft` **thiếu hai khối** mà bộ
eval bắt buộc:

```text
phanBoBuocKiemDuocGhiLai   phân bố mà S8 đòi  -> eval: "phân bố cộng lại = 0, nhóm có 10 case"
_haiCaseLech               giải thích ticket ngoài nhóm -> eval: "ES-343036 … bịa nguồn"
```

Kiểu C# vẫn **build sạch** và **serialize sạch** — nó chỉ im lặng bỏ mất hai khối. Đúng
loại lỗi mà trình biên dịch không thấy và test đơn vị cũng không thấy, vì cả hai chỉ kiểm
những gì người viết nghĩ ra. Sau khi vá: **mã thoát 0.**

→ **Nếu chờ lượt gọi API mới biết thì đã trả tiền cho một lỗi kiểu dữ liệu.** Ghi lại như
một luật: *trước khi gọi một API tốn tiền, hãy hỏi phần nào của đường đi kiểm được mà
không cần gọi nó.* Ở đây là 2/3.

⚠ **Một quyết định thiết kế phát sinh:** phân bố dùng **BỘ 5 Ô CỐ ĐỊNH**, không để mỗi nhóm
tự đặt tên ô. Hai cây dựng tay đặt tên khác nhau (nhóm 1 có 5 ô, nhóm 2 có 3 ô và là bản
chia nhỏ của nhóm 1) — một phân bố mà mỗi nhóm tự đặt tên là phân bố **không so được giữa
các nhóm**, mà so được chính là lý do `S8` đòi nó. Hai file dựng tay GIỮ tên ô riêng của
chúng: `--kiem-cay` nhận cả hai dạng, và hai file đó là **bản A của phép đo `M2`** — sửa
chúng cho "khớp" là làm hỏng mốc so.

⚠ **Và một rủi ro đã bỏ đi thay vì đo:** schema bản đầu dùng `$defs`/`$ref` cho ô phân bố.
Chưa biết structured outputs có nhận không, và cách duy nhất để biết là một lượt gọi tốn
tiền. Đã **viết thẳng 5 lần** thay vì `$ref`. Dài dòng hơn, và đó là cái giá đúng cho việc
bỏ một ẩn số không đo được miễn phí.

⚠ Một lỗi của chính phép đo, ghi vì nó dễ lặp: lần chạy đầu tôi đọc `$?` **sau một pipe**
(`… | tail -8`), nên lấy mã thoát của `tail` chứ không của `--kiem-cay` — báo 0 trong khi
phép kiểm đang trả 1. Đo mã thoát thì đừng đo sau ống dẫn.

5 test mới canh hình dạng (`SopDraftShapeTests`), trong đó một test canh ĐÚNG hai khối đã
từng bị mất. Test: **147** (90 domain + 15 hạ tầng + 42 API).

## `IM-26` · Đường soạn nháp đã nối xong tới sát API — và cổng che đo được trên dữ liệu thật

Hiện thực `ISoạnNhápSOP` (2026-09-08), sau khi chủ dự án chốt đổi thứ tự ở `§4`.

```text
tools/SoanNhapRunner            lệnh chạy theo lô, KHÔNG phải endpoint trong Api
src/…/Infrastructure/Sop/       AnthropicSopDrafter · SopDraftSchema
src/…/Domain/Sop/               ISopDrafter · SopDraft · SopPromptBuilder
package                         Anthropic 12.46.0 (chính thức, owner Anthropic)
model                           claude-opus-5   (AR3)
```

⚠ **Vì sao là LỆNH chứ không phải endpoint** — ghi ra vì nó là một lựa chọn, không phải
thói quen: việc này chạy theo lô, không cần tenant middleware, không cần database (đầu vào
là file trên đĩa), và **mỗi lần chạy là tốn tiền**. Một endpoint thì ai gọi cũng được; một
lệnh thì phải có người gõ. Đó là cái chốt rẻ nhất cho một việc mất tiền.

✅ **CỔNG CHE `AR-o` ĐÃ ĐO ĐƯỢC TRÊN DỮ LIỆU THẬT, không phải trên test:**

```text
nhóm                                    ticket   mẩu   ký tự vào   ĐÃ CHE
Phân quyền & ký hiệu hoá đơn              10      14     17 295       0
NCC từ chối payload                       10      21     24 216       5
Kênh kết nối tới NCC mất hiệu lực          7      26     17 618       5
```

→ Con số **0 ở nhóm 1** không phải cổng hỏng: nhóm phân quyền không có mẩu nào chứa thông
tin đăng nhập. Tỉ lệ 5,2% là tỉ lệ TOÀN CORPUS, không phải tỉ lệ mỗi nhóm — và điều đó có
nghĩa: **nhóm nào cũng phải qua cổng, vì không đoán trước được nhóm nào có.** Nhóm "kênh
kết nối tới NCC mất hiệu lực" thì đúng như tên nó, dày credential nhất trên mỗi mẩu.

🛑 **CHẶN Ở CHỖ KHÔNG PHẢI CODE: tài khoản API hết credit.** Key hợp lệ (xác thực qua
được), request tới được API, và API trả về:
```text
invalid_request_error: "Your credit balance is too low to access the Anthropic API."
```
Nên **chưa có bản nháp nào do máy sinh ra**, và `M2` vẫn chưa có cặp (A, B) thật. Mọi thứ
TRƯỚC ranh giới mạng đã chạy: đọc taxonomy → gom case + evidence → dựng payload → cổng che
→ gọi SDK → bắt lỗi và nói ra đúng nguyên nhân.

⚠ **Điều này KHÔNG chứng minh adapter đúng.** Nó chứng minh: package đúng, kiểu dữ liệu
đúng (build sạch), xác thực đúng, và đường xử lý lỗi đúng. Ba thứ chưa biết, và biết là
chưa biết: (1) model có trả về JSON khớp `SopDraftSchema` không; (2) `output_config.format`
có nhận schema có `"type": ["string","null"]` không — nếu không thì phải bỏ nullable và
dùng chuỗi rỗng; (3) bản nháp có qua được `--kiem-cay` không, đặc biệt phép cộng `phanBo`.
Cả ba chỉ trả lời được bằng một lượt gọi thật.

## `IM-25` · Cổng che ở khâu GỬI RA — port từ luật Python, và đã ĐỐI CHIẾU trên corpus thật

Hiện thực `AR-o` (2026-09-08). `SecretShapeScanner` + `EgressRedactor` ở
`src/KnowledgePlatform.Domain/Redaction/`, **27 test**, và 5 phép đột biến đã chứng minh
bộ test biết ĐỎ (bỏ chữ `ẩ` trong "khẩu" → 5 đỏ · không bỏ URL trước khi quét → 1 đỏ ·
thay URL bằng `<URL>` thay vì khoảng trắng cùng độ dài → 1 đỏ · bỏ hình dạng 4 → 7 đỏ ·
hình dạng 3 chỉ nhìn 1 dòng thay vì 6 → 2 đỏ).

⚠ **Ba tính chất của cổng, mỗi cái vì một lý do đã ghi ở `AR-o`:** fail closed (che xong
QUÉT LẠI, còn bắt được gì ngoài dấu che thì ném, không gửi) · giữ DẤU `[ĐÃ CHE]` (`G6`/`AP3`:
người duyệt phải phân biệt "đã che" với "không có gì") · đếm được theo từng hình dạng.

✅ **VÀ ĐÃ ĐỐI CHIẾU VỚI LUẬT PYTHON TRÊN CÙNG CORPUS — đây là phần đáng nhất của mục này.**
Chạy cả hai trên `fixture-evidence.json` (345 mẩu):

```text
            mẩu có hit    tổng chỗ    nhãn+số   số trần   key JSON   công cụ+số
Python          18           40          18        13        9          0
C#              18           37          18        12        7          0
```

Cùng **18/345 mẩu**, cùng `nhãn+số`. Lệch **3 chỗ**, và đã truy từng chỗ:

```text
ES-346584  '17106'        Python đếm HAI lần: nhãn+số VÀ key JSON, cùng một vị trí
ES-343712  '92255'        y như trên
ES-345502  '110 143 652'  Python đếm HAI lần: hai dòng kích hoạt khác nhau cùng trỏ
                          tới một dòng số trần
```

→ **Không giá trị nào lọt.** C# gộp trùng theo `(vị trí, độ dài)` CỐ Ý: bản Python chỉ cần
ĐẾM để người đọc quyết, còn bản này phải CHE — che hai lần lên cùng một đoạn là làm hỏng
văn bản và làm lệch mọi vị trí sau nó. Con số khác nhau vì hai luật trả lời hai câu khác nhau.

⚠ **Một khác biệt thiết kế bắt buộc, không phải tuỳ chọn:** bản Python thay URL bằng
`" <URL> "` trước khi quét; bản C# thay bằng **khoảng trắng CÙNG ĐỘ DÀI**. Bản Python không
cần giữ offset vì nó không che; bản C# che đúng vị trí trong văn bản gốc, nên mọi phép thay
thế trước khi quét phải giữ nguyên độ dài. Đổi độ dài là lệch âm thầm — đã có test canh
(`URL_dai_o_truoc_khong_lam_lech_vi_tri_che`), và phép đột biến xác nhận nó biết đỏ.

⚠ **Chỗ tôi CHỌN một nhánh mà `AR-o` để mở**, ghi ra để dễ đổi: bắt được bí mật thì **thay
đúng giá trị và giữ phần còn lại của mẩu**, không bỏ cả mẩu. Theo đúng lập luận trong
`AR-o`: bỏ cả mẩu là mất bước kiểm, vì chính mẩu xin Ultraviewer là **biên lai** của một lần
chẩn đoán qua remote — thứ `docs/11` đo được là chỉ 4/10 case ghi lại. Đổi nhánh thì sửa
`EgressRedactor.Redact`, một chỗ.

**Cách chạy lại phép đối chiếu** (harness nằm ngoài repo, vì nó cần corpus):
```text
1  dotnet new console  ở một thư mục tạm, thêm <Reference> tới
   src/KnowledgePlatform.Domain/bin/Debug/net10.0/KnowledgePlatform.Domain.dll
2  đọc fixture-evidence.json, gọi SecretShapeScanner.Scan cho từng content,
   in ra (sourceReference, Shape, Value)
3  python -c "import check_corpus; check_corpus.quet_bi_mat(...)" để lấy phía Python
4  so hai danh sách theo (ref, shape, value)
```

🛑 **VÀ MỘT LỖ ĐÃ SUÝT LỌT, ĐÁNG GHI HƠN CẢ CỔNG: che từng mẩu rồi ghép KHÔNG BẰNG che
trên payload đã ghép.** Bản đầu của `SopPromptBuilder` che từng mẩu evidence rồi mới nối
lại. Test bắt được ngay: hình dạng 3 nhìn TỚI TRƯỚC 6 dòng, nên một từ khoá ở cuối mẩu này
và một dãy số trần ở đầu mẩu sau — **tách riêng thì cả hai vô hại** — ghép lại thành đúng
một cặp đăng nhập. Che từng mẩu đếm **0 chỗ**; quét lại toàn payload đếm **2 chỗ**.

```text
che từng mẩu rồi ghép   ->  0 chỗ che, payload MANG cặp ID + mật khẩu ra ngoài
che trên payload ghép   ->  2 chỗ che, sạch
```

→ **Mẫu sẽ tái diễn: một luật đúng trên từng phần KHÔNG tự động đúng trên phần ghép lại.**
Và đó đúng là chữ `AR-o` đã dùng — che trên **PAYLOAD** trước mỗi lần gọi — nên lần này
tài liệu đã đúng trước code. Đã dựng lại thiết kế cũ để xác nhận test biết đỏ: **3 test đỏ**.

⚠ Kèm một ràng buộc mới cho ai sửa khung payload: khung do `SopPromptBuilder` sinh ra
(`TICKET ES-…`, `--- …#comment-…`) an toàn trước cả bốn hình dạng vì nó không phải dòng
chỉ-có-số, không mang nhãn mật khẩu, không mang tên công cụ remote, không có key JSON. Đổi
khung thành một dòng chỉ có số là **tự che mất mã ticket của mình** — và mã ticket là thứ
bộ eval dùng để kiểm model không bịa nguồn.

⚠ **Điều phép đối chiếu này KHÔNG chứng minh:** rằng luật đủ. Nó chỉ chứng minh **bản port
không yếu hơn bản gốc**. Recall của chính luật gốc là **cận TRÊN** (13/13 trên corpus mà nó
được sửa theo — overfit theo định nghĩa), và corpus 12 tháng đã lộ hình dạng thứ NĂM (JWT)
mà luật bắt được nhờ may. Sửa luật thì phải đo lại, ở CẢ HAI bản.

## `IM-22` · `RlsGuard` báo XANH trong khi dữ liệu đang rò — đã đo, đã sửa

**Đây là lỗ nghiêm trọng nhất tìm được từ đầu dự án, và nó nằm bên trong chính cơ chế
được giao việc chống rò rỉ.** Ghi 2026-09-01, phát hiện qua phản biện có chủ đích vào
thiết kế `AR-d`, rồi đo lại bằng tay trên PostgreSQL 18.

### Tái hiện — chạy được, mất 10 giây

```sql
BEGIN;
CREATE TABLE kp.leak_probe (tenant uuid not null, val text not null);
INSERT INTO kp.leak_probe VALUES
  ('11111111-1111-1111-1111-111111111111','cua khach A'),
  ('22222222-2222-2222-2222-222222222222','cua khach B');
ALTER TABLE kp.leak_probe ENABLE ROW LEVEL SECURITY;
ALTER TABLE kp.leak_probe FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON kp.leak_probe
  USING (tenant = nullif(current_setting('app.current_tenant',true),'')::uuid);
SELECT set_config('app.current_tenant','11111111-1111-1111-1111-111111111111', false);

-- Day la thu ai do co the them, bang mot dong migration:
CREATE POLICY mo_toang ON kp.leak_probe USING (true);

SELECT string_agg(val,' + ') FROM kp.leak_probe;
ROLLBACK;
```

Kết quả thật:

```text
Khách A thấy                   | cua khach A + cua khach B      ← RÒ
Câu SQL của RlsGuard bản cũ    | XANH — "đã được bảo vệ"
Số policy trên bảng            | 2
```

### Vì sao

Policy trong PostgreSQL mặc định là **PERMISSIVE**, tức gộp bằng **OR**. Policy thứ hai
chỉ **nới ra**, không siết vào — nên ranh giới bằng policy LỎNG NHẤT, không phải chặt
nhất. Còn `RlsGuard` bản cũ chỉ hỏi *"bảng này có tồn tại policy nào không"*
(`EXISTS (SELECT 1 FROM pg_policy ...)`), **không hỏi policy đó nói gì**.

⚠ Điều đáng sợ không phải là lỗ. Là chỗ nó nằm: một cơ chế được dựng riêng để biến
"quên" thành "không start được", tự nó lại fail-open. Test xanh, guard xanh, dữ liệu rò.

### Đã sửa — guard giờ đối chiếu BIỂU THỨC, không đếm

Năm luật, mỗi luật một cách ranh giới có thể mất mà bản cũ mù:

```text
1  đúng MỘT policy                      policy thứ hai USING(true) = mở toang  ← đã đo
2  biểu thức USING khớp hằng số đã biết  bảng mới chép policy tiền-IM-9 → IM-9 tái phát
3  biểu thức WITH CHECK cũng khớp        thiếu nó thì ĐỌC bị chặn mà GHI thì không
4  relforcerowsecurity = true            bản cũ KHÔNG kiểm; thiếu FORCE thì chủ sở hữu
                                         bảng được miễn policy, mà app chạy bằng chủ sở hữu
5  không relation lạ nào trong schema     bản cũ lọc relkind='r' nên view và
                                         materialized view vô hình. MV không nhận RLS được.
```

Luật 5 chính là `AR-d` — chiều mà bản cũ không kiểm.

⚠ **Hằng số biểu thức bị lặp lại giữa `RlsGuard` và migration, và trùng lặp đó là cố ý.**
Suy hằng số ra từ migration thì đúng cái sai cần bắt — một migration viết sai — sẽ tự hợp
lệ hoá chính nó. Đổi biểu thức phải đổi ở hai chỗ, lệch một chỗ là không khởi động được.

## `IM-23` · Hai chỗ gọi `RlsGuard` hỏi hai câu khác nhau, nên dùng hai độ sâu

Làm guard chặt hơn suýt tạo ra một đường **mất dịch vụ**, và nó chỉ lộ ra khi có người
hỏi "chuyện gì xảy ra lúc deploy cuốn chiếu".

```text
StartupChecks      → IncludingUndeclaredRelations   "bản build này có khớp DB này không"
/health/ready      → DeclaredTablesOnly             "tôi phục vụ được không"
```

Ca vỡ nếu dùng chung độ sâu: bản N+1 chạy migration tạo bảng mới trong khi các tiến trình
bản N cũ vẫn đang phục vụ. Model bản cũ không biết bảng đó → coi là "relation lạ" → 503 →
**rút cả đội tiến trình đang khoẻ ra khỏi luồng**, vì một bảng chúng không hề đụng tới.
Mất dịch vụ do chính cơ chế an toàn gây ra.

Nới ở readiness KHÔNG mở lỗ: chiều "quên khai entity mới" vẫn bị chặn ở startup — nơi bản
build và schema được nhìn cùng một lúc, và cũng là nơi `G7` đòi chặn.

## `IM-19` · `machineReadability` do BÊN GỬI khai, hệ thống KHÔNG suy — và gõ sai là 400

Connector biết nó đang đẩy text hay đường dẫn ảnh; hệ thống nhìn vào một chuỗi thì
không biết. Tự gán `High` cho mọi thứ là text sẽ dán nhãn sai cho ảnh chưa OCR — đúng
trạng thái `KNOWLEDGE_EXISTS_NOT_RETRIEVABLE` ở §6.3 mà sản phẩm cần **nhìn thấy**.

Bỏ trống → `Unknown`, và đó là hợp lệ: *"chưa ai nói"* là một câu trả lời thật.
Gõ sai (`HIGHT`) → **400**, KHÔNG âm thầm về `Unknown`.

⚠ Vì sao chỗ này đáng một mục riêng: âm thầm về `Unknown` nghe như "chấp nhận rộng
rãi", nhưng nó biến một lỗi cấu hình connector thành **dữ liệu sai vĩnh viễn** — cả
kho evidence dán nhãn `Unknown` trong khi bên gửi tưởng đã khai `High`, và không ai
phát hiện cho tới lúc có người hỏi vì sao §6.3 không phân biệt được ba trạng thái.

## `IM-20` · Gửi lại cùng khoá với nội dung KHÁC thì KHÔNG ghi đè bản cũ

`K-B3`: evidence gắn với MỘT thời điểm và MỘT nguồn. Ghi đè lặng lẽ là **sửa lại quá
khứ** — và nó kéo theo mọi assertion đang dẫn chứng bằng mẩu đó, mà không cảnh báo ai.

Muốn nói *"nguồn đã đổi"* thì đó là một mẩu evidence MỚI với khoá mới. Response vẫn
trả `created: false` như mọi lần gửi lại khác, không phải lỗi.

⚠ Hệ quả chưa xử lý: hệ thống hiện KHÔNG phát hiện được việc nội dung ở nguồn đã đổi.
Đó chính là trigger thứ năm của `V3` mà `AR-b` đã ghim là chưa hiện thực được.

## `IM-21` · Hai chỗ kiểm rỗng đang lệch nhau — ĐÃ BIẾT, chưa xử lý

```text
/signals/case-observed   subject  →  chỉ kiểm null hoặc rỗng     → "   " ĐƯỢC NHẬN
/signals/case-evidence   content  →  IsNullOrWhiteSpace          → "   " bị từ chối
```

Chỗ mới chặt hơn có chủ đích: một `content` toàn dấu cách vẫn tạo ra một dòng rỗng
nghĩa, và Path A sẽ đưa **chính dòng đó** cho model như một quan sát thật. Rác trong
kho gom nguy hiểm hơn rác ở một tiêu đề.

Nhưng hai endpoint kiểm khác nhau là một chỗ dễ gây nhầm. Siết `subject` lại cho khớp
là đụng vào hợp đồng ĐANG CHẠY, nên **để người dùng quyết**, không tự sửa.

## `IM-18` · Luật domain có bộ test riêng, KHÔNG chạm hạ tầng

Trước quyết định này, **100% test của dự án cắm vào PostgreSQL**. Hệ quả không nằm ở
tốc độ mà ở chỗ khác: luật sinh ra từ 23 quyết định của Workstream 04 — `V1` thang
xác minh, `V3` trigger NEEDS_REVIEW, `S7` duyệt một hành động, `M2` giữ bản gốc —
**chưa từng được kiểm lấy một lần**. Chúng chỉ được đọc. Mà đó đúng là loại luật hỏng
mà không có gì hiện ra: không crash, không báo, chỉ lặng lẽ không gắn cờ.

→ `tests/KnowledgePlatform.Domain.Tests` chỉ tham chiếu `KnowledgePlatform.Domain`.
Không EF Core, không Npgsql, không host. Chạy được trên máy chưa cài PostgreSQL.

**Ràng buộc "không chạm hạ tầng" là một phần của quyết định, không phải tiện thể.**
Thêm một `ProjectReference` tới Infrastructure vào project đó sẽ làm nó hỏng đúng
theo cách khó thấy nhất: vẫn xanh trên máy có DB, đỏ trên máy chưa có.

Ba thứ bộ test này ghim mà test tích hợp không ghim được:

```text
1  Khoảng trống ĐÃ BIẾT      V3 có 5 trigger, slice này làm 3. Có một test ghim
                             KnowledgeRelation (L4) CHƯA tồn tại, và nó sẽ ĐỎ khi
                             L4 xuất hiện — bắt người sửa quay lại viết 2 trigger kia.
                             Cùng cách với stub IsSuperseded.
                             Comment thì bị bỏ qua; test thì được chạy và được đếm.
2  Hình dạng của kiểu        S7 nói hệ thống KHÔNG BAO GIỜ tự mở quyền xem. Câu đó
                             chỉ đúng chừng nào Lifecycle/VisibilityScope/LastApproval
                             không có setter public. Mở một cái ra là gỡ mất S7 mà
                             KHÔNG test hành vi nào đỏ — cánh cửa vừa mở, chưa ai đi qua.
3  Enum thiếu phân loại      Thêm một mức VerificationLevel mới mà quên nói nó trong
                             hay ngoài thang: `is ... or ...` lặng lẽ trả false, tức là
                             mặc định coi nó như chỗ tranh chấp. Test quét toàn enum.
```

⚠️ Chỗ phải làm khác đi vì domain tự lấy giờ: `Approve()` đóng dấu bằng
`DateTimeOffset.UtcNow` ở trong domain nên test không chọn hộ được mốc thời gian.
Test "duyệt lại thì tắt cờ" phải quay chờ đồng hồ thật đi qua mốc. Không chớp tắt,
nhưng nó là dấu hiệu: nếu sau này cần kiểm luật thời gian phức tạp hơn, chỗ sửa là
**tiêm đồng hồ vào domain**, không phải viết test khéo hơn.

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

~~Việc tiếp theo nên là **truy vấn "tìm N case cũ liên quan"**~~ — **SỬA 2026-08-30.**

Câu trên đúng rằng FTS là dependency của Path A, nhưng bỏ sót một mắt xích đứng
trước nó: **một `canonical_case` hôm nay là một dòng chữ.** Chỉ có subject + khoá
nguồn + hai mốc thời gian. Không comment, không cách xử lý, không kết quả.

`S8` đòi bản nháp gom mang theo một **phân bố** — *"bước kiểm room mapping: 14/20
case đã làm"*, *"gọi OTA trước khi check log: 6/20 làm, 8/20 làm ngược lại"*. Con số
`14/20` không suy ra được từ 20 cái tiêu đề. Và `06` §5 đã ghi ý định rõ ràng:
*"1M context → Path A: 20 case **+ evidence** trong MỘT request"*.

Nếu build FTS trước: tìm được 20 case → mỗi case một dòng tiêu đề → đưa cho model →
model viết ra một SOP nghe hợp lý mà không dựa trên gì. Đó đúng thứ `G6`/`AP3`
sinh ra để chặn, và nó làm hỏng `M2` ngay tại nguồn (`M2` đo *số nháp được duyệt +
mức sửa diff(A,B)* — nháp bịa thì cả hai con số đều vô nghĩa).

**Thứ tự đúng:** nạp evidence (`AR-f`) → xuất case OTA thật kèm comment (§8.2) →
FTS tune trên corpus thật → `ISoạnNhápSOP`.

🛑 **SỬA THỨ TỰ LẦN HAI — 2026-09-08, chủ dự án chốt: `ISoạnNhápSOP` LÀM TRƯỚC, FTS lùi.**
Lập luận cũ ở trên **không bị xoá vì nó đúng ở thời điểm đó**: khi ấy `canonical_case`
chỉ là một dòng tiêu đề, tìm được cũng không gom được gì. Ba điều đã đổi:

  1  **Evidence đã nạp xong** (`AR-f`, 2026-08-30) và `taxonomy-19-nhom-hoa-don.json` map
     sẵn 19 nhóm → danh sách mã case. Tức đầu vào của `ISoạnNhápSOP` **có sẵn mà
     không cần retrieval** — đưa đúng 10 case của một nhóm, như đã làm TAY ở `11`.
  2  **Phép thủ đã đo FTS 34% so với đoán mù 31%** trên nguồn này (`docs/09` §5).
     Build FTS trước là build đúng phần yếu nhất trước.
  3  **`M2` đóng được vòng ngay.** Hai cây ở `11` là bản A do NGƯỜI viết; `diff(A,B)`
     vì thế đang đo **người viết tài liệu**, không đo sản phẩm. Chỉ khi máy sinh bản
     A thì `M2` mới đo đúng thứ nó định đo.

⚠ Retrieval **vẫn cần**, nhưng cho **case MỚI ĐẾN**, không cho slice đầu. Đừng đọc mục
  này thành "bỏ FTS" — `AR4` vẫn đúng và vẫn là điều kiện chạy lại được.
✅ Và bộ eval **đã có sẵn**: `nhom_sop.py --kiem-cay` (11 phép đột biến, 11/11 bắt được).
  Máy sinh ra cây thì phải qua đúng phép kiểm đó, cộng MỘT phép mới cần thêm: **mã case
  model viết ra phải TỒN TẠI trong nhóm** — `G6`/`AP3` nói model không được bịa nguồn.

✅ **Bước một đã xong cùng ngày** — `POST /signals/case-evidence` chạy được, 16 test,
và đã gọi thật vào app đang chạy (newman: 23 request, 59 assertion, 0 đỏ). Bước kế
tiếp giờ là **việc của người dùng**: xuất case OTA thật KÈM COMMENT.

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

AR-c   RLS chưa được kiểm trên database thật. Việc đầu tiên khi có Postgres.

AR-d   ⚠️ TRÙNG SỐ HIỆU — ✅ ĐÃ SỬA 2026-09-03, người dùng chọn. `AR-d` từng chỉ
       HAI câu hỏi khác nhau: mục ngay dưới đây (RlsGuard) và mục "Chuỗi kết nối
       và mật khẩu DB" ở phía sau. Người dùng chốt: RlsGuard GIỮ chữ `d` (sinh
       trước 2026-08-25, được nhắc nhiều nhất trong 00/07); chuỗi kết nối đổi
       thành `AR-i`. Đã sửa pointer ở README.md §"Rủi ro đang mở" và
       appsettings.Development.json cùng ngày.

AR-d   RlsGuard KHÔNG bắt được entity QUÊN CÀI ITenantScoped.
       ✅ ĐÓNG 2026-09-03 — người dùng xác nhận sau khi xem tóm tắt hiện thực
       (5 luật của guard + chiều quét ngược + bằng chứng đột biến).
       Hiện thực 2026-09-01:
       Làm hướng A + C gộp (đúng gợi ý "hai lớp" của chính mục này): guard quét
       ngược pg_class trong schema kp, mọi relation phải được model khai là
       tenant-scoped HOẶC nằm trong AppDbContext.TenantExemptRelations kèm LÝ DO.
       Quét cả 'v' và 'm' — không chỉ 'r'. Hôm nay đúng một miễn trừ: kp.tenant.
       Đi kèm một phát hiện NẶNG HƠN câu hỏi gốc: xem IM-22.
       6 test mới, và 5 phép đột biến đã chứng minh cả 5 luật đều biết ĐỎ.
       Ghi 2026-08-25, phát hiện khi đọc lại code. CHƯA CHỌN HƯỚNG, chưa code.

       Nó kiểm MỘT CHIỀU:
         có kiểm    "mọi bảng ĐÃ KHAI tenant-scoped, có RLS chưa?"
         KHÔNG kiểm "mọi bảng TỒN TẠI, đã khai gì chưa?"

       Chiều thứ hai mới là chiều chặn được "quên". Chuỗi khi quên interface:
         entity mới rơi khỏi TenantScopedTables  (suy từ ITenantScoped)
         → RlsGuard bỏ qua, app start bình thường
         → danh sách bảng bật RLS trong migration là chuỗi VIẾT TAY, không có nó
         → 5 dòng HasQueryFilter cũng VIẾT TAY, không có nó
         → mọi tenant đọc được cả bảng, KHÔNG có cảnh báo nào

       Tức cơ chế hiện tại là default ALLOW: không khai thì được bỏ qua.
       G7 nói tenant boundary là NỀN TẢNG — nền tảng phải là default DENY.

       Ba kiểu quên khác đều đã được chặn, chỉ kiểu này thì không:
         cài interface, quên RLS trong migration   → RlsGuard ném lúc start
         cài interface, quên HasQueryFilter        → RLS ở DB vẫn chặn
         xoá interface khỏi TẤT CẢ entity          → RlsGuard ném (Count == 0)
         QUÊN INTERFACE Ở MỘT ENTITY MỚI           → không ai bắt  ← lỗ
       Kiểu không được canh lại đúng là kiểu dễ xảy ra nhất — thêm một entity
       là thao tác thường ngày.

       Ba hướng đã cân nhắc:
         A  default deny ở tầng model — mọi entity phải khai là tenant-scoped
            hoặc được miễn trừ TƯỜNG MINH (hiện chỉ Tenant cần miễn).
         B  hẹp hơn — chỉ bắt entity có cột TenantId mà thiếu interface.
            Không bắt được entity quên cả cột lẫn interface.
         C  mở rộng RlsGuard quét ngược từ pg_class: bảng nào trong schema kp
            không nằm trong danh sách mong đợi và không được miễn → ném.
            Mạnh nhất, bắt được cả bảng tạo bằng SQL thô.

       ⚠ A và B chỉ đọc Model.GetEntityTypes() → CHẠY ĐƯỢC KHÔNG CẦN POSTGRES.
       C cần DB sống, nên xếp sau AR-c. A và C không loại trừ nhau — đúng
       nguyên tắc hai lớp mà AppDbContext đã áp cho query.

       → Đây là phần DUY NHẤT của nhóm bảo mật tenant làm được khi chưa có
         Postgres. Và nó sẽ là test đầu tiên của dự án (hiện chưa có test nào).
AR-c   ĐÓNG 2026-08-24. RLS đã kiểm trên PostgreSQL 18.6 thật, bằng code C#
       thật, với role KHÔNG phải superuser. 9/9 test xanh, và đã chứng minh bộ
       test biết ĐỎ (gỡ FORCE → 5 đỏ; gỡ nullif → 3 đỏ). Sinh ra IM-9 và IM-10.

AR-i   Chuỗi kết nối và mật khẩu DB lấy từ đâu ở deploy thật?
       (số hiệu cũ: `AR-d` thứ hai — đánh lại 2026-09-03 vì trùng, xem ghi chú trên)
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

AR-f   Evidence vào hệ thống bằng đường nào?
       ✅ CHỐT 2026-08-30 bởi người dùng. Đã code, 16 test, đã gọi thật.
       → ENDPOINT RIÊNG `POST /signals/case-evidence`, `caseSourceReference` NHẬN NULL.
         Lý do chọn: K-B9 nói evidence được phép không thuộc case nào. Lồng vào tín
         hiệu case thì loại đó vĩnh viễn không có đường vào — rồi cũng phải mở cửa
         thứ hai, và LÚC ĐÓ mới đúng cái bẫy IM-12 (hai đường code cùng tạo ra
         evidence_item, đường ít chạy hơn mục dần). Một cửa duy nhất tránh được.
       → Case được nhắc mà không tồn tại: TỪ CHỐI CẢ LÔ. Không tự tạo case rỗng,
         không nhận rồi để link NULL. Bên gửi không phải "nhớ đã gửi gì" vì
         /signals/case-observed idempotent — cứ gửi case trước mỗi lần.
       → Idempotent theo (TenantId, SourceReference), giống hệt canonical_case.
       → Một evidence gắn ĐÚNG MỘT case (v0.2 §9 nói NHIỀU). Rút gọn có chủ đích,
         ghi thẳng vào EvidenceItem.cs để không ai đọc vào tưởng là quên.
       Sinh IM-19, IM-20, IM-21.

       Câu hỏi phụ CÒN LẠI, chưa quyết:
         · K-B9 mới mở được nửa đường: evidence KHÔNG thuộc case nào đã nạp được,
           nhưng đường Evidence → Knowledge trực tiếp thì chưa có (chưa có
           KnowledgeRecord nào để trỏ tới). Mở nốt khi Path A sinh ra record đầu.

AR-h   Full-text search: bốn ràng buộc ĐÃ ĐO, phải tuân theo khi build.   ← MỚI
       Ghi 2026-09-01. Đo trực tiếp trên PostgreSQL 18 của máy này, role kp_app.
       Ghi ở đây vì mất chúng là phải đo lại, và ba trong bốn cái đi ngược trực giác.

       1) RLS GIẾT INDEX GIN. Toán tử @@ (ts_match_vq) KHÔNG leakproof, nên Postgres
          không được phép chạy nó trước điều kiện RLS -> nó không bao giờ thành
          index condition. Đo được:
             không RLS  -> Bitmap Index Scan on ..._gin   Index Cond: tsv @@ ...
             có RLS     -> Bitmap Index Scan on ..._tenant
                           Filter: tsv @@ ...              <-- tụt xuống filter
          Đã thử hai đường cứu, CẢ HAI THẤT BẠI: GIN gộp (tenant, tsv) chỉ dùng được
          phần tenant; và kể cả tự viết WHERE tenant = '...' tường minh thì @@ vẫn là
          Filter.
          => Cột tsvector LƯU SẴN là thứ chịu lực. Index GIN là đồ thừa chừng nào RLS
             còn bật, mà RLS thì không tắt được (G7). ĐỪNG tạo nó cho giống người ta.

       2) MỘT DẤU GẠCH NGANG ĐẢO NGƯỢC TRUY VẤN.
             websearch_to_tsquery('simple','khong or -ve or room')
                -> 'khong' | !'ve' | 'room'
             to_tsvector('simple','hoan toan khac biet') @@ <cái đó>  -> TRUE
          Bất kỳ chủ đề nào chứa một token bắt đầu bằng '-' (dán từ Jira, một gạch
          thừa) trả về gần như TOÀN BỘ kho của khách đó, không lỗi, không log.

       3) websearch_to_tsquery CÓ NÉM. Chuỗi dài -> "stack depth limit exceeded".
          Nên đầu vào topic không chặn độ dài là một lỗi 500.

       4) ts_rank_cd KHÔNG CÓ TRẦN theo số lần lặp:
             comment lặp "pms" 10 lần, trọng số B  -> 4
             subject đúng chủ đề,      trọng số A  -> 1
          Một comment dài đè bẹp tiêu đề 4:1 — ngược hẳn thứ S8 cần.

       Ba cái sau đều là THẤT BẠI IM LẶNG: không crash, chỉ trả kết quả sai. Và cả ba
       sẽ làm hỏng đúng con số "14/20 case đã làm bước này" mà S8 nói là toàn bộ giá
       trị của bản nháp gom.

       ⚠ ĐO LẠI 2026-09-04 TRÊN CORPUS JIRA THẬT (32 case / 128 evidence, project ES).
         Ba cái được xác nhận, và HAI CÁI TỆ HƠN con số ghi ở trên. Thêm một cái MỚI.
         Tái hiện: scripts/jira-export + psql, xem nhật ký phiên 2026-09-04.

         (2') DẤU GẠCH NGANG — ĐO ĐƯỢC LÀ NGƯỢC CHIỀU với ghi chép trên.
              Trên ghi "trả về gần như TOÀN BỘ kho". Thực tế với websearch_to_tsquery
              (mặc định AND, không phải OR) thì nó trả về SỐ KHÔNG. Tiêu đề THẬT:
                 '[16776 -Villa 22 - Dalat] Kiểm tra ngày hóa đơn'
                 -> '16776' & !'villa' & '22' & !'dalat' & 'kiểm' & 'tra' & ...
                 -> khớp 0/5 dòng, KỂ CẢ CHÍNH NÓ.
              Người dùng copy tiêu đề một case rồi dán vào ô tìm là KHÔNG tìm ra
              chính case đó. Cả hai chiều đều là thất bại im lặng; chiều nào xảy ra
              phụ thuộc hàm tsquery nào được gọi. 1/32 tiêu đề của corpus dính.

         (4') ts_rank_cd: đo bằng dump THẬT 5764 ký tự thì tỉ lệ là 29:1, không phải 4:1.
                 case lệch chủ đề + dump  -> 28.9429
                 case đúng chủ đề, ngắn   ->  1.0000
              Trong corpus: 16/128 mẩu là dump JSON/XML, nhưng chúng chiếm 32% TỔNG SỐ
              KÝ TỰ. Nghĩa là 1/8 số mẩu nắm 1/3 trọng lượng xếp hạng.

         (5)  MỚI — TIẾNG VIỆT KHÔNG DẤU KHÔNG KHỚP GÌ. Không có trong bản trước.
                 to_tsvector('simple','Danh sách booking...') @@ 'danh sach booking' -> f
                 to_tsvector('simple','Danh sách booking...') @@ 'danh sách booking' -> t
              'simple' không có stemmer tiếng Việt nên 'phòng' và 'phong' là HAI token.
              30/32 tiêu đề của corpus có dấu, mà người Việt tìm kiếm gõ không dấu là
              chuyện thường. Extension `unaccent` CÓ SẴN trên máy này (đã kiểm
              pg_available_extensions) — nhưng nó là quyết định thiết kế, không phải
              thao tác: unaccent làm mất khả năng phân biệt các từ chỉ khác nhau ở dấu.
              Cùng lý do đó, JQL lọc theo tiêu đề cũng phải viết CẢ HAI kiểu bỏ dấu:
              tìm "khóa từ" ra 5 tiêu đề, "khoá từ" ra 1, hai tập KHÔNG giao nhau.

AR-g   Đọc evidence ra bằng đường nào?                                    ← MỚI
       Ghi 2026-08-30. CHƯA CHỌN HƯỚNG, chưa code.
       Hiện có đường GHI mà không có đường ĐỌC: không endpoint nào trả về evidence
       của một case. Bộ test phải mở thẳng AppDbContext để kiểm (ApiDatabaseFixture
       .OpenContext) — chấp nhận được cho test, KHÔNG chấp nhận được cho sản phẩm.
       Chưa chặn gì: ô kế tiếp (FTS + soạn nháp) chạy trong tiến trình, đọc thẳng
       từ DbContext. Sẽ chặn khi có bề mặt cần xem lại nguồn của một bản nháp — mà
       đó chính là thứ S8 nói người duyệt cần nhất.

       evidence_item có schema đầy đủ, có RLS, có index — và KHÔNG có đường ghi.
       Cả codebase chỉ một dòng chạm tới nó: khai báo DbSet ở AppDbContext.
       Đây là chỗ chặn Path A, không phải FTS. Xem §4.

       Hai hình dạng API đang cân nhắc:

         (1) LỒNG trong tín hiệu case — thêm mảng evidence[] vào CaseObservedSignal
             + case và nội dung tới cùng lúc, một lần gọi là xong
             + không có trạng thái "case tồn tại mà rỗng nội dung"
             − không bổ sung evidence cho case ĐÃ nạp được
             − body phình to; trần MaxSignalsPerRequest=500 phải tính lại

         (2) ENDPOINT RIÊNG — POST /signals/case-evidence, trỏ case qua sourceReference
             + bổ sung được cho case cũ; comment mới ở nguồn đẩy sang được
             + hai đường độc lập, mỗi đường một trần riêng
             − bên gửi phải gọi hai lần và tự lo thứ tự
             − sinh trạng thái trung gian: case có mặt nhưng chưa có nội dung
             − IM-12 cảnh báo: hai endpoint làm việc gần giống nhau thì đường ít
               chạy hơn sẽ mục dần. Cần cân nhắc chỗ này.

       Câu hỏi phụ chưa quyết, cả hai hướng đều phải trả lời:
         · idempotency của evidence — dùng lại (TenantId, SourceReference) như case,
           hay evidence được phép trùng?
         · K-B9 nói evidence trỏ THẲNG vào Knowledge được, không qua Case. Slice này
           có mở đường đó luôn không, hay chỉ làm ObservedInCaseId trước?
         · MachineReadability do bên gửi khai hay hệ thống suy? AP3 nói provenance
           không được đoán — nhưng đây là metadata, không phải origin.
         ✅ CÂU NÀY ĐÃ CÓ CÂU TRẢ LỜI TỪ DỮ LIỆU THẬT 2026-09-04, xem `AR-k`.

AR-j   Dữ liệu vận hành thật mang theo BÍ MẬT SỐNG. Che bằng luật nào?      ← MỚI
       Ghi 2026-09-04 sau khi quét corpus Jira thật đầu tiên. CHƯA CHỌN HƯỚNG.

       ✅ QUYẾT ĐỊNH 1 CHỐT 2026-09-04 bởi người dùng: **lô 32 case này CHỈ vào
          `kp_dev` làm fixture, KHÔNG vào kho tri thức thật.**
          Lý do chọn không phải vì rủi ro mà vì lợi ích bằng không: bản nháp SOP tốt
          nhất gom được từ lô này là 4 bước, MỖI BƯỚC n=1, và 3/4 bước lấy từ case
          chưa đóng. Người duyệt đọc thẳng 6 ticket mất 5 phút còn được nhiều hơn.
          Đổi lại là 6 bộ credential, dữ liệu bên thứ ba, giá hợp đồng của một khách
          hàng, một cặp trùng byte và một mẩu đã bị người gửi rút lại.
          Ngược lại, làm fixture thì nó có giá trị THẬT và đã chứng minh: nó bắt được
          `AR-k` ngay lần đọc đầu, và nó có đủ mọi hình thù xấu để test luật che, luật
          resolve tenant, luật khử trùng — thứ mà corpus 12 tháng sạch hơn không dạy được.
       ⚠ Việc xoay mật khẩu VNPT KHÔNG phụ thuộc quyết định này và phải làm NGAY.
       ⚠ Đây KHÔNG phải câu hỏi lý thuyết — đã đo trên 128 mẩu evidence có thật.

       Tìm được 6 bộ thông tin đăng nhập CÒN SỐNG của 6 khách sạn khác nhau:
         · 1 bộ VNPT hoá đơn điện tử (ES-346481#comment-802977) — nguy hiểm nhất.
           Nhà cung cấp trả "OK" HAI LẦN trong chính mẩu đó, tức đã xác thực thành
           công trên production. Ai cầm được thì phát hành hoá đơn VAT thật mang MST
           khách sạn, đúng ký hiệu và serial. Hậu quả PHÁP LÝ, không phải sự cố IT.
         · 5 bộ ID+mật khẩu Ultraviewer/Teamviewer của 5 khách sạn. Mật khẩu có thể
           đã xoay, nhưng ID GẮN CỨNG THEO MÁY và không bao giờ đổi.

       ⚠ PHÁT HIỆN QUAN TRỌNG NHẤT, và nó phủ định cách làm hiển nhiên:
         LỌC THEO TỪ KHOÁ CÙNG DÒNG KHÔNG DÙNG ĐƯỢC. Đo thật trên corpus:
         với luật `ultraview|teamview|anydesk|mật khẩu|password|pass|pw|acpass`,
         tỉ lệ bắt được các dòng chứa dãy số dạng ID là **2/17 = 11%**.
         Ca tệ nhất (ES-346406): khách gõ một dãy 9 số rồi một dãy 5 số ở HAI tin nhắn
         liên tiếp, còn chữ "Ultraview" nằm ở lượt nói TRƯỚC ĐÓ của nhân viên. Không
         từ khoá nào trong phạm vi dòng. Bộ lọc mù, nhưng model đọc thì thừa ngữ cảnh
         để hiểu đúng đó là ID/PW — mù đúng chiều xấu nhất.
         → Luật phải neo theo NGỮ CẢNH (cửa sổ N dòng quanh từ khoá) + hình dạng,
           không neo theo dòng. Và KHÔNG được chỉ dùng hình dạng số: sẽ ăn nhầm
           `80771` (số đặt phòng) và `0304746657` (mã số thuế) — đều là dữ liệu cần.

       Ba câu chưa quyết:
         · Che ở TẦNG NÀO — connector (script export), tầng nạp (API), hay tầng đọc?
           Che ở connector là rẻ nhất nhưng mỗi connector mới phải làm lại.
         · Che rồi có GIỮ DẤU không? G6/AP3 nói phải: người duyệt cần phân biệt
           "đã che" với "export lỗi". Xoá lặng lẽ là tạo ra một loại nghi ngờ mới.
         · Bí mật đã lọt vào Jira TỪ TRƯỚC thì che corpus không gỡ được. Sản phẩm có
           trách nhiệm BÁO ĐỘNG khi phát hiện credential trong nguồn không? Đó là một
           capability chưa ai chốt, và nó rất gần cột phải của G11 — cẩn thận.

AR-l   Ranh giới KHÁCH SẠN A ↔ KHÁCH SẠN B có phải ranh giới bảo mật không? ← MỚI
       🛑 **ĐỌC CUỐI MỤC NÀY TRƯỚC KHI LÀM THEO BA DÒNG NGAY DƯỚI.** Nửa *"thêm
          sub-tenant NGAY BÂY GIỜ"* đã **LÙI 2026-09-07** — không phải vì đổi ý về ranh
          giới, mà vì đo xong thì KHÔNG CÓ NGUỒN NÀO để điền trường đó. Quyết định hiện
          hành nằm ở cuối mục. Nửa *"CÓ, đây LÀ ranh giới bảo mật"* thì VẪN ĐỨNG.
       ✅ CHỐT 2026-09-04 bởi người dùng: **CÓ. Thêm sub-tenant vào `evidence_item`
          NGAY BÂY GIỜ**, lúc bảng còn rỗng. Để sau là sửa ngược với dữ liệu đã có.

       Vì sao câu này sinh ra: hôm nay `tenant` = ezCloud, nên 32 khách sạn nằm chung
       MỘT kho. Corpus thật cho thấy giả định "một case = một khách sạn" đã vỡ ngay
       trong 32 case đầu tiên:
         · ES-346594 là việc NỘI BỘ ezCloud, không thuộc khách sạn nào, và đính kèm
           `hotel.xlsx` liệt kê NHIỀU khách sạn
         · ES-346615 là một CHUỖI nhiều cơ sở
         · ES-346764 là SÂN GOLF (ezGolf), không phải khách sạn
         · ES-346481 chứa dữ liệu của KHÁCH CỦA khách sạn — một đơn vị sự nghiệp nhà
           nước có tên, MST, địa chỉ và số tiền giao dịch. Ranh giới hai cấp
           (ezCloud → khách sạn) không mô tả được trường hợp này.

       ⚠ ĐIỂM DỄ HIỂU NHẦM NHẤT, ghi để không ai tưởng RLS đã lo:
         RLS KHÔNG cứu được kiểu rò này. Rò xảy ra ở khâu **XUẤT BẢN SOP**, không ở
         khâu truy vấn hàng. Path A gom evidence của nhiều khách sạn (đúng quyền, RLS
         xanh) rồi sinh ra MỘT bản nháp; bản nháp đó được duyệt thành KnowledgeRecord
         và hiện cho mọi người. Dữ liệu đi qua ranh giới bằng cửa chính, không phải
         bằng lỗ hổng. Đây là loại rò mà `G7` chưa từng phải đối mặt.

       ⚠ VÀ ĐÂY LÀ LÝ DO CÂU NÀY ĐẮT: 4/5 phát hiện bị BÁC BỎ trong vòng quét đều bị
         bác vì câu này chưa có đáp án — người phân tích tự chọn một cách hiểu rồi lập
         luận trên đó. Một câu hỏi chưa quyết làm hỏng cả những phép kiểm không liên
         quan tới nó.

       🛑 **ĐÃ ĐO 2026-09-04 — KHÔNG CÓ NGUỒN NÀO DÙNG ĐƯỢC. Quyết định 2 BỊ CHẶN.**
          Chạy `scripts/jira-export/discover_fields.py` trên Jira thật, rồi đo lại trên
          đúng 32 case của corpus (những case mà TIÊU ĐỀ đã có mã khách sạn rõ ràng):

            Mã khách sạn         [customfield_12710]  -> `-1.0` ở **32/32** case.
                                 Trường TỒN TẠI, phủ 100%, và là HẰNG SỐ. Kiểu number,
                                 giá trị -1 là sentinel "chưa xác định" — tức nó được
                                 thiết kế có ý định rồi KHÔNG BAO GIỜ ĐƯỢC ĐIỀN.
            Tên khách sạn/Resort [customfield_13326]  -> 25/32 rỗng; 7 case có giá trị
                                 thì giá trị là "ezCloud - Customer Support" — TÊN TEAM,
                                 không phải tên khách sạn. Trường đang bị dùng sai việc.
            ezMessageHotelID     [customfield_16115]  -> 25/32 rỗng, 7 case = "EZCLOUD".
            C247ExtentionID      [customfield_15620]  -> 21/32 rỗng, và giá trị
                                 (72706/72724/72729, mỗi số lặp 2-3 lần) là extension
                                 TỔNG ĐÀI CỦA NHÂN VIÊN, không phải mã khách sạn.
            Tên khách hàng       [customfield_12724]  -> tên NGƯỜI ("chị Thủy"), 13/32
                                 rỗng hoặc "None".

       ⚠ MẪU LỖI LẶP LẠI HAI LẦN TRONG MỘT NGÀY, và đây là điều đáng mang đi:
         `machineReadability` = High ở 128/128 (`AR-k`) và `Mã khách sạn` = -1.0 ở 32/32
         là **cùng một hình dạng lỗi**: một trường phủ 100% với MỘT giá trị duy nhất.
         Nó tệ hơn trường rỗng, vì trường rỗng thì ai cũng thấy là thiếu, còn trường
         phủ-100%-một-giá-trị thì trông như đã có dữ liệu. Mọi phép kiểm "trường này có
         được điền không?" đều trả lời CÓ. Chỉ phép kiểm "trường này có mấy giá trị khác
         nhau?" mới thấy.
         → Luật chung nên áp cho MỌI trường dùng làm ranh giới hay bộ lọc:
           **đếm số giá trị PHÂN BIỆT, không đếm độ phủ.** Một giá trị = coi như rỗng.

       ⚠ Và ĐỪNG rơi vào nhánh trông hợp lý nhất: 8/32 case có mã trong tiêu đề
         (`[17468 - ...]`, `[12027 - ...]`, `18182 - ...`). Đó là 25%. Sub-tenant là
         ranh giới BẢO MẬT nên thiếu thì phải CHẶN NẠP — dùng tiêu đề nghĩa là chặn 75%
         corpus, và 25% còn lại vẫn sai vì tên trong tiêu đề lệch với thân bài (ES-346622
         "Mariha" vs `mirahhotel.sales@`; ES-346618 "Mirah Hotel" vs "Thành Danh Hotel").

       Hai lựa chọn còn lại, và cả hai đều là quyết định NGOÀI CODE:
         (a) ezCloud điền `customfield_12710` cho thật — việc của bộ phận support, không
             phải việc của repo này. Điền dần thì corpus cũ vẫn không có.
         (b) LÙI quyết định 2: nạp evidence KHÔNG có sub-tenant, và ghi thẳng vào tài
             liệu rằng ranh giới khách sạn CHƯA được thực thi — để sáu tháng nữa không
             ai tưởng nó đã có. Đây là nhánh trung thực nếu (a) không xảy ra sớm.

       ✅ **CHỐT 2026-09-07 bởi người dùng — NHÁNH (b): LÙI, VÀ GHI RÕ LÀ CHƯA THỰC THI.**
          Nạp evidence KHÔNG có sub-tenant. `evidence_item` KHÔNG thêm cột lúc này.
          Lý do chọn: nhánh (a) nằm ngoài repo và ngoài tầm kiểm soát của dự án, mà corpus
          ĐÃ xuất thì (a) không chữa được. Và thêm một cột luôn rỗng thì tự tạo ra đúng
          hình dạng lỗi mà chính `AR-k`/`AR-l` vừa dạy: một trường TRÔNG NHƯ đã có dữ liệu.
          → Hệ quả PHẢI ghi ra chỗ người ta đọc, không chỉ ở đây (`G6`/`AP3`):
            **ranh giới khách sạn A ↔ B hiện KHÔNG được thực thi ở BẤT KỲ tầng nào.**
            Đã ghi ở `README.md` §"Rủi ro đang mở" và `00_CURRENT_STATE.md` cùng ngày.
          → Điều KHÔNG đổi vì đã đo rồi: đây vẫn LÀ ranh giới bảo mật, và **RLS không cứu
            được kiểu rò này** — rò xảy ra ở khâu XUẤT BẢN SOP, không ở khâu truy vấn hàng.
            Nên câu này phải MỞ LẠI trước lúc Path A có luồng duyệt thật, chứ không phải
            trước lúc Path A gom evidence: gom thì RLS còn đủ, xuất bản thì không.
          ⚠ Điều kiện mở lại — viết dạng CHẠY ĐƯỢC, không phải một ngày trong lịch:
            khi có MỘT nguồn cho ra **≥2 giá trị PHÂN BIỆT** cho sub-tenant trên corpus
            (đúng luật "đếm số giá trị phân biệt, không đếm độ phủ"). `discover_fields.py`
            trả lời được câu đó trong một lần chạy.
       ⚠ Dù chọn nhánh nào, phần đã đo vẫn đứng: RLS không cứu được kiểu rò này, vì rò
         xảy ra ở khâu XUẤT BẢN SOP chứ không ở khâu truy vấn hàng.

       Còn phải quyết khi hiện thực (KHÔNG tự quyết):
         · sub-tenant lấy từ đâu? KHÔNG được suy từ tiêu đề — đã đo: ES-346622 tiêu đề
           "Mariha" mà email trong thân bài là `mirahhotel.sales@`; ES-346618/619 tiêu
           đề "Mirah Hotel" mà thân bài ghi "Thành Danh Hotel". Suy từ chữ là gán nhầm
           khách sạn ngay từ mẩu đầu. Phải lấy từ trường CÓ KIỂM SOÁT của Jira; thiếu
           thì để trống và CHẶN NẠP, không đoán (`G6`/`AP3`).
         · evidence không thuộc khách sạn nào (việc nội bộ ezCloud) biểu diễn thế nào?
         · cấp thứ ba (khách CỦA khách sạn) có cần chỗ riêng, hay chỉ là dữ liệu phải che?

AR-o   Che bí mật ở khâu **GỬI RA**, không chỉ ở khâu NẠP.                  ← MỚI
       ✅ **CHỐT 2026-09-08 bởi chủ dự án:** đồng ý cho nội dung ticket đi ra API của
          Anthropic, **với điều kiện luật che chạy trên PAYLOAD trước MỖI lần gọi.**

       Vì sao câu này KHÁC `AR-j`: `AR-j` nói về che lúc **nạp vào kho của mình** — dữ
       liệu vẫn ở trong nhà. `AR-o` là lúc dữ liệu **rời khỏi máy**, sang hạ tầng của
       một bên thứ ba. Hai ranh giới khác nhau, và `check_corpus.py` đang đứng ở ranh
       giới THỨ NHẤT.
         · đã đo: **5,2% mẩu evidence** chứa thông tin đăng nhập còn sống của khách
         · cộng dữ liệu pháp nhân bên thứ ba và giá hợp đồng theo năm (`AR-n`)

       Yêu cầu thiết kế sinh ra từ quyết định này:
         · cổng che phải **FAIL CLOSED**: bắt được hình dạng bí mật thì **KHÔNG GỬI**,
           không phải gửi kèm cảnh báo. Che ở tầng đọc là tạo ra sự an tâm giả.
         · phải **giữ DẤU** chỗ đã che (`G6`/`AP3`): người duyệt cần phân biệt
           "đã che" với "không có gì".
         · luật che là một **cạn TRÊN** đã đo (92% recall trên chính corpus nó được sửa
           theo), nên cổng phải đếm và ghi lại số chỗ đã che mỗi lần gọi — không để
           nó chạy im lặng.
       ⚠ Còn phải quyết khi hiện thực: bắt được bí mật thì **bỏ cả mẩu** hay **thay
         giá trị giữ hình dạng**? Bỏ cả mẩu là mất bước kiểm (vì chính mẩu xin
         Ultraviewer là **biên lai** của một lần chẩn đoán qua remote).

AR-m   Nội dung ĐÃ BỊ NGƯỜI GỬI RÚT LẠI — chưa có chỗ nào đánh dấu.      ← MỚI
       Ghi 2026-09-05. CHƯA CHỌN HƯỚNG. ⚠ Phải quyết TRƯỚC khi chốt cách cắt transcript.

       Ca thật, đã đọc nguyên văn: `ES-346661#description` chứa một đoạn giải thích
       nguyên nhân overbooking đầy đủ, kèm hai mã booking OTA (Trip.com và Expedia).
       **Mười sáu phút sau**, cùng người gửi viết: *"dạ anh/chị bỏ qua giúp em tin nhắn
       trên ạ, em gửi nhầm nội dung"*. Và tiêu đề case là *"lỗi không sinh mã đặt phòng"*
       — không dính gì tới overbook. Đoạn kia là nội dung của một case KHÁC.

       ⚠ VÌ SAO HÔM NAY CHƯA HỎNG, VÀ VÌ SAO NGÀY MAI SẼ HỎNG:
         hôm nay cả đoạn sai lẫn câu đính chính nằm CHUNG một mẩu 1 916 ký tự, nên model
         đọc mẩu đó sẽ thấy câu đính chính và tự bỏ qua. Đó là **may**, không phải thiết kế.
         Ngày nào pipeline cắt transcript thành nhiều mẩu — việc rất có thể sẽ làm, vì
         `AR-h` đã đo rằng mẩu dài đè bẹp xếp hạng 29:1 — thì đoạn đã bị rút sẽ vào
         `evidence_item` **không mang dấu hiệu gì**, và Path A sẽ gom nó như một quan sát
         thật. Nguyên nhân overbooking của khách A sẽ xuất hiện trong SOP về mã đặt phòng.

       → Cần một cột ở TẦNG MẨU (đề xuất `RetractedAt` + `RetractedReason`), và cần
         quyết trước khi chốt cách chunk — vì sau khi đã cắt thì không ghép lại được.
       ⚠ Chỗ khó thật: ai đánh dấu? Người gửi rút lại bằng CÂU CHỮ ("bỏ qua giúp em"),
         không bằng một nút bấm. Suy ra từ câu chữ là đoán — trái `G6`/`AP3`. Nhưng
         không suy thì không có tín hiệu nào cả. Đây là câu hỏi domain, không phải code.

AR-n   Dữ liệu của BÊN THỨ BA và dữ liệu THƯƠNG MẠI trong evidence.        ← MỚI
       Ghi 2026-09-05. CHƯA CHỌN HƯỚNG. Hai loại, cùng một gốc: ranh giới hai cấp
       (ezCloud → khách sạn) không mô tả hết những gì thật sự nằm trong ticket.

       · BÊN THỨ BA: `ES-346481#comment-802977` chứa payload hoá đơn với khối `<NMua>` —
         tên một đơn vị sự nghiệp nhà nước, mã số thuế, địa chỉ đầy đủ, 8 dòng dịch vụ và
         tổng tiền. Đó không phải dữ liệu ezCloud, cũng không phải dữ liệu khách sạn — là
         dữ liệu **khách CỦA khách sạn**, một pháp nhân chưa từng đồng ý xuất hiện ở đâu.
         Che credential xong vẫn còn công bố một khoản chi lưu trú của một tổ chức có tên.
         → `AR-l` chốt ranh giới hai cấp; đây là **cấp thứ ba** mà nó chưa mô tả.

       · THƯƠNG MẠI: `ES-346608#comment-803265` ghi rõ giá hợp đồng theo năm của một khách
         sạn cụ thể, hạn hợp đồng, và ý định ngừng dùng để chuyển sang PMS tự xây. Giá và
         ý định rời bỏ của một khách hàng không nên hiện cho mọi nhân viên qua một bản SOP.
         → Đây KHÔNG phải bí mật kỹ thuật nên `AR-j` (luật che credential) không bắt được
           nó, và cũng không có hình dạng nào để bắt bằng regex. Cần một cơ chế khác.

AR-k   `machineReadability` đang là HẰNG SỐ. Trường vô nghĩa đang gác cổng. ← MỚI
       Ghi 2026-09-04. ⚠ ĐÂY LÀ BUG THẬT, KHÔNG PHẢI CÂU HỎI THIẾT KẾ.
       Đo: 128/128 mẩu đều mang nhãn `High`, KỂ CẢ mẩu 5 ký tự chỉ chứa "80771",
       mẩu 9 ký tự chỉ chứa chính mã case của nó, và mẩu 35 ký tự chỉ chứa tên file
       ảnh. Nguyên nhân: scripts/jira-export/export_jira_to_channel1.py gán CỨNG
       "High" cho mọi mẩu, với lý do ghi trong code là "connector biết nó đẩy text
       thuần nên khai High là khai thật".

       Lý do đó ĐÚNG nhưng trả lời nhầm câu hỏi. Nó trộn hai trục:
         trục 1  byte có giải mã được thành ký tự không?     → connector biết. Luôn có.
         trục 2  lấy được TRI THỨC ra không?                 → cái sản phẩm cần biết.
       `IM-19` chốt "bên gửi khai, hệ thống không suy" — vẫn đúng. Cái sai là chỉ có
       MỘT trường cho HAI câu hỏi, nên bên gửi khai thật mà kết quả vẫn vô dụng.

       Hậu quả cụ thể: ai viết `WHERE machineReadability = 'High'` rồi báo "đã lọc
       rồi" thì vừa lọc xong 100% dữ liệu. Thất bại im lặng, đúng loại `AP3` sinh ra
       để chặn — và lần này nó nằm trong chính đường nạp vừa build xong.

       Hướng đề xuất (CHƯA CHỐT, cần quyết ở tầng domain trước khi sửa code):
         giữ `machineReadability` cho trục 1, thêm một trường cho trục 2. Và thêm một
         chốt kiểm: sau mỗi lần export, nếu MỘT nhãn chiếm ≥95% thì coi như hỏng.
         Một trường phân loại mà không phân loại được gì thì tệ hơn không có trường.

R-K4   ✅ ĐÃ ĐẾM 2026-09-04. Kết quả đầy đủ: `docs/09_RK4_DEM_NGUYEN_NHAN.md`.
       🛑 **GIẢ ĐỊNH "5-10 NGUYÊN NHÂN" BỊ KHAI TỬ.** Đo trên 150 case hoá đơn đã đóng:
          cỡ **19 nhóm (khoảng 18-30) cho MỘT chủ đề**, và đó là CẬN DƯỚI vì chỉ đo được
          trên 59% corpus. Không phải 5-10, và không phải cho toàn nền tảng mà cho MỘT
          chủ đề — nhân với số chủ đề.

       ⚠ Nhưng phép đếm **thành công một nửa và thất bại một nửa**, phải nói cả hai:
         · THẤT BẠI: không cho ra "một con số". Cùng 88 case, ba tiêu chí gộp ra
           **6 / 19 / 66**, hai phản biện ra **18 / 78** — lệch 13 lần. Theo đúng luật
           đặt TRƯỚC khi chạy: con số phụ thuộc TIÊU CHÍ, không phụ thuộc dữ liệu.
         · THÀNH CÔNG: đủ để khai tử 5-10, đủ để đo trần chất lượng nguồn (41%), và đủ
           để chốt kiến trúc — vì mọi cách cắt đều nằm CÙNG MỘT PHÍA của quyết định.

       ⚠ Một thứ trong đó LÀ thuộc tính của dữ liệu chứ không của tiêu chí: chỉ ở mức
         "một SOP dùng được" (19) mới có **phân bố đầu-đuôi thật** — 6 nhóm lớn nhất phủ
         53% case, 10 nhóm phủ 75%. Lượt 6 phẳng đều (dấu hiệu của một phân hoạch được
         THIẾT KẾ, không phải phát hiện); lượt 66 gần như toàn singleton (đã đập vỡ cái
         đầu). Nên 19 là mức duy nhất đang ĐO DỮ LIỆU.

       ⚠️ **PHÉP ĐO TRÊN MỘT NGUỒN — KHÔNG PHẢI QUYẾT ĐỊNH KIẾN TRÚC.**
          🛑 **HẠ CẤP 2026-09-05 sau phản biện của người dùng.** Bản trước của mục này
             viết *"QUYẾT ĐỊNH KIẾN TRÚC: KHÔNG dựng vector DB"*. Đó là **vượt quá dữ
             liệu**, và nó vi phạm hai guardrail của chính dự án:

             · `G1` — Jira là **connector**, không phải product boundary. Bằng chứng
               đứng sau kết luận là MỘT khách, MỘT nguồn, MỘT chủ đề, 88 case có nhãn.
               Viết ra một quyết định áp cho *sản phẩm* từ đó là biến connector thành
               ranh giới sản phẩm.
             · `G12` — đặc điểm dữ liệu của một khách là **THAM SỐ**, không phải hằng số
               thiết kế. Kết luận cũ đóng nó thành hằng số.
             · Và `D1` nói khách không dùng Jira vẫn phải dùng được. Một deal trong CRM
               có **trường có kiểm soát** (stage, giá trị, ngành); một ticket Jira là
               văn xuôi tự do. AUC đo trên văn xuôi tự do KHÔNG nói gì về CRM.

          **Phát biểu đúng phạm vi:** *trên nguồn Jira của ezCloud, chủ đề hoá đơn, tín
          hiệu văn bản đo được ở mức **AUC 0,61** — quá yếu để retrieval theo văn bản là
          cơ chế chính. **CHƯA ĐO trên nguồn nào khác.***

          → Hệ quả về cách làm việc, và đây là phần mang đi được: `AR4` vốn viết đúng —
            *"Postgres FTS trước, pgvector khi ĐO ĐƯỢC là không đủ"*. Câu đó là một
            **điều kiện chạy lại được**, không phải một cánh cổng đóng một lần. Phiên
            này đã đọc nó thành "đã đo xong, kết thúc" — đó là chỗ trượt.
          → Nên `thu_retrieval.py` phải là **phép đo chuẩn chạy lúc onboard mỗi khách**,
            không phải một script chạy một lần. "Khách này cần vector hay không" là một
            **con số đo được**, đúng tinh thần `G12`.

          🛑 **PHÂN BIỆT ĐÃ LÀM MỜ TRONG PHIÊN NÀY — ghi để không lặp lại:**
            ```text
            dữ liệu thật để TÌM LỖI TRONG CODE     n=1 là ĐỦ
                 (bug 500 timezone · trường hằng số · luật che bắt 11% ·
                  lỗi lấy mẫu "N case gần nhất")
                 → phát biểu về SỰ TỒN TẠI. Một mẫu chứng minh được.

            dữ liệu thật để CHỐT KIẾN TRÚC          n=1 KHÔNG đủ
                 (có cần vector DB không · tập nguyên nhân lớn cỡ nào)
                 → phát biểu về PHÂN BỐ. Cần đại diện.
            ```
            Phiên này dùng CÙNG một corpus cho cả hai và trình bày với CÙNG độ chắc
            chắn. Đó là chỗ trượt thật.

          Lập luận gốc vẫn giữ nguyên dưới đây vì nó không sai — nó chỉ **hẹp hơn** cách
          nó được viết ra:

          ⚠ Lưu ý cách đi tới kết luận này: nó **BÁC chính luật quyết định của R-K4**
            chứ không điền số vào luật đó. Luật cũ nói "≤10 thì phân loại, >100 thì tìm
            kiếm"; đo được ~19-30 là ở giữa, nhưng bốn lý do dưới đây không cái nào dựa
            vào con số:
            1) Câu trả lời LIỆT KÊ ĐƯỢC, và corpus tự chứng minh: nhân viên đã gõ tay
               trọn một SOP có B1/B2 kèm nhánh điều kiện ngay trong chat (ES-346396).
               Cái gì con người liệt kê được tại chỗ thì liệt kê được vào BẢNG. RAG có
               giá trị khi KHÔNG liệt kê được — điều kiện đó sai hẳn ở đây.
            2) Đường vào là MÃ LỖI, không phải văn xuôi: ERR.1518, Status 5000,
               InvalidInvoiceDate, HOTEL_NOT_FOUND. Embedding không thêm gì trên mã lỗi.
            3) NẶNG NHẤT — ở case khó, thông tin phân biệt KHÔNG CÓ TRONG VĂN BẢN. Cùng
               triệu chứng "không chọn được ký hiệu hoá đơn" ứng với BA cơ chế, và thứ
               phân biệt là một PHÉP KIỂM ("danh sách ký hiệu có rỗng không?"), không
               phải từ nào trong lời khách báo. Không công nghệ tìm kiếm nào chữa được.
               Thứ cần là CÂY QUYẾT ĐỊNH CÓ BƯỚC KIỂM — đúng cái bản nháp SOP phải chứa.
            4) Nút cổ chai thật là 41% không ghi nguyên nhân. Tiền vào stack vector là
               tiền không vào việc duy nhất làm con số tiến lên.
          Rủi ro bất đối xứng có lợi: pgvector là thứ CỘNG THÊM vào cùng Postgres.

       ✅ **CHỐT 2026-09-05 bởi người dùng — ĐƠN VỊ ĐẾM: "một nguyên nhân" = "một SOP
          DÙNG ĐƯỢC" = mức 19 nhóm.** KHÔNG phải lớp kiến trúc (6), KHÔNG phải cách sửa
          cụ thể (66).
          Lý do chọn: đó là đơn vị mà sản phẩm THỰC SỰ SINH RA (bản nháp SOP), và là mức
          duy nhất trong ba mức cho ra phân phối có đầu-đuôi thật — tức nó đang đo DỮ
          LIỆU chứ không đo tiêu chí.
          → **Con số kế hoạch: 18-30 SOP cho MỘT chủ đề**, nhân với số chủ đề. Không
            phải 5-10 cho toàn nền tảng.
          → Và nó đóng luôn một tranh luận sẽ tái diễn: từ nay "bao nhiêu nguyên nhân"
            có một nghĩa duy nhất trong dự án này. Ghi ở đây vì `§6.9` đã cho thấy bệnh
            "từ vựng song song" tái phát 3 lần trong một workstream.

       ✅ **CHỐT 2026-09-07 bởi người dùng — Q2: CÓ. Mở phép đếm sang case CÒN MỞ, và
          bắt ghi nguyên nhân ngay tại thời điểm remote / điện thoại.**
          Lý do: đó là việc DUY NHẤT làm con số 41% *"không xác định được nguyên nhân"*
          tiến lên. Đọc thêm case ĐÃ ĐÓNG thì không, vì tỉ lệ đang XẤU ĐI ở cửa sổ mới
          nhất (50%) — thêm dữ liệu cùng loại chỉ làm con số tệ hơn, không làm nó tiến.
          → Hai việc quyết định này sinh ra, CHƯA làm:
            · JQL mới phải KHÔNG lọc `resolved` / status đã đóng. Mọi JQL hiện có trong
              `jira-config.example.bat` đều lọc case đã đóng — chính chỗ làm mẫu lệch.
            · "Bắt ghi nguyên nhân lúc remote" là thay đổi ở QUY TRÌNH CON NGƯỜI của
              support, không phải ở repo này. Repo chỉ đỡ được nửa sau: chỗ để ghi, và
              một phép kiểm NÓI RA khi chỗ đó trống.
          ⚠ Nó KHÔNG chữa được corpus đã xuất — mẫu cũ vẫn lệch về case đã đóng. Quyết
            định này chỉ làm mẫu SAU NÀY khác đi, nên đừng đếm lại trên corpus cũ rồi
            tưởng đã thấy hiệu quả.

       ✅ **PHÉP THỬ ĐÃ CHẠY 2026-09-05 — và nó cho thấy CHÍNH NGƯỠNG ĐÃ HỎI SAI CÂU.**
          `scripts/jira-export/thu_retrieval.py`. Ngưỡng đặt trước: <60% thì embedding
          đáng thử. Kết quả: **34%** — dưới ngưỡng, tức "embedding đáng thử".
          **Nhưng ba phép đo bổ sung cho thấy kết luận đó SAI**, và đây mới là phần đáng
          nhớ:

            1) ĐOÁN MÙ = 31%. Luôn trả 3 nhóm lớn nhất, không đọc truy vấn.
               FTS đo được 34%. **Chênh đúng 3 điểm phần trăm.** FTS gần như không thêm
               tín hiệu nào — nó chỉ đang tái tạo phân bố nhóm.

            2) TRẦN LÝ THUYẾT = 22%, THẤP HƠN CẢ TRUY VẤN NGẮN. Dùng CẢ transcript (đã
               chứa sẵn câu chẩn đoán) làm truy vấn lại TỆ HƠN dùng tin nhắn đầu. Điều
               này chỉ xảy ra nếu xếp hạng bị chi phối bởi ĐỘ DÀI tài liệu — đúng
               `AR-h` #4. Đã thử **cả 7 cờ chuẩn hoá** của `ts_rank` (chia log độ dài,
               chia độ dài, harmonic mean, chia số từ...) và cả cách gom điểm theo NHÓM
               trước khi cắt top-3: **mọi biến thể nằm trong 22-35%.** Không cấu hình
               nào vượt xa đoán mù.

            3) PHÉP KIỂM QUYẾT ĐỊNH — từ vựng có mang thông tin về nhóm không?
               Đo Jaccard trên 3 828 cặp case:
                    cặp CÙNG nhóm  trung bình 0,2206
                    cặp KHÁC nhóm  trung bình 0,1859
                    Cohen's d = 0,38  (|d|<0,2 = hầu như không phân biệt)
                    P(cặp cùng nhóm giống hơn cặp khác nhóm) = **60,9%**  (50% = mù hoàn toàn)
               Ví dụ sắc nhất: `ES-346647` *"ko xuất được hóa đơn"* (nhóm **Phân quyền**)
               giống **65%** với `ES-346303` *"CHỈNH LẠI ĐÚNG NGÀY HÓA ĐƠN"* (nhóm **Ngày
               làm việc/kiểm toán**). Cùng từ vựng, khác cơ chế hoàn toàn.

          ⚠️ **KẾT LUẬN, ĐÚNG PHẠM VI (sửa 2026-09-05): trên nguồn này, embedding cũng
             không cứu được — nhưng đó là phát biểu về NGUỒN NÀY, không về sản phẩm.**
             AUC 60,9% là **trần của MỌI phương pháp đọc văn bản của case NÀY**, không
             riêng FTS. Embedding đọc đúng cái văn bản đó; nó nắm được đồng nghĩa nên có
             thể nhích vài điểm, nhưng không thể tạo ra tín hiệu không có trong dữ liệu.
             ⚠ Nguồn khác có thể có AUC cao hơn hẳn, và lúc đó câu trả lời đổi. Đây là
               lý do phép đo phải chạy lại mỗi khách chứ không chốt một lần — xem đầu
               mục `R-K4` về `G1`/`G12`.
             → Ngưỡng "dưới 60% thì embedding đáng thử" **giả định ngầm rằng trần cao**.
               Giả định đó sai ở corpus này. Một phép thử tốt vẫn có thể hỏi sai câu, và
               cách phát hiện là đo thêm BASELINE và TRẦN — hai thứ mà ngưỡng gốc không có.

          → Điều này XÁC NHẬN bằng số đo cái mà kết luận `R-K4` §5 điểm 3 đã nói bằng lời:
            *"ở case khó, thông tin phân biệt KHÔNG CÓ TRONG VĂN BẢN"*. Thứ phân biệt là
            một **PHÉP KIỂM** (*"danh sách ký hiệu có rỗng không?"*), không phải từ nào
            trong lời khách báo. Thứ cần là **cây quyết định có bước kiểm** — đúng cái
            bản nháp SOP phải chứa — chứ không phải một chỉ mục tốt hơn.

          ⚠ ĐIỀU SẼ LÀM ĐỔI Ý (thay cho ngưỡng cũ đã bị bác):
            · nếu đo lại trên nhãn của một chủ đề KHÁC mà AUC > 75% thì tín hiệu văn bản
              có thật ở chủ đề đó, và câu hỏi mở lại cho riêng chủ đề đó
            · nếu bổ sung **kết quả của bước kiểm** vào truy vấn (không chỉ lời khách
              báo) mà top-3 vượt 70% thì đó là bằng chứng cho cây quyết định, KHÔNG phải
              cho embedding

       🛑 **ĐIỀU BẤT NGỜ NHẤT, và nó giải thích vì sao vòng n=32 thất bại:**
          **CASE JIRA KHÔNG PHẢI MỘT ĐƠN VỊ CỦA GÌ CẢ.** Trong cùng 150 case:
            · ES-332789 một mình chứa **≥6 cơ chế** — chính bộ phận hỗ trợ phải đóng nó
              lại vì "các issue đang trùng lặp dễ gây nhầm lẫn... sẽ TÁCH RIÊNG"
            · ES-337454 / ES-338386 / ES-340759 là **cùng một khách, cùng một việc**,
              đóng trong **52 giây**
          Một ticket chứa 6 nguyên nhân, ba ticket chứa 1 nguyên nhân — cùng bộ dữ liệu.
          Nên mọi phép đếm "nguyên nhân trên số case" đang chia cho một mẫu số là **ĐỘ
          SẠCH TICKET CỦA JIRA**, không phải cấu trúc của hệ thống.
          → **Tăng n KHÔNG sửa được một đơn vị đo sai.** Muốn con số ổn định thì phải
            đếm trên đơn vị khác: MỘT CƠ CHẾ = MỘT DÒNG, cho phép một case sinh nhiều
            dòng. Corpus đã chỉ rõ nhu cầu: 7 case ghi ≥2 cơ chế, 6 case cần 2 SOP, và
            ít nhất 6 họ cơ chế KHÔNG có nhóm nào trong cả ba lượt.

       ⚠ 41,3% (62/150) KHÔNG XÁC ĐỊNH ĐƯỢC NGUYÊN NHÂN — quan trọng hơn con số, và nó
         nói về NGUỒN chứ không về tập nguyên nhân. Ba xác nhận độc lập trong corpus:
         26/150 case tự ghi bước "remote vào máy"; chỉ 15/150 có bước "tra log"; và tỉ
         lệ đang **XẤU ĐI** (50% ở 30 case mới nhất vs 27% ở 30 case cũ nhất).
         → Đếm thêm case ĐÃ ĐÓNG cùng loại sẽ KHÔNG nâng độ tin.
         → Và 41% này lệch CÓ HỆ THỐNG: cơ chế nào viết được bằng một dòng thì còn lại,
           cơ chế nào phải điều tra mới biết thì mất. Hệ quả nặng: **bộ phân loại huấn
           luyện trên corpus này sẽ tự tin nhất ở đúng chỗ ít cần nhất.**
         ✅ Điểm sáng: kỷ luật giữ được — 84/88 case có nguyên nhân là "evidence nói rõ",
           chỉ 4 case (4,5%) là suy ra. Sai số KHÔNG đến từ việc bịa nguyên nhân.

       Ghi chú cũ, giữ lại vì lập luận vẫn đúng:
       "Một loại vấn đề có 5-10 nguyên nhân" — VẪN CHƯA ĐẾM ĐƯỢC (tính tới 2026-09-03).
       ✅ QUYẾT ĐỊNH 3 CHỐT 2026-09-04: **ĐẾM TRƯỚC khi chốt kiến trúc tìm kiếm.**
          Chạy JQL 12 tháng chủ đề hoá đơn (~140 case đã đóng, ước từ chính corpus
          này), đọc và đếm nguyên nhân bằng tay. Ước 1 ngày công.
       ⚠ Hệ quả về THỨ TỰ: bước (c) FTS lùi lại SAU phép đếm. Trước hôm nay FTS là
         việc kế tiếp; giờ không phải nữa. Đếm xong mới biết cần FTS hay cần phân loại.

       🛑 **ĐO 2026-09-04: CƠ SỞ CỦA PHÉP ĐẾM SAI 20 LẦN.** Ước lượng "~140 case hoá
          đơn đã đóng trong 12 tháng" được suy ra từ corpus 4 ngày (1,75 case/ngày ×
          tỉ lệ đóng 21,9%). Đếm thật bằng JQL trên Jira:

            2 723 case   chủ đề hoá đơn · 12 tháng · đã đóng · bỏ Duplicate/CannotRepro
           38 451 case   toàn project · 12 tháng · đã đóng
              896 case   chủ đề khoá từ · 12 tháng · đã đóng

          Sai 20 lần, và lý do là chỗ đáng nhớ: corpus 4 ngày lọc thêm
          `"Kỹ thuật phụ trách" is not EMPTY` — một tập con RẤT nhỏ (chỉ case đã được
          giao cho kỹ thuật). Suy tốc độ của cả project từ một tập con đã lọc là sai,
          và nó sai theo hướng làm mọi thứ trông nhỏ hơn thực tế.
          ⚠ Bài học chung: đừng ngoại suy quy mô từ một mẫu mà chính mình đã lọc.

          Hệ quả thực tế cho phép đếm: KHÔNG tải hết 2723 case (mỗi case ~2,5 request
          nên đó là ~7 000 request lên Jira production). §8.2 nói n=50-200 là đủ, nên
          lấy MẪU 150 case gần nhất — `MAX_ISSUES=150`, `ORDER BY resolved DESC`.
          ⚠ Mẫu đó THIÊN LỆCH VỀ THỜI GIAN (chỉ vài tháng gần nhất, không rải đều 12
          tháng). Ghi ra đây vì con số đếm được phải mang theo cái thiên lệch này —
          nếu tập nguyên nhân thay đổi theo phiên bản sản phẩm thì mẫu gần đây sẽ cho
          ÍT nguyên nhân hơn thực tế 12 tháng.

       🛑 **VÀ MỘT LỖI LẤY MẪU ĐÃ ĐO, ghi vì nó rất dễ lặp lại.** Lô đầu tiên xuất ra
          được gọi là "mẫu 12 tháng" — thực tế nó trải **24 NGÀY** (2026-08-11 →
          2026-09-03). Lý do đơn giản mà không ai nghĩ tới lúc đặt JQL:
          `ORDER BY resolved DESC` + `MAX_ISSUES=150` lấy 150 case **GẦN NHẤT**, và
          project có ~2 723 case hoá đơn đã đóng mỗi năm → 150 case chỉ ăn hết **6,6%**
          khoảng thời gian.

            "N case gần nhất" KHÔNG phải mẫu của N tháng. Nó là mẫu của một cửa sổ hẹp
            mà độ hẹp phụ thuộc LƯU LƯỢNG — và lưu lượng thì không ai kiểm khi viết JQL.

          Hệ quả trực tiếp cho phép đếm này: nếu tập nguyên nhân thay đổi theo phiên bản
          sản phẩm (gần như chắc chắn có — mỗi bản vá đóng lại một nhóm nguyên nhân và
          mở ra nhóm khác) thì mẫu 24 ngày cho con số **cận dưới rất xa**.

          ✅ Đã sửa: `scripts/jira-export/sample_spread.py` chia khoảng thành từng tháng
          và lấy đều 12 case mỗi tháng. Đo được ngay, và kết quả NGƯỢC DỰ ĐOÁN:

            mẫu                      case  evidence  mẩu dùng được/case  case rỗng
            24 ngày gần nhất          150       345                1,46   26 (17%)
            12 tháng rải đều          144       528                2,26   39 (27%)

          Case CŨ HƠN **giàu** nội dung hơn, không nghèo hơn. Nhưng cũng nhiều case rỗng
          hẳn hơn — nên phân bố **lệch hơn**, không phải tốt hơn đều. Cả hai con số đều
          quan trọng: trung bình cao hơn nói rằng có case đáng gom, còn 27% case rỗng nói
          rằng một phần tư corpus không dùng được dù đã lọc đúng chủ đề và đúng trạng thái.

       Vì sao không đếm được từ corpus 32 case: hai vòng phân tích đọc CÙNG dữ liệu ra
       hai kết luận NGƯỢC NHAU — một bên đếm 12 nguyên nhân trên 12 kết luận, độ dốc
       tích luỹ đúng bằng 1.0, suy ra "chưa bão hoà, con số thật có thể >10"; bên kia
       gộp thành 3-4 nguyên nhân mỗi nhóm, suy ra "thấp hơn cận dưới 5-10". Cả hai đều
       lập luận được. Đó KHÔNG phải bằng chứng cho hướng nào — đó là bằng chứng rằng
       n=32 quá nhỏ. Ghi "chưa đếm được", đừng ghi một con số nghe hay.

AR-p   Máy có được trích case NGOÀI nhóm để dựng BƯỚC LOẠI TRỪ không?     ← MỚI
       Sinh 2026-09-10 từ bản B đầu tiên (`IM-29`). ĐO ĐƯỢC, không phải suy đoán.

       Bản B thiếu hẳn `K5` của bản A — "trình duyệt đang đăng nhập nhiều tài khoản
       cùng lúc không?". K5 KHÔNG phải nhánh yếu: nó là `evidence-noi-ro`, chống lưng
       bởi `ES-341290` VÀ `ES-343036`. Và `ES-343036` KHÔNG thuộc nhóm — bản A cố ý
       MƯỢN nó từ nhóm "xung đột phiên" làm mốc cho một bước loại trừ, khai rõ việc
       mượn ở khối `_haiCaseLech`.

       Luật mà `G6`/`AP3` đòi và bộ eval thi hành là: mã case model viết ra phải TỒN
       TẠI trong nhóm. Luật đó chặn model bịa nguồn và nó làm đúng việc đó. Nhưng cùng
       lúc nó RÀO MÁY TRONG MỘT NHÓM, nên máy không dựng được bước loại trừ trỏ sang
       nhóm khác — việc người viết làm rất tự nhiên.

       ⚠ Và bộ eval KHÔNG phân biệt được "không bịa" với "không vươn tới được": bản B
         đặt `_haiCaseLech: null`, trích 9 mã đều trong nhóm, nên phép kiểm chống bịa
         nguồn QUA MỘT CÁCH TẦM THƯỜNG. Một phép kiểm qua vì đối tượng không thể chạm
         tới vùng bị cấm thì nó chưa kiểm gì cả.

       → Đây là lần đo THỨ HAI, TỪ CHIỀU NGƯỢC LẠI, của điều `docs/11` §9 đã ghi: bước
         kiểm đầu tiên của nhóm 2 bị chặn bởi đúng nguyên nhân của nhóm 1. Lần đó là
         hai nhóm phụ thuộc nhau khi THAO TÁC; lần này là một bước loại trừ phải MƯỢN
         case của nhóm khác. Hai quan sát độc lập, cùng kết luận: RANH GIỚI NHÓM CỦA
         TAXONOMY KHÔNG PHẢI RANH GIỚI HỢP LỆ CỦA MỘT SOP.

       Hai nhánh, và nó chạm vào cả `G6` lẫn hình dạng ĐẦU VÀO của `ISoạnNhápSOP`:
         (a) KHÔNG cho mượn — máy vĩnh viễn không dựng được bước loại trừ liên nhóm.
             Trần này là trần CẤU TRÚC, không phải hạn chế tạm của prompt, nên phải
             ghi vào tài liệu sản phẩm chứ không chờ prompt tốt hơn.
         (b) CHO mượn, kèm ba điều kiện: cấp corpus rộng hơn một nhóm · phép kiểm chống
             bịa nguồn đổi từ "trong nhóm" sang "trong corpus" · BẮT KHAI chỗ mượn,
             đúng như `_haiCaseLech` của bản A đã làm bằng tay.
       ⚠ Chọn (b) là làm ĐẮT hơn mỗi lượt gọi (corpus vào nhiều hơn) — đo lại giá
         trước khi chốt. Nhóm 10 case hiện ~$0,16.

AR-q   Bộ 5 ô phân bố cần ĐỊNH NGHĨA, không chỉ cần TÊN CỐ ĐỊNH.          ← MỚI
       Sinh 2026-09-10 từ `IM-29`. Đây là phần chưa xong của `IM-27`.

       `IM-27` đặt bộ 5 ô cố định vì "phân bố mà mỗi nhóm tự đặt tên là phân bố không
       so được giữa các nhóm". Đúng, nhưng CHƯA ĐỦ: cùng bộ ô, hai người đọc độc lập
       xếp khác nhau BA ticket trên MỘT nhóm 10 case.

                                     bản A                      bản B
         ES-341317   suy-ra-được                     ->  ghi-rõ-bước-kiểm
         ES-343733   bước-kiểm-NGOÀI-ticket (remote) ->  ghi-rõ-bước-kiểm
         ES-346559   ghi-rõ-bước-kiểm                ->  suy-ra-được
         ô "ngoài ticket"      A: 1 case                 B: 0 case

       🛑 Ô về 0 là chỗ đáng lo nhất, vì đó là ô mang PHÁT HIỆN NỀN của cả dự án —
         chẩn đoán xảy ra trên remote/điện thoại và không để lại chữ nào trong ticket.
         Bản B CÓ THẤY hiện tượng (nó viết: "mọi kết luận đều nằm trong ticket, dù
         việc kiểm thật xảy ra qua zalo/cuộc gọi/ultraview") nhưng đọc tên ô thành
         "kết luận ngoài ticket" rồi xếp sang ô khác. Tên ô nói về BƯỚC KIỂM; nó đọc
         thành KẾT LUẬN. Một chữ, và mất đúng ô quan trọng nhất.

       → Một ô mà hai người đọc hiểu khác nhau thì phân bố VẪN KHÔNG SO ĐƯỢC — đúng
         thứ `S8` sinh nó ra để tránh. Việc phải làm: viết ĐỊNH NGHĨA + CA BIÊN cho
         từng ô vào chính `description` của schema (model đọc chúng), rồi đo lại bằng
         cách cho HAI ngữ cảnh sạch xếp cùng một nhóm và so.
       ✅ Việc này làm được KHÔNG CẦN CREDIT, bằng đúng đường `--xuat-payload`.
       ⚠ Nhưng ĐỪNG sửa hai file cây dựng tay cho "khớp": chúng là bản A của `M2`
         (`IM-27`), sửa là hỏng mốc so.
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

---

# 9. Điều học được ngày 2026-08-25 (buổi 2)

> **Chuyển máy là phép thử mà không ai cố ý chạy — và nó đo đúng thứ không bộ test
> nào tự đo được: bộ test phụ thuộc vào cái gì.**

Máy mới, chưa cài PostgreSQL. `dotnet build` xanh, `dotnet test` **33/33 đỏ** với
`SocketException`. Không một test nào chạy được — kể cả những test không đụng tới
một dòng dữ liệu nào.

```text
Con số                   Nghĩa
33/33 cần Postgres       không phải "test tích hợp nhiều", mà là
                         KHÔNG CÓ tầng test nào bên dưới tầng tích hợp
```

Điều đáng nhớ không phải "nên có unit test" — mà là **cách phát hiện ra**. Trên máy
cũ, con số 33/33 vô hình: mọi thứ đều xanh nên không có gì để hỏi. Nó chỉ hiện ra khi
môi trường bị lấy đi. Cùng loại với `IM-5` (RLS bật mà không chặn gì) và với điểm 2 ở
§7 (superuser làm bộ đo hỏng): **một cấu hình sai vẫn cho ra toàn màu xanh**.

→ `IM-18`. Sau nó: 48 test chạy được ở bất kỳ đâu, 33 test cần một database thật.
Ranh giới đó giờ là một sự thật ghi trong `.csproj`, không phải một thói quen.
