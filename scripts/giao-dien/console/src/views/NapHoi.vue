<script setup lang="ts">
// Nạp & hỏi (nhóm Q, 2026-10-22):
//   1 · nạp  — một MÃ ISSUE (Q4-1: có trong kho thì mở; chưa có thì ui_server kéo từ Jira, qua check_corpus) hoặc
//              một FILE (Q5-1: POST /documents, tài liệu đứng riêng — không sinh tri thức, S6)
//   2 · hỏi  — câu hỏi TỰ DO về đúng nguồn đã chọn (Q3-1: /internal/hoi-dap). Câu trả lời CHƯA DUYỆT, mỗi phát biểu
//              trỏ về một mẩu nguồn. Đây KHÔNG phải /hoi — /hoi chỉ trả tri thức đã duyệt (F2-1).
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api, LoiGoiApp, moTaLoi, type NguonHoi } from '../api'
import { apiUi, type KetQuaNapIssue } from '../apiUi'
import DanhGiaLuot from '../components/DanhGiaLuot.vue'
import PhienBar from '../components/PhienBar.vue'
import TraLoiHoiDap from '../components/TraLoiHoiDap.vue'
import { LY_DO_HOI_DAP } from '../doc'
import { gio } from '../gio'
import type { CaseTrongDanhSach, KetQuaHoiDap, KetQuaNapTaiLieu, PromptHoiDap, TaiLieu } from '../kieu'
import { danhGiaTrong, DO_DUNG, idMoi, TEN_KET_CUC, type DanhGia, type Luot } from '../phien'
import { useCauHinhAi } from '../stores/cauHinhAi'
import { useKetNoi } from '../stores/ketNoi'
import { usePhien } from '../stores/phien'

const ketNoi = useKetNoi()
const ai = useCauHinhAi()
const phien = usePhien()
const router = useRouter()

const loiChung = ref<string | null>(null)
const ban = ref<string | null>(null)

async function lam(ten: string, viec: () => Promise<void>) {
  ban.value = ten
  loiChung.value = null
  try {
    await viec()
  } catch (e) {
    loiChung.value = e instanceof Error ? e.message : String(e)
  } finally {
    ban.value = null
  }
}

// ------------------------------------------------------------- 1 · nạp

const tab = ref<'ma' | 'file'>('ma')
const ma = ref('')
const kqIssue = ref<(KetQuaNapIssue & { ok: boolean; loi?: string; http: number }) | null>(null)
const canJira = ref(false)
const jira = ref({ baseUrl: '', pat: '', user: '', matKhau: '' })
const baoJira = ref<{ ok: boolean; text: string } | null>(null)

async function napMa() {
  await lam('ma', async () => {
    kqIssue.value = await apiUi.napIssue(ma.value.trim(), ketNoi.cong)
    canJira.value = kqIssue.value.http === 409 && !kqIssue.value.baoCao
    if (kqIssue.value.ok && kqIssue.value.khoaNguon) {
      await taiDaNap()
      chon({ khoaNguon: kqIssue.value.khoaNguon })
    }
  })
}

async function ketNoiJira() {
  await lam('jira', async () => {
    const k = await apiUi.ketNoiJira(jira.value.baseUrl, jira.value.pat, jira.value.user, jira.value.matKhau)
    baoJira.value = k.ok ? { ok: true, text: `Đã kết nối Jira — ${k.ten}. Phiên chỉ nằm trong RAM của ui_server.` } : { ok: false, text: k.loi ?? 'Không kết nối được' }
    // Không giữ mật khẩu/PAT trên trang sau khi đã gửi đi.
    jira.value = { ...jira.value, pat: '', matKhau: '' }
  })
}

const file = ref<File | null>(null)
const subTenant = ref('khong-xac-dinh')
const nguoiNap = ref('console')
const kqFile = ref<KetQuaNapTaiLieu | null>(null)

