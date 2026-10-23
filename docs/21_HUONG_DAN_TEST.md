# 21 — HƯỚNG DẪN TEST: cách chạy, ý nghĩa và luồng chạy của từng bước

> **Viết 2026-10-20.** Cho người cần **tự tay kiểm** sản phẩm trên máy local: chạy cái gì, bấm gì, mỗi bước đang kiểm
> điều gì, dữ liệu đi qua đâu, và thấy gì thì là đúng.
>
> Tài liệu này mô tả **cách test**, không mô tả thiết kế. Lý do của từng quyết định nằm ở `docs/18` (nhóm O, P) và
> `docs/07` §3 (`IM-50` … `IM-57`). Đổi cách chạy thì sửa file này cùng commit.

---

## Mục lục

1. [Có những loại test nào](#1-có-những-loại-test-nào)
2. [Chuẩn bị một lần cho mỗi máy](#2-chuẩn-bị-một-lần-cho-mỗi-máy)
3. [Khởi động: `chay-thu.bat`](#3-khởi-động-chay-thubat)
4. [Bức tranh: ai nói chuyện với ai](#4-bức-tranh-ai-nói-chuyện-với-ai)
5. [Test giả lập hai luồng — từng bước](#5-test-giả-lập-hai-luồng--từng-bước)
6. [Đọc kết quả: bản đồ lô và màn dòng chảy](#6-đọc-kết-quả-bản-đồ-lô-và-màn-dòng-chảy)
7. [Hai cách gán AI: File và API](#7-hai-cách-gán-ai-file-và-api) — chọn ở màn **Cấu hình AI**
   - [7a. Nạp & hỏi: một mã issue hoặc một file](#7a-nạp--hỏi-một-mã-issue-hoặc-một-file)
   - [7b. Phần mềm khách: gửi tín hiệu, nhận kết quả về](#7b-phần-mềm-khách-gửi-tín-hiệu-nhận-kết-quả-về)
   - [7c. Tự trải nghiệm: kịch bản, bản nháp SOP, phiên & tổng kết](#7c-tự-trải-nghiệm-kịch-bản-bản-nháp-sop-phiên--tổng-kết)
8. [Các phép kiểm tự động](#8-các-phép-kiểm-tự-động)
9. [Dọn dẹp và làm lại từ đầu](#9-dọn-dẹp-và-làm-lại-từ-đầu)
10. [Xử lý sự cố](#10-xử-lý-sự-cố)
11. [Test này KHÔNG chứng minh được gì](#11-test-này-không-chứng-minh-được-gì)

---

## 1. Có những loại test nào

| Loại | Chạy bằng | Kiểm điều gì | Khi nào chạy |
|---|---|---|---|
| **Giả lập hai luồng** (thủ công, có giao diện) | `chay-thu.bat` → trình duyệt | Cả vòng sản phẩm chạy được từ tín hiệu tới gợi ý, với dữ liệu bịa có đáp án | Muốn **nhìn** hệ thống làm việc; sau khi đổi luồng nghiệp vụ |
| **Nạp & hỏi · Phần mềm khách** (thủ công, có giao diện) | `chay-thu.bat` → `/console/nap-hoi`, `/console/phan-mem-khach` | Nạp một issue/file rồi hỏi tự do về nó; gửi tín hiệu như phần mềm khách và nhận kết quả gửi về | Sau khi đổi Kênh 1, Kênh 2, webhook, hoặc đường hỏi đáp — §7a, §7b |
| **Bộ test C#** | `dotnet test src\KnowledgePlatform.slnx` | Từng LUẬT của domain + hạ tầng + HTTP, trên PostgreSQL thật | Sau mỗi lần sửa code C# |
| **Kiểm đầu-cuối** | `python scripts\kiem_dau_cuoi.py` | Mọi endpoint trong hợp đồng còn gọi được trên app ĐANG CHẠY | Trước khi commit thay đổi chạm API |
| **Kiểm hai khách** | `python scripts\kiem_hai_khach.py` | Dựng được khách thứ hai mà không sửa code; hai khách không lẫn dữ liệu | Khi đụng tới tenant, xác thực, onboard |
| **Kiểm sơ đồ console** | `npm run kiem:so-do` | Cách console tô các ô của sơ đồ luồng đúng trên dữ liệu thật | Sau khi sửa `console/src/soDo.ts` |
| **Tự trải nghiệm theo kịch bản** (thủ công, có giao diện) | `/console/kich-ban` | Đọc đủ hướng dẫn + dẫn chứng, đọc/sửa/duyệt bản nháp, lưu lượt + đánh giá, tổng kết, xuất | Khi muốn tự đánh giá kết quả — §7c |
| **Kiểm phiên trải nghiệm** | `npm run kiem:phien` · `python scripts\giao-dien\kiem_phien_trai_nghiem.py` | Thống kê đúng mẫu số, lịch sử sửa giữ bản gốc; nơi lưu tách ngữ cảnh, lượt không đè lượt, từ chối bí mật | Sau khi sửa `phien.ts`, `doc.ts`, `phien_trai_nghiem.py` |

Phần lớn tài liệu này nói về **giả lập hai luồng** — loại duy nhất cho người test thấy được dữ liệu đi qua từng ô.
Các phép kiểm tự động tóm ở §8.

---

## 2. Chuẩn bị một lần cho mỗi máy

Máy dev hiện tại **đã có đủ** — bỏ qua mục này. Máy mới thì làm theo thứ tự:

```text
cần cài   .NET 10 SDK · Python 3.12 · PostgreSQL 18 · Node 22 (chỉ để build console)
```

```bat
rem 1. Role kp_app + 3 database dev/test (cần superuser postgres)
psql -U postgres -h localhost -f scripts\dev-db-setup.sql

rem 2. Schema + khách hàng cho kp_dev
dotnet tool restore
dotnet ef database update --project src\KnowledgePlatform.Infrastructure --startup-project src\KnowledgePlatform.Infrastructure --connection "Host=localhost;Port=5432;Database=kp_dev;Username=kp_app;Password=123456"
psql -U kp_app -h localhost -d kp_dev -f scripts\dev-seed-tenant.sql

rem 3. Database RIÊNG cho giả lập (kp_gialap) — xem đầu file scripts\gia-lap-db-setup.sql
psql -U postgres -h localhost -f scripts\gia-lap-db-setup.sql
dotnet ef database update --project src\KnowledgePlatform.Infrastructure --startup-project src\KnowledgePlatform.Infrastructure --connection "Host=localhost;Port=5432;Database=kp_gialap;Username=kp_app;Password=123456"
psql -U kp_app -h localhost -d kp_gialap -f scripts\gia-lap-seed-tenant.sql
```

**Vì sao giả lập có database riêng** (`O6-1`): host chỉ đọc MỘT taxonomy — 19 nhóm hoá đơn thật — nên ticket giả buộc
phải rơi vào nhóm THẬT. Duyệt quy trình giả cho nhóm X trong `kp_dev` sẽ đẩy quy trình thật của nhóm X sang
DEPRECATED, và SOP giả sẽ trả lời ticket thật. Tách database thì không có chuyện đó.

**Khoá model (chỉ cần cho chế độ API):** chép `appsettings.Local.example.json` thành `appsettings.Local.json` ở gốc repo,
điền `Anthropic.ApiKey` và/hoặc `OpenAI.ApiKey` + **`OpenAI.Model`** (OpenAI KHÔNG có model mặc định — `Q2-1`; thiếu model
thì OpenAI coi như chưa dùng được). File này đã nằm trong `.gitignore` — host TỪ CHỐI đọc nó nếu git không chặn. Muốn định tuyến
bằng model thì thêm `"DinhTuyen": { "GoiModel": true }` (tính tiền MỖI ticket mới). Không có khoá vẫn test được đủ hai
luồng bằng chế độ **File**.

---

## 3. Khởi động: `chay-thu.bat`

**Nhấn đúp `chay-thu.bat` ở thư mục gốc.** Tắt hết: nhấn đúp **`dung-thu.bat`**.

`chay-thu.bat` chạy 5 bước, in ra từng bước:

| Bước | Làm gì | Vì sao |
|---|---|---|
| **1/5** Kiểm điều kiện | Có `dotnet`, `python`; PostgreSQL trả lời ở cổng 5432 | Thiếu một thứ thì dừng NGAY và nói thiếu gì, thay vì để một cửa sổ chết sau đó mà không ai biết vì sao |
| **2/5** Build app .NET | `dotnet build src\KnowledgePlatform.Api` | App giả lập, app dev và các nút xuất/nạp file đều chạy file `.exe` đã build. ⚠ App đang chạy thì BỎ QUA build (app giữ khoá file DLL) — sửa code xong phải chạy `dung-thu.bat` trước |
| **3/5** Console Vue | Chỉ build khi chưa có `scripts\giao-dien\console\dist` và máy có Node | Lần sau không cần Node. Sửa code Vue thì chạy `scripts\giao-dien\build-console.bat` |
| **4/5** Mở các thành phần | Ba cửa sổ "KP …" (bảng dưới) | Thành phần nào đang nghe cổng rồi thì KHÔNG mở lại — chạy file nhiều lần vô hại |
| **5/5** Chờ sẵn sàng | Gọi `/health/ready` của app giả lập tối đa 60 giây, rồi mở trình duyệt | `/health/ready` chạm database và kiểm ranh giới tenant — "sẵn sàng" ở đây nghĩa là dữ liệu đọc được thật, không chỉ là tiến trình còn sống |

Ba cửa sổ được mở:

| Cửa sổ | File | Cổng | Database · khách hàng | Dùng để |
|---|---|---|---|---|
| **KP app giả lập :5120** | `scripts\giao-dien\run-gia-lap-app.bat` | 5120 | `kp_gialap` · `gia-lap` | Test giả lập — dữ liệu bịa, làm hỏng thoải mái |
| **KP app dev :5119** | `scripts\giao-dien\run-dev-app.bat` | 5119 | `kp_dev` · `dev-tenant` | Xem 32 case fixture Jira; chạy `kiem_dau_cuoi.py` |
| **KP giao diện :8791** | `scripts\giao-dien\run-ui.bat` | 8791 | — | Console + trang giả lập; chỉ nghe `127.0.0.1` |

`run-gia-lap-app.bat` in `--kiem-cau-hinh` trước khi chạy app. Đọc hai mục **"Khoá Anthropic"** và **"Định tuyến ticket
mới bằng model"** để biết chế độ API có dùng được không. Lệnh này không bao giờ in khoá.

---

## 4. Bức tranh: ai nói chuyện với ai

```text
 Trình duyệt ─────► ui_server.py (:8791) ──── /app/5120/… ────► App giả lập (:5120) ──► PostgreSQL kp_gialap
  /console            · phục vụ console Vue                        KnowledgePlatform.Api
  /console/gia-lap    · CHUYỂN TIẾP lời gọi API sang app            │
                      · chạy lệnh CLI xuất/nạp file thay bạn ───────┤  (cùng .exe, cùng cấu hình kp_gialap)
                      · ghi file trao đổi với AI vào gia-lap-io\    │
                                                                    └──► Anthropic  (CHỈ khi chọn chế độ API,
                                     └── /app/5119/… ────► App dev (:5119) ──► kp_dev        hoặc bấm Thử khoá)
```

Ba điều cần nhớ khi đọc kết quả test:

1. **Mọi luật nằm trong app .NET.** `ui_server.py` không quyết định gì; nó chỉ đóng vai phần mềm của khách (gửi tín
   hiệu), đóng vai người vận hành (bấm duyệt, xác nhận bước), và chạy hộ lệnh CLI. Một kết quả sai trên màn hình là
   sai của app — trừ khi lỗi nằm ở cách console *vẽ* (xem `npm run kiem:so-do`).
2. **Trang giả lập chỉ đi qua đường sản phẩm có sẵn** — HTTP của app hoặc lệnh CLI của chính app. Nó không ghi thẳng
   vào database, không tự dựng kết quả. Thấy được trên trang nghĩa là sản phẩm làm được.
3. **Mở màn xem không bao giờ gọi model.** Danh sách case, dòng chảy, bản đồ lô đều đọc bằng đường xem (`XemAsync`):
   không gọi Anthropic, không ghi biên lai. Chỉ các nút có chữ **API** (và nút **Thử khoá**) mới ra mạng.

---

## 5. Test giả lập hai luồng — từng bước

Mở **http://127.0.0.1:8791/console/gia-lap**. Trang có 6 bước, đánh số 0–5.

**Hai luồng đang được test:**

```text
LUỒNG XUÔI   một ticket đến → hệ thống nhận ra nó thuộc nhóm nào → đang ở bước nào của quy trình → đưa gợi ý
             → ghi lại người có dùng gợi ý không                                  (bước 1 → 2 → 5)
LUỒNG NGƯỢC  gom các case cũ của một nhóm → AI soạn bản nháp quy trình (SOP) → người duyệt
             → từ đó luồng xuôi mới có tri thức để trả lời                           (bước 3 → 4)
```

Luồng ngược là **nguồn tri thức** của luồng xuôi. Trước bước 4, luồng xuôi chỉ có thể trả **KHÔNG BIẾT** — và đó là
câu trả lời đúng, không phải lỗi.

### Bước 0 · Kiểm tra & chọn cách gán AI

**Ý nghĩa:** biết trước mình đang test trong điều kiện nào, để không đọc nhầm kết quả về sau.

**Luồng chạy:**

```text
bấm "Kiểm lại"   ui_server ── chạy KnowledgePlatform.Api.exe --kiem-cau-hinh (cấu hình kp_gialap)
                 ui_server ── GET /app/5120/health/ready
bấm "Thử khoá"   trình duyệt → ui_server → POST :5120/internal/thu-khoa-model
                 → app hỏi Anthropic GET /v1/models/claude-opus-5 bằng ĐÚNG khoá app đang dùng — MIỄN PHÍ
```

**Đọc kết quả:**

| Thấy | Nghĩa |
|---|---|
| ✓ App giả lập đang chạy | app 5120 sống và ranh giới tenant còn nguyên |
| ✓ Có khoá / ○ Không có khoá | host có tìm thấy khoá không — CHƯA nói khoá dùng được |
| ✓/○ `DinhTuyen:GoiModel` | định tuyến bằng model có bật không — tắt thì định tuyến API trả KHÔNG BIẾT cho ticket mới |
| Thử khoá: **✓ Khoá dùng được** | Anthropic nhận khoá VÀ có model. ⚠ KHÔNG nói còn credit — đường miễn phí không đọc số dư |
| Thử khoá: **Anthropic TỪ CHỐI khoá** | khoá sai / thu hồi / chép thiếu. Trang tự chuyển hai bước về **File** và khoá chế độ API |
| Thử khoá: **Không thử được** | lỗi mạng / máy chủ Anthropic — KHÔNG kết luận gì về khoá |

**Chọn chế độ** cho hai bước có AI: *Định tuyến* (luồng xuôi) và *Soạn nháp* (luồng ngược) — mỗi bước chọn **API**
hoặc **File** độc lập. Lựa chọn được nhớ khi tải lại trang. Chi tiết hai chế độ: §7.

### Bước 1 · Sinh lô dữ liệu giả & gửi vào Kênh 1

**Ý nghĩa:** đóng vai **phần mềm của khách** báo "có việc mới". Kiểm ô **A → B → C** của sơ đồ: tín hiệu vào được, gắn
đúng khách hàng, tạo case, gửi lại không sinh case trùng.

**Dữ liệu:** 11 ticket bịa trong `scripts\giao-dien\gia_lap_du_lieu.py`, rải trên 4 nhóm thật + 1 ticket mơ hồ:

```text
001–003  Phân quyền & ký hiệu hoá đơn chưa sẵn sàng
004–006  NCC từ chối payload — tra mã lỗi, sửa dữ liệu nguồn, phát hành lại
007–008  Ngày làm việc/kiểm toán quyết định ngày trên hoá đơn
009–010  Hoá đơn đoàn lớn/nhiều giao dịch — timeout, treo Đang xử lý
011      "Khách báo hoá đơn có vấn đề" — đáp án là KHONG-BIET (không đủ thông tin)
```
Mỗi ticket có 2–3 mẩu evidence: mẩu ĐẦU là thứ có lúc ticket vừa đến (định tuyến chỉ được đọc mẩu này — `docs/13` §1),
các mẩu sau là chẩn đoán + cách sửa (luồng ngược đọc chúng để soạn SOP). Mỗi ticket mang sẵn **nhóm đúng** để chấm AI.

**Luồng chạy:**

```text
ui_server ── POST :5120/signals/case-observed   (11 case, khoá gialap:<lô>-001 … -011)
          ── POST :5120/signals/case-evidence   (32 mẩu, mỗi mẩu khai mã khách sạn GL-KS-0x — luật AR-l)
          ── ghi gia-lap-io\<lô>\lo.json        (để tải lại trang không mất lô)
```

**Mong đợi:** "tạo 11 case, 32 mẩu evidence". Bản đồ lô hiện 11 hàng, mỗi hàng **A B C xanh, ĐT cam** (chưa định tuyến,
nguồn "model chưa hỏi" nếu bật model), các ô sau xám.

### Bước 2 · Luồng xuôi — định tuyến

**Ý nghĩa:** kiểm ô **ĐT** — hệ thống có nhận ra ticket thuộc nhóm nguyên nhân nào không. Đây là bước có AI của luồng
xuôi, và là bước quyết định mọi thứ phía sau: sai nhóm thì quy trình sai, tri thức sai.

**Thứ tự tra của app** (giống hệt khi phần mềm khách hỏi thật):

```text
1. kho NGƯỜI NẠP     (kết quả do người/AI-qua-file nạp vào)      → nguồn "người nạp"
2. bảng ĐÃ ĐO        (150 ticket thật đã chấm, docs/13)           → nguồn "bảng đo"   — ticket giả luôn KHÔNG có ở đây
3. kết quả MODEL đã lưu                                          → nguồn "model"
4. gọi MODEL         (chỉ khi có khoá + GoiModel bật)            → nguồn "model"     — tính tiền
   không tầng nào biết → KHÔNG BIẾT
```

**Chế độ API — luồng chạy:**

```text
bấm "Định tuyến cả lô bằng API"
ui_server ── POST :5120/hoi {maTicket} × 11 ──► app tra 4 tầng ──► tầng 4 gọi claude-opus-5 (1 lượt/ticket)
          app ghi biên lai MỖI lượt gọi (kể cả lượt hỏng) TRƯỚC khi gọi; model tự chấm độ tin < 0,9 → KHÔNG BIẾT
```

**Chế độ File — luồng chạy:**

```text
a · "Xuất prompt ra file"
    ui_server ── KnowledgePlatform.Api.exe --xuat-dinh-tuyen gia-lap <file> 11 gialap:<lô>-
    → gia-lap-io\<lô>\dinh-tuyen.prompt.json   (CHỈ ticket của lô này, đã qua cổng che, kèm taxonomy + hướng dẫn)
b · bạn đưa file cho Claude Code / Codex (trang hiện sẵn câu lệnh) → AI ghi dinh-tuyen.ketqua.json
c · "Nạp kết quả"  (dán JSON, hoặc để trống để đọc file kết quả)
    ui_server ── gỡ hàng rào ``` → KnowledgePlatform.Api.exe --nap-dinh-tuyen gia-lap <file> "<người>"
              ── POST :5120/hoi × 11 để hiện kết quả
```

**Mong đợi:**

- Bảng chấm: **đúng / sai nhóm / không biết** so với đáp án bịa.
- Chế độ File: dòng nào AI viết tên nhóm KHÔNG chép nguyên từ taxonomy thì **bị từ chối và báo rõ dòng nào** trong
  "Báo cáo lệnh nạp" — không sửa hộ. Ticket đó thành KHÔNG BIẾT.
- Bản đồ lô: ticket có nhóm sáng tới **ĐT**; ô **D** cam nếu nhóm chưa có quy trình được duyệt (lần đầu thì đúng là
  vậy). Ticket 011 dừng ở **ĐT**.
- 🎯 Nếu một nhóm ĐÃ có quy trình + SOP được duyệt từ lô trước, ticket của nhóm đó trong lô mới sáng **A → J** ngay
  sau bước này — tri thức của lô trước trả lời lô sau.

### Bước 3 · Luồng ngược — soạn nháp SOP cho một nhóm

**Ý nghĩa:** kiểm đường **tạo ra tri thức**: gom các case đã biết thuộc nhóm X → AI viết bản nháp quy trình (các bước
kiểm, nhánh, bảng tra mã lỗi, bước sửa) → nạp vào kho ở trạng thái **DRAFT**. Nạp KHÔNG phải duyệt.

**Gom case nào:** mọi case của nhóm trong **toàn kho** — không chỉ lô vừa sinh — từ kho người nạp, cộng case do model
định tuyến qua ngưỡng (`O5-1`). Nên chọn nhóm đông nhất; lô thứ hai trở đi thường gom được case của cả các lô trước.

**Chế độ API — luồng chạy:**

```text
bấm "Soạn nháp bằng API"
ui_server ── POST :5120/knowledge/soan-nhap {nhom, nguoiYeuCau}
app ── gom case (tối đa 30) → cổng che → ghi biên lai → gọi claude-opus-5 MỘT lượt → nạp bản nháp (DRAFT)
     → sinh: N bản ghi tri thức (mỗi bước sửa = một nguyên nhân) + 1 quy trình DRAFT
```

**Chế độ File — luồng chạy:**

```text
a · ui_server ── KnowledgePlatform.Api.exe --xuat-soan-nhap gia-lap "<nhóm>" <file>
    → gia-lap-io\<lô>\soan-nhap.prompt.json   (system + user + schema đầu ra; mọi mẩu evidence của các case)
b · AI đọc, ghi soan-nhap.ketqua.json         (một bản nháp SOP đúng schemaDauRa)
c · ui_server ── POST :5120/knowledge/sop-draft  (cùng bộ kiểm mà đường API đi qua)
```

**Mong đợi:**

- "tạo N bản ghi tri thức", mỗi bản ghi tên theo **nguyên nhân** (không phải theo cách sửa), trạng thái DRAFT.
- Quy trình DRAFT, M bước, kèm cảnh báo *"chưa có dấu hiệu nào cho bước nào — chỉ xong được bằng lời xác nhận"* (đúng
  thiết kế: bản nháp không có trường dấu hiệu).
- "dẫn chứng nối được" > 0 và "mã mơ hồ" = 0 — mỗi phát biểu trỏ về mẩu evidence nguồn.
- ⚠ Bản ghi tên **"KHÔNG CÓ ĐƯỜNG NHẬN RA"** là bước sửa không cách nào nhận ra — bản nháp có lỗ. Từ `P6-1`, dòng bảng tra
  mã lỗi cũng là một cách nhận ra, nên bản nháp dạng bảng không còn ra tên này.
- Lỗi thường gặp ở chế độ API: **409** kèm `lyDo` — `khoa-bi-tu-choi`, `model-khong-ton-tai`, `het-credit`,
  `chua-co-credit`, `vuot-tran-thang`, `nhom-khong-co-case`. Mỗi lý do kèm việc phải làm. ⚠ Lỗi TẠM THỜI của Anthropic
  (429 quá tải, 5xx, mất mạng) vẫn ra **500** — cố ý: chúng cần thử lại, không cần sửa cấu hình (`IM-56`).

### Bước 4 · Luồng ngược — duyệt tri thức + quy trình

**Ý nghĩa:** kiểm cổng **người công nhận** (`D4`): AI soạn, người duyệt. Chỉ thứ đã duyệt mới được trả cho người làm
thật. Duyệt kèm quyền xem (`noi-bo`) và lời khai đã soát dữ liệu thương mại (`AR-n`) trong MỘT hành động.

**Luồng chạy:**

```text
ui_server ── POST :5120/knowledge/{id}/approve  × mỗi bản ghi tri thức
          ── POST :5120/processes/{id}/approve  × quy trình
          body: { nguoiDuyet, phamViXem: "noi-bo", xacNhanDuLieuThuongMai: true }
```

**Mong đợi:** mọi dòng HTTP 200, trạng thái **ACTIVE**. Duyệt một phiên bản quy trình mới thì phiên bản cũ của cùng nhóm
thành DEPRECATED (`N6-1`) — case đang chạy dở vẫn giữ phiên bản nó bắt đầu.

⚠ **Từ 2026-10-23** nút ở bước 4 tên là **"Duyệt nhanh (dữ liệu giả)"** — nó tự khai AR-n hộ bạn, nên CHỈ dùng cho dữ liệu
bịa. Cách đề xuất: nút **"Đọc · sửa · duyệt bản nháp"** ở bước 3 → màn Bản nháp SOP (§7c): đọc nội dung + dẫn chứng, sửa,
tự chọn phạm vi xem, tự tích lời khai, duyệt từng đối tượng.

### Bước 5 · Đóng vòng — hỏi lại, xác nhận bước, phản hồi

**Ý nghĩa:** chạy lại **luồng xuôi** sau khi luồng ngược đã tạo tri thức. Kiểm các ô **D → J**: khớp quy trình, suy ra
bước hiện tại từ bằng chứng, tra tri thức theo bước, trả gợi ý kèm dẫn chứng, ghi lại người có dùng không.

**Luồng chạy:**

```text
"Hỏi lại các ticket của nhóm"
  ui_server ── POST :5120/hoi {maTicket} cho từng ticket của nhóm
  app ── định tuyến → khớp quy trình ACTIVE của nhóm (tạo LƯỢT CHẠY nếu chưa có)
      ── suy bước hiện tại = bước chưa xong đầu tiên  (KHÔNG lưu cờ tiến độ — PR1)
      ── tra tri thức theo CHỦ ĐỀ của bước đó → trả hướng dẫn + mức kiểm chứng + dẫn chứng
      ── ghi LƯỢT GỢI Ý: nấc RETRIEVED + SHOWN

"Xác nhận <bước> xong"
  ui_server ── POST :5120/quy-trinh/xac-nhan-buoc {khoaNguon, maBuoc, nguoiXacNhan, subTenantCode}
  app ── lời xác nhận được lưu thành MỘT MẨU EVIDENCE có tác giả (không phải cờ) → bước hiện tại nhảy sang bước sau

"Khai: đã dùng gợi ý"
  ui_server ── POST :5120/goi-y/{maLuotGoiY}/phan-hoi {nac: "used", datDuoc: true, nguoiKhai}
  app ── ghi nấc USED (LỜI KHAI) → phễu 5 nấc ở GET /internal/goi-y/thong-ke
```

**Mong đợi:**

- Mọi ticket của nhóm: **"biết"**, có bước hiện tại (thường là bước đầu, `K1`), số hướng dẫn > 0.
- Bấm "Xác nhận K1 xong" → *"bước hiện tại giờ là K2"* (hoặc bước kế). Hỏi lại thì vẫn K2 — tiến độ suy từ evidence
  nên bền qua mọi lần hỏi.
- Bản đồ lô: ticket của nhóm sáng **A → J**.
- Bấm **"n hướng dẫn ▸"**: đọc được nội dung từng hướng dẫn, mức kiểm chứng, dẫn chứng (§7c). Mở xem không đổi bước hiện tại.
- Mỗi ticket hỏi lại = một lượt trong phiên đang chọn; đánh giá ngay dưới phần đọc. Xem tổng ở `/console/phien`.

---

## 6. Đọc kết quả: bản đồ lô và màn dòng chảy

### Bản đồ lô (dưới bước 1)

Mỗi hàng là một ticket, mỗi cột là một ô của sơ đồ luồng. Tô bằng **đúng hàm** của màn dòng chảy (`soDo.ts`), nên hai
màn không thể nói khác nhau về cùng một case. Tự tô lại sau mỗi bước, không tốn gì (đường xem). Rê chuột vào ô để xem
lý do; bấm hàng để mở dòng chảy.

| Màu | Nghĩa |
|---|---|
| 🟩 xanh | đã qua ô này |
| 🟧 cam | **dừng ở đây** — lý do nằm trong ô |
| ⬜ xám | chưa tới |

Cột ✓ / ✗ / ? so **nhóm hiệu lực** với **đáp án bịa** của ticket.

### Màn dòng chảy (bấm một hàng, hoặc `/console/case/<khoá>`)

Sơ đồ 11 ô — README có 10 ô (A–J), console thêm ô **ĐT** vì định tuyến là bước có AI:

| Ô | Tên | Xanh khi | Cam (dừng) khi |
|---|---|---|---|
| A | Kênh 1 — tín hiệu | luôn (case tồn tại) | — |
| B | Xác định tenant | luôn (đọc được case = ranh giới khớp) | — |
| C | Tìm hoặc tạo Case | luôn | — |
| **ĐT** | Định tuyến nhóm | có nhóm hiệu lực | KHÔNG BIẾT — kèm tầng đã trả lời |
| D | Khớp quy trình đã duyệt | khớp được quy trình ACTIVE | có nhóm mà nhóm chưa có quy trình duyệt |
| E | Suy ra đang ở bước nào | có quy trình | — |
| F | Bước tiếp theo | có bước hiện tại / đã xong hết | — |
| G | Tra tri thức | có lượt hỏi **có nhóm** | — (không nhóm thì không có gì để tra → xám) |
| H | Tri thức + dẫn chứng | có lượt **biết trả lời** | đã tra mà nhóm chưa có SOP duyệt |
| I | Trả gợi ý | đã GIAO một gợi ý biết trả lời | chỉ giao đi câu KHÔNG BIẾT |
| J | Ghi lại — có dùng không | có lượt gợi ý được ghi | — |

Mũi tên nét đứt: **C ⇢ G** (đường lùi — nhóm chưa có quy trình thì tra tri thức theo NHÓM) và **J ⇢ G** (người khai
"nhóm đúng là…" sửa định tuyến cho lần sau).

**Hai kiểu "dừng" quan trọng nhất — mỗi kiểu đòi một việc khác:**

```text
dừng ở ĐT   không biết nhóm (tên nhóm AI viết sai bị từ chối, hoặc AI nói KHONG-BIET)   → sửa ĐỊNH TUYẾN
dừng ở D    biết nhóm, nhưng nhóm chưa có quy trình được duyệt                           → chạy LUỒNG NGƯỢC + duyệt
```

Dưới sơ đồ: **dòng thời gian** (mọi việc xảy ra với case, đúng thứ tự), rồi chi tiết **định tuyến** (người nạp + mọi
biên lai model, kể cả lượt hỏng), **quy trình** (bước nào xong, căn cứ nào), **gợi ý** (phễu RETRIEVED · SHOWN · OPENED ·
ACCEPTED · USED), **gửi về**, **evidence**. Nhãn **nguồn** đi kèm mọi kết quả định tuyến: *người nạp · bảng đo · model
(ngưỡng chưa đo) · model chưa hỏi*.

---

## 7. Hai cách gán AI: File và API

**Chọn ở đâu (`Q6-1`):** màn **Cấu hình AI** (`/console/cau-hinh`) — mỗi bước có AI (định tuyến, soạn nháp, hỏi đáp) chọn
**API** hoặc **File**; với API thì soạn nháp và hỏi đáp chọn thêm **Anthropic** hoặc **OpenAI**. Lựa chọn lưu ở
`scripts\giao-dien\cau-hinh-ai.json` (git chặn, không chứa khoá) và mọi màn đọc chung — đổi ở bước 0 trang giả lập cũng là
đổi ở đây. Chọn API mà nhà cung cấp chưa dùng được (thiếu khoá / model, hoặc Thử khoá bị từ chối) thì bước đó **chạy bằng
File** và màn Cấu hình nói vì sao.

⚠ **Định tuyến chọn nhà cung cấp ở HOST**, không ở console: `DinhTuyen:NhaCungCap` trong `appsettings.Local.json`
(`anthropic` mặc định · `openai`). Lý do: định tuyến chạy tự động trên mọi webhook, không có ai đứng đó chọn. Sai chính tả
tên → app **không khởi động**.

|  | **File** | **API** |
|---|---|---|
| Cần gì | không cần khoá | khoá hợp lệ trong `appsettings.Local.json` (+ `GoiModel` cho định tuyến; + `OpenAI.Model` nếu dùng OpenAI) |
| Ai đóng vai AI | bạn đưa file prompt cho Claude Code / Codex / một khung chat | app tự gọi `claude-opus-5`, hoặc model OpenAI đã cấu hình |
| Tốn gì | lượt dùng của công cụ AI bạn chọn | định tuyến: 1 lượt/ticket mới · soạn nháp: 1 lượt/bấm |
| Kiểm được gì | toàn bộ đường ống + cổng kiểm đầu vào | thêm: lượt gọi thật, biên lai, ngưỡng tin cậy, trần tháng |
| Đọc kết quả ở | "Báo cáo lệnh nạp" (dòng bị từ chối) | biên lai model trên màn dòng chảy |

**Chế độ File — đưa file cho AI:** sau "a · Xuất prompt ra file", trang hiện ba cách, mỗi cách có nút **chép**:

```text
1. Dán câu giao việc vào Claude Code / Codex (khung chat trong IDE)
2. claude -p "Đọc file <prompt>. … Ghi DUY NHẤT khối JSON kết quả … vào file <ketqua>." --allowedTools "Read,Write"
3. codex exec --full-auto "Đọc file <prompt>. … vào file <ketqua>."
```
Hoặc chép toàn bộ nội dung file vào một khung chat AI rồi dán JSON trả về vào ô ở bước c. File nằm ở
`scripts\giao-dien\gia-lap-io\<lô>\` (git đã chặn thư mục này). ⚠ Lệnh `claude -p` / `codex exec` CHƯA được chạy thử
trên máy này — đó là gợi ý.

**Chế độ API — trước khi bấm:** bấm **Thử khoá** ở bước 0 hoặc màn Cấu hình AI — mỗi nhà cung cấp một nút, cùng đường
miễn phí (`GET /v1/models/{model}`). Ra "✓ Khoá dùng được" rồi mới chọn API. Khoá bị từ chối thì mọi lượt API đều hỏng
(hỏng có kiểm soát: định tuyến ra KHÔNG BIẾT + biên lai lỗi; soạn nháp ra 409 `khoa-bi-tu-choi`). Biên lai ghi cả nhà
cung cấp và model của từng lượt.

---

## 7a. Nạp & hỏi: một mã issue hoặc một file

Mở **`/console/nap-hoi`**. Chạy trên app đang chọn ở góc phải (5120 giả lập hoặc 5119 dev).

**Ý nghĩa:** đưa MỘT nguồn vào hệ thống rồi hỏi tự do về nó. Hai nửa kiểm hai thứ khác nhau:

```text
nạp   mã issue → ô A → B → C (như phần mềm khách báo việc mới)      file → ô A2 → K (Kênh 2, đọc chữ)
hỏi   đường NỘI BỘ /internal/hoi-dap (Q3-1) — KHÔNG phải /hoi. /hoi chỉ trả tri thức ĐÃ DUYỆT; đường này cho model đọc
      thẳng evidence / chữ tài liệu, nên mọi câu trả lời mang nhãn CHƯA DUYỆT và không vào kho tri thức.
```

**Nạp một mã issue — luồng chạy:**

```text
trình duyệt ── POST /api/nap-issue {ma, cong} ──► ui_server
   1. hỏi host: có case "jira:<mã>" hoặc "<mã>" chưa?  (GET /internal/cases/{k}/dong-chay)  → có thì CHỌN LUÔN, xong
   2. chưa có + mã đúng hình dạng Jira (ES-123) + đã kết nối Jira:
      ui_server ── Jira /rest/api/2/search  jql = key = "<MÃ>"   (phiên Jira trong RAM của ui_server)
                ── description + comment → dry-run-issue-<mã>-*.json   (git chặn)
                ── check_corpus.py → báo CHẶN thì DỪNG, không gửi (AR-j; ngoại lệ duy nhất: KP_CONG_CHE_TAT_TAM=1)
                ── POST /signals/case-observed + /signals/case-evidence
```

Host **không bao giờ** gọi Jira. Chưa kết nối Jira thì trang mở ô **Kết nối Jira** (PAT hoặc tên + mật khẩu; chỉ nằm trong
RAM ui_server, tắt là mất). ⚠ Đường kéo từ Jira THẬT chưa được chạy trên máy dev (không có phiên Jira) — chỉ nhánh "đã có
trong kho" và các nhánh từ chối đã được bấm thật.

**Import một file — luồng chạy:**

```text
trình duyệt ── POST /app/<cổng>/documents (multipart: file, subTenantCode, nguoiNap) ──► host
host ── khử trùng theo SHA-256 (nạp lại cùng nội dung → trả bản cũ, tên cũ) ── trích chữ (N4-1) ── lưu kho file
```

Đọc được chữ: txt · md · csv · html · json · xml · docx. PDF/ảnh vào kho nhưng `chua-ho-tro` — **không hỏi được**.

**Hỏi — luồng chạy:**

```text
chế độ API    POST /internal/hoi-dap {khoaNguon | taiLieuId, cauHoi, nguoiHoi, nhaCungCap}
              host ── gom nguồn: case = N1 tiêu đề + N2… evidence CHƯA rút lại · tài liệu = chữ chia đoạn ~1500 ký tự
                   ── cổng che trên CẢ câu hỏi lẫn nguồn ── trần tháng (HoiDap:TranThang) ── biên lai TRƯỚC khi gọi
                   ── model trả {bietTraLoi, phatBieu[{noiDung, nguon[]}], ghiChu}
                   ── phát biểu không dẫn được mã CÓ THẬT bị BỎ
chế độ File   a · POST /internal/hoi-dap/xuat-prompt → trang hiện khối để chép cho Claude Code / Codex
              c · dán JSON → POST /internal/hoi-dap/nap-ket-qua → đối chiếu mã bằng ĐÚNG luật của chế độ API
```

**Đọc kết quả:**

| Thấy | Nghĩa |
|---|---|
| Nhãn vàng **CHƯA DUYỆT** | luôn có — kể cả khi câu trả lời trông đúng |
| Chip `N2` sau mỗi phát biểu | bấm để mở đúng mẩu model đã đọc (dạng ĐÃ che) — đối chiếu phát biểu với nó |
| "Đã BỎ k phát biểu…" | model viết k câu không trỏ được về mẩu có thật — chúng không được hiện |
| **KHÔNG BIẾT** — nguồn không nói | đúng, không phải lỗi. Nếu kèm "⚠ chỉ phần đầu được gửi" thì có thể là phần đó không được gửi |
| **KHÔNG BIẾT** — không dẫn được nguồn | model nói "biết" mà mọi phát biểu đều bị bỏ |
| Hộp đỏ + lý do | không hỏi được: trần tháng, thiếu khoá, khoá bị từ chối, cổng che chặn |

---

## 7b. Phần mềm khách: gửi tín hiệu, nhận kết quả về

Mở **`/console/phan-mem-khach`**. Console đóng vai **phần mềm của khách** (`Q8-1`).

**Ý nghĩa:** kiểm hợp đồng tích hợp (`docs/19`) từ phía bên tích hợp — cả hai chiều:

```text
GỬI   trình duyệt ── POST /app/<cổng>/signals/* hoặc /hoi ──► host ── phản hồi TRẢ NGAY (đồng bộ)
NHẬN  host ── hàng đợi bền → vòng quét ~15 giây ── POST http://127.0.0.1:8791/khach-gia-lap/webhook/<cổng>
      ui_server ── kiểm X-KP-Signature = sha256=HMAC(bí mật ký, NGUYÊN BYTE thân) ── sai → 401 (host thử lại theo lịch)
```

**Các bước:**

1. **Đăng ký hộp nhận** — ui_server gọi `PUT /internal/webhook`, giữ bí mật ký trong RAM (trang không thấy nó).
   ⚠ Mỗi khách MỘT đích gửi: đăng ký trên app **5119** là THAY đích hiện có của `dev-tenant`.
2. Chọn mẫu **Có việc mới** → **Gửi**. Mong đợi: HTTP 200, `created: 1`.
3. Vài giây sau hộp nhận hiện gói `suKien: case-moi` của đúng mã, **✓ hợp lệ**.
4. Trên CÙNG mã: **Thêm một mẩu evidence**, **Hỏi**, **Việc ở nguồn đổi trạng thái** — mỗi lượt xem phản hồi trả ngay;
   mẩu làm case bước sang bước khác thì có thêm gói `buoc-hien-tai-doi`.

`? không kiểm được` ở cột chữ ký = ui_server đã khởi động lại sau khi đăng ký (mất bí mật trong RAM) — đăng ký lại.

## 7c. Tự trải nghiệm: kịch bản, bản nháp SOP, phiên & tổng kết

> **Thêm 2026-10-23** (`docs/DE_BAI_HOAN_THIEN_GIAO_DIEN_TRAI_NGHIEM.md`; lý do + chênh lệch với đề bài: `docs/07` §3 `IM-61`).
> Để chủ dự án tự thao tác, đọc kết quả, đối chiếu nguồn và lưu đánh giá — không phải mở mã nguồn hay tự gọi API.

**Bắt đầu ở `/console/kich-ban`.** Sáu kịch bản trên dữ liệu BỊA của app giả lập :5120, mỗi kịch bản ghi mục tiêu, dữ liệu,
điều kiện đầu, thao tác, kết quả mong đợi và giới hạn. "Bắt đầu phiên theo kịch bản này" mở một **phiên** gắn tên kịch bản
rồi đưa sang màn đầu tiên:

```text
1  Chưa có tri thức được duyệt     định tuyến đúng nhưng /hoi trả KHÔNG BIẾT "nhóm chưa có tri thức được người duyệt"
2  Soạn nháp → xem/sửa → duyệt     chỉ ra điểm nội dung bắt đầu được dùng: sau duyệt, và đúng bản ĐÃ SỬA
3  Xác nhận bước                   mở xem không đổi bước; xác nhận K1 → bước hiện tại K2, hỏi lại vẫn K2
4  Hỏi tự do có dẫn chứng          CHƯA DUYỆT; mỗi phát biểu mở được đúng mẩu đã gửi; tải lại vẫn mở lại được
5  Nguồn thiếu câu trả lời         KHÔNG BIẾT (nguon-khong-noi) — đánh giá việc từ chối là ĐÚNG
6  Lỗi vận hành                    JSON hỏng → 400 · tắt app :5120 → 502; tổng kết tách khỏi "không biết". Không tốn tiền
```

### Thanh phiên (đầu các màn Giả lập, Nạp & hỏi, Bản nháp)

Cho biết đang dùng **dữ liệu nào** (app, cổng, tenant, chế độ tenant), **chế độ AI nào** (định tuyến / soạn nháp / hỏi đáp:
API · nhà cung cấp hoặc File) và ghi vào **phiên nào**. Chưa chọn phiên thì lượt đầu tiên tự mở một phiên — một lượt hỏi
không bao giờ mất vì quên bấm "Phiên mới".

**Phiên lưu ở đâu:** ui_server, `scripts\giao-dien\phien-trai-nghiem\<ngữ cảnh>\<id>.json` — `.gitignore` chặn, ghi xong mới
thay file. Ngữ cảnh = `cong-<cổng>.<tenantId>` (đọc từ `GET /internal/tenant-boundary`): đổi app hay đổi khách là đổi thư
mục, nên lịch sử của ngữ cảnh trước KHÔNG hiện lại. Chung cho mọi trình duyệt trên máy này (localStorage chỉ nhớ phiên nào
đang chọn). Người thao tác là LỜI KHAI — console chưa xác thực danh tính. Lượt mang khoá tên `apiKey`/`pat`/`matKhau`/
`token`/`secret`… bị ui_server từ chối cả lượt.

### Giả lập bước 5 — đọc ĐỦ hướng dẫn

Bấm **"n hướng dẫn ▸"** ở một ticket: định tuyến kèm NGUỒN (người nạp / bảng tra / model) và chấm trên đáp án giả (chỉ về
định tuyến), KHÔNG BIẾT kèm lý do (một kết quả hợp lệ, khác hẳn **lỗi vận hành** = HTTP khác 200), quy trình + bước hiện tại
+ "xem ở", từng bản ghi tri thức: điều kiện áp dụng / cách nhận ra / cách xử lý, mỗi phát biểu có **mức kiểm chứng**,
**nguồn gốc** và **dẫn chứng**. Bấm một dẫn chứng → nội dung mẩu đọc qua `GET /cases/{k}/evidence` (đường duy nhất thực
thi luật rút lại): thiếu liên kết, case không truy cập được, mẩu đã rút — mỗi ca nói đúng ca của nó, không dựng thay.
JSON thô chỉ là phần mở rộng. **Mở/đóng phần đọc không gửi request nào** (đã đo). Dưới phần đọc là form đánh giá.

### Bản nháp SOP — `/console/ban-nhap` (nút "Đọc · sửa · duyệt" ở bước 3)

```text
đọc    GET  /knowledge/{id} · GET /processes/{id}                 phát biểu ĐANG hiệu lực theo loại; nội dung trái, dẫn
                                                                  chứng phải khi màn đủ rộng; "không mẩu nào hỗ trợ trực tiếp"
sửa    POST /knowledge/{id}/sua                                   tạo bản B, bản A còn nguyên → "Lịch sử: n bản — xem bản gốc"
       PUT  /processes/{id}/buoc                                  CHỈ DRAFT (N5-1). ⚠ THAY toàn bộ bước — host không giữ bản
                                                                  cũ; console ghi bản trước vào lịch sử phiên
duyệt  POST /knowledge/{id}/approve · /processes/{id}/approve     TỪNG đối tượng, bảng kết quả từng dòng, thẻ đọc lại từ app
```

**Lưu chỉnh sửa ≠ Duyệt.** Lưu sửa không đổi trạng thái (DRAFT vẫn DRAFT). Nút duyệt khoá cho tới khi: không còn sửa chưa
lưu, khai người duyệt, CHỌN phạm vi xem (không có mặc định — S7), tích "đã đọc", và TỰ TÍCH lời khai AR-n — giao diện liệt
kê thiếu gì. Một đối tượng hỏng (vd. 409 "chỉ duyệt được bản DRAFT", hoặc 409 cổng shared `AR-l` trên app shared) không làm
đối tượng khác hỏng theo và không thành "thành công toàn bộ": bảng ghi `k/n đối tượng duyệt được`. Rời trang khi còn sửa
chưa lưu thì trình duyệt hỏi lại. Mức kiểm chứng khi sửa giữ nguyên mức cũ trừ khi bạn chủ động đổi.

⚠ Nút **"Duyệt nhanh (dữ liệu giả)"** ở bước 4 vẫn còn, ghi rõ CHỈ cho giả lập: nó tự khai AR-n + `noi-bo` hộ bạn.

### Nạp & hỏi — lượt, lịch sử, đánh giá

Mỗi lần **Hỏi** (API) hoặc **Nạp kết quả** (File) là MỘT lượt: ghi "đang chờ" TRƯỚC khi gọi host, cập nhật khi có kết quả
(tải lại giữa chừng thì lượt còn đó, kết cục "đang chờ / gián đoạn"). Lượt lưu câu hỏi, nguồn, chế độ, nhà cung cấp/model
**theo kết quả host trả** (File thì "không rõ", không gán hộ), câu trả lời + các mẩu ĐÃ gửi (bản đã qua cổng che), cảnh
báo nguồn bị cắt, thời gian chờ đo ở giao diện (gồm mạng + host, không phải thời gian model), token/chi phí "chưa có dữ
liệu". Bảng **"Lượt hỏi trong phiên"** mở lại đúng bản chụp — không gọi app, không chạy lại câu hỏi.

### Đánh giá — ba chiều độc lập

| Chiều | Giá trị |
|---|---|
| Độ đúng nội dung | Chưa đánh giá · Đúng · Đúng một phần · Sai · Chưa đủ cơ sở |
| Dẫn chứng hỗ trợ | Chưa đánh giá · Đủ · Một phần · Không hỗ trợ · Không áp dụng |
| Tính thực hiện được | Chưa đánh giá · Làm theo được · Cần bổ sung · Không dùng được · Không áp dụng |

\+ nhận xét + kết quả mong đợi. Mặc định "Chưa đánh giá" — không bao giờ suy từ HTTP 200. Lưu đánh giá CHỈ ghi vào phiên:
không đổi mức kiểm chứng, trạng thái duyệt hay nấc USED (nút "Khai: đã dùng gợi ý" là việc khác, ghi lên app).

### Phiên — `/console/phien`: lịch sử, tổng kết, so sánh, xuất

Chọn ngữ cảnh (app) + phiên. **Tổng kết**: tổng lượt; "chạy được" (trả lời + không biết) tách khỏi lỗi vận hành và đang
chờ; đã/chưa đánh giá; phân bố từng chiều **kèm mẫu số** (chưa ai chấm thì "chưa có đánh giá", không phải 0%); chấm định
tuyến trên đáp án giả trình bày RIÊNG; danh sách "cần mở lại"; bản nháp: số lần lưu sửa / duyệt được / duyệt hỏng. Tích hai
lượt để **so sánh** cạnh nhau — khác câu hỏi/nguồn/chế độ/model/phiên bản quy trình thì nói thẳng "không phải so sánh công
bằng". **Xuất JSON** (`dinhDang: kp-phien-trai-nghiem/1`) và **Xuất Markdown** — chỉ khi bấm. Mở trang này chỉ đọc file
phiên (đã đo: không POST nào tới app). **Xoá phiên** ở đây (xoá file, không hoàn tác).

---

## 8. Các phép kiểm tự động

### Bộ test C# — kiểm LUẬT

```bat
dung-thu.bat                          rem app đang chạy giữ DLL → build trong lúc test sẽ hỏng
dotnet test src\KnowledgePlatform.slnx
```
Ba project, chạy trên PostgreSQL thật (`kp_test`, `kp_api_test` — bộ test tự migrate):

```text
KnowledgePlatform.Domain.Tests          luật domain thuần, không cần database (~0,5 s)
KnowledgePlatform.Infrastructure.Tests  RLS trên PostgreSQL thật, client Anthropic qua HttpMessageHandler giả (không mạng)
KnowledgePlatform.Api.Tests             HTTP thật trong bộ nhớ: tín hiệu, định tuyến, quy trình, gợi ý, console (~2–3 phút)
```
Mong đợi: `Failed: 0` ở cả ba dòng. Không test nào gọi Anthropic thật.

### Kiểm đầu-cuối — kiểm HỢP ĐỒNG trên app đang chạy

```bat
rem cần app dev :5119 đang chạy — chay-thu.bat mở sẵn (run-dev-app.bat đã đặt Link__PublicBaseUrl)
python scripts\kiem_dau_cuoi.py
```
⚠ Mở app dev bằng cách khác (vd. `dotnet run`) mà thiếu `Link__PublicBaseUrl` thì mục H tự bỏ qua và mục J báo
`… · 1 sai` (`GET /xem/{token}` chưa được gọi) — do cấu hình, không phải lỗi code.
Gọi thật mọi endpoint của hợp đồng (`GET /openapi/v1.json`), kiểm các cổng còn chặn đúng chỗ. Mong đợi:
`… phép kiểm · 0 sai`. Nó ghi một ít dữ liệu tiền tố `kiem-dau-cuoi:` vào `kp_dev` — vô hại, lọc được. ⚠ Nếu app có khoá,
mục C2 gọi Thử khoá — một lượt ra Anthropic, miễn phí.

### Kiểm hai khách — kiểm RANH GIỚI giữa các công ty

Cần app chạy ở chế độ **shared** (nhiều khách một tiến trình) — KHÔNG chạy cùng lúc với app dev :5119:

```powershell
# PowerShell, từ gốc repo. Chạy dung-thu.bat trước.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5119'
$env:Tenancy__Mode = 'SharedMultiTenant'
$env:Auth__AcknowledgeUnauthenticatedApi = 'false'
$env:Link__PublicBaseUrl = 'http://localhost:5119'
$env:Webhook__BatVongQuet = 'false'
dotnet run --project src\KnowledgePlatform.Api --no-launch-profile
# cửa sổ khác:
python scripts\kiem_hai_khach.py
```
Dựng hai khách từ số 0 chỉ bằng CLI + HTTP, kiểm khách A không thấy dữ liệu khách B. Mong đợi: mã thoát 0. ⚠ Ở chế độ
shared, mọi lượt duyệt trả **409** (`AR-l`) — đó là thiết kế, không phải lỗi.

### Kiểm sơ đồ console — kiểm cách VẼ

```bat
rem cần app giả lập :5120 + giao diện :8791 đang chạy
cd scripts\giao-dien\console
npm run kiem:so-do
```
Chạy `tinhSoDo` trên mọi case giả lập thật: bất biến (A/B/C luôn qua, không nhóm thì G/H không qua, đường lùi chỉ sáng
khi không có quy trình…) + kỳ vọng trên lô `20261019-045231` nếu còn. Mong đợi: `… phép kiểm · 0 sai`.

⚠ **2026-10-23:** phần KỲ VỌNG báo `1 sai` (ticket 004 của lô đó "nhóm CHƯA có quy trình: dừng ở D"). Không phải lỗi vẽ:
nhóm "NCC từ chối payload" đã có quy trình ACTIVE trong `kp_gialap` từ 2026-10-19 19:21 (một lượt duyệt sau IM-50), nên
dữ liệu đã đổi dưới kỳ vọng. Phần BẤT BIẾN vẫn 0 sai. Muốn lại `0 sai` thì dựng lại `kp_gialap` (§9).

### Kiểm phiên trải nghiệm — kiểm ĐẾM và LƯU

```bat
cd scripts\giao-dien\console
npm run kiem:phien                                  rem phép tính thuần — không cần app
cd ..\..\..
python scripts\giao-dien\kiem_phien_trai_nghiem.py  rem nơi lưu ở ui_server — không cần app, ghi vào thư mục tạm rồi xoá
```
`kiem:phien`: mẫu số của từng chiều đánh giá, "chưa có đánh giá" ≠ 0%, "chạy được" không gồm lỗi/đang chờ, chấm định
tuyến không đổ vào độ đúng, lịch sử sửa giữ bản gốc, so sánh nói ra khi đầu vào khác, Markdown khớp số liệu.
`kiem_phien_trai_nghiem.py`: hai ngữ cảnh không thấy phiên của nhau, lượt không đè lượt (kể cả cùng câu hỏi), khoá bí
mật bị từ chối mà không ghi gì, tên ngữ cảnh/id không thoát ra ngoài thư mục. Mong đợi: `✓ n/n phép kiểm đúng` cả hai.

---

## 9. Dọn dẹp và làm lại từ đầu

| Muốn | Làm |
|---|---|
| Tắt mọi thứ | `dung-thu.bat` (PostgreSQL không bị đóng) |
| Sinh lô mới | bước 1 → "Sinh lô mới". Lô cũ vẫn nằm trong `kp_gialap`, vô hại |
| Xoá sạch dữ liệu giả lập | `dung-thu.bat`, rồi `psql -U postgres -h localhost -c "DROP DATABASE kp_gialap"`, rồi 3 lệnh ở §2 bước 3 |
| Xoá file trao đổi với AI | xoá `scripts\giao-dien\gia-lap-io\` (đã nằm ngoài git) |
| Xoá một phiên trải nghiệm | `/console/phien` → "Xoá phiên" · xoá hết: xoá `scripts\giao-dien\phien-trai-nghiem\` (ngoài git) |
| Build lại sau khi sửa code | `dung-thu.bat` → `chay-thu.bat` (C#) · `scripts\giao-dien\build-console.bat` (Vue) |

⚠ Kho tri thức chỉ GHI THÊM: nạp lại một bản nháp sinh bản ghi mới bên cạnh bản cũ, không đè. Muốn kho "sạch" thì xoá
database, đừng mong nạp lại sẽ thay thế.

---

## 10. Xử lý sự cố

| Triệu chứng | Nguyên nhân thường gặp | Làm gì |
|---|---|---|
| `chay-thu.bat` dừng ở **1/5**: PostgreSQL không trả lời | service chưa bật | bật `postgresql-x64-18` trong Services |
| Dừng ở **5/5**: app giả lập chưa sẵn sàng | chưa dựng `kp_gialap`, hoặc thiếu khách `gia-lap` | đọc cửa sổ "KP app giả lập"; làm §2 bước 3 |
| Sửa code mà chạy vẫn như cũ | app đang chạy → bước build bị bỏ qua | `dung-thu.bat` rồi `chay-thu.bat` |
| `/console` báo "Console chưa build" | chưa có `console\dist` | `scripts\giao-dien\build-console.bat` (cần Node) |
| Bước 0: "Anthropic TỪ CHỐI khoá" | khoá sai / thu hồi / chép thiếu | tạo khoá mới, sửa `appsettings.Local.json`, chạy lại app |
| Định tuyến API: mọi ticket "Gọi model định tuyến thất bại" | khoá bị từ chối | xem biên lai trên màn dòng chảy; bấm Thử khoá |
| Định tuyến API: mọi ticket KHÔNG BIẾT, nguồn "bảng đo" | `GoiModel` đang tắt | thêm `"DinhTuyen": {"GoiModel": true}`, chạy lại app |
| Định tuyến API: KHÔNG BIẾT, lý do "dưới ngưỡng" | model tự chấm độ tin < 0,9 | đúng thiết kế (`E1-1`: thà im lặng hơn đoán); ngưỡng CHƯA ĐO |
| Bước 3: "Chưa ticket nào của lô có nhóm" | chưa định tuyến, hoặc định tuyến ra KHÔNG BIẾT hết | làm bước 2 trước; thử chế độ File |
| Bước 3 (File): dòng bị bỏ qua khi nạp định tuyến | AI viết tên nhóm không chép nguyên taxonomy | sửa JSON (chép nguyên tên) rồi nạp lại — nạp lại thì ĐÈ, vô hại |
| Bước 5: "không biết" dù đã duyệt | ticket thuộc nhóm KHÁC nhóm vừa soạn, hoặc chưa duyệt | kiểm cột nhóm trên bản đồ lô; làm bước 4 |
| `curl` từ Git Bash gửi tiếng Việt → "không phải JSON" | Git Bash gửi tham số theo codepage ANSI | ghi body ra file UTF-8 rồi `curl --data-binary @file` — trình duyệt không bị |

---

## 11. Test này KHÔNG chứng minh được gì

⚠ **Thêm 2026-10-22 (nhóm Q):** đường **OpenAI** chưa từng gọi OpenAI thật — request/response được kiểm bằng HttpClient giả
theo tài liệu công khai; lượt thật đầu tiên là phép thử thật đầu tiên (bấm Thử khoá trước). Đường **kéo một issue từ
Jira** chưa chạy với Jira thật. **Chất lượng câu trả lời hỏi đáp** chưa được đo — luật trỏ nguồn chỉ bảo đảm mỗi phát
biểu CÓ dẫn một mẩu có thật, không bảo đảm mẩu đó NÓI điều phát biểu nói.

Ghi ra để không ai đọc màn hình xanh thành "sản phẩm dùng được":

```text
1  Điểm chấm định tuyến KHÔNG phải độ chính xác. 11 ticket do chính người viết bộ đề soạn; nó nói ĐƯỜNG ỐNG chạy và
   chấm được. Con số đo được trên dữ liệu thật vẫn là 84,1% của docs/13 — một prompt, một model, một ngày.
2  Sơ đồ nói hệ thống ĐÃ LÀM gì, không nói làm ĐÚNG không. Ticket định tuyến SAI vào một nhóm ĐÃ có SOP duyệt vẫn sáng
   A → J. Giả lập có cột ✓/✗ để bắt chuyện đó; dữ liệu thật thì không có đáp án.
3  Ngưỡng tin cậy 0,9 của định tuyến bằng model CHƯA ĐO. Tới 2026-10-19 định tuyến bằng model có 0 lượt THÀNH CÔNG
   (khoá bị từ chối — IM-56).
4  Chế độ dedicated là chế độ được test ở đây. Chế độ shared (nhiều công ty một bản deploy) mọi lượt duyệt vẫn 409 (AR-l).
5  "Thử khoá ✓" không nói còn credit — chỉ lượt gọi tốn tiền đầu tiên mới nói.
6  Bước quy trình sinh từ bản nháp chưa có dấu hiệu nào, nên chỉ xong được bằng lời XÁC NHẬN — test bước 5 kiểm đường
   xác nhận, không kiểm đường "evidence mới tự làm xong bước" (bộ test C# TienDoQuyTrinhTests, XuLyNgayTests giữ đường đó).
7  Đánh giá trong phiên là ý kiến của NGƯỜI THỬ trên dữ liệu BỊA — nó không phải số đo chất lượng của sản phẩm, và tổng
   kết không suy ra tiết kiệm thời gian, giảm escalation hay ROI. Bản chụp lượt giả lập giữ KHOÁ dẫn chứng, không giữ nội
   dung mẩu: mở dẫn chứng từ lịch sử là đọc bản HIỆN TẠI trong app.
```
