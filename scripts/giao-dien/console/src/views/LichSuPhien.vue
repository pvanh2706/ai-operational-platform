<script setup lang="ts">
// Lịch sử + tổng kết + so sánh MỘT phiên trải nghiệm. Mọi thứ ở đây đọc từ ui_server (bản chụp đã lưu): mở trang, xem lượt,
// tổng kết, so sánh KHÔNG gọi app, không gọi model, không ghi nhận USED. Chỉ "Lưu đánh giá" / "Đổi tên" / "Xoá" mới ghi —
// và chỉ ghi vào file phiên.
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import DanhGiaLuot from '../components/DanhGiaLuot.vue'
import KetQuaHoiDoc from '../components/KetQuaHoiDoc.vue'
import TraLoiHoiDap from '../components/TraLoiHoiDap.vue'
import { CONG_GIA_LAP } from '../apiGiaLap'
import { gio } from '../gio'
import {
  canXemLai, daDanhGia, DAN_CHUNG, DO_DUNG, khacBiet, TEN_KET_CUC, thongKe, THUC_HIEN, tiLe, xuatMarkdown,
  type DanhGia, type Luot,
} from '../phien'
import { CAC_APP, useKetNoi } from '../stores/ketNoi'
import { usePhien } from '../stores/phien'

const route = useRoute()
const ketNoi = useKetNoi()
const phien = usePhien()

const cong = ref<number>(Number(route.query.cong) || ketNoi.cong)

const s = computed(() => phien.o(cong.value))
const p = computed(() => s.value.hienTai)
const tk = computed(() => (p.value ? thongKe(p.value.luot) : null))
const loi = ref<string | null>(null)

async function chonPhien(e: Event) {
  loi.value = null
  chonId.value = null
  soSanh.value = []
  try {
    await phien.chon(cong.value, (e.target as HTMLSelectElement).value || null)
  } catch (x) {
    loi.value = x instanceof Error ? x.message : String(x)
  }
}

// ------------------------------------------------------------------ lượt

type Loc = 'tat-ca' | 'can-xem-lai' | 'chua-danh-gia' | 'loi-van-hanh'
const loc = ref<Loc>('tat-ca')
const dsLuot = computed(() => {
  const ds = p.value?.luot ?? []
  if (loc.value === 'can-xem-lai') return ds.filter((l) => canXemLai(l).length)
  if (loc.value === 'chua-danh-gia') return ds.filter((l) => !daDanhGia(l.danhGia))
  if (loc.value === 'loi-van-hanh') return ds.filter((l) => l.ketCuc === 'loi-van-hanh' || l.ketCuc === 'dang-cho')
  return ds
})
const chonId = ref<string | null>(null)
const luotChon = computed(() => p.value?.luot.find((l) => l.id === chonId.value) ?? null)
function mo(id: string) {
  chonId.value = id
}

async function luuDanhGia(l: Luot, g: DanhGia) {
  loi.value = await phien.ghiLuot(cong.value, { ...l, danhGia: g })
}

const soSanh = ref<string[]>([])
function batSoSanh(id: string) {
  soSanh.value = soSanh.value.includes(id) ? soSanh.value.filter((x) => x !== id) : [...soSanh.value, id].slice(-2)
}
const cap = computed(() => {
  const [a, b] = soSanh.value.map((id) => p.value?.luot.find((l) => l.id === id)).filter(Boolean) as Luot[]
  return a && b ? { a, b, khac: khacBiet(a, b) } : null
})

// Đổi ngữ cảnh: bỏ lượt đang chọn + cặp so sánh của ngữ cảnh trước, đọc lại danh sách của ngữ cảnh mới.
// (Đặt SAU chonId/soSanh: immediate chạy ngay trong setup.)
watch(cong, (c) => {
  chonId.value = null
  soSanh.value = []
  void phien.tai(c, true)
}, { immediate: true })

