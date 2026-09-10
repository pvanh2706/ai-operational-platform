# 10 — CHUYỂN SANG MÁY KHÁC

> **Viết 2026-09-05**, **cập nhật 2026-09-10** khi người dùng chuyển máy lần thứ hai.
> Mục đích: dựng lại **toàn bộ ngữ cảnh** trên một máy trắng, không mất thông tin.
>
> Sửa gì ở lần cập nhật 2026-09-10, ghi ra để biết chỗ nào từng sai:
> §1 thiếu hẳn `appsettings.Local.json` (nó sinh ra 2026-09-08, sau bản trước) và
> `redact-list.json` · §2 ghi "phải 105/105 xanh", giờ là **147** · §4 thiếu bốn file
> kết quả, gồm **bản A và bản B của `M2`** · §5.2 chỉ ghi cp1252 ở chiều GHI, thiếu
> chiều ĐỌC (`open()`), và chiều đọc đã cắn 3 lần trong một phiên · §6 liệt ba câu chờ
> quyết mà cả ba đã chuyển trạng thái, nên **§6 đã viết lại hoàn toàn**.
>
> Đọc file này **sau** `00_CURRENT_STATE.md`. `00` nói *đang ở đâu*; file này nói
> *làm sao có lại được những gì máy cũ có mà git không giữ*.

---

## 0. ⚠ REPO NÀY PHẢI Ở CHẾ ĐỘ PRIVATE

**Git history chứa credential còn sống của khách hàng.** Cụ thể: hai commit `ec1ce50` và
`3a15f3b` có mật khẩu tài khoản hoá đơn điện tử VNPT, một tài khoản dịch vụ, và 15 cặp
ID + mật khẩu Ultraviewer của các khách sạn khách hàng.

Chúng **đã được bỏ khỏi working tree** ngày 2026-09-05 (commit `0e602c3`) và không còn
trong bất kỳ file nào đang được theo dõi — nhưng git giữ lịch sử, nên `git log -p` vẫn
đọc được. Chủ dữ liệu đã chọn **push nguyên trạng** thay vì viết lại history.

Hệ quả phải biết:

```text
· ĐỪNG chuyển repo sang public khi chưa viết lại history.
· ĐỪNG thêm collaborator không được phép xem dữ liệu khách hàng.
· Ultraviewer ID gắn cứng theo MÁY và không xoay được — chỉ đổi được mật khẩu
  cố định trên chính máy đó.
· Mật khẩu VNPT thì xoay được, và việc đó đã được nêu từ 2026-09-04. Nếu đã xoay
  thì giá trị trong history thành vô hại — đó là cách rẻ nhất để đóng lỗ này.
```

Muốn dọn history về sau: `git filter-repo --replace-text <file>` với danh sách giá trị
lấy từ `scripts/jira-export/redact-list.json` (file đó nằm ngoài repo, xem §3). Sau khi
dọn thì mọi SHA đổi, nên phải force-push và mọi bản clone khác phải clone lại.

---

## 1. Thứ gì KHÔNG nằm trong git — đọc mục này trước tiên

Repo giữ code, tài liệu, script và kết quả phân tích. Bốn thứ dưới đây **không** có
trong repo, và mỗi thứ có cách xử lý khác nhau:

```text
THỨ                          MẤT KHI ĐỔI MÁY?   CÁCH CÓ LẠI
─────────────────────────────────────────────────────────────────────────────
Corpus Jira (dry-run-*.json,  MẤT               chạy lại script, ~6 phút
spread-*.json, fixture-*.json)                  (.gitignore — dữ liệu khách hàng)

jira-config.bat (có PAT)      MẤT               chép từ jira-config.example.bat
                                                rồi điền lại PAT

appsettings.Local.json        MẤT               chép từ appsettings.Local.example.json
(khoá API Anthropic)                            rồi dán khoá. ⚠ ĐỪNG đặt khoá vào
                                                appsettings.Development.json — file đó
                                                ĐANG ĐƯỢC GIT THEO DÕI, và history
                                                không xoá được (§0). SoanNhapRunner
                                                TỪ CHỐI CHẠY nếu file nó đọc khoá từ
                                                đó lại không được .gitignore chặn.

redact-list.json              MẤT               KHÔNG cần làm gì: make_fixture.py TỰ
(cột giá trị bí mật thật)                       SINH lại nếu thiếu. Nằm ngoài repo vì
                                                nó chứa giá trị bí mật thật.

Database kp_dev + dữ liệu      MẤT               dev-db-setup.sql rồi nạp lại fixture

Bộ nhớ của agent               MẤT               ⚠ ĐÃ CHÉP VÀO §5 CỦA FILE NÀY
(~/.claude/.../memory/)                          — đó là lý do §5 tồn tại

Công cụ máy: .NET SDK,        MẤT               §2 bước 0 và 1b. dotnet-ef KHÔNG
dotnet-ef, PostgreSQL, Python                    đi theo git — nhưng .config/
                                                 dotnet-tools.json thì có, nên
                                                 `dotnet tool restore` lo phiên bản

Định nghĩa các workflow        KHÔNG mất         ĐÃ ĐƯA VÀO scripts/workflows/
đã chạy                        (từ 2026-09-05)   ~4,7 triệu token, chạy lại rất đắt
```

⚠ **Thứ đắt nhất KHÔNG mất, vì đã được đưa vào repo hôm nay:** kết quả của hai workflow
(28 agent + 16 agent, ~3,7 triệu token) nằm ở `docs/ket-qua-phan-tich/`. Chạy lại chúng
tốn hàng giờ. Xem §4.

---

## 2. Dựng lại từ máy trắng — theo đúng thứ tự

```bash
git clone <repo> && cd 23.ai-operational-platform

# 0. Python cho nhóm script vận hành. CHỈ thu_retrieval.py cần thư viện ngoài;
#    các script còn lại dùng thuần stdlib có chủ đích (xem requirements.txt).
pip install -r scripts/jira-export/requirements.txt

# 1. Database. Cần superuser MỘT LẦN.
psql -U postgres -h localhost -f scripts/dev-db-setup.sql

# 1b. Công cụ EF. KHÔNG đi theo git; phiên bản do .config/dotnet-tools.json giữ.
dotnet tool restore

# 2. Build + schema
dotnet build src/KnowledgePlatform.slnx
dotnet ef database update --project src/KnowledgePlatform.Infrastructure \
  --connection "Host=localhost;Database=kp_dev;Username=kp_app;Password=123456"

# 3. Tenant cho máy dev
psql -U kp_app -h localhost -d kp_dev -f scripts/dev-seed-tenant.sql

# 4. Kiểm — phải 147/147 xanh (90 domain + 15 hạ tầng + 42 API)
dotnet test src/KnowledgePlatform.slnx

# 5. Kiểm THẬT SỰ: app khởi động được và thấy kp_dev
dotnet run --project src/KnowledgePlatform.Api --launch-profile http
#    rồi ở cửa sổ khác:
curl localhost:5119/health/ready               # DB sống + ranh giới tenant còn nguyên
curl localhost:5119/internal/tenant-boundary   # đếm bằng SQL thô, KHÔNG có điều kiện tenant
```

🛑 **BƯỚC 5 KHÔNG THỪA — bước 4 có thể XANH GIẢ.** `dotnet test` không đọc `kp_dev` lấy
một dòng: bộ Infrastructure trỏ vào `kp_test`, bộ API trỏ vào `kp_api_test`, cả hai **tự
chạy migration** và bộ API còn **tự tạo tenant riêng**. Nên bỏ qua hoặc làm hỏng bước 2
và bước 3 thì bước 4 **vẫn 147/147**. Chỉ bước 5 mới chạm vào thứ ba bước đầu vừa dựng.
Nếu bước 3 bị bỏ, app sẽ **từ chối khởi động** — đó là hành vi cố ý, không phải lỗi.

⚠ **Ba chỗ vấp đã đo trên máy cũ, sẽ lặp lại trên máy mới:**

- **Solution nằm ở `src/KnowledgePlatform.slnx`**, không phải thư mục gốc. `dotnet test`
  từ gốc repo trả `MSB1003`.
- **`dotnet ef database update` KHÔNG tự trỏ vào `kp_dev`.** `DesignTimeDbContextFactory`
  cố tình dùng chuỗi kết nối GIẢ (`kp_design_time`) vì lúc sinh migration không có
  request nào. Thiếu `--connection` thì nó **im lặng migrate một DB không ai dùng**.
