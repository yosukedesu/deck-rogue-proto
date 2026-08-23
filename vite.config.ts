/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  // GitHub Pages はサブパス (https://<user>.github.io/<repo>/) 配信のため相対パスにする
  base: './',
  plugins: [react()],
  test: {
    // engine は純ロジックなので DOM 環境不要 (CLAUDE.md「アーキテクチャ原則」)
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