async function napFile() {
  if (!file.value) return
  await lam('file', async () => {
    kqFile.value = await api.napTaiLieu(ketNoi.cong, file.value!, subTenant.value.trim(), nguoiNap.value.trim())
    await taiDaNap()
    if (kqFile.value.trangThaiTrichChu === 'da-trich') chon({ taiLieuId: kqFile.value.id })
  })
}

// ------------------------------------------------------------- đã nạp

const cases = ref<CaseTrongDanhSach[]>([])
const taiLieu = ref<TaiLieu[]>([])

async function taiDaNap() {
  const [c, t] = await Promise.all([api.danhSachCase(ketNoi.cong, undefined, 30), api.danhSachTaiLieu(ketNoi.cong)])
  cases.value = c
  taiLieu.value = t
}
onMounted(() => lam('tai', async () => { await Promise.all([taiDaNap(), ai.tai(ketNoi.cong)]) }))
// Đổi app = đổi ngữ cảnh: không giữ lại kết quả/lượt của app trước trên màn hình.
watch(() => ketNoi.cong, () => { nguon.value = null; kq.value = null; xemLai.value = null; luotMoiNhat.value = null; void lam('tai', async () => { await Promise.all([taiDaNap(), ai.tai(ketNoi.cong)]) }) })

// ------------------------------------------------------------- 2 · hỏi

const nguon = ref<NguonHoi | null>(null)
const cauHoi = ref('')
const nguoiHoi = ref('console')
const kq = ref<KetQuaHoiDap | null>(null)
const loiHoi = ref<{ lyDo: string | null; text: string } | null>(null)
const prompt = ref<PromptHoiDap | null>(null)
const ketQuaFile = ref('')

function chon(n: NguonHoi) {
  nguon.value = n
  kq.value = null
  loiHoi.value = null
  prompt.value = null
  xemLai.value = null
  luotMoiNhat.value = null
}

const tenNguon = computed(() => {
  const n = nguon.value
  if (!n) return null
  if (n.khoaNguon) return `case ${n.khoaNguon}`
  return `tài liệu ${taiLieu.value.find((t) => t.id === n.taiLieuId)?.tenKhaiBao ?? n.taiLieuId}`
})

const cheDo = computed(() => ai.cheDoHieuLuc.hoiDap)
const nccHoiDap = computed(() => ai.cauHinh?.hoiDap.nhaCungCap ?? 'anthropic')

// ------------------------------------------------------------- lượt trong phiên
//
// Mỗi lần bấm Hỏi / Nạp kết quả là MỘT lượt: ghi "đang chờ" TRƯỚC khi gọi host (tải lại trang giữa chừng thì lượt còn đó,
// mang kết cục đang chờ / bị gián đoạn), cập nhật khi có kết quả. Lượt mới không bao giờ đè lượt cũ, kể cả cùng câu hỏi.

const luotMoiNhat = ref<Luot | null>(null)
const xemLai = ref<Luot | null>(null)
const loiPhien = ref<string | null>(null)
const luotHoiDap = computed(() =>
  [...(phien.o(ketNoi.cong).hienTai?.luot ?? [])].filter((l) => l.loai === 'hoi-dap').reverse())

function luotDau(cheDoLuot: 'api' | 'file'): Luot {
  const n = nguon.value!
  return {
    id: idMoi(), loai: 'hoi-dap', batDau: new Date().toISOString(), ketThuc: null, thoiGianChoMs: null,
    nguoiThaoTac: nguoiHoi.value.trim() || null,
    nguon: n.khoaNguon ? { loai: 'case', khoa: n.khoaNguon, ten: tenNguon.value ?? n.khoaNguon }
      : { loai: 'tai-lieu', khoa: n.taiLieuId!, ten: tenNguon.value ?? n.taiLieuId! },
    cauHoi: cauHoi.value, cheDo: cheDoLuot, nhaCungCap: null, model: null, ketCuc: 'dang-cho', loi: null, hoiDap: null,
    lienKet: {}, kichBan: phien.o(ketNoi.cong).hienTai?.kichBan ?? null, danhGia: danhGiaTrong(),
  }
}