- **ĐỪNG chạy app hay test bằng role `postgres`.** Superuser đi vòng qua row-level
  security *kể cả khi bảng có FORCE* → RLS bằng không, và **mọi test cách ly tenant PASS
  GIẢ**. Test đầu tiên trong bộ test kiểm đúng điều này.

---

## 3. Lấy lại corpus Jira

```bash
cp scripts/jira-export/jira-config.example.bat scripts/jira-export/jira-config.bat
# điền JIRA_BASE_URL + JIRA_PAT vào jira-config.bat (file này ĐÃ trong .gitignore)

# Mẫu 150 case gần nhất (nhanh, nhưng xem cảnh báo bên dưới)
cmd /c "call scripts\jira-export\jira-config.bat && python scripts\jira-export\export_jira_to_channel1.py --dry-run"

# Mẫu RẢI ĐỀU 12 tháng — dùng cái này cho mọi phép đếm
cmd /c "call scripts\jira-export\jira-config.bat && python scripts\jira-export\sample_spread.py"

# Kiểm TRƯỚC KHI nạp (trả mã thoát ≠ 0 nếu có phát hiện chặn)
python scripts/jira-export/check_corpus.py

# Dựng fixture (thay credential thật bằng giá trị GIẢ giữ nguyên hình dạng) rồi nạp
python scripts/jira-export/make_fixture.py     # ⚠ mã thoát 1 = còn credential, ĐỌC KỸ
dotnet run --project src/KnowledgePlatform.Api --launch-profile http   # cửa sổ khác
python scripts/jira-export/load_fixture.py
```

🛑 **`make_fixture.py` mang một danh sách credential XÁC ĐỊNH BẰNG TAY trên corpus của
2026-09-04.** Chạy nó trên corpus khác thì danh sách đó lỗi thời — đo thật trên corpus
150 case: 16/19 mục không khớp, và **36 chỗ credential mới đi thẳng qua**. Script giờ tự
hỏi ngược bằng luật che (`check_corpus.quet_bi_mat`) và **thoát với mã 1** nếu còn; nó
vẫn ghi file ra để bạn xem, nhưng **đừng chia sẻ fixture đó**.

⚠ **Cạm bẫy đã vấp thật, đừng vấp lại:** `ORDER BY resolved DESC` + `MAX_ISSUES=150` KHÔNG
cho mẫu của 12 tháng — nó cho mẫu của **24 ngày**, vì project có ~2 723 case hoá đơn đã
đóng mỗi năm. *"N case gần nhất"* là mẫu của một cửa sổ hẹp mà độ hẹp **phụ thuộc lưu
lượng**, và lưu lượng thì không ai kiểm khi viết JQL. Dùng `sample_spread.py` cho phép đếm.

⚠ **Chạy foreground với timeout dài.** Kéo 150 issue là ~380 request; chạy nền bị cắt sau
~2-3 phút **và vẫn trả exit code 0**, nên trông như xong bình thường. Đã mất hai lần chạy
vì chuyện này.

---

## 4. Kết quả phân tích đã lưu — thứ KHÔNG chạy lại được rẻ

`docs/ket-qua-phan-tich/`

| File | Là gì | Chạy lại tốn |
|---|---|---|
| `taxonomy-19-nhom-hoa-don.json` | 19 nhóm nguyên nhân + case nào thuộc nhóm nào | workflow 16 agent, ~40 phút |
| `nguyen-nhan-150-case.json` | nguyên nhân + mức chắc chắn + bước xử lý của từng case | (cùng workflow) |
| `cay-quyet-dinh-*.json` (2 file) | hai bản nháp SOP dựng TAY — **bản A của `M2`** | nhiều giờ người |
| `may-sinh-phan-quyen-ky-hieu.json` | bản nháp SOP đầu tiên do MÁY sinh — **bản B của `M2`** | 1 ngữ cảnh sạch, ~6 phút |
| `dinh-tuyen-150-case.json` | kết quả phép đo định tuyến (`docs/13`) | 5 ngữ cảnh sạch, ~6 phút |

