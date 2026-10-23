import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../api'
import { apiUi, type TomTatPhien } from '../apiUi'
import type { Luot, NguCanh, PhienTraiNghiem } from '../phien'
import { CAC_APP } from './ketNoi'

/**
 * Phiên trải nghiệm theo TỪNG cổng app — Giả lập luôn ghi vào ngữ cảnh 5120 dù ô chọn app đang ở 5119.
 *
 * Ngữ cảnh = cổng + tenant mà app ĐANG phục vụ (GET /internal/tenant-boundary). Cùng cổng mà trỏ sang database/khách
 * khác thì ra thư mục khác ở ui_server — lịch sử của khách trước không hiện lại.
 *
 * localStorage chỉ nhớ "phiên nào đang chọn" cho trình duyệt này (tiện ích). Nội dung phiên nằm ở ui_server.
 */
interface TheoCong {
  nguCanh: NguCanh | null
  ds: TomTatPhien[]
  hienTai: PhienTraiNghiem | null
  loi: string | null
}

const khoaChon = (khoa: string) => `kp-console:phien:${khoa}`

function nhoChon(khoa: string, id: string | null) {
  try {
    if (id) localStorage.setItem(khoaChon(khoa), id)
    else localStorage.removeItem(khoaChon(khoa))
  } catch {
    /* chế độ riêng tư — chỉ mất tiện ích nhớ lựa chọn */
  }
}
function docChon(khoa: string): string | null {
  try {
    return localStorage.getItem(khoaChon(khoa))
  } catch {
    return null
  }
}

export const usePhien = defineStore('phien', () => {
  const theoCong = ref<Record<number, TheoCong>>({})

  function o(cong: number): TheoCong {
    // ⚠ KHÔNG `return (theoCong.value[cong] ??= {...})`: biểu thức gán trả về object THÔ, không phải proxy reactive — màn
    //   nào gọi o() đầu tiên giữ object thô, và danh sách tải về sau không bao giờ hiện ra (bấm thật bắt được: mở thẳng màn
    //   Phiên trên trình duyệt mới thì báo "chưa có phiên nào" dù có hai phiên).
    if (!theoCong.value[cong]) theoCong.value[cong] = { nguCanh: null, ds: [], hienTai: null, loi: null }
    return theoCong.value[cong]!
  }

  async function nguCanh(cong: number): Promise<NguCanh> {
    const s = o(cong)
    if (s.nguCanh) return s.nguCanh
    const app = CAC_APP.find((a) => a.cong === cong)
    let tenantId: string | null = null
    let cheDoTenant: string | null = null
    try {
      const r = await api.ranhGioi(cong)
      tenantId = r.tenantId ? String(r.tenantId).toLowerCase() : null
      cheDoTenant = r.mode
    } catch {
      // App không trả lời: vẫn ghi được lượt LỖI VẬN HÀNH — vào thư mục "khong-ro-tenant" của cổng này, ghi rõ ra.
    }
    s.nguCanh = {
      khoa: `cong-${cong}.${tenantId ?? 'khong-ro-tenant'}`,
      cong,
      app: app?.ten ?? `App :${cong}`,
      moTa: app?.moTa ?? '',
      tenantId,
      cheDoTenant,
      moiTruong: cong === 5120 ? 'gia-lap' : 'dev',
    }
    return s.nguCanh
  }

  /** Đọc danh sách + phiên đang chọn. KHÔNG tạo gì — mở trang không ghi. */
  async function tai(cong: number, lamMoiNguCanh = false) {
    const s = o(cong)
    if (lamMoiNguCanh) s.nguCanh = null
    s.loi = null
    try {
      const nc = await nguCanh(cong)
      const k = await apiUi.phienDs(nc.khoa)
      if (!k.ok) throw new Error(k.loi ?? 'ui_server không đọc được danh sách phiên')
      s.ds = k.phien
      const id = s.hienTai?.nguCanh.khoa === nc.khoa ? s.hienTai.id : docChon(nc.khoa)
      s.hienTai = null
      if (id && s.ds.some((p) => p.id === id)) await chon(cong, id)
    } catch (e) {
      s.loi = e instanceof Error ? e.message : String(e)
    }
  }

  async function chon(cong: number, id: string | null) {
    const s = o(cong)
    const nc = await nguCanh(cong)
    if (!id) {
      s.hienTai = null
      nhoChon(nc.khoa, null)
      return
    }
    const k = await apiUi.phienDoc(nc.khoa, id)
    if (!k.ok) throw new Error(k.loi ?? 'Không đọc được phiên')
    s.hienTai = k.phien
    nhoChon(nc.khoa, id)
  }

  async function taoMoi(cong: number, ten: string, nguoi: string, kichBan: string | null = null) {
    const s = o(cong)
    const nc = await nguCanh(cong)
    const k = await apiUi.phienTao(nc, ten, nguoi, kichBan)
    if (!k.ok) throw new Error(k.loi ?? 'Không tạo được phiên')
    s.hienTai = k.phien
    nhoChon(nc.khoa, k.phien.id)
    const ds = await apiUi.phienDs(nc.khoa)
    if (ds.ok) s.ds = ds.phien
    return k.phien
  }

  /**
   * Ghi (thêm hoặc cập nhật) MỘT lượt vào phiên đang chọn; chưa có phiên thì tạo một phiên mặc định — một lượt hỏi
   * không bao giờ mất chỉ vì người thử quên bấm "phiên mới". Lỗi ghi KHÔNG làm hỏng thao tác chính: trả chuỗi lỗi.
   */
  async function ghiLuot(cong: number, luot: Luot): Promise<string | null> {
    const s = o(cong)
    try {
      const nc = await nguCanh(cong)
      if (!s.hienTai || s.hienTai.nguCanh.khoa !== nc.khoa) {
        await taoMoi(cong, '', luot.nguoiThaoTac ?? '')
      }
      const k = await apiUi.phienLuot(nc.khoa, s.hienTai!.id, luot)
      if (!k.ok) throw new Error(k.loi ?? `ui_server trả HTTP ${k.http}`)
      s.hienTai = k.phien
      const t = s.ds.find((p) => p.id === k.phien.id)
      if (t) t.soLuot = k.phien.luot.length
      return null
    } catch (e) {
      return `Chưa lưu được vào phiên: ${e instanceof Error ? e.message : String(e)}`
    }
  }

  async function xoa(cong: number, id: string) {
    const nc = await nguCanh(cong)
    const k = await apiUi.phienXoa(nc.khoa, id)
    if (!k.ok) throw new Error(k.loi ?? 'Không xoá được phiên')
    if (o(cong).hienTai?.id === id) await chon(cong, null)
    await tai(cong)
  }

  async function sua(cong: number, id: string, s: { ten?: string; ghiChu?: string; nguoiThaoTac?: string }) {
    const nc = await nguCanh(cong)
    const k = await apiUi.phienSua(nc.khoa, id, s)
    if (!k.ok) throw new Error(k.loi ?? 'Không sửa được phiên')
    if (o(cong).hienTai?.id === id) o(cong).hienTai = k.phien
  }

  return { theoCong, o, nguCanh, tai, chon, taoMoi, ghiLuot, xoa, sua }
})
