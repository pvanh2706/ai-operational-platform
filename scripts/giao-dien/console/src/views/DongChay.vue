<script setup lang="ts">
// Dòng chảy MỘT case: sơ đồ luồng tô theo case → dòng thời gian gộp → chi tiết từng phần.
// Mọi thứ ở đây đến từ MỘT lượt GET không tác dụng phụ (IM-51): mở trang này không gọi model, không ghi gì.
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { api, LoiGoiApp } from '../api'
import NhanNguon from '../components/NhanNguon.vue'
import SoDoLuong from '../components/SoDoLuong.vue'
import { gio } from '../gio'
import type { DongChayCase } from '../kieu'
import { dongThoiGian, tinhSoDo } from '../soDo'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()
const route = useRoute()
const khoa = computed(() => String(route.params.khoa ?? ''))

const dc = ref<DongChayCase | null>(null)
const loi = ref<string | null>(null)
const dangTai = ref(false)

async function tai() {
  dangTai.value = true
  loi.value = null
  try {
    dc.value = await api.dongChay(ketNoi.cong, khoa.value)
  } catch (e) {
    dc.value = null
    loi.value = e instanceof LoiGoiApp && e.status === 404
      ? `Không có case "${khoa.value}" trong ${ketNoi.app.ten}. Đúng app chưa? (chọn ở góc phải)`
      : e instanceof Error ? e.message : String(e)
  } finally {
    dangTai.value = false
  }
}
watch([khoa, () => ketNoi.cong], () => void tai(), { immediate: true })

const soDo = computed(() => (dc.value ? tinhSoDo(dc.value) : null))
const moc = computed(() => (dc.value ? dongThoiGian(dc.value) : []))

const NAC = ['RETRIEVED', 'SHOWN', 'OPENED', 'ACCEPTED', 'USED'] as const

