# 15 — KHẢO SÁT: support THẬT SỰ làm việc thế nào

> **Sinh 2026-09-15.** Bảy câu. Khoảng **20 phút**.
>
> **Ai điền:** người làm support trực tiếp — hoặc chủ dự án, nếu đã biết câu trả lời.
> Càng gần người trực ca càng tốt: mấy câu dưới đây hỏi về *thói quen làm việc*, không hỏi
> về *quy trình trên giấy*, và hai thứ đó thường khác nhau.
>
> **Cách điền:** đánh `[x]` vào ô đúng nhất, rồi viết thêm nếu muốn. Câu nào không chắc thì
> tích ô *"không chắc"* — **đó là câu trả lời có ích**, còn đoán bừa thì không.

---

## Vì sao có bảng này

Chúng tôi đọc lại **150 ticket đã đóng** trong 12 tháng và đo được một số điều **đi ngược
với giả định ban đầu của chính chúng tôi**. Trước khi xây tiếp trên mấy con số đó, cần người
làm nghề nói xem chúng đúng hay chúng chỉ là hình chiếu của cách ghi ticket.

🛑 **Xin trả lời theo cái ĐANG XẢY RA, không theo cái LẼ RA phải xảy ra.** Nếu quy trình
viết một đằng mà thực tế làm một nẻo thì chính chỗ lệch đó mới là thứ chúng tôi cần.
Không có câu trả lời nào ở đây bị coi là sai.

---

## Câu 1 — Bước đầu tiên khi nhận một ticket là gì?

**Chúng tôi đo được:** trong **128 mẩu** nội dung của đợt đọc đầu, chữ **Kibana** xuất hiện
**0 lần**. Nhưng tài liệu quy trình nội bộ ghi tra Kibana là **bước 1**.

- [ ] Có, tôi mở Kibana trước — **chỉ là không ghi vào ticket**
- [ ] Có, nhưng chỉ với một số loại lỗi nhất định (ghi rõ loại nào bên dưới)
- [ ] Không, tôi làm việc khác trước (ghi rõ bên dưới)
- [ ] Kibana gần như không dùng nữa
- [ ] Không chắc

**Nếu không phải Kibana thì bước đầu tiên thật sự là gì?**

>
>

---

## Câu 2 — Việc chẩn đoán xảy ra Ở ĐÂU?

**Chúng tôi đo được:** **62/150 case (41%)** không có một dòng nào ghi nguyên nhân. Đọc kỹ
thì thấy nhiều case chuyển sang **phiên remote desktop**, hoặc ghi thẳng *"như đã trao đổi
qua điện thoại"*.

**Ước chừng, trong 10 ca bạn xử lý thì bao nhiêu ca phần chẩn đoán chính xảy ra NGOÀI ticket?**

- [ ] 0–2 ca
- [ ] 3–5 ca
- [ ] 6–8 ca
- [ ] 9–10 ca
- [ ] Không chắc

**Ngoài ticket thì chủ yếu ở đâu?** (tích nhiều ô được)

- [ ] Remote vào máy khách (Ultraviewer hoặc tương tự)
- [ ] Gọi điện
- [ ] Zalo / chat riêng
- [ ] Hỏi đồng nghiệp trực tiếp
- [ ] Khác:

>

---

## Câu 3 — Sau khi xong, bạn có ghi lại CÁCH làm không?

**Chúng tôi đo được:** cả **7/7** case đã đóng trong đợt đọc đầu đều kết thúc bằng một câu
xác nhận *hết triệu chứng*, không câu nào ghi *đã làm gì*. Ví dụ nguyên văn: *"Done nhé"*
(23 ký tự), *"Khách tạo được thẻ thành công rồi"* (33 ký tự). Một case đóng bằng đúng một
tấm ảnh.

- [ ] Có ghi, khá đầy đủ
- [ ] Có ghi, nhưng ngắn — chủ yếu để đánh dấu đã xong
- [ ] Hầu như không ghi, vì khách đã hết lỗi là đóng
- [ ] Không chắc

**Nếu không ghi thì lý do chính là gì?** (tích nhiều ô được)

- [ ] Không có thời gian, ca sau đang chờ
- [ ] Ghi cũng không ai đọc lại
- [ ] Đã trao đổi miệng với khách rồi, ghi lại thấy thừa
- [ ] Không biết ghi vào đâu cho đúng chỗ
- [ ] Khác:

