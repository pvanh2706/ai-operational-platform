# 06 — MVP Architecture

## AI Operational Knowledge & Process Platform

> **Chốt:** 2026-08-23 · `CONFIRMED` · năm quyết định `AR1`–`AR5`
> **Đây là workstream chốt công nghệ** — `AGENT.md` §10.1. Domain Modeling đã kết thúc.
> **Người quyết:** người dùng. Agent đề xuất kèm đánh đổi và phản biện; mọi lựa chọn dưới đây do người dùng chọn.

---

# 0. File này là gì và không phải gì

**Là:** các quyết định công nghệ và ranh giới kỹ thuật cho MVP, cùng lý do và đánh đổi của từng cái.

**Không phải:** code. Không phải schema cụ thể, không phải danh sách endpoint, không phải cấu trúc project. Đó là Workstream 07.

**Cũng không phải:** kiến trúc cho toàn bộ vision. Chỉ đủ để build first use case, theo đúng luật §6.7.

## Nền tảng — 27 quyết định domain đã chốt

```text
docs/Canonical Case Model v0.2.md   Case domain
docs/04_KNOWLEDGE_MODEL_V0.1.md     Knowledge domain (23 quyết định)
    ★ §3C.5  hình dạng đầy đủ KnowledgeRecord
    ★ §3D.7  BẢNG TỪ VỰNG ĐÃ KHÓA — không định nghĩa lại ở tầng này
docs/05_PROCESS_MODEL_V0.1.md       Process domain (4 quyết định)
docs/02_SUCCESS_METRICS_V1.md       Success Metrics (M1-M4)
```

⚠️ Kiến trúc **không được** định nghĩa lại vocabulary. Bảng khóa duy nhất là `04` §3D.7.

---

# 1. Mô hình tích hợp — thông tin mới, làm rõ D2/D3

Người dùng cung cấp 2026-08-23. Đây là mô tả rõ ràng nhất về hình dạng sản phẩm từ đầu dự án:

```text
Phần mềm có sẵn của khách A ──tín hiệu──┐
   · có issue mới                        │
   · người dùng đổi trạng thái           ├──►  [SẢN PHẨM NÀY]  ──► trả kết quả
   · người dùng hỏi về tài liệu          │      thức tỉnh khi
Phần mềm có sẵn của khách B ──tín hiệu──┘      có tín hiệu

        + cấu hình quét/đồng bộ dữ liệu riêng  (xem AR5)
```

## Ba hệ quả

**(1) Đây là service phản ứng theo SỰ KIỆN, không phải app người dùng đăng nhập vào.**
Ở MVP: không cần quản lý phiên đăng nhập, không cần frontend riêng. Bề mặt là API nhận tín hiệu + một widget nhúng.

**(2) Xác nhận `D2` và `D3` bằng mô tả cụ thể.** `D3` đã ghi: *"interface nhúng nên được thiết kế generic ngay, dù mới có một host app."* Mô hình này chính là interface đó.

**(3) Nó định nghĩa "tenant" một cách cụ thể** — xem `AR2`.

---

# 2. Stack · `CONFIRMED` (AR1)

```text
Backend        C# / .NET
Database       PostgreSQL
File storage   blob storage (Azure Blob / S3 / filesystem cho on-prem)  — xem AR4
Bộ eval        Python, công cụ RIÊNG chạy offline
Widget nhúng   Vue3 + TypeScript
LLM            Anthropic SDK chính thức (package `Anthropic`)            — xem AR3
```

## Vì sao .NET — yếu tố áp đảo là team, không phải ưu điểm ngôn ngữ

```text
1  Team bảo trì sản phẩm này = team đang bảo trì phần mềm có sẵn.
   Khách #0 là công ty của người dùng (D3).
2  Sản phẩm TÍCH HỢP với phần mềm có sẵn (§1). Cùng stack thì phần nhúng,
   auth, deploy dùng lại được.
3  Phần LLM ĐƠN GIẢN — Anthropic có SDK chính thức cho cả 7 ngôn ngữ
   (Python, TypeScript, Java, Go, Ruby, C#, PHP). Không có ngôn ngữ nào
   "làm AI tốt hơn" ở quy mô này.
```

⚠️ **Điểm agent đã sai và phải sửa:** trước khi đọc tài liệu tham chiếu, agent định lập luận *"Python/TS có ecosystem LLM tốt hơn"*. Sai — cả 7 ngôn ngữ có SDK chính thức. Đó là lý do phải đọc thay vì nói theo ký ức.

