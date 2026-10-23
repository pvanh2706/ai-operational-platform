<script setup lang="ts">
// MỘT phát biểu: nội dung + mức kiểm chứng + nguồn gốc + dẫn chứng — luôn đi cùng nhau (`AP3`).
// "Có nguồn hỗ trợ" chỉ khi có mẩu quan hệ Support chưa bị rút; còn lại nói thẳng là chưa có — không tự nâng mức.
// `canh`: màn đủ rộng thì nội dung bên trái, dẫn chứng bên phải; màn hẹp thì xếp lần lượt.
import { computed } from 'vue'
import { laHoTro, MUC_KIEM_CHUNG, NGUON_GOC, type MotDanChung } from '../doc'
import DanChungDoc from './DanChungDoc.vue'

const props = defineProps<{
  cong: number
  noiDung: string
  mucKiemChung: string
  nguonGoc: string
  danChung: MotDanChung[]
  canh?: boolean
}>()

const muc = computed(() => MUC_KIEM_CHUNG[props.mucKiemChung] ?? { ten: props.mucKiemChung, loai: 'yeu' as const })
const coHoTro = computed(() => props.danChung.some(laHoTro))
</script>

<template>
  <div class="pb" :class="{ canh }">
    <div class="trai">
      <div class="nd">{{ noiDung }}</div>
      <div class="nhan">
        <span class="muc" :class="muc.loai" :title="mucKiemChung">{{ muc.ten }}</span>
        <span class="goc" :title="nguonGoc">{{ NGUON_GOC[nguonGoc] ?? nguonGoc }}</span>
        <span v-if="!coHoTro" class="khong-ho">không mẩu nào hỗ trợ trực tiếp</span>
        <slot name="nhan" />
      </div>
      <slot />
    </div>
    <div class="dc">
      <div v-if="canh" class="tieu-dc">Dẫn chứng</div>
      <DanChungDoc :cong="cong" :ds="danChung" />
    </div>
  </div>
</template>

<style scoped>
.pb { padding: 6px 0; border-bottom: 1px dashed var(--vien); }
.pb:last-child { border-bottom: 0; }
.nd { white-space: pre-wrap; word-break: break-word; }
.nhan { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 3px; font-size: 11px; }
.muc, .goc, .khong-ho { border-radius: 10px; padding: 0 7px; font-weight: 600; }
.muc.yeu { background: var(--vang-nhat); color: var(--vang); }
.muc.vua { background: var(--lam-nhat); color: var(--lam); }
.muc.manh { background: var(--xanh-nhat); color: var(--xanh); }
.muc.tranh-chap { background: var(--do-nhat); color: var(--do); }
.goc { background: #f0f2f5; color: #475467; }
.khong-ho { color: var(--vang); font-weight: 400; font-style: italic; }
.dc { margin-top: 2px; min-width: 0; }
.tieu-dc { font-size: 10px; font-weight: 700; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
@media (min-width: 900px) {
  .pb.canh { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 42%); gap: 14px; }
  .pb.canh .dc { margin-top: 0; border-left: 1px solid var(--vien); padding-left: 12px; }
}
</style>
