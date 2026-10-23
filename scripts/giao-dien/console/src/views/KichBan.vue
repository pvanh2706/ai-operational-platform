<script setup lang="ts">
// Trải nghiệm THEO KỊCH BẢN — điểm bắt đầu cho người tự thử mà không có ai giải thích bên cạnh.
//
// ⚠ Mọi kịch bản chạy trên DỮ LIỆU BỊA của app giả lập :5120 (kp_gialap). Đáp án bịa kiểm LUỒNG xử lý — nó chưa chứng minh
//   độ chính xác trên dữ liệu khách thật. Kết quả mong đợi ở đây là để người thử ĐỐI CHIẾU và tự đánh giá; console không tự
//   điền điểm đánh giá từ HTTP 200.
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { CONG_GIA_LAP } from '../apiGiaLap'
import { useKetNoi } from '../stores/ketNoi'
import { usePhien } from '../stores/phien'

const router = useRouter()
const ketNoi = useKetNoi()
const phien = usePhien()

interface KichBan {
  id: string
  ten: string
  mucTieu: string
  duLieu: string
  dieuKien: string
  thaoTac: string[]
  mongDoi: string[]
  gioiHan: string
  batDau: 'gia-lap' | 'nap-hoi'
}

const DS: KichBan[] = [
  {
    id: 'kb1-chua-co-tri-thuc',
    ten: 'Chưa có tri thức được duyệt',
    mucTieu: 'Thấy hệ thống trả KHÔNG BIẾT đúng lý do khi định tuyến được nhóm nhưng nhóm chưa có tri thức nào được người duyệt.',
    duLieu: 'Lô ticket bịa của Giả lập (gia_lap_du_lieu.py) trên app :5120.',
    dieuKien: 'Nhóm sẽ hỏi chưa từng được duyệt trong kp_gialap. Không chắc thì dọn database giả lập theo docs/21 §9 trước.',
    thaoTac: [
      'Giả lập · bước 1: Sinh lô mới.',
      'Bước 2: Định tuyến (chế độ File không tốn tiền: xuất → cho Claude Code/Codex làm → nạp).',
      'Bước 3: chọn một nhóm trong ô “Nhóm” nhưng KHÔNG soạn nháp.',
      'Bước 5: Hỏi lại các ticket của nhóm, mở “n hướng dẫn ▸” của một ticket.',
    ],
    mongDoi: [
      'Bước 2: ticket được chấm định tuyến (đúng/sai so với đáp án giả) — có nhãn nguồn: người nạp / bảng tra / model.',
      'Bước 5: “không biết” với lý do “nhóm này chưa có tri thức nào được NGƯỜI duyệt”, 0 hướng dẫn. Đây là kết quả hợp lệ, không phải lỗi.',
    ],
    gioiHan: 'Định tuyến đúng không có nghĩa hướng dẫn tốt — hai thứ được trình bày và đánh giá riêng.',
    batDau: 'gia-lap',
  },
  {
    id: 'kb2-soan-sua-duyet',
    ten: 'Soạn nháp → xem / sửa → duyệt → hỏi lại',
    mucTieu: 'Chỉ ra ĐIỂM nội dung bắt đầu được phép dùng: chỉ sau khi người duyệt, và đúng bản đã sửa.',
    duLieu: 'Nhóm đã định tuyến ở kịch bản 1 (hoặc bất kỳ nhóm nào có case).',
    dieuKien: 'Đã qua bước 1–2 của Giả lập.',
    thaoTac: [
      'Bước 3: Soạn nháp (File hoặc API) → bấm “Đọc · sửa · duyệt bản nháp”.',
      'Ở màn Bản nháp: đọc từng phát biểu, bấm vài dẫn chứng để mở mẩu nguồn.',
      'Sửa MỘT phát biểu → Lưu chỉnh sửa. Tải lại trang (F5): mở “Lịch sử” của phát biểu đó.',
      'Trước khi duyệt: quay lại Giả lập bước 5, Hỏi lại → vẫn “không biết”.',
      'Màn Bản nháp: khai người duyệt, chọn phạm vi xem, tích hai ô xác nhận → Duyệt các đối tượng đang chờ.',
      'Giả lập bước 5: Hỏi lại → mở hướng dẫn.',
    ],
    mongDoi: [
      'Lưu sửa KHÔNG đổi trạng thái thành “đã duyệt”; lịch sử còn cả bản gốc lẫn bản mới (nguồn gốc “người đánh giá”).',
      'Thiếu người/phạm vi/xác nhận thì nút duyệt bị khoá và giao diện liệt kê thiếu gì.',
      'Bảng kết quả duyệt báo từng đối tượng; trạng thái thẻ đọc lại từ app: ACTIVE.',
      'Sau duyệt: “biết”, hướng dẫn hiện ĐÚNG câu đã sửa — đó là điểm nội dung bắt đầu được dùng.',
    ],
    gioiHan: 'Bản nháp do AI soạn trên ticket bịa; đánh giá SOP ở đây là về cách trình bày + luồng duyệt, chưa phải chất lượng SOP thật.',
    batDau: 'gia-lap',
  },
  {
    id: 'kb3-xac-nhan-buoc',
    ten: 'Xác nhận bước',
    mucTieu: 'Bước hiện tại chuyển đúng theo quy trình khi có lời xác nhận — và KHÔNG nhảy nếu chỉ mở xem.',
    duLieu: 'Nhóm đã duyệt cả tri thức lẫn quy trình (kịch bản 2).',
    dieuKien: 'Bước 5 của Giả lập đã hiện “bước hiện tại” cho các ticket.',
    thaoTac: [
      'Bước 5: mở rồi đóng phần hướng dẫn của một ticket vài lần, mở vài dẫn chứng.',
      'Hỏi lại các ticket của nhóm → ghi lại bước hiện tại của ticket đó.',
      'Bấm “Xác nhận <bước> xong” cho ticket đó.',
      'Hỏi lại lần nữa.',
    ],
    mongDoi: [
      'Mở xem không đổi bước hiện tại (hỏi lại vẫn ra đúng bước cũ).',
      'Sau xác nhận: thông báo “bước hiện tại giờ là <bước kế> · <nội dung>”; hỏi lại vẫn giữ bước kế — tiến độ suy từ evidence.',
    ],
    gioiHan: 'Lời xác nhận là LỜI KHAI của người thao tác; hệ thống không kiểm được việc đã thật sự làm bước đó.',
    batDau: 'gia-lap',
  },
  {
    id: 'kb4-hoi-tu-do',
    ten: 'Hỏi tự do có dẫn chứng',
    mucTieu: 'Đối chiếu từng phát biểu với đoạn nguồn nó dẫn; kết quả luôn mang nhãn CHƯA DUYỆT và không vào kho.',
    duLieu: 'Một case giả lập trên app :5120 (chọn “App giả lập” ở góc phải).',
    dieuKien: 'Đã có lô giả lập (Giả lập bước 1).',
    thaoTac: [
      'Nạp & hỏi: chọn một case gialap:… ở cột “Case”.',
      'Hỏi một câu MÀ nguồn có trả lời, vd. “Khách báo lỗi gì?” (File: xuất prompt → AI trả JSON → nạp).',
      'Bấm từng mã mẩu [N1]… cạnh phát biểu.',
      'Đổi sang case khác, hỏi câu khác. Chuyển trang rồi tải lại (F5), mở lại hai lượt ở “Lượt hỏi trong phiên”.',
    ],
    mongDoi: [
      'Nhãn CHƯA DUYỆT; mỗi phát biểu có ít nhất một mã mẩu; mã mở ra đúng đoạn nguồn đã gửi.',
      'Hai lượt vẫn mở lại đúng câu hỏi, câu trả lời, mẩu nguồn (bản chụp) và đánh giá tương ứng.',
    ],
    gioiHan: 'Câu trả lời đọc thẳng vật liệu chưa duyệt — đánh giá ở đây không biến nó thành tri thức.',
    batDau: 'nap-hoi',
  },
  {
    id: 'kb5-nguon-thieu',
    ten: 'Nguồn thiếu câu trả lời',
    mucTieu: 'Từ chối trả lời khi nguồn không có thông tin — và đánh giá việc từ chối đó là ĐÚNG.',
    duLieu: 'Case giả lập như kịch bản 4.',
    dieuKien: 'Như kịch bản 4.',
    thaoTac: [
      'Hỏi một câu nguồn KHÔNG có, vd. “Hợp đồng của khách sạn này hết hạn ngày nào?”.',
      'Đánh giá: Độ đúng = “Đúng” nếu hệ thống từ chối; Dẫn chứng = “Không áp dụng”.',
    ],
    mongDoi: [
      'KHÔNG BIẾT với lý do “Nguồn không trả lời được câu hỏi này” (nguon-khong-noi), không phát biểu nào.',
      'Tổng kết đếm lượt này vào “không biết” (thành công vận hành), không vào “lỗi vận hành” — và không tự tính là sai.',
    ],
    gioiHan: 'Ở chế độ File, câu trả lời phụ thuộc AI bạn dán prompt cho; nếu nó bịa, phát biểu không dẫn được mẩu sẽ bị host bỏ.',
    batDau: 'nap-hoi',
  },
  {
    id: 'kb6-loi-van-hanh',
    ten: 'Lỗi vận hành',
    mucTieu: 'Phân biệt sự cố hệ thống (app chết, khoá sai, đầu vào hỏng) với “không biết” về nghiệp vụ — không tốn tiền.',
    duLieu: 'Bất kỳ case giả lập nào.',
    dieuKien: 'Không cần khoá API.',
    thaoTac: [
      'Cách A — đầu vào hỏng: Nạp & hỏi ở chế độ File, xuất prompt, rồi dán một JSON hỏng (vd. “{ abc”) và bấm Nạp kết quả.',
      'Cách B — app không sẵn sàng: tắt cửa sổ “KP app giả lập :5120” (Ctrl+C), rồi Hỏi lại ở Giả lập bước 5 hoặc nạp kết quả ở Nạp & hỏi. Bật lại bằng chay-thu.bat.',
      'Mở màn Phiên → Tổng kết.',
    ],
    mongDoi: [
      'Lượt mang kết cục “lỗi vận hành” kèm HTTP (400 với đầu vào hỏng, 502 khi app không trả lời) — không phải “không biết”.',
      'Tổng kết tách lỗi vận hành khỏi “chạy được” và khỏi “không biết”.',
    ],
    gioiHan: 'Không thử khoá thật bị từ chối ở đây (cần khoá + có thể tính tiền) — nút “Thử khoá” ở Cấu hình AI làm việc đó miễn phí.',
    batDau: 'nap-hoi',
  },
]

