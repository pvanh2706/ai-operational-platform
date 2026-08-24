# 07 — MVP Implementation

## Nhật ký hiện thực · slice đầu: Path A

> **Bắt đầu:** 2026-08-23 · Slice đầu tiên = **Path A đi hết một đường**
> **Cập nhật:** 2026-08-24 — có PostgreSQL thật. `AR-c` ĐÓNG. Sinh `IM-9`..`IM-11`,
> test project đầu tiên, và `TenantConnectionInterceptor` (mắt xích C# ↔ RLS).
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

tests/KnowledgePlatform.Infrastructure.Tests/
    TestDatabase.cs                     fixture — chạy trên PostgreSQL THẬT
    TenantIsolationTests.cs             9 test cách ly tenant

scripts/dev-db-setup.sql                dựng kp_dev + kp_test + role kp_app
```

## Trạng thái verify

```text
✅  dotnet build          toàn solution, 0 error 0 warning
✅  dotnet ef migrations   sinh được
✅  apply migration        PostgreSQL 18.6 local · kp_dev + kp_test · cả 2 migration
✅  RlsGuard chạy thật     PASS trên DB sống, bằng code C# thật (không phải SQL tay)
✅  cách ly tenant         9/9 test xanh, chạy bằng role KHÔNG phải superuser
✅  bộ test có thể ĐỎ      gỡ FORCE khỏi một bảng    → 5 test đỏ
                           gỡ nullif khỏi policy      → 3 test đỏ
```

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

Mười một quyết định dưới đây **suy ra từ** các quyết định domain đã chốt, không phát minh gì mới. Nhưng chúng là lựa chọn, nên ghi lại.

`IM-1`..`IM-8` viết khi chưa có PostgreSQL. `IM-9`..`IM-11` sinh ra từ việc **chạy thật** ngày 2026-08-24 — hai trong ba là thứ đọc SQL không phát hiện được.

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

---

# 4. Chưa build — phần còn lại của slice Path A

```text
· Project host (API / Worker)              CHẶN 3 việc dưới nó — cần một "request"
                                           thật để ITenantContext có gì mà đọc.
                                           Interceptor đã sẵn sàng nhận (IM-10);
                                           chỉ còn thiếu cài đặt đọc tenant từ
                                           tín hiệu của host app.
· Truy vấn "tìm N case cũ liên quan"       Q-C đã chốt là dependency của Cap 3
                                           AR4: Postgres FTS trước
· ISoạnNhápSOP → Anthropic SDK             AR3 interface mỏng · structured outputs
                                           · Batches API (S5: Path A không nhạy latency)
· Luồng duyệt (S7 một hành động)           gọi KnowledgeRecord.Approve
· Đường nhận tín hiệu từ host app          06 §1
· Tính diff(A,B) cho M2
```

**Ranh giới tenant giờ đã ĐÓNG hết một vòng** — từ `ITenantContext` trong C#, qua
interceptor, xuống policy của Postgres, và có test giữ. Mọi thứ trong danh sách trên
xây trên một nền đã được đo, không phải trên một nền được cho là đúng.

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
       Hiện chỉ có mặc định cho máy dev (biến môi trường KP_TEST_DB ghi đè được).
       Cần quyết khi có project host — cùng lúc với việc cài ITenantContext.
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
