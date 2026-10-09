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

## `IM-49` · Mười một phase để mọi ô của sơ đồ có code — và chín chỗ bản nháp kế hoạch đã sai

**Ngày 2026-10-10.** Sinh từ `AR-s` (§5). Đây là các lựa chọn KỸ THUẬT (`J1-1`); câu sản
phẩm nằm ở `docs/18` NHÓM N. Mỗi phase là một commit, và mục này được bổ sung khi phase xong.

### Bảy lựa chọn, kèm lý do

```text
1  Quy trình TUYẾN TÍNH (PR2). Bước = các bước KIỂM của SOP theo thứ tự + "xử lý theo
   nguyên nhân đã nhận ra" + bước xác nhận. Nhánh "thấy X thì là nguyên nhân Y" ở lại
   Knowledge (Recognition).
   Vì sao: SOP thật là CÂY (docs/11, IM-30) trong khi PR2 cấm nhánh — mâu thuẫn chưa ai
   gỡ. Tách "làm gì tiếp" (Process) khỏi "thấy gì thì kết luận gì" (Knowledge) gỡ được
   nó mà không phải mở lại PR2, và khớp PR3 (escalation là Knowledge) lẫn L1.

2  ProcessDefinition sinh ở DRAFT từ bản nháp SOP của Path A (S4 quy tắc 1, 05:197-198)
   + tạo tay qua API. Chỉ người duyệt mới ACTIVE (D4). Dùng lại StoredLifecycleState,
   VisibilityScope, KnowledgeApproval — 05 §5 nói Process dùng CHUNG nhân với Knowledge,
   nên không đẻ từ vựng mới (bệnh "từ vựng song song" đã tái phát 3 lần).

3  Ô D (khớp case → quy trình) = định tuyến nhóm → quy trình ACTIVE của nhóm đó.
   Không có quy trình thì RƠI VỀ đường SOP-theo-nhóm đang chạy, KHÔNG trả rỗng.

4  Chủ đề (L1) = khoá trên từng bước, khớp tri thức qua assertion Applicability — đúng
   cơ chế HoiHandler đang dùng cho nhãn nhóm. "Chủ đề" chưa từng được định nghĩa ở đâu
   (04 §3D.7 không có), nên KHÔNG thêm cột: AP1 nói applicability là assertion.

5  Bản B kế thừa dẫn chứng của bản A. Người sửa NỘI DUNG; mẩu nguồn không đổi.

6  Sửa lệch khoá jira:ES-… / ES-… (README §Rủi ro, 2026-10-09). Hôm qua chủ dự án chọn
   "ghi lại, chưa sửa"; mục tiêu mới đòi ô G chạy qua webhook nên phải sửa. ĐỔI LỰA CHỌN
   VÌ MỤC TIÊU ĐỔI, không phải vì lựa chọn cũ sai.

7  Nấc của ô J theo từ vựng ĐÃ KHOÁ L2 (04 §3B.2): RETRIEVED → SHOWN → OPENED → ACCEPTED
   → USED. "Không dùng" = nấc USED với DatDuoc=false — không đẻ nấc thứ sáu.
```

### Chín chỗ bản nháp kế hoạch sai — một agent soi lại với code TRƯỚC khi viết dòng nào

```text
1  Tên nấc tự đặt (da-tra, da-gui…) va với từ vựng đã khoá L2           -> dùng L2
2  PR-c, PR-e là câu MỞ ở 05 -> không được tự chốt                        -> PROPOSED, docs/18 N5/N6
3  Sửa khoá Anthropic định "truyền thẳng khoá" — đi ngược comment
   SopDrafterOptions ("khoá không đi qua tay mình")                       -> đọc từ IConfiguration
4  Định tuyến bằng model là một ĐƯỜNG RA MỚI tới bên thứ ba, và nó tự
   tính tiền trên mọi ticket                                               -> qua cổng che + cờ riêng
5  "Rút lại → NEEDS_REVIEW" là luật SUY RA: handler nào không nạp liên
   kết evidence thì luật hỏng IM LẶNG                                      -> MỘT extension nạp chung
6  Bản B chép dẫn chứng: KnowledgeRevisionHandler không nạp EvidenceLinks
   -> chép được 0 dòng mà không lỗi                                        -> nạp trước khi chép
7  Tắt che ở khâu NẠP là cửa MỘT CHIỀU                                     -> dấu trên từng mẩu,
                                                                              shared từ chối khởi động
8  ProcessDefinition thiếu phạm vi xem + cổng duyệt (05 §5 shared kernel)  -> dùng chung với Knowledge
9  A2, K không thể ✅ một cách trung thực khi không thêm thư viện          -> giữ ◐, nói rõ thiếu gì
```

⚠ Năm trong chín chỗ là **lỗi im lặng** — đúng loại lỗi mà cả dự án dựng ra để chặn. Không chỗ
nào làm crash. Ghi lại vì bản nháp đó trông hoàn chỉnh.

### P1 ✅ — hai lỗi định tuyến (2026-10-10)

```text
1  Bảng đo khớp chính xác, mà bảng lưu ES-… còn case mang jira:ES-…  -> mọi webhook KHÔNG BIẾT
   Sửa: khớp chính xác TRƯỚC (ý người ghi thắng), trượt thì bỏ tiền tố nguồn rồi thử lại.
   Bỏ MỌI tiền tố trước ':' đầu tiên + đuôi '#…' — gắn cứng "jira:" là trái G1.
2  Host KIỂM Anthropic:ApiKey, client ĐỌC ANTHROPIC_API_KEY — hai tên, hai chỗ đọc
   Sửa: KhoaAnthropic.Doc đọc cả hai từ IConfiguration, đưa thẳng cho AnthropicClient.
   Tách SopDrafterOptions.ModelMacDinh để đường gọi model thứ hai (P8) dùng lại đúng nó.
```

⚠ **Bẫy dựng sẵn mà sửa (2) suýt mở ra:** test chạy với biến môi trường của máy. Host bắt đầu
đọc `ANTHROPIC_API_KEY` thì một máy dev có sẵn khoá sẽ biến test *"chưa có credit"* thành một
lượt **gọi model thật** — tốn tiền, xanh/đỏ theo máy. `ApiFactory` giờ xoá trắng hai khoá đó
trước mọi cấu hình của test.

✅ 432/432 test (212 + 27 + 193), trước 418. Test webhook mới đã chứng minh biết ĐỎ: tắt đường
lùi → đỏ; bật lại → xanh.

### P2 ✅ — cờ tắt tạm cổng che `CongChe:TatTamVaChapNhanRuiRo` (2026-10-10)

```text
MỘT cửa          Api/Startup/CongChe — CheDuongRa (AR-o) · CheBenThuBa (AR-n B2a)
                 thay 4 helper Che() riêng lẻ: WebhookSender · XemLinkHandler · VongKhongCredit
                 + CaseEvidenceHandler. Cờ chỉ đọc ở MỘT chỗ.
Domain           SopPromptBuilder.Build(req, quaCongChe = true) — Domain không đọc cấu hình,
                 nên chỗ gọi mới quên tham số phải rơi về phía CÓ che. Có test giữ chiều rơi đó.
cửa MỘT CHIỀU    evidence_item.DaCheBenThuBa (nullable, KHÔNG điền bù: NULL = trước luật)
shared           TỪ CHỐI KHỞI ĐỘNG khi cờ bật — dữ liệu nhiều công ty, không ai trong số họ chấp nhận
nói ra           cảnh báo mỗi lần khởi động · mục riêng trong --kiem-cau-hinh · daQuaCongChe ở
                 webhook + /xem · câu "ĐÃ qua cổng che" trong file xuất đổi theo cờ
giao diện Jira   KP_CONG_CHE_TAT_TAM=1: check_corpus vẫn chạy và vẫn BÁO, chỉ thôi khoá nút gửi
```

⚠ **Tìm thấy lúc làm, sửa trong cùng commit:** mô tả OpenAPI của `POST /hoi` ngụ ý nội dung trả về
đi qua cổng che (*"cổng che ở khâu gửi ra chỉ phải canh một đường"*). Không đúng — `HoiHandler` trả
nội dung tri thức nguyên văn từ trước tới nay. Chỉ webhook và `/xem` đi qua cổng. Câu mô tả đã sửa
cho khớp; hành vi KHÔNG đổi.

⚠ **Sự cố môi trường giữa phase:** ổ `C:` của máy dev hết sạch chỗ (0 GB) — build, test và cả
heredoc của bash đều chết với *"No space left on device"*. Thư mục tạm chỉ chiếm ~800 MB nên thủ
phạm nằm chỗ khác, chưa tìm. Chủ dự án chọn chuyển thư mục tạm của phiên sang `D:	mp-kp`; KHÔNG
xoá gì trên `C:`.

✅ 439/439 test (214 + 27 + 198). Đột biến: cho `CongChe` bỏ qua cờ → đúng 2 test "cờ bật" đỏ.

### P3 ✅ — ô H: dẫn chứng thật cho từng phát biểu (2026-10-10)

`assertion_evidence` có 0 dòng KHÔNG phải vì chưa ai nạp: bản nháp mang mã ticket ở mọi nhánh
và mọi bước sửa, và `SopDraftToKnowledge` dùng nó để chấm mức kiểm chứng rồi VỨT.

```text
Domain     SopMappingResult.TrichDan — (phát biểu, mã case, chungCu, ghi chú). CauseExists trích
           HỢP các nhánh, không trùng; nhãn nhóm không trích gì.
nạp        mã → case: khớp CHÍNH XÁC, rồi khớp đuôi ":mã" (một câu LIKE ANY). Khớp >1 case =
           maMoHo, KHÔNG tự chọn. Không khớp = maKhongGiaiDuoc, VẪN nạp.
           case → MỌI mẩu chưa rút lại (AR-m). Bản nháp trích TICKET, chọn mẩu là đoán thay model.
           evidence-noi-ro / bước sửa → Support · toi-suy-ra → ContextFor (chungCu chỉ chọn quan hệ)
điền bù    nạp lại bản nháp thì nối thêm vào record ĐÃ CÓ, chỉ ở phát biểu AI còn hiệu lực chưa có
           liên kết nào. Nạp lần ba: 0.
lưu gốc    sop_draft_goc (RLS, unique theo hash) — lần sửa phép map sau KHÔNG phải nói lại câu
           "8 record cũ không điền bù được".
DB         khoá ngoại GHÉP (TenantId, EvidenceItemId) → evidence_item, RESTRICT. RLS WITH CHECK chỉ
           kiểm dòng đang ghi, không kiểm dòng nó trỏ tới.
V3         mẩu chống lưng bị RÚT SAU lần duyệt cuối → NEEDS_REVIEW; duyệt lại xoá được cờ.
bản B      ReviseAssertion chép liên kết, ghi chú "kế thừa từ bản A".
đọc        GET /knowledge/{id}: mỗi phát biểu kèm danChung (khoá nguồn mẩu, quan hệ, ghi chú,
           daRutLai) — KHÔNG kèm nội dung mẩu.
```

🛑 **Luật hỏng im lặng nhất của phase này** là luật SUY RA: `NeedsReview` đọc `EvidenceItem.RetractedAt`
qua navigation, nên chỗ đọc nào quên nạp tầng đó thì luật trả `false`. Năm chỗ tự viết `Include` được
thay bằng MỘT extension `KemDanChung`. Đột biến: bỏ tầng nạp mẩu khỏi extension → đúng test NEEDS_REVIEW
qua HTTP đỏ. Có thêm một test Domain GHIM chiều hỏng đó (thiếu navigation → `false`).

⚠ Một kỳ vọng của chính tôi sai, sửa trong cùng phase: `ON DELETE RESTRICT` báo `23001`
(*restrict_violation*), không phải `23503`. Database đang làm đúng việc của nó.

⚠ **CHƯA kiểm trên dữ liệu thật.** Mọi số ở đây đo trên vật liệu test. Bản nháp thật
(`docs/ket-qua-phan-tich/may-sinh-*.json`) trích mã TRẦN trong khi `kp_dev` lưu `jira:ES-…` — đúng ca
khớp đuôi được thiết kế cho, nhưng chưa chạy. Để vào phép kiểm đầu-cuối cuối lộ trình.

✅ 460/460 test (223 + 30 + 207), trước 439.

### P4 ✅ — ô D/E/F: quy trình, lượt chạy, bước hiện tại (2026-10-10)

```text
Domain/Process   ProcessDefinition (DRAFT/ACTIVE/DEPRECATED, phạm vi xem, origin, lượt duyệt —
                 CHUNG nhân với Knowledge) · ProcessStep (thứ tự, mã, nội dung, dấu hiệu, chủ đề)
                 · ProcessRun (case, định nghĩa, lúc bắt đầu, ĐÃ BỎ chưa — KHÔNG cột tiến độ)
E + F            TienDoQuyTrinh.SuyRa — hàm THUẦN. Bước xong ⟸ mẩu chứa dấu hiệu (không dấu,
                 không hoa thường) HOẶC lời xác nhận trỏ đúng bước. Bước hiện tại = bước chưa
                 xong ĐẦU TIÊN. Mỗi bước xong kèm căn cứ: mẩu nào, bằng cách nào.
D                KhopQuyTrinh: định tuyến case → quy trình ACTIVE của nhóm → tìm/tạo lượt chạy.
                 Case đang có lượt chạy thì GIỮ nó (N6-1). Lượt bị bỏ thì KHÔNG tự dựng lại.
sinh từ Path A   bản nháp SOP → quy trình DRAFT, đúng một lần theo sop_draft_goc. Bước = bước
                 kiểm + "XL" + "XN". Nhánh ở lại Knowledge (PR2).
chủ đề (L1)      record mang Applicability "Áp dụng ở bước kiểm: <nhóm> · <mã>" — CÙNG một hàm
                 ghép nhãn cho cả lúc ghi và lúc bước tra, nên lệch nhau là không thể.
xác nhận         POST /quy-trinh/xac-nhan-buoc → một mẩu EVIDENCE (Origin = UserConfirmed,
                 XacNhanProcessStepId), subTenantCode bắt buộc. Rút lại được như mọi mẩu.
DB               một nhóm ≤ 1 bản ACTIVE · một case ≤ 1 lượt chạy mở · một bản nháp ≤ 1 quy trình
                 — cả ba là unique index có điều kiện, không chỉ là kiểm ở handler
duyệt            cùng ba chốt với duyệt tri thức; cổng AR-l tách ra CongDuyetARl dùng chung
```

⚠ **Hai lượt ghi trong một giao dịch, ở hai chỗ, và cùng một lý do:** thay bước (xoá cũ rồi thêm
mới) và duyệt phiên bản mới (rút bản cũ rồi bật bản mới). Một lượt `SaveChanges` không hứa thứ tự
giữa các lệnh, và cả hai chỗ đều có unique index sẽ nổ nếu thứ tự sai.

⚠ **Bước sinh từ bản nháp KHÔNG có dấu hiệu nào** — `SopDraft` không có trường đó. Chúng chỉ xong
bằng lời xác nhận cho tới khi người duyệt thêm dấu hiệu lúc còn DRAFT. Response nói rõ ở cả lúc nạp
lẫn lúc đọc quy trình. Không tự đoán dấu hiệu từ `noiXem`: đoán là bước xong GIẢ.

⚠ Namespace phía API là `QuyTrinh`, không phải `Process`: `LenhDinhTuyen` gọi `Process.Start`, và
một namespace `KnowledgePlatform.Api.Process` sẽ che tên đó ở mọi file trong `KnowledgePlatform.Api.*`.

✅ 489/489 test (240 + 30 + 219), trước 460. Đột biến: gỡ luật "lượt bị bỏ thì không dựng lại" →
đúng test đó đỏ. `kiem_dau_cuoi.py` mục M gọi thật cả 8 route mới; `kiem_hai_khach.py` thêm hai phép
(duyệt quy trình ở shared → 409 AR-l · khoá A đọc quy trình của B → 404). Hai script này CHƯA chạy —
để vào phép kiểm đầu-cuối cuối lộ trình.

### P5 ✅ — ô G/I: tra theo BƯỚC, trả kèm dẫn chứng (2026-10-10)

```text
lõi chung    HoiHandler dùng cho CẢ /hoi lẫn webhook — một chỗ quyết định biết / không biết
case đã có   maTicket (khoá đầy đủ hoặc mã TRẦN) → TraCaseTheoMa (tách từ P3, dùng chung) →
             định tuyến bằng chính dữ liệu của case (tiêu đề + mẩu sớm nhất, docs/13 §1)
ô D          khớp quy trình; hỏi về case = case đang được xử lý → tạo lượt chạy NẾU có bản ACTIVE
ô G          tra theo chủ đề của bước hiện tại → không có tri thức gắn bước thì RƠI VỀ nhóm
             (traTheo: buoc | nhom). Case đang chạy quy trình thì nhóm lấy từ quy trình, kể cả khi
             định tuyến nói không biết.
ô I          mỗi phát biểu kèm danChung: khoá nguồn mẩu + ticket + quan hệ + ghi chú. KHÔNG nội
             dung mẩu. Mẩu đã rút không được trích. /xem cũng kèm; webhook kèm ticketDanChung.
hợp đồng     ô mới đều bỏ qua khi null (IM-15) — thiếu quyTrinh KHÔNG phải "quy trình rỗng";
             phienBan webhook giữ "1"
```

⚠ Hai cửa KHÔNG BIẾT giữ nguyên, và test cũ (219 test API trước P5) xanh nguyên không sửa dòng nào.

⚠ **Sự cố quy trình của chính tôi lúc commit phase này:** bước sửa tài liệu thất bại (một mẫu khớp sai),
nên file commit message vẫn là của P4 — và lệnh commit nằm trên dòng riêng nên vẫn chạy. Kết quả là một
commit chứa code P5 mang tên P4. Commit đó chưa push (`main` đi trước `origin/main` 35 commit), nên sửa bằng
`--amend` ngay. Bài học: lệnh commit phải nối `&&` với bước chuẩn bị message, không đứng riêng một dòng.

✅ 494/494 test (240 + 30 + 224), trước 489. Đột biến: tắt nhánh tra theo bước → 2 test đỏ (câu
trả lời đổi theo bước · gói webhook chỉ mang nguyên nhân của bước hiện tại).

### P6 ✅ — ô J + vòng J→G: lượt gợi ý, 5 nấc L2, lời khai (2026-10-10)

```text
luot_goi_y          một dòng = một câu trả lời ĐÃ trả / ĐÃ gửi, kèm ẢNH CHỤP JSON đúng thứ đó
luot_goi_y_su_kien  append-only; nấc theo từ vựng ĐÃ KHOÁ L2 + DatDuoc (không đẻ nấc thứ sáu)
RETRIEVED           mọi lượt, kể cả KHÔNG BIẾT (DatDuoc = false)
SHOWN               /hoi: lúc trả response · webhook: CHỈ khi bên nhận trả 2xx
OPENED              mở /xem/{token} — token mang id lượt gợi ý đã phát nó; ghi mỗi lần mở
ACCEPTED / USED     LỜI KHAI qua POST /goi-y/{id}/phan-hoi: datDuoc + nguoiKhai BẮT BUỘC
J → G               nhomDung (E2-1, phải có trong taxonomy) → ghi kho định tuyến của CHÍNH case
                    đó; lượt chạy quy trình của nhóm cũ bị bỏ, bởi chính người khai
phễu                GET /internal/goi-y/thong-ke — số LƯỢT có ít nhất một sự kiện đạt / không đạt
```

🛑 **Hai thứ trông giống nhau mà khác hẳn, ghi ra để không ai "gộp cho gọn":**
`WebhookDelivery` cố ý KHÔNG lưu payload (lượt thử lại phải mang câu trả lời MỚI NHẤT), còn `luot_goi_y`
lưu đúng payload (câu *"hôm ấy đã gửi gì"* không dựng lại được từ một kho tri thức đổi liên tục, `G5`).
Và `maGoi` (chống trùng của lượt GỬI) khác `maLuotGoiY` (đổi theo từng lần thử) — có test giữ.

⚠ Vì sao phản hồi sửa nhóm phải BỎ lượt chạy cũ: case đang chạy thì giữ quy trình của nó (`N6-1`), nên
không bỏ thì sửa định tuyến không có tác dụng gì cho tới khi ai đó nhớ ra mà bỏ tay.

⚠ Nhãn vòng J→G trên sơ đồ README ghi *"nạp lại cho thước đo tháng đầu"* — nhưng `docs/02` định nghĩa
thước đo tháng đầu là M2 (duyệt + diff A/B), còn "có được dùng không" là M3 / tầng 1. Sửa nhãn ở P10.

✅ 505/505 test (240 + 30 + 235), trước 494. Đột biến: bỏ dòng ghi SHOWN sau khi gửi thành công → đúng
test đó đỏ. `kiem_dau_cuoi.py` mục N gọi thật hai route mới (không khai `nhomDung` — nó sửa dữ liệu thật).

### P7 ✅ — "nhận về là xử lý luôn" (2026-10-10)

Mẩu evidence MỚI (đường nạp tín hiệu) hoặc một lời xác nhận bước làm case BƯỚC SANG bước khác → xếp một
lượt trả kết quả về, `suKien: buoc-hien-tai-doi`, gói mang tri thức của bước MỚI.

🛑 "Bước đổi" nhận ra bằng cách SUY tiến độ HAI LẦN — trước và sau lô vừa nhận — bằng cùng hàm thuần
`TienDoQuyTrinh`. Không có cờ "bước trước đó" nào được lưu (PR1). So trước/sau chứ không so với lượt gửi
gần nhất: lượt "case mới" đã mang bước K1, và so với nó thì mẩu đầu tiên ở K1 sẽ đẻ một thông báo trùng.

```text
KHÔNG xếp khi    chưa có lượt chạy · case đã xong ở nguồn (tinh thần F1-1) · đang có lượt CHỜ (nó dựng
                 gói lúc gửi nên tự mang bước mới nhất) · khoá chống trùng (lượt chạy + bước) đã có
chống trùng      webhook_delivery.KhoaKhuTrung, unique khi có giá trị — hai lô cùng làm đổi một bước
                 cùng lúc thì database giữ đúng một lượt
case-status      VẪN không làm gì (F1-1, có test giữ)
rút lại (AR-m)   có thể làm tiến độ LÙI, nhưng đó là việc của người — không kích hoạt
```

✅ 511/511 test (240 + 30 + 241), trước 505. Đột biến: bỏ phép so trước/sau (xếp với mọi mẩu mới) → đúng
test "không đổi bước thì không xếp" đỏ.

### P8 ✅ (code) — định tuyến ticket MỚI bằng model (2026-10-10, `N7-1` PROPOSED)

```text
thứ tự tra      người nạp → bảng đo → kết quả model đã lưu → GỌI model
                bảng đo nói KHONG-BIET cho ticket ĐÃ ĐO thì đó là KẾT QUẢ — model không đoán lại
chỉ gọi khi     có khoá Anthropic VÀ DinhTuyen:GoiModel = true (mặc định TẮT)
payload         DinhTuyenPromptBuilder: CHỈ tiêu đề + mô tả, KHÔNG tham số mã ticket nào (docs/13 §1);
                qua cổng che như mọi đường ra (AR-o, cờ N1-2)
nghe model khi  doTinCay ≥ NguongTinCay (0,9 — CHƯA ĐO) VÀ nhóm có trong taxonomy (G6)
chi phí         biên lai GHI TRƯỚC khi gọi · trần DinhTuyen:TranThang (500/tháng/khách) · lượt thành công
                là BỘ NHỚ ĐỆM (định tuyến chạy trên mọi webhook và mọi /hoi)
lỗi             mất mạng / hết credit → KHÔNG BIẾT + biên lai giữ lỗi; /hoi VẪN 200
nguồn           "model:…· ngưỡng 0.90 CHƯA ĐO" — không trộn với tra-san: hay nguoi-nap:
```

🛑 **CHƯA TỪNG GỌI THẬT.** Hình dạng request/response kiểm bằng HttpClient giả cắm vào `AnthropicClient`
(SDK nhận response giả, request mang `output_config`). Con số 84,1% của `docs/13` KHÔNG áp cho prompt này —
prompt này chưa được đo. Ngưỡng 0,9 cũng chưa đo. Hai thứ đó chỉ có được khi có credit.

⚠ Model dùng lại đúng `SopDrafterOptions.ModelMacDinh` (`AR3`). Một model rẻ hơn cho phân loại là hợp lý nhưng là
câu §10.1 — agent không tự chọn.

✅ 526/526 test (244 + 33 + 249), trước 511. Đột biến: gỡ ngưỡng tin cậy → đúng test "dưới ngưỡng thì im lặng" đỏ.

### P9 ✅ — Kênh 2: trích chữ + job quét thư mục, KHÔNG thêm thư viện (2026-10-10, `N4-1`)

