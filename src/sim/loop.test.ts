// 無限ループの回帰テスト (2026-08-26)。
// 魔力変換(青)・自然の奔流(緑) が正味+1エナジー・消滅なしだったため、
// 集中(次のカード-1)と連鎖する思考(詠唱数ぶんドロー)を挟むとエナジーもドローも青天井になった
// (実測: deck_storm がターン3で詠唱数301・エナジー102、simの3000手セーフガードに到達)。
// カード追加でこの手のループが再発したら、ここで落ちる。
import { describe, expect, it } from 'vitest'
import { allDecks, allEnemies } from '../engine/content.ts'
import { startCombat } from '../engine/combat.ts'
import { applyCommand } from '../engine/state.ts'
import { chooseCommand } from './run.ts'

/** 1ターンの詠唱数の上限。健全なデッキの実測最大は11 (deck_chaos の衝動連打) */
const MAX_PLAYS_PER_TURN = 20
/** 1戦闘のコマンド数上限 */
const MAX_ACTIONS = 500

describe('無限ループ検知', () => {
  it('全デッキ × 敵 × 複数シードで、1ターンの詠唱数と1戦闘のコマンド数が上限を超えない', () => {
    const offenders: string[] = []
    let worstPlays = 0
    for (const deck of allDecks) {
      for (let seed = 1; seed <= 5; seed++) {
        const enemy = allEnemies[(seed * 3) % allEnemies.length]
        let s = startCombat(seed, 'set-confirm', enemy.id, deck.id)
        let actions = 0
        while (s.phase !== 'won' && s.phase !== 'lost') {
          if (++actions > MAX_ACTIONS) {
            offenders.push(`${deck.id} vs ${enemy.id} seed${seed}: コマンド数${actions}超過`)
            break
          }
          s = applyCommand(s, chooseCommand(s))
          if (s.player.cardsPlayedThisTurn > worstPlays) worstPlays = s.player.cardsPlayedThisTurn
          if (s.player.cardsPlayedThisTurn > MAX_PLAYS_PER_TURN) {
            offenders.push(
              `${deck.id} vs ${enemy.id} seed${seed}: ターン${s.turn}で詠唱数${s.player.cardsPlayedThisTurn}`,
            )
            break
          }
        }
      }
    }
    expect(offenders).toEqual([])
    // 上限に余裕があることも確認 (健全な最大は10台のはず)
    expect(worstPlays).toBeLessThanOrEqual(MAX_PLAYS_PER_TURN)
  })
})
