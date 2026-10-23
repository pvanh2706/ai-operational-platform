<script setup lang="ts">
// Đọc trọn MỘT kết quả /hoi của một ticket (hàng `DongCham` của giả lập): lỗi vận hành ≠ không biết ≠ biết trả lời; bước
// hiện tại; hướng dẫn đầy đủ; định tuyến kèm NGUỒN. JSON thô chỉ là phần mở rộng để chẩn đoán.
//
// ⚠ Điểm "đúng/sai nhóm" là chấm ĐỊNH TUYẾN trên đáp án BỊA — nó không nói gì về chất lượng hướng dẫn.
import type { DongCham } from '../apiGiaLap'
import { LY_DO_KHONG_BIET, LY_DO_KHONG_CO_QUY_TRINH } from '../doc'
import HuongDanDoc from './HuongDanDoc.vue'
import NhanNguon from './NhanNguon.vue'

defineProps<{ cong: number; d: DongCham }>()
</script>

<template>
  <div class="kq">
    <div v-if="d.loiVanHanh" class="bao loi">
      <b>Lỗi vận hành</b> — POST /hoi trả HTTP {{ d.http }}. Đây là sự cố của hệ thống, KHÔNG phải câu trả lời “không biết”.
      <pre>{{ d.loiVanHanh }}</pre>
    </div>
    <template v-else>
      <div class="hang">
        <span class="nhan-dt">Định tuyến</span>
        nhóm <b>{{ d.nhom }}</b> <NhanNguon :nguon="d.nguon" />
        <span v-if="d.giaiThich" class="mo"> — {{ d.giaiThich }}</span>
      </div>
      <div class="mo">Đáp án giả: <b>{{ d.nhomDung }}</b> → chấm định tuyến
        <b>{{ d.ketQua === 'dung' ? 'đúng' : d.ketQua === 'sai' ? 'sai nhóm' : 'không biết' }}</b>. Chấm này chỉ nói về bước định tuyến,
        không phải về chất lượng hướng dẫn.</div>

      <div v-if="d.bietTraLoi === false" class="bao chu-y">
        <b>KHÔNG BIẾT</b> — một câu trả lời hợp lệ, không phải lỗi.
        {{ LY_DO_KHONG_BIET[d.lyDoKhongBiet ?? ''] ?? d.lyDoKhongBiet }}
      </div>

      <div class="hang">
        <span class="nhan-dt">Quy trình</span>
        <template v-if="d.quyTrinh">
          <b>{{ d.quyTrinh.ten }}</b> v{{ d.quyTrinh.phienBan }} ·
          <template v-if="d.buocHienTai">bước hiện tại <b>{{ d.buocHienTai }}</b> — {{ d.noiDungBuoc }}
            <span v-if="d.noiXemBuoc" class="mo"> · xem ở: {{ d.noiXemBuoc }}</span></template>
          <template v-else-if="d.daXongHet">đã xong hết các bước</template>
          <div class="mo">{{ d.quyTrinh.traTheo === 'buoc' ? 'Hướng dẫn tra theo CHỦ ĐỀ của bước hiện tại.' : 'Hướng dẫn tra theo mức NHÓM (bước không có tri thức riêng, hoặc đã xong hết).' }}</div>
        </template>
        <span v-else class="mo">{{ LY_DO_KHONG_CO_QUY_TRINH[d.lyDoKhongCoQuyTrinh ?? ''] ?? d.lyDoKhongCoQuyTrinh ?? 'không khớp quy trình nào' }}</span>
      </div>

      <template v-if="d.bietTraLoi">
        <div class="nhan-dt">Hướng dẫn ({{ d.huongDan?.length ?? d.soHuongDan }})</div>
        <HuongDanDoc v-if="d.huongDan" :cong="cong" :huong-dan="d.huongDan" />
        <p v-else class="bao chu-y">Kết quả này không mang nội dung hướng dẫn (chỉ có số lượng {{ d.soHuongDan }}) — không có bản chụp để đọc lại.</p>
      </template>
    </template>
    <details class="tho"><summary>JSON thô (chẩn đoán)</summary><pre>{{ JSON.stringify(d, null, 2) }}</pre></details>
  </div>
</template>

<style scoped>
.kq { font-size: 13px; padding: 4px 0; }
.hang { margin: 6px 0 2px; }
.nhan-dt { font-size: 11px; font-weight: 700; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; margin-right: 6px; }
pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; word-break: break-word; max-height: 360px; overflow: auto; }
.bao pre { background: transparent; color: inherit; padding: 0; }
.tho { margin-top: 8px; }
</style>
