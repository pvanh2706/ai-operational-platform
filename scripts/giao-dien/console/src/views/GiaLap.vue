<script setup lang="ts">
// Giả lập hai luồng trong console (P12-4) — thay trang /gia-lap HTML cũ. Cùng các nút /api/gl-* của ui_server, cộng
// BẢN ĐỒ LÔ: sau mỗi bước, cả lô được tô lại theo đúng sơ đồ luồng của màn dòng chảy.
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../api'
import { CONG_GIA_LAP, giaLap, type DongCham, type KetQuaDinhTuyen, type KetQuaSoan, type TicketGiaLap, type TrangThaiGiaLap } from '../apiGiaLap'
import BanDoLo from '../components/BanDoLo.vue'
import NhanNguon from '../components/NhanNguon.vue'
import XuatNapFile from '../components/XuatNapFile.vue'
import ThuKhoa from '../components/ThuKhoa.vue'
import type { KetQuaThuKhoa } from '../kieu'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()
const router = useRouter()

type CheDo = 'api' | 'file'
const KHOA_CHE_DO = 'kp-console:gia-lap-che-do'

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
    try {
      const luu = JSON.parse(localStorage.getItem(KHOA_CHE_DO) ?? 'null')
      if (luu) cheDo.value = luu
    } catch { /* tiện ích — xem stores/ketNoi.ts */ }
    if (!k.coKhoa) cheDo.value = { dinhTuyen: 'file', soanNhap: 'file' }
    await taiNhomLo()
  } catch (e) {
    loiChung.value = e instanceof Error ? e.message : String(e)
  } finally {
    ban.value = null
  }
}
watch(cheDo, (v) => { try { localStorage.setItem(KHOA_CHE_DO, JSON.stringify(v)) } catch { /* bỏ qua */ } }, { deep: true })

/** Kết quả lần thử khoá gần nhất: false = Anthropic đã TỪ CHỐI → API bị khoá cho tới khi thử lại được. */
const khoaHopLe = ref<boolean | null>(null)
const apiDung = computed(() => !!tt.value?.coKhoa && khoaHopLe.value !== false)