function tomTat(l: Luot): string {
  if (l.hoiDap) return l.hoiDap.bietTraLoi ? l.hoiDap.phatBieu.map((x) => x.noiDung).join(' · ') : `không biết — ${l.hoiDap.lyDoKhongBiet ?? ''}`
  if (l.giaLap) return l.giaLap.bietTraLoi ? `${l.giaLap.huongDan?.length ?? l.giaLap.soHuongDan} hướng dẫn · bước ${l.giaLap.buocHienTai ?? '—'}` : `không biết — ${l.giaLap.lyDoKhongBiet ?? ''}`
  if (l.banNhap) return `${l.banNhap.thaoTac.length} thao tác`
  return l.loi?.text ?? '—'
}

// ------------------------------------------------------------------ phiên: đổi tên, xoá, xuất

const tenMoi = ref('')
watch(p, (x) => { tenMoi.value = x?.ten ?? '' }, { immediate: true })
async function doiTen() {
  if (!p.value) return
  try {
    await phien.sua(cong.value, p.value.id, { ten: tenMoi.value.trim() })
    await phien.tai(cong.value)
  } catch (x) {
    loi.value = x instanceof Error ? x.message : String(x)
  }
}
async function xoaPhien() {
  if (!p.value || !window.confirm(`Xoá hẳn phiên “${p.value.ten}” (${p.value.luot.length} lượt) khỏi ui_server? Không hoàn tác được.`)) return
  try {
    await phien.xoa(cong.value, p.value.id)
  } catch (x) {
    loi.value = x instanceof Error ? x.message : String(x)
  }
}

