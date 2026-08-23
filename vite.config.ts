/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // engine は純ロジックなので DOM 環境不要 (CLAUDE.md「アーキテクチャ原則」)
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
