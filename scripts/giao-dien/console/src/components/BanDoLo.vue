<script setup lang="ts">
// BẢN ĐỒ LÔ: mỗi ticket một hàng, mỗi ô của sơ đồ luồng một cột — tô bằng ĐÚNG hàm `tinhSoDo` của màn dòng chảy,
// nên hai màn không thể nói hai điều khác nhau về cùng một case. Nhìn một lần là thấy lô đã đi tới đâu, và ticket
// nào dừng ở ô nào.
//
// ⚠ Đọc bằng đường XEM (không gọi model, không ghi gì) — tải lại sau mỗi bước bao nhiêu lần cũng không tốn gì.
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../api'
import { CONG_GIA_LAP, type TicketGiaLap } from '../apiGiaLap'
import { tinhSoDo, type OSoDo } from '../soDo'
import { useKetNoi } from '../stores/ketNoi'
import NhanNguon from './NhanNguon.vue'

const props = defineProps<{ ticket: TicketGiaLap[]; phien: number }>()
const ketNoi = useKetNoi()
const router = useRouter()

interface Hang {
  t: TicketGiaLap
  o: OSoDo[]
  nhom: string | null
  nguon: string
  cham: 'dung' | 'sai' | 'khong-biet'
}

const hang = ref<Hang[]>([])
const loi = ref<string | null>(null)
const dangTai = ref(false)
const COT = ['A', 'B', 'C', 'ĐT', 'D', 'E', 'F', 'G', 'H', 'I', 'J']

/** Cùng luật chấm với gia_lap.cham_mot: không biết mà đáp án là KHONG-BIET thì ĐÚNG. */
function cham(nhom: string | null, dung: string): Hang['cham'] {
  if (!nhom) return dung === 'KHONG-BIET' ? 'dung' : 'khong-biet'
  return nhom === dung ? 'dung' : 'sai'
}

async function tai() {
  if (!props.ticket.length) {
    hang.value = []
    return
  }
  dangTai.value = true
  loi.value = null
  try {
    hang.value = await Promise.all(
      props.ticket.map(async (t) => {
        const dc = await api.dongChay(CONG_GIA_LAP, t.khoa)
        return { t, o: tinhSoDo(dc).o, nhom: dc.dinhTuyen.nhom, nguon: dc.dinhTuyen.nguon, cham: cham(dc.dinhTuyen.nhom, t.nhomDung) }
      }),
    )
  } catch (e) {
    loi.value = e instanceof Error ? e.message : String(e)
  } finally {
    dangTai.value = false
  }
}
watch(() => [props.ticket, props.phien], () => void tai(), { immediate: true })

function mo(khoa: string) {
  ketNoi.cong = CONG_GIA_LAP
  void router.push({ name: 'case', params: { khoa } })
}

const KQ = { dung: '✓', sai: '✗', 'khong-biet': '?' } as const
</script>

<template>
  <div class="ban-do">
    <div v-if="loi" class="bao loi">{{ loi }}</div>
    <div class="cuon">
      <table>
        <thead>
          <tr>
            <th>Ticket</th>
            <th>Nhóm đúng → nhóm hiệu lực</th>
            <th v-for="c in COT" :key="c" class="o-dau">{{ c }}</th>
          </tr>
        </thead>
        <tbody :class="{ mo: dangTai }">
          <tr v-for="h in hang" :key="h.t.khoa" @click="mo(h.t.khoa)">
            <td><code>{{ h.t.khoa.split('-').pop() }}</code> {{ h.t.tieuDe }}</td>
            <td class="nhom">
              <span class="kq" :class="h.cham">{{ KQ[h.cham] }}</span>
              <span class="mo">{{ h.t.nhomDung }}</span><br />
              {{ h.nhom ?? 'KHÔNG BIẾT' }} <NhanNguon :nguon="h.nguon" />
            </td>
            <td v-for="o in h.o" :key="o.ma" class="o" :class="o.trangThai" :title="`${o.ma} · ${o.ten}\n${o.chiTiet}`" />
          </tr>
        </tbody>
      </table>
    </div>
    <p class="mo chu">
      Ô xanh = đã qua · cam = dừng ở đây · xám = chưa tới. Rê chuột vào ô để xem lý do, bấm một hàng để mở dòng chảy.
      ✓/✗/? là chấm nhóm hiệu lực với ĐÁP ÁN bịa — nói đường ống chạy, KHÔNG nói độ chính xác định tuyến.
    </p>
  </div>
</template>

<style scoped>
.cuon { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 12.5px; }
th, td { padding: 5px 6px; border-bottom: 1px solid var(--vien); vertical-align: middle; text-align: left; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
th.o-dau { text-align: center; width: 30px; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f7f9fc; }
tbody.mo { opacity: .5; }
.nhom { font-size: 12px; min-width: 260px; }
td.o { width: 30px; height: 26px; border: 2px solid #fff; border-radius: 5px; background: #e4e7ec; }
td.o.qua { background: #1a7f5a; }
td.o.dung { background: #f79009; }
.kq { font-weight: 800; margin-right: 4px; }
.kq.dung { color: var(--xanh); } .kq.sai { color: var(--do); } .kq.khong-biet { color: var(--vang); }
.chu { font-size: 12px; margin: 6px 0 0; }
</style>
