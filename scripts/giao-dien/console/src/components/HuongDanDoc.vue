<script setup lang="ts">
// Đọc ĐỦ `huongDan[]` mà /hoi trả — theo đúng cấu trúc payload: mỗi bản ghi tri thức ĐÃ DUYỆT, ba loại phát biểu, mỗi
// phát biểu kèm mức kiểm chứng và dẫn chứng. Thứ người làm support thật sẽ đọc, không phải con số đếm.
import type { HuongDan } from '../kieu'
import PhatBieuDoc from './PhatBieuDoc.vue'

defineProps<{ cong: number; huongDan: HuongDan[] }>()

const PHAN = [
  { k: 'dieuKienApDung', ten: 'Điều kiện áp dụng' },
  { k: 'cachNhanRa', ten: 'Cách nhận ra' },
  { k: 'cachXuLy', ten: 'Cách xử lý' },
] as const
</script>

<template>
  <p v-if="!huongDan.length" class="mo">/hoi không trả hướng dẫn nào.</p>
  <div v-for="h in huongDan" :key="h.id" class="hd">
    <div class="dau">
      <b>{{ h.tenNguyenNhan }}</b>
      <span class="tt" :class="{ canh: h.trangThai !== 'ACTIVE' }">{{ h.trangThai }}</span>
      <span class="mo">phạm vi xem {{ h.phamViXem ?? '—' }}</span>
      <RouterLink class="mo" :to="{ name: 'ban-nhap', query: { cong: String(cong), tri: h.id } }">mở bản ghi · lịch sử sửa →</RouterLink>
    </div>
    <p v-if="h.trangThai === 'NEEDS_REVIEW'" class="bao chu-y">
      NEEDS_REVIEW — nội dung hoặc dẫn chứng đã đổi kể từ lần duyệt cuối (V3). Vẫn được trả, nhưng cần người duyệt lại.
    </p>
    <template v-for="p in PHAN" :key="p.k">
      <div v-if="h[p.k].length" class="phan">
        <div class="tieu">{{ p.ten }}</div>
        <PhatBieuDoc v-for="(pb, i) in h[p.k]" :key="i" :cong="cong" :noi-dung="pb.noiDung" :muc-kiem-chung="pb.mucKiemChung"
                     :nguon-goc="pb.nguonGoc" :dan-chung="pb.danChung ?? []" />
      </div>
    </template>
  </div>
</template>

<style scoped>
.hd { border: 1px solid var(--vien); border-radius: 8px; padding: 8px 12px; margin: 8px 0; background: #fff; }
.dau { display: flex; flex-wrap: wrap; gap: 8px; align-items: baseline; }
.tt { font-size: 11px; font-weight: 700; color: var(--xanh); }
.tt.canh { color: var(--vang); }
.phan { margin-top: 8px; }
.tieu { font-size: 11px; font-weight: 700; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
</style>
