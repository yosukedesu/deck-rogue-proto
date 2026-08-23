// sim/run.ts — ヘッドレス自動対戦 (Node 単体実行: npm run sim)
// TODO(実装フェーズ): 単純ボットで N 戦回し、方式ごとの統計を CSV 出力する。
//   現時点は「engine が React 無しで Node 単体で動く」ことの検証を兼ねたスタブ。
//
// 使い方: npm run sim -- [対戦数] [シード]

import { createInitialState, applyCommand } from '../engine/state.ts'
import type { ReactionMode } from '../engine/types.ts'
import cards from '../data/cards.green.json' with { type: 'json' }
import enemies from '../data/enemies.json' with { type: 'json' }

const battles = Number(process.argv[2] ?? 100)
const baseSeed = Number(process.argv[3] ?? 1)
const modes: ReactionMode[] = ['set-auto', 'hold-manual', 'set-confirm']

console.error(`# deck-rogue-proto sim: ${battles}戦/方式, baseSeed=${baseSeed}`)
console.error(`# データ読込: カード${cards.length}枚, 敵${enemies.length}類型`)

// CSV は stdout へ (リダイレクトで results.csv に保存する運用)
console.log('mode,battles,wins,winRate,avgTurns,reactionFireRate,whiffRate')

for (const mode of modes) {
  // スタブ: 戦闘ループ未実装のため StartCombat だけ通して 0 行を出す
  const state = applyCommand(createInitialState(baseSeed, mode), {
    type: 'StartCombat',
    seed: baseSeed,
    enemyIds: enemies.map((e) => e.id),
  })
  console.log(`${mode},${battles},0,0.000,${state.turn},0.000,0.000`)
}

console.error('# ⚠️ 戦闘ループ未実装のスタブ出力 (環境検証用)')
