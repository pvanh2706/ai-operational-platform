# 16 — GÓI DUYỆT: một bản nháp quy trình xử lý, cần người làm support sửa

> **File này được SINH TỰ ĐỘNG** từ `scripts/jira-export/goi_duyet.py` và từ một file
> cây quyết định. Nếu bạn bảo trì repo thì đừng sửa trực tiếp vào đây — sửa file
> JSON rồi sinh lại. Người DUYỆT thì cứ ghi thoải mái vào bản của mình.

---

## Gửi anh/chị làm support

Chúng tôi đang thử dựng lại **quy trình xử lý** cho một loại sự cố hay gặp về hoá đơn
điện tử, bằng cách đọc lại 10 ticket đã đóng trên Jira. Kết quả ở dưới.

**Chỗ chúng tôi chắc chắn còn sai: ticket ghi được *kết luận*, nhưng hầu như không ghi
*cách anh/chị biết*.** Có ticket kết thúc bằng đúng tám chữ — *"Sai CCCD"*. Những
chỗ như vậy chúng tôi **tự đoán**, và đã đánh dấu ⚠ **CHÚNG TÔI TỰ ĐOÁN** ngay tại chỗ đoán.
Chỉ anh/chị nói được chỗ nào đoán sai.

**Mất bao lâu:** khoảng 20—30 phút. **Không cần đọc lại ticket** — mã ticket để
ở đó cho anh/chị tra khi muốn, không phải để anh/chị đi kiểm tra chúng tôi.

**Trả lời thế nào:** ghi thẳng vào cột **“Sửa lại”**, hoặc trả lời theo số câu ở phần
cuối. Đúng thì để trống — **ô trống nghĩa là “đúng”**, không phải “chưa xem”.

🛑 **Chỗ hữu ích nhất lại là chỗ không có ô trống nào sẵn:** *bước kiểm nào chúng
tôi KHÔNG CÓ trong danh sách* — thứ anh/chị vẫn làm mà không ai ghi vào ticket.
Xin ghi vào mục **“Còn thiếu”** ở cuối mỗi phần. Kể cả *"gọi điện hỏi khách"* hay
*"remote vào xem"* — đó vẫn là bước kiểm, và đúng là loại không bao giờ được ghi lại.

---

# Phần 1. Phân quyền & ký hiệu hoá đơn chưa sẵn sàng

**Số ticket đứng sau phần này:** 10

## 1.1 Khách báo vào bằng những câu nào

| Khách nói / màn hình báo | Ticket | Sửa lại |
|---|---|---|
| "Dạ hđ em xuất ko dc, hỗ trợ giúp em nha" — khách chỉ gửi ảnh chụp màn hình lỗi, không đọc ra chữ báo lỗi. | ES-343662, ES-341317 | |
| Bấm phát hành thì báo đỏ / báo lỗi "Validation form error" trên đúng một mã đặt phòng. | ES-341290, ES-346647 | |
| "a chọn nó không ra kí hiệ em à" — mở ô Ký hiệu hoá đơn ra nhưng danh sách không có dòng nào để chọn. | ES-346396 | |
| "dạ không hiện mục KÝ HIỆU HÓA ĐƠN ạ" — trên màn xuất hoá đơn không thấy mục Ký hiệu hoá đơn. | ES-346039 | |
| "cái phần kí hiệu hóa đơn là mình tự điền hay là khi phần mềm đồng bộ là nó tự nhảy ạ" — khách chỉ hỏi, chưa có lỗi nào. | ES-344424 | |
| "c đã phân quyền cho FO xuất hoá đơn" / "e cần ultra view chỉ phân quyền lại giúp để FO xuất hoá đơn đc ạ" — nhân viên lễ tân không vào được màn kế toán, không có báo lỗi nào. | ES-343733 | |
| Ghi nhận qua cuộc gọi, trong ticket chỉ còn ba chữ "Chưa phân quyền" — không có câu nào của khách và không có báo lỗi. | ES-346065 | |
| Site/MST mới: đã cấu hình trên admin + PMS nhưng xem trước PDF trả về "Ký hiệu hóa đơn không tồn tại", và tài khoản đang dùng chỉ vào được invoice admin của MST cũ, MST mới thì không vào được. | ES-346559 | |

