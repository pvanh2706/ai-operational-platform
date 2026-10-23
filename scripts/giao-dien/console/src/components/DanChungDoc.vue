<script setup lang="ts">
// Dẫn chứng của MỘT phát biểu: mỗi mẩu là một nút; bấm thì đọc nội dung mẩu qua GET /cases/{k}/evidence (nguon.ts).
//
// 🛑 Không dựng dẫn chứng thay thế. Thiếu liên kết, case không truy cập được, mẩu đã rút, không thấy mẩu — mỗi ca nói
//   đúng ca của nó. Chỉ MỞ xem: không ghi gì, không gọi model, không đổi nấc gợi ý nào.
import { ref } from 'vue'
import { LoiGoiApp, moTaLoi } from '../api'
import { caseTuKhoaMau, laHoTro, QUAN_HE, type MotDanChung } from '../doc'
import { gio } from '../gio'
import type { MauEvidence, MauEvidenceDaRutLai } from '../kieu'
import { docEvidence } from '../nguon'

const props = defineProps<{ cong: number; ds: MotDanChung[] }>()

type KetQuaMo =
  | { loai: 'dang' }
  | { loai: 'thay'; mau: MauEvidence; khoaCase: string; suyRa: boolean; luc: string }
  | { loai: 'da-rut'; mau: MauEvidenceDaRutLai; khoaCase: string }
  | { loai: 'khong-mo-duoc'; lyDo: string }

const dangMo = ref<number | null>(null)
const ketQua = ref<KetQuaMo | null>(null)

async function mo(i: number) {
  if (dangMo.value === i) {
    dangMo.value = null
    return
  }
  dangMo.value = i
  const d = props.ds[i]!
  const suyRa = !d.khoaNguonCase
  const khoaCase = d.khoaNguonCase ?? caseTuKhoaMau(d.khoaNguonMau)
  if (!khoaCase) {
    ketQua.value = { loai: 'khong-mo-duoc', lyDo: 'Host không cho biết mẩu này thuộc case nào, và khoá mẩu không có dạng <case>#<mẩu> để tìm — không mở được.' }
    return
  }
  ketQua.value = { loai: 'dang' }
  try {
    const ev = await docEvidence(props.cong, khoaCase)
    const khop = <T extends { id: string; khoaNguon: string }>(m: T) => (d.mauId ? m.id === d.mauId : m.khoaNguon === d.khoaNguonMau)
    const thay = ev.mau.find(khop)
    const rut = ev.mauDaRutLai.find(khop)
    if (dangMo.value !== i) return
    if (thay) ketQua.value = { loai: 'thay', mau: thay, khoaCase, suyRa, luc: new Date().toISOString() }
    else if (rut) ketQua.value = { loai: 'da-rut', mau: rut, khoaCase }
    else ketQua.value = { loai: 'khong-mo-duoc', lyDo: `Không thấy mẩu ${d.khoaNguonMau ?? d.mauId} trong evidence của case ${khoaCase}${suyRa ? ' (case suy từ khoá mẩu)' : ''}.` }
  } catch (e) {
    if (dangMo.value !== i) return
    ketQua.value = {
      loai: 'khong-mo-duoc',
      lyDo: e instanceof LoiGoiApp && e.status === 404
        ? `Case ${khoaCase} không truy cập được trong app này (không có, thuộc khách khác, hoặc đang chọn sai app).`
        : moTaLoi(e),
    }
  }
}
</script>

<template>
  <span v-if="!ds.length" class="trong">chưa có dẫn chứng</span>
  <template v-else>
    <button v-for="(d, i) in ds" :key="i" class="chip" :class="{ ho: laHoTro(d), rut: d.daRutLai, mo: dangMo === i }"
            :title="`${QUAN_HE[d.quanHe] ?? d.quanHe}${d.ghiChu ? ` — ${d.ghiChu}` : ''}`" @click="mo(i)">
      {{ (d.khoaNguonMau ?? d.mauId ?? '?').split(/[:#]/).slice(-2).join('#') }}
      <small>{{ QUAN_HE[d.quanHe] ?? d.quanHe }}{{ d.daRutLai ? ' · đã rút' : '' }}</small>
    </button>
    <div v-if="dangMo !== null && ketQua" class="mau">
      <template v-if="ketQua.loai === 'dang'"><span class="mo">Đang đọc evidence…</span></template>
      <template v-else-if="ketQua.loai === 'thay'">
        <div class="mo">
          <code>{{ ketQua.mau.khoaNguon }}</code> · case <code>{{ ketQua.khoaCase }}</code>{{ ketQua.suyRa ? ' (suy từ khoá mẩu, đã khớp đúng id)' : '' }}
          · quan sát {{ gio(ketQua.mau.quanSatLuc ?? ketQua.mau.napLuc) }} · khách sạn {{ ketQua.mau.maKhachSan ?? 'chưa khai' }}
        </div>
        <div v-if="ds[dangMo]?.ghiChu" class="mo">Ghi chú liên kết: {{ ds[dangMo]?.ghiChu }}</div>
        <div class="noi-dung">{{ ketQua.mau.noiDung }}</div>
        <div class="mo nho">Đọc LÚC {{ gio(ketQua.luc) }} qua GET /cases/…/evidence — bản hiện tại trong app, không phải bản chụp lúc hỏi.</div>
      </template>
      <div v-else-if="ketQua.loai === 'da-rut'" class="bao chu-y">
        Mẩu <code>{{ ketQua.mau.khoaNguon }}</code> đã bị <b>{{ ketQua.mau.rutLaiBoi }}</b> rút lại lúc {{ gio(ketQua.mau.rutLaiLuc) }}:
        {{ ketQua.mau.rutLaiLyDo }}. Nội dung không hiện ra nữa (AR-m) — nó không còn chống lưng phát biểu này.
      </div>
      <div v-else class="bao chu-y">{{ ketQua.lyDo }}</div>
    </div>
  </template>
</template>

<style scoped>
.trong { font-size: 11px; color: var(--vang); font-style: italic; }
.chip { margin: 2px 4px 2px 0; padding: 0 6px; font: 600 11px ui-monospace, Consolas, monospace; background: #f0f2f5; color: #475467;
  border: 1px solid var(--vien); border-radius: 4px; }
.chip small { font-weight: 400; font-family: -apple-system, "Segoe UI", sans-serif; }
.chip.ho { background: var(--lam-nhat); color: var(--lam); border-color: #c7d7fe; }
.chip.rut { background: var(--do-nhat); color: var(--do); text-decoration: line-through; }
.chip.mo { outline: 2px solid var(--lam); }
.mau { margin: 6px 0 4px; padding: 6px 10px; border-left: 3px solid var(--lam); background: #fafbfc; font-size: 13px; }
.noi-dung { white-space: pre-wrap; word-break: break-word; margin: 6px 0; }
.nho { font-size: 11px; }
</style>
