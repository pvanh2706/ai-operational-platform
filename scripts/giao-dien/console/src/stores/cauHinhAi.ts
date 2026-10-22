import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api } from '../api'
import { apiUi } from '../apiUi'
import type { CauHinhAi, CheDoAi, KetQuaThuKhoa, NhaCungCap, TrangThaiAi } from '../kieu'

/**
 * Cấu hình AI CHUNG cho mọi màn (Q6-1) — thay localStorage riêng của trang giả lập.
 *
 * Hai nguồn, hai vai, cố ý không trộn:
 *   ui_server  LỰA CHỌN của người vận hành: bước nào đi API, bước nào đi File, API thì nhà cung cấp nào (cau-hinh-ai.json)
 *   host       KHẢ NĂNG: nhà cung cấp nào có khoá/model; định tuyến dùng nhà cung cấp nào (GET /internal/ai)
 * Một lựa chọn API mà host không dùng được thì `cheDoHieuLuc` trả File — chọn không làm nó dùng được.
 */
export const useCauHinhAi = defineStore('cauHinhAi', () => {
  const cauHinh = ref<CauHinhAi | null>(null)
  const trangThai = ref<TrangThaiAi | null>(null)
  const loi = ref<string | null>(null)
  /** Kết quả Thử khoá gần nhất theo nhà cung cấp — false = bị TỪ CHỐI → API bị khoá cho tới khi thử lại được. */
  const hopLe = ref<Record<NhaCungCap, boolean | null>>({ anthropic: null, openai: null })

  async function tai(cong: number) {
    loi.value = null
    const k = await apiUi.cauHinhDoc()
    if (!k.ok) loi.value = k.loi ?? 'ui_server không đọc được cấu hình'
    else cauHinh.value = k.cauHinh
    try {
      trangThai.value = await api.trangThaiAi(cong)
    } catch (e) {
      trangThai.value = null
      loi.value = e instanceof Error ? e.message : String(e)
    }
  }

  async function ghi(moi: CauHinhAi) {
    const k = await apiUi.cauHinhGhi(moi)
    if (!k.ok) throw new Error(k.loi ?? 'ui_server không ghi được cấu hình')
    cauHinh.value = k.cauHinh
  }

  function sauThuKhoa(kq: KetQuaThuKhoa) {
    hopLe.value = { ...hopLe.value, [kq.nhaCungCap ?? 'anthropic']: kq.hopLe }
  }

  /** Nhà cung cấp này có gọi được không — theo host, trừ khi lần Thử khoá gần nhất đã bị từ chối. */
  function dungDuoc(ncc: NhaCungCap): boolean {
    const t = trangThai.value
    if (!t) return false
    const n = ncc === 'openai' ? t.openAI : t.anthropic
    return n.dungDuoc && hopLe.value[ncc] !== false
  }

  /** API có chọn được cho bước này không. Định tuyến còn cần DinhTuyen:GoiModel — không thì /hoi trả KHÔNG BIẾT. */
  function apiChonDuoc(buoc: keyof CauHinhAi, ncc?: NhaCungCap): boolean {
    const t = trangThai.value
    if (!t) return false
    if (buoc === 'dinhTuyen') return t.dinhTuyenGoiModel && dungDuoc(t.dinhTuyen.nhaCungCap)
    return dungDuoc(ncc ?? cauHinh.value?.[buoc].nhaCungCap ?? 'anthropic')
  }

  /** Chế độ THẬT SỰ dùng: chọn API mà không dùng được thì là File. */
  const cheDoHieuLuc = computed(() => {
    const c = cauHinh.value
    const tinh = (b: keyof CauHinhAi): CheDoAi => (c?.[b].cheDo === 'api' && apiChonDuoc(b) ? 'api' : 'file')
    return { dinhTuyen: tinh('dinhTuyen'), soanNhap: tinh('soanNhap'), hoiDap: tinh('hoiDap') }
  })

  return { cauHinh, trangThai, loi, hopLe, tai, ghi, sauThuKhoa, dungDuoc, apiChonDuoc, cheDoHieuLuc }
})
