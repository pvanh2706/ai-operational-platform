# 19 — HỢP ĐỒNG TÍCH HỢP

> **Sinh 2026-09-17.** Tài liệu dành cho **người viết code ở phía bên kia** — connector Jira,
> CRM, helpdesk. Không phải tài liệu nội bộ.
>
> Phần **GỌI VÀO** sinh tự động từ code, luôn khớp với bản đang chạy:
>
> ```
> GET /openapi/v1.json
> ```
>
> File này chỉ chứa thứ OpenAPI **không tả được**: đường sản phẩm **GỬI RA**, và những lời
> hứa về hành vi mà một schema không nói được.

---

## 0. Mô hình, trong ba câu

Sản phẩm này **không phải** một app để đăng nhập. Nó là service phản ứng theo sự kiện:

```
phần mềm của bạn  --(1) tín hiệu-->  sản phẩm  --(2) webhook-->  phần mềm của bạn
                                        |
                                        +--(3) link có hạn -->  người làm support
```

Ba đường, ba cơ chế xác thực khác nhau — và đó là chỗ dễ nhầm nhất, nên nói trước:

| Đường | Ai xác thực | Bằng gì |
|---|---|---|
| (1) bạn gọi vào | bạn | `Authorization: Bearer kp_...` |
| (2) sản phẩm gọi ra | **sản phẩm** | chữ ký HMAC trong header — *bạn* phải kiểm |
| (3) người bấm link | chính cái link | token nằm trong URL |

---

## 1. Khoá API

Mỗi khách hàng một khoá riêng, thu hồi được riêng. Khoá **xác định luôn khách hàng** — không
có header nào để tự khai mình là ai.

```http
Authorization: Bearer kp_<id>_<secret>
```

Người vận hành sản phẩm tạo khoá cho bạn bằng:

```bash
dotnet run --project src/KnowledgePlatform.Api -- --tao-khoa <mã-khách-hàng> "connector Jira"
```

Chuỗi khoá **chỉ hiện ra đúng một lần**. Mất thì xin khoá mới — không ai đọc lại được.

⚠ **Bốn tình huống dưới đây trả `401` GIỐNG HỆT NHAU**, và đó là chủ đích: thiếu khoá · sai
khoá · khoá đã thu hồi · khoá của khách hàng khác. Phân biệt chúng ra là dựng sẵn một kênh dò.
Đừng viết code phân nhánh theo nội dung câu báo lỗi.

Ba endpoint **không** cần khoá: `/health`, `/health/ready`, và `/xem/{token}`.

---

## 2. Bạn gửi tín hiệu vào

```bash
curl -X POST https://<host>/signals/case-observed \
  -H "Authorization: Bearer kp_..." \
  -H "Content-Type: application/json" \
  -d '[{"sourceReference":"jira:ES-12345","subject":"Không phát hành được hoá đơn"}]'
```

**Ba lời hứa đáng dựa vào:**

```text
GỬI LẠI KHÔNG SINH BẢN THỨ HAI   cùng `sourceReference` -> trả lại Case cũ, `created: false`.
                                 Cứ retry thoải mái.
VƯỢT TRẦN THÌ TỪ CHỐI CẢ LÔ      kèm nói rõ trần là bao nhiêu. KHÔNG cắt bớt im lặng —
                                 200 mà thiếu dữ liệu là kiểu hỏng tệ nhất ở đường nạp.
MỘT LÔ, MỘT KẾT QUẢ MỖI PHẦN TỬ  đọc `results[]` để biết phần tử nào mới, phần tử nào đã có.
```

⚠ Khi nạp **evidence** (`/signals/case-evidence`), trường `subTenantCode` là **bắt buộc**.
Chưa biết thì khai `khong-xac-dinh`; việc nội bộ không thuộc khách sạn nào thì khai
`khong-thuoc-khach-san`. Bỏ trống là `400` — sản phẩm cố ý **không tự điền hộ**, vì *"chưa
biết"* và *"chưa ai hỏi"* là hai chuyện khác nhau và về sau không dựng lại được.

### 2.1 Báo việc ở nguồn đổi trạng thái

```bash
curl -X POST https://<host>/signals/case-status \
  -H "Authorization: Bearer kp_..." \
  -H "Content-Type: application/json" \
  -d '[{"sourceReference":"jira:ES-12345","daXongONguon":true,"sourceResolvedAt":"2026-09-17T09:00:00+07:00"}]'
```

```text
daXongONguon        BẮT BUỘC, true/false. Cố ý KHÔNG suy từ việc có hay không có
                    `sourceResolvedAt`: "chưa gửi" và "đã mở lại" đều cho ra một ô trống,
                    mà hai điều đó khác hẳn nhau.
case chưa tồn tại   trả `khong-thay-case` cho riêng phần tử đó — KHÔNG tự tạo. Một case
                    dựng từ tín hiệu này sẽ không có tiêu đề. Gửi case-observed trước.
gửi lại cùng trạng thái  trả `khong-doi`. Không phải lỗi.
```