## Vì sao typing mạnh có lợi ích CỤ THỂ ở dự án này

Nhìn hình dạng đã chốt ở `04` §3C.5: mỗi assertion mang **riêng** `origin · actor · evidence[] · verification`.

```text
Gán sai origin của một assertion — ví dụ assertion do senior tự viết
bị ghi thành AI_INFERENCE — là một LỖI PROVENANCE (vi phạm G6).

Nó KHÔNG làm chương trình crash. Nó nằm im trong dữ liệu cho tới khi
bộ eval phát hiện, hoặc không ai phát hiện.
```

Static typing bắt loại lỗi đó lúc biên dịch. Đây là lợi ích cho **đúng dự án này**, không phải lời khen chung về typing.

## Vì sao PostgreSQL, không phải SQL Server

Team có ops SQL Server sẵn, nên đây là lựa chọn đi ngược thói quen — có hai lý do cụ thể:

```text
1  ĐÚNG HƯỚNG CÔNG TY. Phần mềm mới của công ty đang chuyển sang Postgres.
   Đây LÀ phần mềm mới. Team mới tiếp cận nhưng sẵn sàng học sâu, và
   dự án này quy mô nhỏ ngày đầu → đúng chỗ để học, không phải chỗ đánh cược.

2  ĐƯỜNG NÂNG CẤP MIỄN PHÍ cho chỗ còn chưa chắc (xem AR4):
     full-text search  →  CÓ SẴN trong lõi Postgres
     vector search     →  pgvector là EXTENSION, bật thêm
                          KHÔNG đổi database, KHÔNG thêm service
```

Điểm (2) quan trọng vì `AR4` để mở câu *"có cần vector search không"*. Postgres làm câu đó rẻ để trả lời sau.

## Vì sao quan hệ (không phải document/graph) — lập luận từ domain model

```text
assertion → evidence           NHIỀU-NHIỀU, có THUỘC TÍNH trên liên kết   L3
Knowledge ↔ Knowledge          SUPERSEDES · REFINES · CONTRADICTS          L4
NEEDS_REVIEW, SUPERSEDED       SUY RA bằng truy vấn, không lưu             V3
bước xong / bước hiện tại      SUY RA từ evidence                          PR1
lịch sử, không snapshot                                                    G5
provenance từng assertion                                                   AP3
```

Rất nhiều quan hệ + rất nhiều state suy ra bằng truy vấn = hình dạng của cơ sở dữ liệu **quan hệ**.

- **Document store** → `L3` đòi thuộc tính trên liên kết; phải denormalize, rồi truy vấn suy-ra-state của `V3`/`PR1` thành đắt.
- **Graph DB** → `PROJECT_CONTEXT §20` xếp *"perfect Knowledge Graph"* vào **non-goals**; `D5` xếp hạ tầng graph dựng sớm vào **giàn giáo tạm**. Ở ~10 record không có lý lẽ nào về scale.

## Bộ eval bằng Python KHÔNG buộc cả app phải là Python

`D5 hệ quả 1` đòi bộ eval là first-class. Ecosystem eval của Python giàu nhất. Nhưng bộ eval là **script offline** chạy trên tập case gán nhãn — nó không cần cùng ngôn ngữ với service. Team biết Python "ở mức dùng được" là đủ cho việc này.

→ Chọn C# **không** làm mất lợi thế eval của Python.

---

# 3. Tenant · `CONFIRMED` (AR2) — kèm guardrail mới `G13`

```text
Ranh giới TENANT (giữa các công ty khách hàng)   →  DB, row-level security
Visibility TRONG một tenant (S7 "hẹp nhất")      →  tầng ứng dụng
```

## "Tenant" nghĩa là gì, cụ thể với mô hình §1

**Một tenant = một công ty khách hàng.** Sản phẩm nhận tín hiệu từ nhiều công ty qua **cùng một đường code**.

```text
Khách A và khách B đều là khách sạn. Cùng dùng Traveloka, Agoda.
Cùng gặp những nguyên nhân giống nhau.

Tri thức của A KHÔNG ĐƯỢC hiện ra cho B.
```

Trộn lẫn = không bán được sản phẩm. `D5 hệ quả 3` đã dự đoán chính xác: *"nút cổ chai sẽ dịch từ 'AI có hiểu không' sang 'AI có được phép xem không'."*