function taiXuong(ten: string, kieu: string, noiDung: string) {
  const url = URL.createObjectURL(new Blob([noiDung], { type: kieu }))
  const a = document.createElement('a')
  a.href = url
  a.download = ten
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
const tenFile = () => `phien-${p.value!.taoLuc.slice(0, 10)}-${p.value!.id.slice(0, 8)}`
function xuatJson() {
  if (!p.value) return
  taiXuong(`${tenFile()}.json`, 'application/json', JSON.stringify({ ...p.value, xuatLuc: new Date().toISOString() }, null, 2))
}
function xuatMd() {
  if (!p.value) return
  taiXuong(`${tenFile()}.md`, 'text/markdown', xuatMarkdown(p.value, new Date().toISOString()))
}

const nhanDG = (l: Luot) =>
  daDanhGia(l.danhGia) ? `${DO_DUNG[l.danhGia.doDung]} · ${DAN_CHUNG[l.danhGia.danChung]} · ${THUC_HIEN[l.danhGia.thucHienDuoc]}` : 'chưa đánh giá'
</script>

<template>
  <section class="the">
    <h2>Phiên trải nghiệm — lịch sử &amp; tổng kết</h2>
    <div class="hang">
      <label class="o">Ngữ cảnh (app)
        <select v-model.number="cong"><option v-for="a in CAC_APP" :key="a.cong" :value="a.cong">{{ a.ten }} · :{{ a.cong }}</option></select>
      </label>
      <label class="o">Phiên
        <select :value="p?.id ?? ''" @change="chonPhien">
          <option value="">— chọn phiên —</option>
          <option v-for="x in s.ds" :key="x.id" :value="x.id">{{ x.ten }} · {{ x.soLuot }} lượt · {{ gio(x.capNhatLuc) }}</option>
        </select>
      </label>
    </div>
    <p class="mo">
      Ngữ cảnh <code>{{ s.nguCanh?.khoa ?? '…' }}</code> — chỉ hiện phiên của app + tenant này. Lưu ở ui_server
      (<code>scripts/giao-dien/phien-trai-nghiem/</code>), git bỏ qua. Trang này chỉ ĐỌC bản đã lưu: không gọi app, không gọi model.
    </p>
    <p v-if="!s.ds.length" class="mo">Chưa có phiên nào trong ngữ cảnh này. Hỏi ở <RouterLink :to="{ name: 'nap-hoi' }">Nạp &amp; hỏi</RouterLink>
      hoặc <RouterLink :to="{ name: 'gia-lap' }">Giả lập</RouterLink> — lượt đầu tiên tự mở một phiên. Hoặc theo một
      <RouterLink :to="{ name: 'kich-ban' }">kịch bản</RouterLink>.</p>
    <p v-if="s.ds.length && !p" class="mo">Có {{ s.ds.length }} phiên — chọn một phiên ở ô “Phiên” để xem lịch sử và tổng kết.</p>
    <div v-if="loi || s.loi" class="bao loi">{{ loi ?? s.loi }}</div>

    <div v-if="p" class="hang">
      <label class="o rong">Tên phiên <input v-model="tenMoi" /></label>
      <button class="phu" :disabled="tenMoi.trim() === p.ten" @click="doiTen">Đổi tên</button>
      <button class="phu" @click="xuatJson">Xuất JSON</button>
      <button class="phu" @click="xuatMd">Xuất Markdown</button>
      <button class="phu xoa" @click="xoaPhien">Xoá phiên</button>
    </div>
    <p v-if="p" class="mo">
      Tạo {{ gio(p.taoLuc) }} · cập nhật {{ gio(p.capNhatLuc) }} · người thao tác (lời khai) {{ p.nguoiThaoTac ?? '—' }}
      <template v-if="p.kichBan"> · kịch bản <b>{{ p.kichBan }}</b></template> · định dạng <code>{{ p.dinhDang }}</code>
    </p>
  </section>

  <template v-if="p && tk">
    <section class="the">
      <h2>Tổng kết</h2>
      <div class="o-so">
        <div><b>{{ tk.tong }}</b>tổng lượt</div>
        <div><b>{{ tk.thanhCongVanHanh }}</b>chạy được<small>{{ tk.ketCuc['tra-loi'] }} trả lời · {{ tk.ketCuc['khong-biet'] }} không biết</small></div>
        <div class="do"><b>{{ tk.ketCuc['loi-van-hanh'] }}</b>lỗi vận hành</div>
        <div><b>{{ tk.ketCuc['dang-cho'] }}</b>đang chờ / gián đoạn</div>
        <div><b>{{ tk.ketCuc['thao-tac'] }}</b>lượt bản nháp</div>
        <div><b>{{ tk.daDanhGia }}/{{ tk.tong }}</b>đã đánh giá<small>{{ tk.chuaDanhGia }} chưa</small></div>
      </div>
      <p class="mo">“Chạy được” là thành công VỀ VẬN HÀNH — chưa nói gì về chất lượng. “Không biết” không mặc nhiên là sai.</p>

      <h3>Đánh giá của người thử</h3>
      <div class="phan-bo">
        <div v-for="pb in tk.phanBo" :key="pb.ten" class="mot-chieu">
          <div class="tieu">{{ pb.ten }} <span class="mo">· mẫu số {{ pb.mauSo }} · chưa đánh giá {{ pb.chua }}</span></div>
          <p v-if="!pb.mauSo" class="mo">chưa có đánh giá</p>
          <div v-for="d in pb.dem" v-else :key="d.gia" class="thanh">
            <span class="nhan">{{ d.ten }}</span>
            <span class="vach"><span :style="{ width: `${(d.so / pb.mauSo) * 100}%` }" /></span>
            <span class="so">{{ tiLe(d.so, pb.mauSo) }}</span>
          </div>
        </div>
      </div>

      <h3>Chấm định tuyến trên đáp án giả <span class="mo">— riêng, không phải chất lượng hướng dẫn / SOP</span></h3>
      <p v-if="!tk.dinhTuyen.mauSo" class="mo">Không có lượt giả lập nào chạy được trong phiên này.</p>
      <p v-else>Đúng nhóm {{ tiLe(tk.dinhTuyen.dung, tk.dinhTuyen.mauSo) }} · sai nhóm {{ tk.dinhTuyen.sai }} · không biết {{ tk.dinhTuyen.khongBiet }}
        <span class="mo">— đáp án BỊA, kiểm luồng xử lý; chưa chứng minh độ chính xác trên dữ liệu khách thật.</span></p>

      <h3>Cần mở lại ({{ tk.canXemLai.length }})</h3>
      <p v-if="!tk.canXemLai.length" class="mo">Chưa lượt nào bị đánh dấu sai / thiếu dẫn chứng / cần bổ sung.</p>
      <ul v-else>
        <li v-for="x in tk.canXemLai" :key="x.luot.id"><a href="#" @click.prevent="mo(x.luot.id)">{{ x.luot.nguon?.ten ?? x.luot.id }}</a>
          <span class="mo"> — {{ x.lyDo.join(', ') }}</span></li>
      </ul>

      <h3>Bản nháp ({{ tk.banNhap.length }})</h3>
      <p v-if="!tk.banNhap.length" class="mo">Phiên này chưa sửa / duyệt bản nháp nào.</p>
      <ul v-else>
        <li v-for="x in tk.banNhap" :key="x.luot.id">
          <a href="#" @click.prevent="mo(x.luot.id)">{{ x.luot.nguon?.ten }}</a> — {{ x.soSua }} lần lưu sửa ·
          {{ x.duyetOk }} lần duyệt được{{ x.duyetHong ? ` · ${x.duyetHong} lần duyệt HỎNG` : '' }}
          <span class="mo">{{ x.doiTuongDaDuyet.length ? `· đã duyệt: ${x.doiTuongDaDuyet.join(', ')}` : '· chưa duyệt gì' }}</span>
        </li>
      </ul>
      <p class="mo">“Không sửa” không phải bằng chứng đã rà kỹ. Không suy ra tiết kiệm thời gian, giảm escalation hay ROI từ những con số này.</p>
    </section>

    <section class="the">
      <h2>Lượt ({{ p.luot.length }})</h2>
      <div class="hang">
        <label v-for="(t, k) in { 'tat-ca': 'Tất cả', 'can-xem-lai': 'Cần mở lại', 'chua-danh-gia': 'Chưa đánh giá', 'loi-van-hanh': 'Lỗi / gián đoạn' }" :key="k" class="loc">
          <input v-model="loc" type="radio" :value="k" /> {{ t }}
        </label>
        <span class="mo">Tích hai ô “so sánh” để đặt hai lượt cạnh nhau.</span>
      </div>
      <table class="ds">
        <thead><tr><th>So sánh</th><th>Lúc</th><th>Loại · nguồn</th><th>Kết cục</th><th>Tóm tắt</th><th>Đánh giá</th></tr></thead>
        <tbody>
          <tr v-for="l in dsLuot" :key="l.id" :class="{ chon: chonId === l.id }">
            <td><input type="checkbox" :checked="soSanh.includes(l.id)" @change="batSoSanh(l.id)" /></td>
            <td class="mo">{{ gio(l.batDau) }}</td>
            <td><a href="#" @click.prevent="mo(l.id)">{{ l.loai }} · {{ l.nguon?.ten ?? '—' }}</a></td>
            <td :class="l.ketCuc">{{ TEN_KET_CUC[l.ketCuc] }}</td>
            <td class="tom">{{ tomTat(l).slice(0, 140) }}</td>
            <td class="mo">{{ nhanDG(l) }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <section v-if="luotChon" class="the">
      <h2>Lượt <code>{{ luotChon.id.slice(0, 8) }}</code> · {{ TEN_KET_CUC[luotChon.ketCuc] }}</h2>
      <div class="mo">
        {{ luotChon.loai }} · {{ gio(luotChon.batDau) }} → {{ gio(luotChon.ketThuc) }} ·
        thời gian chờ đo ở giao diện {{ luotChon.thoiGianChoMs != null ? `${(luotChon.thoiGianChoMs / 1000).toFixed(1)} s` : '—' }}{{ luotChon.thoiGianCaLo ? ` (cho cả lô ${luotChon.thoiGianCaLo} ticket)` : '' }}
        · người thao tác (lời khai) {{ luotChon.nguoiThaoTac ?? '—' }}
      </div>
      <div class="mo">
        Chế độ {{ luotChon.cheDo ?? 'không rõ' }} · nhà cung cấp {{ luotChon.nhaCungCap ?? 'không rõ' }} · model {{ luotChon.model ?? 'không rõ' }}
        · token / chi phí: chưa có dữ liệu
        <template v-if="luotChon.kichBan"> · kịch bản {{ luotChon.kichBan }}</template>
      </div>
      <div v-if="luotChon.nguon" class="mo">Nguồn: {{ luotChon.nguon.loai }} <code>{{ luotChon.nguon.khoa }}</code> — {{ luotChon.nguon.ten }}</div>
      <div v-if="luotChon.cauHoi"><b>Câu hỏi:</b> {{ luotChon.cauHoi }}</div>
      <div class="mo">
        <template v-if="luotChon.lienKet.maLuotGoiY">mã lượt gợi ý <code>{{ luotChon.lienKet.maLuotGoiY }}</code> · </template>
        <template v-if="luotChon.lienKet.truocDo">sau lượt <a href="#" @click.prevent="mo(luotChon.lienKet.truocDo!)"><code>{{ luotChon.lienKet.truocDo.slice(0, 8) }}</code></a> · </template>
        <RouterLink v-if="luotChon.lienKet.triThuc?.length || luotChon.lienKet.quyTrinh"
                    :to="{ name: 'ban-nhap', query: { cong: String(cong), tri: (luotChon.lienKet.triThuc ?? []).join(','), qt: luotChon.lienKet.quyTrinh ?? undefined } }">
          mở tri thức / quy trình HIỆN TẠI →</RouterLink>
      </div>
      <div v-if="luotChon.loi" class="bao loi"><b>Lỗi vận hành</b> · HTTP {{ luotChon.loi.http ?? '—' }} {{ luotChon.loi.lyDo ?? '' }}<pre>{{ luotChon.loi.text }}</pre></div>

      <template v-if="luotChon.loai === 'hoi-dap'">
        <TraLoiHoiDap v-if="luotChon.hoiDap" :kq="luotChon.hoiDap" />
        <p v-else class="mo">Lượt này không có bản chụp câu trả lời.</p>
      </template>
      <template v-else-if="luotChon.loai === 'hoi-gia-lap'">
        <p class="bao chu-y">Bản chụp giữ nội dung hướng dẫn + KHOÁ dẫn chứng. Bấm một dẫn chứng là đọc mẩu BẢN HIỆN TẠI từ app :{{ CONG_GIA_LAP }},
          không phải bản lúc hỏi.</p>
        <KetQuaHoiDoc v-if="luotChon.giaLap" :cong="CONG_GIA_LAP" :d="luotChon.giaLap" />
      </template>
      <template v-else-if="luotChon.banNhap">
        <table class="ds">
          <thead><tr><th>Lúc</th><th>Việc</th><th>Đối tượng</th><th>Kết quả</th><th>Trạng thái sau</th></tr></thead>
          <tbody>
            <tr v-for="(t, i) in luotChon.banNhap.thaoTac" :key="i" :class="{ xau: !t.ok }">
              <td class="mo">{{ gio(t.luc) }}</td><td>{{ t.viec }}</td><td>{{ t.ten }}</td>
              <td>{{ t.ok ? 'xong' : `HỎNG · HTTP ${t.http ?? '—'}` }}<pre v-if="t.loi">{{ t.loi }}</pre>
                <details v-if="t.banTruoc"><summary>bản bước trước khi thay</summary><pre>{{ JSON.stringify(t.banTruoc, null, 2) }}</pre></details></td>
              <td>{{ t.trangThaiSau ?? '—' }}</td>
            </tr>
          </tbody>
        </table>
        <p v-if="!luotChon.banNhap.thaoTac.length" class="mo">Chưa có thao tác nào — chỉ có đánh giá.</p>
      </template>
      <DanhGiaLuot :luot="luotChon" @luu="(g) => luuDanhGia(luotChon!, g)" />
    </section>

    <section v-if="cap" class="the">
      <h2>So sánh hai lượt</h2>
      <div v-if="cap.khac.length" class="bao chu-y">
        <b>Đầu vào KHÁC nhau — đây không phải phép so sánh công bằng.</b>
        <table class="ds">
          <thead><tr><th>Khác ở</th><th>Lượt A</th><th>Lượt B</th></tr></thead>
          <tbody><tr v-for="k in cap.khac" :key="k.truong"><td>{{ k.truong }}</td><td>{{ k.a }}</td><td>{{ k.b }}</td></tr></tbody>
        </table>
      </div>
      <p v-else class="mo">Cùng loại lượt, câu hỏi, nguồn, chế độ và model đã biết. (Phiên bản nội dung nguồn có thể vẫn khác — bản chụp không giữ phiên bản mẩu.)</p>
      <div class="hai-cot">
        <div v-for="(l, i) in [cap.a, cap.b]" :key="l.id">
          <div class="tieu">{{ i === 0 ? 'A' : 'B' }} · {{ gio(l.batDau) }} · {{ TEN_KET_CUC[l.ketCuc] }}</div>
          <div class="mo">{{ nhanDG(l) }}</div>
          <TraLoiHoiDap v-if="l.hoiDap" :kq="l.hoiDap" />
          <KetQuaHoiDoc v-else-if="l.giaLap" :cong="CONG_GIA_LAP" :d="l.giaLap" />
          <p v-else class="mo">{{ tomTat(l) }}</p>
          <div v-if="l.danhGia.nhanXet" class="mo">Nhận xét: {{ l.danhGia.nhanXet }}</div>
        </div>
      </div>
    </section>
  </template>
</template>

<style scoped>
.hang { display: flex; flex-wrap: wrap; gap: 10px 14px; align-items: flex-end; margin: 8px 0; }
.o { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; min-width: 0; }
.o.rong { flex: 1 1 260px; }
input, select { font: inherit; font-weight: 400; padding: 5px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
button.xoa { color: var(--do); }
h3 { font-size: 13px; margin: 16px 0 6px; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
.o-so { display: flex; flex-wrap: wrap; gap: 10px; }
.o-so div { border: 1px solid var(--vien); border-radius: 8px; padding: 6px 14px; font-size: 12px; color: var(--mo); min-width: 110px; }
.o-so b { display: block; font-size: 20px; color: var(--chu); }
.o-so small { display: block; }
.o-so .do b { color: var(--do); }
.phan-bo { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 14px; }
.tieu { font-size: 12px; font-weight: 700; }
.thanh { display: grid; grid-template-columns: 110px 1fr auto; gap: 8px; align-items: center; font-size: 12px; margin: 3px 0; }
.vach { height: 8px; background: #f0f2f5; border-radius: 4px; overflow: hidden; }
.vach span { display: block; height: 100%; background: var(--lam); }
.so { font-variant-numeric: tabular-nums; }
.loc { font-size: 13px; }
table.ds { width: 100%; border-collapse: collapse; font-size: 13px; }
.ds th, .ds td { text-align: left; padding: 5px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
.ds th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.ds tr.chon td { background: var(--lam-nhat); }
.ds tr.xau td { color: var(--do); }
.ds .loi-van-hanh { color: var(--do); } .ds .khong-biet { color: var(--vang); } .ds .dang-cho { color: var(--mo); }
.tom { max-width: 360px; word-break: break-word; }
.hai-cot { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; margin-top: 10px; }
pre { white-space: pre-wrap; word-break: break-word; font-size: 12px; margin: 4px 0; }
@media (max-width: 640px) { .ds { display: block; overflow-x: auto; } }
</style>
