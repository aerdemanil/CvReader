import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Geliştirmede /api istekleri .NET API'ye aktarılır (aynı köken → CORS gerekmez, cookie çalışır).
// Build çıktısı API'nin wwwroot klasörüne yazılır ve tek adresten sunulur.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5130',
    },
  },
  build: {
    outDir: '../CvReader.Api/wwwroot',
    emptyOutDir: true,
  },
})
