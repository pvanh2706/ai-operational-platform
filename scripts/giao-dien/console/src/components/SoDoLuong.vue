<script setup lang="ts">
// Sơ đồ luồng README, tô theo MỘT case. Bố cục hai hàng: hàng trên A → F (nhận tín hiệu → quy trình), hàng dưới
// G → J (tri thức → ghi lại). Hai mũi tên nét đứt đúng như README: đường lùi C ⇢ G, vòng phản hồi J ⇢ G.
import { computed } from 'vue'
import { Handle, MarkerType, Position, VueFlow, type Edge, type Node } from '@vue-flow/core'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import type { CanhSoDo, OSoDo } from '../soDo'

const props = defineProps<{ o: OSoDo[]; canh: CanhSoDo[] }>()
const emit = defineEmits<{ chon: [muc: string] }>()

const RONG = 178
const VI_TRI: Record<string, { x: number; y: number }> = {
  A: { x: 0, y: 0 }, B: { x: RONG, y: 0 }, C: { x: RONG * 2, y: 0 }, 'ĐT': { x: RONG * 3, y: 0 },
  D: { x: RONG * 4, y: 0 }, E: { x: RONG * 5, y: 0 }, F: { x: RONG * 6, y: 0 },
  G: { x: RONG * 3, y: 190 }, H: { x: RONG * 4, y: 190 }, I: { x: RONG * 5, y: 190 }, J: { x: RONG * 6, y: 190 },
}

const MAU = { qua: '#1a7f5a', dung: '#b54708', chua: '#c4cad3' } as const

const nodes = computed<Node<OSoDo>[]>(() =>
  props.o.map((x) => ({ id: x.ma, type: 'o', position: VI_TRI[x.ma] ?? { x: 0, y: 0 }, data: x })),
)

// Mũi tên nối: hàng trên trái → phải; F đi xuống G; hai đường nét đứt đi từ đáy lên đỉnh/đáy G.
function cong(c: CanhSoDo): { sourceHandle: string; targetHandle: string } {
  if (c.tu === 'F' && c.den === 'G') return { sourceHandle: 'day', targetHandle: 'dinh' }
  if (c.tu === 'C' && c.den === 'G') return { sourceHandle: 'day', targetHandle: 'dinh' }
  if (c.tu === 'J' && c.den === 'G') return { sourceHandle: 'day', targetHandle: 'day-vao' }
  return { sourceHandle: 'phai', targetHandle: 'trai' }
}

const edges = computed<Edge[]>(() =>
  props.canh.map((c) => {
    const mau = c.daDi ? MAU.qua : MAU.chua
    return {
      id: `${c.tu}-${c.den}`,
      source: c.tu,
      target: c.den,
      ...cong(c),
      type: c.tu === 'J' ? 'smoothstep' : c.netDut ? 'smoothstep' : 'default',
      animated: c.daDi && !!c.netDut,
      label: c.netDut ? c.nhan : undefined,
      labelStyle: { fontSize: '10px', fill: '#667085' },
      style: { stroke: mau, strokeWidth: c.daDi ? 2.5 : 1.5, strokeDasharray: c.netDut ? '6 4' : undefined },
      markerEnd: { type: MarkerType.ArrowClosed, color: mau },
    }
  }),
)
</script>

<template>
  <div class="so-do">
    <VueFlow
      :nodes="nodes"
      :edges="edges"
      :nodes-draggable="false"
      :nodes-connectable="false"
      :elements-selectable="false"
      :zoom-on-scroll="false"
      :zoom-on-double-click="false"
      :pan-on-drag="false"
      :prevent-scrolling="false"
      fit-view-on-init
      @node-click="({ node }) => emit('chon', (node.data as OSoDo).muc)"
    >
      <template #node-o="{ data }">
        <div class="o" :class="data.trangThai" :title="data.chiTiet">
          <Handle id="trai" type="target" :position="Position.Left" />
          <Handle id="dinh" type="target" :position="Position.Top" />
          <Handle id="day-vao" type="target" :position="Position.Bottom" style="left: 70%" />
          <div class="ma">{{ data.ma }}</div>
          <div class="ten">{{ data.ten }}</div>
          <div class="chi-tiet">{{ data.chiTiet }}</div>
          <Handle id="phai" type="source" :position="Position.Right" />
          <Handle id="day" type="source" :position="Position.Bottom" />
        </div>
      </template>
    </VueFlow>
    <div class="chu-giai">
      <span><i class="qua" /> đã qua</span>
      <span><i class="dung" /> dừng ở đây — xem lý do trong ô</span>
      <span><i class="chua" /> chưa tới</span>
      <span class="mo">Bấm một ô để tới phần chi tiết.</span>
    </div>
  </div>
</template>

<style scoped>
.so-do { height: 360px; border: 1px solid var(--vien); border-radius: 10px; background: #fbfcfd; position: relative; }
.o {
  width: 150px; min-height: 92px; padding: 7px 9px; border-radius: 8px; background: #fff; cursor: pointer;
  border: 2px solid #c4cad3; font-size: 11px; line-height: 1.35; color: var(--mo);
}
.o.qua { border-color: #1a7f5a; background: #f2faf6; }
.o.dung { border-color: #b54708; background: #fffaeb; }
.o .ma { font-weight: 800; font-size: 13px; color: var(--chu); }
.o .ten { font-weight: 600; color: var(--chu); margin-bottom: 2px; }
.o .chi-tiet { overflow: hidden; display: -webkit-box; -webkit-line-clamp: 3; line-clamp: 3; -webkit-box-orient: vertical; }
.chu-giai {
  position: absolute; left: 10px; bottom: 8px; display: flex; gap: 14px; flex-wrap: wrap; font-size: 12px;
  background: rgba(255, 255, 255, .9); padding: 3px 8px; border-radius: 6px;
}
.chu-giai i { display: inline-block; width: 10px; height: 10px; border-radius: 3px; margin-right: 4px; vertical-align: -1px; }
.chu-giai i.qua { background: #1a7f5a; } .chu-giai i.dung { background: #b54708; } .chu-giai i.chua { background: #c4cad3; }
:deep(.vue-flow__handle) { opacity: 0; pointer-events: none; }
</style>