🛑 **ĐỪNG SỬA hai file `cay-quyet-dinh-*.json` cho "khớp" với bản máy sinh.** Chúng là
**bản A của phép đo `M2`**; sửa chúng là làm hỏng mốc so, và mốc đó không dựng lại được
(xem `07` §3 `IM-27` và `AR-q`). Bản máy sinh thì chạy lại được, bản người viết thì không.

`nguyen-nhan-150-case.json` **đã bỏ trường `trichDan`** (nguyên văn evidence của khách)
có chủ đích — đó là dữ liệu vận hành thật, và `.gitignore` của repo đặt nguyên tắc rằng
đưa nội dung nghiệp vụ của khách vào repo là **quyết định của chủ dữ liệu**, không phải
thao tác kỹ thuật. Cần trích dẫn thì chạy lại export + workflow.

Dùng lại chúng, ví dụ cho phép thử retrieval:

```bash
python scripts/jira-export/thu_retrieval.py     # cần corpus ở §3 trước
```

Script tự tìm taxonomy ở `docs/ket-qua-phan-tich/`, không cần đặt biến. Đặt `TAXONOMY`
chỉ khi muốn đo trên một taxonomy khác.

⚠ **Taxonomy trong repo gán nhãn cho corpus `dry-run-*`, KHÔNG phải `spread-*`.** Khớp
88/88 với cái đầu và **0/144** với cái sau. Nên dù §3 khuyên dùng `spread` cho phép đếm,
`thu_retrieval.py` mặc định đọc `dry-run-*` — và nó **chặn trước** nếu hai bên không giao
nhau, thay vì chạy nửa chừng rồi chết. Muốn đo trên corpus khác thì phải có taxonomy của
chính corpus đó: chạy lại `scripts/workflows/dem-nguyen-nhan-rk4.js` trên nó.

```bash
python scripts/jira-export/thu_retrieval.py spread-cases.json spread-evidence.json
```

---

### 4b. Định nghĩa các workflow — `scripts/workflows/`

Ba workflow đã chạy trong hai ngày 2026-09-04/05, tổng ~4,7 triệu token. Chúng nằm ở
`scripts/workflows/` từ 2026-09-05; trước đó chúng chỉ sống trong thư mục phiên của
Claude và **sẽ mất khi đổi máy**.

| File | Làm gì | Đã sinh ra |
|---|---|---|
| `audit-jira-corpus.js` | quét corpus 9 lăng kính + phản biện đối kháng, 28 agent | `AR-j` `AR-k` `AR-l` `AR-m` `AR-n`, đo lại `AR-h` |
| `dem-nguyen-nhan-rk4.js` | đếm nguyên nhân: 10 lượt rút → 3 lượt GỘP độc lập → 2 phản biện, 16 agent | `docs/09`, taxonomy 19 nhóm |
| `kiem-handoff-chuyen-may.js` | 8 agent đóng vai người mới trên máy trắng đi tìm chỗ bàn giao thiếu | 61 phát hiện, 21 mức chặn — phần lớn file này |

Chạy lại: mở lại bằng công cụ Workflow với nội dung file tương ứng. Nhớ rằng chúng đọc
dữ liệu từ **thư mục tạm của phiên cũ** — đường dẫn trong script phải sửa theo máy mới.

⚠ Thiết kế đáng giữ lại của `dem-nguyen-nhan-rk4.js`: **ba lượt gộp độc lập theo ba tiêu
chí ĐẶT TRƯỚC**. Vòng đếm trước đó thất bại vì hai lượt phân tích tự chọn độ mịn rồi
lập luận trên đó và ra hai kết luận ngược nhau. Có ba tiêu chí đặt trước thì **độ phân
kỳ giữa chúng trở thành dữ liệu** — và nó cho biết con số phụ thuộc tiêu chí chứ không
phụ thuộc dữ liệu.

---

## 5. Bộ nhớ của agent — chép sang đây vì nó KHÔNG theo git

Máy cũ có 5 ghi chú trong `~/.claude/projects/.../memory/`. Chúng sẽ mất khi đổi máy,
nên phần **thuộc về dự án** được chép xuống đây. Phần thuộc về *máy cụ thể* thì bỏ, vì
máy mới sẽ khác.

### 5.1 Cách người dùng muốn làm việc

