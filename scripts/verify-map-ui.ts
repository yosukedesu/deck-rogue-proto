// scripts/verify-map-ui.ts — マップ画面のスクショ検証 + 機械検査
//
//   npm run dev &                                   # 別ターミナルで
//   APP_URL=http://localhost:5173/ npx tsx scripts/verify-map-ui.ts [出力先プレフィクス]
//
// 前提 (どちらもリポジトリの依存にはしない = CIを重くしないため。scripts/ は tsconfig 対象外):
//   1. playwright-core と chromium: `npm i playwright-core` + `npx playwright install chromium`
//   2. 日本語フォント (Noto Sans CJK JP) と絵文字フォント (Noto Color Emoji)。
//      どちらか欠けると豆腐になり、文字幅・折返しのバグを見逃す
//      (2026-08-24 の前例: 豆腐スクショでモバイルのはみ出しを見逃した)
//
// 検査するもの:
//   - 接続線がマップデータのエッジ数と一致するか (「線がある」でなく「1本も欠けていない」)
//   - 長さ0の線 (座標計算ミスの典型) が無いか
//   - ノード数が一致するか
//   - 横スクロール / ビューポートはみ出しが無いか (3画面: 1280x900・390x844・844x390)
//   - クリック可否の回帰: 進めるノードだけが押せるか (SVG化で <button> を失ったため)

import { chromium, type Page } from 'playwright-core'
import { createRun } from '../src/engine/run.ts'
import { existsSync } from 'node:fs'
import { homedir } from 'node:os'
import { join } from 'node:path'

const OUT = process.argv[2] ?? '/tmp/map-ui'
const URL = process.env.APP_URL ?? 'http://localhost:5175/'
const FONT = join(homedir(), '.local/share/fonts/NotoSansCJKjp-Regular.otf')

/** はみ出しの機械検査: ページ全体の横スクロールと、ビューポート右端を超える要素 */
async function overflowReport(page: Page): Promise<{ pageOverflow: boolean; offenders: string[] }> {
  return page.evaluate(() => {
    const doc = document.documentElement
    const pageOverflow = doc.scrollWidth > doc.clientWidth
    const vw = window.innerWidth
    const offenders: string[] = []
    for (const el of Array.from(document.querySelectorAll('*'))) {
      const r = el.getBoundingClientRect()
      if (r.width === 0 || r.height === 0) continue
      // スクロールコンテナの内部は除外 (意図的に横に伸びる領域がある)
      let scrollable = false
      for (let p = el.parentElement; p; p = p.parentElement) {
        const ov = getComputedStyle(p).overflowX
        if (ov === 'auto' || ov === 'scroll') { scrollable = true; break }
      }
      if (scrollable) continue
      if (r.right > vw + 1 || r.left < -1) {
        const tag = el.tagName.toLowerCase()
        const cls = typeof el.className === 'string' ? el.className.split(' ')[0] : ''
        offenders.push(`${tag}.${cls} right=${Math.round(r.right)} (vw=${vw})`)
      }
    }
    return { pageOverflow, offenders: offenders.slice(0, 12) }
  })
}

/** マップ画面まで進める (リーダー既定=このは、シード指定でランを開始) */
async function gotoMap(page: Page, seed: number): Promise<void> {
  await page.goto(URL, { waitUntil: 'networkidle' })
  const seedInput = page.locator('input').first()
  if (await seedInput.count()) {
    await seedInput.fill(String(seed))
  }
  await page.getByRole('button', { name: /ランを開始/ }).click()
  await page.getByRole('heading', { name: /マップ/ }).waitFor({ timeout: 5000 })
}

/** 線 (SVG) が実際に描かれているかの機械検査 */
async function edgeReport(page: Page): Promise<{ svgCount: number; lineCount: number; zeroLength: number }> {
  return page.evaluate(() => {
    const svgs = Array.from(document.querySelectorAll('svg'))
    const lines = Array.from(document.querySelectorAll('svg line, svg path, svg polyline'))
    let zeroLength = 0
    for (const l of lines) {
      const r = (l as SVGGraphicsElement).getBoundingClientRect()
      if (r.width < 1 && r.height < 1) zeroLength++
    }
    return { svgCount: svgs.length, lineCount: lines.length, zeroLength }
  })
}