```text
TrichChu (Domain)  txt · md · csv · json · xml · log → UTF-8 NGHIÊM (sai thì "khong-giai-ma-duoc", không đoán)
                   html → bỏ thẻ / script / style, giải thực thể
                   docx → ZIP → word/document.xml → w:t   (System.IO.Compression + XmlReader)
                   pdf · ảnh → "chua-ho-tro" — KHÔNG OCR, KHÔNG đọc PDF
an toàn .docx      DtdProcessing.Prohibit (XXE / entity bomb) · trần 50 MB sau giải nén (zip bomb)
lúc nạp            trích từ ĐÚNG chuỗi byte vừa băm; trích hỏng KHÔNG chặn nạp, trạng thái nói vì sao
đọc                GET /documents/{id}/chu
job quét           QuetThuMucService — mỗi nguồn một scope, tenant đặt tường minh (khuôn webhook);
                   nạp qua ĐÚNG DocumentHandler nên khử trùng theo hash + S6 áp nguyên
cấu hình           KenhHai:Nguon[] {ThuMuc, SubTenantCode, NguoiNap, TenantKey} — thiếu là KHÔNG KHỞI ĐỘNG;
                   dedicated khai khách khác cũng không khởi động (bài học I2-1)
```

⚠ **Lỗi của chính tôi, tìm ra khi đọc lại trước khi chạy test:** vòng đọc XML bản đầu dùng
`while (reader.Read())` — mà `ReadElementContentAsString` đã đưa con trỏ tới nút KẾ TIẾP, nên Read() nhảy qua
mất nó. Hai `w:t` liền nhau trong một run thì mất đoạn thứ hai, im lặng. Có test giữ, và đã chứng minh biết ĐỎ
bằng cách tái tạo đúng lỗi đó.

⚠ **A2 và K vẫn `◐`, đúng như `N4-1` đã nói trước:** Drive không có connector (thư mục Drive ĐỒNG BỘ VỀ MÁY là
đường duy nhất), Jira vẫn quét bằng giao diện Python rồi đẩy vào Kênh 1, PDF/ảnh chưa đọc được.

⚠ Chữ trích ra **CHƯA qua cổng che nào** — hôm nay nó không đi ra đâu ngoài chính đường đọc của nó (như `/tai`
trả file gốc). Ngày nó đi vào bản nháp SOP thì PHẢI qua cổng che, y như evidence (`IM-47`).

✅ 539/539 test (252 + 33 + 254), trước 526. Đột biến: tái tạo lỗi nhảy nút trong vòng đọc docx → đúng test đó đỏ;
gỡ luật bắt buộc SubTenantCode của nguồn quét → đúng test "không khởi động" đỏ.

---

## `IM-48` · Cổng credential kêu 45 lần, đúng 9 lần — và chủ dự án chọn nạp cả lô

**Ngày 2026-09-24.** Lô 150 case quét ngày 23/09 qua giao diện (`IM-45`) bị `check_corpus`
báo **CHẶN**: *45 chỗ nghi là credential*. `api_gui_kho` trả 409, đúng thiết kế `AR-j`.

Soi tay cả 45 chỗ, không đọc output mà gọi thẳng `quet_bi_mat()` để lấy dữ liệu (đúng lý do
hàm đó được tách ra):

```text
THẬT   9 chỗ / 5 mẩu
NHẦM  36 chỗ / 15 mẩu          ->  precision 20%
```

### 9 chỗ thật

```text
ES-348807  JWT dán trong comment.  nbf 2026-09-16 · exp 2036-09-16 -> CÒN HIỆU LỰC
           payload mang sẵn SiteID + ClientID.  Nghiêm trọng nhất trong lô.
ES-349641  cặp UltraViewer: ID + mật khẩu
ES-349666  cặp UltraViewer: ID + mật khẩu
ES-349510  cặp UltraViewer: ID + mật khẩu
ES-349684  cặp UltraViewer: ID + mật khẩu  — KHÔNG có nhãn nào
```

⚠ Giá trị thật **không** ghi ở đây, cố ý. File này được git theo dõi.

`ES-349684` là chỗ đáng giữ nhất: nhân viên nhắn *"cho em xin ultraview vào máy kiểm tra ạ"*,
khách trả lời bằng **hai dãy số trần**, không một chữ "mật khẩu" nào. Hình dạng 2 (tên công
cụ) + hình dạng 3 (số trần ở dòng kế tiếp) bắt được nó. Không luật nào dựa vào nhãn làm được
việc này — và recall đó là lý do giữ nguyên bộ luật ở phần dưới.

### 36 chỗ nhầm — nhiễu CÓ HỆ THỐNG, không ngẫu nhiên

Truy ngược đúng từ khoá làm hình dạng 4 (`key JSON`) nổ:

```text
"username" : null                    11   giá trị là chữ `null`
PublicKeyToken=31bf3856ad364e35      10   hằng số công khai của Microsoft, trong stack trace .NET
"bookingAccount" : "<GUID>"          11   mã booking nội bộ, không phải khoá đăng nhập
Category id 12704                     2   id danh mục sản phẩm
Agoda ID: 91997688                    2   id kênh OTA
```

32/36 đến từ ba từ khoá `account|username|token` khớp vào **payload nghiệp vụ**, không phải
vào bí mật. Nghĩa là mỗi case OTA lỗi sẽ sinh đúng 3 chỗ nhầm — càng quét nhiều càng ồn.

### Quyết định của chủ dự án, 2026-09-24

```text
lô 23/09          NẠP CẢ 150, chấp nhận rủi ro      (sau khi đã đọc đủ 9 chỗ thật)
luật check_corpus GIỮ NGUYÊN fail-closed, không thêm ngoại lệ nào
```

Hai lựa chọn này xung đột về cơ chế: giữ nguyên luật thì nút "gửi vào kho" vẫn 409. Giải bằng
cách **không đụng vào luật** — lô này đẩy thẳng qua `/signals/*` bằng script một lần, không
qua giao diện. `check_corpus.py` và `api_gui_kho` không sửa một dòng nào.

Kết quả: `dev-tenant` 235 → **384 case**, 377 → **717 evidence**. Năm mẩu mang bí mật nằm
trong `kp.evidence_item` của `kp_dev` trên localhost — **database dev, không phải bản chạy
thật**.

### Phản biện đã nêu, và đã bị bác

Một cổng kêu 45 lần mà sai 36 lần sẽ dạy người đọc bấm qua nó. Lần sau đúng cái JWT 10 năm
có thể đi lọt vì nó nằm lẫn trong 36 dòng nhiễu mà ai cũng đã quen bỏ qua — cùng bệnh với
`IM-42` (cổng canh trông như đang canh) và `IM-38` (bộ kiểm trông như đang phủ). Chủ dự án
đã đọc lập luận này và vẫn chọn giữ nguyên. Ghi lại để lần sau không phải suy luận lại từ đầu.

⚠ Một điều không được quên: **5 mẩu này đã nằm trên Jira từ trước.** Chặn ở đây chỉ ngăn
nhân bản, không thu hồi được gì. Việc thu hồi JWT `ES-348807` phải làm ở phía hệ thống cấp
token, và nó vẫn đang mở.

---

## `IM-47` · Kênh 2 mở — và nó KHÔNG làm kho tri thức tăng một dòng nào

**Ngày 2026-09-17, bước 8 — bước cuối của lộ trình mức 3.**

```text
kp.document                  siêu dữ liệu, tenant-scoped, CÓ RLS. Nội dung ở kho file (AR5)
IKhoFile + KhoFileDia        interface MỎNG theo khuôn AR3, bản đang cắm chạy trên ĐĨA
POST /documents              multipart; nạp lại cùng nội dung KHÔNG sinh bản thứ hai
GET  /documents              siêu dữ liệu
GET  /documents/{id}/tai     tải nội dung
✅ 418/418 test xanh (212 + 19 + 187), trước 407 · 0 warning
✅ kiem_dau_cuoi.py 60 phép kiểm · 0 sai · 22 endpoint
```

**🛑 TEST QUAN TRỌNG NHẤT KHẲNG ĐỊNH MỘT SỐ KHÔNG.** `S6` chốt từ Workstream 04: *nạp tài
liệu KHÔNG sinh KnowledgeRecord*. Đó là lý do Kênh 2 bị hoãn suốt — build xong đường này thì
kho tri thức **vẫn là 0**, vì tri thức chỉ ra đời khi có **hành vi khẳng định của một người**.
Một file .docx trong kho là một **VẬT CHỨA**, không phải một phát biểu ai chịu trách nhiệm.
→ `Nap_tai_lieu_KHONG_sinh_ra_tri_thuc` giữ chính quyết định đó. Ngày nào nó đỏ là ngày cả
  `D4` lẫn hai cổng của `/hoi` bị đi vòng qua.

**🛑 `H3-2` CHỐT MinIO, NHƯNG BẢN ĐANG CẮM LÀ ĐĨA CỤC BỘ — và lý do phải nói thẳng ra:** máy
dựng bản này **không có container runtime**, nên một bản hiện thực MinIO viết ở đây sẽ là
**code chưa từng chạy một lần nào**. Dự án đã đo cái giá của thứ đó hai lần trong hai ngày
(`IM-42`: một cổng canh trông như đang canh; `IM-38`: một bộ kiểm trông như đang phủ).
→ Đĩa cục bộ **không phải lựa chọn thay thế `H3-2`**; nó là chỗ đứng cho tới ngày có MinIO
  chạy thật. Bản MinIO là MỘT class cài `IKhoFile`, cắm bằng một dòng DI.

**🛑 TẦNG KHO FILE KHÔNG CÓ RLS CHE LƯNG, nên nó phải tự đứng vững.** Hai chốt, và test
`Khach_A_khong_tai_duoc_tai_lieu_cua_khach_B` cố ý cho cả hai khách dùng **CHUNG một thư mục
kho** — ca xấu nhất, không phải ca dễ:
```text
thư mục theo tenant     một lỗi ghép đường dẫn không đủ để khách A đọc file khách B
mã kho do SẢN PHẨM sinh tên người nạp khai KHÔNG BAO GIỜ chạm tới đường dẫn. Dùng nó là mở
                        thẳng đường đi ngang thư mục (`../../`) và đường ghi đè file khách khác
```
Có test riêng cho tên file mang `../../../`.

**⚠ Nạp trùng so bằng SHA-256 của NỘI DUNG, không bằng tên** — job đồng bộ chạy lại là chuyện
thường (cùng lý do `IM-20`), và cùng một file thường được đặt hai tên khác nhau.

**🛑 CÒN LẠI, và đây là chỗ dễ đọc nhầm nhất của cả bước này:**
```text
· Tài liệu CHƯA đi qua cổng che nào. Hôm nay nó không đi đâu cả nên chưa cần — nhưng ngày
  nội dung tài liệu được đưa vào bản nháp SOP thì nó PHẢI qua `AR-o`, y hệt evidence.
· Chưa quét credential lúc nạp. `AR-j` quét evidence; tài liệu thì chưa. Một file .txt chứa
  mật khẩu nạp lên hôm nay sẽ nằm nguyên trong kho.
· Chưa có OCR, chưa trích văn bản. Kênh 2 mới mang FILE vào, chưa mang CHỮ vào.
```

---

## `IM-46` · Soạn nháp vào host — và hai vòng không-credit nối vào nhau

**Ngày 2026-09-17, bước 4.** `ISopDrafter` trước nay chỉ sống trong `SoanNhapRunner`, một công
cụ rời đọc corpus trên đĩa và **không biết gì về tenant**.

```text
POST /knowledge/soan-nhap        gom case của một nhóm -> gọi model -> nạp (DRAFT)
--xuat-soan-nhap                 nửa XUẤT của vòng không-credit, đọc từ DB của khách
kp.luot_soan_nhap                biên lai mỗi lượt TỐN TIỀN + chỗ đếm cho trần tháng (E3-1)
SopDrafterChuaCoCredit           bản cắm khi chưa có khoá — NÉM kèm hướng dẫn
✅ 407/407 test xanh (212 + 19 + 176), trước 400 · 0 warning
✅ kiem_dau_cuoi.py 55 phép kiểm · 0 sai · 19 endpoint
```

**🎯 CHỖ NỐI ĐÁNG NHẤT: nhóm lấy từ KHO ĐỊNH TUYẾN, không từ file taxonomy.** Vòng
không-credit của bước 3 (`IM-45`) nạp *ticket nào thuộc nhóm nào*; vòng này đọc chính nó ra để
biết gom case nào. **Hai vòng nối vào nhau, và cả hai chạy được khi chưa có credit** — đó là
toàn bộ giá trị của hôm nay.

**🛑 CHƯA CÓ CREDIT THÌ KHÔNG HỎNG IM LẶNG.** Ba nhánh đã cân nhắc, và hai nhánh đầu đều tệ:

```text
để trống DI              -> host KHÔNG KHỞI ĐỘNG vì một tính năng chưa dùng tới
trả bản nháp rỗng        -> nó đi thẳng vào kho ở trạng thái Draft và TRÔNG NHƯ THẬT
bản NÉM kèm hướng dẫn    <- chọn cái này: 409 + `lyDo: chua-co-credit` + ba bước thay thế
```
Có test giữ cả hai vế: trả đúng lý do, **và** không record nào lọt vào kho.

**🛑 TRẦN THÁNG CHẶN *TRƯỚC* LƯỢT GỌI, và test khẳng định đúng điều đó** bằng cách đếm số lần
bản giả bị gọi. Chặn sau thì tiền đã tiêu rồi — một cái trần như vậy chỉ là một dòng chữ.

**⚠ Biên lai ghi CẢ lượt hỏng.** Gọi model mà đứt giữa chừng vẫn có thể đã tính tiền; không
ghi thì trần đếm thiếu, và nó đếm thiếu **đúng vào lúc có sự cố lặp đi lặp lại**.

**⚠ `nguoiYeuCau` là LỜI KHAI, không phải phép xác thực** — và điều đó nằm trong tên biến lẫn
trong mô tả OpenAPI. Sản phẩm hôm nay biết *request thuộc công ty nào*, chưa biết *người nào*
(`docs/18` §D5). `E3-1` nói *"chỉ người có quyền duyệt"*; phần "người" của câu đó vẫn chưa
thực thi được, nên đừng đọc ô này thành một chuỗi uỷ quyền.

**✅ Và bài học tenant-dùng-chung nhận ra sớm hơn một nhịp.** Biên lai `LuotSoanNhap` là dữ
liệu *"đếm được bao nhiêu"* của cả tenant, nên test trần tháng dùng chung tenant A sẽ đỏ theo
THỨ TỰ CHẠY. Lần này sửa ngay bằng một khách hàng riêng cho mỗi test — cùng hình dạng đã trả
giá ở `IM-31` và `IM-40`.

---

## `IM-45` · Vòng KHÔNG-CREDIT cho định tuyến — cái nạng, và vì sao phải gọi nó là cái nạng

**Ngày 2026-09-17.** Bước 3 và 4 đều chặn ở credit Anthropic. Chủ dự án hỏi: *xuất dữ liệu ra,
thêm prompt để AI đọc rồi nạp kết quả về, được không?* — được, và **một nửa đã có sẵn**:

```text
bước 4  soạn nháp SOP   ĐÃ CÓ từ `IM-29`: SoanNhapRunner --xuat-payload -> AI -> POST sop-draft
bước 3  định tuyến      CHƯA CÓ  ->  làm hôm nay
```

```text
kp.dinh_tuyen_da_nap        kho kết quả NGƯỜI nạp, tenant-scoped, CÓ RLS
--xuat-dinh-tuyen <khách> <file> [n]   xuất ticket chưa định tuyến + taxonomy + prompt
--nap-dinh-tuyen  <khách> <file> <ai>  nạp về, từ chối tên nhóm bịa
DinhTuyenKho                kho TRƯỚC, bảng đã đo SAU; nguồn nói rõ đến từ đâu
✅ 400/400 test xanh (212 + 19 + 169), trước 391 · 0 warning
✅ chạy THẬT cả vòng: xuất 5 ticket -> đóng vai AI -> nạp -> /hoi trả đúng nhóm đã nạp
```

**🛑 VÌ SAO GỌI NÓ LÀ CÁI NẠNG, VÀ VÌ SAO ĐIỀU ĐÓ PHẢI NẰM TRONG CODE chứ không chỉ trong
tài liệu.** Bảng này là **bộ nhớ của một việc NGƯỜI làm**, không phải sản phẩm tự định tuyến.
Mỗi lô ticket mới lại cần một lượt xuất-dán-nạp nữa. Nguy cơ thật là sáu tháng sau có người
nhìn thấy `/hoi` trả đúng nhóm rồi kết luận *"định tuyến chạy rồi"*.
→ Nên `KetQuaDinhTuyen.Nguon` mang **tiền tố nói rõ nguồn**: `nguoi-nap:` hay `tra-san:`.
  Một câu trả lời không phân biệt được hai thứ đó là cách nhanh nhất để về sau **không ai
  chấm lại được gì** — đúng chỗ mà `R-K4` đã phải hạ cấp một kết luận vì nó viết ở sai mức.

**🛑 VÀ NÓ KHÔNG MANG CON SỐ 84,1%.** Con số ấy đo trên MỘT prompt và MỘT model ở `docs/13`.
Một ngữ cảnh chat khác, một model khác, một ngày khác — chất lượng có thể khác, và **không ai
biết khác bao nhiêu cho tới khi chấm lại** bằng `cham_routing.py`. Lệnh nạp in ra đúng câu đó
sau mỗi lần chạy.

**Bốn cổng chặn ở đường nạp, mỗi cổng vì một sự cố đã có:**
```text
tên nhóm không trong taxonomy  -> TỪ CHỐI, và KHÔNG "sửa cho giống". Sửa hộ là biến một lỗi
                                  nhìn thấy được thành một lỗi im lặng. `G6`/`AP3`, và
                                  `cham_routing.py` gọi đây là bẫy thứ ba.
khoá nguồn không có case       -> TỪ CHỐI. Nạp cho case không tồn tại là rác im lặng.
KHONG-BIET                     -> NHẬN. 62/150 ticket rơi vào đó ở phép đo trước; từ chối nó
                                  là ép người nạp đoán bừa.
nạp lại                        -> ĐÈ. Người nạp lại là người đang sửa thứ họ thấy sai. Khác
                                  `K-B3` (không ghi đè quá khứ của TRI THỨC đã duyệt).
```

**Ba cái chốt chép nguyên từ `SoanNhapRunner --xuat-payload`**, không phải để nhất quán cho
đẹp mà vì mỗi cái có một sự cố đứng sau:
```text
· nội dung đi qua cổng che AR-o TRƯỚC khi ra file — đã kiểm bằng vật liệu lấy từ luật đo được
· từ chối ghi vào chỗ trong repo mà git KHÔNG chặn — HỎI GIT, không so tên thư mục.
  Repo đã rò một lần theo đúng hình dạng đó (`888c8e7`). Đã chạy thử: nó từ chối thật.
· bắt truyền đường dẫn tường minh, không có mặc định tiện tay
```

**⚠ Đầu vào cố ý NGHÈO:** chỉ tiêu đề + **mẩu SỚM NHẤT** của case. Đúng định nghĩa `docs/13`
§1 — thứ có mặt LÚC TICKET ĐẾN. `thu_retrieval.py` đã đo được rằng đưa cả transcript vào còn
**tệ hơn**, vì transcript chứa sẵn câu chẩn đoán: đó là đưa đáp án vào đề.

**⚠ Còn lại:** vòng này chỉ giải bước 3. Bước 4 (nối `ISopDrafter` vào host) vẫn dùng đường
`SoanNhapRunner --xuat-payload` rời — chạy được, nhưng là một công cụ khác, không phải một
lệnh của host.

---

## `IM-44` · Hồ sơ deploy là một LỆNH CHẠY ĐƯỢC, không phải một mục trong tài liệu

**Ngày 2026-09-17, bước 6.** `H1-5` chốt *chạy local Windows trước*, `H1b-1` chốt *máy nằm
trong LAN/VPN*. Ba thứ thêm ở bước 1-2 (`Auth`, `Link:PublicBaseUrl`, `Webhook`) chưa có
hướng dẫn nào.

```text
--kiem-cau-hinh                       in cấu hình ĐANG CÓ HIỆU LỰC + gọi tên chỗ chưa xong
docs/20_CHAY_TREN_WINDOWS.md          vì sao từng giá trị được đặt như vậy
scripts/chay-windows.example.ps1      bản mẫu, chép ra rồi sửa (bản thật đã .gitignore)
cảnh báo loopback lúc khởi động       cho cái bẫy mà không phép kiểm tự động nào bắt được
✅ 391/391 test xanh (212 + 19 + 160), trước 382 · 0 warning
```

**🎯 QUYẾT ĐỊNH CHÍNH: hồ sơ deploy là một LỆNH, tài liệu chỉ giải thích VÌ SAO.**

Dự án đã đo được **hai lần** rằng tài liệu tự lỗi thời trong im lặng: bộ Postman đứng ở 5/13
endpoint hơn hai tuần (`IM-38`), và `docs/10` ghi *"newman có sẵn"* trong khi máy mới không
có. Một hồ sơ deploy viết ra giấy sẽ đi đúng đường đó — **nó mô tả cái máy mà người viết đang
ngồi, không mô tả cái máy người đọc đang dựng.**

`--kiem-cau-hinh` thì đọc chính cấu hình đang chạy, nên không lỗi thời được. `docs/20` vì thế
cố ý **không** liệt kê giá trị; nó chỉ trả lời câu *"vì sao đặt như vậy"* — phần mà một lệnh
không nói hộ được.

**🛑 CÁI BẪY NGUY NHẤT CỦA BẢN DEPLOY LOCAL, và nó có hình dạng rất đặc biệt:**

```text
Link:PublicBaseUrl = localhost   ->  link mở được khi NGƯỜI DEPLOY tự bấm trên máy chủ
                                 ->  CHẾT khi người support bấm từ máy của họ
```

**Nó chạy đúng với đúng người đi kiểm nó.** Không phép kiểm tự động nào bắt được, vì máy chạy
phép kiểm cũng chính là máy đó. Sản phẩm làm được hai việc, và **cả hai đều không thay được
mắt người**: thiếu hẳn cấu hình thì endpoint phát link trả 409 (không đoán), trỏ vào loopback
thì cảnh báo ở stderr lúc khởi động và `--kiem-cau-hinh` gọi tên nó. Cách kiểm thật vẫn là:
phát một link rồi **mở từ máy khác**.

**⚠ Hai chi tiết nhỏ có lý do:**
```text
· CheMatKhau() che mật khẩu trong chuỗi kết nối trước khi in. Lệnh này in ra màn hình và
  người ta SẼ dán kết quả vào chat để hỏi — đó đúng là đường mà sự cố `0e602c3` đi qua.
  Có test riêng, vì hỏng ở đây là rò credential chứ không phải sai định dạng.
· Lệnh trả mã thoát 0 kể cả khi có cảnh báo. Trả khác 0 thì người ta sẽ cắm nó vào CI rồi
  TẮT NÓ ĐI ở đúng ngày nó bắt đầu nói điều khó nghe. Nó là lệnh SOI, không phải lệnh chặn.
```

**⚠ Và nó vừa phơi ra một thứ thật ngay lần chạy đầu:** `kp_dev` đã tích 10 tenant rác từ các
lượt nghiệm thu, và `dev-tenant` thì **0 khoá còn hiệu lực** (tôi thu hồi sau mỗi lượt kiểm).
Không phải lỗi — nhưng đó chính là loại chuyện mà không ai nhìn ra nếu không có lệnh nào chịu
đếm.

---

## `IM-43` · Kênh 1 đóng đủ ba loại tín hiệu — và loại thứ ba KHÔNG cần endpoint nào

**Ngày 2026-09-17, bước 5.** `06` §1 liệt kê ba loại tín hiệu thật; từ 2026-08-25 tới nay
mới có loại thứ nhất.

```text
loại 1  có việc mới ở nguồn        POST /signals/case-observed    (đã có từ 08-25)
loại 2  người dùng đổi trạng thái  POST /signals/case-status      MỚI
loại 3  người dùng hỏi tài liệu    POST /hoi                      KHÔNG endpoint mới (F2-1)
✅ 382/382 test xanh (212 + 19 + 151), trước 374 · 0 warning
✅ kiem_dau_cuoi.py 52 phép kiểm · 0 sai
```

**🎯 Quyết định đáng nhất nằm ở chỗ endpoint mới KHÔNG làm gì, và nó NGƯỢC với trực giác.**
`F1-1` chốt *"cập nhật Case, không làm gì thêm"* — không kích hoạt gom tri thức, không xếp
hàng trả kết quả về. Trực giác nói ngược lại: case đóng rồi thì gom tri thức từ nó. Số đo
nói:

```text
corpus 12 tháng   100% case đã đóng  ->  1,6 mẩu dùng được / case
corpus  4 ngày     22% case đã đóng  ->  2,5 mẩu dùng được / case
```

Case **đã đóng mỏng hơn** case còn mở, vì bước kết luận xảy ra ở nơi khác (phiên remote, cuộc
gọi) và chỉ để lại một dòng *"Done nhé"*. Kích hoạt gom lúc đóng case là build đúng bộ lọc mà
dự án đã đo được là vứt đi phần có giá trị — **lần thứ ba cùng một hình dạng**, sau *"chỉ lấy
case đã đóng"* và *"chỉ lấy phần khách đọc được"*.
→ Có một test giữ riêng điều này (`Doi_trang_thai_KHONG_kich_hoat_gui_ket_qua_ve`), vì nếu
  không thì ai đó sẽ "sửa cho nhất quán" với đường tín hiệu loại một.

**⚠ `daXongONguon` là trường BẮT BUỘC, không suy từ `sourceResolvedAt`.** Cùng luật với
`subTenantCode` của `AR-l`: bỏ trống thì *"bên gửi chưa gửi trạng thái"* và *"việc ở nguồn đã
mở lại"* đều cho ra một ô trống, mà hai điều đó khác hẳn nhau và **về sau không dựng lại được
từ dữ liệu**.