```text
· Hỏi từng quyết định qua FORM để tích chọn, KHÔNG liệt kê rồi chờ trả lời bằng chữ.
  Hỏi TỪNG CÂU MỘT. Preview trong form phải ngắn, dài quá thì lỗi payload.
· Phản biện TRƯỚC khi đề xuất. Không chỉ đồng ý.
· Ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết.
· Ghi quyết định xuống tài liệu NGAY khi chốt — dự án đã từng mất tài liệu 02 cùng
  toàn bộ Success Metrics mà không ai phát hiện. Đó là failure mode ĐÃ XẢY RA,
  không phải giả thuyết.
· Không tự chuyển PROPOSED → CONFIRMED, không tự đóng OPEN QUESTION.
· Chốt công nghệ là quyền của người dùng.
```

### 5.2 Chỗ dễ vấp về công cụ (không phụ thuộc máy)

```text
· PowerShell 5.1 phá UTF-8 tiếng Việt khi roundtrip file:
  (Get-Content -Raw) -replace ... | Set-Content  →  mojibake.
  Sửa file có tiếng Việt thì dùng tool Write/Edit.

· File .bat: chữ có dấu HỎNG khi cmd PARSE nó như lệnh (echo, rem trong khối if),
  nhưng AN TOÀN trong set "VAR=giá trị có dấu" — ngoặc kép bao trọn nên không bị
  parse. Đã kiểm xuyên suốt bat → biến môi trường → os.environ của Python: tiếng
  Việt có dấu, ngoặc kép LỒNG và dấu nháy đơn đều đi nguyên vẹn.
  → Phần chữ hiển thị viết KHÔNG DẤU, phần dữ liệu trong set thì để dấu thoải mái.

· Python trên Windows: khi output bị pipe, stdout về cp1252 và mọi chữ tiếng Việt
  ném UnicodeEncodeError. Năm script trong scripts/jira-export/ đã tự ép UTF-8;
  script mới phải làm theo (chép khối `for _luong in (sys.stdout, sys.stderr)`).

· ⚠ VÀ CHIỀU ĐỌC CŨNG VẬY, chỗ này chưa được ghi cho tới 2026-09-10 và đã cắn 3 lần
  trong một phiên: `open()` không truyền encoding cũng mặc định cp1252, nên đọc một
  file UTF-8 có tiếng Việt (appsettings.Local.json, mọi file docs/) chết bằng
  `UnicodeDecodeError: byte 0x81`. Thông điệp lỗi trỏ về FILE, trong khi lỗi nằm ở
  CÁCH MỞ file — nên rất dễ đi sửa sai chỗ.
  → Luôn `io.open(p, encoding="utf-8")` (và `utf-8-sig` nếu nghi có BOM).
  → Và một hệ quả xấu hơn: khi lỗi xảy ra ở `print()` SAU khi file đã ghi xong thì
    mã thoát 1 KHÔNG có nghĩa là việc chưa làm. Kiểm bằng grep trước khi chạy lại,
    kẻo chèn hai lần vào tài liệu.

· Heredoc Python-trong-Bash làm BUNG escape \n thành newline thật → SyntaxError.
  Vấp 3 lần trong một phiên. Sửa code có escape thì dùng tool Write/Edit.
```

### 5.3 Thứ phụ thuộc máy — kiểm lại trên máy mới

```text
· psql trên máy cũ KHÔNG có trên PATH, nằm ở D:\ProgramFile\PostgreSQL\18\bin.
  Máy mới: tự tìm lại.
· Cổng 8765 trên máy cũ bị IIS chiếm (một site TodoApi cũ) → bind fail và request
  rơi vào IIS trả 500.19, rất dễ tưởng là lỗi của mình. Máy mới: kiểm cổng trước
  khi dựng server test.
· newman 6.2.2 có sẵn qua npx trên máy cũ (`npx --no-install newman run <file>`).
```

---

## 6. Đang dở việc gì — đọc `00_CURRENT_STATE.md` để đủ, đây là bản nén

> ⚠ **Viết lại 2026-09-10.** Bản cũ (2026-09-05) liệt ba câu chờ quyết mà **cả ba đã
> chuyển trạng thái**, và ghi "việc làm được ngay" là dựng cây quyết định — việc đó
> **đã xong** (`docs/11`). Giữ lập luận cũ thì tốt, nhưng đừng làm theo danh sách cũ.

