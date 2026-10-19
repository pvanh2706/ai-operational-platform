import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Console nội bộ (docs/18 nhóm P):
//  · build ra dist/, ui_server.py phục vụ ở /console — base phải khớp đường đó.
//  · lúc phát triển (`npm run dev`), Vite chuyển /app và /api sang ui_server.py, nên trình duyệt chỉ nói chuyện
//    với MỘT gốc — y hệt bản build. Không ai phải mở CORS trên host .NET (P3-1).
//  · CHỈ nghe 127.0.0.1, cùng luật DIA_CHI của ui_server: trang này hiện dữ liệu khách.
const UI_SERVER = 'http://127.0.0.1:8791'

export default defineConfig({
  base: '/console/',
  plugins: [vue()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/app': UI_SERVER,
      '/api': UI_SERVER,
    },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
})
