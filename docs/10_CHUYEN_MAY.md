# 10 — CHUYỂN SANG MÁY KHÁC

> **Viết 2026-09-05** khi người dùng chuyển máy làm việc.
> Mục đích: dựng lại **toàn bộ ngữ cảnh** trên một máy trắng, không mất thông tin.
>
> Đọc file này **sau** `00_CURRENT_STATE.md`. `00` nói *đang ở đâu*; file này nói
> *làm sao có lại được những gì máy cũ có mà git không giữ*.

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

Database kp_dev + dữ liệu      MẤT               dev-db-setup.sql rồi nạp lại fixture

Bộ nhớ của agent               MẤT               ⚠ ĐÃ CHÉP VÀO §5 CỦA FILE NÀY
(~/.claude/.../memory/)                          — đó là lý do §5 tồn tại
```

⚠ **Thứ đắt nhất KHÔNG mất, vì đã được đưa vào repo hôm nay:** kết quả của hai workflow
(28 agent + 16 agent, ~3,7 triệu token) nằm ở `docs/ket-qua-phan-tich/`. Chạy lại chúng
tốn hàng giờ. Xem §4.

---

## 2. Dựng lại từ máy trắng — theo đúng thứ tự

```bash
git clone <repo> && cd 23.ai-operational-platform

# 1. Database. Cần superuser MỘT LẦN.
psql -U postgres -h localhost -f scripts/dev-db-setup.sql

# 2. Build + schema
dotnet build src/KnowledgePlatform.slnx
dotnet ef database update --project src/KnowledgePlatform.Infrastructure \
  --connection "Host=localhost;Database=kp_dev;Username=kp_app;Password=123456"

# 3. Tenant cho máy dev
psql -U kp_app -h localhost -d kp_dev -f scripts/dev-seed-tenant.sql

# 4. Kiểm — phải 105/105 xanh
dotnet test src/KnowledgePlatform.slnx
```

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
```

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

`nguyen-nhan-150-case.json` **đã bỏ trường `trichDan`** (nguyên văn evidence của khách)
có chủ đích — đó là dữ liệu vận hành thật, và `.gitignore` của repo đặt nguyên tắc rằng
đưa nội dung nghiệp vụ của khách vào repo là **quyết định của chủ dữ liệu**, không phải
thao tác kỹ thuật. Cần trích dẫn thì chạy lại export + workflow.

Dùng lại chúng, ví dụ cho phép thử retrieval:

```bash
export TAXONOMY=docs/ket-qua-phan-tich/taxonomy-19-nhom-hoa-don.json
python scripts/jira-export/thu_retrieval.py     # cần corpus ở §3 trước
```

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
  ném UnicodeEncodeError. Bốn script trong scripts/jira-export/ đã tự ép UTF-8;
  script mới phải làm theo (chép khối `for _luong in (sys.stdout, sys.stderr)`).

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

**Ba câu đang chờ người dùng quyết:**

1. **`AR-l` sub-tenant** — bị chặn thật. Jira không có trường nào dùng được (`Mã khách
   sạn` = `-1.0` ở 32/32 case). Hai nhánh đều ngoài code: ezCloud điền trường đó cho
   thật, hoặc lùi quyết định và ghi rõ ranh giới khách sạn chưa được thực thi.
2. **`R-K4` Q2** — có mở phép đếm sang case CÒN MỞ và bắt ghi nguyên nhân lúc remote
   không? Đây là việc duy nhất làm con số 41% tiến lên.
3. **`AR-k`** — luật gán nhãn thay cho `machineReadability` hằng số.

**Việc làm được ngay, không chờ ai:** dựng cây quyết định có bước kiểm cho nhóm SOP lớn
nhất (*Phân quyền & ký hiệu hoá đơn*, 10 case). `ES-346396` chứa trọn một SOP có B1/B2
kèm nhánh điều kiện do nhân viên tự gõ — bằng chứng trực tiếp rằng dạng đầu ra này viết
được, và nó là thứ Path A phải sinh ra.

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
