<script setup lang="ts">
// Giả lập hai luồng trong console (P12-4) — thay trang /gia-lap HTML cũ. Cùng các nút /api/gl-* của ui_server, cộng
// BẢN ĐỒ LÔ: sau mỗi bước, cả lô được tô lại theo đúng sơ đồ luồng của màn dòng chảy.
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, LoiGoiApp, moTaLoi } from '../api'
import {
  CONG_GIA_LAP, giaLap, type BanNhapDaNap, type DongCham, type KetQuaDinhTuyen, type KetQuaSoan, type TicketGiaLap,
  type TrangThaiGiaLap,
} from '../apiGiaLap'
import BanDoLo from '../components/BanDoLo.vue'
import DanhGiaLuot from '../components/DanhGiaLuot.vue'
import KetQuaHoiDoc from '../components/KetQuaHoiDoc.vue'
import NhanNguon from '../components/NhanNguon.vue'
import PhienBar from '../components/PhienBar.vue'
import XuatNapFile from '../components/XuatNapFile.vue'
import ThuKhoa from '../components/ThuKhoa.vue'
import type { CheDoAi as CheDo, KetQuaThuKhoa } from '../kieu'
import { LY_DO_KHONG_BIET, LY_DO_KHONG_CO_QUY_TRINH } from '../doc'
import { quenEvidence } from '../nguon'
import { danhGiaTrong, idMoi, ketCucGiaLap, type DanhGia, type Luot } from '../phien'
import { useCauHinhAi } from '../stores/cauHinhAi'
import { useKetNoi } from '../stores/ketNoi'
import { usePhien } from '../stores/phien'

const ketNoi = useKetNoi()
const ai = useCauHinhAi()
const phienTN = usePhien()
const router = useRouter()

const tt = ref<TrangThaiGiaLap | null>(null)
const nguoi = ref('trang-gia-lap')
const cheDo = ref<{ dinhTuyen: CheDo; soanNhap: CheDo }>({ dinhTuyen: 'file', soanNhap: 'file' })
const ticket = ref<TicketGiaLap[]>([])
const lo = ref<string | null>(null)
const tienTo = ref<string | null>(null)
const phien = ref(0)
const ban = ref<string | null>(null)
const soLuong = ref(11)

const baoSinh = ref<string | null>(null)
const ketQuaDT = ref<KetQuaDinhTuyen | null>(null)
const nhomLo = ref<{ ten: string; soCase: number }[]>([])
const nhom = ref('')
const ketQuaSoan = ref<KetQuaSoan | null>(null)
const ketQuaDuyet = ref<Awaited<ReturnType<typeof giaLap.duyet>> | null>(null)
const hoiLai = ref<DongCham[] | null>(null)
const baoVong = ref<{ ok: boolean; text: string } | null>(null)
const loiChung = ref<string | null>(null)

// ------------------------------------------------------------- bước 0

async function kiem() {
  ban.value = 'kiem'
  loiChung.value = null
  try {
    const k = await giaLap.trangThai()
    if (!k.ok) throw new Error(k.loi ?? 'ui_server từ chối')
    tt.value = k
    lo.value = k.lo
    tienTo.value = k.tienTo
    ticket.value = k.ticket
    banNhapLuu.value = k.banNhap
    // Q6-1: chế độ đọc từ cấu hình CHUNG (ui_server), không từ localStorage riêng của trang này. Chọn API mà host không
    // dùng được thì cheDoHieuLuc đã là File.
    await ai.tai(CONG_GIA_LAP)
    cheDo.value = { dinhTuyen: ai.cheDoHieuLuc.dinhTuyen, soanNhap: ai.cheDoHieuLuc.soanNhap }
    await taiNhomLo()
  } catch (e) {
    loiChung.value = e instanceof Error ? e.message : String(e)
  } finally {
    ban.value = null
  }
}
/**
 * Người dùng BẤM đổi chế độ ở trang này = đổi cấu hình chung (màn Cấu hình AI thấy cùng lựa chọn).
 *
 * 🛑 Gọi từ @change của ô chọn, KHÔNG từ watch(cheDo). Bản đầu dùng watch + cờ "đang nạp", và lượt bấm thật bắt được:
 *   watcher của Vue chạy SAU khi cờ đã tắt, nên chỉ MỞ trang là ghi file — tệ hơn, ghi chế độ HIỆU LỰC (File khi khoá
 *   tạm thiếu) đè lên lựa chọn API của người dùng. Mở một trang không được đổi cấu hình.
 */
