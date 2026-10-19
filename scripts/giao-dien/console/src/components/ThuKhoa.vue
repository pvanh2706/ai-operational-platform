<script setup lang="ts">
// Nút "Thử khoá" (IM-57): hỏi Anthropic bằng ĐÚNG khoá host đang dùng, qua đường MIỄN PHÍ — trước khi tiêu lượt gọi.
// ⚠ Chỉ chạy khi người BẤM. Không tự chạy lúc mở trang: nó là một lượt đi ra mạng, và trang này mở rất thường.
import { ref } from 'vue'
import { api } from '../api'
import type { KetQuaThuKhoa } from '../kieu'

const props = defineProps<{ cong: number }>()
const emit = defineEmits<{ ketQua: [kq: KetQuaThuKhoa] }>()

const kq = ref<KetQuaThuKhoa | null>(null)
const loi = ref<string | null>(null)
const dangThu = ref(false)

async function thu() {
  dangThu.value = true
  loi.value = null
  try {
    kq.value = await api.thuKhoa(props.cong)
    emit('ketQua', kq.value)
  } catch (e) {
    kq.value = null
    loi.value = e instanceof Error ? e.message : String(e)
  } finally {
    dangThu.value = false
  }
}

const TIEU_DE: Record<string, string> = {
  'khong-co-khoa': 'Host không có khoá',
  'khoa-bi-tu-choi': 'Anthropic TỪ CHỐI khoá',
  'model-khong-ton-tai': 'Anthropic không có model này',
  'loi-khac': 'Không thử được — không phải lỗi của khoá',
}
</script>

<template>
  <div class="thu-khoa">
    <button class="phu" :disabled="dangThu" @click="thu">{{ dangThu ? 'Đang thử…' : 'Thử khoá (miễn phí)' }}</button>
    <span class="mo"> gọi Anthropic xem model <code>{{ kq?.model ?? 'claude-opus-5' }}</code> bằng khoá của app :{{ cong }} — không tính tiền</span>
    <div v-if="loi" class="bao loi">{{ loi }}</div>
    <div v-else-if="kq" class="bao" :class="kq.hopLe === true ? 'ok' : kq.hopLe === false ? 'loi' : 'chu-y'">
      <b>{{ kq.hopLe === true ? `✓ Khoá dùng được · ${kq.tenModelHienThi}` : TIEU_DE[kq.lyDo ?? ''] ?? kq.lyDo }}</b>
      <pre>{{ kq.giaiThich }}</pre>
    </div>
  </div>
</template>

<style scoped>
.thu-khoa { margin-top: 10px; font-size: 13px; }
pre { white-space: pre-wrap; word-break: break-word; font: 12px ui-monospace, Consolas, monospace; margin: 6px 0 0; }
</style>