**⚠ Case chưa tồn tại thì NÓI RA, không tự tạo.** Một case dựng từ tín hiệu đổi trạng thái sẽ
không có `Subject` — tức một dòng rỗng nghĩa nằm trong kho gom của Path A. Trả
`khong-thay-case` cho riêng phần tử đó, không hỏng cả lô.

**✅ Và cổng của bước 7 vừa tự chứng minh trên VIỆC THẬT, không phải trên đột biến giả.** Sau
khi thêm endpoint mà chưa cập nhật bộ kiểm, mục `J` của `kiem_dau_cuoi.py` đỏ ngay và gọi
đúng tên:
```text
chưa gọi: POST /signals/case-status
SAI  mọi thao tác trong hợp đồng đều được file này gọi: nhận 1, mong 0
```
Đây là lần đầu một cơ chế chống-lỗi-thời của dự án bắt được một thiếu sót **do chính công
việc kế tiếp tạo ra**, thay vì phải chờ ai đó nhớ ra.

---

## `IM-42` · Hợp đồng tích hợp sinh từ code — và cái cổng canh tôi vừa viết ra là ĐỒ TRANG TRÍ

**Ngày 2026-09-17, bước 7 của lộ trình mức 3.** Trước hôm nay sản phẩm có 17 endpoint cộng
một hợp đồng webhook có chữ ký, mà **bên thứ ba không có gì để đọc ngoài code**.

```text
GET /openapi/v1.json          sinh từ chính các endpoint — không lỗi thời được
docs/19_HOP_DONG_TICH_HOP.md  phần OpenAPI KHÔNG tả được: webhook gửi ra, cách kiểm chữ ký
OpenApiMoTa.Bang              toàn bộ chữ bên ngoài đọc, ở MỘT chỗ, đối chiếu được
kiem_dau_cuoi.py mục J        tự đối chiếu CHÍNH NÓ với hợp đồng
✅ 374/374 test xanh (212 + 19 + 143), trước 363 · 0 warning
✅ chạy THẬT: 47 phép kiểm · 0 sai · 17/17 thao tác trong hợp đồng đều được gọi
```

**🛑 CỔNG CANH TÔI VỪA VIẾT RA LÀ ĐỒ TRANG TRÍ, và test của nó VẪN XANH.**

Ý định: thêm endpoint mà quên viết mô tả thì **không khởi động được** — cùng cơ chế `RlsGuard`
dùng cho bảng thiếu RLS. Viết xong, test xanh, chạy thật cũng xanh.

Rồi làm phép **đột biến**: xoá mô tả của `POST /hoi` rồi chạy. **App khởi động bình thường.**

```text
nguyên nhân   lấy EndpointDataSource từ DI thì lúc gọi nó còn RỖNG. Lệnh Map* của minimal
              API đi vào WebApplication.DataSources, chỉ được nối vào nguồn toàn cục khi
              pipeline dựng xong — tức SAU lệnh gọi này.
hậu quả       cổng duyệt một danh sách rỗng, không bao giờ ném, và TRÔNG HỆT NHƯ ĐANG CANH.
vì sao test không thấy   test gọi thẳng ThieuMoTa(), không đi qua đường nối.
```

⚠ **Đây là kiểu hỏng tệ nhất của một cơ chế canh gác: nó không chỉ mất tác dụng, nó còn lấy
mất cả sự cảnh giác của người đọc.** Ai nhìn thấy dòng `RequireMoTaEndpoint(...)` trong
`Program.cs` sẽ tin rằng chuyện đó đã được canh.

**✅ Sửa xong rồi làm thêm một bước: cho cổng TỰ TỐ CÁO ĐƯỢC KHI NÓ BỊ NGẮT.**
Phép kiểm giờ đi **cả hai chiều** — mô tả thiếu thì ném, và **mô tả THỪA cũng ném**. Danh
sách route rỗng (đúng ca của bug) làm cả 17 mô tả thành thừa → ném ngay. Chiều thứ hai còn
bắt được một thứ thật khác: mô tả cho endpoint đã bị xoá.

**🎯 Và một quyết định về BỘ POSTMAN, ghi ra vì nó là "bớt đi" chứ không phải "thêm vào".**
`kiem_dau_cuoi.py` giờ gọi thật cả 17 endpoint và tự đối chiếu với OpenAPI. Bộ Postman vì thế
**thừa** ở vai trò bản kiểm kê — mà một bản kiểm kê thừa và lỗi thời thì tệ hơn không có, vì
người ta tin vào nó. Đã ghi thẳng vào phần mô tả của collection rằng nó **không còn là nơi
giữ hợp đồng**, chỉ là đồ dùng tay.

**⚠ Ba chỗ cố ý, đề phòng người sau đọc thành sơ suất:**
```text
· /openapi/v1.json KHÔNG đòi khoá. Nó tả HÌNH DẠNG, không chứa dữ liệu khách hàng, và mọi
  endpoint nó liệt kê vẫn trả 401. Giấu tài liệu đi không làm hệ thống an toàn hơn.
· Mô tả gom vào MỘT bảng thay vì rắc .WithSummary() khắp nơi — để đối chiếu được với danh
  sách route thật. Rắc rải rác thì không có gì để so.
· docs/19 viết cho NGƯỜI NGOÀI đọc, nên nó nói thẳng cả những gì sản phẩm KHÔNG hứa:
  không hứa có câu trả lời, không hứa thứ tự gói, không hứa đúng-một-lần.
```

---

## `IM-41` · Phép kiểm nghiệm thu HAI KHÁCH HÀNG — và nó tìm ra BA thứ mà 344 test không thấy

**Ngày 2026-09-16, bước 10 của lộ trình mức 3.** `I2-1` chốt nghiệm thu mức 3 là *"onboard
được khách thứ hai mà không sửa code"*. Bước này dựng cái thước cho câu đó, và làm ngay sau
bước 1-2 chứ không để cuối — theo đúng nguyên tắc *làm trước cái mà làm sau sẽ đắt*: nếu có
rò trong phần hạ tầng vừa dựng thì mọi bước sau xây lên trên nó.

```text
tests/.../HaiKhachHangTests.cs   5 test hai tenant, gọi được cả vòng quét NỀN
scripts/kiem_hai_khach.py        dựng 2 khách từ con số 0, CHỈ bằng CLI + HTTP công khai
--tao-khach-hang                 lệnh mới — trước đó tạo khách phải viết SQL tay
✅ 363/363 test xanh (212 + 19 + 132), trước 344 · 0 warning
✅ đột biến "mọi lượt dùng chung tenant đầu" -> 4/5 test đỏ
✅ chạy THẬT ở chế độ SHARED: 24 phép kiểm · 0 sai
```

**🛑 BA THỨ TÌM RA, và không thứ nào bộ test cũ thấy được:**

```text
1  Bản deploy DEDICATED của khách A ĐANG LÀM VIỆC THAY khách B.
   Vòng quét nền duyệt danh bạ tenant mà không hỏi chế độ deploy. KHÔNG rò dữ liệu —
   gói của B vẫn tới đích của B — nên RlsGuard, policy RLS và mọi test tenant cũ đều
   XANH. Cái sai là PHẠM VI LÀM VIỆC, trái `G13`. -> Đã sửa: PhamViAsync().

2  Payload VIẾT TAY làm endpoint trả 500 thay vì 400.
   Gõ `dieuKien` thay cho `quanSat` -> NullReferenceException ở tận tầng chuyển đổi.
   -> Đã sửa: SopDraftKiemTra ở cửa vào.

3  Tạo khách hàng mới CHỈ làm được bằng SQL tay.
   Không phải "sửa code", nhưng cũng không phải *onboard được*. -> --tao-khach-hang.
```

**🎯 VÀ MỘT PHÂN BIỆT TÔI ĐÃ ĐỌC NHẦM, ghi ra vì nó quyết định chỗ đặt phép kiểm:**
`System.Text.Json` xử lý hai kiểu trường **khác hẳn nhau**, và tôi đã tưởng chúng như nhau.
```text
required property            thiếu -> NÉM JsonException -> ASP.NET đổi thành 400. KÍN SẴN.
tham số POSITIONAL RECORD    thiếu -> IM LẶNG thành null. Không ai ném gì.
```
`SopBranch`, `SopSymptom`, `SopFixStep`, `SopDistribution` đều là positional record — đó mới
là nhóm sinh ra lỗi 500, và là nhóm `SopDraftKiemTra` thật sự canh. Có một test riêng ghim
đúng ranh giới này, để người sau không tưởng bộ kiểm đang giữ cả nhóm thứ nhất.

**🛑 PHÁT HIỆN LỚN NHẤT, VÀ NÓ KHÔNG SỬA ĐƯỢC BẰNG CODE:**

```text
Ở chế độ SHARED, DUYỆT bị từ chối bằng 409 — cổng `AR-l` (`IM-31`), CỐ Ý.
-> Hai khách onboard được, hỏi được, nhưng kho của họ KHÔNG có tri thức nào duyệt được.
-> /hoi trả KHÔNG BIẾT cho mọi câu.
```
Nghĩa là **`I2-1` chỉ đạt được nửa trên**: *onboard được* thì đo xong và đúng; *dùng thật
được* thì còn chặn ở `AR-l`, mà `AR-l` cần `G1-1` — thêm field mã khách sạn vào Jira, việc
của tổ chức. Đây không phải lỗi phát sinh: tài liệu đã nói cổng ấy phải ở nguyên. Cái mới là
**giờ nó thành một con số trong báo cáo nghiệm thu**, thay vì một dòng cảnh báo trong tài liệu.

**⚠ Và chính phép kiểm này suýt XANH VÌ LÝ DO SAI.** Bản đầu kiểm chế độ shared bằng cách gọi
không khoá rồi mong 401 — nhưng một bản DEDICATED có đòi khoá cũng trả 401 y hệt. Lần chạy
đầu vì thế đo nhầm một tiến trình dedicated còn sót trên cổng 5119, rồi báo ba chỗ sai vô
nghĩa. → Đã đổi sang **hỏi thẳng** `mode` từ `/internal/tenant-boundary`. Một phép kiểm xanh
vì lý do sai nguy hiểm hơn một phép kiểm không có.

**⚠ Còn lại:** phép kiểm này đo ONBOARD ĐƯỢC, không đo TRẢ LỜI CÓ ÍCH — thước đó là `M1`/`M2`
(`docs/02`), và nó phụ thuộc người làm support, không phụ thuộc code.

---

## `IM-40` · Đường TRẢ KẾT QUẢ VỀ đã tồn tại — và hai lần bộ test tự phơi ra chỗ nó đang nói dối

**Ngày 2026-09-16, bước 2 của lộ trình mức 3 (`docs/18`).** Trước hôm nay sản phẩm tự mô tả
là *"phần mềm của khách phát tín hiệu, sản phẩm thức tỉnh, xử lý, và **trả kết quả về**"* —
nhưng `grep "HttpClient|webhook|callback"` trong `src/` trả về **0 dòng**. Chỉ có `/hoi` đồng
bộ, tức bên kia phải tự hỏi. Giờ vòng đó khép được.

```text
A  kp.knowledge_link_token   POST /knowledge/{id}/link  ·  GET /xem/{token}      (D5-3)
B  kp.webhook_endpoint       PUT/GET /internal/webhook                            (D1-1)
   kp.webhook_delivery       hàng đợi BỀN + vòng quét nền, ký HMAC-SHA256
✅ 344/344 test xanh (198 + 19 + 127), trước 318 · 0 warning
✅ 6 đột biến -> 6 test đỏ đúng chỗ
✅ chạy THẬT: kiem_dau_cuoi.py 51 phép kiểm · 0 sai (trước 38), thêm mục H và I
✅ và một vòng khép kín THẬT tới một bên nhận Python: tín hiệu -> case -> xếp hàng ->
   vòng quét nền -> định tuyến -> ký -> POST. Gói tới nơi mang đúng maGoi và chữ ký.
```

**🎯 QUYẾT ĐỊNH NẶNG NHẤT: webhook dùng lại `HoiHandler`, không tự truy vấn kho.** Tự truy
vấn thì sẽ có **HAI chỗ** quyết định *"biết hay không biết"*, và hai chỗ đó **sẽ** lệch —
hai cửa KHÔNG BIẾT của `docs/13` đúng là loại luật dễ quên một vế. Lúc lệch thì `/hoi` nói
không biết còn webhook gửi ra hướng dẫn (hoặc ngược lại), và **không ai phát hiện vì cả hai
đều "chạy"**. Bằng chứng nó đúng: lần chạy thật trả `dinh-tuyen-khong-biet` cho một ticket
mới — khớp chính xác điều `docs/13` đã cảnh báo về bản định tuyến TRA SẴN.

**🛑 HAI LẦN BỘ TEST TỰ PHƠI RA CHỖ NÓ ĐANG NÓI DỐI, ghi cả hai vì chúng khác loại:**

```text
1  Test "sửa xong thì link phải ĐÓNG" — tôi tự nghĩ ra, nghe rất hợp lý, và SAI với quyết
   định đã có: V3 gắn cờ NEEDS_REVIEW nhưng KHÔNG rút khỏi /hoi. Hai đường đọc cùng một kho
   mà trả lời khác nhau thì người dùng học được "thử đường kia xem" — thói quen đó vô hiệu
   hoá CẢ HAI cổng. -> Đổi thành: link VẪN mở, và phải MANG CỜ.
2  Ba test webhook đỏ vì cả class dùng chung một tenant: cấu hình đích và hàng đợi là dữ
   liệu "CÓ hay KHÔNG CÓ" của cả tenant, nên test "chưa khai báo đích thì không xếp hàng"
   đọc phải cái đích test trước vừa khai -> xanh/đỏ tuỳ THỨ TỰ CHẠY. Cùng hình dạng lỗi mà
   `IM-31` đã ghi. -> Sửa ở GỐC: mỗi test một khách hàng riêng.
```

⚠ Và **lần thứ ba** bài học *"vật liệu test phải lấy từ dữ liệu, đừng lấy từ trí nhớ"*
(`IM-37`, `IM-38`): test cổng che bản đầu bịa ra `pass 9h3kd0` rồi đỏ — không phải vì cổng
hỏng, mà vì luật `AR-j` chỉ biết bí mật **dạng SỐ** (bốn hình dạng đo trên corpus thật).
→ Đó là **xác nhận độc lập thứ hai** cho rủi ro `AR-j` vốn đã mở, lần này từ chiều ngược
lại: vật liệu bịa, không phải corpus. Không giấu đi bằng cách sửa test cho xanh.

**🛑 BA BẢNG MỚI, VÀ CHÚNG KHÔNG CÙNG MỘT LOẠI — chỗ này dễ đọc gộp nhất:**

```text
knowledge_link_token   MIỄN RLS   phải đọc TRƯỚC khi biết tenant (token quyết định tenant)
webhook_endpoint       CÓ  RLS    lúc đụng tới nó ta ĐÃ biết tenant rồi
webhook_delivery       CÓ  RLS    như trên
```
→ Câu hỏi đúng cho mỗi bảng mới là *"ai đọc nó, và lúc đó đã biết tenant chưa"*, **không**
phải *"bảng trước làm sao thì làm vậy"*.

**🎯 Và vòng quét nền DUYỆT THEO TENANT thay vì đọc một phát cả bảng.** Đọc một phát thì phải
miễn RLS cho hai bảng webhook — mở thêm một lỗ ở đúng nơi `G7` gọi là nền tảng, đổi lấy vài
truy vấn. Duyệt theo tenant giữ ranh giới còn hiệu lực **kể cả trong tiến trình nền**, chỗ
không có request nào để mà kiểm. Giá: một truy vấn cho mỗi khách mỗi chu kỳ — với vài chục
khách thì không đáng kể; tới vài nghìn thì đổi cách, và lúc đó **đổi sang gì cũng được TRỪ
việc miễn RLS**.

**Ba quyết định nhỏ có lý do đo được:**
```text
· Dòng hàng đợi KHÔNG chứa payload  -> lượt thử lại mang câu trả lời MỚI NHẤT. Kho tri thức
                                       đổi liên tục; gửi lại bản đông cứng là gửi thứ đã sai.
· Thất bại KHÔNG xoá dòng           -> `K-B3`. Câu "vì sao hôm ấy khách không nhận được gì"
                                       chỉ trả lời được nếu dòng còn đó kèm LastError.
· Bí mật ký lưu ĐỌC LẠI ĐƯỢC        -> khác ApiKey.SecretHash có chủ đích: khoá API là thứ ta
                                       KIỂM (so hash đủ), bí mật này là thứ ta KÝ BẰNG.
                                       ⚠ Hệ quả: dump database là lộ. Nâng lên mã hoá khi có
                                       secret store thật.
```

**🛑 CÒN LẠI, đừng đọc thành đã xong:**
```text
· Định tuyến vẫn là bảng TRA SẴN -> webhook gửi ra "không biết" cho mọi ticket mới. Vòng ống
  đã chạy; ĐỘ CHÍNH XÁC thì vẫn nằm ở Python (84,1%) và vẫn chờ credit.
· Chưa có giới hạn tần suất, chưa chống replay ở chiều nhận (bên kia phải tự kiểm maGoi).
· Chưa có đường ĐỌC hàng đợi. Hỏng ở đó hôm nay chỉ thấy được bằng cách mở database.
· Bí mật ký chưa xoay vòng được: đổi địa chỉ thì giữ bí mật cũ (cố ý), nhưng chưa có lệnh
  đổi riêng bí mật.
```

---

## `IM-39` · `AR-e` thành CODE — và cái cổng TRÁNH đầu tiên của dự án được gỡ vì đã GIẢI

**Ngày 2026-09-16, lượt cuối.** Bước 1 của lộ trình mức 3 (`docs/18`). Xác thực người gọi
theo từng khách hàng, thay khoá dùng chung.

```text
kp.api_key           Id · TenantId · KeyId · SecretHash · Label · CreatedAt · RevokedAt
Authorization        Bearer kp_<keyId>_<secret>        <- thay X-Tenant-Key VÀ X-Signal-Key
lệnh vận hành        --tao-khoa <tenant> "<nhãn>"  ·  --thu-hoi-khoa <keyId>
✅ 318/318 test xanh (198 + 19 + 101), trước 302 · 0 warning
✅ chạy THẬT: 8 phép kiểm trên app sống · kiem_dau_cuoi.py 38 phép kiểm, 0 sai (trước 32)
```

**🎯 Điều đáng nhất không phải cái bảng mới, mà là chỗ lỗ hổng thật sự nằm.** Khoá dùng
chung cũ trả lời câu *"người gọi có biết mật khẩu không"*. Ranh giới tenant hỏi câu
*"người gọi LÀ AI"*. Chừng nào tenant còn đến từ `X-Tenant-Key` — một header mà chính người
gọi tự điền — thì **mọi lớp xác thực thêm vào đều là trang trí**: biết khoá là khai được
mình là bất kỳ khách nào. Nên `AR-e` không đóng bằng cách thêm một cổng; nó đóng bằng cách
**đổi nguồn của tenant**. Header cũ vì thế bị xoá hẳn, không giữ song song — giữ hai đường
vào là giữ nguyên lỗ hổng và thêm một đường để quên.

**✅ Lần ĐẦU TIÊN một cổng TRÁNH được gỡ vì đã GIẢI.** `Tenancy:AcknowledgeUnauthenticatedTenantHeader`
(`IM-13`) tồn tại để việc deploy một API chưa xác thực thành QUYẾT ĐỊNH thay vì SƠ SUẤT.
Tài liệu luôn gọi đúng tên nó: TRÁNH. Giờ thứ nó tránh đã được giải, nên nó bị xoá.
```text
⚠ Ba cổng TRÁNH khác VẪN CÒN, và KHÔNG được gỡ theo:
   cổng 409 của AR-l · lời khai xacNhanDuLieuThuongMai (AR-n B2b) · luật che một-hình-dạng (AR-n B2a)
```

**🛑 Ba quyết định thiết kế mà người review sẽ hỏi, trả lời sẵn:**

```text
· api_key MIỄN RLS, và lý do KHÁC kp.tenant. tenant miễn vì không thuộc tenant nào;
  api_key thì CÓ thuộc một tenant — nó miễn vì chính nó QUYẾT ĐỊNH tenant, nên không
  thể nằm sau cánh cửa nó đang mở. Bù lại: KHÔNG có DbSet (đọc qua đúng một cửa), và
  không endpoint nào trả dòng của bảng này ra.
· Lưu HASH, không lưu khoá trần — `0e602c3` đã một lần phải gỡ credential thật khỏi 5
  file được git theo dõi. Tự đẻ ra một bảng chứa khoá trần là mời sự cố đó quay lại ở
  chỗ không ai quét.
· SHA-256 chứ không bcrypt/argon2: bí mật là 32 byte từ RandomNumberGenerator, không
  phải mật khẩu người gõ. Làm chậm hàm băm không mua thêm gì khi entropy đã là 2^256.
· Lệnh CLI chứ không endpoint quản trị: một endpoint tạo khoá tự nó cần xác thực, và
  thứ xác thực nó lại là khoá -> vòng tròn. Ai đó sẽ giải bằng một "khoá quản trị" cấu
  hình sẵn, và thế là khoá dùng chung quay lại bằng cửa sau.
```

**🛑 CÁI BẪY THẬT CỦA HÌNH DẠNG KHOÁ, và nó hỏng NGẪU NHIÊN nên test một lần rất dễ xanh
giả.** Phần bí mật mã base64url, mà base64url dùng `-` và `_` thay `+` và `/` — nên **khoảng
một nửa** số khoá sinh ra có dấu gạch dưới BÊN TRONG. Tách bằng *"lấy phần sau dấu `_` cuối
cùng"* cắt cụt đúng những khoá đó.
```text
đột biến: Split('_') rồi lấy phần tử CUỐI   -> 2 test đỏ, đúng hai test viết ra để chặn nó
```
⚠ Và một test cố ý sinh **200 khoá** rồi khẳng định có ít nhất một khoá chứa `_`: sinh một
lượt là ca hỏng chỉ xuất hiện ~50% số lần chạy — tức một bộ test **đỏ ngẫu nhiên**, thứ mà
người ta sẽ chạy lại cho tới khi xanh rồi đi tiếp.

**✅ Một cổng canh làm đúng việc của nó, ghi lại vì nó là bằng chứng cơ chế còn sống:** thêm
`api_key` vào `TenantExemptRelations` làm test `Moi_mien_tru_deu_phai_co_ly_do` **ĐỎ NGAY** —
nó khẳng định danh sách miễn trừ đếm ĐỦ, không chỉ "chứa". Một bảng miễn RLS lọt vào mà không
ai để ý đúng là kiểu nới lỏng im lặng mà cả nhóm cơ chế này sinh ra để chặn.

**⚠ Một cổng khởi động bị gỡ, và lý do KHÔNG phải "bớt phiền":** `RequireSignalEndpointDecision`
từ chối khởi động khi endpoint ghi không có khoá và không ai thừa nhận. Cổng đó cần thiết vì
mặc định lúc ấy là **MỞ** — quên cấu hình là endpoint ghi phơi ra. Với `AR-e`, mặc định đã là
**ĐÓNG**: không cấu hình gì thì không khoá nào hợp lệ và mọi endpoint trả 401. Không còn trạng
thái nguy hiểm nào để chặn lúc khởi động. Test cũ được thay bằng test kiểm chính cái mặc định
đóng đó.

**⚠ Đường FIXTURE vẫn sống — kiểm trước, không phải hy vọng.** `IM-36` đã một lần giết đường
`load_fixture.py` bằng một cổng mới, và đó là đường DUY NHẤT dựng lại dữ liệu trên máy không
có credential Jira. Lần này ba script đọc `KP_API_KEY` và **bỏ trống vẫn chạy** trên máy dev
(cờ thừa nhận). Mục F của `kiem_dau_cuoi.py` xanh chính là bằng chứng đường đó còn sống.

**🛑 CÒN LẠI, đừng đọc thành đã xong:**
```text
· Khoá xác thực HỆ THỐNG gọi vào. NGƯỜI đọc vẫn chưa có gì xác thực — D5-3 (link token
  có hạn) và E3-1 (chỉ người có quyền duyệt) đều đang đợi thứ đó. Xem docs/18.
· Chưa có chống replay, chưa có giới hạn tần suất. C1-1 chọn "rẻ nhất mà giải đúng vấn
  đề", và hai thứ đó không nằm trong vấn đề nó giải.
· Chưa có đường ĐỌC danh sách khoá của một khách. Cố ý: chưa ai cần, và mỗi đường đọc
  thêm là một chỗ dòng của bảng miễn-RLS này có thể lọt ra ngoài.
```

---

## `IM-38` · 8/13 endpoint chưa từng được gọi bởi thứ gì ngoài bộ test — và phép kiểm mới BẮT ĐƯỢC cả hai lỗi của hai ngày trước

**Ngày 2026-09-16, lượt cuối.** Ba ngày qua có ba lỗi thật, và **hai trong ba nằm đúng chỗ
`dotnet test` không nhìn thấy**. Đây không phải trùng hợp, nên lần này đo thẳng khoảng trống
đó thay vì sửa tiếp từng lỗi:

```text
Sản phẩm có           13 endpoint
Bộ Postman phủ         5   health · health/ready · tenant-boundary · case-observed · case-evidence
CHƯA TỪNG được gọi     8   sop-draft · approve · hoi · sua · GET knowledge/{id}
ngoài bộ test C#           GET cases/{k}/evidence · evidence/rut-lai · sub-tenant-coverage
```

Bộ Postman đứng nguyên từ **2026-08-30** — trước cả vòng tri thức (`IM-31`, ngày 13). Nó
không "hơi lỗi thời"; nó thiếu **hơn một nửa sản phẩm**.

---

### Vì sao không phải lỗi của bộ test, và vì sao thêm test cũng không chữa được

Bộ test **tự dựng payload của chính nó**. Đó là điều đúng đắn cho việc nó làm (kiểm LUẬT, kể
cả những nhánh không diễn đạt được qua HTTP), nhưng nó cũng nghĩa là: mọi **bên gửi THẬT** đều
nằm ngoài tầm nhìn của trình biên dịch — hai script Python, một bộ Postman, và về sau là phần
mềm của khách.

Hôm nay có một minh chứng sạch cho cả hai chiều: siết `Approve()` thêm một tham số bắt buộc
thì trình biên dịch bắt **27 chỗ gọi trong C#** ngay lập tức — và **không bắt được chỗ nào**
trong số các bên gửi kia.

---

### `scripts/kiem_dau_cuoi.py` — gọi thật cả 13 endpoint, chỉ dùng thư viện chuẩn

```text
A  Sức khoẻ          health · health/ready · tenant-boundary
B  Đường nạp         case-observed (idempotent) · case-evidence (+ cổng AR-l, che AR-n)
C  Đường đọc         cases/{k}/evidence · internal/sub-tenant-coverage
D  AR-m              evidence/rut-lai  (200 · 409 lần hai · 404 · 400 thiếu người)
E  Vòng tri thức     sop-draft · hoi · approve (400 AR-n · 400 AR-a · 200) · knowledge/{id} · sua
F  Bên gửi THẬT      chạy chính `load_fixture.py`, kiểm mã thoát
```

⚠ **Vì sao KHÔNG dùng newman:** bộ Postman cần `newman`, thứ **không có trên mọi máy** (đã đo
2026-09-16: máy này không có, `npx --no-install` từ chối cài). Một phép kiểm chỉ chạy được
trên một số máy là một phép kiểm sẽ bị bỏ qua. File này chỉ dùng thư viện chuẩn của Python,
đúng quy ước sẵn có của repo (`load_fixture.py`, `check_corpus.py`, `quet_credential_repo.py`).

🛑 **MỤC `F` PHẢI TỒN TẠI RIÊNG, và đây là chỗ dễ làm ẩu nhất.** Mục `A`-`E` dựng payload bằng
CHÍNH file này, nên chúng **luôn khai đủ trường mới** — và sẽ xanh ngay cả khi mọi bên gửi
khác của repo đã chết. Đó đúng là chuyện đã xảy ra: ngày 2026-09-16 `AR-l` thêm một trường bắt
buộc, `load_fixture.py` nhận **400 cả lô**, mà 302 test xanh và mọi endpoint xanh. Cách duy
nhất bắt được là **chạy chính nó**.

---

### 🎯 Đã CHỨNG MINH phép kiểm có răng — tái hiện đúng hai lỗi lịch sử

Không tuyên bố suông "nó sẽ bắt được"; đã cắm lại đúng hai lỗi rồi chạy:

```text
ĐỘT BIẾN kiểu IM-36   bỏ subTenantCode khỏi load_fixture.py (giả lập bên gửi lỗi thời)
                      -> mục F ĐỎ: "load_fixture.py chạy được (mã thoát 0): nhận 1, mong 0"

ĐỘT BIẾN kiểu IM-37   quay LaRong về IsNullOrWhiteSpace trong ThirdPartyRedactor
                      -> mục B ĐỎ: "AR-n che ĐÚNG 3 ... nhận 4, mong 3"
```

⚠ **Và phép thử thứ hai chỉ bắt được vì payload mang HÌNH DẠNG THẬT.** Bản đầu của file này
viết thẻ rỗng bằng xuống dòng thật, nên nó đếm ra 3 ở **cả bản hỏng lẫn bản đúng** — tức nó sẽ
xanh trên chính cái bug nó được viết ra để chặn. Đã sửa payload sang **chuỗi thoát nguyên
văn** như corpus thật. Cùng một bài học lại xuất hiện ở ngay cái công cụ sinh ra để chống nó:
*vật liệu phải lấy từ dữ liệu, đừng lấy từ trí nhớ của người viết.*

---

### Trạng thái verify

```text
✅ 32 phép kiểm · 0 sai · mã thoát 0, chạy thật trên app + kp_dev
✅ 302/302 test C# xanh · 0 warning  (không đụng code sản phẩm ở lượt này)
✅ 2 phép đột biến ra đỏ, mỗi phép tái hiện một lỗi thật của hai ngày trước
✅ quet_credential_repo.py sạch
```

⚠ **Giới hạn, ghi ra để không ai đọc quá:**

```text
· Nó KHÔNG thay bộ test. Bộ test kiểm LUẬT; file này kiểm HỢP ĐỒNG còn gọi được —
  và đó là thứ đã hỏng hai lần trong khi mọi test vẫn xanh.
· Nó GHI dữ liệu thật vào kho đang trỏ tới (tiền tố `kiem-dau-cuoi:` để lọc ra được).
  Dùng cho kho dev/CI, không chạy vào kho của khách.
· Bộ Postman VẪN chưa được cập nhật. File này không thay nó — nó chỉ làm cho việc
  Postman lỗi thời thôi im lặng.
```

---

## `IM-37` · `AR-n` — hai loại dữ liệu, HAI cơ chế khác nhau; và một lỗi chỉ CHẠY THẬT mới bắt được

**Ngày 2026-09-16.** `AR-n` là quyết định duy nhất của `docs/14` có **hai nhánh cố ý khác
nhau**, và lý do đã ghi sẵn trong chính nó: hai loại dữ liệu khác nhau về BẢN CHẤT — một cái
**có hình dạng**, một cái **không**. Một cơ chế chung sẽ hoặc bỏ sót loại thứ hai, hoặc bắt
người duyệt xem cả những mẩu mà luật thừa sức xử lý.

```text
B2a  BÊN THỨ BA   có hình dạng  -> LUẬT, che ở khâu NẠP        ThirdPartyRedactor
B2b  THƯƠNG MẠI   không hình dạng -> NGƯỜI, cổng ở khâu DUYỆT  xacNhanDuLieuThuongMai
```

---

### `B2a` — che theo CẤU TRÚC, không theo từ khoá

`ES-346481#comment-802977` chứa payload hoá đơn với khối `<NMua>`: tên một **đơn vị sự nghiệp
nhà nước**, mã số thuế, địa chỉ đầy đủ. Không phải dữ liệu ezCloud, không phải của khách sạn —
là dữ liệu **khách CỦA khách sạn**, một pháp nhân chưa từng được hỏi. `AR-l` chốt ranh giới
HAI cấp; đây là **cấp thứ ba** nó chưa mô tả.

🛑 **KHÔNG bắt theo từ khoá, và đây là chỗ đã có số để cãi.** Chuỗi `MST` xuất hiện trần ở
**10/345** mẩu fixture, phần lớn là **MST của chính khách sạn** — một định danh CÔNG KHAI mà
`make_fixture.py` **cố ý giữ lại** (ghi rõ trong code: *"định danh công khai"*). Một luật bắt
theo chữ "MST" sẽ che mất đúng dữ liệu cần giữ. Luật ở đây chỉ nhìn **vị trí trong cây XML**.
→ Cùng bài học `AR-j`: luật theo từ khoá cùng dòng đo được chỉ bắt **11%**.

🛑 **CHE ĐỊNH DANH, GIỮ SỐ TIỀN — quyết định có giá, không phải cẩu thả.** Các dòng hàng
(`<DSHHDVu>`) và tổng tiền (`<TgTTTBSo>`) nằm NGOÀI `<NMua>` và **không bị đụng**. Lý do đo
được: nhóm SOP dày nhất trong 19 nhóm là *"Lệch cấu hình tiền/thuế"* (8 case, 63 mẩu) và nó
chẩn đoán bằng cách **mở cùng một hoá đơn ở ba nơi rồi so từng cột tiền**. Che số tiền cho
"an toàn" là vứt đúng bằng chứng của nhóm dày nhất — sẽ là lần thứ **BA** một bộ lọc nghe rất
hợp lý vứt đúng phần có giá trị, sau *"chỉ lấy case đã đóng"* và *"chỉ lấy phần khách đọc
được"*.
→ Bỏ định danh đi thì phần còn lại là một **hoá đơn vô danh**: vẫn tra được mã lỗi, vẫn so
  được cột tiền, mà không còn pháp nhân nào bị nêu tên.

⚠ **Thẻ RỖNG KHÔNG bị che.** Che một thẻ vốn trống là biến *"không có"* thành *"có nhưng bị
giấu"* — và chính sự rỗng đó là thứ **nhóm 2** (*NCC từ chối payload*) cần để biết nhà cung
cấp từ chối vì **trường nào THIẾU**.

🛑 **ĐỘ PHỦ: n = 1.** Trên **873 mẩu thật** (345 fixture + 528 corpus 12 tháng), `<NMua>` xuất
hiện ở **đúng MỘT mẩu**. Đủ để chứng minh loại dữ liệu này TỒN TẠI — `AR-n` được chốt trên
chính nó — nhưng **không đủ để nói luật phủ được bao nhiêu**. Hình dạng thứ hai sẽ tới, và nó
sẽ không mang thẻ `NMua`.
→ Nên **đừng đọc `thirdPartyRedacted: 0` thành "mẩu này sạch"**. Nó chỉ nghĩa là luật không
  thấy gì.

---

### 🛑 LỖI CHỈ CHẠY THẬT MỚI BẮT ĐƯỢC — và nó là bài học của cả repo, lặp lại lần nữa

Bản đầu dùng `string.IsNullOrWhiteSpace` để nhận ra thẻ rỗng. **Mọi test đều xanh.** Nạp
`kp_dev` xong mở database ra xem thì thấy:

```text
<SDThoai>[ĐÃ CHE: bên thứ ba]</SDThoai>     <- thẻ này VỐN TRỐNG
<DCTDTu>[ĐÃ CHE: bên thứ ba]</DCTDTu>       <- cũng vốn trống
<HVTNMHang>[ĐÃ CHE: bên thứ ba]</HVTNMHang> <- cũng vốn trống
```

**Nguyên nhân:** bản xuất Jira mang xuống dòng ở dạng **chuỗi thoát nguyên văn** — một thẻ
rỗng trong corpus chứa BỐN ký tự (gạch chéo, chữ r, gạch chéo, chữ n), không phải hai ký tự
xuống dòng thật. `IsNullOrWhiteSpace` thấy có gạch chéo và chữ nên nói *"không rỗng"*, rồi thẻ
trống bị dán dấu che — **đúng sự nhầm lẫn mà guard đó sinh ra để chặn, chỉ khác là nó tự gây ra.**

⚠ **Vì sao test không thấy:** test viết thẻ rỗng bằng **xuống dòng thật**, vì tay người viết
test luôn gõ xuống dòng thật. Đây là **cùng một hình dạng lỗi với `IM-24`** (mốc `+07:00` làm
endpoint trả 500, mà 103 test cũ không thấy vì tay người viết test luôn viết UTC), và là câu
mà README đã ghi từ 2026-09-04: *"một bộ test tự cấp vật liệu cho mình chỉ kiểm được những
hình dạng mà người viết nghĩ ra."*

→ Đã sửa bằng `LaRong()` — gỡ chuỗi thoát rồi mới hỏi rỗng — và **đã chứng minh phép thử mới
  bắt được lỗi cũ**: quay `LaRong` về `IsNullOrWhiteSpace` thì
  `The_rong_kieu_CORPUS_THAT_khong_bi_che` **ĐỎ**.

---

### `B2b` — cổng ở khâu DUYỆT, và nó là một LỜI KHAI chứ không phải phép kiểm

✅ **Chủ dự án chốt 2026-09-16: cổng đặt ở khâu DUYỆT SOP.** Lý do: *"vào kho"* trong `AR-n` là
vào **kho TRI THỨC**, không phải vào bảng evidence. Duyệt **LÀ xuất bản** — đó là lúc một mẩu
thôi nằm trong dữ liệu thô và bắt đầu hiện ra cho người khác. Gác ở tầng mẩu thì bắt người soát
cả những mẩu không bao giờ được dùng tới.

```text
POST /knowledge/{id}/approve   thiếu xacNhanDuLieuThuongMai  -> 400
                               khai false                    -> 400
                               khai true                     -> 200, LƯU lời khai
```

🛑 **ĐÂY LÀ LỜI KHAI, KHÔNG PHẢI PHÉP KIỂM — ghi thẳng ra để không ai đọc ô `true` thành "đã
kiểm".** Hệ thống không đối chiếu được: dữ liệu thương mại (giá hợp đồng theo năm, hạn hợp
đồng, ý định ngừng dùng — `ES-346608#comment-803265`) **không có hình dạng nào** để bắt bằng
regex; chính `AR-n` ghi điều đó, và đó là lý do nhánh này chọn NGƯỜI.

⚠ **Và hôm nay hệ thống còn không biết mẩu nguồn là những mẩu nào:** `assertion_evidence` có
**0 dòng** (đo trên `kp_dev`), vì đường nạp bản nháp SOP chưa nối assertion với evidence. Nên
chưa có cách nào đối chiếu lời khai — **kể cả muốn**. Khi mắt xích đó có, chỗ này mới nâng
được từ *khai* lên *đối chiếu được*.

⚠ **Ba trạng thái, đừng gộp:** `NULL` = duyệt TRƯỚC luật này · `true` = có người khai ·
`false` thì **không bao giờ nằm trong kho** (không đi qua được cổng). Đo trên `kp_dev` sau khi
chạy thật: **1 `true` · 7 `NULL`**. Backfill `true` cho 7 bản ghi cũ sẽ là bịa ra một lời khai
mà không ai từng nói — đúng thứ `AP3` cấm.

---

### ⚠ HỆ QUẢ CỦA `K-B3`: cổng chỉ áp cho lượt nạp MỚI

Sau khi build xong, mẩu `<NMua>` **đã nằm sẵn** trong `kp_dev` từ lượt nạp trước vẫn **chưa bị
che** — đã kiểm bằng SQL, `da_che = f`. `K-B3` cấm ghi đè quá khứ, nên gửi lại không sửa được
gì. Phải **xoá evidence rồi nạp lại** mới có kho nhất quán. Đây là lần thứ HAI cùng một hệ quả
trong hai ngày (lần đầu: mã khách sạn, `IM-36`).
→ **Luật rút ra: mỗi lần siết một luật ở cửa vào thì phải nạp lại kho, không chỉ chạy test.**

---

### Trạng thái verify

```text
✅ 302/302 xanh (184 domain + 19 hạ tầng + 99 API) · trước 282 · 0 warning
✅ migration: thêm 1 cột `commercial_data_confirmed` (nullable), đã apply lên kp_dev
✅ CHẠY THẬT trên app + kp_dev:
      khối <NMua>  -> Ten/MST/DChi bị che, ba thẻ rỗng để yên, <STKNHang /> không đụng
      số tiền      -> <TgTTTBSo> CÒN NGUYÊN
      duyệt        -> thiếu khai 400 · khai false 400 · khai true 200
      kho          -> 1 true · 7 NULL (duyệt trước luật)
✅ bộ test biết ĐỎ — 4 phép đột biến:
      thẻ rỗng cũng bị che                 -> 2 test đỏ
      che TẤT CẢ, không chỉ khối bên mua   -> 6 domain + 3 API đỏ
      che chồng lên dấu che cũ             -> 1 test đỏ
      bỏ cổng xác nhận ở domain            -> 1 test đỏ (API vẫn xanh: endpoint tự kiểm
                                              trước — cùng khuôn hai tầng của AR-m)
✅ quet_credential_repo.py sạch
```

⚠ **CHƯA LÀM, đừng đọc thành đã xong:**

```text
· Luật B2a chỉ biết MỘT hình dạng (<NMua>), đo trên n=1. Không có cơ sở nào để nói nó
  phủ được loại dữ liệu bên thứ ba nói chung.
· B2b là LỜI KHAI. Không chặn được người tích cứ tích — nó chỉ làm việc đó thành một
  hành động có tên và có dấu vết.
· Bộ Postman vẫn chưa có request cho subTenantCode / rut-lai / coverage / xác nhận
  thương mại. Newman không có trên máy này.
· AR-p và AR-e/AR-i vẫn chưa code (AR-q không cần code).
```

---

## `IM-36` · Cổng `AR-l` giết đường fixture — đường DUY NHẤT chạy được trên máy không có credential Jira

**Ngày 2026-09-16, ngay sau `IM-35`.** Tìm ra bằng cách **chạy**, không bằng đọc code.

```text
dry-run-*.json   bản xuất Jira thật (có secret, bị .gitignore)
  -> make_fixture.py     thay secret bằng giá trị GIẢ GIỮ NGUYÊN HÌNH DẠNG
  -> fixture-*.json      345 mẩu, sinh TRƯỚC cả AR-k lẫn AR-l
  -> load_fixture.py     -> HTTP 400 CHO CẢ LÔ
```

🛑 **Vì sao đây không phải một bất tiện nhỏ.** `load_fixture.py` là đường **duy nhất** dựng
lại được dữ liệu trên một máy **không có credential Jira** — chính docstring của nó nói thế
(*"không có bí mật nào đi qua đây, nên nó chạy được trên máy bất kỳ ai clone repo"*), và
`docs/10` dặn dùng nó khi chuyển máy. Cổng `AR-l` làm **đúng** việc của nó (fail closed, nói
rõ vì sao), nhưng thứ nó chặn lại là con đường bàn giao.

⚠ **Và nó sẽ không bao giờ đỏ trong `dotnet test`:** bộ test API tự dựng payload của chính
nó, nên nó không biết gì về `fixture-*.json`. Một cửa vào siết chặt hơn thì mọi **bên gửi**
phải sửa theo — và bên gửi ở đây là hai script Python nằm ngoài tầm nhìn của trình biên dịch.

---

### Sửa: gọi lại luật, KHÔNG chép luật

`make_fixture.py` đang **chép nguyên si** `machineReadability` từ file dry-run, nên fixture
mang `High` ở 345/345 — đúng cái hằng số mà `AR-k` đã chốt là bug. Nay nó `import` thẳng
`doan_machine_readability` và `MA_KHACH_SAN` từ script export.

```text
                     TRƯỚC          SAU
giá trị phân biệt    1              3     (Medium 157 · High 101 · Low 87)
subTenantCode        không có       "khong-xac-dinh" × 345, khai TƯỜNG MINH
load_fixture.py      400 cả lô      345 mẩu vào kho
```

Nhãn được tính **trên nội dung ĐÃ CHE**, không trên bản gốc: mẩu đi vào kho là bản đã che,
nên nhãn phải mô tả đúng bản đó. Giá trị giả giữ nguyên hình dạng nên hai bên gần như luôn
trùng — nhưng *"gần như"* không phải lý do để tính sai chỗ.

---

### 🛑 Một lý do CŨ bị lật ngược, và không được xoá lặng lẽ

Bản trước ghi thẳng trong code: *"nhãn High: 345/345 (giữ nguyên — **đây LÀ bug AR-k, fixture
phải tái hiện được nó**)"*. Câu đó **đúng** khi bug chưa có bản vá — giữ một mẫu vật để luật
quét có cái mà bắt. Từ khi `AR-k` thành luật (2026-09-15) thì nó **lật ngược**: fixture mang
nhãn hằng số nghĩa là fixture mô tả một đường nạp KHÔNG CÒN TỒN TẠI, và mọi phép kiểm chạy
trên nó vẫn xanh.

→ **Mẫu vật không biến mất, nó chuyển chỗ** — và đã kiểm cả hai chiều:

```text
check_corpus.py  fixture-*.json   -> 3 giá trị phân biệt, KHÔNG cảnh báo
check_corpus.py  dry-run-*.json   -> 1 giá trị phân biệt, CÒN NGUYÊN cảnh báo AR-k
```

`dry-run-*.json` là bản xuất thô, không sinh lại — đó mới là chỗ đúng để giữ một mẫu vật
lịch sử, chứ không phải ở file mà người ta nạp thẳng vào DB.

---

### 🛑 Vì sao KHÔNG bịa mã khách sạn giả, dù cả file này xây trên nguyên tắc "giá trị giả"

`make_fixture.py` tồn tại để **thay giá trị thật bằng giá trị giả cùng hình dạng**. Theo phản
xạ đó thì nên rắc vài mã khách sạn giả vào fixture cho phép đếm có cái mà đếm. **Không.**
Khác biệt nằm ở chỗ: giá trị giả kia thay cho một **bí mật ĐÃ TỒN TẠI** ở nguồn; còn mã khách
sạn thì **KHÔNG TỒN TẠI** ở nguồn (`customfield_12710` = `-1.0` ở 32/32). Bịa ra vài mã sẽ làm
`datDieuKienMoLai` thành `true` trên kho dev vì một lý do **không có thật** — tức tự dựng lại
đúng thứ mà ô `canTro` vừa sinh ra để chặn, chỉ một ngày sau khi sinh ra nó.

✅ **Và đây là phần trả công:** trên corpus thật, phép đếm nay nói đúng.

```text
TRƯỚC (kp_dev có 2 dòng tạo lúc thử)   soMaPhanBiet 2  ·  datDieuKienMoLai TRUE   <- sai
SAU  (345 mẩu fixture thật)            soMaPhanBiet 0  ·  datDieuKienMoLai FALSE  ·  canTro nói rõ
```

---

### Nạp lại kho: xoá 145 mẩu cũ

Chủ dự án chốt **xoá evidence cũ rồi nạp lại sạch** — `K-B3` cấm ghi đè nên gửi chồng lên
không đổi được gì (`subTenantNotApplied`). Kiểm ràng buộc trước khi xoá, và nó sạch:

```text
assertion_evidence  0 dòng   -> KHÔNG có gì trỏ vào evidence, xoá an toàn
delete              145 mẩu  (137 nạp trước luật + 8 dòng tạo lúc thử IM-35)
load_fixture.py     345 mẩu vào kho, exit 0
knowledge_record 8 · assertion 41 · canonical_case 216  -> giữ nguyên, không đụng
```

---

### ⚠ HAI CẢNH BÁO CỦA `make_fixture.py` KHÔNG PHẢI DO LẦN SỬA NÀY

Script thoát **mã 1** và in `🛑 CÒN 36 CHỖ NGHI LÀ CREDENTIAL SAU KHI THAY` cùng
`MẤT <-- SAI 80771`. Đã **chứng minh là có sẵn**, không suy đoán: `git stash` phần sửa rồi
chạy lại bản HEAD → **đúng 36 chỗ, đúng dòng `MẤT 80771`, đúng mã thoát 1**.

Nguyên nhân đã nằm sẵn trong chính script: danh sách `THAY_THE` được soát **bằng tay** trên
corpus 2026-09-04, còn corpus đang chạy là bản 12 tháng lớn hơn. Đây là món nợ RIÊNG, và nó
vẫn mở — ghi ra đây để không ai đọc mã thoát 1 thành "lần sửa này làm hỏng".

---

### Trạng thái verify

```text
✅ 282/282 xanh (170 + 19 + 93) · 0 warning  (code C# KHÔNG đổi ở lượt này)
✅ make_fixture.py sinh lại: 1 -> 3 giá trị phân biệt, subTenantCode khai tường minh
✅ load_fixture.py CHẠY LẠI ĐƯỢC: 345 mẩu, exit 0 (trước: 400 cả lô)
✅ check_corpus.py: fixture sạch cảnh báo · dry-run VẪN nổ cảnh báo AR-k
✅ coverage trên corpus thật: soMaPhanBiet 0 · datDieuKienMoLai FALSE · canTro nói rõ
✅ quet_credential_repo.py sạch
⚠ 36 chỗ nghi credential + "MẤT 80771": CÓ SẴN TỪ TRƯỚC, đã chứng minh bằng bản HEAD
```

---

## `IM-35` · `AR-l` + `AR-m` vào chung MỘT migration — và phép đếm "mở lại được chưa" suýt nói dối ngay lần chạy đầu

**Ngày 2026-09-16.** Hai quyết định còn lại nặng nhất của `docs/14` đã thành code. Gộp chung
một migration vì cả hai đổi **cùng một bảng** và **cùng hợp đồng đường nạp** — tách ra là hai
lượt migration trên `evidence_item` trong cùng một tuần, mà mỗi lượt đều phải được đối chiếu
lại với 137 mẩu đang nằm sẵn trong `kp_dev`.

```text
evidence_item  + SubTenantCode      varchar(64)     AR-l
               + RetractedAt        timestamptz     AR-m
               + RetractedBy        varchar(256)    AR-m
               + RetractedReason    varchar(1024)   AR-m
               + index (TenantId, SubTenantCode)    để hỏi điều kiện mở lại không quét bảng
```

---

### 🛑 Một mâu thuẫn phải đưa ra trước khi code: nguồn được chọn KHÔNG tồn tại

