// scripts/verify-goldens.ts — goldens/runs/*.json を TS エンジンで再生し、ハッシュ列が一致するか照合する。
// ルール変更で分岐したゴールデンは「再生成が必要」= 移植側にも同じ変更が要るサイン。
// 使い方: npx tsx scripts/verify-goldens.ts [files...]  (省略時は goldens/runs/ 全部)
import { readdirSync, readFileSync } from 'node:fs'
import { replayStates } from '../src/engine/run.ts'
import { verifyGolden } from '../src/sim/golden-driver.ts'
import type { GoldenRun } from '../src/sim/golden-driver.ts'

const files = process.argv.slice(2).length > 0 ? process.argv.slice(2) : readdirSync('goldens/runs').filter((f) => f.endsWith('.json')).map((f) => `goldens/runs/${f}`)
let bad = 0
for (const f of files) {
  const g = JSON.parse(readFileSync(f, 'utf-8')) as GoldenRun
  const r = verifyGolden(g, (origin, commands) => replayStates({ origin, commands }))
  if (r.ok) console.log(`OK   ${f} (${g.hashes.length})`)
  else {
    bad++
    console.log(`DIFF ${f}: コマンド${r.at}で分岐: ${r.error}`)
  }
}
process.exit(bad > 0 ? 1 : 0)