### 6.1 · Việc chặn DUY NHẤT, và nó không phải việc của code

```text
🛑 Tài khoản API Anthropic hết credit, và việc nạp CẦN TỔ CHỨC DUYỆT.
   Đã kiểm 2026-09-10, và kiểm được với giá $0:
     GET  /v1/models    -> 200  (khoá hợp lệ, 108 ký tự, xác thực qua)
     POST /v1/messages  -> 400  "Your credit balance is too low"
     POST /v1/messages/count_tokens -> 400  (cùng cổng credit)
   Một lượt 400 vì hết credit KHÔNG bị tính tiền -> câu "còn credit không" hỏi được
   miễn phí, và nó tách ba nguyên nhân: khoá sai (401) · hết credit (400 credit) ·
   schema bị từ chối (400 schema).
```

**Nạp được credit rồi thì chạy đúng hai lệnh** (mọi thứ khác đã sẵn sàng):
```bash
dotnet run --project tools/SoanNhapRunner -- "Phân quyền" --ra nhap-nhom1.json
python scripts/jira-export/nhom_sop.py --kiem-cay nhap-nhom1.json
```

⚠ Còn **một** ẩn số API duy nhất: `output_config.format` có nhận schema có
`"type": ["string","null"]` không. **Cố ý KHÔNG phòng thủ trước** — nếu bị từ chối thì
API trả 400 lúc validate, **tốn $0**. Cái giá của việc SAI mới quyết định có nên đề
phòng, không phải cái giá của việc biết. Xem `07` §3 `IM-28`.

### 6.2 · Làm được ngay, KHÔNG cần credit

Đường `--xuat-payload` (2026-09-10) mở ra việc này: xuất payload **đã qua cổng che**
rồi để một ngữ cảnh sạch sinh bản nháp. Đã dùng nó để có bản B đầu tiên.

```text
AR-q   Viết ĐỊNH NGHĨA + ca biên cho 5 ô phân bố vào `description` của schema, rồi
       đo lại bằng cách cho HAI ngữ cảnh sạch xếp cùng một nhóm và so.
       Vì sao gấp: đây là DỤNG CỤ ĐO của M2. Hiện hai người đọc xếp khác nhau 3/10
       ticket, và ô "bước kiểm NGOÀI ticket" — ô mang phát hiện nền của cả dự án —
       về 0 ở bản B.  -> 07 §5 AR-q
AR-k   Luật gán nhãn thay `machineReadability` hằng số. Là BUG THẬT trong đường nạp
       đã build, không phải câu hỏi thiết kế.
18 nhóm còn lại: sinh bản B cho chúng qua cùng đường, để biết prompt có tổng quát hoá
       ngoài nhóm 1 hay không.
Đo lại định tuyến kiểu TỪNG TICKET MỘT (hiện chạy theo lô 30 -> xem docs/13 §1 giới hạn a).
```

### 6.3 · Ba câu chờ NGƯỜI DÙNG quyết

```text
AR-p   MỚI, và giờ có BA phép đo chống lưng. Máy có được trích case NGOÀI nhóm để dựng
       bước loại trừ không? Đây đã thành câu về TAXONOMY, không còn là câu về prompt:
         docs/11 §9  bước kiểm đầu của nhóm 2 bị chặn bởi nguyên nhân của nhóm 1
         AR-p        bản A phải MƯỢN ES-343036 của nhóm khác để dựng K5
         docs/13     4/5 lỗi định tuyến nằm ở SEAM giữa hai nhóm kề nhau
       Vẽ lại các nhóm quanh nhà cung cấp thì giải cả ba cùng lúc.  -> 07 §5 AR-p
AR-m   Nội dung người gửi ĐÃ RÚT LẠI — chưa có chỗ nào đánh dấu.
       ⚠ Phải quyết TRƯỚC khi chốt cách cắt transcript; cắt rồi không ghép lại được.
AR-n   Dữ liệu BÊN THỨ BA (khách của khách sạn) và dữ liệu THƯƠNG MẠI trong evidence.
```

### 6.4 · Cổng CỨNG phải qua trước khi bật tính năng tư vấn