`C3-1` chốt *"thêm một trường BẮT BUỘC ở đường nạp, bên gửi khai mã khách sạn"*, với lý do đó
là **nguồn duy nhất chắc chắn cho ≥2 giá trị phân biệt**. Nhưng bên gửi duy nhất đang tồn tại
là `export_jira_to_channel1.py`, và nó không có gì để khai — đó là điều chính `AR-l` đã đo:

```text
Mã khách sạn   customfield_12710  ->  -1.0 ở 32/32 case      HẰNG SỐ
Tên khách sạn  customfield_13326  ->  25/32 rỗng, còn lại là TÊN TEAM
suy từ tiêu đề                    ->  BỊ CẤM, đã đo: 8/32 có mã, và lệch với thân bài
```

Tức làm đúng chữ của quyết định thì hoặc **chặn đường nạp duy nhất đang chạy** (kể cả lượt
export lại mà `AR-k` đang cần), hoặc **khai một hằng số** — đúng hình dạng `-1.0` mà chính mục
này sinh ra để chặn.

✅ **Chủ dự án chọn nhánh thứ ba, 2026-09-16:** bắt buộc khai, `khong-xac-dinh` là một lời khai
**hợp lệ và tường minh**, và **đường nạp ĐẾM SỐ GIÁ TRỊ PHÂN BIỆT rồi nói thẳng ra** khi cả lô
chỉ có một. Nghĩa là bài học của `-1.0` không còn nằm trong tài liệu nữa — nó chạy.

---

### `AR-l` — ba trạng thái, và đừng gộp chúng

```text
NULL                     mẩu nạp TRƯỚC luật này. Chưa ai từng được hỏi.
khong-xac-dinh           bên gửi ĐÃ được hỏi và trả lời "chưa biết".
khong-thuoc-khach-san    khẳng định KHÔNG thuộc khách sạn nào — việc nội bộ ezCloud
                         (ES-346594), ezGolf (ES-346764), chuỗi nhiều cơ sở (ES-346615).
mã thật                  giữ NGUYÊN VĂN (v0.2 R5). Phép đếm thì so không phân biệt hoa thường.
```

🛑 **Bỏ trống là 400, KHÔNG âm thầm về `khong-xac-dinh`.** Đây là chỗ dễ bị "sửa cho tiện" nhất
về sau, nên ghi rõ: *"bên gửi đã cân nhắc và chưa biết"* khác *"bên gửi chưa đọc tài liệu"*, và
nếu cửa vào tự điền hộ thì **về sau không có cột nào dựng lại được khác biệt đó**.

⚠ **Cột NULL được ở DB dù hợp đồng BẮT BUỘC khai — một ngoại lệ có lý do.** Dự án ưu tiên chặn
lúc biên dịch, nên `required` mới là phản xạ đúng. Nhưng `required` ở đây sẽ nói dối về 137 mẩu
đã nằm trong `kp_dev`: cách duy nhất để chúng thoả là **bịa ra một giá trị**. Tính bắt buộc vì
thế được thực thi ở **cửa vào**, và `NULL` giữ đúng nghĩa "chưa ai từng được hỏi".

🛑 **Hai giá trị dành riêng KHÔNG tính vào điều kiện mở lại.** Nếu tính thì chỉ cần đổi từ
`khong-xac-dinh` sang `khong-thuoc-khach-san` là "đạt ≥2 giá trị phân biệt", trong khi không
khách sạn nào được phân biệt với khách sạn nào — ta sẽ tự dựng lại cái bẫy `-1.0` bằng tay mình.

---

### 🛑 Một ca BÁO XANH TRONG KHI HỎNG đã chặn được TRƯỚC khi nó xảy ra

Kịch bản: export lại toàn bộ corpus kèm mã khách sạn, nhận `200 OK`, thấy `created: 0` — và tin
rằng mã đã vào kho. Thực tế `K-B3` cấm ghi đè quá khứ nên **không mẩu nào đổi**, và `created: 0`
trông y hệt một lượt bình thường (chính `IM-15` đã dạy `created 0` không phải lỗi).

→ Không ghi đè là ĐÚNG. **Im lặng mới là sai.** Response nay mang `subTenantNotApplied` theo
từng mẩu, và cảnh báo của lô nói thẳng rằng muốn mã vào những mẩu đó thì phải **nạp lại vào kho
sạch**, chứ không phải gửi chồng lên.

---

### 🛑 Và phép đếm "mở lại được chưa" suýt nói dối NGAY LẦN CHẠY ĐẦU

`AR-l` cố ý phát biểu điều kiện mở lại dạng **chạy được**: *"khi có MỘT nguồn cho ra ≥2 giá trị
PHÂN BIỆT"*. Đã dựng `GET /internal/sub-tenant-coverage` để trả lời đúng câu đó. Chạy thật trên
`kp_dev` ra:

```text
mã PHÂN BIỆT        2   ['12027', '17468']   ->  datDieuKienMoLai: TRUE
chưa khai (NULL)  137 / 141                  ->  97% kho không có gì để thực thi ranh giới
```

**Cả hai con số đều đúng, và đặt cạnh nhau thì cái "TRUE" là một lời nói dối.** Hai mã kia đến
từ đúng hai dòng vừa nạp lúc thử. Ai đọc `true` rồi gỡ cổng 409 là mở ranh giới cho một kho mà
97% số mẩu không biết thuộc khách sạn nào.

→ **KHÔNG sửa ngưỡng của quyết định** — đổi ngưỡng một quyết định đã chốt là việc của chủ dự án.
  Thay vào đó thêm ô `canTro`: `datDieuKienMoLai` trả lời đúng câu chữ của `AR-l` và không hơn,
  còn `canTro` nói ra phần còn thiếu. Có test giữ nó khỏi biến mất.
⚠ **Đây là lần thứ ba cùng một hình dạng lỗi trong dự án** (sau `machineReadability = High`
  128/128 và `Mã khách sạn = -1.0` 32/32), và lần này **do chính ta sắp tạo ra**. Khác biệt duy
  nhất: lần này có một phép đếm nhìn thấy nó.

---

### `AR-m` — dấu rút lại, và vì sao khoá nguồn nằm trong THÂN

Ba cột + `EvidenceItem.RutLai(nguoiDanhDau, lyDo)` là **đường duy nhất** đặt chúng.
`CaseEvidenceSignal` KHÔNG mang ba trường này, nên connector không có cách nào tự suy từ câu chữ
rồi ghi vào — đó là toàn bộ nội dung của `B1-1`, và có test gửi kèm `retractedAt`/`retractedBy`
qua đường nạp để chứng minh chúng bị bỏ qua.

⚠ **`RetractedBy` KHÔNG có trong đề xuất gốc** (`AR-m` chỉ nêu `RetractedAt` + `RetractedReason`).
Thêm vào vì đúng bài học của `M2` phần (c): một dấu vết không có tác giả thì không phân biệt được
với một dấu vết do script đặt — mà *"script không được đặt"* chính là nội dung của quyết định.

🛑 **`POST /evidence/rut-lai` nhận khoá nguồn trong THÂN, không trong đường dẫn** — học thẳng từ
`AR-g` trong cùng tuần: khoá chứa `/` không đọc ra được qua đường dẫn (ASP.NET giải mã `%2F`
TRƯỚC khi định tuyến), và khoá của evidence còn chứa `#` (`jira:ES-1234#comment-7`). Không đi
ngược `C4-1`: quyết định đó chọn hình dạng cho đường ĐỌC của case.

🛑 **HAI DANH SÁCH, HAI KIỂU — không phải một danh sách kèm cờ `daRutLai`.** Một cờ boolean thoả
được cả `AR-m` lẫn `G6` trên giấy, nhưng nó đặt trách nhiệm lọc lên MỌI nơi gọi về sau, và chỗ
quên lọc đầu tiên sẽ **không đỏ test nào**. Hai KIỂU khác nhau thì quên lọc là không biên dịch
được. Và `MauEvidenceDaRutLai` **không mang `NoiDung`**: giữ dấu vết mà vẫn trả nguyên văn thì
đoạn overbooking của `ES-346661` vẫn đi ra bằng đúng endpoint này.

⚠ **Rút lần hai thì 409, và cổng đó nằm ở HANDLER, không ở domain.** Phép đột biến số 4 (bỏ luật
chống ghi đè trong `EvidenceItem`) chỉ làm **domain test** đỏ, còn test API vẫn xanh — vì handler
tự kiểm trước khi gọi. Trùng lặp là cố ý (handler cần biết trước để trả đúng mã lỗi, domain là
chốt cuối cho mọi nơi gọi tương lai), nhưng nó nghĩa là **đổi luật phải đổi hai chỗ** — đúng dư
chấn mà `IM-22` đã ghi về biểu thức policy.

---

### Trạng thái verify

```text
✅ 282/282 xanh (170 domain + 19 hạ tầng + 93 API) · trước đó 245 · 0 warning
✅ migration đã apply lên kp_dev thật, không đụng RLS (không có bảng mới)
✅ CHẠY THẬT trên app đang chạy + kp_dev — 25 phép kiểm, 0 sai:
      bỏ trống mã           -> 400, và thông báo nói ra CẢ HAI giá trị dành riêng
      hai mã thật           -> distinct 2, không cảnh báo
      hai sentinel          -> distinct 0, CÓ cảnh báo
      gửi lại kèm mã mới    -> created 0, subTenantNotApplied true, cảnh báo nói ra
      rút lại               -> mẩu rời danh sách dùng được, dấu vết còn, NỘI DUNG không ra
      rút lần hai           -> 409, tác giả lần đầu còn nguyên
      coverage              -> 137/145 chưa khai, canTro nói thẳng
✅ bộ test biết ĐỎ — 4 phép đột biến:
      tính sentinel như mã thật                  -> 1 test đỏ
      mẩu đã rút vẫn nằm trong danh sách dùng    -> 2 test đỏ
      bỏ trống mã vẫn nạp được                   -> 1 test đỏ
      rút lần hai được ghi đè                    -> 1 test đỏ (domain — xem ghi chú ở trên)
```

⚠ **CHƯA LÀM, và đừng đọc mục này thành đã xong:**

```text
· Cổng 409 (shared TỪ CHỐI DUYỆT) KHÔNG được gỡ. AR-l vẫn MỞ — có trường không phải
  có dữ liệu, và coverage đang nói rõ 137/145 mẩu chưa khai.
· ✅ ĐÃ XỬ LÝ cùng ngày qua đường FIXTURE — xem `IM-36`. kp_dev nay mang nhãn mới
  (3 giá trị phân biệt) và mã khách sạn khai tường minh, nạp vào kho SẠCH sau khi xoá
  145 mẩu cũ. ⚠ Vẫn CHƯA export lại từ Jira: `dry-run-*.json` giữ nguyên nhãn cũ, cố ý
  (nó là mẫu vật lịch sử để check_corpus còn cái mà bắt).
· Bộ Postman chưa có request cho /evidence/rut-lai và /internal/sub-tenant-coverage.
  Và newman KHÔNG có trên máy này (docs/10 §? ghi là có — đó là dòng của máy CŨ), nên
  lượt chạy thật ở trên đi bằng script gọi HTTP trực tiếp.
· AR-n (che khối <NMua> lúc nạp) và AR-p (mượn case ngoài nhóm) vẫn chưa code, dù cùng
  đụng đường nạp — cố ý không gộp thêm vào migration này.
```

---

## `IM-34` · Ba quyết định đầu tiên được HIỆN THỰC — và đường đọc evidence vừa ra đời đã phơi ra hai thứ

**Ngày 2026-09-15, cùng ngày chốt.** Ba trong mười quyết định của `docs/14` đã code xong:
`AR-a` (tập giá trị quyền xem) · `AR-k` (luật gán `machineReadability`) · `AR-g` (đường đọc
evidence). Chọn ba cái này vì chúng **chặn nợ tích thêm**, không phải vì chúng dễ.

---

### `AR-a` — siết `visibilityScope` từ chuỗi tự do sang enum ba mức

```text
enum VisibilityScope { RiengTu = 1, NoiBo = 2, ToanCongTy = 3 }
```

🛑 **THỨ TỰ CÓ NGHĨA, và nó là lý do chọn BA mức chứ không phải hai.** `S7` nói mặc định là
*"HẸP NHẤT trong các nguồn"* — mà "hẹp nhất" chỉ có nghĩa khi tập giá trị **có thứ tự**.
Hai mức thì câu đó gần như vô nghĩa.

🛑 **DẠNG LƯU LÀ KEBAB, KHÔNG PHẢI TÊN C#** — và đây là chỗ suýt hỏng dữ liệu:

```text
HasConversion<string>()  lưu "NoiBo"     -> 7 dòng `noi-bo` trong kp_dev thành không đọc được
ValueConverter tường minh lưu "noi-bo"   -> 7 dòng cũ đọc nguyên vẹn      ← đã chọn
```

Các enum khác trong `AppDbContext` đều dùng `HasConversion<string>()`, nên làm theo cho
"nhất quán" là phản xạ tự nhiên — và nó sẽ hỏng ở lúc **ĐỌC**, tức xa chỗ gây ra. Bảng ánh
xạ vì thế **viết tường minh**, không sinh từ tên C#: sinh tự động thì một lần đổi tên enum
âm thầm đổi dạng lưu, mà **không test nào đỏ** — vì test cũng sinh chuỗi bằng chính hàm đó.

⚠ **Siết kiểu KHÔNG tự đóng lỗ:** `(VisibilityScope)99` vẫn ép kiểu được và enum không ném.
Nếu chỉ đổi kiểu mà bỏ `Enum.IsDefined` thì việc siết chỉ đổi một lỗ hổng **dễ thấy** (chuỗi
lạ, đọc log là thấy) thành một lỗ hổng **khó thấy hơn** (số lạ).

✅ **Test cũ `Duyet_ma_khong_noi_pham_vi_xem_thi_nem` ĐÃ ĐỔI ĐỐI TƯỢNG, không bị nới lỏng.**
Ba ca `null` / `""` / `"   "` giờ **không viết ra được** — trình biên dịch chặn, và chặn lúc
biên dịch mạnh hơn test. Nó được thay bằng ca còn lại thật sự: giá trị ngoài tập.

✅ **KHÔNG cần migration** — `has-pending-model-changes` trả *"No changes"*. Cột vẫn
`varchar(128)`; chỉ kiểu CLR và converter đổi.

---

### `AR-k` — luật gán `machineReadability`, và nó phải ĐO chứ không phải TIN

Lập luận cũ nằm ngay trong script export: *"script chỉ đẩy text thuần từ REST API — nó BIẾT
là text, nên khai High là khai thật."* **Câu đó sai ở chỗ đánh tráo hai câu hỏi:** trường
này không hỏi *"có phải text không"*, nó hỏi *"MÁY ĐỌC ĐƯỢC ĐẾN ĐÂU"*. Một mẩu `"80771"` là
text, và máy không đọc được gì từ nó.

**Đo trên 873 mẩu THẬT** (dry-run 345 + spread 528), hai corpus độc lập:

```text
                   TRƯỚC          SAU
giá trị phân biệt  1              3
High               100,0%         29,3% / 19,5%
Medium             —              45,5% / 52,8%
Low                —              25,2% / 27,7%
```

Hai corpus cho phân bố **tương tự nhau** — đó là tín hiệu, không phải nhiễu.

🎯 **Và đường đọc `AR-g` vừa build xong phơi ra ngay hiệu ứng, trên một case thật:**
`ES-346425` có 13 mẩu, tất cả đang mang `High`. Luật mới đổi **11/13**:

```text
!image-2026-09-01-12-02-20-470.png|width=1223   High -> Low      ảnh, máy không đọc được
!image-2026-09-01-12-03-31-901.png!             High -> Low
Gửi [~tuannt] Anh báo khách vào xóa phiếu xuất  High -> Medium   văn xuôi của người
Gửi [~anhnd2] Là NL-{-}00003 với 2 dấu "{-}"    High -> High     có cấu trúc thật
```

⚠ **11/16 dấu hiệu "có cấu trúc" KHÔNG KHỚP MẨU NÀO** trên 873 mẩu — xml, Traceback, stack
frame java/.NET, `HTTP/1.`, `statusCode`, SQL, `curl`. Trên nguồn này, nhãn `High` thực chất
do **một cặp dấu** quyết định: `{` và `}`.
→ **GIỮ LẠI 11 dấu hiệu chưa khớp là có chủ đích**, không phải quên dọn: `G12` nói đặc điểm
  dữ liệu của MỘT khách là **tham số**, không phải hằng số thiết kế. Nhưng **đừng đọc danh
  sách đó thành "đã phủ stack trace"** — phần ấy chưa từng được một mẩu nào kiểm chứng.

⚠ **Một ca biên, ghi ra thay vì tinh chỉnh cho đẹp số:** mẩu *"Anh liên hệ lại khách…
Mrs Thanh - 09xxx … `!screenshot-1.png|thumbnail!`"* (108 ký tự) bị gán `Low` vì có ảnh và
dưới ngưỡng 200. Có phần chữ đọc được thật, nên `Medium` cũng bảo vệ được. Luật đoán theo
HÌNH DẠNG thì sẽ sai lẻ tẻ — và điều đó **khác hẳn cái cũ**: một nhãn sai lẻ tẻ thì lọc ra
vẫn còn dữ liệu, còn một nhãn hằng số thì lọc xong **mất sạch**.

⚠ **Corpus hiện có VẪN mang nhãn cũ.** Luật nằm ở script export, nên `dry-run-*.json` và
`spread-*.json` chỉ đổi khi export lại — và `check_corpus.py` vẫn đang (đúng) báo
*"1 giá trị phân biệt, 100% High ← HẰNG SỐ"* trên chúng.

---

### `AR-g` — đường đọc evidence, và một GIỚI HẠN ĐO ĐƯỢC mà lúc chốt chưa ai biết

`GET /cases/{khoaNguon}/evidence` — tra theo **khoá nguồn**, thứ bên gửi đã biết về case của
chính họ (cùng lý do `AR-f` chọn khoá nguồn làm mỏ neo).

🛑 **NHƯNG: khoá nguồn chứa dấu `/` KHÔNG đọc ra được, kể cả khi mã hoá đúng.**

```text
GET /cases/crm:deal%2F4471/evidence   ->  404
GET /cases/crm:deal/4471/evidence     ->  404
```

ASP.NET **giải mã `%2F` thành `/` TRƯỚC khi định tuyến**, nên `{khoaNguon}` (một đoạn đường
dẫn) không bao giờ khớp. Mà `G1` nói khoá nguồn là **chuỗi tự do**, và README nêu thẳng ví dụ
`crm:deal/4471`.

→ Đã ghim bằng test `Khoa_nguon_co_dau_gach_cheo_HIEN_CHUA_doc_ra_duoc`, và test đó **chứng
  minh luôn case CÓ THẬT trong kho** (gửi lại tín hiệu → `created: 0`). Không có vế thứ hai
  thì cái 404 đọc ra thành *"chắc case chưa được tạo"*, và người sửa sẽ đi tìm sai chỗ.
→ **Chưa tự ý sửa hình dạng endpoint.** `C4-1` chọn dạng đường dẫn có lý do; đổi nó (thêm
  dạng query, hoặc route catch-all ở cuối) là một quyết định mới, không phải một chi tiết
  triển khai. Khoá dạng `jira:ES-123` — áp đảo về số lượng — không dính giới hạn này.

✅ Và endpoint này trả ra **nhãn `MachineReadability` đã lưu**, cố ý: đó chính là trường mà
`AR-k` đo được là hằng số. Không có đường đọc thì một lỗi kiểu đó chỉ lộ ra khi có người mở
database lên xem — và thực tế nó đã nằm đó **11 ngày**.

---

### Trạng thái verify

```text
✅ 245/245 xanh (148 domain + 19 hạ tầng + 78 API) · trước đó 216 · 0 warning
✅ KHÔNG cần migration
✅ CHẠY THẬT trên kp_dev:
      7 record `noi-bo` CŨ đọc lại nguyên vẹn qua converter mới
      `phamViXem: "team:ops"` -> 400 kèm ĐỦ danh sách: rieng-tu · noi-bo · toan-cong-ty
      GET /cases/jira:ES-346425/evidence -> 13 mẩu, xếp theo thời điểm QUAN SÁT
✅ bộ test biết ĐỎ — 3 phép đột biến:
      bỏ `Enum.IsDefined` khỏi Approve          -> 5 test đỏ
      đổi dạng lưu `noi-bo` thành `NoiBo`       -> 6 test đỏ
      bỏ `OrderBy` khỏi đường đọc evidence      -> 1 test đỏ
```

⚠ **Còn lại của ba câu này:** corpus chưa export lại (nhãn mới chưa vào `kp_dev`) · hình dạng
URL cho khoá có dấu `/` chưa quyết · và **bảy quyết định còn lại của `docs/14` vẫn chưa code**
— trong đó `AR-l` và `AR-e` là hai cái mà mọi rủi ro ở README đang chờ.

---

## `IM-33` · Hình dạng thứ BA phá khoá cứng của `bangTraMaLoi` — và `diff(A,B)` giờ là một phép ĐẾM, không phải một lần đọc

**Ngày 2026-09-15**, làm trong lúc chờ support duyệt `docs/16`. Ba việc, và mỗi việc để lại
một thứ dùng lại được. Chi tiết đầy đủ ở `docs/11` §11 và §12; đây chỉ ghi phần chạm vào code.

### Cây thứ ba: `SopDraftSchema` KHÔNG chứa nổi nó

Nhóm *Lệch cấu hình tiền/thuế* (8 case, **63 mẩu** — dày nhất trong 19 nhóm) ra một topology
thứ ba: **bảng đối chiếu ba nơi**, khoá `(cột tiền bị lệch, nơi khai sai)`. Schema có
`bangTraMaLoi` với khoá **cứng** `(ncc, maLoi)` nên không có chỗ để vào.

```text
nhóm 1   cây phân nhánh lồng nhau          5 bước kiểm · 13 nhánh · 0 bảng
nhóm 2   bảng tra, khoá (NCC, mã lỗi)      3 bước kiểm ·  6 dòng bảng
nhóm 3   bảng đối chiếu, khoá (cột, nơi)   6 bước kiểm ·  8 dòng bảng
```

🛒 **Chỗ phải sửa khi đụng lại `SopDraftSchema`:** trường bảng để bản nháp **KHAI RA khoá của
nó**, đừng đặt khoá sẵn trong schema. Hai phép kiểm đã đi trước theo hướng đó và chạy được:
`nhom_sop.py --kiem-cay` và `goi_duyet.py` đều đọc khoá theo khai báo, và đều bắt trùng khoá
trên chính khoá được khai. Schema thì **chưa sửa** — sửa nó là đổi prompt thật, nên chờ cùng
lượt với quyết định `A3`/`AR-p`.

⚠ Và khác biệt thật không nằm ở số chiều của khoá mà ở chỗ **khoá đến từ đâu**: mã lỗi thì
hệ thống trả về, còn `(cột, nơi khai sai)` **không có trên màn hình nào** — phải dựng ra bằng
cách mở cùng một hoá đơn ở ba nơi rồi so từng cột. Nên nhóm này có một bước kiểm **số 0** mà
hai nhóm kia không cần, và nó sinh từ hai lần vấp thật chứ không phải phòng xa.

### Năm ô của `AR-q` lệch nhãn giữa bản A và bản B — không phép kiểm nào bắt được

Năm ô chốt **2026-09-11**; hai cây bản A viết **09-07/08** nên đặt tên ô theo ý người viết lúc
đó. `--kiem-cay` cộng mọi ô **bất kể tên**, nên phép cộng vẫn khớp, không cảnh báo nào — trong
khi `M2` so A với B **theo ô**, tức mọi ô đều "khác tên". Một phép so im lặng vô nghĩa.

```text
đã sửa   nhãn migrate về tập đóng, GIỮ NGUYÊN từng con số và từng mã case
         (xếp lại theo định nghĩa mới = viết lại kết luận cũ bằng tiêu chí người viết
          lúc đó không có; chỗ đó ghi vào `_nhanDaDoi` của chính hai file)
đã sửa   --kiem-cay nay CHẶN một ô lạ thay vì cộng nó vào
```

⚠ Nó lộ ra theo một đường đáng ghi: phép kiểm cũ bỏ qua khoá bắt đầu bằng `_`, còn cây nhóm 3
đặt chú thích tên `🎯_moiTuongQuan…` — **tiền tố emoji rồi mới tới `_`**, đúng lối đặt tên repo
này dùng ở `duyet` và `hinhDang`. Nó chết bằng `TypeError: string indices must be integers`,
một thông báo không chỉ về phía cây. Phép kiểm nay xét **hình dạng giá trị**, không xét tên.

### `diff_ab.py` — `M2` có phép đếm chạy lại được

`scripts/jira-export/diff_ab.py`, chạy `--tat-ca` để bắt cặp A/B theo tên nhóm bên trong file.

```text
                                   nhóm 1          nhóm 2
(a) bất đồng PHÂN BỐ theo ticket   3/10  30,0%     3/10  30,0%
(b) chênh lệch MÔ TẢ (chuẩn theo A)      31,0%           52,8%
(c) chênh TỈ LỆ NHÁNH CÓ NGUỒN      3,8 điểm %      30,8 điểm %
                                   A 54% · B 50%   A 94% · B 63%
```