🛑 **Endpoint này cố ý KHÔNG kích hoạt gì thêm** — nó không sinh ra một lượt trả kết quả về.
Lý do là một số đo, không phải trực giác: trên corpus 12 tháng, case **đã đóng** chỉ có 1,6
mẩu dùng được mỗi case, **mỏng hơn** corpus 4 ngày (2,5) vốn phần lớn còn mở. Bước kết luận
thường xảy ra ở nơi khác (phiên remote, cuộc gọi) và chỉ để lại trong ticket một dòng *"Done
nhé"*. Nếu bạn mong nhận được hướng dẫn lúc đóng case, **bạn sẽ không nhận được** — và đó là
thiết kế.

### 2.2 Người dùng hỏi về tài liệu

Không có endpoint riêng. Dùng `POST /hoi` — một cửa cho mọi câu hỏi, một cổng KHÔNG BIẾT.

---

## 3. Sản phẩm gửi kết quả về — phần OpenAPI không tả được

Khai báo nơi nhận một lần:

```bash
curl -X PUT https://<host>/internal/webhook \
  -H "Authorization: Bearer kp_..." \
  -H "Content-Type: application/json" \
  -d '{"url":"https://connector.cua-ban/kp-webhook"}'
```

Response chứa `biMatKy` — **chỉ lần này**. Lưu nó ở nơi bạn lưu bí mật; bạn cần nó để kiểm
chữ ký. Đổi địa chỉ về sau **không** đổi bí mật, cố ý: đổi cả hai cùng lúc làm mọi gói đang
bay trở thành gói không kiểm được.

### 3.1 Gói gửi tới bạn

```http
POST /kp-webhook HTTP/1.1
Content-Type: application/json
X-KP-Signature: sha256=<hex>
X-KP-Delivery: 01a0aa95-53a5-72db-8dcc-23be22c888a1
```

```json
{
  "phienBan": "1",
  "maGoi": "01a0aa95-53a5-72db-8dcc-23be22c888a1",
  "luc": "2026-09-17T09:12:03.4512Z",
  "case": { "khoaNguon": "jira:ES-12345", "tieuDe": "Không phát hành được hoá đơn" },
  "bietTraLoi": true,
  "nhom": "Lệch cấu hình tiền/thuế",
  "nguonDinhTuyen": "noi-ro-trong-mo-ta",
  "lyDoKhongBiet": null,
  "huongDan": [
    {
      "id": "01a0a...",
      "tenNguyenNhan": "Ký hiệu hoá đơn chưa được chọn",
      "trangThai": "ACTIVE",
      "tomTat": "Vào Cấu hình > Ký hiệu, thêm ký hiệu TRƯỚC rồi mới phát hành…",
      "link": "https://<host>/xem/kpl_<id>_<secret>",
      "linkHetHanLuc": "2026-10-01T09:12:03Z"
    }
  ]
}
```

### 3.2 Kiểm chữ ký — **bắt buộc**

Chữ ký là HMAC-SHA256 tính trên **đúng chuỗi byte của thân gói**, hex thường.

⚠ Kiểm trên **raw body**, không phải trên object bạn vừa parse rồi serialize lại. Serialize
lại gần như chắc chắn cho ra chuỗi khác (thứ tự khoá, khoảng trắng, cách thoát ký tự Unicode),
và chữ ký sẽ không bao giờ khớp.

```python
import hashlib, hmac

def hop_le(raw_body: bytes, header_chu_ky: str, bi_mat: str) -> bool:
    mong = "sha256=" + hmac.new(bi_mat.encode(), raw_body, hashlib.sha256).hexdigest()
    return hmac.compare_digest(mong, header_chu_ky or "")   # so theo thời gian hằng số
```

```csharp
static bool HopLe(byte[] rawBody, string? headerChuKy, string biMat)
{
    var mong = "sha256=" + Convert.ToHexStringLower(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(biMat), rawBody));

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(mong), Encoding.UTF8.GetBytes(headerChuKy ?? ""));
}
```

### 3.3 Idempotency và thử lại

```text
maGoi / X-KP-Delivery   KHOÁ IDEMPOTENCY. Lượt thử lại mang ĐÚNG mã này.
                        -> Bạn PHẢI nhớ mã đã xử lý và bỏ qua lần thứ hai.
2xx                     coi là đã nhận. Sản phẩm đóng lượt gửi.
mọi mã khác / timeout   sản phẩm thử lại, giãn dần: 1 · 2 · 4 · 8 · 16 phút, tối đa 6 lượt.
sau 6 lượt              bỏ cuộc, ghi lại lý do. Bạn sẽ KHÔNG nhận được gói đó nữa.
```