/** マップデータ上のエッジ総数 (これと描画された線の数が一致すべき) */
function expectedEdgeCount(seed: number): number {
  const run = createRun(seed, 'set-confirm')
  return run.map.reduce((sum, row) => sum + row.reduce((s2, n) => s2 + n.next.length, 0), 0)
}

async function main(): Promise<void> {
  if (!existsSync(FONT)) {
    console.error(`❌ 日本語フォントが無い: ${FONT} — 豆腐スクショでは文字幅を検証できない`)
    process.exitCode = 1
    return
  }
  // playwright-core は自前でブラウザを持たないので、キャッシュ済み chromium を明示的に指す
  const exe = process.env.CHROMIUM_PATH ?? join(homedir(), '.cache/ms-playwright/chromium-1228/chrome-linux64/chrome')
  const expected = expectedEdgeCount(2100)
  console.log(`マップデータのエッジ数: ${expected}`)
  const browser = await chromium.launch(existsSync(exe) ? { executablePath: exe } : {})
  const cases = [
    { name: 'desktop', width: 1280, height: 900 },
    { name: 'mobile', width: 390, height: 844 },
    { name: 'mobile-landscape', width: 844, height: 390 },
  ]
  let bad = false
  for (const c of cases) {
    const ctx = await browser.newContext({ viewport: { width: c.width, height: c.height } })
    const page = await ctx.newPage()
    await gotoMap(page, 2100)
    await page.screenshot({ path: `${OUT}-${c.name}.png`, fullPage: true })
    const ov = await overflowReport(page)
    const ed = await edgeReport(page)
    // 全エッジが描かれているか (「線がある」でなく「1本も欠けていない」の確認)
    const edgesOk = ed.lineCount === expected
    const ok =
      !ov.pageOverflow && ov.offenders.length === 0 && edgesOk && ed.zeroLength === 0
    if (!ok) bad = true
    console.log(
      `${ok ? '✅' : '❌'} ${c.name}: 横スクロール=${ov.pageOverflow} はみ出し=${ov.offenders.length}件 ` +
        `svg=${ed.svgCount} 線=${ed.lineCount}/${expected} 長さ0の線=${ed.zeroLength}`,
    )
    for (const o of ov.offenders) console.log(`    はみ出し: ${o}`)

    // ノード数がマップデータと一致するか (SVG化で描画漏れが起きていないか)
    const nodeCount = await page.locator('.map-node').count()
    const expectedNodes = createRun(2100, 'set-confirm').map.reduce((n, row) => n + row.length, 0)
    // クリック可否の回帰: SVG化で <button> を失ったので、進めるノードが実際に押せるかを見る
    // (押せないノードを押しても何も起きないことも確認する)
    const openNodes = page.locator('.map-node-open')
    const openCount = await openNodes.count()
    await page.locator('.map-node:not(.map-node-open)').first().click({ force: true })
    const stillMap = await page.getByRole('heading', { name: /マップ/ }).count()
    await openNodes.first().click()
    const leftMap = (await page.getByRole('heading', { name: /マップ/ }).count()) === 0
    const clickOk = nodeCount === expectedNodes && openCount > 0 && stillMap === 1 && leftMap
    if (!clickOk) bad = true
    console.log(
      `${clickOk ? '✅' : '❌'} ${c.name}: ノード=${nodeCount}/${expectedNodes} ` +
        `進めるノード=${openCount} 押せないノードは無反応=${stillMap === 1} 進めるノードで遷移=${leftMap}`,
    )
    await ctx.close()
  }
  await browser.close()
  console.log(`スクショ: ${OUT}-*.png`)
  if (bad) process.exitCode = 1
}

main()