function luuCheDo() {
  if (!ai.cauHinh) return
  void ai.ghi({
    ...ai.cauHinh,
    dinhTuyen: { cheDo: cheDo.value.dinhTuyen },
    soanNhap: { ...ai.cauHinh.soanNhap, cheDo: cheDo.value.soanNhap },
  }).catch((e) => { loiChung.value = e instanceof Error ? e.message : String(e) })
}

/** API chọn được không, theo TỪNG bước: định tuyến cần DinhTuyen:GoiModel; soạn nháp theo nhà cung cấp đã chọn. */
const apiDung = computed(() => ({ dinhTuyen: ai.apiChonDuoc('dinhTuyen'), soanNhap: ai.apiChonDuoc('soanNhap') }))
const nccSoan = computed(() => ai.cauHinh?.soanNhap.nhaCungCap ?? 'anthropic')

function sauThuKhoa(kq: KetQuaThuKhoa) {
  ai.sauThuKhoa(kq)
  // Khoá bị từ chối thì mọi lượt bấm API sau đó đều hỏng — chuyển về File thay vì để người dùng tốn lượt mới biết.
  // Chỉ những bước DÙNG nhà cung cấp vừa bị từ chối: khoá OpenAI sai không làm định tuyến bằng Anthropic hỏng theo.
  // ⚠ Chỉ đổi cho phiên này, KHÔNG ghi file: sửa khoá rồi thử lại được thì lựa chọn API đã lưu vẫn còn nguyên.
  if (kq.hopLe === false) {
    const ncc = kq.nhaCungCap ?? 'anthropic'
    cheDo.value = {
      dinhTuyen: ai.trangThai?.dinhTuyen.nhaCungCap === ncc ? 'file' : cheDo.value.dinhTuyen,
      soanNhap: nccSoan.value === ncc ? 'file' : cheDo.value.soanNhap,
    }
  }
}
onMounted(kiem)

/** Nhóm của lô, đọc từ HOST (nhóm hiệu lực) chứ không từ RAM của ui_server — tải lại trang không mất. */
async function taiNhomLo() {
  if (!tienTo.value) return
  const ds = await api.danhSachCase(CONG_GIA_LAP, tienTo.value, 200)
  const dem = new Map<string, number>()
  for (const c of ds) if (c.nhomHieuLuc) dem.set(c.nhomHieuLuc, (dem.get(c.nhomHieuLuc) ?? 0) + 1)
  nhomLo.value = [...dem].map(([ten, soCase]) => ({ ten, soCase })).sort((a, b) => b.soCase - a.soCase)
  if (!nhomLo.value.some((n) => n.ten === nhom.value)) nhom.value = nhomLo.value[0]?.ten ?? ''
}

async function sauMoiBuoc() {
  phien.value++
  await taiNhomLo()
}

async function lam<T>(ten: string, viec: () => Promise<T>): Promise<T | undefined> {
  ban.value = ten
  loiChung.value = null
  try {
    return await viec()
  } catch (e) {
    loiChung.value = e instanceof Error ? e.message : String(e)
  } finally {
    ban.value = null
  }
}

// ------------------------------------------------------------- bước 1

async function sinh() {
  await lam('sinh', async () => {
    const k = await giaLap.sinh(soLuong.value)
    if (!k.ok) { baoSinh.value = k.loi ?? 'Không sinh được lô'; return }
    lo.value = k.lo
    tienTo.value = k.tienTo
    ticket.value = k.ticket
    baoSinh.value = `Lô ${k.lo} — tạo ${k.soCase} case, ${k.soEvidence} mẩu evidence.`
    ketQuaDT.value = ketQuaSoan.value = ketQuaDuyet.value = null
    hoiLai.value = null
    await sauMoiBuoc()
  })
}

// ------------------------------------------------------------- bước 2 — luồng xuôi

async function dinhTuyenApi() {
  await lam('dt', async () => {
    ketQuaDT.value = await giaLap.dinhTuyenApi()
    await sauMoiBuoc()
  })
}
async function napDinhTuyen(txt: string) {
  ketQuaDT.value = await giaLap.napDinhTuyen(txt, nguoi.value)
  await sauMoiBuoc()
}

// ------------------------------------------------------------- bước 3–4 — luồng ngược

async function soanApi() {
  await lam('soan', async () => {
    ketQuaSoan.value = await giaLap.soanApi(nhom.value, nguoi.value, nccSoan.value)
    await sauMoiBuoc()
  })
}
const xuatSoan = () => giaLap.xuatSoan(nhom.value)

async function napSoan(txt: string) {
  ketQuaSoan.value = await giaLap.napSoan(txt)
  await sauMoiBuoc()
}

