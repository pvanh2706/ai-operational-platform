import type {
  BanGhiDaDuyet, BanGhiDaSua, BuocChiTiet, CaseEvidenceView, CaseTrongDanhSach, DongChayCase, KetQuaHoiDap,
  KetQuaNapTaiLieu, KetQuaThuKhoa, LichSuTriThuc, LoiHost, NhaCungCap, PromptHoiDap, QuyTrinhChiTiet, RanhGioiTenant,
  TaiLieu, TrangThaiAi,
} from './kieu'

/**
 * Gọi host QUA ui_server.py: `/app/<cổng>/<đường>` → `http://127.0.0.1:<cổng>/<đường>`.
 *
 * ⚠ Không gọi thẳng host: trình duyệt sẽ đòi CORS, và P3-1 chốt host .NET KHÔNG mở CORS. ui_server chỉ chuyển
 *   tiếp sang các cổng trong danh sách cho phép (mặc định 5119 app dev, 5120 app giả lập) — không phải proxy mở.
 */
export class LoiGoiApp extends Error {
  constructor(
    readonly status: number,
    readonly chiTiet: LoiHost | string | null,
  ) {
    const t = typeof chiTiet === 'string' ? chiTiet : chiTiet?.title ?? chiTiet?.detail
    super(`HTTP ${status}${t ? ` — ${t}` : ''}`)
  }
}