function sauThuKhoa(kq: KetQuaThuKhoa) {
  khoaHopLe.value = kq.hopLe
  // Khoá bị từ chối thì mọi lượt bấm API sau đó đều hỏng — chuyển về File thay vì để người dùng tốn lượt mới biết.
  if (kq.hopLe === false) cheDo.value = { dinhTuyen: 'file', soanNhap: 'file' }
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
    ketQuaSoan.value = await giaLap.soanApi(nhom.value, nguoi.value)
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

async function hoi() {
  await lam('hoi', async () => {
    const ds = await api.danhSachCase(CONG_GIA_LAP, tienTo.value ?? undefined, 200)
    const khoa = ds.filter((c) => c.nhomHieuLuc === nhom.value).map((c) => c.khoaNguon)
    const k = await giaLap.hoiLai(nhom.value, khoa.length ? khoa : khoaCuaNhom.value)
    hoiLai.value = k.ok ? k.dong : null
    if (!k.ok) loiChung.value = k.loi ?? 'Hỏi lại thất bại'
    await sauMoiBuoc()
  })
}

async function xacNhan(d: DongCham) {
  await lam(`xn-${d.khoa}`, async () => {
    const k = await giaLap.xacNhanBuoc(d.khoa, d.buocHienTai!, nguoi.value)
    const sau = k.dong?.[0]
    baoVong.value = k.ok
      ? { ok: true, text: `Đã xác nhận ${d.buocHienTai} cho ${d.khoa.split('-').pop()} → bước hiện tại giờ là ${sau?.buocHienTai ?? (sau?.daXongHet ? 'đã xong hết' : '—')}.` }
      : { ok: false, text: `HTTP ${k.http}: ${JSON.stringify(k.ketQua)}` }
    if (k.ok && hoiLai.value && sau) hoiLai.value = hoiLai.value.map((x) => (x.khoa === d.khoa ? sau : x))
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
    </div>
    <div class="hang">
      <fieldset>
        <legend>Định tuyến — luồng xuôi</legend>
        <label><input v-model="cheDo.dinhTuyen" type="radio" value="api" :disabled="!apiDung" /> API</label>
        <label><input v-model="cheDo.dinhTuyen" type="radio" value="file" /> File</label>
      </fieldset>
      <fieldset>
        <legend>Soạn nháp SOP — luồng ngược</legend>
        <label><input v-model="cheDo.soanNhap" type="radio" value="api" :disabled="!apiDung" /> API</label>
        <label><input v-model="cheDo.soanNhap" type="radio" value="file" /> File</label>
      </fieldset>
      <label class="nguoi">Người thao tác <input v-model="nguoi" /></label>
    </div>
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
    </section>

    <!-- 4 -->
    <section class="the">
      <h2>4 · <span class="luong">Luồng ngược</span> Duyệt tri thức + quy trình</h2>
      <p class="mo">Quyền xem <code>noi-bo</code> + lời khai đã soát dữ liệu thương mại (AR-n) — dữ liệu ở đây là bịa.</p>
      <button :disabled="!!ban || !banNhap?.nap" @click="duyet">{{ ban === 'duyet' ? 'Đang duyệt…' : 'Duyệt bản nháp vừa nạp' }}</button>
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
      <p class="mo">Ticket của nhóm vừa duyệt phải ra <b>biết trả lời</b>, kèm bước hiện tại. Xác nhận một bước xong thì bước hiện tại phải nhảy.</p>
      <button :disabled="!!ban || !nhom" @click="hoi">{{ ban === 'hoi' ? 'Đang hỏi…' : 'Hỏi lại các ticket của nhóm' }}</button>
      <div v-if="baoVong" class="bao" :class="baoVong.ok ? 'ok' : 'loi'">{{ baoVong.text }}</div>
      <table v-if="hoiLai">
        <thead><tr><th>Ticket</th><th>Trả lời</th><th>Bước hiện tại</th><th>Hướng dẫn</th><th /></tr></thead>
        <tbody>
          <tr v-for="d in hoiLai" :key="d.khoa">
            <td><a href="#" @click.prevent="mo(d.khoa)"><code>{{ d.khoa.split('-').pop() }}</code></a> {{ d.tieuDe }}</td>
            <td :class="d.bietTraLoi ? 'tot' : 'xau'">{{ d.bietTraLoi ? 'biết' : 'không biết' }} <span class="mo">{{ d.lyDoKhongBiet }}</span></td>
            <td>{{ d.buocHienTai ? `${d.buocHienTai} · ${d.noiDungBuoc}` : d.daXongHet ? 'đã xong hết' : d.lyDoKhongCoQuyTrinh ?? '—' }}</td>
            <td>{{ d.soHuongDan }}</td>
            <td class="nut">
              <button v-if="d.buocHienTai" class="phu nho" :disabled="!!ban" @click="xacNhan(d)">Xác nhận {{ d.buocHienTai }} xong</button>
              <button v-if="d.maLuotGoiY" class="phu nho" :disabled="!!ban" @click="khaiDung(d)">Khai: đã dùng gợi ý</button>
            </td>
          </tr>
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
.nguoi { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; margin-right: 10px; }
.nguoi input, .nguoi select { font: inherit; font-weight: 400; padding: 6px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
section button { margin-top: 8px; }
.luong { font-size: 11px; font-weight: 700; text-transform: uppercase; color: var(--lam); background: var(--lam-nhat); border-radius: 4px; padding: 2px 7px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 10px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.tot { color: var(--xanh); } .xau { color: var(--do); }
.kq { font-weight: 700; white-space: nowrap; }
.kq.dung { color: var(--xanh); } .kq.sai { color: var(--do); } .kq.khong-biet { color: var(--vang); }
.diem { display: flex; gap: 10px; margin-top: 10px; }
.diem div { border: 1px solid var(--vien); border-radius: 8px; padding: 6px 14px; font-size: 12px; color: var(--mo); }
.diem b { display: block; font-size: 20px; color: var(--chu); }
button.nho { padding: 3px 9px; font-size: 12px; margin: 2px 4px 2px 0; }
.nut { white-space: nowrap; }
</style>