/** Đường API trả KetQuaSoanNhap {daNap, soCase, soCaseTuModel}; đường file trả thẳng kết quả nạp; lỗi là ProblemDetails. */
const banNhap = computed(() => {
  const k = ketQuaSoan.value?.ketQua as Record<string, any> | null | undefined
  if (!k) return null
  return { nap: (k.daNap ?? (k.nguyenNhan ? k : null)) as Record<string, any> | null, goc: k }
})

/** Bản nháp ui_server còn nhớ (RAM hoặc gia-lap-io/<lô>/ban-nhap.json) — tải lại trang vẫn mở được màn đọc/duyệt. */
const banNhapLuu = ref<BanNhapDaNap | null>(null)
const banNhapMo = computed<BanNhapDaNap | null>(() => (banNhap.value?.nap as BanNhapDaNap | null | undefined) ?? banNhapLuu.value)
const denBanNhap = computed(() => {
  const b = banNhapMo.value
  if (!b) return null
  return {
    name: 'ban-nhap',
    query: { cong: String(CONG_GIA_LAP), tri: b.nguyenNhan.map((n) => n.id).join(','), qt: b.quyTrinh?.id ?? undefined, nhom: b.nhom },
  }
})

async function duyet() {
  await lam('duyet', async () => {
    ketQuaDuyet.value = await giaLap.duyet(nguoi.value)
    await sauMoiBuoc()
  })
}

// ------------------------------------------------------------- bước 5 — đóng vòng

const khoaCuaNhom = computed(() => {
  // Ticket của nhóm đang soạn, theo nhóm HIỆU LỰC mà bản đồ lô vừa đọc từ host.
  return ticket.value.map((t) => t.khoa)
})

// Mỗi ticket hỏi lại = MỘT lượt trong phiên (bản chụp hàng /hoi, gồm hướng dẫn + khoá dẫn chứng). Hỏi lại lần nữa là
// lượt MỚI — lượt cũ giữ nguyên. Mở/đóng phần đọc chỉ là giao diện: không gọi host.
const luotCua = ref<Record<string, Luot>>({})
const moRong = ref<Set<string>>(new Set())
const loiPhien = ref<string | null>(null)

function batTat(khoa: string) {
  const s = new Set(moRong.value)
  if (s.has(khoa)) s.delete(khoa)
  else s.add(khoa)
  moRong.value = s
}

function luotGiaLap(
  d: DongCham | null, batDau: Date, ms: number, soTicket: number, truocDo: string | null, loi?: { http: number | null; text: string },
): Luot {
  return {
    id: idMoi(),
    loai: 'hoi-gia-lap',
    batDau: batDau.toISOString(),
    ketThuc: new Date().toISOString(),
    thoiGianChoMs: ms,
    thoiGianCaLo: soTicket > 1 ? soTicket : undefined,
    nguoiThaoTac: nguoi.value || null,
    nguon: d
      ? { loai: 'ticket-gia-lap', khoa: d.khoa, ten: d.tieuDe }
      : { loai: 'ticket-gia-lap', khoa: `nhóm ${nhom.value}`, ten: 'hỏi lại cả nhóm' },
    cauHoi: d ? `POST /hoi {maTicket: ${d.khoa}}` : `hỏi lại nhóm ${nhom.value}`,
    // /hoi không nói nó có gọi model hay không — nguồn định tuyến nằm trong bản chụp. Không gán chế độ/model khi không biết.
    cheDo: null,
    nhaCungCap: null,
    model: null,
    ketCuc: d ? ketCucGiaLap(d) : 'loi-van-hanh',
    loi: d?.loiVanHanh ? { http: d.http ?? null, lyDo: null, text: d.loiVanHanh } : loi ? { http: loi.http, lyDo: null, text: loi.text } : null,
    giaLap: d,
    lienKet: { maLuotGoiY: d?.maLuotGoiY ?? null, truocDo, triThuc: d?.huongDan?.map((h) => h.id) ?? [], quyTrinh: d?.quyTrinh?.id ?? null },
    kichBan: phienTN.o(CONG_GIA_LAP).hienTai?.kichBan ?? null,
    danhGia: danhGiaTrong(),
  }
}

async function ghiLuotDs(ds: Luot[]) {
  for (const l of ds) {
    const loi = await phienTN.ghiLuot(CONG_GIA_LAP, l)
    if (loi) {
      loiPhien.value = loi
      return
    }
    if (l.giaLap) luotCua.value = { ...luotCua.value, [l.giaLap.khoa]: l }
  }
  loiPhien.value = null
}