async function toiMuc(muc: string) {
  await nextTick()
  document.getElementById(`muc-${muc}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
</script>

<template>
  <p><RouterLink :to="{ name: 'danh-sach', query: route.query }">← Danh sách case</RouterLink></p>
  <div v-if="loi" class="bao loi">{{ loi }}</div>
  <div v-else-if="!dc" class="mo">Đang tải…</div>

  <template v-else>
    <section id="muc-dau" class="the">
      <h2><code>{{ dc.khoaNguon }}</code></h2>
      <div class="dau-case">
        <div><b>{{ dc.tieuDe }}</b></div>
        <div class="mo">Tạo ở nguồn {{ gio(dc.taoONguonLuc) }} · nạp {{ gio(dc.napLuc) }} · {{ dc.xongONguonLuc ? `xong ở nguồn ${gio(dc.xongONguonLuc)}` : 'chưa xong ở nguồn' }}</div>
      </div>
      <button class="phu" :disabled="dangTai" @click="tai">{{ dangTai ? 'Đang tải…' : 'Tải lại' }}</button>
    </section>

    <section class="the">
      <h2>Sơ đồ luồng của case này</h2>
      <SoDoLuong v-if="soDo" :o="soDo.o" :canh="soDo.canh" @chon="toiMuc" />
    </section>

    <section class="the">
      <h2>Dòng thời gian</h2>
      <ol class="dong-tg">
        <li v-for="(m, i) in moc" :key="i" :class="[m.loai, { xau: m.xau }]">
          <span class="luc mo">{{ gio(m.luc) }}</span>
          <span class="loai">{{ m.loai }}</span>
          <span>
            <b>{{ m.tieuDe }}</b>
            <span v-if="m.chiTiet" class="mo"> — {{ m.chiTiet }}</span>
          </span>
        </li>
      </ol>
    </section>

    <section id="muc-dinh-tuyen" class="the">
      <h2>Định tuyến nhóm</h2>
      <p>
        Hiệu lực: <b>{{ dc.dinhTuyen.nhom ?? 'KHÔNG BIẾT' }}</b> <NhanNguon :nguon="dc.dinhTuyen.nguon" /><br />
        <span class="mo">{{ dc.dinhTuyen.giaiThich }}</span>
      </p>
      <p class="mo">Thứ tự tra: người nạp → bảng đo → kết quả model đã lưu → (chỉ /hoi và webhook) gọi model. Trang này không bao giờ gọi model.</p>
      <h3>Người nạp</h3>
      <p v-if="!dc.dinhTuyen.nguoiNap" class="mo">Chưa ai nạp kết quả cho case này.</p>
      <p v-else>
        <b>{{ dc.dinhTuyen.nguoiNap.nhom }}</b> · {{ dc.dinhTuyen.nguoiNap.nguon }} · {{ dc.dinhTuyen.nguoiNap.nguoiNap }}, {{ gio(dc.dinhTuyen.nguoiNap.napLuc) }}<br />
        <span class="mo">{{ dc.dinhTuyen.nguoiNap.giaiThich }}</span>
      </p>
      <h3>Biên lai model ({{ dc.dinhTuyen.bienLaiModel.length }})</h3>
      <p v-if="!dc.dinhTuyen.bienLaiModel.length" class="mo">Model chưa từng được gọi cho case này.</p>
      <table v-else>
        <thead><tr><th>Lúc</th><th>Kết quả</th><th>Nhóm</th><th class="so">Độ tin</th><th>Giải thích / lỗi</th></tr></thead>
        <tbody>
          <tr v-for="(b, i) in dc.dinhTuyen.bienLaiModel" :key="i" :class="{ xau: !b.thanhCong }">
            <td class="mo">{{ gio(b.luc) }}</td>
            <td>{{ b.thanhCong ? 'xong' : 'HỎNG' }}</td>
            <td>{{ b.nhom ?? '—' }}</td>
            <td class="so">{{ b.doTinCay?.toFixed(2) ?? '—' }}</td>
            <td class="mo">{{ b.loi ?? b.giaiThich }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <section id="muc-quy-trinh" class="the">
      <h2>Quy trình</h2>
      <p v-if="!dc.quyTrinh.tienDo.coQuyTrinh">
        <b>Không khớp quy trình nào</b> — <code>{{ dc.quyTrinh.tienDo.lyDoKhongCo }}</code><br />
        <span class="mo">{{ dc.quyTrinh.tienDo.giaiThich }}</span>
      </p>
      <template v-else-if="dc.quyTrinh.tienDo.quyTrinh">
        <p>
          <b>{{ dc.quyTrinh.tienDo.quyTrinh.ten }}</b> · phiên bản {{ dc.quyTrinh.tienDo.quyTrinh.phienBan }} ·
          {{ dc.quyTrinh.tienDo.quyTrinh.trangThai }} · {{ dc.quyTrinh.tienDo.dangChay ? 'đang có lượt chạy' : 'chưa có lượt chạy (chỉ xem sẽ khớp)' }}
        </p>
        <ol class="buoc">
          <li v-for="b in dc.quyTrinh.tienDo.buoc" :key="b.id" :class="{ xong: b.daXong, hien: dc.quyTrinh.tienDo.buocHienTai?.id === b.id }">
            <b>{{ b.ma }}</b> {{ b.noiDung }}
            <span v-if="dc.quyTrinh.tienDo.buocHienTai?.id === b.id" class="nhan-hien">bước hiện tại</span>
            <div v-for="(c, i) in b.canCu" :key="i" class="mo can-cu">✓ {{ c.cach }} — <code>{{ c.khoaNguonMau }}</code></div>
          </li>
        </ol>
        <p v-if="dc.quyTrinh.tienDo.daXongHet" class="bao ok">Đã xong hết các bước.</p>
      </template>
      <h3>Lượt chạy ({{ dc.quyTrinh.luotChay.length }})</h3>
      <p v-if="!dc.quyTrinh.luotChay.length" class="mo">Chưa có lượt chạy nào — lượt chạy chỉ được tạo khi có người hỏi hoặc xác nhận bước.</p>
      <ul v-else>
        <li v-for="r in dc.quyTrinh.luotChay" :key="r.id">
          {{ r.ten }} v{{ r.phienBan }} ({{ r.trangThaiQuyTrinh }}) · bắt đầu {{ gio(r.batDau) }}
          <span v-if="r.boLuc" class="xau"> · bị bỏ {{ gio(r.boLuc) }} bởi {{ r.boBoi }}: {{ r.lyDoBo }}</span>
        </li>
      </ul>
    </section>

    <section id="muc-goi-y" class="the">
      <h2>Gợi ý — ô G đến J ({{ dc.goiY.length }} lượt)</h2>
      <p v-if="!dc.goiY.length" class="mo">Chưa ai hỏi case này (/hoi) và chưa có webhook nào được gửi.</p>
      <div v-for="g in dc.goiY" :key="g.id" class="luot">
        <div>
          <b>{{ gio(g.luc) }}</b> · qua <code>{{ g.kenhRa }}</code> ·
          <span :class="g.bietTraLoi ? 'tot' : 'xau'">{{ g.bietTraLoi ? 'biết trả lời' : 'KHÔNG BIẾT' }}</span>
          <template v-if="g.maBuoc"> · bước {{ g.maBuoc }}</template>
          · {{ g.soTriThuc }} bản ghi tri thức · nhóm {{ g.nhom ?? '—' }} <NhanNguon :nguon="g.nguonDinhTuyen" />
        </div>
        <div v-if="g.lyDoKhongBiet" class="mo">lý do: {{ g.lyDoKhongBiet }}</div>
        <div class="pheu">
          <span v-for="n in NAC" :key="n" class="nac"
                :class="g.suKien.some((s) => s.nac === n && s.datDuoc) ? 'dat' : g.suKien.some((s) => s.nac === n) ? 'khong-dat' : 'chua'">
            {{ n }}
          </span>
        </div>
        <div v-for="(s, i) in g.suKien.filter((x) => x.nguoiKhai)" :key="i" class="mo">
          lời khai {{ s.nac }} của {{ s.nguoiKhai }}{{ s.ghiChu ? `: ${s.ghiChu}` : '' }}{{ s.nhomDung ? ` · nhóm đúng: ${s.nhomDung}` : '' }}
        </div>
      </div>
      <p class="mo">SHOWN = hệ thống đã GIAO, không phải người đã đọc. ACCEPTED / USED là LỜI KHAI.</p>
      <h3>Gửi kết quả về ({{ dc.guiVe.length }})</h3>
      <p v-if="!dc.guiVe.length" class="mo">Không có lượt gửi về — khách chưa khai webhook, hoặc case không mới.</p>
      <ul v-else>
        <li v-for="w in dc.guiVe" :key="w.id" :class="{ xau: !!w.loiCuoi && !w.guiLuc }">
          {{ w.trangThai }} · {{ w.soLan }} lần thử · tạo {{ gio(w.taoLuc) }}{{ w.guiLuc ? ` · gửi ${gio(w.guiLuc)}` : '' }}
          <span v-if="w.loiCuoi" class="mo"> · {{ w.loiCuoi }}</span>
        </li>
      </ul>
    </section>

    <section id="muc-evidence" class="the">
      <h2>Evidence ({{ dc.evidence.length }})</h2>
      <p class="mo">Rút gọn. Bản đủ: <code>GET /cases/{{ dc.khoaNguon }}/evidence</code>.</p>
      <div v-for="e in dc.evidence" :key="e.khoaNguon" class="mau" :class="{ rut: e.rutLaiLuc, xn: e.xacNhanBuoc }">
        <div class="mo">
          <code>{{ e.khoaNguon }}</code> · {{ gio(e.quanSatLuc ?? e.napLuc) }} · khách sạn {{ e.maKhachSan ?? 'chưa khai' }} · {{ e.doDai }} ký tự
          <b v-if="e.xacNhanBuoc"> · LỜI XÁC NHẬN bước {{ e.xacNhanBuoc }}</b>
          <b v-if="e.rutLaiLuc" class="xau"> · ĐÃ RÚT LẠI {{ gio(e.rutLaiLuc) }}</b>
        </div>
        <div>{{ e.noiDungRutGon }}</div>
      </div>
    </section>
  </template>
</template>

<style scoped>
h3 { font-size: 13px; margin: 14px 0 6px; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
.dau-case { margin-bottom: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.so { text-align: right; font-variant-numeric: tabular-nums; }
.xau { color: var(--do); }
.tot { color: var(--xanh); font-weight: 600; }
.dong-tg { list-style: none; padding: 0; margin: 0; font-size: 13px; }
.dong-tg li { display: grid; grid-template-columns: 150px 80px 1fr; gap: 10px; padding: 5px 0; border-bottom: 1px dashed var(--vien); }
.dong-tg .loai { font-size: 11px; font-weight: 700; text-transform: uppercase; color: var(--mo); }
.dong-tg li.model .loai, .dong-tg li.nguoi-nap .loai { color: #6927da; }
.dong-tg li.goi-y .loai, .dong-tg li.nac .loai { color: var(--lam); }
.dong-tg li.xau b { color: var(--do); }
.buoc { padding-left: 20px; }
.buoc li { margin: 4px 0; color: var(--mo); }
.buoc li.xong { color: var(--xanh); }
.buoc li.hien { color: var(--chu); }
.nhan-hien { font-size: 11px; background: var(--lam-nhat); color: var(--lam); border-radius: 10px; padding: 1px 7px; margin-left: 6px; }
.can-cu { font-size: 12px; margin-left: 14px; }
.luot { border: 1px solid var(--vien); border-radius: 8px; padding: 8px 10px; margin: 8px 0; font-size: 13px; }
.pheu { display: flex; gap: 6px; margin-top: 6px; flex-wrap: wrap; }
.nac { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px; }
.nac.dat { background: var(--xanh-nhat); color: var(--xanh); }
.nac.khong-dat { background: var(--do-nhat); color: var(--do); }
.nac.chua { background: #f0f2f5; color: #98a2b3; }
.mau { border-left: 3px solid var(--vien); padding: 4px 10px; margin: 8px 0; font-size: 13px; }
.mau.xn { border-left-color: var(--lam); }
.mau.rut { border-left-color: var(--do); opacity: .7; }
@media (max-width: 640px) { .dong-tg li { grid-template-columns: 1fr; gap: 2px; } }
</style>