## 1.2 Các bước kiểm, theo thứ tự chúng tôi đoán

> 🛑 **Thứ tự này chúng tôi TỰ ĐẶT.** Không ticket nào ghi thứ tự kiểm, nên
> đây là chỗ sai nhiều nhất. Nếu thực tế anh/chị kiểm theo thứ tự khác, xin ghi ra.

### K1. Trên màn phát hành hoá đơn của ĐÚNG mã đặt phòng đang lỗi (hỏi khách mã đặt phòng / mã phòng trước), ô "Ký hiệu hoá đơn" đang ở trạng thái nào trong ba trạng thái: (a) trống nhưng bấm vào thì CÓ ký hiệu để chọn, (b) bấm vào thì danh sách RỖNG / không thấy cả mục Ký hiệu hoá đơn, (c) đã có ký hiệu được chọn sẵn mà vẫn báo đỏ?

- **Xem ở:** PMS > mở đặt phòng đang lỗi > màn phát hành hoá đơn điện tử > ô "Ký hiệu hoá đơn" (bấm mở danh sách ra, không chỉ nhìn ô trống).
- **Cần quyền:** Phải xem được màn phát hành hoá đơn bằng chính phiên của người đang lỗi: hoặc có quyền vào màn kế toán/hoá đơn trong PMS, hoặc xin được ultraview + tài khoản của khách (ES-343733 phải xin ultraview mới xem được).
- **Lấy từ ticket:** Mã đặt phòng 2387 của căn Nexus Xuân Thủy báo lỗi “Validation form error” nguyên nhân do chưa chọn kí hiệu hóa đơn. (ES-341290) — và "Do mình chưa chọn ký hiệu hóa đơn nên không xuất được ạ" (ES-341317)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Ô ký hiệu đang trống nhưng bấm vào thì CÓ ký hiệu trong danh sách để chọn; chọn xong phát hành được. | `S1` | ✔ có ca thật | ES-341317, ES-341290 | |
| Cùng nhánh trên, nhưng ticket chỉ ghi nhân viên bảo khách "chọn ký hiệu hóa đơn" rồi thấy hoá đơn ra — không ghi đã mở màn nào và thấy ô ký hiệu thế nào. | `S1` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-343662 | |
| Bấm vào ô ký hiệu thì danh sách RỖNG, hoặc không thấy cả mục "Ký hiệu hoá đơn" trên màn xuất hoá đơn. | `K2` | ✔ có ca thật | ES-346396, ES-346039 | |
| Người dùng không vào được cả màn kế toán / màn phát hành nên chưa tới được ô ký hiệu. | `K3` | ✔ có ca thật | ES-343733 | |
| Ô ký hiệu đã có giá trị được chọn mà vẫn báo đỏ, và đây là site/MST vừa được tạo. | `K4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-346559 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Nhảy thẳng sang câu "anh/chị chọn ký hiệu hoá đơn giúp em" mà chưa hỏi danh sách ký hiệu có gì để chọn hay không: ES-346396 làm đúng như vậy và khách trả lời "a chọn nó không ra kí hiệ em à", mất một lượt hỏi đáp; ES-343662 may mắn đúng nên xong ngay. Hai trạng thái "chưa chọn" và "không có gì để chọn" nhìn từ khách là như nhau.

### K2. Tài khoản NÀO đang đăng nhập để phát hành (hỏi thẳng email đăng nhập của khách), và tài khoản đó có mặt trong Cấu hình > Quản lí người dùng của admin.ezinvoice.vn không, rồi nó có nằm trong mục "User truy cập / Nhóm truy cập" của ký hiệu cần dùng không?

- **Xem ở:** admin.ezinvoice.vn > Cấu hình > Quản lí người dùng (tài khoản có tồn tại trong ezInvoice không) và admin.ezinvoice.vn > Cấu hình > Ký hiệu > chọn ký hiệu > mục User truy cập/Nhóm truy cập (email đó có trong danh sách không) + Danh sách ký hiệu (có ký hiệu nào chưa).
- **Cần quyền:** Phải đăng nhập admin.ezinvoice.vn bằng một tài khoản ĐÃ được phân quyền trong ezInvoice mới xem/thao tác được phần người dùng và ký hiệu — chính tài khoản đang bị thiếu quyền thì không thấy gì. ES-346396: "Dùng tài khoản đã được phân quyền trong ezInvoice như người dùng: ngochan12052503@gmail.com thì mới thao tác phân quyền người dùng ạ." Nhiều ca support không tự làm được, phải nhờ tài khoản admin của khách (ES-346039).
- **Lấy từ ticket:** Anh phát hành bằng người dùng linhsangpham280301@gmail.com này có phải không ạ / Người dùng này chưa được phân quyền kí hiệu hóa đơn  và người dùng trên ezInvoice nên không phát hành được hóa đơn điện tử cũng như chọn được kí hiệu hóa đơn ạ. (ES-346396)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Tài khoản đang phát hành CHƯA có trong Quản lí người dùng của admin.ezinvoice.vn (chưa được mời vào ezInvoice). | `S2` | ✔ có ca thật | ES-346396, ES-341290 | |
| Tài khoản đã có trong ezInvoice nhưng CHƯA nằm trong User truy cập của ký hiệu → danh sách ký hiệu rỗng / không hiện mục Ký hiệu hoá đơn. | `S3` | ✔ có ca thật | ES-346039 | |
| Danh sách ký hiệu trong ezInvoice trống, chưa có ký hiệu/mẫu hoá đơn nào để mà phân quyền. | `S4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |
| Người đang phát hành là một tài khoản KHÁC với tài khoản đã được cấu hình (vd lễ tân dùng tài khoản riêng) nên báo đỏ. | `S2` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-346647 | |
| Tài khoản đã có đủ quyền trong ezInvoice mà vẫn không vào được màn kế toán trong PMS. | `K3` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Hai lỗ đã vấp thật: (1) đăng nhập admin.ezinvoice.vn bằng chính tài khoản đang thiếu quyền thì không sửa được gì, ES-346396 phải chỉ định tài khoản khác (ngochan12052503@gmail.com) mới thao tác được; (2) ES-341290 — dùng một tài khoản để xuất hoá đơn và một tài khoản để thao tác PMS trên CÙNG một trình duyệt gây xung đột tài khoản, ra lỗi số thập phân, phải mở hai trình duyệt.

