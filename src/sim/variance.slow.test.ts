// アーキ別勝率の分散監視 (2026-09-02 残件議論の採択 #111)。StS2教訓「平均勝率でなく、どのデッキが
// どの敵に詰むかの分散が炎上の火種」を機械固定する: 理想形デッキ×ボス/エリートで勝率0%のセルは
// KNOWN_STRUCTURAL_HOLES に意識的に書かれていなければ落ちる (loop.test の offenders 空配列と同型)。
// 重いので既定のテストからは外し、VARIANCE=1 で明示実行する: `npm run test:variance`
import { describe, expect, it } from 'vitest'
import { allDecks } from '../engine/content.ts'
import { ACT_BOSSES, ELITE_POOLS } from '../engine/map.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import { chooseCommand } from './run.ts'

/**
 * 既知の0%セル (デッキ×敵) = variance-baseline.json。初版は 2026-09-02 の実測 (93セル。収穫・ストーム・
 * 反復・抱え込み・軍団など「ボットが最も下手な操作」の床値を含む)。新しく0%になるセルはここに
 * 足す1手を強制し、逆に解消したセルは console に出るので掃除する
 */
import baseline from './variance-baseline.json'
const KNOWN_STRUCTURAL_HOLES: ReadonlySet<string> = new Set<string>(baseline.zeroCells)

const SEEDS = [11, 22, 33, 44]

describe.skipIf(!process.env.VARIANCE)('アーキ別勝率の分散監視 (VARIANCE=1)', () => {
  it('理想形デッキ×ボス/エリートで勝率0%のセルは KNOWN_STRUCTURAL_HOLES に載っている', () => {
    const decks = allDecks.filter((d) => d.id.startsWith('deck_'))
    const enemies = [...ACT_BOSSES, ...ELITE_POOLS.flat()]
    const zeroCells: string[] = []
    const summary: string[] = []
    for (const deck of decks) {
      for (const enemy of enemies) {
        let wins = 0
        for (const seed of SEEDS) {
          let s
          try {
            s = applyCommand(createInitialState(seed, 'set-confirm'), { type: 'StartCombat', seed, enemyId: enemy, deckId: deck.id })
          } catch {
            continue
          }
          let guard = 0
          while (s.phase !== 'won' && s.phase !== 'lost' && guard < 1500) {
            s = applyCommand(s, chooseCommand(s))
            guard++
          }
          if (s.phase === 'won') wins++
        }
        const key = `${deck.id}|${enemy}`
        summary.push(`${key}:${wins}/${SEEDS.length}`)
        if (wins === 0 && !KNOWN_STRUCTURAL_HOLES.has(key)) zeroCells.push(key)
      }
    }
    console.error('# variance cells: ' + summary.join(' '))
    const resolved = [...KNOWN_STRUCTURAL_HOLES].filter((k) => !summary.some((x) => x.startsWith(k + ':0/')))
    if (resolved.length > 0) console.error('# 解消した既知0%セル (baseline から外せる): ' + resolved.join(' '))
    expect(zeroCells, '新しい構造詰みセル (理由を添えて KNOWN_STRUCTURAL_HOLES へ)').toEqual([])
  })
})