>

---

## Câu 4 — Cùng một triệu chứng, bao nhiêu nguyên nhân?

**Chúng tôi đo được:** chủ đề dày nhất (hoá đơn điện tử) có 7 case → **7 nguyên nhân khác
nhau, không cái nào lặp lại**. Và **4 nguyên nhân** của nhóm dày nhất có **triệu chứng khách
kể giống hệt nhau**, lại **cùng kết thúc ở một trang quản trị**.

**Với loại sự cố bạn gặp nhiều nhất, khách kể giống nhau thì thường có mấy nguyên nhân?**

- [ ] Hầu như chỉ một — nghe là biết
- [ ] 2–3 nguyên nhân, phân biệt được bằng một bước kiểm
- [ ] 4–6 nguyên nhân, phải kiểm vài bước mới ra
- [ ] Rất nhiều, gần như ca nào cũng khác
- [ ] Không chắc

**Thứ giúp bạn phân biệt chúng là gì?**

- [ ] Một bước kiểm phải THỰC HIỆN (mở màn hình nào đó ra xem)
- [ ] Hỏi thêm khách một câu
- [ ] Nhìn ảnh khách gửi
- [ ] Kinh nghiệm — thường đoán đúng ngay
- [ ] Khác:

>

---

## Câu 5 — Phần có giá trị nhất nằm ở đâu trong ticket?

**Chúng tôi đo được:** với một nhóm sự cố, **8/10** case có ghi bước kiểm — nhưng phần dày
lên **nằm trọn trong các trao đổi NỘI BỘ** giữa triển khai và kỹ thuật, không nằm ở phần
trả lời khách. Một bộ lọc *"chỉ lấy phần khách đọc được"* sẽ vứt mất **5/10** bước kiểm.

**Theo bạn, thông tin kỹ thuật thật sự thường nằm ở đâu?**

- [ ] Trong phần trao đổi nội bộ giữa các bộ phận
- [ ] Trong phần trả lời khách
- [ ] Chia đều cả hai
- [ ] Không nằm ở đâu cả — nó ở trong đầu người xử lý
- [ ] Không chắc

>

---

## Câu 6 — Bạn có copy lại câu trả lời cũ không?

**Chúng tôi đo được:** trên 150 case có **115 cặp** chia nhau một khối văn bản lặp lại
**nguyên văn** từ 300 ký tự trở lên. Nhưng chỉ **đúng 1 khối** trong số đó chứa các bước
làm thật — còn lại là câu trả lời mẫu, nhắc việc tự động, và form nội bộ.

Điều thú vị: chính khối đó có B1/B2 kèm nhánh điều kiện, và **cùng một người gõ lại nguyên
văn ở hai case cách nhau 21 ngày**.

- [ ] Có, tôi giữ sẵn một bộ câu trả lời/hướng dẫn riêng
- [ ] Có, nhưng tìm lại trong ticket cũ chứ không lưu riêng
- [ ] Thỉnh thoảng
- [ ] Không, tôi gõ lại mỗi lần
- [ ] Không chắc

**Nếu có giữ riêng thì giữ ở đâu?** (file Word, ghi chú Zalo, sổ tay, …)

>

---

## Câu 7 — Câu quan trọng nhất: chúng tôi đang THIẾU gì?

Sáu câu trên đều xuất phát từ thứ **có trong ticket**. Câu này hỏi về thứ **không bao giờ
vào ticket**.

**Bước kiểm nào bạn vẫn làm mà gần như không bao giờ ghi lại?**

> 
>
>

**Có loại sự cố nào mà một bản hướng dẫn từng bước sẽ KHÔNG giúp được gì không?**
(Chúng tôi cần biết chỗ không nên tự động hoá, không kém gì chỗ nên.)

> 
>

**Nếu có một trợ lý đọc được toàn bộ ticket cũ, bạn muốn nó làm gì nhất?**

> 
>

---

## Cảm ơn

Nếu có chỗ nào trong bảng này hỏi sai vấn đề, xin cứ nói thẳng — **một câu hỏi sai được chỉ
ra thì đáng giá hơn một câu trả lời lịch sự**. Mấy con số ở trên đều là đo từ ticket, và
ticket chỉ cho thấy phần được ghi lại.

**Điền xong gửi lại cho:** anh.phamviet
