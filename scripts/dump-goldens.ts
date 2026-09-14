// scripts/dump-goldens.ts — ゴールデンマスター (ランのジャーナル+状態ハッシュ列) を goldens/runs/ に書き出す。
// 使い方: npx tsx scripts/dump-goldens.ts [count=4] [baseSeed=9001] [leaderIds=leader_green,...]
//         npx tsx scripts/dump-goldens.ts --from <save.json>...   (人間/Opusのジャーナル付きセーブから作る)
//         npx tsx scripts/dump-goldens.ts --checkpoint <act> <deckId> [relicIds(,区切り)|-] [count=2] [baseSeed=9201] [leaderId]
//           (幕2/3のチェックポイント開始 = 幕2/3の地図・ボス・敵を照合に載せる。2026-09-14)
// C# 側 (unity/) は同じ origin+commands を再生し、各手のハッシュが一致することで等価性を証明する (docs/unity-port.md §1)
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { runDigest, runHash } from '../src/engine/golden.ts'
import { replayStates } from '../src/engine/run.ts'
import type { RunJournal } from '../src/engine/run.ts'
import { generateGolden } from '../src/sim/golden-driver.ts'
import type { GoldenRun } from '../src/sim/golden-driver.ts'

mkdirSync('goldens/runs', { recursive: true })
const args = process.argv.slice(2)
if (args[0] === '--from') {
  for (const f of args.slice(1)) {
    const d = JSON.parse(readFileSync(f, 'utf-8')) as { journal?: RunJournal; run?: { journal?: RunJournal } }
    const journal = d.journal ?? d.run?.journal
    if (!journal) {
      console.error(`skip (journal なし): ${f}`)
      continue
    }
    const { states, error } = replayStates(journal)
    const hashes = states.slice(1).map(runHash)
    const g: GoldenRun = { version: 1, origin: journal.origin, commands: journal.commands.slice(0, hashes.length), hashes, final: runDigest(states[states.length - 1]) }
    const name = `${journal.origin.leaderId}-${journal.origin.seed}-journal.json`
    writeFileSync(`goldens/runs/${name}`, JSON.stringify(g))
    console.log(`${name}: ${hashes.length} コマンド${error ? ` (再現が分岐: ${error})` : ''}`)
  }
} else if (args[0] === '--checkpoint') {
  const act = Number(args[1])
  const deckId = args[2]
  const relicIds = args[3] && args[3] !== '-' ? args[3].split(',') : undefined
  const count = Number(args[4] ?? 2)
  const baseSeed = Number(args[5] ?? 9201)
  const leaderId = args[6] ?? 'leader_green'
  for (let i = 0; i < count; i++) {
    const seed = baseSeed + i
    const origin: RunJournal['origin'] = { kind: 'checkpoint', seed, leaderId, checkpoint: { act, deckId, ...(relicIds ? { relicIds } : {}) } }
    const g = generateGolden(seed, leaderId, 6000, origin)
    const name = `${leaderId}-${seed}-act${act}.json`
    writeFileSync(`goldens/runs/${name}`, JSON.stringify(g))
    console.log(`${name}: ${g.commands.length} コマンド / 幕${g.final.act} 行${g.final.row} ${g.final.phase} HP${g.final.hp}`)
  }
} else {
  const count = Number(args[0] ?? 4)
  const baseSeed = Number(args[1] ?? 9001)
  const leaders = (args[2] ?? 'leader_green').split(',')
  for (const leaderId of leaders) {
    for (let i = 0; i < count; i++) {
      const seed = baseSeed + i
      const g = generateGolden(seed, leaderId)
      const name = `${leaderId}-${seed}.json`
      writeFileSync(`goldens/runs/${name}`, JSON.stringify(g))
      console.log(`${name}: ${g.commands.length} コマンド / 幕${g.final.act} 行${g.final.row} ${g.final.phase} HP${g.final.hp}`)
    }
  }
}
