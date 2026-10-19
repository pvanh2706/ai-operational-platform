<script setup lang="ts">
// Chế độ FILE của một bước có AI (O4-2): a · xuất prompt ra file → b · Claude/Codex đọc, ghi JSON → c · nạp lại.
// Dùng chung cho định tuyến và soạn nháp — hai bước khác nhau ở NỘI DUNG, không khác ở thao tác.
import { ref } from 'vue'
import type { KetQuaXuat } from '../apiGiaLap'

const props = defineProps<{
  xuat: () => Promise<KetQuaXuat>
  nap: (ketQua: string) => Promise<void>
  goiY: string
}>()

const da = ref<KetQuaXuat | null>(null)
const loi = ref<string | null>(null)
const ketQua = ref('')
const ban = ref<'xuat' | 'nap' | null>(null)
const daChep = ref<string | null>(null)

async function xuat() {
  ban.value = 'xuat'
  loi.value = null
  try {
    const k = await props.xuat()
    if (!k.ok) {
      da.value = null
      loi.value = `${k.loi ?? 'Lệnh xuất thất bại'}${k.baoCao ? `\n\n${k.baoCao}` : ''}`
    } else {
      da.value = k
    }
  } finally {
    ban.value = null
  }
}

async function nap() {
  ban.value = 'nap'
  try {
    await props.nap(ketQua.value)
  } finally {
    ban.value = null
  }
}

async function chep(ten: string, s: string) {
  try {
    await navigator.clipboard.writeText(s)
    daChep.value = ten
    setTimeout(() => (daChep.value = null), 1500)
  } catch {
    daChep.value = 'không chép được — bôi đen chép tay'
  }
}
</script>

<template>
  <div class="xn">
    <button :disabled="!!ban" @click="xuat">{{ ban === 'xuat' ? 'Đang xuất…' : 'a · Xuất prompt ra file' }}</button>
    <pre v-if="loi" class="bao loi">{{ loi }}</pre>

    <div v-if="da" class="da">
      <div>Đã xuất <code>{{ da.duongVao }}</code></div>
      <div class="mo">b · Giao cho AI ghi kết quả vào <code>{{ da.duongRa }}</code> — một trong ba cách:</div>
      <div v-for="[ten, lenh] in ([['Dán vào Claude Code / Codex', da.lenh.cau], ['Claude Code không tương tác', da.lenh.claude], ['Codex', da.lenh.codex]] as const)" :key="ten" class="lenh">
        <div class="ten-lenh">{{ ten }} <button class="phu nho" @click="chep(ten, lenh)">{{ daChep === ten ? 'đã chép' : 'chép' }}</button></div>
        <pre>{{ lenh }}</pre>
      </div>
      <details>
        <summary>Nội dung file prompt ({{ da.noiDung.length.toLocaleString('vi') }} ký tự) — hoặc chép cả khối này vào một khung chat AI</summary>
        <button class="phu nho" @click="chep('prompt', da.noiDung)">{{ daChep === 'prompt' ? 'đã chép' : 'chép toàn bộ' }}</button>
        <pre class="dai">{{ da.noiDung }}</pre>
      </details>
    </div>

    <label class="o-nhap">
      c · Dán JSON AI trả về <span class="mo">— để trống thì đọc file kết quả ở đường dẫn trên; kèm hàng rào ``` cũng được</span>
      <textarea v-model="ketQua" rows="5" :placeholder="goiY" />
    </label>
    <button :disabled="!!ban" @click="nap">{{ ban === 'nap' ? 'Đang nạp…' : 'c · Nạp kết quả' }}</button>
  </div>
</template>

<style scoped>
.xn button { margin: 8px 8px 0 0; }
.da { margin-top: 10px; font-size: 13px; }
.lenh { margin-top: 6px; }
.ten-lenh { font-size: 12px; font-weight: 600; }
pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; word-break: break-word; margin: 4px 0; }
pre.dai { max-height: 300px; overflow: auto; }
pre.bao.loi { background: var(--do-nhat); color: var(--do); }
button.nho { padding: 2px 8px; font-size: 11px; margin: 0 0 0 6px; }
.o-nhap { display: block; font-size: 13px; font-weight: 600; margin-top: 12px; }
.o-nhap textarea { display: block; width: 100%; margin-top: 4px; font: 12.5px ui-monospace, Consolas, monospace; padding: 8px; border: 1px solid var(--vien); border-radius: 6px; }
details { margin-top: 8px; font-size: 12px; }
</style>