🎯 `(a)` **bằng nhau đúng 3/10 ở hai nhóm dựng độc lập** — con số `M2` đầu tiên đến từ một phép
đếm, không từ một lần đọc. Nhưng **chiều lệch ngược nhau**, và đó mới là chỗ đáng đọc: nhóm 1
máy xếp **cao hơn** người (nặng nhất là `ES-343733` — máy đưa một ca chẩn đoán-qua-remote vào ô
`ghi-ro-buoc-kiem`, đúng ca biên mà schema viết hẳn một đoạn để cảnh báo), nhóm 2 máy xếp
**thấp hơn** người ở cả 3 ca — chỗ này đọc có điều kiện vì bản A nhóm 2 viết trước khi năm ô có
định nghĩa.

🛑 `(c)` nhóm 2 là con số khó chịu nhất: máy dựng **24 nhánh** so với 11 của người, mà phần mở
rộng đó phần lớn **không có ca thật đứng sau** (94% → 63%). Nhiều hơn không phải tốt hơn, và
`(b)` — vốn chỉ đếm độ chi tiết — sẽ chấm ngược nếu đọc một mình. Vì vậy script in **ba cách
tính cạnh nhau, không có điểm tổng**: cách tính `diff(A,B)` vẫn là câu CHƯA QUYẾT (`docs/11`
§8 mục 3), và một script tự chọn một cách rồi in ra "32%" sẽ biến câu chưa quyết thành một con
số trông như đã quyết.

**Một thứ trước đây phải tìm bằng tay nay là một dòng bảng:** mục *ĐỘ PHỦ NGUỒN* của nhóm 1 in
ra `chỉ A dẫn: ES-343036` — đúng case mà bản A mượn từ nhóm khác để dựng bước loại trừ, và là
chỗ bản B không với tới được. Câu `A3`/`AR-p` của `docs/14` vì thế có thêm một phép đo đứng sau.

### Một câu SAI suýt gửi ra ngoài, và ngưỡng phải đặt ở hai phần ba

`goi_duyet.py` mở đầu bằng một câu **hằng số**: *"ticket ghi được kết luận, nhưng hầu như không
ghi cách anh/chị biết"*. Câu đó sai với nhóm 3 (**6/8 ticket CÓ ghi**). Đã đổi thành câu được
**tính** từ chính các cây trong gói — nhưng bản đầu đặt ngưỡng ở **một nửa** và hỏng ngay lần
chạy đầu: bản máy sinh nhóm 1 khai 5/10, đúng ranh giới, nên gói **đang chờ duyệt** (`docs/16`)
đổi lời sang *"phần lớn chữ trong đó là chữ của chính anh/chị"*. Ở mức một nửa thì đó là nói
quá, và nói quá với đúng người biết rõ sự thật thì mất nhiều hơn được.

→ Ngưỡng đặt ở **hai phần ba**. Hệ quả kiểm được: `docs/12` và `docs/16` sinh lại ra **y nguyên
từng bit** bản đã gửi, chỉ `docs/17` dùng câu mới.

---

## `IM-32` · Đường nạp BẢN B đã có — `M2` giờ đo được; và một bẫy EF nằm chờ sẵn cho MỌI đường tương lai

**Ngày 2026-09-13 (lượt hai).** `M2` = *số nháp được duyệt + mức sửa `diff(A, B)`*. Sáng nay
A (máy sinh) đã vào kho được, nhưng **B chưa có đường nào** — nên `M2` vẫn không đo được gì,
dù cả hai nửa đã thiết kế xong từ lâu. Hai endpoint mới đóng nốt vòng đó:

```text
POST /knowledge/{id}/sua    nạp bản người sửa, KHÔNG đè bản A
GET  /knowledge/{id}        đọc lại CẢ lịch sử assertion — chỗ diff(A,B) đọc được
```

⚠ **Đường RIÊNG, cố ý không gộp vào `/approve`.** `S7` nói duyệt là *mở quyền xem*, còn đây
là *đổi nội dung*. Gộp thì không phân biệt được "người duyệt đồng ý nguyên văn" với "người
duyệt sửa rồi mới đồng ý" — mà đúng **khoảng cách giữa hai ca đó** là con số `M2` muốn đo.

⚠ **`GET /knowledge/{id}` không phải tiện ích.** `/hoi` cố ý chỉ trả bản CÒN HIỆU LỰC (người
đang xử lý ticket không cần lịch sử). Thiếu đường đọc này thì bản A vẫn nằm trong database
mà **không có cách nào lấy ra** — `M2` vẫn không đo được dù dữ liệu đã đủ. Một cách hỏng rất
im lặng: **dữ liệu đúng, đường đọc thiếu.**

### `KnowledgeRecord.ReviseAssertion` — bốn bất biến, và vì sao phải là một HÀM

Cơ chế (`ReplacedByAssertionId` / `IsCurrent`) có từ 2026-08-24, nhưng tới hôm nay **chỗ duy
nhất trong cả repo thực hiện phép sửa là bộ test, và nó gán tay**. Một cơ chế mà chỉ test
biết dùng đúng thì mã sản phẩm sẽ dùng sai — nhất là khi bốn bất biến ĐỘC LẬP với nhau, nên
quên một cái không làm hỏng ba cái kia, tức quên là quên IM LẶNG.

```text
1  bản mới giữ NGUYÊN Kind      sửa "cách nhận ra" mà ra "cách xử lý" thì diff không so
                                 được cái gì với cái gì
2  origin KHÔNG được AiInference M2 chỉ đọc được khi hai vế khác origin; cùng origin thì
                                 diff còn tồn tại nhưng mất nghĩa
3  chỉ sửa bản CÒN HIỆU LỰC      sửa bản đã bị thay là rẽ nhánh — diff(A,B) không biết B là ai
4  assertion phải thuộc record   này
```

✅ Và mặc định là `HumanAssessment`, **không** phải `UserConfirmed`: người support sửa một
bản nháp là *nhận định có chuyên môn*, còn `UserConfirmed` nghĩa là *"người dùng đã nói vậy"*
— một sự thật về LỜI NÓI chứ không về nội dung (v0.2 §7.5).

⚠ Hàm này **KHÔNG động tới `CauseName`**, cố ý. `T1`: tên nguyên nhân là IDENTITY. Đổi nó là
đổi record đang nói về cái gì, không phải sửa một phát biểu. Nếu người duyệt nói bản thân
nguyên nhân sai thì đó là một record KHÁC cộng `Deprecate` bản cũ. **Chưa có đường cho việc
đó** — và đừng lẳng lặng thêm vào hàm sửa.

✅ **Hệ quả ĐÚNG, không phải tác dụng phụ:** sửa sau khi duyệt làm `NeedsReview` bật (`V3`),
nên `DisplayState` = `NEEDS_REVIEW`. Tri thức **KHÔNG bị rút** khỏi đường trả lời (V3: gắn cờ
chứ không rút) — người đang xử lý ticket vẫn dùng được, và vẫn thấy ngay nó đã đổi kể từ lần
duyệt cuối. Duyệt lại thì sạch cờ, và **chính lần duyệt lại đó là vế "số nháp được duyệt"**.

### 🛑 PHÁT HIỆN NẶNG NHẤT — KHOÁ CHÍNH DO CODE SINH, MÀ EF KHÔNG ĐƯỢC NÓI

Bộ test API đỏ với `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually
affected 0`. Thông điệp nói về **concurrency** và trỏ hoàn toàn sai chỗ. In trạng thái
`ChangeTracker` ra mới thấy:

```text
Assertion=Modified | Assertion=Modified | KnowledgeRecord=Unchanged | Assertion=Unchanged ...
                                            ^ KHÔNG có cái nào Added
```

**Nguyên nhân:** mọi entity đặt `Id` ngay trong bộ khởi tạo (`Guid.CreateVersion7()`), nhưng
theo QUY ƯỚC của EF, khoá chính kiểu Guid là `ValueGenerated.OnAdd` — và với khoá OnAdd *đã
có giá trị khác mặc định*, EF suy ra entity đó **đã tồn tại**. Thêm một `Assertion` vào một
`KnowledgeRecord` ĐANG ĐƯỢC THEO DÕI thì nó bị gắn `Modified`, rồi EF sinh một `UPDATE` vào
một dòng không tồn tại.

🛑 **Vì sao nó không lộ ra sớm hơn, và vì sao đó mới là phần đáng sợ:** đường nạp bản nháp
`Add()` **nguyên cả đồ thị** một lần, và khi đó cả cụm được gắn `Added` nên không ai thấy gì.
Lỗi chỉ hiện ra ở chỗ ĐẦU TIÊN thêm con vào một cha đã nạp. Tức nó **nằm chờ sẵn cho mọi
đường tương lai**, không riêng đường sửa — và mỗi đường đó sẽ tự phát hiện lại nó bằng một
thông điệp lỗi nói về chuyện khác.

→ **Sửa ở GỐC, và suy TỪ MODEL chứ không phải danh sách viết tay** (cùng lý do `IM-7`: thêm
  entity mới mà quên thêm vào danh sách thì cái bẫy lặng lẽ quay lại):

```csharp
foreach (var khoa in b.Model.GetEntityTypes()
             .Select(t => t.FindPrimaryKey()).Where(k => k is not null)
             .SelectMany(k => k!.Properties).Where(p => p.ClrType == typeof(Guid)))
{
    khoa.ValueGenerated = ValueGenerated.Never;
}
```

✅ **KHÔNG cần migration** — `dotnet ef migrations has-pending-model-changes` trả *"No changes
have been made to the model"*. Thay đổi chỉ ở cách EF **phân loại trạng thái**, không đụng DDL.

### Một lỗi nhỏ trong `goi_duyet.py`, sửa vì nó gửi RA NGOÀI dự án

`dau_trang()` nhận tham số `so_cay` rồi **không dùng**, và viết cứng "hai" ở ba chỗ. Sinh gói
duyệt cho MỘT nhóm thì ngay dòng đầu đã nói sai với chính người được nhờ đọc. Đã sửa theo số
cây thật; `docs/12` sinh lại **giống hệt từng byte** nên tài liệu cũ không lệch.

### Trạng thái verify

```text
✅ 216/216 xanh (125 domain + 19 hạ tầng + 72 API) · trước đó 194 · 0 warning
✅ CHẠY THẬT trên kp_dev, trên chính record đã duyệt sáng nay:
      trước     6 assertion, tất cả AiInference, ACTIVE
      sửa       bản A [AiInference/Supported] và bản B [HumanAssessment/Verified]
                cùng nằm trong response · record tự thành NEEDS_REVIEW
      đọc lại   7 assertion · bản A conHieuLuc=false, có biThayBoi · bản B hiệu lực
      /hoi      trả bản B, trangThai NEEDS_REVIEW   ← V3 gắn cờ chứ không rút
      duyệt lại ACTIVE, canDuyetLai=false
✅ bộ test biết ĐỎ — 2 phép đột biến:
      bỏ luật ValueGeneratedNever       -> 4 test API đỏ
      bỏ luật từ chối AiInference       -> 1 test domain đỏ
```

⚠ **Một cơ chế KHÔNG đỏ được khi thử phá, ghi rõ kẻo nói quá:** gỡ luật `AiInference` khỏi
Domain thì test API vẫn XANH, vì tầng API kiểm riêng trước khi gọi xuống. Đó là hai lớp bảo
vệ làm đúng việc của chúng — nhưng nói "mọi test đều biết đỏ" là nói quá. Cùng dạng với ghi
chú về tính idempotent ở §2.

⚠ **Còn thiếu, biết rõ:** chưa có đường nào cho ca *"chính NGUYÊN NHÂN sai"* (phải là record
mới + `Deprecate`), và chưa có chỗ nào TỔNG HỢP `diff(A,B)` thành con số — hiện đọc được
từng record một, nhưng `M2` cuối cùng cần một phép đếm trên cả nhóm.

---

## `IM-31` · VÒNG TRI THỨC ĐÃ ĐÓNG — nạp → duyệt → hỏi chạy hết trên dữ liệu thật, và cổng `AR-l` giờ là CODE chứ không phải lời dặn

**Ngày 2026-09-13.** Bốn mảnh (a)(b)(c)(d) của phạm vi demo đã build xong và **đã chạy
thật**, không chỉ xanh trong test. Ba endpoint mới:

```text
POST /knowledge/sop-draft        (a)  nháp JSON -> knowledge_record + assertion, ở Draft
POST /knowledge/{id}/approve     (b)  đường ĐẦU TIÊN với tới KnowledgeRecord.Approve()
POST /hoi                        (c)(d)  cửa đầu tiên cho một CÂU HỎI + cổng KHÔNG BIẾT
```

Trước hôm nay, `Approve()` có ở Domain từ 2026-08-24 mà **không nơi nào gọi**, và host chỉ
có hai cửa NẠP. Kho rỗng theo nghĩa cấu trúc: không phải chưa ai nạp, mà **không có đường
nào để nạp**.

### Chạy thật, không phải chỉ test — dữ liệu thật, app thật, `kp_dev` thật

```text
kho trước    knowledge = 0
nạp nháp     may-sinh-phan-quyen-ky-hieu.json -> 8 record, tất cả Draft
             buocSuaMoCoi = ["S8"]        ← bắt được lỗi thật qua đường HTTP thật
HỎI trước    ES-346039 -> bietTraLoi=FALSE · lyDo="nhom-chua-co-sop-duoc-duyet"
   khi duyệt    (định tuyến ĐÚNG nhóm, nhưng chưa ai duyệt)   ← CỬA 2 làm việc
duyệt        7 record CÓ đường nhận ra. Record mồ côi S8 CỐ Ý KHÔNG duyệt —
             một nguyên nhân không có cách nhận ra thì không nên phát cho ai.
HỎI lại      ES-346039 -> bietTraLoi=TRUE · 7 hướng dẫn · tất cả ACTIVE / noi-bo
kho sau      Active 7 · Draft 1        ← đúng bằng số đã duyệt, mồ côi vẫn nằm ngoài
```

Ba ca KHÔNG BIẾT cũng đo thật, mỗi ca một lý do khác nhau:

```text
ES-345990   phép đo nói KHONG-BIET  -> "Trộn hai việc: mất mục cấu hình VNPT và đòi tự phát hành"
chỉ tiêu đề + mô tả                 -> "Bản đang chạy là bản TRA SẴN, chỉ trả lời được cho ticket đã đo"
ES-999999   không có trong bảng     -> "không nằm trong 150 ticket đã đo"
```

### 🛑 Cổng `AR-l` giờ CHẠY ĐƯỢC

Phạm vi demo tránh `AR-l` bằng cách khoá một tenant, không xuất bản ra ngoài — nhưng **một
phạm vi chỉ nằm trong tài liệu thì không chặn được gì**. Nên nó nằm trong
`KnowledgeApprovalHandler`: **chế độ shared TỪ CHỐI DUYỆT**, trả 409 kèm lý do. Duyệt LÀ
xuất bản, và rò của `AR-l` xảy ra ở khâu xuất bản chứ không ở khâu truy vấn hàng.
→ Muốn duyệt ở chế độ shared thì phải giải `AR-l`, không phải sửa dòng đó.

### 🛑 Cổng "KHÔNG BIẾT" có HAI CỬA, và cửa thứ hai là cửa dễ quên

```text
CỬA 1  định tuyến không biết       docs/13 §3.2: trên 62 case hồ sơ KHÔNG BAO GIỜ ghi
                                    nguyên nhân, bộ định tuyến vẫn dám gán 40,3%
CỬA 2  biết nhóm, nhưng nhóm CHƯA  docs/13: "định tuyến đúng nhóm chỉ có nghĩa NẾU nhóm
       CÓ SOP ĐƯỢC DUYỆT            đó có một SOP đã duyệt để trả về". Thiếu cửa này thì
                                    hệ thống trả RỖNG, và rỗng đọc ra thành "không sao cả".
```

### Hai lỗi thật tìm được khi build — cả hai đều do TEST bắt, không do đọc code

🛑 **1. GỘP ÂM THẦM HAI NGUYÊN NHÂN CỦA HAI NHÓM KHÁC NHAU.** Bản đầu của
`SopDraftIngestHandler` khử trùng theo `CauseName` trên **toàn tenant** — nghe hợp lý, vì
`T1` nói identity của record LÀ nguyên nhân. Nhưng `CauseName` lấy từ `quanSat`, mà `quanSat`
là **lời model viết**: "Danh sách rỗng", "Ô trống nhưng chọn được" trùng nhau giữa các nhóm
là chuyện thường. Hậu quả: nạp bản nháp của nhóm thứ hai thì **nạp 3, tạo 1, trả 200, không
ai biết**.
→ Sửa: khử trùng theo CẶP `(nhóm, CauseName)`. Gộp im lặng tệ hơn trùng lặp — trùng thì đếm
  ra là thấy, gộp thì record biến mất mà response vẫn xanh. Cùng họ với `AR-k`/`AR-l`.

⚠ **2. TEST XANH KHI CHẠY MỘT MÌNH, ĐỎ KHI CHẠY CẢ BỘ — và lý do đáng mang đi.**
Helper dựng bối cảnh test dùng `Guid.CreateVersion7().ToString("N")[..12]` làm hậu tố duy
nhất. Nhưng **GUID v7 xếp theo THỜI GIAN**: 12 ký tự đầu là timestamp, nên hai lần gọi trong
cùng một mili giây ra **cùng một chuỗi**. Hai bối cảnh lẽ ra độc lập hoá ra dùng chung một
tên nhóm.
→ **Cắt phần ĐẦU của một id thời-gian-tăng-dần là cắt đúng phần KHÔNG phân biệt được.**
⚠ Và chiều nguy hiểm không phải chiều đã xảy ra: một test đỏ vì test khác thì còn thấy;
  cùng cơ chế đó có thể làm một test **XANH vì lý do sai**.

### Ba quyết định hình dạng, ghi vì chúng khó đọc ra từ code

```text
· "LÁ của cây" hiện thực là BƯỚC SỬA, không phải đường-từ-gốc-tới-lá. Cây thật CÓ CHU
  TRÌNH (K2 <-> K3) nên liệt kê đường đi sẽ lặp vô hạn — xem IM-30.
· Nhóm gắn vào record bằng assertion `Applicability`, KHÔNG bằng một cột. `AP1` nói
  applicability là assertion; và docs/13 §3.3 đo được 4/5 lỗi định tuyến nằm ở SEAM giữa
  hai nhóm kề nhau, `AR-p` đang mở đúng câu vẽ lại nhóm. Đóng một taxonomy CHƯA ỔN ĐỊNH
  thành cột là cam kết vào thứ vừa đo được là đang lung lay.
· `IDinhTuyenNhom` mỏng như `ISopDrafter` (`AR3`). Bản đang cắm TRA SẴN, không gọi API.
  Đổi sang bản gọi model thật = đổi MỘT dòng DI, nơi gọi không đổi.
```

### Trạng thái verify

```text
✅ 194/194 xanh (114 domain + 19 hạ tầng + 61 API) · trước đó 157 · 0 warning
✅ chạy THẬT trên kp_dev: nạp -> hỏi(không biết) -> duyệt -> hỏi(7 hướng dẫn)
✅ bộ test biết ĐỎ — 2 phép đột biến trên hai cổng an toàn:
      bỏ lọc `Lifecycle == Active` khỏi HoiHandler  -> 2 test đỏ
      bỏ cổng shared khỏi KnowledgeApprovalHandler  -> 1 test đỏ
✅ quét credential: sạch (53 giá trị mạnh đối chiếu, mã thoát 0)
✅ RLS vẫn sống: psql bằng kp_app mà quên đặt app.current_tenant -> 0 dòng (IM-6)
```

### ⚠ Demo này KHÔNG chứng minh gì — đọc trước khi báo cáo ra ngoài

```text
❌ KHÔNG  độ chính xác định tuyến. Bản đang cắm TRA BẢNG có sẵn đáp án. Con số 84,1%
          đã đo ở docs/13 và VẪN nằm ở đó, không phải ở đây.
❌ KHÔNG  chất lượng SOP. Vẫn là bản B do máy sinh, chưa người làm support nào sửa.
          Mọi assertion vẫn mang Origin=AiInference SAU KHI DUYỆT — cố ý, `M2` cần
          đọc lại được bản gốc. "Duyệt" mở quyền xem, nó KHÔNG biến lời máy thành lời người.
❌ KHÔNG  `AR-l`. Cổng shared là TRÁNH, không phải GIẢI.
❌ KHÔNG  ngưỡng của cổng KHÔNG BIẾT. Hiện cổng là nhị phân (có/không có SOP duyệt),
          chưa có ngưỡng độ tin cậy nào được ĐO. docs/10 §6.5 mục 3 vẫn mở.
```

⚠ **Còn thiếu, biết rõ:** chưa có unique index trên `(tenant_id, cause_name, nhóm)`, nên hai
request nạp CÙNG LÚC vẫn sinh được record trùng — khác đường tín hiệu (có index làm chốt
cuối). Với một người gọi tay thì chưa chặn việc gì, nhưng đừng đọc thành đã kín.

---

## `IM-30` · Một bản nháp SOP sinh ra NHIỀU record — và cây thật có CHU TRÌNH nên không liệt kê đường đi được

**Ngày 2026-09-13.** Bắt đầu mảnh (a) của phạm vi demo thì chạm ngay một chỗ không được
tự quyết: `SopDraft` là bản nháp của MỘT NHÓM, còn `T1` nói identity của `KnowledgeRecord`
là MỘT NGUYÊN NHÂN. Mà `docs/11` đã đo: nhóm dày nhất có **bốn** nguyên nhân với triệu
chứng khách kể trùng nhau. Nên phép map **không phải 1-1**, và hạt của record là một
quyết định domain chứ không phải chi tiết triển khai.

✅ **Chủ dự án chốt qua form: mỗi LÁ của cây = 1 record.** Đúng `T1`. Hai phương án bị bỏ:
gộp cả nhóm thành một record (nhanh hơn nhưng lệch đúng quyết định nền của domain model —
`D5` gọi đó là giàn giáo tạm), và phương án lai thêm một bảng artifact riêng (đúng `T1` và
dễ cho bước (c) nhất, nhưng nhiều việc nhất).

🛑 **PHÁT HIỆN KHI HIỆN THỰC — "đường từ gốc tới lá" KHÔNG LIỆT KÊ ĐƯỢC.** Cây thật do máy
sinh cho nhóm 1 **có chu trình**:

```text
K2 --("Đã có quyền, xem tiếp role")--> K3
K3 --("Ticket chỉ ghi 'chưa phân quyền'")--> K2
```

Một mapper liệt kê đường đi sẽ **lặp vô hạn trên dữ liệu thật**. Nên "lá" được hiện thực
là **bước sửa** (`SopFixStep`): mỗi bước sửa là một nguyên nhân phân biệt, và các NHÁNH trỏ
tới nó là các cách nhận ra nó. Giữ đúng điều chủ dự án chốt, và chạy được trên cây có chu trình.
→ Test `Cay_co_chu_trinh_van_map_xong` **tự khẳng định cây thử có chu trình trước khi đo** —
  nếu ai sửa dữ liệu thử làm mất chu trình, test tự tố cáo là nó đã mất nghĩa.

⚠ **Và `S8` trong nháp thật KHÔNG NHÁNH NÀO TRỎ TỚI.** Một bước sửa không tới được là một
bước sửa không bao giờ được dùng. Không nuốt im lặng: nó vào `SopMappingResult.BuocSuaMoCoi`,
và chính record đó mang tên `"[nhóm · S8] KHÔNG CÓ ĐƯỜNG NHẬN RA — …"` để ai đọc kho tri thức
cũng thấy, thay vì mang một cái tên nghe bình thường rồi nằm im.

**Bốn ràng buộc đã đóng vào mapper, mỗi cái có test riêng:**

```text
AP4  một nhánh = MỘT assertion Recognition, KHÔNG gộp — nhánh LÀ một cách nhận ra
D4   MỌI assertion mang Origin.AiInference, kể cả nhánh tự khai `evidence-noi-ro`.
     Nhãn đó nói bản nháp CÓ ticket chống lưng; nó không biến nháp thành lời của người.
     Record ra đời ở Draft, VisibilityScope null, LastApproval null.
V1   TRẦN là Supported, KHÔNG BAO GIỜ Verified. Một phát biểu do máy sinh, TỰ KHAI là có
     nguồn, mà được gắn Verified thì cả cái thang mất nghĩa.
T1   CauseName lấy từ QUAN SÁT dẫn tới bước sửa, KHÔNG lấy từ `viec` — `viec` mô tả cách
     CHỮA chứ không mô tả BỆNH. Đặt identity vào cách chữa là đặt vào thứ dễ mục (D5).
```

**Trạng thái verify:**

```text
✅ 173/173 xanh (112 domain + 19 hạ tầng + 42 API) · trước đó 157 · 0 warning
✅ 16 test mới, KHÔNG cần PostgreSQL                              ← IM-18
✅ CHẠY TRÊN BẢN NHÁP THẬT do máy sinh, không chỉ dữ liệu thử:
      8 bước sửa -> 8 record  ·  S8 đúng là mồ côi  ·  0 nhánh bịa đích
✅ bộ test biết ĐỎ: gỡ guard "nhánh trỏ sang bước kiểm khác" -> 1 test đỏ
```

⚠ **Chưa làm, đừng đọc nhầm là xong:** mapper mới dựng record trong bộ nhớ. **Chưa ghi
DB**, chưa endpoint nào gọi, và `Approve()` vẫn chưa nơi nào gọi. Đó là phần còn lại của
mảnh (a) và toàn bộ mảnh (b).

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

