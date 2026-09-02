// scripts/aggregate-picks.ts — 報酬ピックの「提示されたが選ばれなかった率」を札ごとに集計する (退屈指数の物差し。2026-09-02)。
// 入力: プレイレポート .md (「## 選択履歴」の行) と、ジャーナル付きセーブ .json (replayStates で選択履歴を再構成)。
// 使い方: npx tsx scripts/aggregate-picks.ts [--color green] <file...>
//   例: npx tsx scripts/aggregate-picks.ts --color green ~/Downloads/play-*.md scratchpad/opus-*.json
// 出力: 札 | 提示 | ピック | 見送り | ピック率 (提示回数の多い順・率の低い順)。★=3回以上提示で0ピック
import { readFileSync } from 'node:fs'
import { allCards } from '../src/engine/content.ts'
import { replayStates } from '../src/engine/run.ts'
import type { RunJournal } from '../src/engine/run.ts'
import { describeRunChoice } from '../src/ui/report.ts'

const args = process.argv.slice(2)
let color: string | null = null
const files: string[] = []
for (let i = 0; i < args.length; i++) {
  if (args[i] === '--color') color = args[++i] ?? null
  else files.push(args[i])
}
if (files.length === 0) {
  console.error('usage: npx tsx scripts/aggregate-picks.ts [--color green] <report.md|save.json>...')
  process.exit(1)
}

const offered = new Map<string, number>()
const picked = new Map<string, number>()
const bump = (m: Map<string, number>, k: string) => m.set(k, (m.get(k) ?? 0) + 1)
let runs = 0
let lines = 0

/** 選択履歴の1行から (獲得, 見送り候補) を取り出す。describeRunChoice の文言に依存 (ui/report.ts) */
function ingest(text: string): void {
  const pick = /報酬ピック: (.+?) を獲得（見送り: (.+?)）/.exec(text)
  if (pick) {
    lines++
    bump(offered, pick[1])
    bump(picked, pick[1])
    for (const c of pick[2].split('・')) bump(offered, c)
    return
  }
  const skip = /報酬ピック: スキップ（候補: (.+?)）/.exec(text)
  if (skip) {
    lines++
    for (const c of skip[1].split('・')) bump(offered, c)
  }
}

for (const f of files) {
  const raw = readFileSync(f, 'utf-8')
  if (f.endsWith('.json')) {
    const d = JSON.parse(raw) as { journal?: RunJournal; run?: { journal?: RunJournal } }
    const journal = d.journal ?? d.run?.journal
    if (!journal) {
      console.error(`skip (journal なし): ${f}`)
      continue
    }
    const { states, error } = replayStates(journal)
    for (let i = 0; i + 1 < states.length; i++) {
      const ch = describeRunChoice(states[i], journal.commands[i], states[i + 1])
      if (ch) ingest(ch.text)
    }
    if (error) console.error(`${f}: ${error} (そこまでの ${states.length - 1} コマンドを集計)`)
    runs++
  } else {
    let n = 0
    for (const line of raw.split('\n')) {
      const before = lines
      ingest(line)
      if (lines > before) n++
    }
    if (n > 0) runs++
    else console.error(`skip (選択履歴なし): ${f}`)
  }
}

const nameToCard = new Map(allCards.map((c) => [c.name, c] as const))
const rows = [...offered.entries()]
  .filter(([name]) => (color ? nameToCard.get(name)?.color === color : true))
  .map(([name, o]) => {
    const p = picked.get(name) ?? 0
    const c = nameToCard.get(name)
    return { name, o, p, rate: p / o, cost: c ? (c.xCost ? 'X' : String(c.cost)) : '?', rarity: c?.rarity?.[0] ?? '?' }
  })
  .sort((a, b) => b.o - a.o || a.rate - b.rate)
console.log(`# ${runs}ラン / 選択履歴 ${lines}行 / 提示スロット ${[...offered.values()].reduce((a, b) => a + b, 0)}${color ? ` / 色=${color}` : ''}`)
console.log('| 札 | レア | コスト | 提示 | ピック | 見送り | ピック率 |')
console.log('|---|---|---|---|---|---|---|')
for (const r of rows) {
  const flag = r.o >= 3 && r.p === 0 ? '★' : ''
  console.log(`| ${flag}${r.name} | ${r.rarity} | ${r.cost} | ${r.o} | ${r.p} | ${r.o - r.p} | ${Math.round(r.rate * 100)}% |`)
}
