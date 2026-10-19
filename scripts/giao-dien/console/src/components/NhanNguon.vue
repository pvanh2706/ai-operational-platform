<script setup lang="ts">
// Nhãn NGUỒN của một kết quả định tuyến. Luôn hiện — một nhóm không kèm nguồn là cách nhanh nhất để người xem tưởng
// định tuyến "chạy rồi" (IM-45). Tầng model luôn kèm chữ "ngưỡng chưa đo" (N7-1).
import { computed } from 'vue'
import { TEN_TANG, tangDinhTuyen } from '../soDo'

const props = defineProps<{ nguon: string }>()
const tang = computed(() => tangDinhTuyen(props.nguon))
</script>

<template>
  <span class="nhan" :class="tang" :title="nguon">
    {{ TEN_TANG[tang] }}<template v-if="tang === 'model'"> · ngưỡng chưa đo</template>
  </span>
</template>

<style scoped>
.nhan { display: inline-block; font-size: 11px; font-weight: 600; padding: 1px 7px; border-radius: 10px; white-space: nowrap; }
.nguoi-nap { background: #eff4ff; color: #1d4ed8; }
.tra-san { background: #f0f2f5; color: #475467; }
.model { background: #f4f0ff; color: #6927da; }
.model-chua-goi { background: #fffaeb; color: #b54708; }
.khac { background: #f0f2f5; color: #667085; }
</style>
