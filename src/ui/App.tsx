// ui/ は状態を読んでコマンドを投げるだけの薄い層。ゲームロジックを書かない (CLAUDE.md)。
// 見た目は作らない方針 (長方形+テキストのみ)。
import { useState } from 'react'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { GameState, ReactionMode } from '../engine/types.ts'

const MODES: ReactionMode[] = ['set-auto', 'hold-manual', 'set-confirm']

export default function App() {
  const [state, setState] = useState<GameState | null>(null)

  if (state === null) {
    // ニューゲーム時に方式を選択する (CLAUDE.md「比較する3方式」)
    return (
      <main style={{ fontFamily: 'monospace', padding: 16 }}>
        <h1>deck-rogue-proto</h1>
        <p>リアクション方式を選択:</p>
        {MODES.map((mode) => (
          <button
            key={mode}
            style={{ display: 'block', margin: 8, padding: 8 }}
            onClick={() => {
              const seed = Date.now() % 2 ** 32
              setState(
                applyCommand(createInitialState(seed, mode), {
                  type: 'StartCombat',
                  seed,
                  enemyIds: [],
                }),
              )
            }}
          >
            {mode}
          </button>
        ))}
      </main>
    )
  }

  return (
    <main style={{ fontFamily: 'monospace', padding: 16 }}>
      <h1>deck-rogue-proto — {state.reactionMode}</h1>
      <p>
        ターン {state.turn} / フェーズ {state.phase} / HP {state.player.hp}/{state.player.maxHp} /
        エナジー {state.player.energy}/{state.player.energyMax}
      </p>
      <p>(戦闘 UI は実装フェーズで構築)</p>
      <button onClick={() => setState(null)}>方式選択に戻る</button>
    </main>
  )
}
