// Các nút của nhóm Q phía ui_server — `/api/*` của scripts/giao-dien/khach_va_nap.py (và `/api/ket-noi` của
// ui_server.py cho phiên Jira). Hình dạng CHÉP từ đó; nguồn sự thật là file Python.
import type { CauHinhAi } from './kieu'

export interface KetQuaUi { ok: boolean; loi?: string }

async function ui<T>(ten: string, than: object = {}): Promise<T & KetQuaUi & { http: number }> {
  const r = await fetch(`/api/${ten}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(than),
  })
  try {
    return { http: r.status, ...(await r.json()) }
  } catch {
    return { ok: false, http: r.status, loi: `HTTP ${r.status} — ui_server không trả JSON` } as T & KetQuaUi & { http: number }
  }
}

export interface KetQuaNapIssue {
  daCo?: boolean
  khoaNguon?: string
  soCase?: number
  soEvidence?: number
  corpusChan?: boolean
  baoCao?: string
  nhatKy?: string[]
}

export interface GoiNhan {
  luc: string
  cong: number | null
  maGui: string | null
  /** true/false = đã kiểm chữ ký · null = ui_server không giữ bí mật (khởi động lại sau khi đăng ký). */
  chuKyHopLe: boolean | null
  than: unknown
}

export const apiUi = {
  cauHinhDoc: () => ui<{ cauHinh: CauHinhAi; coFile: boolean }>('cau-hinh-ai-doc'),
  cauHinhGhi: (cauHinh: CauHinhAi) => ui<{ cauHinh: CauHinhAi }>('cau-hinh-ai-ghi', { cauHinh }),

  /** Phiên Jira — PAT/mật khẩu chỉ nằm trong RAM của ui_server, không trả về trình duyệt. */
  ketNoiJira: (baseUrl: string, pat: string, user: string, matKhau: string) =>
    ui<{ ten?: string }>('ket-noi', { baseUrl, pat, user, matKhau }),
  napIssue: (ma: string, cong: number) => ui<KetQuaNapIssue>('nap-issue', { ma, cong }),

  khachDangKy: (cong: number) => ui<{ url: string; active: boolean; giuBiMat: boolean }>('khach-dang-ky', { cong }),
  khachHopNhan: (cong: number) =>
    ui<{ goi: GoiNhan[]; giuBiMat: boolean | null; dangKy: { url: string; active: boolean } | null }>('khach-hop-nhan', { cong }),
  khachXoaHop: () => ui<object>('khach-xoa-hop'),
}
