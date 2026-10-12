// scripts/find-event-journals.ts — 指定した出来事が実際に出る「再生できるジャーナル」をボットのランで探し、
// 通常のゴールデンとは別の goldens/events/ に書く（形は GoldenRun＝origin＋commands＋hashes＋final。verify-goldens と C# の verify で照合できる）。
// Godot 移植（dot）の依頼（2026-10-12 返信 4）: ゴールデンに出ない CascadeRetargeted／ImpulseKept の再生できる実例。
//
// 使い方: npx tsx scripts/find-event-journals.ts [--events A,B] [--decks d1,d2] [--act 2] [--leader leader_red] [--seeds 7001-7060] [--per 1] [--out goldens/events]
//   種類ごとに --per 本（既定 1）見つかるまでシードを順に試す。見つけたジャーナルの名前: <leader>-<deck>-<seed>-act<act>-<event>.json
import { mkdirSync, writeFileSync } from 'node:fs'
import { generateGolden } from '../src/sim/golden-driver.ts'
import { RED_EVENTS, scanEvents } from '../src/sim/event-scan.ts'
import type { ReplayOrigin } from '../src/engine/run.ts'

const args = process.argv.slice(2)
let events: readonly string[] = ['CascadeRetargeted', 'ImpulseKept']
let decks = ['run_red_s2_c', 'run_red_s2_b', 'run_red_s2_a']
let act = 2
let leaderId = 'leader_red'
let seedFrom = 7001
let seedTo = 7060
let per = 1
let outDir = 'goldens/events'
for (let i = 0; i < args.length; i++) {
  const a = args[i]
  if (a === '--events') events = args[++i].split(',')
  else if (a === '--decks') decks = args[++i].split(',')
  else if (a === '--act') act = Number(args[++i])
  else if (a === '--leader') leaderId = args[++i]
  else if (a === '--seeds') { const [f, t] = args[++i].split('-').map(Number); seedFrom = f; seedTo = t ?? f }
  else if (a === '--per') per = Number(args[++i])
  else if (a === '--out') outDir = args[++i]
}
mkdirSync(outDir, { recursive: true })
const found: Record<string, number> = {}
for (const t of events) found[t] = 0
const need = () => events.filter((t) => found[t] < per)

outer: for (let seed = seedFrom; seed <= seedTo; seed++) {
  for (const deckId of decks) {
    if (need().length === 0) break outer
    const origin: ReplayOrigin = { kind: 'checkpoint', seed, leaderId, checkpoint: { act, deckId } }
    const g = generateGolden(seed, leaderId, 6000, origin)
    const s = scanEvents({ origin: g.origin, commands: g.commands }, RED_EVENTS, 1)
    const hits = need().filter((t) => s.counts[t] > 0)
    const line = RED_EVENTS.map((t) => `${t}=${s.counts[t]}`).join(' ')
    console.error(`seed ${seed} ${deckId}: ${g.commands.length} 手 ${line}`)
    for (const t of hits) {
      found[t]++
      const name = `${leaderId}-${deckId}-${seed}-act${act}-${t}.json`
      writeFileSync(`${outDir}/${name}`, JSON.stringify(g))
      console.log(`${outDir}/${name}: ${g.commands.length} 手・${t} は ${s.counts[t]} 回（最初はコマンド${s.firstAt[t]}）`)
    }
  }
}
for (const t of events) if (found[t] < per) console.log(`${t}: 見つからず（seed ${seedFrom}〜${seedTo}・${decks.join(',')}）`)