export async function goiApp<T>(cong: number, duong: string, init?: RequestInit): Promise<T> {
  const r = await fetch(`/app/${cong}${duong}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body && !(init.body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  })
  const raw = await r.text()
  let body: unknown = raw
  try {
    body = raw ? JSON.parse(raw) : null
  } catch {
    // không phải JSON (vd. trang lỗi của proxy) — giữ chuỗi thô để hiện ra, đừng nuốt
  }
  if (!r.ok) throw new LoiGoiApp(r.status, body as LoiHost | string | null)
  return body as T
}

/** Khoá nguồn đi trong ĐƯỜNG DẪN: `/` và `#` phải mã hoá, nếu không là mất nửa sau (`AR-g`). */
const maHoa = (khoa: string) => encodeURIComponent(khoa)

export const api = {
  suKhoe: (cong: number) => goiApp<unknown>(cong, '/health/ready'),
  hopDong: (cong: number) => goiApp<{ paths?: Record<string, Record<string, unknown>> }>(cong, '/openapi/v1.json'),
  danhSachCase: (cong: number, tienTo?: string, soLuong = 50) =>
    goiApp<CaseTrongDanhSach[]>(
      cong,
      `/internal/cases?soLuong=${soLuong}${tienTo ? `&tienTo=${encodeURIComponent(tienTo)}` : ''}`,
    ),
  dongChay: (cong: number, khoa: string) => goiApp<DongChayCase>(cong, `/internal/cases/${maHoa(khoa)}/dong-chay`),
  /** ĐI RA MẠNG (miễn phí) bằng khoá của host — chỉ gọi khi người bấm nút, không tự gọi khi mở trang. */
  thuKhoa: (cong: number, nhaCungCap: NhaCungCap = 'anthropic') =>
    goiApp<KetQuaThuKhoa>(cong, `/internal/thu-khoa-model?nhaCungCap=${nhaCungCap}`, { method: 'POST' }),
  /** Q6-1 — không ra mạng, không trả khoá. */
  trangThaiAi: (cong: number) => goiApp<TrangThaiAi>(cong, '/internal/ai'),

  danhSachTaiLieu: (cong: number) => goiApp<TaiLieu[]>(cong, '/documents'),
  /** Multipart — KHÔNG đặt Content-Type tay: trình duyệt phải tự viết boundary. */
  napTaiLieu: (cong: number, file: File, subTenantCode: string, nguoiNap: string) => {
    const f = new FormData()
    f.append('file', file)
    f.append('subTenantCode', subTenantCode)
    f.append('nguoiNap', nguoiNap)
    return goiApp<KetQuaNapTaiLieu>(cong, '/documents', { method: 'POST', body: f })
  },

  /** Q3-1 — TỐN TIỀN. 409 (không hỏi được) ném LoiGoiApp, kèm `chiTiet.ketQua`. */
  hoiDap: (cong: number, than: NguonHoi & { cauHoi: string; nguoiHoi: string; nhaCungCap: NhaCungCap }) =>
    goiApp<KetQuaHoiDap>(cong, '/internal/hoi-dap', { method: 'POST', body: JSON.stringify(than) }),
  xuatPromptHoiDap: (cong: number, than: NguonHoi & { cauHoi: string }) =>
    goiApp<PromptHoiDap>(cong, '/internal/hoi-dap/xuat-prompt', { method: 'POST', body: JSON.stringify(than) }),
  napKetQuaHoiDap: (cong: number, than: NguonHoi & { cauHoi: string; ketQua: string }) =>
    goiApp<KetQuaHoiDap>(cong, '/internal/hoi-dap/nap-ket-qua', { method: 'POST', body: JSON.stringify(than) }),

  // --- đọc / sửa / duyệt bản nháp (màn Bản nháp SOP). Đọc KHÔNG có tác dụng phụ; sửa và duyệt là hai đường riêng (S7).
  triThuc: (cong: number, id: string) => goiApp<LichSuTriThuc>(cong, `/knowledge/${id}`),
  /** Không đè bản A: host tạo bản B, bản A còn nguyên trong lịch sử. Lô bị từ chối TRỌN VẸN nếu một mục sai. */
  suaTriThuc: (cong: number, id: string, than: { nguoiSua: string; sua: { assertionId: string; noiDungMoi: string; mucKiemChung: string }[] }) =>
    goiApp<BanGhiDaSua>(cong, `/knowledge/${id}/sua`, { method: 'POST', body: JSON.stringify(than) }),
  duyetTriThuc: (cong: number, id: string, than: YeuCauDuyet) =>
    goiApp<BanGhiDaDuyet>(cong, `/knowledge/${id}/approve`, { method: 'POST', body: JSON.stringify(than) }),
  quyTrinh: (cong: number, id: string) => goiApp<QuyTrinhChiTiet>(cong, `/processes/${id}`),
  /** Chỉ DRAFT (N5-1). ⚠ THAY toàn bộ bước — host không giữ bản bước cũ. */
  thayBuoc: (cong: number, id: string, than: { nguoiSua: string; buoc: Omit<BuocChiTiet, 'thuTu'>[] }) =>
    goiApp<QuyTrinhChiTiet>(cong, `/processes/${id}/buoc`, { method: 'PUT', body: JSON.stringify(than) }),
  duyetQuyTrinh: (cong: number, id: string, than: YeuCauDuyet) =>
    goiApp<QuyTrinhChiTiet>(cong, `/processes/${id}/approve`, { method: 'POST', body: JSON.stringify(than) }),

  /** Đường đọc nội dung mẩu DUY NHẤT — nơi luật rút lại (AR-m) đã thực thi. Không tác dụng phụ. */
  evidenceCase: (cong: number, khoa: string) => goiApp<CaseEvidenceView>(cong, `/cases/${maHoa(khoa)}/evidence`),
  ranhGioi: (cong: number) => goiApp<RanhGioiTenant>(cong, '/internal/tenant-boundary'),
}

/** Thân duyệt — CÙNG cho tri thức và quy trình. `xacNhanDuLieuThuongMai` do NGƯỜI tích, không bao giờ điền sẵn (AR-n). */
export interface YeuCauDuyet {
  nguoiDuyet: string
  phamViXem: string
  xacNhanDuLieuThuongMai: boolean
  lyDo?: string
}

/** Lỗi host đọc được cho người: tiêu đề + chi tiết + lỗi từng mục (ValidationProblem). */
export function moTaLoi(e: unknown): string {
  if (!(e instanceof LoiGoiApp)) return e instanceof Error ? e.message : String(e)
  const c = e.chiTiet
  if (!c || typeof c === 'string') return `HTTP ${e.status}${c ? ` — ${c.slice(0, 400)}` : ''}`
  const muc = Object.entries(c.errors ?? {}).map(([k, v]) => `${k}: ${v.join(' ')}`)
  return [`HTTP ${e.status}${c.title ? ` — ${c.title}` : ''}`, c.detail, ...muc].filter(Boolean).join('\n')
}

/** Đúng MỘT trong hai — host trả 400 nếu không. */
export type NguonHoi = { khoaNguon: string; taiLieuId?: never } | { taiLieuId: string; khoaNguon?: never }
