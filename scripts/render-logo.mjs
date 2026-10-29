// scripts/render-logo.mjs — 題のロゴと歯車の紋 (docs/design/quality-sprint/logo-title.html の #logo・#gear) を透明 PNG に撮る (2026-10-29 #4)
//   使い方: node scripts/render-logo.mjs <abs logo.html> <outdir>   → <outdir>/logo.png (1600×440)・gear.png (1120×1120)。playwright は scratchpad の pw を使う (手元の道具)
import { createRequire } from 'node:module'
const require = createRequire('/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/31d623d9-6b03-4c3d-b43c-8579b601c36c/scratchpad/pw/package.json')
const { chromium } = require('playwright-core')
const [, , htmlPath, outDir] = process.argv
const browser = await chromium.launch({ executablePath: process.env.HOME + '/.cache/ms-playwright/chromium-1228/chrome-linux64/chrome' })
const page = await browser.newPage({ viewport: { width: 1700, height: 1700 }, deviceScaleFactor: 1 })
await page.goto('file://' + htmlPath)
await page.waitForTimeout(1500)
await page.evaluate(() => document.fonts.ready)
await page.waitForTimeout(500)
for (const id of ['logo', 'gear']) {
  const el = await page.$('#' + id)
  await el.screenshot({ path: `${outDir}/${id}.png`, omitBackground: true })
  console.log('ok', id)
}
await browser.close()
