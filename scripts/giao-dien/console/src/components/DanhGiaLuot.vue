<script setup lang="ts">
// Đánh giá MỘT lượt theo ba chiều ĐỘC LẬP + nhận xét + kết quả mong đợi. Mặc định "chưa đánh giá" — không suy từ HTTP 200.
// 🛑 Lưu ở đây KHÔNG gọi host: không đổi mức kiểm chứng, trạng thái duyệt hay nấc USED. Đánh giá ≠ duyệt ≠ đã dùng.
import { computed, ref, watch } from 'vue'
import { CHIEU, daDanhGia, type DanhGia, type Luot } from '../phien'
import { gio } from '../gio'

const props = defineProps<{ luot: Luot }>()
const emit = defineEmits<{ luu: [danhGia: DanhGia] }>()

const nhap = ref<DanhGia>({ ...props.luot.danhGia })
watch(() => props.luot, (l) => { nhap.value = { ...l.danhGia } })

const doi = computed(() => JSON.stringify({ ...nhap.value, luc: null }) !== JSON.stringify({ ...props.luot.danhGia, luc: null }))

function luu() {
  emit('luu', { ...nhap.value, luc: new Date().toISOString() })
}
</script>

<template>
  <div class="danh-gia">
    <div class="tieu">Đánh giá của bạn <span class="mo">— chỉ lưu vào phiên, không duyệt, không khai “đã dùng”</span></div>
    <div class="hang">
      <label v-for="c in CHIEU" :key="c.k">{{ c.ten }}
        <select v-model="nhap[c.k]">
          <option v-for="(ten, gia) in c.nhan" :key="gia" :value="gia">{{ ten }}</option>
        </select>
      </label>
    </div>
    <div class="hang">
      <label class="rong">Kết quả / hướng dẫn mong đợi <textarea v-model="nhap.mongDoi" rows="2" /></label>
      <label class="rong">Nhận xét <textarea v-model="nhap.nhanXet" rows="2" /></label>
    </div>
    <button class="nho" :disabled="!doi" @click="luu">Lưu đánh giá</button>
    <span v-if="doi" class="mo"> chưa lưu</span>
    <span v-else-if="daDanhGia(luot.danhGia)" class="mo"> đã lưu {{ gio(luot.danhGia.luc) }}</span>
    <span v-else class="mo"> chưa đánh giá</span>
  </div>
</template>

<style scoped>
.danh-gia { border-top: 1px dashed var(--vien); margin-top: 8px; padding-top: 8px; font-size: 13px; }
.tieu { font-size: 12px; font-weight: 700; }
.hang { display: flex; flex-wrap: wrap; gap: 10px; margin: 6px 0; }
label { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 3px; min-width: 0; }
label.rong { flex: 1 1 260px; }
select, textarea { font: inherit; font-weight: 400; padding: 4px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
button.nho { padding: 3px 10px; font-size: 12px; }
</style>