```text
🛑 AR-l — ranh giới KHÁCH SẠN chưa được thực thi ở BẤT KỲ TẦNG NÀO (đã chốt nhánh lùi
   2026-09-07, vì không nguồn nào cho ≥2 giá trị phân biệt).
   Và tài liệu đã ghi rõ: phải mở lại TRƯỚC khi có luồng duyệt thật, vì rò xảy ra ở
   khâu XUẤT BẢN SOP — mà "hệ thống hướng dẫn cách giải quyết" CHÍNH LÀ xuất bản SOP.
   RLS KHÔNG cứu được kiểu rò này: nó rò ở khâu xuất bản, không ở khâu truy vấn hàng.
```

### 6.5 · Khoảng cách tới "issue mới → hướng dẫn tự động"

**Ẩn số nghiên cứu khó nhất đã trả lời XONG, và trả lời là CÓ** (`docs/13`: định tuyến
84,1% so với đoán mù 11,4%). Trước đó retrieval đo được gần bằng chance (34% vs 31%) và
điều đó *có thể* đã phủ định cả ý tưởng. Giờ biết là không — nhưng bằng cách **đổi cơ
chế**: phân loại, không phải tìm case giống bằng văn bản.

Còn lại **không phải ẩn số nghiên cứu, mà là việc build + việc quản trị**:

```text
1  Endpoint để HỎI. Hiện chỉ có 2 endpoint NẠP (/signals/case-observed, case-evidence)
   cùng /health và /internal/tenant-boundary. Không có đường vào cho một câu hỏi.
2  Kho tri thức RỖNG. `KnowledgeRecord.Approve()` có ở Domain nhưng KHÔNG endpoint nào
   gọi. 0/19 nhóm được duyệt. M2 chưa có phần (a) và (c).
   -> Định tuyến đúng nhóm chỉ có nghĩa NẾU nhóm đó có một SOP ĐÃ DUYỆT để trả về.
3  Cổng "KHÔNG BIẾT" phải là PHẦN CỦA SẢN PHẨM, không phải tuỳ chọn. 40,3% dám gán
   trên case rỗng nghĩa là: bật tính năng mà thiếu cổng này thì ~4/10 ticket không xác
   định được nguyên nhân sẽ nhận một hướng dẫn KHÔNG KIỂM ĐƯỢC. Ngưỡng phải ĐO.
4  AR-l (§6.4) — cổng cứng.
5  41% case không có nguyên nhân trong hồ sơ (62/150). KHÔNG phải bài toán của mô hình:
   nguyên nhân xảy ra trên remote/điện thoại. Trùng đúng việc R-K4 Q2 đã chốt.
```

⚠ Và **quét thêm dữ liệu KHÔNG tạo ra thêm SOP** — đã đo: corpus 12 tháng có 100% case
đã đóng nhưng chỉ **1,6 mẩu dùng được mỗi case**, *mỏng hơn* corpus 4 ngày (2,5), và 25%
case có 0 mẩu. Nút cổ chai không phải khối lượng dữ liệu. Việc quét lại 3 tháng vẫn đáng
làm nhưng vì lý do KHÁC: `R-K4` Q2 đã chốt JQL mới **không lọc case đã đóng**, và mọi JQL
trong `jira-config.example.bat` đều lọc — chính chỗ làm mẫu lệch.

---

## 7. Một nguyên tắc rút ra ngày 2026-09-05, mang theo sang máy mới

```text
dữ liệu thật để TÌM LỖI TRONG CODE      n=1 là ĐỦ
     phát biểu về SỰ TỒN TẠI — một mẫu chứng minh được

dữ liệu thật để CHỐT KIẾN TRÚC           n=1 KHÔNG đủ
     phát biểu về PHÂN BỐ — cần đại diện
```

Phiên 2026-09-04/05 dùng **cùng một corpus** cho cả hai và trình bày với **cùng độ chắc
chắn**. Người dùng bắt được, và một kết luận đã phải hạ cấp từ *"quyết định kiến trúc"*
xuống *"phép đo trên một nguồn"*. Lý do sâu hơn: `G1` nói Jira là **connector**, không
phải product boundary; `G12` nói đặc điểm dữ liệu của một khách là **tham số**, không
phải hằng số thiết kế. Chi tiết ở `07` §5 `R-K4`.