## Vì sao thực thi ở tầng DB

Luồng một tín hiệu có **hàng chục truy vấn**, tất cả đi qua cùng đường code cho mọi khách hàng:

```text
MỘT câu WHERE thiếu điều kiện tenant, ở MỘT truy vấn, MỘT lần
   →  tri thức của khách A hiện ra cho khách B
   →  KHÔNG có gì báo lỗi. Nó chỉ trả về dữ liệu.
```

Row-level security làm điều đó **không thể xảy ra** — database tự chặn, kể cả khi lập trình viên quên.

`G7` nói tenant boundary là **nền tảng**. Nền tảng nghĩa là không dựa vào việc không ai quên.

## Vì sao visibility ở tầng ứng dụng

`S7` là quy tắc **suy ra** (*"hẹp nhất trong các nguồn"*), và `Q-D` còn **OPEN** — v2 sẽ tách kết luận khỏi dẫn chứng ở mức từng câu. Nhồi logic sẽ-đổi vào database là tự làm khó.

```text
tenant       cứng, không đổi   →  DB
visibility   mềm, sẽ đổi ở v2  →  ứng dụng
```

## `G13` — Hai chế độ deploy trên MỘT codebase · `CONFIRMED`

Sinh ra từ lo ngại của người dùng: *"phân vân case khách yêu cầu hạ server riêng"*.

**Lo ngại này tự tan, và tin tốt là RLS chính là thứ LÀM ĐƯỢC deploy riêng.**

```text
Bản deploy riêng = CÙNG code, CÙNG schema, chỉ khác cấu hình trỏ vào
database của khách đó — database ấy tình cờ chỉ chứa một tenant.
Không có gì phải sửa.
```

Thứ **sẽ** chặn không phải lựa chọn RLS, mà là:

```text
⛔ hardcode giả định "chỉ có một database dùng chung"
⛔ một control plane bắt buộc phải dùng chung
⛔ tenant được xác định từ biến toàn cục thay vì từ ngữ cảnh request
```

> **`G13`: Sản phẩm phải deploy được ở CẢ HAI chế độ — shared multi-tenant và dedicated single-tenant — trên cùng một codebase. Tenant luôn được xác định từ cấu hình hoặc ngữ cảnh request, KHÔNG BAO GIỜ từ hằng số toàn cục.**

Nhất quán với `D3` (*"nhồi vào sau rất đắt"*) và `G12` (đặc điểm của khách là tham số, không phải hằng số thiết kế). Toàn văn: `AGENT.md` §3.10.

**Phương án bị loại:** DB/schema riêng từng khách ngay từ đầu — trả toàn bộ chi phí migration + ops **hôm nay** cho khách #0 chỉ có **một** tenant, trong khi RLS không chặn việc chuyển sang dedicated sau.

---

# 4. Ranh giới LLM · `CONFIRMED` (AR3)

```text
SDK chính thức Anthropic (package `Anthropic`) + interface MỎNG của mình.
KHÔNG framework. KHÔNG gateway ở MVP.
```

## Bề mặt LLM của MVP chỉ có HAI hàm

```text
PhânLoạiNguyênNhân(bằng chứng của case)
   →  khớp nguyên nhân nào trong tập ~10 đã biết  (04 §3.5, T1)

SoạnNhápSOP(N case liên quan)
   →  bản nháp, evidence link TỪNG PHÁT BIỂU, đánh dấu CONFLICTING  (S8)
```

Hai hàm. Đó là toàn bộ phần AI ở MVP. Phần code nghiệp vụ không bao giờ thấy SDK.

## Vì sao không framework

`D5` xếp vào cột **giàn giáo tạm**: *"prompt phức tạp nhiều tầng"*, *"multi-agent dựng để bù reasoning yếu"*, *"pipeline cắt chunk"*. Đó chính là những thứ framework LLM cung cấp.

```text
Giá trị chính của framework LLM  =  phần retrieval/RAG
04 §3.5 đã kết luận               =  ở ~10 record KHÔNG cần phần đó

→ trả giá abstraction cho đúng phần không dùng
→ và framework mã hoá điểm yếu của model HÔM NAY thành kiến trúc
```

## Vì sao không gateway ở MVP

Gateway (LiteLLM, OpenRouter) cho phép đổi provider không sửa code. Nhưng:

```text
· thêm một hop cho lợi ích chưa cần (một provider)
· gateway thường CHẬM hỗ trợ tính năng mới — mà structured outputs,
  prompt caching, Batches API là đúng ba thứ làm dự án này rẻ và chính xác
· thêm sau RẤT RẺ, nếu interface đã mỏng
```

## Model mặc định

```text
claude-opus-5      1M context   $5  / $25  per MTok    ← mặc định
claude-sonnet-5    1M context   $3  / $15  ($2/$10 khuyến mãi tới 31/08/2026)
claude-haiku-4-5   200K         $1  / $5
```

**Bộ eval là thứ cho phép hạ bậc model một cách AN TOÀN.** Không có eval thì đổi model là đánh cược; có eval thì thử Sonnet 5 trên tập case gán nhãn, chất lượng giữ thì tiết kiệm ~40%.

→ Thứ tự đúng: chạy Opus 5 trước, để eval nói cho biết có hạ được không. Đây là `D5 hệ quả 1` trở thành một quyết định vận hành cụ thể.

⚠️ Lưu ý kỹ thuật cho Workstream 07: trên `claude-opus-5` thinking **bật theo mặc định**, và `max_tokens` là giới hạn cho **thinking + phần trả lời cộng lại**. Đừng đặt `max_tokens` sát mức output mong đợi.

---

# 5. Tài liệu khách nạp · `CONFIRMED` (AR4)

Nguồn dạng **A** (tài liệu tĩnh có phân quyền) — `§7` đã xếp là nguồn MVP thứ hai. Không phải scope creep.

## Ba câu hỏi tách rời

### (a) File nằm ở đâu → blob storage, KHÔNG trong database

```text
Blob storage (Azure Blob / S3 / filesystem cho bản on-prem)
   → file gốc, không đổi, có version

Database chỉ giữ:
   tenant_id · tên file · loại · version · visibility/ACL
   · con trỏ tới blob · thời điểm nạp · SourceReference
```

Đây đúng là `Document (carrier)` mà `S6` đã định nghĩa. Nhồi PDF vào cột DB làm backup phình, tốn RAM, mất khả năng phục hồi từng phần.

### (b) Model đọc tài liệu bằng cách nào → gửi thẳng, KHÔNG dựng pipeline

```text
API Anthropic đọc PDF NGUYÊN BẢN (base64, hoặc qua Files API)
Context 1M token  →  một tài liệu thường nằm trọn trong một request

→ KHÔNG cắt chunk, KHÔNG embedding, KHÔNG vector DB để ĐỌC một tài liệu
```

`D5` xếp *"pipeline cắt chunk, template extraction"* vào **giàn giáo tạm**. Model đọc được PDF trực tiếp thì pipeline bóc tách là công sức bỏ ra để thành nợ.

⚠️ **Ngoại lệ thật:** `.docx` **không** được đọc nguyên bản như PDF. Cần một bước chuyển đổi (docx → text, hoặc docx → PDF). Trong .NET có sẵn thư viện. Phân biệt cho rõ: đó là **một adapter mỏng**, không phải một pipeline — vài chục dòng, không phải một tầng kiến trúc.

### (c) Tìm ĐÚNG tài liệu trong N tài liệu → Postgres full-text search trước

```text
Postgres FTS  →  có sẵn trong lõi, không thêm hạ tầng
pgvector      →  extension, bật khi ĐO ĐƯỢC là FTS không đủ
```

Lý do FTS đủ dùng trước, từ `§8.1-KQ`:

> Tài liệu ở bước B3 là **tài liệu HỆ THỐNG (API / field / behavior)**.

Loại tài liệu đó đầy **tên field, tên API, mã lỗi cụ thể**. Tìm kiếm theo **từ khoá thường THẮNG** tìm kiếm ngữ nghĩa trên nội dung này — vector search mạnh khi câu hỏi diễn đạt khác từ ngữ trong tài liệu, không mạnh hơn khi người ta tìm đúng chữ `booking_status`.

**Điều kiện xem lại:** khi đo được là FTS không đủ. Ghi lại số đo đó là gì.

---

# 6. "Quét dữ liệu riêng" · `CONFIRMED` (AR5)

Người dùng nêu khả năng *"cấu hình để phần mềm này quét dữ liệu riêng"*. Có hai nghĩa rất khác nhau, và §2.3 của `00_CURRENT_STATE` đã **dự đoán trước** sự nhập nhèm này.