async function ghi(l: Luot) {
  const cong = ketNoi.cong
  loiPhien.value = await phien.ghiLuot(cong, l)
}

/** Chạy một lượt hỏi: ghi đang chờ → gọi → ghi kết cục. `goi` trả kết quả hoặc ném. */
async function chayLuot(cheDoLuot: 'api' | 'file', goi: () => Promise<KetQuaHoiDap>) {
  const l = luotDau(cheDoLuot)
  xemLai.value = null
  await ghi(l)
  const t0 = performance.now()
  loiHoi.value = null
  try {
    const k = await goi()
    kq.value = k
    Object.assign(l, {
      ketCuc: k.bietTraLoi ? 'tra-loi' : 'khong-biet', hoiDap: k,
      // Chế độ/nhà cung cấp/model theo KẾT QUẢ host trả — đường file không có model, và không gán hộ.
      cheDo: k.cheDo, nhaCungCap: k.nhaCungCap, model: k.model,
    })
  } catch (e) {
    kq.value = null
    const ct = e instanceof LoiGoiApp && typeof e.chiTiet === 'object' ? e.chiTiet : null
    loiHoi.value = { lyDo: ct?.lyDo ?? null, text: ct?.detail ?? (e instanceof Error ? e.message : String(e)) }
    Object.assign(l, {
      ketCuc: 'loi-van-hanh',
      loi: { http: e instanceof LoiGoiApp ? e.status : null, lyDo: ct?.lyDo ?? null, text: moTaLoi(e) },
      // 409 mang kèm kết quả (các mẩu đã định gửi, lý do) — giữ làm bản chụp.
      hoiDap: ct?.ketQua ?? null,
      nhaCungCap: ct?.ketQua?.nhaCungCap ?? null, model: ct?.ketQua?.model ?? null,
    })
  }
  l.ketThuc = new Date().toISOString()
  l.thoiGianChoMs = Math.round(performance.now() - t0)
  luotMoiNhat.value = { ...l }
  await ghi(l)
}

async function luuDanhGia(l: Luot, g: DanhGia) {
  const moi = { ...l, danhGia: g }
  await ghi(moi)
  if (!loiPhien.value) {
    if (luotMoiNhat.value?.id === l.id) luotMoiNhat.value = moi
    if (xemLai.value?.id === l.id) xemLai.value = moi
  }
}

/** Mở lại một lượt đã lưu — đọc BẢN CHỤP trong phiên, không gọi host, không chạy lại câu hỏi. */
function moLai(l: Luot) {
  // Chọn lại đúng nguồn của lượt đó (để khung hỏi hiện ra) — nhưng nội dung hiện ra vẫn là bản chụp của lượt.
  if (l.nguon) chon(l.nguon.loai === 'case' ? { khoaNguon: l.nguon.khoa } : { taiLieuId: l.nguon.khoa })
  xemLai.value = l
}

async function hoiApi() {
  if (!nguon.value) return
  await lam('hoi', () => chayLuot('api', () =>
    api.hoiDap(ketNoi.cong, { ...nguon.value!, cauHoi: cauHoi.value, nguoiHoi: nguoiHoi.value, nhaCungCap: nccHoiDap.value })))
}

async function xuatPrompt() {
  if (!nguon.value) return
  await lam('xuat', async () => {
    prompt.value = await api.xuatPromptHoiDap(ketNoi.cong, { ...nguon.value!, cauHoi: cauHoi.value })
  })
}

const khoiChep = computed(() => {
  const p = prompt.value
  if (!p) return ''
  return `=== HỆ THỐNG ===\n${p.systemText}\n\n=== NGƯỜI DÙNG ===\n${p.userText}\n\n` +
    `=== TRẢ VỀ ĐÚNG MỘT OBJECT JSON THEO SCHEMA NÀY, KHÔNG CHỮ NÀO KHÁC ===\n${p.schema}`
})

