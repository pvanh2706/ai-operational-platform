import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { api } from '../api'

/** Hai app trên máy dev. Danh sách thật nằm ở ui_server.py (KP_CONSOLE_CONG) — ngoài đó proxy từ chối. */
export const CAC_APP = [
  { cong: 5119, ten: 'App dev', moTa: 'kp_dev · khách dev-tenant' },
  { cong: 5120, ten: 'App giả lập', moTa: 'kp_gialap · khách gia-lap (O6-1)' },
] as const

const KHOA_LUU = 'kp-console:cong'

/** localStorage có thể ném (chế độ riêng tư, bị chặn) — chỉ là tiện ích, không được làm hỏng trang. */
function docCong(): number {
  try {
    const v = Number(localStorage.getItem(KHOA_LUU))
    return CAC_APP.some((a) => a.cong === v) ? v : 5120
  } catch {
    return 5120
  }
}

export const useKetNoi = defineStore('ketNoi', () => {
  const cong = ref<number>(docCong())
  const song = ref<boolean | null>(null)
  const loi = ref<string | null>(null)
  const soThaoTac = ref<number | null>(null)

  const app = computed(() => CAC_APP.find((a) => a.cong === cong.value) ?? CAC_APP[0])

  async function kiemTra() {
    song.value = null
    loi.value = null
    try {
      await api.suKhoe(cong.value)
      song.value = true
      const hd = await api.hopDong(cong.value)
      soThaoTac.value = Object.values(hd.paths ?? {}).reduce((n, p) => n + Object.keys(p).length, 0)
    } catch (e) {
      song.value = false
      soThaoTac.value = null
      loi.value = e instanceof Error ? e.message : String(e)
    }
  }

  watch(cong, (v) => {
    try {
      localStorage.setItem(KHOA_LUU, String(v))
    } catch {
      /* bỏ qua — xem docCong */
    }
    void kiemTra()
  })

  return { cong, app, song, loi, soThaoTac, kiemTra }
})
