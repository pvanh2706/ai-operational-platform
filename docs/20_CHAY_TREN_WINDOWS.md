# 20 — CHẠY TRÊN MÁY WINDOWS

> **Sinh 2026-09-17.** Hồ sơ chạy bản deploy đầu tiên, theo đúng `H1-5` (*chạy local Windows
> trước*) và `H1b-1` (*máy nằm trong LAN/VPN*).
>
> ⚠ **File này KHÔNG phải nguồn sự thật về cấu hình.** Nguồn sự thật là lệnh:
>
> ```bash
> dotnet run --project src/KnowledgePlatform.Api -- --kiem-cau-hinh
> ```
>
> Nó đọc chính cấu hình đang chạy nên không lỗi thời được. File này chỉ giải thích **vì sao**
> từng giá trị được đặt như vậy — phần mà một lệnh không nói hộ được.
>
> Dự án đã đo được hai lần rằng tài liệu tự lỗi thời trong im lặng: bộ Postman đứng ở 5/13
> endpoint hơn hai tuần (`IM-38`), và `docs/10` ghi *"newman có sẵn"* trong khi máy mới không
> có. Đừng để file này thành cái thứ ba.

---

## 0. Khác gì `docs/10`

```text
docs/10  CHUYỂN MÁY — dựng lại môi trường PHÁT TRIỂN trên một máy trắng
docs/20  DEPLOY     — chạy bản phục vụ người dùng thật, trong mạng nội bộ
```

Hai việc khác nhau, và phần lớn nội dung không dùng chung được.

---

## 1. Cần gì trên máy

```text
.NET 10 SDK        (bản build hiện tại: 10.0.401)
PostgreSQL 18      role `kp_app`, KHÔNG phải superuser
                   ⚠ superuser đi vòng qua row-level security -> mọi ranh giới tenant
                     biến mất mà không có dấu hiệu nào. Xem scripts/dev-db-setup.sql.
một cổng trống     mặc định 5119
                   ⚠ ĐỪNG dùng 8765 — trên máy dev hiện tại IIS đang giữ cổng đó.
```

---

## 2. Bảy biến môi trường, và vì sao từng cái

`AR-i`/`C2-1` chốt: cấu hình đến từ **biến môi trường**, không phải file được git theo dõi.

```text
ConnectionStrings__Default          BẮT BUỘC. Thiếu là KHÔNG KHỞI ĐỘNG ĐƯỢC — cố ý, vì
                                    một app chạy mà không có DB thì mọi phép kiểm sức
                                    khoẻ đều xanh trong khi không phục vụ được gì.

Tenancy__Mode                       BẮT BUỘC. DedicatedSingleTenant | SharedMultiTenant.
                                    KHÔNG có mặc định (`G13` cấm đoán hộ): một giả định
                                    "một DB dùng chung" lọt vào là rẻ hôm nay, rất đắt
                                    khi đã có ba khách hàng.

Tenancy__TenantExternalKey          Chỉ ở chế độ dedicated. Khoá sai thì app TỪ CHỐI
                                    KHỞI ĐỘNG thay vì chạy và không thấy dữ liệu nào.

Auth__AcknowledgeUnauthenticatedApi ⚠ ĐỪNG ĐẶT ở deploy thật. Đặt `true` nghĩa là API
                                    chạy KHÔNG cần khoá. Máy dev thì hợp lý; máy phục vụ
                                    người thật thì không. Chế độ shared TỪ CHỐI cờ này.

Link__PublicBaseUrl                 🛑 Địa chỉ mà MÁY CỦA NGƯỜI ĐỌC tới được — không phải
                                    localhost. Xem §4, đây là cái bẫy nguy nhất của bản
                                    deploy local.

Webhook__ChuKyGiay                  Bao lâu quét hàng đợi gửi kết quả về. Mặc định 15.
Webhook__BatVongQuet                Đặt `false` nếu chưa muốn gửi ra ngoài. Hàng đợi vẫn
                                    được xếp — không mất gì, chỉ là chưa ai gửi.
```

Bản mẫu chạy được: [`scripts/chay-windows.example.ps1`](../scripts/chay-windows.example.ps1).

---

## 3. Dựng lần đầu, theo thứ tự

```powershell
# 1. Schema
dotnet ef database update --project src/KnowledgePlatform.Infrastructure `
  --connection "Host=localhost;Database=kp;Username=kp_app;Password=..."

# 2. Khách hàng đầu tiên
dotnet run --project src/KnowledgePlatform.Api -- --tao-khach-hang ezcloud "Công ty ezCloud"

