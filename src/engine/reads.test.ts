// 確定済みルール表「再生」(regenBreak) を固定する。
// 旧「読み合いの全敵展開」(setAlt = 行動単位の条件分岐) は 2026-09-13 罠モデルで撤去 = 敵は伏せを見ない
// (残るのは罠壊し・道化の破壊分岐だけ。trap-model.test がデータ固定)
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** 次の自ターンまで進める (確認ウィンドウは全て温存) */
function toNextTurn(state: GameState): GameState {
  let s = applyCommand(state, { type: 'EndTurn' })
  let guard = 0
  while (s.phase === 'awaiting-reaction' && guard++ < 20) {
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
  }
  return s
}

describe('regenBreak (苔まといの主の再生をバーストで止める)', () => {
  it('そのターンに12以上削ると次の再生が発動しない', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    // 2026-08-30 割合化: 12は幕3の平均ターン火力66に対して自動成立していた (再生が1点も仕事せず)。
    // 幕3の想定ターン火力の半分 = 30 へ
    expect(getEnemyDef('enemy_moss').regenBreak).toBe(30)
    s = withHand(s, ['green_fang']) // 14ダメ + 成長16 = 30 (しきい値ちょうど)
    s = { ...s, player: { ...s.player, growth: 16 } }
    const target = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    const afterHit = s.enemies[0].hp
    expect(target - afterHit).toBeGreaterThanOrEqual(30)
    s = toNextTurn(s)
    // 再生していない (敵の攻撃後もHPは削った値のまま)
    expect(s.enemies[0].hp).toBe(afterHit)
    expect(s.eventLog.some((e) => e.type === 'RegenBroken')).toBe(true)
  })

  it('12未満の削りでは従来どおり再生する', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    s = withHand(s, ['green_fang']) // 14ダメージ (<30)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    const afterHit = s.enemies[0].hp
    s = toNextTurn(s)
    expect(s.enemies[0].hp).toBe(afterHit + 5) // regen 5
  })

  it('累積はターンごとにリセットされる (前ターンの削りは持ち越さない)', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' }) // 6
    s = toNextTurn(s) // 再生する (6 < 12)。累積リセット
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' }) // また6 (合計12だが別ターン)
    const afterHit = s.enemies[0].hp
    s = toNextTurn(s)
    expect(s.enemies[0].hp).toBe(afterHit + 5) // 持ち越し無し = 再生する
  })
})