async function luuDanhGia(khoa: string, danhGia: DanhGia) {
  const l = luotCua.value[khoa]
  if (!l) return
  const moi = { ...l, danhGia }
  const loi = await phienTN.ghiLuot(CONG_GIA_LAP, moi)
  loiPhien.value = loi
  if (!loi) luotCua.value = { ...luotCua.value, [khoa]: moi }
}

async function hoi() {
  const batDau = new Date()
  await lam('hoi', async () => {
    // Mọi chỗ hỏng của lượt (app chết ngay ở bước đọc danh sách case, ui_server lỗi…) đều phải thành một lượt LỖI VẬN HÀNH
    // trong phiên — bấm thật đã bắt được bản đầu bỏ sót: 502 ở danhSachCase ném ra trước khối try, lượt không được ghi.
    let k: Awaited<ReturnType<typeof giaLap.hoiLai>>
    let httpLoi: number | null = null
    try {
      const ds = await api.danhSachCase(CONG_GIA_LAP, tienTo.value ?? undefined, 200)
      const khoa = ds.filter((c) => c.nhomHieuLuc === nhom.value).map((c) => c.khoaNguon)
      k = await giaLap.hoiLai(nhom.value, khoa.length ? khoa : khoaCuaNhom.value)
    } catch (e) {
      k = { ok: false, loi: moTaLoi(e), nhom: nhom.value, dong: [] }
      httpLoi = e instanceof LoiGoiApp ? e.status : null
    }
    const ms = Date.now() - batDau.getTime()
    hoiLai.value = k.ok ? k.dong : null
    if (!k.ok) {
      loiChung.value = k.loi ?? 'Hỏi lại thất bại'
      // Cả lô không chạy được (ui_server/app chết) — vẫn là một lượt, kết cục LỖI VẬN HÀNH, không phải "không biết".
      await ghiLuotDs([luotGiaLap(null, batDau, ms, 0, null, { http: httpLoi, text: k.loi ?? 'Hỏi lại thất bại' })])
      return
    }
    await ghiLuotDs(k.dong.map((d) => luotGiaLap(d, batDau, ms, k.dong.length, luotCua.value[d.khoa]?.id ?? null)))
    await sauMoiBuoc()
  })
}

async function xacNhan(d: DongCham) {
  const batDau = new Date()
  await lam(`xn-${d.khoa}`, async () => {
    const k = await giaLap.xacNhanBuoc(d.khoa, d.buocHienTai!, nguoi.value)
    quenEvidence(CONG_GIA_LAP, d.khoa)
    const sau = k.dong?.[0]
    const ketQua = k.ketQua as { title?: string; detail?: string } | null
    baoVong.value = k.ok
      ? {
          ok: true,
          text: `Đã xác nhận ${d.buocHienTai} cho ${d.khoa.split('-').pop()} → bước hiện tại giờ là ${
            sau?.buocHienTai ? `${sau.buocHienTai} · ${sau.noiDungBuoc ?? ''}` : sau?.daXongHet ? 'đã xong hết' : '—'}.`,
        }
      : { ok: false, text: `HTTP ${k.http}: ${ketQua?.title ?? ''} ${ketQua?.detail ?? JSON.stringify(k.ketQua)}` }
    if (k.ok && hoiLai.value && sau) {
      hoiLai.value = hoiLai.value.map((x) => (x.khoa === d.khoa ? sau : x))
      // ui_server hỏi lại ticket sau khi ghi lời xác nhận — một kết quả /hoi MỚI, nối sau lượt trước.
      await ghiLuotDs([luotGiaLap(sau, batDau, Date.now() - batDau.getTime(), 1, luotCua.value[d.khoa]?.id ?? null)])
    }
    await sauMoiBuoc()
  })
}

async function khaiDung(d: DongCham) {
  await lam(`ph-${d.khoa}`, async () => {
    const k = await giaLap.phanHoi(d.maLuotGoiY!, nguoi.value)
    baoVong.value = k.ok ? { ok: true, text: `Đã ghi nấc USED (lời khai) cho ${d.khoa.split('-').pop()}.` } : { ok: false, text: `HTTP ${k.http}` }
    await sauMoiBuoc()
  })
}

function mo(khoa: string) {
  ketNoi.cong = CONG_GIA_LAP
  void router.push({ name: 'case', params: { khoa } })
}

const chamLabel = { dung: '✓ đúng', sai: '✗ sai', 'khong-biet': '? không biết' } as const
</script>

