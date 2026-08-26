// 赤=速さの色への再設計 (2026-08-26) のテスト。
// 人間プレイテストで「赤は緑と同じ攻撃力＋劣る防御＝純粋な下位互換」と判明したのを受け、
// 確定済みルール表「赤の柱⑤速さで対価を払う / ⑥防御は割高のままだが腐らせない」を固定する。
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** 山札の中身を差し替える (衝動ドローの対象を決定的にするため) */
function withDrawPile(state: GameState, cardIds: readonly string[]): GameState {
  return {
    ...state,
    player: {
      ...state.player,
      drawPile: cardIds.map((id, i) => ({ uid: `d${i}_${id}`, def: getCardDef(id) })),
    },
  }
}

describe('赤の0マナ攻撃 (火花)', () => {
  it('0マナ・3ダメージで、消滅せず捨て札に行く (何度でも回ってくる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_spark'])
    const energy = s.player.energy
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spark' })
    expect(s.enemies[0].hp).toBe(enemyHp - 3)
    expect(s.player.energy).toBe(energy) // エナジーを1点も使わない
    expect(s.player.discardPile.map((c) => c.def.id)).toContain('red_spark')
    expect(s.player.exhaustPile).toHaveLength(0) // 消滅必須ルールの対象外 (補充を伴わないため)
  })

  it('手数が増えるので「攻撃プレイ後」誘発が余分に乗る (残り火で延焼+1)', () => {
    // 赤が速さで対価を受け取る核心: 火花は火弾より低火力だが、延焼・勢い・パッシブを1回多く誘発させる
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_perm_ember',
      'red_spark',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_ember' })
    expect(s.enemies[0].burn).toBe(0)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_spark' })
    expect(s.enemies[0].burn).toBe(1) // 0マナの一手が延焼を1点積む
  })

  it('連打しても手札が尽きて必ず止まる (補充を伴わない0マナはループしない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_spark',
      'red_spark',
      'red_spark',
    ])
    for (let i = 0; i < 3; i++) s = applyCommand(s, { type: 'PlayCard', cardUid: `t${i}_red_spark` })
    expect(s.player.hand).toHaveLength(0)
    expect(s.player.energy).toBe(3) // エナジーは減らないが、手札という別の在庫が尽きる
  })
})

describe('灰の盾 (防御に衝動ドローを付けて腐らせない)', () => {
  it('ブロック4 ＋ 山札の一番上を「このターン限り」で手札に加える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_guard'])
    s = withDrawPile(s, ['red_spark', 'red_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' })
    expect(s.player.block).toBe(4)
    expect(s.player.hand.map((c) => c.def.id)).toEqual(['red_spark'])
    expect(s.player.impulseUids).toEqual(['d0_red_spark']) // 衝動 = 未使用ならターン終了時に消滅
  })

  it('引いたのが0マナ攻撃なら、同じ1エナジーで防御と攻撃が同時に成立する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_guard'])
    s = withDrawPile(s, ['red_spark'])
    const energy = s.player.energy
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'd0_red_spark' })
    expect(s.player.block).toBe(4)
    expect(s.enemies[0].hp).toBe(enemyHp - 3)
    expect(s.player.energy).toBe(energy - 1) // 灰の盾の1Eだけ。攻防一体
  })
})

describe('熾火の一閃 (1マナで火力が低い代わりにキャントリップ)', () => {
  it('4ダメージ＋1ドローで手札の枚数が減らないため、後続の攻撃に繋がる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_ember_slash',
    ])
    s = withDrawPile(s, ['red_strike'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ember_slash' })
    expect(s.enemies[0].hp).toBe(enemyHp - 4) // 火弾6より2点低い
    expect(s.player.hand.map((c) => c.def.id)).toEqual(['red_strike']) // が、手札は減っていない
    expect(s.player.impulseUids).toHaveLength(0) // 衝動ではない通常のドロー
  })
})