```text
NGHĨA 1 — quét để NẠP / ĐỒNG BỘ dữ liệu           ✅ THUỘC MVP
  đọc Jira/tài liệu/email của khách vào hệ thống, phát hiện thay đổi,
  cập nhật index
  → đây là "connector sync" mà §7 đã lên kế hoạch
  → nó chỉ MANG DỮ LIỆU VÀO, không tự quyết định gì

NGHĨA 2 — quét để TỰ TÌM chủ đề cần SOP            ⚪ V2, giữ G11
  job chạy nền, tự đào quy luật, tự quyết chủ đề nào cần tri thức
  → đúng cột phải của G11
  → = Process Discovery + Knowledge Gap Detection
    (PROJECT_CONTEXT §17, đã có nhãn future capability)
```

**Chốt: MVP làm nghĩa 1. Nghĩa 2 để v2. `G11` giữ nguyên.**

§2.3 viết sẵn: *"Sẽ có lúc ai đó đề nghị 'hay là mình tự động phát hiện chủ đề nào cần SOP luôn'. Câu đó nghe rất hợp lý và nó là cột phải. Ghi xuống đây để lần sau có chỗ đối chiếu."* → Cảnh báo đó vừa được dùng đúng mục đích, đúng 2 ngày sau khi viết.

---

# 7. Năm tính năng API map 1:1 vào quyết định domain

Đây là cổ tức của việc làm domain trước kiến trúc. Không phải agent thiết kế ra sự trùng khớp này.

```text
Structured outputs        →  §3.5 "khớp bằng chứng với 1 trong ~10 nguyên nhân"
(output_config.format)       trả về shape ĐÃ VALIDATE, không parse text tự do
                             → đúng là bài toán phân loại, có công cụ sẵn

Prompt caching            →  ~10 KnowledgeRecord là prefix ỔN ĐỊNH
                             đọc cache ~0.1× giá input. Tối thiểu 512 token
                             trên claude-opus-5.
                             → kho tri thức NHỎ hoá ra là LỢI THẾ chi phí

1M context                →  Path A: 20 case + evidence trong MỘT request
                             → §3.5 chuyển từ suy luận thành số đo

Batches API (50% giá)     →  Path A không nhạy latency
                             (S5: "ngân sách chú ý PHÚT — người ta chủ động xin")
                             → nửa giá cho đúng thao tác đắt nhất

inference_geo             →  D1 bán cho doanh nghiệp: ghim vùng chạy inference
                             cho khách đòi data residency
```

## Một trùng khớp đáng ghi

`S5` chia Path A / Path B theo **ngân sách chú ý của người dùng** (phút vs giây). Hoá ra nó chia đúng theo **ranh giới kỹ thuật**:

```text
Path A (người chủ động xin, chờ được)   →  Batches API, nửa giá, chậm
Path B (lúc đóng case, phải nhanh)      →  realtime
```

Một phân chia domain trùng khít một phân chia hạ tầng. Đây là dấu hiệu `S5` cắt đúng khớp tự nhiên.

---

# 8. Decision Register

## `CONFIRMED 2026-08-23`

```text
AR1  Stack: C#/.NET + PostgreSQL. Blob storage cho file. Eval = Python
     riêng (script offline). Widget nhúng = Vue3+TS.
     → yếu tố áp đảo là TEAM, không phải ưu điểm ngôn ngữ
     → cả 7 ngôn ngữ đều có SDK Anthropic chính thức
     → Postgres vì: đúng hướng công ty + FTS có sẵn + pgvector là extension
     → quan hệ (không document/graph) vì L3/L4/V3/PR1/G5/AP3

AR2  tenant → DB (row-level security) · visibility → tầng ứng dụng
     → tenant cứng không đổi (G7 "nền tảng"); visibility mềm, Q-D còn mở
     → KÈM G13: hai chế độ deploy trên MỘT codebase (AGENT.md §3.10)

AR3  SDK chính thức + interface MỎNG hai hàm. Không framework, không gateway.
     → D5 xếp thẳng framework LLM vào giàn giáo tạm
     → bề mặt LLM của MVP chỉ có 2 hàm
     → model mặc định claude-opus-5; eval là thứ cho phép hạ bậc an toàn

AR4  Tài liệu: blob storage (file) + DB (metadata, Document carrier per S6)
     → model đọc PDF NGUYÊN BẢN, không pipeline (D5)
     → .docx qua adapter chuyển đổi MỎNG (ngoại lệ thật)
     → tìm tài liệu bằng Postgres FTS trước; pgvector khi ĐO ĐƯỢC là không đủ

AR5  "Quét dữ liệu riêng": nghĩa NẠP/ĐỒNG BỘ thuộc MVP;
     nghĩa TỰ TÌM CHỦ ĐỀ để v2. G11 giữ nguyên.

§1   Mô hình tích hợp: service phản ứng theo SỰ KIỆN, không phải app
     người dùng đăng nhập. Xác nhận D2 + D3.
```