const nguoi = ref('')
const loi = ref<string | null>(null)
const dang = ref<string | null>(null)

async function batDau(k: KichBan, i: number) {
  dang.value = k.id
  loi.value = null
  try {
    ketNoi.cong = CONG_GIA_LAP
    await phien.taoMoi(CONG_GIA_LAP, `Kịch bản ${i + 1} — ${k.ten}`, nguoi.value, `${i + 1}. ${k.ten}`)
    await router.push({ name: k.batDau })
  } catch (e) {
    loi.value = e instanceof Error ? e.message : String(e)
  } finally {
    dang.value = null
  }
}
</script>

<template>
  <section class="the">
    <h2>Trải nghiệm theo kịch bản</h2>
    <p class="mo">
      Mỗi kịch bản nói rõ mục tiêu, dữ liệu, điều kiện đầu, thao tác, kết quả mong đợi và giới hạn. “Bắt đầu” mở một PHIÊN mới gắn
      tên kịch bản trên app giả lập :{{ CONG_GIA_LAP }}, rồi đưa bạn tới màn đầu tiên. Mọi lượt sau đó vào phiên này — đánh giá ngay
      dưới từng kết quả, rồi xem <RouterLink :to="{ name: 'phien', query: { cong: String(CONG_GIA_LAP) } }">Tổng kết</RouterLink> và xuất JSON/Markdown.
    </p>
    <p class="bao chu-y">Dữ liệu và đáp án ở đây là BỊA — chúng kiểm luồng xử lý, chưa chứng minh độ chính xác trên dữ liệu khách thật.
      Console không tự điền điểm đánh giá từ một HTTP 200: kết quả đúng hay sai là do BẠN đánh giá.</p>
    <label class="o">Người thao tác (lời khai) <input v-model="nguoi" placeholder="tên của bạn" /></label>
    <div v-if="loi" class="bao loi">{{ loi }}</div>
  </section>

  <section v-for="(k, i) in DS" :key="k.id" class="the">
    <h2>{{ i + 1 }} · {{ k.ten }}</h2>
    <dl>
      <dt>Mục tiêu</dt><dd>{{ k.mucTieu }}</dd>
      <dt>Dữ liệu</dt><dd>{{ k.duLieu }}</dd>
      <dt>Điều kiện đầu</dt><dd>{{ k.dieuKien }}</dd>
      <dt>Thao tác</dt><dd><ol><li v-for="t in k.thaoTac" :key="t">{{ t }}</li></ol></dd>
      <dt>Kết quả mong đợi</dt><dd><ul><li v-for="t in k.mongDoi" :key="t">{{ t }}</li></ul></dd>
      <dt>Giới hạn</dt><dd class="mo">{{ k.gioiHan }}</dd>
    </dl>
    <button :disabled="!!dang" @click="batDau(k, i)">{{ dang === k.id ? 'Đang mở phiên…' : 'Bắt đầu phiên theo kịch bản này' }}</button>
  </section>
</template>

<style scoped>
.o { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; }
input { font: inherit; font-weight: 400; padding: 5px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
dl { display: grid; grid-template-columns: 140px 1fr; gap: 4px 12px; font-size: 14px; margin: 0 0 10px; }
dt { font-weight: 700; color: var(--mo); font-size: 12px; text-transform: uppercase; padding-top: 2px; }
dd { margin: 0; }
dd ol, dd ul { margin: 0; padding-left: 20px; }
@media (max-width: 640px) { dl { grid-template-columns: 1fr; } dt { padding-top: 6px; } }
</style>