⚠ **Trả `2xx` NGAY, xử lý sau.** Sản phẩm chờ tối đa 20 giây. Nếu bạn xử lý đồng bộ và chậm
hơn thế, gói sẽ bị coi là thất bại và gửi lại — bạn sẽ xử lý hai lần cùng một việc.

### 3.4 `bietTraLoi: false` **không phải lỗi**

Đây là chỗ dễ viết code sai nhất, nên nói thẳng: sản phẩm coi *"không biết"* là **dữ liệu
hạng nhất**. Nó trả `200` kèm một ô nói rõ là chưa biết, thay vì trả lỗi hay trả rỗng.

```text
dinh-tuyen-khong-biet          không xác định được ticket này thuộc nhóm nguyên nhân nào
nhom-chua-co-sop-duoc-duyet    xác định được nhóm, nhưng nhóm đó chưa có quy trình nào
                               được NGƯỜI duyệt
```

Hai thứ này rất khác nhau về việc phải làm tiếp, nên đừng gộp chúng vào một nhánh `else`.

🛑 **Hôm nay `bietTraLoi: false` là trạng thái THƯỜNG GẶP**, không phải ngoại lệ. Xem §5.

---

## 4. Link xem tri thức

Gói webhook mang **tóm tắt + link**, không mang toàn văn. Cố ý:

- gói gửi đi nằm lại trong hệ thống của bạn (comment, log) — càng ít nội dung ra ngoài càng ít bề mặt;
- bản đầy đủ đọc qua link thì **mỗi lượt đọc** đi qua cổng che và cổng trạng thái *tại thời
  điểm đọc* — trừ khi bản deploy bật cờ tắt-tạm cổng che (`N1-2`, 2026-10-10). Gói webhook và
  trang `/xem` mang `daQuaCongChe`; `false` nghĩa là nội dung NGUYÊN VĂN, có thể chứa mật khẩu. Nhét toàn văn vào gói là đóng băng một bản chưa chắc còn đúng, ở nơi không thu hồi được.

```text
link mở được NHIỀU LẦN trong hạn      "một lần" là một lần PHÁT, không phải một lần MỞ
hết hạn · thu hồi · sai  -> 404       giống hệt nhau, cố ý
trangThai: NEEDS_REVIEW               bản này đã có người sửa và chưa duyệt lại.
                                      VẪN mở được — hiện cảnh báo cho người đọc, đừng ẩn đi.
```

⚠ Link chỉ mở được từ máy **tới được địa chỉ đó**. Bản deploy nội bộ thì link mang hostname
nội bộ; đừng chuyển tiếp nó ra ngoài mạng rồi trông đợi nó chạy.

---

## 5. Những gì hợp đồng này **KHÔNG** hứa

Ghi ra vì đọc thiếu phần này sẽ dẫn tới kỳ vọng sai:

```text
🛑 KHÔNG hứa có câu trả lời.
   Kho tri thức chỉ trả về thứ đã được NGƯỜI duyệt. Ngày đầu tích hợp, gần như mọi tín hiệu
   sẽ nhận `bietTraLoi: false`. Đó là hệ thống chạy đúng, không phải hỏng.

🛑 KHÔNG hứa định tuyến chính xác.
   Bản đang chạy tra một bảng kết quả đã đo sẵn. Ticket nằm ngoài bảng -> `dinh-tuyen-khong-biet`.

🛑 KHÔNG hứa thứ tự gói.
   Hàng đợi có thử lại, nên gói của case B có thể tới trước case A. Dùng `case.khoaNguon`
   để ghép, đừng dựa vào thứ tự đến.

🛑 KHÔNG hứa đúng-một-lần.
   Hứa là ÍT NHẤT MỘT LẦN. Khử trùng bằng `maGoi` là việc của bên nhận.

⚠ Ở chế độ nhiều khách dùng chung, DUYỆT tri thức đang bị từ chối bằng `409` (ranh giới
  giữa các cơ sở của cùng một khách chưa thực thi được). Nghĩa là kho sẽ rỗng cho tới khi
  điều đó được giải. Chi tiết: `docs/07` §5 `AR-l`.
```

---

## 6. Phiên bản

`phienBan` trong gói webhook là phiên bản **hợp đồng**, không phải phiên bản sản phẩm.

```text
thêm một ô mới       -> GIỮ NGUYÊN số. Bên nhận phải bỏ qua ô mình không biết.
bỏ hoặc đổi nghĩa    -> TĂNG số.
```

Bên nhận nên **từ chối** phiên bản mình chưa biết, thay vì cố đoán.