<template>
  <PhienBar :cong="CONG_GIA_LAP" :nguoi="nguoi" />
  <div v-if="loiChung" class="bao loi">{{ loiChung }}</div>

  <!-- 0 -->
  <section class="the">
    <h2>0 · Kiểm tra &amp; chọn cách gán AI</h2>
    <p class="mo">
      Giả lập chạy trên <b>app giả lập :{{ CONG_GIA_LAP }}</b> (kp_gialap) — chạy <code>scripts\giao-dien\run-gia-lap-app.bat</code> trước.
      Ô chọn app ở góc phải KHÔNG đổi chỗ giả lập chạy.
    </p>
    <div v-if="tt" class="trang-thai">
      <div :class="tt.appSong ? 'tot' : 'xau'">{{ tt.appSong ? '✓ App giả lập đang chạy' : '✗ App giả lập không trả lời' }}</div>
      <div :class="tt.coKhoa ? 'tot' : 'mo'">{{ tt.coKhoa ? '✓ Có khoá Anthropic' : '○ Không có khoá — chỉ dùng được chế độ file' }}</div>
      <div :class="tt.goiModel ? 'tot' : 'mo'">
        {{ tt.goiModel ? '✓ DinhTuyen:GoiModel bật — định tuyến bằng API dùng được (tính tiền mỗi ticket)' : '○ DinhTuyen:GoiModel tắt — định tuyến bằng API trả KHÔNG BIẾT cho ticket mới' }}
      </div>
      <div v-if="tt.nhomDungLech.length" class="xau">✗ Dữ liệu giả có nhóm ngoài taxonomy: {{ tt.nhomDungLech.join(', ') }}</div>
      <details v-if="tt.cauHinh"><summary>--kiem-cau-hinh</summary><pre>{{ tt.cauHinh }}</pre></details>
      <ThuKhoa v-if="tt.coKhoa" :cong="CONG_GIA_LAP" @ket-qua="sauThuKhoa" />
      <ThuKhoa v-if="ai.trangThai?.openAI.coKhoa" :cong="CONG_GIA_LAP" nha-cung-cap="openai" :model="ai.trangThai.openAI.model" @ket-qua="sauThuKhoa" />
    </div>
    <div class="hang">
      <fieldset>
        <legend>Định tuyến — luồng xuôi</legend>
        <label><input v-model="cheDo.dinhTuyen" type="radio" value="api" :disabled="!apiDung.dinhTuyen" @change="luuCheDo" /> API</label>
        <label><input v-model="cheDo.dinhTuyen" type="radio" value="file" @change="luuCheDo" /> File</label>
      </fieldset>
      <fieldset>
        <legend>Soạn nháp SOP — luồng ngược</legend>
        <label><input v-model="cheDo.soanNhap" type="radio" value="api" :disabled="!apiDung.soanNhap" @change="luuCheDo" /> API · {{ nccSoan }}</label>
        <label><input v-model="cheDo.soanNhap" type="radio" value="file" @change="luuCheDo" /> File</label>
      </fieldset>
      <label class="nguoi">Người thao tác <input v-model="nguoi" /></label>
    </div>
    <p class="mo">Lựa chọn ở đây LÀ cấu hình chung — đổi nhà cung cấp, hoặc xem vì sao API bị khoá, ở màn
      <RouterLink :to="{ name: 'cau-hinh' }">Cấu hình AI</RouterLink>.</p>
    <button class="phu" :disabled="!!ban" @click="kiem">{{ ban === 'kiem' ? 'Đang kiểm…' : 'Kiểm lại' }}</button>
  </section>

  <!-- 1 + bản đồ lô -->
  <section class="the">
    <h2>1 · Lô dữ liệu giả</h2>
    <div class="hang">
      <label class="nguoi">Số ticket <input v-model.number="soLuong" type="number" min="1" max="11" style="width: 70px" /></label>
      <button :disabled="!!ban || !tt?.appSong" @click="sinh">{{ ban === 'sinh' ? 'Đang sinh…' : 'Sinh lô mới & gửi vào Kênh 1' }}</button>
    </div>
    <div v-if="baoSinh" class="bao ok">{{ baoSinh }}</div>
    <p v-if="lo" class="mo">Đang dùng lô <b>{{ lo }}</b> · {{ ticket.length }} ticket</p>
    <BanDoLo v-if="ticket.length" :ticket="ticket" :phien="phien" />
  </section>

  <template v-if="ticket.length">
    <!-- 2 -->
    <section class="the">
      <h2>2 · <span class="luong">Luồng xuôi</span> Định tuyến — {{ cheDo.dinhTuyen === 'api' ? 'API' : 'file' }}</h2>
      <template v-if="cheDo.dinhTuyen === 'api'">
        <p class="mo">Gọi <code>POST /hoi</code> cho từng ticket — app tự định tuyến: người nạp → bảng đo → model.</p>
        <button :disabled="!!ban" @click="dinhTuyenApi">{{ ban === 'dt' ? 'Đang định tuyến…' : 'Định tuyến cả lô bằng API' }}</button>
      </template>
      <XuatNapFile v-else :xuat="giaLap.xuatDinhTuyen" :nap="napDinhTuyen"
                   goi-y='{ "ketQua": [ { "khoaNguon": "...", "nhom": "...", "nguon": "...", "giaiThich": "..." } ] }' />
      <template v-if="ketQuaDT">
        <div v-if="ketQuaDT.cham" class="diem">
          <div><b>{{ ketQuaDT.cham.dung }}/{{ ketQuaDT.cham.tong }}</b>đúng</div>
          <div><b>{{ ketQuaDT.cham.sai }}</b>sai nhóm</div>
          <div><b>{{ ketQuaDT.cham.khongBiet }}</b>không biết</div>
        </div>
        <div v-if="ketQuaDT.loi && !ketQuaDT.dong" class="bao loi">{{ ketQuaDT.loi }}</div>
        <details v-if="ketQuaDT.baoCao" :open="ketQuaDT.ok === false"><summary>Báo cáo lệnh nạp (mã thoát {{ ketQuaDT.maThoat }})</summary><pre>{{ ketQuaDT.baoCao }}</pre></details>
        <table v-if="ketQuaDT.dong">
          <thead><tr><th>Ticket</th><th>Nhóm AI</th><th>Nguồn · giải thích</th><th /></tr></thead>
          <tbody>
            <tr v-for="d in ketQuaDT.dong" :key="d.khoa">
              <td><a href="#" @click.prevent="mo(d.khoa)"><code>{{ d.khoa.split('-').pop() }}</code></a> {{ d.tieuDe }}</td>
              <td>{{ d.nhom }}</td>
              <td><NhanNguon :nguon="d.nguon" /> <span class="mo">{{ d.giaiThich }}</span></td>
              <td :class="['kq', d.ketQua]">{{ chamLabel[d.ketQua] }}</td>
            </tr>
          </tbody>
        </table>
      </template>
    </section>

    <!-- 3 -->
    <section class="the">
      <h2>3 · <span class="luong">Luồng ngược</span> Soạn nháp SOP — {{ cheDo.soanNhap === 'api' ? 'API' : 'file' }}</h2>
      <p class="mo">Gom case của nhóm (người nạp + model qua ngưỡng, O5-1). Bản nháp vào kho ở DRAFT, kèm một quy trình DRAFT.</p>
      <p v-if="!nhomLo.length" class="bao chu-y">Chưa ticket nào của lô có nhóm — định tuyến ở bước 2 trước.</p>
      <template v-else>
        <label class="nguoi">Nhóm
          <select v-model="nhom"><option v-for="n in nhomLo" :key="n.ten" :value="n.ten">{{ n.ten }} — {{ n.soCase }} case</option></select>
        </label>
        <button v-if="cheDo.soanNhap === 'api'" :disabled="!!ban" @click="soanApi">{{ ban === 'soan' ? 'Đang soạn (gọi model)…' : 'Soạn nháp bằng API' }}</button>
        <XuatNapFile v-else :xuat="xuatSoan" :nap="napSoan" goi-y='{ "nhom": "...", "trieuChungVao": [...], "buocKiem": [...], ... }' />
      </template>
      <template v-if="ketQuaSoan">
        <div v-if="ketQuaSoan.loi && !ketQuaSoan.ketQua" class="bao loi">{{ ketQuaSoan.loi }}</div>
        <div v-else-if="!ketQuaSoan.ok" class="bao loi">
          HTTP {{ ketQuaSoan.http }} — <b>{{ banNhap?.goc?.lyDo ?? banNhap?.goc?.title }}</b>
          <pre v-if="banNhap?.goc?.detail">{{ banNhap?.goc?.detail }}</pre>
          <span v-if="banNhap?.goc?.lyDo === 'chua-co-credit'">Chuyển “Soạn nháp” ở bước 0 sang <b>File</b> để đi tiếp không cần khoá.</span>
        </div>
        <div v-else-if="banNhap?.nap" class="bao ok">
          Nhóm <b>{{ banNhap.nap.nhom }}</b> · tạo <b>{{ banNhap.nap.daTao }}</b> bản ghi tri thức (đã có từ trước {{ banNhap.nap.daCoTuTruoc }})
          <template v-if="banNhap.goc.soCase != null"> · gom {{ banNhap.goc.soCase }} case, {{ banNhap.goc.soCaseTuModel ?? 0 }} do MODEL định tuyến (ngưỡng chưa đo)</template>
          <ul><li v-for="n in banNhap.nap.nguyenNhan" :key="n.id">{{ n.tenNguyenNhan }} — {{ n.soAssertion }} phát biểu, {{ n.trangThai }}</li></ul>
          Quy trình phiên bản {{ banNhap.nap.quyTrinh?.phienBan }} · {{ banNhap.nap.quyTrinh?.soBuoc }} bước · <b>{{ banNhap.nap.quyTrinh?.trangThai }}</b>
          · dẫn chứng nối được {{ banNhap.nap.danChung?.soLienKetMoi }} · mã mơ hồ {{ banNhap.nap.danChung?.maMoHo?.length ?? 0 }}
        </div>
      </template>
      <div v-if="denBanNhap" class="den-ban-nhap">
        <RouterLink class="nut-lien" :to="denBanNhap">Đọc · sửa · duyệt bản nháp nhóm “{{ banNhapMo?.nhom }}” →</RouterLink>
        <span class="mo">{{ banNhapMo?.nguyenNhan.length }} bản ghi tri thức + quy trình {{ banNhapMo?.quyTrinh ? `v${banNhapMo.quyTrinh.phienBan}` : '—' }}
          — đọc nội dung và dẫn chứng, sửa, rồi duyệt từng đối tượng.</span>
      </div>
    </section>

    <!-- 4 -->
    <section class="the">
      <h2>4 · <span class="luong">Luồng ngược</span> Duyệt tri thức + quy trình</h2>
      <p class="mo">
        Cách đề xuất: nút <b>Đọc · sửa · duyệt bản nháp</b> ở bước 3 — đọc nội dung + dẫn chứng, tự chọn phạm vi xem và tự tích lời khai.
      </p>
      <p class="bao chu-y">
        <b>Duyệt nhanh — CHỈ cho dữ liệu giả lập.</b> Nút dưới duyệt hết mà không cho đọc, và TỰ KHAI hộ bạn
        <code>xacNhanDuLieuThuongMai: true</code> + phạm vi <code>noi-bo</code> (AR-n). Chỉ chấp nhận được vì dữ liệu ở đây là bịa.
      </p>
      <button :disabled="!!ban || !banNhapMo" @click="duyet">{{ ban === 'duyet' ? 'Đang duyệt…' : 'Duyệt nhanh (dữ liệu giả)' }}</button>
      <table v-if="ketQuaDuyet?.dong">
        <thead><tr><th>Loại</th><th>Tên</th><th>HTTP</th><th>Trạng thái</th><th /></tr></thead>
        <tbody>
          <tr v-for="(d, i) in ketQuaDuyet.dong" :key="i"><td>{{ d.loai }}</td><td>{{ d.ten }}</td><td>{{ d.http }}</td><td>{{ d.trangThai }}</td><td class="mo">{{ d.loi }}</td></tr>
        </tbody>
      </table>
      <div v-else-if="ketQuaDuyet?.loi" class="bao loi">{{ ketQuaDuyet.loi }}</div>
    </section>

    <!-- 5 -->
    <section class="the">
      <h2>5 · <span class="luong">Đóng vòng</span> Hỏi lại nhóm “{{ nhom || '—' }}”</h2>
      <p class="mo">Ticket của nhóm vừa duyệt phải ra <b>biết trả lời</b>, kèm bước hiện tại. Xác nhận một bước xong thì bước hiện tại phải nhảy.
        Bấm <b>“n hướng dẫn ▸”</b> để đọc đủ nội dung, mức kiểm chứng và mở dẫn chứng — chỉ mở xem, không ghi gì lên app.</p>
      <button :disabled="!!ban || !nhom" @click="hoi">{{ ban === 'hoi' ? 'Đang hỏi…' : 'Hỏi lại các ticket của nhóm' }}</button>
      <div v-if="baoVong" class="bao" :class="baoVong.ok ? 'ok' : 'loi'">{{ baoVong.text }}</div>
      <div v-if="loiPhien" class="bao chu-y">{{ loiPhien }}</div>
      <table v-if="hoiLai" class="bang-hoi">
        <thead><tr><th>Ticket</th><th>Trả lời · định tuyến</th><th>Bước hiện tại</th><th>Hướng dẫn</th><th /></tr></thead>
        <tbody>
          <template v-for="d in hoiLai" :key="d.khoa">
            <tr :class="{ mo: moRong.has(d.khoa) }">
              <td><a href="#" @click.prevent="mo(d.khoa)"><code>{{ d.khoa.split('-').pop() }}</code></a> {{ d.tieuDe }}</td>
              <td>
                <span v-if="d.loiVanHanh" class="xau">lỗi vận hành · HTTP {{ d.http }}</span>
                <span v-else :class="d.bietTraLoi ? 'tot' : 'vang'">{{ d.bietTraLoi ? 'biết' : 'không biết' }}</span>
                <div v-if="d.bietTraLoi === false && !d.loiVanHanh" class="mo">{{ LY_DO_KHONG_BIET[d.lyDoKhongBiet ?? ''] ?? d.lyDoKhongBiet }}</div>
                <div><NhanNguon :nguon="d.nguon" /> <span class="mo">{{ d.nhom }}</span></div>
              </td>
              <td>
                <template v-if="d.buocHienTai"><b>{{ d.buocHienTai }}</b> · {{ d.noiDungBuoc }}</template>
                <template v-else>{{ d.daXongHet ? 'đã xong hết' : LY_DO_KHONG_CO_QUY_TRINH[d.lyDoKhongCoQuyTrinh ?? ''] ?? d.lyDoKhongCoQuyTrinh ?? '—' }}</template>
              </td>
              <td><button class="phu nho" @click="batTat(d.khoa)">{{ d.huongDan?.length ?? d.soHuongDan }} hướng dẫn {{ moRong.has(d.khoa) ? '▾' : '▸' }}</button></td>
              <td class="nut">
                <button v-if="d.buocHienTai" class="phu nho" :disabled="!!ban" @click="xacNhan(d)">Xác nhận {{ d.buocHienTai }} xong</button>
                <button v-if="d.maLuotGoiY" class="phu nho" :disabled="!!ban" title="Ghi nấc USED (lời khai) lên app — khác với đánh giá chất lượng" @click="khaiDung(d)">Khai: đã dùng gợi ý</button>
              </td>
            </tr>
            <tr v-if="moRong.has(d.khoa)" class="doc">
              <td colspan="5">
                <KetQuaHoiDoc :cong="CONG_GIA_LAP" :d="d" />
                <DanhGiaLuot v-if="luotCua[d.khoa]" :luot="luotCua[d.khoa]!" @luu="(g) => luuDanhGia(d.khoa, g)" />
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </section>
  </template>