async function napKetQua() {
  if (!nguon.value) return
  await lam('nap', () => chayLuot('file', () =>
    api.napKetQuaHoiDap(ketNoi.cong, { ...nguon.value!, cauHoi: cauHoi.value, ketQua: ketQuaFile.value })))
}

const daChep = ref(false)
async function chep() {
  try {
    await navigator.clipboard.writeText(khoiChep.value)
    daChep.value = true
    setTimeout(() => (daChep.value = false), 1500)
  } catch { /* bôi đen chép tay */ }
}

/** Thứ đang hiện: lượt cũ mở lại (bản chụp) hoặc kết quả vừa có. */
const kqHien = computed(() => (xemLai.value ? xemLai.value.hoiDap ?? null : kq.value))
const luotHien = computed(() => xemLai.value ?? luotMoiNhat.value)


function moCase(khoa: string) {
  void router.push({ name: 'case', params: { khoa } })
}
</script>

<template>
  <PhienBar :cong="ketNoi.cong" :nguoi="nguoiHoi" />
  <div v-if="loiChung" class="bao loi">{{ loiChung }}</div>

  <section class="the">
    <h2>1 · Nạp dữ liệu vào app :{{ ketNoi.cong }}</h2>
    <div class="tab">
      <button :class="{ phu: tab !== 'ma' }" @click="tab = 'ma'">Mã issue</button>
      <button :class="{ phu: tab !== 'file' }" @click="tab = 'file'">Import file</button>
    </div>

    <template v-if="tab === 'ma'">
      <p class="mo">Mã đã có trong kho thì mở luôn. Mã Jira chưa có (dạng <code>ES-123</code>) thì kéo MỘT issue từ Jira — description +
        comment — qua cổng credential <code>check_corpus</code>, rồi gửi vào Kênh 1. Host không bao giờ gọi Jira.</p>
      <div class="hang">
        <label class="o">Mã issue <input v-model="ma" placeholder="ES-346481 hoặc jira:ES-346481" @keyup.enter="napMa" /></label>
        <button :disabled="!!ban || !ma.trim()" @click="napMa">{{ ban === 'ma' ? 'Đang nạp…' : 'Nạp' }}</button>
      </div>
      <div v-if="kqIssue" class="bao" :class="kqIssue.ok ? 'ok' : 'loi'">
        <template v-if="kqIssue.ok && kqIssue.daCo">Đã có trong kho: <code>{{ kqIssue.khoaNguon }}</code> — đã chọn để hỏi bên dưới.</template>
        <template v-else-if="kqIssue.ok">Đã kéo từ Jira: <code>{{ kqIssue.khoaNguon }}</code> · {{ kqIssue.soEvidence }} mẩu evidence.
          <span v-if="kqIssue.corpusChan"> ⚠ check_corpus báo CHẶN nhưng cờ N1-2 đang tắt cổng — mẩu có thể chứa credential.</span></template>
        <template v-else>{{ kqIssue.loi }}</template>
        <details v-if="kqIssue.baoCao"><summary>Báo cáo check_corpus</summary><pre>{{ kqIssue.baoCao }}</pre></details>
        <details v-if="kqIssue.nhatKy?.length"><summary>Nhật ký kéo</summary><pre>{{ kqIssue.nhatKy.join('\n') }}</pre></details>
      </div>
      <details class="jira" :open="canJira">
        <summary>Kết nối Jira {{ baoJira?.ok ? '— đã kết nối' : '' }}</summary>
        <p class="mo">PAT (hoặc tên đăng nhập + mật khẩu) đi tới ui_server và chỉ nằm trong RAM của nó — tắt ui_server là mất.</p>
        <div class="hang">
          <label class="o">Địa chỉ Jira <input v-model="jira.baseUrl" placeholder="https://jira.cong-ty.vn" /></label>
          <label class="o">Personal Access Token <input v-model="jira.pat" type="password" autocomplete="off" /></label>
          <span class="mo">hoặc</span>
          <label class="o">Tên đăng nhập <input v-model="jira.user" autocomplete="off" /></label>
          <label class="o">Mật khẩu <input v-model="jira.matKhau" type="password" autocomplete="off" /></label>
          <button class="phu" :disabled="!!ban || !jira.baseUrl" @click="ketNoiJira">{{ ban === 'jira' ? 'Đang nối…' : 'Kết nối' }}</button>
        </div>
        <div v-if="baoJira" class="bao" :class="baoJira.ok ? 'ok' : 'loi'">{{ baoJira.text }}</div>
      </details>
    </template>

    <template v-else>
      <p class="mo">Đọc được chữ: txt · md · csv · html · json · xml · docx. PDF và ảnh vào kho nhưng <b>chưa đọc được</b> (N4-1) — không
        hỏi được. Tài liệu đứng riêng, không gắn case, không sinh tri thức (S6).</p>
      <div class="hang">
        <label class="o">File <input type="file" @change="file = ($event.target as HTMLInputElement).files?.[0] ?? null" /></label>
        <label class="o">Mã khách sạn <input v-model="subTenant" title="AR-l: chưa biết thì khong-xac-dinh; không thuộc khách sạn nào thì khong-thuoc-khach-san" /></label>
        <label class="o">Người nạp <input v-model="nguoiNap" /></label>
        <button :disabled="!!ban || !file" @click="napFile">{{ ban === 'file' ? 'Đang nạp…' : 'Nạp file' }}</button>
      </div>
      <div v-if="kqFile" class="bao" :class="kqFile.trangThaiTrichChu === 'da-trich' ? 'ok' : 'chu-y'">
        <b>{{ kqFile.tenKhaiBao }}</b> · {{ kqFile.kichThuoc.toLocaleString('vi') }} byte
        {{ kqFile.daCoTuTruoc ? '· đã có từ trước (cùng nội dung)' : '' }} · chữ: <b>{{ kqFile.trangThaiTrichChu }}</b>
        <span v-if="kqFile.ghiChuTrichChu" class="mo"> — {{ kqFile.ghiChuTrichChu }}</span>
      </div>
    </template>
  </section>

  <section class="the">
    <h2>Đã nạp — chọn một nguồn để hỏi</h2>
    <div class="hai-cot">
      <div>
        <div class="tieu">Case (mới nạp trước)</div>
        <ul class="ds">
          <li v-for="c in cases" :key="c.khoaNguon" :class="{ chon: nguon?.khoaNguon === c.khoaNguon }">
            <a href="#" @click.prevent="chon({ khoaNguon: c.khoaNguon })"><code>{{ c.khoaNguon }}</code></a>
            <span class="mo"> {{ c.tieuDe }}</span>
          </li>
          <li v-if="!cases.length" class="mo">Chưa có case nào.</li>
        </ul>
      </div>
      <div>
        <div class="tieu">Tài liệu</div>
        <ul class="ds">
          <li v-for="t in taiLieu" :key="t.id" :class="{ chon: nguon?.taiLieuId === t.id }">
            <a v-if="t.trangThaiTrichChu === 'da-trich'" href="#" @click.prevent="chon({ taiLieuId: t.id })">{{ t.tenKhaiBao }}</a>
            <span v-else>{{ t.tenKhaiBao }}</span>
            <span class="mo"> · {{ t.trangThaiTrichChu ?? 'chưa trích' }}</span>
          </li>
          <li v-if="!taiLieu.length" class="mo">Chưa có tài liệu nào.</li>
        </ul>
      </div>
    </div>
  </section>

  <section v-if="nguon" class="the">
    <h2>2 · Hỏi về {{ tenNguon }}</h2>
    <p class="mo">
      Chế độ: <b>{{ cheDo === 'api' ? `API · ${nccHoiDap}` : 'File' }}</b> — đổi ở màn <RouterLink :to="{ name: 'cau-hinh' }">Cấu hình</RouterLink>.
      <a v-if="nguon.khoaNguon" href="#" @click.prevent="moCase(nguon.khoaNguon!)">Xem dòng chảy của case →</a>
    </p>
    <div class="bao chu-y">
      🛑 Câu trả lời ở đây <b>CHƯA ĐƯỢC NGƯỜI DUYỆT</b>: model đọc thẳng evidence / chữ tài liệu (đã qua cổng che), không đọc tri thức
      đã duyệt. Nó không vào kho tri thức. Đối chiếu từng phát biểu với mẩu nguồn nó dẫn.
    </div>
    <label class="o rong">Câu hỏi <textarea v-model="cauHoi" rows="3" placeholder="Khách báo lỗi gì, và đã thử những bước nào?" /></label>

    <template v-if="cheDo === 'api'">
      <div class="hang">
        <label class="o">Người hỏi (lời khai — lượt này tính tiền) <input v-model="nguoiHoi" /></label>
        <button :disabled="!!ban || !cauHoi.trim() || !nguoiHoi.trim()" @click="hoiApi">{{ ban === 'hoi' ? 'Đang hỏi model…' : 'Hỏi' }}</button>
      </div>
    </template>
    <template v-else>
      <button :disabled="!!ban || !cauHoi.trim()" @click="xuatPrompt">{{ ban === 'xuat' ? 'Đang xuất…' : 'a · Xuất prompt' }}</button>
      <div v-if="prompt" class="xuat">
        <div class="mo">b · Chép khối dưới cho Claude Code / Codex (hoặc một khung chat AI). Nó phải trả ĐÚNG MỘT object JSON.
          {{ prompt.soChoDaChe ? `Đã che ${prompt.soChoDaChe} chỗ.` : '' }} {{ prompt.nguonBiCat ? '⚠ Nguồn dài quá trần — chỉ phần đầu được gửi.' : '' }}</div>
        <button class="phu nho" @click="chep">{{ daChep ? 'đã chép' : 'chép toàn bộ' }}</button>
        <pre class="dai">{{ khoiChep }}</pre>
        <label class="o rong">c · Dán JSON AI trả về <span class="mo">— kèm hàng rào ``` cũng được</span>
          <textarea v-model="ketQuaFile" rows="5" placeholder='{ "bietTraLoi": true, "phatBieu": [ { "noiDung": "...", "nguon": ["N2"] } ], "ghiChu": "" }' />
        </label>
        <button :disabled="!!ban || !ketQuaFile.trim()" @click="napKetQua">{{ ban === 'nap' ? 'Đang nạp…' : 'c · Nạp kết quả' }}</button>
      </div>
    </template>

    <div v-if="xemLai" class="bao chu-y">
      Đang xem LẠI lượt lúc {{ gio(xemLai.batDau) }} — bản chụp lưu trong phiên (câu hỏi, câu trả lời, các mẩu nguồn ĐÃ gửi lúc đó).
      Không gọi lại app. <a href="#" @click.prevent="xemLai = null">Đóng</a>
      <div><b>Nguồn:</b> {{ xemLai.nguon?.ten }} · <b>Câu hỏi:</b> {{ xemLai.cauHoi }}</div>
    </div>
    <div v-if="xemLai?.loi" class="bao loi"><b>Lỗi vận hành</b> — {{ LY_DO_HOI_DAP[xemLai.loi.lyDo ?? ''] ?? xemLai.loi.lyDo ?? '' }}<pre>{{ xemLai.loi.text }}</pre></div>
    <div v-else-if="loiHoi && !xemLai" class="bao loi"><b>Lỗi vận hành — {{ LY_DO_HOI_DAP[loiHoi.lyDo ?? ''] ?? loiHoi.lyDo ?? 'Không hỏi được' }}</b>
      <span class="mo"> (không phải câu trả lời “không biết”)</span><pre>{{ loiHoi.text }}</pre></div>
    <div v-if="loiPhien" class="bao chu-y">{{ loiPhien }}</div>

    <TraLoiHoiDap v-if="kqHien" :kq="kqHien" />
    <template v-if="luotHien">
      <div class="mo">
        Lượt <code>{{ luotHien.id.slice(0, 8) }}</code> · {{ TEN_KET_CUC[luotHien.ketCuc] }} ·
        thời gian chờ đo ở giao diện {{ luotHien.thoiGianChoMs != null ? `${(luotHien.thoiGianChoMs / 1000).toFixed(1)} s` : '—' }}
        (gồm mạng + host, không phải thời gian model) · token / chi phí: chưa có dữ liệu
      </div>
      <DanhGiaLuot :luot="luotHien" @luu="(g) => luuDanhGia(luotHien!, g)" />
    </template>
  </section>

  <section class="the">
    <h2>Lượt hỏi trong phiên ({{ luotHoiDap.length }})</h2>
    <p v-if="!luotHoiDap.length" class="mo">Chưa có lượt nào trong phiên đang chọn.</p>
    <table v-else class="ds-luot">
      <thead><tr><th>Lúc</th><th>Nguồn</th><th>Câu hỏi</th><th>Kết cục</th><th>Đánh giá</th></tr></thead>
      <tbody>
        <tr v-for="l in luotHoiDap" :key="l.id" :class="{ chon: luotHien?.id === l.id }">
          <td class="mo">{{ gio(l.batDau) }}</td>
          <td>{{ l.nguon?.ten }}</td>
          <td><a href="#" @click.prevent="moLai(l)">{{ (l.cauHoi ?? '').slice(0, 80) || '—' }}</a></td>
          <td :class="l.ketCuc">{{ TEN_KET_CUC[l.ketCuc] }}{{ l.cheDo ? ` · ${l.cheDo}` : '' }}</td>
          <td class="mo">{{ l.danhGia.doDung === 'chua' ? 'chưa đánh giá' : DO_DUNG[l.danhGia.doDung] }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.tab { display: flex; gap: 8px; margin-bottom: 8px; }
.hang { display: flex; gap: 12px; align-items: flex-end; flex-wrap: wrap; margin: 8px 0; }
.o { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; min-width: 0; }
.o input, .o textarea { font: inherit; font-weight: 400; padding: 6px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
.o.rong { display: flex; margin-top: 10px; }
.o textarea { font: 13px ui-monospace, Consolas, monospace; }
.jira { margin-top: 12px; font-size: 13px; }
.hai-cot { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
.tieu { font-size: 12px; font-weight: 700; color: var(--mo); text-transform: uppercase; }
.ds { list-style: none; padding: 0; margin: 6px 0 0; max-height: 280px; overflow: auto; font-size: 13px; }
.ds li { padding: 4px 6px; border-radius: 4px; }
.ds li.chon { background: var(--lam-nhat); }
pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; word-break: break-word; }
pre.dai { max-height: 320px; overflow: auto; }
.bao pre { background: transparent; color: inherit; padding: 0; }
.xuat { margin-top: 10px; }
section button { margin-top: 8px; }
button.nho { padding: 2px 8px; font-size: 11px; }
.ds-luot { width: 100%; border-collapse: collapse; font-size: 13px; }
.ds-luot th, .ds-luot td { text-align: left; padding: 5px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
.ds-luot th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.ds-luot tr.chon td { background: var(--lam-nhat); }
.ds-luot .loi-van-hanh { color: var(--do); } .ds-luot .khong-biet { color: var(--vang); } .ds-luot .dang-cho { color: var(--mo); }
</style>
