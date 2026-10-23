<script setup lang="ts">
// Câu trả lời của /internal/hoi-dap (hỏi tự do, Q3-1) — dùng chung cho Nạp & hỏi và Lịch sử phiên, để hai nơi đọc như nhau.
// Mẩu nguồn hiện ra là mẩu NẰM TRONG kết quả (bản đã gửi model, đã qua cổng che) — không đọc lại từ app.
import { computed, ref, watch } from 'vue'
import { LY_DO_HOI_DAP } from '../doc'
import type { KetQuaHoiDap } from '../kieu'

const props = defineProps<{ kq: KetQuaHoiDap }>()
const moMa = ref<string | null>(null)
watch(() => props.kq, () => { moMa.value = null })
const noiDungMa = computed(() => props.kq.nguon.find((n) => n.ma === moMa.value) ?? null)
</script>

<template>
  <div class="tra-loi">
    <div class="nhan-cd">CHƯA DUYỆT · {{ kq.cheDo === 'api' ? `${kq.nhaCungCap} · ${kq.model}` : 'qua file' }}
      · {{ kq.nguon.length }} mẩu đã gửi{{ kq.soChoDaChe ? ` · đã che ${kq.soChoDaChe} chỗ` : '' }}</div>
    <div v-if="kq.nguonBiCat" class="bao chu-y">⚠ Nguồn dài quá trần — chỉ phần đầu được gửi cho model; câu trả lời có thể thiếu.</div>
    <div v-if="!kq.bietTraLoi" class="bao chu-y"><b>KHÔNG BIẾT</b> — {{ LY_DO_HOI_DAP[kq.lyDoKhongBiet ?? ''] ?? kq.lyDoKhongBiet }}
      <span class="mo"> Nếu nguồn thật sự không có câu trả lời, từ chối là ĐÚNG — đánh giá nó như vậy.</span></div>
    <ol v-else class="phat-bieu">
      <li v-for="(p, i) in kq.phatBieu" :key="i">
        {{ p.noiDung }}
        <button v-for="m in p.nguon" :key="m" class="ma" :class="{ mo: moMa === m }" @click="moMa = moMa === m ? null : m">{{ m }}</button>
      </li>
    </ol>
    <div v-if="moMa && !noiDungMa" class="bao chu-y">Mẩu {{ moMa }} không có trong bản kết quả này.</div>
    <div v-if="noiDungMa" class="mau"><b>[{{ noiDungMa.ma }}] {{ noiDungMa.nhan }}</b><pre>{{ noiDungMa.noiDung }}</pre></div>
    <div v-if="kq.soPhatBieuBiBo" class="mo">Đã BỎ {{ kq.soPhatBieuBiBo }} phát biểu không dẫn được mẩu có thật.</div>
    <div v-if="kq.ghiChu" class="mo">Ghi chú của AI: {{ kq.ghiChu }}</div>
    <div v-if="kq.giaiThich" class="mo">{{ kq.giaiThich }}</div>
  </div>
</template>

<style scoped>
.tra-loi { margin-top: 14px; border-top: 1px solid var(--vien); padding-top: 10px; }
.nhan-cd { display: inline-block; font-size: 11px; font-weight: 700; color: var(--vang); background: var(--vang-nhat); border: 1px solid #fedf89; border-radius: 4px; padding: 2px 8px; }
.phat-bieu { margin: 10px 0; padding-left: 22px; }
.phat-bieu li { margin-bottom: 6px; }
button.ma { margin: 0 0 0 4px; padding: 0 6px; font: 600 11px ui-monospace, Consolas, monospace; background: var(--lam-nhat); color: var(--lam); border: 1px solid #c7d7fe; border-radius: 4px; }
button.ma.mo { background: var(--lam); color: #fff; }
.mau { margin: 6px 0 10px; font-size: 13px; }
pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; word-break: break-word; }
</style>
