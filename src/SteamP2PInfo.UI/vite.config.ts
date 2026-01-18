import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: './',
  build: {
    outDir: '../SteamP2PInfo.App/wwwroot',
    emptyOutDir: true,
  }
})