## Kế thừa — không mở lại ở đây

```text
04 §3D.7   BẢNG TỪ VỰNG ĐÃ KHÓA — kiến trúc KHÔNG định nghĩa lại
04 §3C.5   hình dạng KnowledgeRecord
05 §5      hình dạng ProcessDefinition / ProcessRun
02_SUCCESS_METRICS §4   metric đòi dữ liệu gì
D1 D3 G7   multi-tenant từ ngày đầu
D5         "model mạnh gấp 10 thì cái này thành giá trị hơn hay thành rác?"
G11 G12    không phình capability · tỉ trọng khách là tham số
```

---

# 9. Còn `OPEN`

```text
§8.2   CHƯA CHẠY. Quyết định 04 §3.5 (bài toán 1: phân loại hay tìm kiếm).
       ⚠ Nó KHÔNG trả lời bài toán 2 (tìm tài liệu) — xem R-A1 ở 04 §3.5.
AR4-a  Ngưỡng nào thì FTS không đủ và cần pgvector? Cần số đo thật.
AR4-b  Khách thực tế nạp bao nhiêu tài liệu, loại gì? Chưa đếm.
QM-1   Ngưỡng của Success Metrics. Cần chạy thật vài tuần.
Q-D    Visibility mức từng câu + redaction → v2
Q-G    Ai có quyền verify Knowledge (đã thu hẹp bởi S7)
Q-H    AI có được suggest update knowledge đã ACTIVE?
Q-I    Vai trò Secondary Persona L3 (gắn QM-4)
```

Không câu nào chặn việc bắt đầu Workstream 07.

---

# 10. Bước tiếp theo: `07 — MVP Implementation`

> `AGENT.md` §10.1: Workstream 07 **chỉ bắt đầu sau khi người dùng đã chốt công nghệ.** Điều kiện đó giờ đã thoả — `AR1`-`AR5`.

## Ràng buộc mang sang

```text
1  Bảng từ vựng khóa ở 04 §3D.7 — dùng CHUNG, không định nghĩa lại
2  G13 — tenant từ cấu hình/request, KHÔNG từ hằng số toàn cục.
       Kiểm tra điều này ở review, không để phát hiện sau.
3  AP3 — origin/actor/evidence/verification gắn ở TỪNG ASSERTION.
       Đây là chỗ dễ sai im lặng nhất (lỗi provenance, vi phạm G6).
4  V3, PR1 — NEEDS_REVIEW, SUPERSEDED, "bước xong", "bước hiện tại"
       là SUY RA, không lưu. Đừng thêm cột cờ.
5  M2 — cần giữ CẢ HAI bản nháp Path A (trước và sau khi người sửa),
       vì diff(A,B) là thước đo tháng đầu VÀ là nhãn eval.
6  D5 — mọi thứ build ra phải trả lời được: "model mạnh gấp 10 thì
       cái này thành giá trị hơn hay thành rác?"
```

## Việc nên làm song song, không thuộc implementation

```text
§8.2   Đếm case OTA, bản nhẹ. Luật quyết định đã chốt (≤15 / ≥40 / ở giữa).
AR4-b  Đếm khách thực tế có bao nhiêu tài liệu, loại gì.
QM-1   Chạy thật vài tuần rồi mới đặt ngưỡng Success Metrics.
```

---

# Nguyên tắc cốt lõi của MVP Architecture

> **Kiến trúc phục vụ domain model, không định nghĩa lại nó.**

Và ba lựa chọn khó đảo đã được quyết theo cùng một logic:

```text
Ngôn ngữ    →  team bảo trì được cái gì        (không phải ưu điểm trên giấy)
Database    →  hình dạng dữ liệu đòi gì        (quan hệ, vì L3/L4/V3/PR1)
Ranh giới LLM →  D5: cái gì thành nợ khi model mạnh lên
```

Ba thứ còn lại — engine cụ thể, chỗ đặt file, cơ chế tìm kiếm — đều có đường nâng cấp rẻ nếu chọn sai.
