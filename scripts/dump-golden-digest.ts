// scripts/dump-golden-digest.ts — ゴールデン (goldens/runs/*.json) を TS エンジンで再生し、指定した手 (コマンド index) の後の
// 要約 (engine/golden.ts の runDigest) を JSON で出す。C# 移植側の不一致を「どのフィールドが違うか」まで突き止めるための道具。
// 使い方: npx tsx scripts/dump-golden-digest.ts goldens/runs/leader_green-9001.json 37   (index 省略時は最終手)
//        npx tsx scripts/dump-golden-digest.ts goldens/runs/leader_green-9001.json 37 --events   (その手のイベント型列を全部出す)
import { readFileSync } from 'node:fs'
import { runDigest, runHash } from '../src/engine/golden.ts'
import { replayStates } from '../src/engine/run.ts'
import type { GoldenRun } from '../src/sim/golden-driver.ts'

const [file, idxArg, flag] = process.argv.slice(2)
if (!file) {
  console.error('usage: dump-golden-digest.ts <golden.json> [commandIndex] [--events]')
  process.exit(2)
}
const g = JSON.parse(readFileSync(file, 'utf-8')) as GoldenRun
const idx = idxArg !== undefined && idxArg !== '--events' ? Number(idxArg) : g.commands.length - 1
const showEvents = flag === '--events' || idxArg === '--events'
const { states, error } = replayStates({ origin: g.origin, commands: g.commands.slice(0, idx + 1) })
// replayStates は初期状態 + 各コマンド後の状態を返す (先頭が初期状態)。error があれば途中で止まっている
if (error) console.error(error)
const state = states[states.length - 1]
if (!state) {
  console.error('再生できない (状態が空)')
  process.exit(1)
}
const digest = runDigest(state)
console.log(JSON.stringify({ index: idx, command: g.commands[idx], expectedHash: g.hashes[idx], actualHash: runHash(state), digest }, null, 2))
if (showEvents && state.combat) {
  console.log('--- events (all) ---')
  state.combat.eventLog.forEach((e, i) => console.log(i, JSON.stringify(e)))
}