→ 2026-09-29, `AR-r` (4): mẩu mới đó phải **trỏ về mẩu cũ** — không thì hai phiên bản của
cùng một comment trông như hai quan sát độc lập. Xoá ở nguồn là một dấu RIÊNG, `AR-r` (5).

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

> 🎯 **VÒNG TRI THỨC ĐÃ ĐÓNG — 2026-09-13. Danh sách `text` ngay dưới là bản CŨ.**
> Ba dòng của nó đã xong và không còn đúng nữa: **`ISoạnNhápSOP`** (xong 2026-09-08/10),
> **luồng duyệt** (`POST /knowledge/{id}/approve`), và **một cửa cho câu hỏi**
> (`POST /hoi`). Giữ nguyên danh sách cũ vì lập luận thứ tự trong đó vẫn giải thích được
> *vì sao* từng bước đến trước bước nào — nhưng đừng làm theo nó. Chi tiết: §3 `IM-31`.
>
> ```text
> ✅ (a) nháp JSON -> knowledge_record + assertion     POST /knowledge/sop-draft
> ✅ (b) luồng duyệt, S7 một hành động                 POST /knowledge/{id}/approve
> ✅ (c) đường vào cho câu hỏi + định tuyến             POST /hoi
> ✅ (d) cổng "KHÔNG BIẾT", HAI cửa độc lập            (trong /hoi)
> ```
> ✅ **VÀ VẾ B ĐÃ CÓ ĐƯỜNG VÀO — cùng ngày, xem §3 `IM-32`.**
> `POST /knowledge/{id}/sua` (người sửa, không đè bản A) và `GET /knowledge/{id}` (đọc cả
> lịch sử — chỗ `diff(A,B)` đọc được). Dòng `Tính diff(A,B) cho M2` ở danh sách dưới vì thế
> **đã có đủ DỮ LIỆU**; cái còn thiếu chỉ là phép ĐẾM trên cả nhóm, không phải đường ghi.
>
> ⚠ **CÒN LẠI, và không cái nào là việc của code:** kho chỉ có tri thức của MỘT nhóm và
> nó là bản B do máy sinh (chưa người làm support nào sửa) · định tuyến đang là bản TRA
> SẴN, chưa gọi model · `AR-l` mới TRÁNH chứ chưa GIẢI · ngưỡng của cổng KHÔNG BIẾT chưa
> được ĐO (hiện là nhị phân: có hay không có SOP đã duyệt).
> ⚠ `Tính diff(A,B) cho M2`: **đường GHI và đường ĐỌC đã xong** (`IM-32`), còn thiếu phép
> ĐẾM trên cả nhóm — và quan trọng hơn là còn thiếu **bản B thật của một người làm
> support**, thứ không code ra được.


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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §A2 = A2-1).
       ✅ ĐÃ CODE 2026-09-15 cùng ngày chốt — enum ba mức + converter kebab; 7 dòng cũ đọc
         nguyên vẹn, KHÔNG cần migration. §3 `IM-34`.
       → BA GIÁ TRỊ: `rieng-tu` · `noi-bo` · `toan-cong-ty`. Siết thành enum ở code.
         Lý do chọn: đủ cho `S7`, và quy tắc "mặc định HẸP NHẤT" chỉ có nghĩa khi tập giá
         trị có thứ tự — ba mức cho thứ tự đó. Không chọn theo PHÒNG BAN vì nó kéo theo
         câu "ai thuộc phòng nào", mà `AR-e` (xác thực người gọi) lúc chốt vẫn chưa code.
       ⚠ Việc phải làm, và nó gấp dần: đường duyệt build ngày 13 nhận `phamViXem` là CHUỖI
         TỰ DO, nên `kp_dev` đã có 7 record mang `noi-bo` mà không quyết định nào đứng sau.
         `noi-bo` nằm trong tập vừa chốt nên KHÔNG phải migrate — nhưng mỗi ngày chưa siết
         là thêm một cơ hội sinh ra giá trị thứ tư.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §C2 = C2-1).
       → BIẾN MÔI TRƯỜNG `ConnectionStrings__Default`.
         Lý do chọn: đủ cho khách hàng đầu tiên và không thêm một hạ tầng nào phải vận hành.
         Và vì host đọc qua `IConfiguration`, đổi sang secret store về sau là đổi CẤU HÌNH
         chứ không đổi CODE — nên quyết định này rẻ để đảo, khác hẳn hai câu ở nhóm B.
       (số hiệu cũ: `AR-d` thứ hai — đánh lại 2026-09-03 vì trùng, xem ghi chú trên)
       ĐÃ CÓ HÌNH DẠNG, chưa chốt nguồn. Host đọc ConnectionStrings:Default từ
       IConfiguration, nên biến môi trường ConnectionStrings__Default hoặc bất kỳ
       secret provider nào của .NET đều cắm vào được, không sửa code. Thiếu nó là
       KHÔNG START ĐƯỢC. Còn phải quyết: dùng secret store nào ở deploy thật.

AR-e   Chế độ shared multi-tenant xác thực người gọi bằng cách gì?          ← MỚI
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §C1 = C1-1).
       → API KEY THEO TỪNG KHÁCH HÀNG, thu hồi được riêng từng khách.
         Lý do chọn: nó giải đúng ba thiếu sót mà khoá dùng chung hiện tại có — phân biệt
         được khách A với khách B, thu hồi được theo từng khách, và không bắt bên tích hợp
         viết thêm code. mTLS và chữ ký payload đều nặng hơn cho bên tích hợp mà ở mức này
         chưa giải thêm được gì; chống replay chưa phải vấn đề của đường GHI idempotent.
       ⚠ Chưa code. Cho tới lúc code xong, chế độ shared vẫn TỪ CHỐI KHỞI ĐỘNG trừ khi có
         cờ thừa nhận tường minh — giữ nguyên `IM-13`.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §C4 = C4-1).
       ✅ ĐÃ CODE 2026-09-15 — `GET /cases/{khoaNguon}/evidence`.
       🛑 GIỚI HẠN ĐO ĐƯỢC mà lúc chốt chưa ai biết: khoá nguồn có dấu `/` KHÔNG đọc ra
         được, KỂ CẢ khi mã hoá `%2F` — ASP.NET giải mã TRƯỚC khi định tuyến. Cần một quyết
         định MỚI về hình dạng URL. §3 `IM-34`.
       → `GET /cases/{sourceReference}/evidence`.
         Lý do chọn: khớp với thứ bên gửi ĐÃ biết về case (khoá nguồn của chính họ), nên
         không bắt ai nhớ thêm một id nào — cùng lý do `AR-f` chọn khoá nguồn làm mỏ neo.
       ⚠ Đừng nhầm với `GET /knowledge/{id}` thêm ngày 13 (`IM-32`): cái đó đọc TRI THỨC
         kèm lịch sử assertion. Evidence vẫn CHƯA có đường ra tính tới lúc chốt.
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
       ✅ 2026-09-29: hai câu mở đầu dưới đây ("che ở TẦNG NÀO", "có GIỮ DẤU không") đã có
          câu trả lời ở `AR-r` (1) — che ở TẦNG NẠP của server, giữ dấu, không chặn. Câu
          thứ ba (sản phẩm có BÁO ĐỘNG khi thấy credential trong nguồn không) VẪN MỞ.

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
       ✅ MỞ LẠI THEO HƯỚNG NÀO — CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §C3 = C3-1).
       ✅ **ĐÃ CODE 2026-09-16 — nhưng MỤC NÀY VẪN MỞ.** `subTenantCode` là trường BẮT BUỘC ở
          đường nạp (bỏ trống = 400), ba trạng thái NULL / `khong-xac-dinh` /
          `khong-thuoc-khach-san` không gộp được vào nhau, và điều kiện mở lại hỏi được bằng
          `GET /internal/sub-tenant-coverage`. Chi tiết: §3 `IM-35`.
          🛑 **Có TRƯỜNG không phải có DỮ LIỆU.** Đo trên `kp_dev` ngay hôm code xong:
          137/145 mẩu chưa khai (nạp trước luật), và 2 mã phân biệt duy nhất đến từ đúng hai
          dòng tạo ra lúc thử. Cổng 409 KHÔNG được gỡ.
          ⚠ Và một mâu thuẫn phải biết: nguồn được `C3-1` chọn ("bên gửi khai") KHÔNG tồn tại
          cho connector Jira — `customfield_12710` là hằng số `-1.0`. Nên connector khai
          `khong-xac-dinh`, tức lô export từ nguồn này có ĐÚNG 0 giá trị phân biệt. Chủ dự án
          chốt 2026-09-16: bắt buộc khai + đường nạp tự ĐẾM giá trị phân biệt và nói thẳng.
       → THÊM MỘT TRƯỜNG BẮT BUỘC Ở ĐƯỜNG NẠP: bên gửi phải khai mã khách sạn.
         Lý do chọn: đây là nguồn DUY NHẤT chắc chắn cho ≥2 giá trị phân biệt — điều kiện
         mà chính mục này đặt ra để được mở lại. Suy từ tài khoản/email đã ĐO là không đáng
         tin (8/32 case có mã trong tiêu đề, và tên tiêu đề lệch với thân bài). Sửa quy
         trình Jira thì đúng gốc hơn nhưng phụ thuộc người khác, nên không chặn được gì.
       ⚠ Câu "có phải ranh giới bảo mật không" ĐÃ chốt từ 2026-09-04 (PHẢI). Quyết định
         hôm nay chỉ trả lời "mở lại bằng NGUỒN nào" — nhánh lùi 2026-09-07 vẫn đúng cho
         tới khi trường đó tồn tại VÀ có dữ liệu thật.
       ⚠ Cổng chạy được hiện tại GIỮ NGUYÊN: chế độ shared TỪ CHỐI DUYỆT (409) —
         xem `IM-31`. Đó là TRÁNH, không phải GIẢI, và nó chỉ được gỡ khi mục này đóng.
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

       🛑 **CỔNG CHE ĐÃ TRƯỢT MỘT CẶP ĐĂNG NHẬP THẬT — tìm ra 2026-09-11, không phải
          bằng phép thử mà bằng cách NHÌN payload sắp gửi.** Payload nhóm 1 in ra
          `Đã che: 0 chỗ`, trong khi nó đang mang ID + mật khẩu Ultraviewer của một
          khách sạn trong `ES-343733`. Tức `daChe = 0` KHÔNG có nghĩa "không có gì",
          và dòng chữ đó đang được đọc như thể có.
          Hai nguyên nhân ĐỘC LẬP, mỗi cái đủ để gây trượt — đã dựng lại được:
```text
· hình dạng 3 (dãy số TRẦN) chỉ nhìn xuôi 6 DÒNG từ một dòng có neo. Ở ES-343733
  neo "ultra view" ở dòng 255, hai dãy số ở dòng 285 và 288 — cách 30 và 33 dòng,
  vì khối tự-trả-lời của Zalo chen vào giữa.
· neo GẦN nhất là "ulatrview" — biến thể gõ sai THỨ SÁU, không khớp regex `CongCu`
  (`ultra ?view|utlatra|teamview|anydesk`). Chính file đó đã tiên đoán: "Hình dạng
  thứ sáu sẽ tới, và nó sẽ không may như vậy."
```
       ⚠ Và `make_fixture.py` KHÔNG đỡ được: `redact-list.json` là danh sách 19 GIÁ TRỊ
          nguyên văn, sinh từ mẫu 32 case của 2026-09-04. Nó có 5 cặp Ultraviewer —
          nhưng không có cặp của `ES-343733`, vì ticket đó đến từ mẫu 150 case về sau.
          → Mọi credential trong ticket NGOÀI mẫu 2026-09-04 đi thẳng qua fixture,
            rồi qua cổng che, rồi ra API. Danh sách theo GIÁ TRỊ không mở rộng được
            theo corpus; chỉ luật theo HÌNH DẠNG mới mở rộng được.
       → ĐÃ ĐO 2026-09-11 trên toàn fixture (345 mẩu · 252 387 ký tự), và **phép đo bác
         CẢ HAI hướng từng đề xuất.** Bảng dưới đếm trên cùng một tập, mọi chỗ bắt được
         đều đã đọc bằng mắt để đếm che nhầm:
```text
luật                                                bắt   sót   che nhầm
R0  hiện tại (cửa sổ 6 dòng, neo = tên công cụ)      12    17      0
R1  nới cửa sổ lên 40 dòng, giữ neo cũ               18    11      0
R2  đảo mặc định: cả mẩu CÓ nhắc công cụ remote      20     9      0
R3  dòng chỉ-có-số  +  số kề trong 3 dòng trước      29     0      0   ← ĐỀ XUẤT
```
       🛑 **Vì sao R2 — hướng "fail closed" nghe chắc nhất — vẫn sót 9:** neo nằm ở
         **mẩu KHÁC** của cùng case (`ES-342142`: cặp ID+mật khẩu ở mẩu biểu mẫu, chữ
         "ultra" ở mẩu khác), hoặc **không có neo nào** (`ES-345857` cặp đi sau
         "@Cs Ezcloud"; `ES-341377` hai dãy số đứng trần không một chữ nào quanh).
         Mọi luật dựa vào TỪ KHOÁ CÔNG CỤ đều thua ở đây, và thua vì cùng một lý do.
       🎯 **Thứ nhận ra chúng không phải từ khoá mà là HÌNH DẠNG TRÌNH BÀY:** trong
         corpus này, **một dòng mà toàn bộ nội dung chỉ là một dãy số thì luôn là
         credential** — 25/25. Số của nghiệp vụ (số hoá đơn, MST, mã phòng) luôn đi
         kèm chữ trong câu; chỉ ID và mật khẩu mới được dán đứng một mình.
         Bốn chỗ "số kề trong dòng" mà R3 bắt thêm cũng đều là ID thật, không cái nào
         che nhầm: `@Cs Ezcloud 10000007`, `ultra : 100000008` (×3).
       ✅ **R3 KHÔNG CÓ danh sách từ khoá nào để phải vá**, nên nó thoát được đúng cái
         bẫy mà `AR-q` vừa đo: không có biến thể gõ sai thứ bảy nào phá được nó.
       ⚠ Ba chỗ R3 KHÔNG giải, nói ra kẻo tưởng đã đủ:
         · ngưỡng `\d{4,9}` loại số 10 chữ số → số điện thoại KHÔNG bị che (đó là `AR-n`,
           câu khác), nhưng một ID 10 chữ số cũng sẽ lọt.
         · credential nằm trong câu văn xuôi vẫn phải nhờ hình dạng 1 và 4 — R3 là phần
           THÊM, không phải phần THAY cho cả bộ luật; nó chỉ thay hình dạng 3.
         · "0 che nhầm" đo trên chính corpus dùng để soi ra luật. Khác `AR-q` ở chỗ luật
           này thuần cấu trúc, không chỉnh theo giá trị hay từ khoá nào — nhưng rủi ro
           còn đó: khách dán một mã đặt phòng đứng một mình sẽ bị che. Giá của một lần
           che nhầm là mất một con số và còn lại dấu `[ĐÃ CHE]`; giá của một lần sót là
           credential rời khỏi máy.
       ✅ **CHỐT 2026-09-11 bởi chủ dự án: ÁP R3, THAY hình dạng 3.** Ba hình dạng còn
          lại giữ nguyên. Đã sửa `SecretShapeScanner`, thêm 6 test canh đúng ba kiểu sót
          đã đo — gồm cả một test ghim **giá phải trả** (số nghiệp vụ đứng sát trước dòng
          chỉ-có-số thì cũng bị che) để chuyện đó không bị phát hiện như một bất ngờ.
       ✅ **Kiểm ĐẦU-CUỐI trên đúng payload đã rò, không phải chỉ trên test:**
```text
trước:  Đã che 0 chỗ   ·  payload còn nguyên cặp ID + mật khẩu của ES-343733
sau :   Đã che 2 chỗ   ·  cả hai giá trị KHÔNG còn trong payload
```
       ⚠ Câu chưa quyết vẫn còn nguyên một câu, và nó độc lập với R3: bắt được bí mật thì
         **bỏ cả mẩu** hay **thay giá trị giữ hình dạng**? Hiện đang là thay-giá-trị
         (`[ĐÃ CHE]`), và phép đo nhóm 2 cho thấy cách đó ĐÚNG: hai ngữ cảnh sạch đều dùng
         chính dấu `[ĐÃ CHE]` làm bằng chứng rằng một phiên remote đã xảy ra, tức mẩu vẫn
         là **biên lai** của một lần chẩn đoán. Bỏ cả mẩu sẽ xoá luôn bằng chứng đó.
       ⚠ Chừng nào chưa sửa: điều kiện mà chủ dự án gắn vào `AR-o` ("luật che chạy
         trên payload trước mỗi lần gọi") ĐANG ĐƯỢC THOẢ VỀ HÌNH THỨC mà không thoả
         về thực chất. Lượt gọi thật đầu tiên nên xem payload bằng mắt trước.

AR-m   Nội dung ĐÃ BỊ NGƯỜI GỬI RÚT LẠI — chưa có chỗ nào đánh dấu.      ← MỚI
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §B1 = B1-1).
       ✅ **ĐÃ CODE 2026-09-16 — mục này ĐÓNG.** Ba cột + `EvidenceItem.RutLai` (đường DUY
          NHẤT đặt chúng) + `POST /evidence/rut-lai`. Đường nạp KHÔNG mang ba trường này nên
          máy không có cách nào tự suy từ câu chữ — có test chứng minh. Mẩu đã rút rời khỏi
          danh sách dùng được qua một KIỂU RIÊNG (không phải một cờ), và nội dung của nó
          không đi ra qua đường đọc. Chi tiết: §3 `IM-35`.
          ⚠ Thêm `RetractedBy` ngoài đề xuất gốc — lý do ở `IM-35`.
          ⚠ 2026-09-29: mẩu bị XOÁ Ở NGUỒN KHÔNG đi qua dấu này — nó là sự kiện máy quan sát
             được, không phải lời khẳng định của người. Dấu riêng: `AR-r` (5).
          🛑 **Giờ MỚI được chốt cách cắt transcript**, đúng thứ tự mục này đặt ra. Trước hôm
          nay thì không, và cắt rồi thì không ghép lại được.
       → THÊM CỘT `RetractedAt` + `RetractedReason` ở TẦNG MẨU, và CHỈ NGƯỜI điền tay.
         Máy KHÔNG được suy dấu hiệu rút lại từ câu chữ.
         Lý do chọn: suy từ câu chữ là ĐOÁN, trái `G6`/`AP3`. Chọn ít tín hiệu mà mỗi cái
         đều đúng, hơn là nhiều tín hiệu mà không biết cái nào tin được.
       ⚠ HỆ QUẢ MỞ ĐƯỜNG, và phải đúng thứ tự: có cột này rồi thì MỚI được chốt cách cắt
         transcript. Trước đó thì không — `AR-h` đo được mẩu dài đè bẹp xếp hạng 29:1 nên
         áp lực cắt là có thật, nhưng cắt trước khi có chỗ đánh dấu là tạo ra đúng ca
         `ES-346661` mà mục này sinh ra để chặn.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §B2 = B2a-1 + B2b-1).
       ✅ **ĐÃ CODE 2026-09-16 — hai cơ chế, và mục này ĐÓNG MỘT NỬA.** Chi tiết: §3 `IM-37`.
          · B2a: `ThirdPartyRedactor` che theo CẤU TRÚC (vị trí trong cây XML), chạy ở khâu
            NẠP trước khi nội dung chạm database. Che định danh, GIỮ số tiền — nhóm 3 chẩn
            đoán bằng cách so từng cột tiền, che số tiền là vứt bằng chứng của nhóm dày nhất.
          · B2b: cổng ở khâu DUYỆT (chủ dự án chốt 2026-09-16), `xacNhanDuLieuThuongMai` —
            thiếu hoặc false là 400, khai rồi thì lời khai được LƯU cùng lượt duyệt.
       🛑 **B2a chỉ biết MỘT hình dạng, đo trên n=1:** trên 873 mẩu thật, `<NMua>` xuất hiện ở
          ĐÚNG MỘT mẩu. Đủ để chứng minh loại dữ liệu này tồn tại, KHÔNG đủ để nói luật phủ
          được bao nhiêu. `thirdPartyRedacted: 0` nghĩa là luật không thấy gì, KHÔNG nghĩa là
          mẩu sạch.
       🛑 **B2b là LỜI KHAI, không phải phép kiểm.** Hệ thống không đối chiếu được — loại dữ
          liệu này không có hình dạng — và hôm nay còn không biết mẩu nguồn là những mẩu nào
          (`assertion_evidence` = 0 dòng). Nâng được lên "đối chiếu được" chỉ khi có mắt xích
          assertion → evidence.
       → BÊN THỨ BA: che khối `<NMua>` và tương đương NGAY Ở KHÂU NẠP.
         Lý do chọn: loại này CÓ hình dạng rõ nên bắt được bằng luật, giống `AR-j`. Và che
         ở khâu nạp giữ lại được phần kỹ thuật của payload — chỗ chứa mã lỗi để tra, tức
         đúng phần có giá trị nhất của mẩu đó.
       → THƯƠNG MẠI: cần NGƯỜI DUYỆT xác nhận trước khi mẩu đó vào kho.
         Lý do chọn: loại này KHÔNG có hình dạng nào để bắt bằng regex — chính mục này đã
         ghi điều đó. Không có hình dạng thì không có luật, và không có luật thì phải có người.
       ⚠ HAI NHÁNH KHÁC NHAU LÀ CỐ Ý, không phải thiếu nhất quán: hai loại khác nhau về
         BẢN CHẤT (một cái có hình dạng, một cái không), nên một cơ chế chung sẽ hoặc bỏ
         sót loại thứ hai, hoặc bắt người duyệt xem cả những mẩu mà luật thừa sức xử lý.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §B3 = B3-1).
       ✅ ĐÃ CODE 2026-09-15 — luật theo HÌNH DẠNG trong script export. Đo trên 873 mẩu thật:
         1 giá trị phân biệt -> 3. ⚠ Corpus hiện có VẪN mang nhãn cũ, phải export lại. §3 `IM-34`.
       → BÊN GỬI KHAI, hệ thống KHÔNG suy. Việc phải làm là SỬA SCRIPT EXPORT.
         Lý do chọn: endpoint ĐÃ đúng từ đầu (`IM-19`: gõ sai giá trị là 400, bỏ trống là
         `Unknown`). Tức chỗ hỏng chưa bao giờ nằm ở sản phẩm — nó nằm ở script export
         luôn khai `High`. Sửa đúng chỗ hỏng, đừng thêm cơ chế ở chỗ không hỏng.
       ⚠ Không chọn "hệ thống tự suy" vì đó là thêm MỘT PHÉP ĐOÁN NỮA, và phép đoán nào
         cũng phải đo recall của chính nó — đúng bài học của luật che (`AR-j`): luật theo
         dòng bắt 11%, tưởng là đủ cho tới khi có người đo.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (phiếu `docs/14` §A3 = A3-1).
       → CHO MƯỢN, kèm ĐỦ BA điều kiện, thiếu một là không được mượn:
           1  cấp corpus rộng hơn một nhóm cho `ISoạnNhápSOP`
           2  phép kiểm chống bịa nguồn đổi từ "trong NHÓM" sang "trong CORPUS"
           3  BẮT KHAI chỗ mượn — đúng như `_haiCaseLech` của bản người dựng
         Lý do chọn: điều kiện 2 và 3 mới là phần nặng. Không có chúng thì việc cho mượn
         là gỡ bỏ chính cái chốt `G6`/`AP3` sinh ra để chặn bịa nguồn.
       🛑 Và lý do KHÔNG giữ nguyên trạng: phép kiểm hiện tại đang QUA MỘT CÁCH TẦM THƯỜNG
         — bản B đặt `_haiCaseLech: null` rồi trích 9 mã đều trong nhóm, nên nó "qua" KHÔNG
         phải vì trung thực mà vì **không với tới được vùng bị cấm**. Một phép kiểm qua kiểu
         đó chưa kiểm gì cả, và giữ nguyên là giữ một con số xanh vô nghĩa.
       ⚠ KHÔNG chọn nhánh "vẽ lại taxonomy" nghĩa là CÂU TAXONOMY VẪN MỞ. Ba quan sát độc
         lập (docs/11 §9 · AR-p · docs/13 §3.3) vẫn đang nói ranh giới nhóm không phải ranh
         giới hợp lệ của một SOP. Cho mượn là VÁ chỗ đó, không phải giải nó.
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
       ✅ CHỐT 2026-09-15 bởi chủ dự án (form hỏi đáp; phiếu ở `docs/14` §A1 = A1-1).
       → DÙNG NGUYÊN TRẠNG bộ định nghĩa + ca biên đang nằm trong `SopDraftSchema`.
         Lý do chọn: con số chống lưng là 10/10 của NHÓM 2 — nhóm chưa hề được dùng để
         viết một chữ nào trong định nghĩa. 8/10 của nhóm 1 KHÔNG dùng được cho câu hỏi
         tổng quát hoá, vì nhóm 1 đã thành tập "đã-chỉnh-theo". Và nhóm 2 là phép thử
         KHÓ hơn chứ không dễ hơn: bản A của nó xếp 8/10 vào "ghi rõ bước kiểm".
       ⚠ Bản vá "xác nhận sau khi sửa ≠ bước kiểm" VẪN CHƯA ĐƯỢC ĐO. Con số mang đi tiếp
         là 10/10 (nhóm 2), và ĐỪNG đo lại nhóm 1 bằng bản vá đó — bản vá sinh TỪ chỗ
         lệch của nhóm 1, đo lại là đo chính mình.
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

