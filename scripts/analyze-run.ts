// プレイレポート (.md) またはセーブ (.json) の計測ブロックを表に展開する (2026-09-03)。
//   npm run analyze -- play-20260903-0146.md
//   npm run analyze -- save-*.json            (history を持つブラウザ保存)
// 出力は標準出力の markdown。物差し (通常戦4〜6T・ボス6〜10T) との比較を先頭に出す。
import { readFileSync } from 'node:fs'
import { actSummaries, formatAnalysis, type MetricsExport } from '../src/engine/analysis.ts'
import { safeEncounterName, toBattleRows, type BattleArchive } from '../src/ui/report.ts'

function fromMarkdown(md: string): MetricsExport | null {
  const lines = md.split(/\r?\n/)
  const head = lines.findIndex((l) => l.startsWith('## 計測（機械可読）'))
  if (head < 0) return null
  const open = lines.findIndex((l, i) => i > head && l.trim() === '```json')
  if (open < 0) return null
  const close = lines.findIndex((l, i) => i > open && l.trim() === '```')
  if (close < 0) return null
  return JSON.parse(lines.slice(open + 1, close).join('\n')) as MetricsExport
}

function fromSave(json: string): MetricsExport | null {
  const obj = JSON.parse(json) as { history?: BattleArchive[]; run?: { act: number; battlesWon: number; hp: number; deck: unknown[] } }
  if (!Array.isArray(obj.history)) return null
  const battles = toBattleRows(obj.history)
  return {
    schema: 'deck-rogue-metrics/1',
    run: obj.run ? { act: obj.run.act, battlesWon: obj.run.battlesWon, hp: obj.run.hp, deckSize: obj.run.deck.length } : null,
    battles,
    acts: actSummaries(battles),
  }
}

const files = process.argv.slice(2)
if (files.length === 0) {
  console.error('usage: npm run analyze -- <play-*.md | save-*.json> [...]')
  process.exit(2)
}
for (const f of files) {
  const text = readFileSync(f, 'utf-8')
  const exp = f.endsWith('.json') ? fromSave(text) : fromMarkdown(text)
  if (!exp) {
    console.log(`# ${f}\n計測ブロックがありません (2026-09-03 より前の書き出し、または history 無しのセーブ)。\n`)
    continue
  }
  if (files.length > 1) console.log(`<!-- ${f} -->`)
  console.log(formatAnalysis(exp, safeEncounterName))
  console.log('')
}