### K3. Người bấm phát hành thuộc role nào trong PMS, và role đó có được tick quyền "Thêm hóa đơn" chưa (đó chính là quyền chặn họ vào màn kế toán)?

- **Xem ở:** PMS > phần phân quyền/role của khách sạn > mở role của đúng người dùng đó > nhóm quyền hoá đơn > mục "Thêm hóa đơn". Ticket không ghi đường menu chính xác tới màn này.
- **Cần quyền:** Quyền quản trị phân quyền/role trong PMS. ES-343733: nhân viên hỗ trợ không tự vào được, phải xin ultraview và tài khoản của khách ("Chị cho em xin ulatrview để em kiểm tra nhé ạ").
- **Lấy từ ticket:** Chị ơi, nguyên nhân các bạn không vào được màn kế toán là do chưa được phân quyền Thêm hóa đơn, Hiện tại, quyền này đã được thêm rồi, mai chị báo các bạn vào lại xem được chưa giúp em nhé ạ (ES-343733)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Role của user thiếu quyền "Thêm hóa đơn" → không vào được màn kế toán. | `S5` | ✔ có ca thật | ES-343733 | |
| Ticket chỉ ghi "chưa phân quyền" mà không nói là quyền trong PMS hay quyền trên ezInvoice. | `K2` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-346065 | |
| Role đã có quyền hoá đơn, vào được màn kế toán, mà vẫn không phát hành được — và cơ sở/MST là mới khai. | `K4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Thêm quyền xong chưa ăn ngay: ES-343733 nhân viên phải bảo khách "đăng nhập lại giúp em" và hẹn "mai chị báo các bạn vào lại xem được chưa" — nếu kiểm ngay trên phiên đang mở thì sẽ tưởng là chưa được. Ticket không có xác nhận kết quả sau đó.

### K4. Đây có phải site/MST vừa được tạo không — ký hiệu hoá đơn đã được khai báo cho ĐÚNG từng cơ sở/mã căn của MST đó chưa, và tài khoản đang dùng có vào được invoice admin của MST MỚI (không phải MST cũ) không?

- **Xem ở:** admin.ezinvoice.vn của MST mới > Cấu hình > Ký hiệu (ký hiệu có tồn tại cho cơ sở này không) + bấm Xem trước PDF trên đúng đặt phòng của site mới + đối chiếu với file thông tin cấu hình khách gửi.
- **Cần quyền:** Tài khoản phải được cấp truy cập vào MST mới. ES-346559: "Tài khoản ngoc.avatar@gmail.com đang không truy cập được vào invoice admin (chỉ truy cập được mst cũ, mst mới 0304746657 k truy cập được)" — nên bước kiểm này có thể bị chặn ngay từ đầu.
- **Lấy từ ticket:** Support đã tạo site mới, cấu hình trên admin + pms nhưng khi xem trước pdf trả về: "Ký hiệu hóa đơn không tồn tại", dù ký hiệu này khách vẫn đang xuất tại nhà cung cấp. (ES-346559)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Xem trước PDF trả về "Ký hiệu hóa đơn không tồn tại", dù ký hiệu đó khách vẫn đang xuất bình thường bên nhà cung cấp. | `S6` | ✔ có ca thật | ES-346559 | |
| Tài khoản chỉ vào được invoice admin của MST cũ, MST mới thì không vào được. | `S7` | ✔ có ca thật | ES-346559 | |
| Không phải site/MST mới, và ba bước trên đều không ra nguyên nhân. | `NGOAI-PHAM-VI` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** "Đã cấu hình trên admin + pms" không có nghĩa là đủ: ES-346559 phải cấu hình cho hai căn 1497 và 1708 riêng, tức khai theo TỪNG cơ sở. Và vì ký hiệu vẫn xuất được bên nhà cung cấp nên rất dễ kết luận sai rằng ký hiệu đã đúng, rồi đi tìm nguyên nhân ở chỗ khác.

## 1.3 Cách sửa

| Mã | Việc | Ticket | Sửa lại |
|---|---|---|---|
| `S1` | Chọn Ký hiệu hoá đơn trên màn phát hành của đặt phòng đó rồi phát hành lại. | ES-343662, ES-341317, ES-341290 | |
| `S2` | Thêm tài khoản đang phát hành vào ezInvoice: truy cập admin.ezinvoice.vn > Cấu hình >> Quản lí người dùng >> chọn mời thêm tài khoản khác. Phải làm bằng một tài khoản đã được phân quyền trong ezInvoice; nếu support không có, báo tài khoản admin của khách làm. | ES-346396, ES-341290, ES-346647 | |
| `S3` | Phân quyền ký hiệu hoá đơn cho tài khoản đó: admin.ezinvoice.vn > Cấu hình > Ký hiệu > chọn ký hiệu cần phân quyền > mục User truy cập/Nhóm truy cập > chọn người dùng/nhóm > Thêm > Cập Nhật. | ES-346039, ES-346396, ES-341290, ES-344424 | |
| `S4` |  | — | |
| `S5` | Trong PMS, thêm quyền "Thêm hóa đơn" cho role của người dùng đó, rồi cho họ đăng nhập lại và vào lại màn kế toán để kiểm. | ES-343733 | |
| `S6` | Với site/MST mới: khai ký hiệu hoá đơn cho ĐÚNG từng cơ sở/mã căn trên admin ezInvoice của MST mới, đối chiếu lại cấu hình admin và PMS theo file thông tin cấu hình khách gửi, rồi Xem trước PDF lại. | ES-346559 | |
| `S7` | Cấp cho tài khoản đang dùng quyền truy cập invoice admin của MST mới (hiện chỉ vào được MST cũ). | ES-346559 | |
| `S8` | Ca chỉ hỏi, chưa lỗi: trả lời rằng ký hiệu hoá đơn KHÔNG tự nhảy khi phần mềm đồng bộ — nó phải được phân quyền sử dụng cho tài khoản, thao tác cập nhật ở admin.ezinvoice.vn. | ES-344424 | |

## 1.4 Khi nào coi là xong

**Kiểm bằng CHÍNH tài khoản của người đang lỗi (không phải tài khoản admin), sau khi họ đăng nhập lại: (1) mở đúng đặt phòng đó > màn phát hành hoá đơn > bấm ô Ký hiệu hoá đơn thì ra ít nhất một ký hiệu và chọn được; (2) Xem trước PDF ra được bản PDF mang đúng ký hiệu của cơ sở đó, không còn dòng "Ký hiệu hóa đơn không tồn tại"; (3) phát hành xong thì hoá đơn xuất hiện trong danh sách hoá đơn của đặt phòng và có số hoá đơn. Với ca không có báo lỗi từ đầu (khách chỉ hỏi, hoặc chỉ xin phân quyền cho FO) thì điều kiện đóng là: email đó xuất hiện trong Quản lí người dùng và trong User truy cập của ký hiệu trên admin.ezinvoice.vn, và người đó tự vào được màn kế toán trong PMS.**

Điều kiện này đủ chưa? Nếu chưa thì còn thiếu gì:

```