AR-r   Khách KHÔNG có Jira thì Kênh 1 và Kênh 2 vào bằng đường nào?        ← MỚI
       ✅ CHỐT 2026-09-29 bởi chủ dự án (form hỏi đáp, bốn lượt). Năm ô dưới đây.
       Sinh từ câu của chủ dự án: *"phần mềm này sẽ tích hợp với nhiều công ty/sản phẩm
       khác nên không phải lúc nào cũng có jira"* — và ở lượt sau: *"có công ty có IT có
       công ty không"*.

       Ba tiền đề đã KIỂM trước khi hỏi, để không đọc câu này thành "phải viết lại":
         · Hợp đồng server ĐÃ không biết Jira (`G1`). Không DTO nào có trường của Jira;
           `sourceReference` là chuỗi tự do. Thứ gắn Jira là MỌI THỨ ĐỨNG TRƯỚC API:
           `export_jira_to_channel1.py` và bước "sinh JQL" của giao diện.
         · "Quét Jira" hôm nay thực chất là KÊNH 2 (nạp lịch sử hàng loạt) đi qua cửa
           Kênh 1 — Program.cs đã cố ý dùng một đường cho cả lô một phần tử lẫn lô lịch
           sử. Nguồn dạng B mới KHÔNG cần endpoint mới.
         · `dry-run-cases.json` + `dry-run-evidence.json` đã là ĐỊNH DẠNG TRUNG GIAN trên
           thực tế: check_corpus, prompt AI, load_fixture đều đọc chúng, không đọc Jira.
       🛑 Và chỗ hở thật: ba cổng CHỈ sống ở phía script Jira — quét credential
         (check_corpus), đoán machineReadability, xem trước bằng dry-run. Công ty tự viết
         connector gọi thẳng API là đi vòng qua cả ba. Lúc nạp, server chỉ chạy
         `ThirdPartyRedactor` (CaseEvidenceHandler), KHÔNG chạy `SecretShapeScanner`.

       (1) CREDENTIAL LÚC NẠP → CHE Ở SERVER, GIỮ DẤU, ĐẾM, KHÔNG CHẶN.
           `SecretShapeScanner` chạy ở CaseEvidenceHandler; chỗ nghi thay bằng dấu giữ
           chỗ; response trả SỐ chỗ đã che (KHÔNG kèm giá trị), đúng khuôn
           `ThirdPartyRedactedTotal`. Trả lời hai câu mở đầu của `AR-j`: che ở TẦNG NẠP
           của server (không chỉ ở connector), và che thì GIỮ DẤU.
           Lý do chọn: DB và backup không giữ credential, nên không đường đọc nào — kể
           cả đường chưa viết — rò được. Không chặn vì với đồng bộ tự động, dữ liệu ở
           nguồn không đổi: chặn lô là job kẹt vĩnh viễn ở đúng ticket đó.
           Cái giá, nói thẳng: chỗ luật che NHẦM (vd một số đặt phòng) mất khỏi kho
           vĩnh viễn. Chấp nhận vì bản gốc vẫn ở hệ thống nguồn, và luật đã được chỉnh
           để không ăn `80771` / mã số thuế (`AR-j`).
           ⚠ Cổng CHẶN của giao diện (check_corpus, đường chạy tay) GIỮ NGUYÊN — ở đó có
             người để sửa. Cổng che ở khâu gửi ra (`AR-o`) cũng giữ nguyên.
           ⚠ Che thêm ở đường ĐỌC không bắt thêm được gì: `EgressRedactor` dùng CÙNG
             `SecretShapeScanner`, lúc nạp lọt thì lúc đọc cũng lọt. Recall của luật vẫn
             là rủi ro mở của `AR-j`; quyết định này không đóng nó.

           🔁 LỊCH SỬ CỦA Ô NÀY — giữ lại vì chủ dự án chọn khác ô đề xuất ở lượt đầu, và
             lý do mở lại là LỖI CỦA AGENT:
             Lượt 1 chủ dự án chọn "NẠP NGUYÊN VĂN, CHỈ CẢNH BÁO" (agent đề xuất "che +
             đếm + báo"). Mô tả agent viết cho ô đó là *"vẫn được che ở khâu gửi ra"* —
             và câu đó SAI. Lúc ghi quyết định, agent kiểm các đường đọc và thấy
             `GET /cases/{khoaNguon}/evidence` (CaseEvidenceReadHandler, `AR-g`) trả
             `Content` NGUYÊN VĂN, không qua `EgressRedactor`. Bốn đường kia (webhook ·
             /xem · SopPromptBuilder · VongKhongCredit) có che; đường này không.
             Trước đây chỗ đó ít hệ quả vì cổng giao diện chặn credential trước khi nạp.
             Với nạp nguyên văn từ connector của công ty bất kỳ, ai cầm khoá API của một
             khách là rút được MỌI credential khách đó đã nạp.
             → Agent báo lại; chủ dự án chọn MỞ LẠI câu nạp (không chọn "che thêm đường
               đọc"), rồi chốt ô hiện tại ở lượt 4.
             Bài học: mô tả một phương án bằng một khẳng định về code CHƯA grep là đưa cho
             người quyết một tiền đề sai. Lần này bắt được vì ghi quyết định buộc phải đọc
             lại code.

       (2) THỨ TỰ BUILD → CỔNG SERVER → CSV → ADAPTER SAAS KHI CÓ KHÁCH THẬT.
             có IT                  họ viết adapter, gọi thẳng API. Mình cấp khoá,
                                    docs/19, bộ kiểm. Cổng ở server là lưới DUY NHẤT.
             không IT, SaaS có API  mình chạy adapter, họ cấp token — y hệt Jira hôm nay
             không IT, không API    xuất CSV, ghép cột trên giao diện — không ai viết code
           Lý do chọn: cổng server phục vụ nhóm có IT VÀ là lưới cho mọi đường khác. CSV
           phủ cả nhóm "không IT dùng SaaS" ở mức chạy tay, nên không phí khi sau này có
           adapter riêng. Adapter SaaS viết trước khi biết tên khách là đoán.
           Kèm: tách script Jira thành ADAPTER (kết nối · lấy item · map trường · giữ con
           trỏ) và PHẦN CHUNG (machineReadability · chia lô · dry-run → check_corpus →
           đẩy · báo phần bỏ qua). Giao diện: bước "sinh JQL" thành bước "chọn nguồn".
           ⚠ CSV đọc bằng thư viện chuẩn. `.xlsx` cần thư viện mới → §10.1, hỏi riêng.
           ⚠ Giới hạn SẢN PHẨM phải nói với bên bán: công ty không IT và không dùng SaaS
             có webhook thì KHÔNG có tín hiệu thời gian thực ở Kênh 1 — chỉ đồng bộ định
             kỳ, cộng `/hoi`.

       (3) ADAPTER NHẬN WEBHOOK CỦA SAAS → TIẾN TRÌNH RIÊNG, NGOÀI CORE.
           Adapter kiểm chữ ký THEO KIỂU CỦA NGUỒN, dịch sang JSON chuẩn, gọi core bằng
           khoá `kp_`. Lý do chọn: mỗi SaaS xác thực webhook một kiểu riêng; đặt
           `/inbound/{nguon}` trong core là core phải biết từng nguồn — phá `G1`.

       (4) NỘI DUNG BỊ SỬA Ở NGUỒN → MẨU MỚI, TRỎ VỀ MẨU CŨ. Bản cũ không đổi.
           Khớp `IM-20` (*"nguồn đã đổi = một mẩu MỚI với khoá mới"*), thêm vế còn thiếu:
           liên kết mẩu mới → mẩu cũ. Không có liên kết thì hai phiên bản của cùng một
           comment trông như hai quan sát độc lập, và Path A đếm đôi.
           ⚠ Chưa chốt, và là câu KỸ THUẬT (`J1-1`, agent tự quyết khi build): adapter
             phát hiện "đã sửa" bằng gì (hash nội dung hay mốc updated của nguồn), khoá
             mới đặt theo khuôn nào.

       (5) MẨU BỊ XOÁ Ở NGUỒN → DẤU RIÊNG, LOẠI KHỎI DANH SÁCH DÙNG ĐƯỢC, GIỮ DÒNG.
           🛑 KHÔNG dùng `RutLai` của `AR-m`. `AR-m` chốt dấu rút lại CHỈ NGƯỜI đặt; "xoá ở
             nguồn" là SỰ KIỆN máy quan sát được, không phải lời khẳng định của ai. Khác
             nguồn gốc thì khác dấu (`G6`/`AP3`). Agent viết lẫn hai thứ này ở lượt hỏi
             đầu và tách ra ở lượt hai, trước khi chủ dự án chọn.
           Lý do chọn: khách đã xoá mà mình vẫn gom là giữ thứ khách không muốn giữ. Giữ
           dòng vì `K-B3`/`G5`: lịch sử không bị xoá.
           ⚠ Nên dùng lại khuôn HAI KIỂU của CaseEvidenceReadHandler (danh sách dùng được
             vs danh sách đã rút), không phải một cờ — cùng lý do: quên lọc thì không
             biên dịch được.

       Ba việc nhỏ agent nêu cùng lượt, CHƯA hỏi vì không chặn:
         · `case-status` thêm trường tuỳ chọn `trangThaiNguon` (chuỗi thô). `R5` đòi giữ
           bản thô khi dịch "Done / Đã xử lý / Chờ khách" thành `daXongONguon`. Thêm ô
           thì giữ số phiên bản hợp đồng (docs/19 §6).
         · Sentinel `khong-thuoc-khach-san` là từ vựng ezCloud; công ty không làm khách
           sạn cần tên trung tính (vd `khong-thuoc-don-vi-con`), giữ tên cũ làm bí danh.
           Đổi giá trị hợp đồng nhận vào → câu SẢN PHẨM, gom vào docs/18 khi tới lượt.
         · Gắn mỗi khoá API với một tiền tố nguồn (`jira:*`), để provenance "ai ghi mẩu
           này" đọc từ khoá thay vì tin chuỗi tự khai.

AR-s   Hoàn thiện MỌI ô của sơ đồ luồng tín hiệu — mở lại I1, và bảy câu sinh ra từ đó  ← MỚI
       ✅ CHỐT 2026-10-10 bởi chủ dự án (form, một lượt bốn câu) + ba câu PROPOSED.
       Phiếu: docs/18 NHÓM N. Kế hoạch 11 phase: docs/00 §"Việc tiếp theo". Kỹ thuật: §3 IM-49.
       Sinh từ yêu cầu: *"hoàn thiện nốt code để đáp ứng toàn bộ case trong sơ đồ"* · *"tạm thời
       bỏ qua việc ẩn dữ liệu như tài khoản, mật khẩu"* · *"cứ nhận dữ liệu về là cho vào luồng
       trong sơ đồ để xử lý luôn"*.

       (0) I1-1 → I1-2. Capability 2 (ProcessDefinition/ProcessRun) VÀO phạm vi.
           Lập luận cũ của I1-1 KHÔNG sai: đây vẫn là bước nặng nhất và không chặn tích hợp.
           Thứ đổi là MỤC TIÊU — sơ đồ có ba ô D E F không có dòng code nào.

       (1) N1-2 — CỜ TẮT TẠM MỌI CỔNG CHE, mặc định tắt cờ. Chủ dự án chọn KHÁC ô đề xuất
           (N1-1: chỉ bỏ chặn lô, giữ cổng đường ra).
           🛑 Đây là MỞ LẠI điều kiện chính chủ dự án đặt ngày 2026-09-08 (§5, đoạn
             "đồng ý gửi dữ liệu ticket cho API" — chỉ khi luật che chạy trước mỗi lần gọi),
             và nới AR-o + AR-n B2a CÓ ĐIỀU KIỆN. Điều kiện cũ KHÔNG bị xoá: nó vẫn là trạng
             thái mặc định, và là trạng thái DUY NHẤT được phép ở chế độ shared.
           Ba thứ code phải giữ (IM-49): dấu "đã qua cổng che bên thứ ba" trên từng mẩu vì
           khâu nạp là cửa MỘT CHIỀU · shared từ chối khởi động khi bật cờ · cảnh báo mỗi
           lần khởi động + `daQuaCongChe` ở mọi cửa ra.
       (2) N2-1 — bước xong ⟸ dấu hiệu trong evidence HOẶC lời xác nhận lưu thành EVIDENCE.
           Đúng PR1 (`05`:72-97): xác nhận của người là một loại evidence, không phải cờ.
       (3) N3-1 — đóng L2-a của `04`: 3 nấc đầu hệ thống tự ghi, 2 nấc cuối là lời khai qua
           API phản hồi. Nấc theo từ vựng ĐÃ KHOÁ L2 — không đặt tên mới.
       (4) N4-1 — Kênh 2 không thêm thư viện. A2 và K của sơ đồ VẪN ◐ sau khi làm xong.

       PROPOSED — agent chọn để code chạy được, chủ dự án bác được bất cứ lúc nào:
       (5) N5-1 — PR-c: quy trình ACTIVE bất biến, sửa = phiên bản mới.
       (6) N6-1 — PR-e: ProcessRun giữ phiên bản nó bắt đầu.
       (7) N7-1 — định tuyến bằng model cần cờ riêng + trần tháng + biên lai trước khi gọi.
           Lý do nó là câu SẢN PHẨM chứ không phải kỹ thuật: E3-1 đặt trần cho việc NGƯỜI
           bấm soạn nháp; định tuyến chạy TỰ ĐỘNG trên mọi ticket — một loại chi phí E3
           chưa bao giờ phủ.
```

## `AR-q` · ĐÃ VIẾT ĐỊNH NGHĨA, ĐÃ ĐO — và ✅ **CHỐT 2026-09-15** (đo 2026-09-11)

> ✅ **Chủ dự án chốt 2026-09-15: DÙNG NGUYÊN TRẠNG.** Phiếu ở `docs/14` §A1 (= A1-1).
> Con số chống lưng là **10/10 của NHÓM 2** ở cuối mục này, không phải 8/10 của nhóm 1.
> Tiêu đề cũ ghi `PROPOSED, chờ chủ dự án chốt` — giữ lại trong dòng này để thấy mục đã
> đứng chờ **bốn ngày**, đúng loại độ trễ mà `docs/14` sinh ra để cắt.
> ⚠ Bản vá *"xác nhận sau khi sửa ≠ bước kiểm"* vẫn **chưa được đo**, xem cảnh báo cuối mục.

Định nghĩa + ca biên đã nằm trong `description` của năm ô ở `SopDraftSchema`, kèm **luật
thứ tự xét** (các ô chồng lấn thật; xét 1 `khong-phai-chan-doan` → 2 `buoc-kiem-ngoai-ticket`
→ 3 `ghi-ro-buoc-kiem` → 4 `suy-ra-duoc-nhung-khong-ghi` → 5 `chi-ghi-ket-luan`, dừng ở ô
khớp đầu tiên) và một câu tách trục: **năm ô trả lời "hồ sơ CÓ GHI bước kiểm không", không
trả lời "nguyên nhân có chắc không"** — độ chắc đã có chỗ riêng ở `chungCu` của từng nhánh.

**Ba chỗ lệch của `IM-29` hoá ra là BA nhầm lẫn khác nhau, không phải một:**

```text
ES-341317   câu KẾT LUẬN bị đọc thành bước kiểm
ES-343733   nơi GHI kết luận bị đọc thành nơi KIỂM   (ô này vì thế về 0)
ES-346559   độ chắc của NGUYÊN NHÂN (`toi-suy-ra`) rò sang một ô nói về việc GHI
```

**Phép đo, bốn lượt, mỗi lượt một ngữ cảnh sạch chỉ đọc file payload:**

```text
mốc cũ (IM-29)   bản A vs bản B, ô chỉ có TÊN                     7/10 khớp
vòng 1           định nghĩa CÓ TRÍCH NGUYÊN VĂN ticket nhóm 1    10/10 khớp  ← SỐ BỊ THỔI
vòng 2           định nghĩa viết theo HÌNH DẠNG, không trích       8/10 khớp  ← số dùng được
```

🛑 **Vòng 1 không được đọc là thành công, và bằng chứng là trực tiếp chứ không phải suy
đoán:** một trong hai ngữ cảnh khai rõ nó xử ca khó bằng lý do *"ví dụ đích danh thắng ca
biên tổng quát"* — tức nó **nhận ra ví dụ**, không **áp dụng luật**. Bỏ hết câu trích
nguyên văn thì mức khớp tụt từ 10/10 xuống 8/10. Đây đúng cái bẫy `SecretShapeScanner` đã
ghi: luật sửa theo chính dữ liệu dùng để đo nó thì con số đo được là **cận trên**.
→ Luật mới, đã ghi vào chính file schema: **ca biên phải là HÌNH DẠNG, không được là câu
  nguyên văn của một ticket.**

**Hai ticket còn lệch ở vòng 2 — và cả hai ngữ cảnh đều TỰ NÊU ĐÚNG hai ticket đó là ca
khó của mình, trước khi biết kết quả của nhau:**

```text
ES-346039   ghi-ro-buoc-kiem  ↔  suy-ra-duoc   "em kiểm tra thấy X" — có động từ kiểm
            nhưng phần còn lại là kết luận, và đối tượng thì do HỎI khách mà có
ES-343662   ghi-ro-buoc-kiem  ↔  chi-ghi-ket-luan  ↔  suy-ra-duoc  (ba ô khác nhau!)
```

🎯 **Tín hiệu phụ, có giá trị riêng: phần "ca khó" mà bản nháp tự khai DỰ ĐOÁN ĐÚNG chỗ nó
sẽ lệch.** Hai ngữ cảnh độc lập gọi tên đúng hai ticket rồi lệch đúng ở đó. Nếu giữ được
qua nhóm khác thì đây là một cổng "KHÔNG CHẮC" rẻ hơn nhiều so với đo lại bằng hai lượt —
và nó chạm thẳng vào cổng "KHÔNG BIẾT" mà `docs/13` đòi.

⚠ **`ES-343662` bị VỠ RA BA Ô ở vòng 2 trong khi nó ổn định ở vòng 1 và ở cả A lẫn B.**
Nguyên nhân đã tìm ra: câu "ai làm bước kiểm cũng được" (thêm để gỡ chỗ khách tự thao tác)
nuốt luôn **bước XÁC NHẬN sau khi sửa** — "làm theo hướng dẫn rồi thấy chạy được" không
phải bước chẩn đoán. Đã vá bằng một câu, và `buocXacNhan` vốn đã là chỗ riêng cho nó.
🛑 **Bản vá đó CHƯA ĐƯỢC ĐO.** Con số phải mang đi tiếp là **8/10**, không phải số đo lại
trên cùng 10 ticket sau khi đã chỉnh theo chúng — đo lại ở đây là đo chính mình.

### `AR-q` · PHÉP ĐO TỔNG QUÁT HOÁ — NHÓM 2, 2026-09-11

Nhóm 1 từ vòng 2 đã thành tập **đã-chỉnh-theo**, nên con số ở đó không nói được gì về khả
năng tổng quát hoá. Đo lại trên nhóm **"NCC từ chối payload"** (10 case, 21 mẩu) — nhóm
chưa hề được dùng để viết một chữ nào trong định nghĩa.

```text
nhóm 2, hai ngữ cảnh sạch, cùng payload          10/10 KHỚP TUYỆT ĐỐI
   ghi-ro 6 · suy-ra 2 · ngoai-ticket 2 · chi-ghi-ket-luan 0 · khong-phai-chan-doan 0
   (trùng nhau từng mã case, kể cả hai ô rỗng)
```

🎯 **Đây là con số có nghĩa của `AR-q`, không phải 8/10 của nhóm 1.** Nhóm 2 còn là phép
thử KHÓ chứ không dễ: bản A của nó xếp 8/10 vào "ghi rõ bước kiểm" (nhóm 1 chỉ 4/10), tức
đây đúng chỗ một luật quá chặt sẽ phơi ra to nhất.

⚠ **Một chỗ không so thẳng được, nói ra kẻo đọc nhầm:** nhóm 2 chạy trên schema **mới hơn
một bản vá** so với lúc đo nhóm 1 (bản vá "xác nhận sau khi sửa ≠ bước kiểm"). Hai con số
8/10 và 10/10 vì thế không cùng một dụng cụ. KHÔNG đo lại nhóm 1 bằng bản vá đó: bản vá
sinh ra TỪ chỗ lệch của nhóm 1, đo lại là đo chính mình.

**Chênh với mốc người dựng tay — và nó KHÔNG phải nhiễu, nó là MỘT luật:**

```text
bản A nhóm 2 (quy về bộ 5 ô)   ghi-ro 8 · ngoai-ticket 2 · còn lại 0
hai ngữ cảnh, luật mới         ghi-ro 6 · ngoai-ticket 2 · suy-ra 2
   → ô `buoc-kiem-ngoai-ticket` KHỚP CHÍNH XÁC (2 = 2, cùng hai mã case)
   → lệch đúng 2 ticket, cả hai đi từ `ghi-ro` sang `suy-ra`: ES-345257, ES-345813
```

Cộng với `ES-346039` của nhóm 1, **ba ticket lệch đều lệch cùng một chiều và vì cùng một
lý do**: ticket có **đáp án được dán vào** (mã lỗi NCC nguyên văn, response, câu kết luận
gọi đúng tên trường, ảnh) nhưng **không ai nói đã mở đâu để lấy nó**. Người dựng tay tính
đó là "ghi rõ bước kiểm"; luật hiện tại tính là "suy ra được nhưng không ghi".

✅ **CHỐT 2026-09-11 bởi chủ dự án — `CONFIRMED`. `AR-q` HẾT CÂU MỞ.**
```text
Dán nguyên văn ĐÁP ÁN (mã lỗi / response / kết luận gọi đúng trường) mà không nói
chỗ đã tra — có tính là "ghi rõ bước kiểm" không?
  ✅ KHÔNG TÍNH. Giữ nguyên định nghĩa hiện tại.
     → ô `ghi-ro-buoc-kiem` từ nay chỉ chứa bước kiểm mà người CHƯA BIẾT đáp án lặp
       lại được. Chấp nhận lệch 3/20 so với mốc người dựng tay, có chủ ý.
     → nhánh bị bác: "tính" sẽ về đúng mốc bản A (nhóm 2 thành 8/2/0) nhưng làm ô
       `ghi-ro` gộp cả BIẾT CÁCH HỎI lẫn CHỈ CÓ ĐÁP ÁN — mà tách đúng hai thứ đó là
       việc mà cả phân bố được sinh ra để làm. Mốc khớp người không đáng giá bằng.
     → nhánh bị bác: tách thành ô thứ sáu — phá bộ 5 ô cố định của `IM-27`.
⚠ Hệ quả phải nhớ: ba ticket ES-346039 · ES-345257 · ES-345813 nay thuộc `suy-ra`,
  KHÁC với cả bản A lẫn bản B. Hai file cây dựng tay KHÔNG sửa theo (`IM-27`) — chúng
  là mốc `M2`, và chỗ lệch này giờ là lệch ĐÃ BIẾT LÝ DO, không phải lệch chưa giải.
```

**Hai thứ khác phép đo này cho, ngoài con số:**

```text
· Ô `buoc-kiem-ngoai-ticket` ĐÃ SỐNG LẠI và sống đúng chỗ: 2/10 ở nhóm 2, đúng hai
  case bản A cũng chọn. Ô từng về 0 ở `IM-29` giờ ổn định qua hai nhóm, hai ngữ cảnh.
  Chống lưng thêm: tên ô người dựng tay tự đặt cho nhóm 2 là
  `buoc-kiem-xay-ra-qua-remote-chi-con-ket-luan` — chính họ đã mã hoá đúng luật "kết
  luận nằm trong ticket không kéo nó ra khỏi ô này". Luật đó không phải do tôi bịa ra.
· Tín hiệu "tự khai ca khó" KHÔNG phải thước dự đoán. Ở nhóm 1 nó trỏ đúng hai chỗ sẽ
  lệch; ở nhóm 2 cả hai ngữ cảnh đều nêu ES-343712 là ca khó nhất rồi VẪN xếp giống
  nhau. Nó là cờ CẢNH BÁO, không phải phép đoán chỗ sẽ sai. Đừng dựng cổng trên nó.
```

⚠ Lỗ định nghĩa còn lại mà chính phép đo nêu ra: điều kiện "suy ra được NÓ LÀ GÌ" **không
nói cần cụ thể tới mức nào** — biết LOẠI phép tra là đủ, hay phải biết cả MÀN HÌNH? Ở nhóm
2 nó quyết định ranh giới giữa `suy-ra` và `chi-ghi-ket-luan`, và ô `chi-ghi-ket-luan` về 0
chính vì đọc theo nghĩa lỏng. Chưa vá, vì vá là lại chỉnh theo nhóm 2.

⚠ Và một thứ `AR-q` KHÔNG chứng minh: bốn lượt này đều là ngữ cảnh đọc file, **tuân thủ
schema tự nguyện**. Qua API thì `output_config.format` RÀNG BUỘC. Mức khớp 8/10 nói về
chất lượng ĐỊNH NGHĨA, không nói gì về việc lượt gọi thật có trả đúng hình dạng hay không.

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
