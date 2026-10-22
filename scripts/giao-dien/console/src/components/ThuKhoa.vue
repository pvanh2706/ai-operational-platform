<script setup lang="ts">
// Nút "Thử khoá" (IM-57; OpenAI từ Q1-1): hỏi nhà cung cấp bằng ĐÚNG khoá host đang dùng, qua đường MIỄN PHÍ — trước
// khi tiêu lượt gọi.
// ⚠ Chỉ chạy khi người BẤM. Không tự chạy lúc mở trang: nó là một lượt đi ra mạng, và trang này mở rất thường.
import { computed, ref } from 'vue'
import { api } from '../api'
import type { KetQuaThuKhoa, NhaCungCap } from '../kieu'

const props = withDefaults(defineProps<{ cong: number; nhaCungCap?: NhaCungCap; model?: string | null }>(), {
  nhaCungCap: 'anthropic',
  model: null,
})
const emit = defineEmits<{ ketQua: [kq: KetQuaThuKhoa] }>()

const kq = ref<KetQuaThuKhoa | null>(null)
const loi = ref<string | null>(null)
const dangThu = ref(false)

const ten = computed(() => (props.nhaCungCap === 'openai' ? 'OpenAI' : 'Anthropic'))

async function thu() {
  dangThu.value = true
  loi.value = null
  try {
    kq.value = await api.thuKhoa(props.cong, props.nhaCungCap)
    emit('ketQua', kq.value)
  } catch (e) {
    kq.value = null
    loi.value = e instanceof Error ? e.message : String(e)
  } finally {
    dangThu.value = false
  }
}

const tieuDe = computed<Record<string, string>>(() => ({
  'khong-co-khoa': 'Host không có khoá',
  'thieu-model': 'Có khoá nhưng chưa đặt OpenAI:Model (Q2-1 — không có mặc định)',
  'khoa-bi-tu-choi': `${ten.value} TỪ CHỐI khoá`,
  'model-khong-ton-tai': `${ten.value} không có model này`,
  'het-credit': `Tài khoản ${ten.value} hết tiền`,
  'loi-khac': 'Không thử được — không phải lỗi của khoá',
}))
</script>

<template>
  <div class="thu-khoa">
    <button class="phu" :disabled="dangThu" @click="thu">{{ dangThu ? 'Đang thử…' : `Thử khoá ${ten} (miễn phí)` }}</button>
    <span class="mo"> xem model <code>{{ kq?.model || model || (nhaCungCap === 'openai' ? 'OpenAI:Model' : 'claude-opus-5') }}</code>
      bằng khoá của app :{{ cong }} — không tính tiền</span>
    <div v-if="loi" class="bao loi">{{ loi }}</div>
    <div v-else-if="kq" class="bao" :class="kq.hopLe === true ? 'ok' : kq.hopLe === false ? 'loi' : 'chu-y'">
      <b>{{ kq.hopLe === true ? `✓ Khoá dùng được · ${kq.tenModelHienThi}` : tieuDe[kq.lyDo ?? ''] ?? kq.lyDo }}</b>
      <pre>{{ kq.giaiThich }}</pre>
    </div>
  </div>
</template>

<style scoped>
.thu-khoa { margin-top: 10px; font-size: 13px; }
pre { white-space: pre-wrap; word-break: break-word; font: 12px ui-monospace, Consolas, monospace; margin: 6px 0 0; }
</style>