```

## 1.5 8 chỗ chúng tôi TỰ ĐOÁN — xin trả lời từng chỗ

Mỗi chỗ chỉ cần một trong ba: **đúng** / **sai, phải là …** / **không bao giờ xảy ra**.

| # | Ở bước | Chúng tôi đoán rằng | Đúng / Sai / Không có |
|---|---|---|---|
| 1 | `K1` | Cùng nhánh trên, nhưng ticket chỉ ghi nhân viên bảo khách "chọn ký hiệu hóa đơn" rồi thấy hoá đơn ra — không ghi đã mở màn nào và thấy ô ký hiệu thế nào. | |
| 2 | `K1` | Ô ký hiệu đã có giá trị được chọn mà vẫn báo đỏ, và đây là site/MST vừa được tạo. | |
| 3 | `K2` | Danh sách ký hiệu trong ezInvoice trống, chưa có ký hiệu/mẫu hoá đơn nào để mà phân quyền. | |
| 4 | `K2` | Người đang phát hành là một tài khoản KHÁC với tài khoản đã được cấu hình (vd lễ tân dùng tài khoản riêng) nên báo đỏ. | |
| 5 | `K2` | Tài khoản đã có đủ quyền trong ezInvoice mà vẫn không vào được màn kế toán trong PMS. | |
| 6 | `K3` | Ticket chỉ ghi "chưa phân quyền" mà không nói là quyền trong PMS hay quyền trên ezInvoice. | |
| 7 | `K3` | Role đã có quyền hoá đơn, vào được màn kế toán, mà vẫn không phát hành được — và cơ sở/MST là mới khai. | |
| 8 | `K4` | Không phải site/MST mới, và ba bước trên đều không ra nguyên nhân. | |

## 1.6 Còn thiếu — bước kiểm anh/chị vẫn làm mà không có ở trên

```




```

---

# Phần cuối. Năm câu xin trả lời bằng chữ

**Câu 1.** Thứ tự các bước kiểm ở trên có đúng thứ tự anh/chị THỰC SỰ làm không? Nếu không, thứ tự đúng là gì?

```


```

**Câu 2.** Có bước nào ở trên mà thực tế **không ai làm** vì không đáng làm — tức chúng tôi đã thêm việc vô ích?

```


```

**Câu 3.** Bước kiểm nào hay phải làm mà **không tự làm được** — thiếu quyền, thiếu màn hình, hay phải nhờ người khác?

```


```

**Câu 4.** Nếu đưa hai quy trình này cho một bạn **mới vào làm**, chỗ nào bạn đó sẽ hiểu sai hoặc làm sai thứ tự?

```


```

**Câu 5.** Có loại sự cố hoá đơn nào **hay gặp hơn** hai loại này mà chúng tôi bỏ qua không?

```


```

---

Cảm ơn anh/chị. Phần anh/chị sửa được dùng để đo xem bản nháp do máy dựng còn lệch
bao nhiêu so với người làm thật — nên **chỗ nào anh/chị sửa nhiều nhất là chỗ
có giá trị nhất**, không phải chỗ làm chúng tôi mất mặt.
