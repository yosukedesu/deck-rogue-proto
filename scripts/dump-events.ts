// scripts/dump-events.ts — ジャーナル（ゴールデン／セーブ／CLI の状態ファイル）を TS エンジンで再生し、
// 指定した出来事（既定＝赤の解凍で増えた7種）が「どのコマンドで・どんな順で・誰に」出たかを JSON に出す。
// Godot 移植（dot）への実例（2026-10-12 dot の返信 3）。同じコミットのエンジンで再生するので、コミット ID と実例が一致する。
//
// 使い方: npx tsx scripts/dump-events.ts [--events A,B,...] [--max N] [--out file.json] [--label text] <journal.json ...>
//   ファイル省略時は goldens/runs/*.json。--max は「出来事の種類ごと・ファイルごと」の実例の上限（既定 3。数は全部数える）
//   ゴールデンに出ない出来事は summary に「未出現」と出る。中身の作り方は src/sim/event-scan.ts
import { readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { execSync } from 'node:child_process'
import { RED_EVENTS, journalOf, scanEvents } from '../src/sim/event-scan.ts'
import type { EventExample } from '../src/sim/event-scan.ts'

const args = process.argv.slice(2)
let events: readonly string[] = RED_EVENTS
let max = 3
let out: string | null = null
let label = ''
const files: string[] = []
for (let i = 0; i < args.length; i++) {
  const a = args[i]
  if (a === '--events') events = args[++i].split(',').map((s) => s.trim()).filter(Boolean)
  else if (a === '--max') max = Number(args[++i])
  else if (a === '--out') out = args[++i]
  else if (a === '--label') label = args[++i]
  else files.push(a)
}
if (files.length === 0) files.push(...readdirSync('goldens/runs').filter((f) => f.endsWith('.json')).sort().map((f) => `goldens/runs/${f}`))

function git(cmd: string): string {
  try { return execSync(`git ${cmd}`, { encoding: 'utf-8', stdio: ['ignore', 'pipe', 'ignore'] }).trim() } catch { return '' }
}

const summary: Record<string, { count: number; files: string[]; firstAt: { file: string; commandIndex: number } | null }> = {}
for (const t of events) summary[t] = { count: 0, files: [], firstAt: null }
const examples: (EventExample & { readonly file: string })[] = []
const sources: { file: string; label: string; commands: number; replayed: number; replayError: string | null; skipped?: string }[] = []

for (const file of files) {
  const journal = journalOf(JSON.parse(readFileSync(file, 'utf-8')))
  if (!journal) { sources.push({ file, label, commands: 0, replayed: 0, replayError: null, skipped: 'journal が無い' }); continue }
  const s = scanEvents(journal, events, max)
  sources.push({ file, label, commands: s.commands, replayed: s.replayed, replayError: s.replayError })
  for (const t of events) {
    if (s.counts[t] > 0) {
      summary[t].count += s.counts[t]
      summary[t].files.push(file)
      if (!summary[t].firstAt) summary[t].firstAt = { file, commandIndex: s.firstAt[t] ?? 0 }
    }
  }
  for (const ex of s.examples) examples.push({ file, ...ex })
}

const dirty = git('status --porcelain -- src/engine src/sim src/data goldens scripts/dump-events.ts')
const result = {
  generatedAt: new Date().toISOString(),
  commit: git('rev-parse HEAD'),
  branch: git('branch --show-current'),
  engineTreeClean: dirty === '',
  note: '出来事は TS エンジン（src/engine）でジャーナルを再生して取った。commandIndex は 1 始まり（verify-goldens の「コマンドN」と同じ）。events は その手で新しく出た出来事を順序どおり全部、n は戦闘の eventLog の添字。matched の付いた出来事が対象の種類',
  eventTypes: events,
  summary: Object.fromEntries(events.map((t) => [t, { status: summary[t].count > 0 ? '出現' : '未出現', count: summary[t].count, files: summary[t].files, firstAt: summary[t].firstAt }])),
  sources,
  examples,
}
const text = JSON.stringify(result, null, 1)
if (out) { writeFileSync(out, text); console.log(`wrote ${out}: ${examples.length} examples`) }
else console.log(text)
for (const t of events) console.error(`${t}: ${summary[t].count > 0 ? `${summary[t].count} 回（${summary[t].files.length} ファイル）` : '未出現'}`)