</template>

<style scoped>
.trang-thai { font-size: 13px; margin-bottom: 10px; }
.trang-thai pre, details pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; }
.hang { display: flex; gap: 14px; align-items: flex-end; flex-wrap: wrap; margin: 8px 0; }
fieldset { border: 1px solid var(--vien); border-radius: 8px; padding: 6px 12px; font-size: 13px; }
fieldset label { margin-right: 12px; }
legend { font-size: 12px; font-weight: 600; color: var(--mo); }
.nguoi { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; margin-right: 10px; max-width: 100%; }
.nguoi input, .nguoi select { font: inherit; font-weight: 400; padding: 6px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
section button { margin-top: 8px; }
.luong { font-size: 11px; font-weight: 700; text-transform: uppercase; color: var(--lam); background: var(--lam-nhat); border-radius: 4px; padding: 2px 7px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 10px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.tot { color: var(--xanh); } .xau { color: var(--do); } .vang { color: var(--vang); }
.bang-hoi tr.mo td { border-bottom-color: transparent; background: #fafbfc; }
.bang-hoi tr.doc td { background: #fafbfc; padding: 4px 12px 12px; }
.den-ban-nhap { margin-top: 12px; display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; }
.nut-lien { display: inline-block; background: var(--chu); color: #fff; border-radius: 6px; padding: 8px 14px; font-weight: 600; text-decoration: none; }
.kq { font-weight: 700; white-space: nowrap; }
.kq.dung { color: var(--xanh); } .kq.sai { color: var(--do); } .kq.khong-biet { color: var(--vang); }
.diem { display: flex; gap: 10px; margin-top: 10px; }
.diem div { border: 1px solid var(--vien); border-radius: 8px; padding: 6px 14px; font-size: 12px; color: var(--mo); }
.diem b { display: block; font-size: 20px; color: var(--chu); }
button.nho { padding: 3px 9px; font-size: 12px; margin: 2px 4px 2px 0; }
.nut { white-space: nowrap; }
</style>