# 3. Khoá cho connector.  ⚠ Chuỗi in ra CHỈ hiện một lần.
dotnet run --project src/KnowledgePlatform.Api -- --tao-khoa ezcloud "connector Jira"

# 4. SOI LẠI trước khi gọi là xong
dotnet run --project src/KnowledgePlatform.Api -- --kiem-cau-hinh

# 5. Chạy
.\scripts\chay-windows.example.ps1
```

⚠ Bước 2 và 3 **không** làm được bằng endpoint, cố ý: một endpoint tạo khoá thì tự nó cần
xác thực, mà thứ xác thực nó lại là khoá — vòng tròn. Ai đó sẽ giải vòng tròn bằng một
*"khoá quản trị"* cấu hình sẵn, và thế là khoá dùng chung quay lại bằng cửa sau.

---

## 4. 🛑 Cái bẫy nguy nhất của bản deploy LOCAL

`Link__PublicBaseUrl` là địa chỉ gốc của link gửi cho người làm support đọc tri thức.

**Nó chạy đúng với đúng người đi kiểm nó.** Người deploy đứng trên máy chủ, bấm link, thấy
mở được. Người support bấm từ máy của họ thì chết. Không phép kiểm tự động nào bắt được, vì
máy chạy phép kiểm cũng chính là máy đó.

```text
SAI   Link__PublicBaseUrl = http://localhost:5119
ĐÚNG  Link__PublicBaseUrl = http://ten-may-trong-lan:5119
```

Sản phẩm làm được hai việc, và **cả hai đều không thay được mắt người**:

```text
thiếu hẳn cấu hình  -> endpoint phát link trả 409, KHÔNG đoán ra localhost
trỏ vào loopback    -> cảnh báo ở stderr lúc khởi động, và --kiem-cau-hinh gọi tên nó
```

Cách kiểm thật, chỉ tốn một phút: phát một link rồi **mở nó từ máy khác**.

---

## 5. Chạy như một service

Bản đầu chạy bằng `dotnet run` trong một cửa sổ là chấp nhận được — `H1-5` chốt *chạy local
trước*, không chốt phải dựng service ngay. Khi cần nó sống qua lần đăng xuất:

```powershell
# Task Scheduler: chạy lúc khởi động máy, tài khoản chạy được đặt biến môi trường ở mức MÁY
schtasks /create /tn "KnowledgePlatform" /tr "powershell -File D:\...\chay-windows.ps1" `
         /sc onstart /ru SYSTEM
```

⚠ Đặt biến môi trường ở mức **Machine**, không phải **User** — service chạy bằng tài khoản
khác thì không thấy biến của bạn, và triệu chứng sẽ là *"chạy tay thì được, chạy service thì
không khởi động"*.

---

## 6. Kiểm sau khi deploy

```bash
curl http://<host>:5119/health          # còn sống
curl http://<host>:5119/health/ready    # DB sống VÀ ranh giới tenant còn nguyên
curl http://<host>:5119/openapi/v1.json # hợp đồng tích hợp

# và phép kiểm đầu-cuối, gọi thật cả 18 endpoint
KP_API_KEY=kp_... python scripts/kiem_dau_cuoi.py
```

🛑 **`/health` xanh KHÔNG có nghĩa là dùng được.** Nó không chạm database. Đọc
`/health/ready`, và đọc `--kiem-cau-hinh`.

---

## 7. Những gì bản deploy này CHƯA làm được

Ghi ra để không ai hứa nhầm với người dùng:

```text
🛑 Kho tri thức RỖNG cho tới khi có người support duyệt một bản nháp.
   /hoi và webhook sẽ trả "không biết" cho gần như mọi câu. Hệ thống chạy đúng.

🛑 Định tuyến là BẢNG TRA SẴN, không gọi model. Ticket ngoài bảng -> "không biết".
   Chờ credit Anthropic.

🛑 Chế độ shared TỪ CHỐI DUYỆT bằng 409 (`AR-l` chưa giải, cần `G1-1`).
   Deploy dedicated thì duyệt được bình thường.

⚠ Chưa có TLS. Bản deploy nội bộ chạy http. Khoá API đi qua mạng ở dạng đọc được —
  chấp nhận được trong LAN đã kiểm soát, KHÔNG chấp nhận được nếu mở ra internet.
⚠ Chưa có giới hạn tần suất, chưa có xoay vòng bí mật ký webhook.
```
