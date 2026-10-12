// 赤の軸の穴埋め (2026-08-30)。アーキタイプの整理で見つかった3つの穴を固定する:
// ①カオス = 乱数4枚に対しペイオフ0 (2026-10-10 に撤去) ②衝動 = 撒く14枚に対しペイオフ1 ③憤怒 = 被弾の見返りが1テンポ遅れる。
import { describe, expect, it } from 'vitest'
import { isBlazing } from './effects.ts'
import { getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withEnergy = (s: GameState, energy: number): GameState => ({
  ...s,
  player: { ...s.player, energy },
})

// カオスの受け皿 (賭博師の焔・一擲乾坤・続け賭け) は 2026-10-10 の赤の解凍で撤去 (乱数は味付け4枚・第3の軸はチェイン = red-thaw.test.ts)。
// engine の onRandomPlayed / dealDamagePerRandomPlayed は残っている (工房・他色の資産)

describe('衝動の受け皿 (onImpulsePlayed のペイオフ3枚目・4枚目)', () => {
  it('衝動で引いた札をプレイすると焔の目録が勢いを、走り火がダメージを出す', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_perm_ledger',
      'red_perm_runfire',
      'red_impulse', // 衝動3 + 延焼2
    ])
    s = withEnergy(s, 99)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_ledger' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_perm_runfire' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_red_impulse' })
    const impulse = s.player.impulseUids
    expect(impulse.length).toBeGreaterThan(0)
    const hpBefore = s.enemies[0].hp
    const momentumBefore = s.player.momentum
    s = applyCommand(s, { type: 'PlayCard', cardUid: impulse[0] })
    expect(s.player.momentum).toBe(momentumBefore + 2) // 焔の目録 (2026-08-30 勢い三重掛けの是正で+4→+2。ユーザー許可)
    // 走り火の3ダメージ + 衝動札自身のダメージ (勢い込み) が入る
    expect(s.enemies[0].hp).toBeLessThanOrEqual(hpBefore - 3)
  })
})

describe('憤怒の即時性 (被弾がその場で火になる = 憤怒→猛り火の橋)', () => {
  it('業腹は直前の敵ターンに受けたダメージぶん延焼を積む', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    s = withIntent(s, attackIntent(9))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.damageTakenLastEnemyPhase).toBe(9)
    s = withHand(s, ['red_spite'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spite' })
    expect(s.enemies[0].burn).toBe(3 + 9) // 素の3 + 被弾9
    expect(isBlazing(s)).toBe(true) // 殴られた分がそのまま猛り火のしきい値に届く
  })

  it('ブロックで防いだ分は火にならない (HP損失のみ数える既存則と同じ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_guard'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' }) // ブロック4
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    s = withHand(s, ['red_spite'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spite' })
    expect(s.enemies[0].burn).toBe(3 + 6) // 10 - ブロック4
  })

  it('火だるまは殴られたその場で火を積む (敵フェーズ中に誘発する)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_perm_pyre',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_pyre' })
    s = withIntent(s, attackIntent(7))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].burn).toBeGreaterThanOrEqual(7)
  })
})

// --- 橋渡しモード札 (2026-08-30。「緑同様のモード呪文を赤にも」) ---
describe('赤のモード札 (アーキ分岐の入口)', () => {
  it('焚の岐路: 延焼5 か 勢い+3+3ダメ をプレイ時に選ぶ (バーン線⇄手数線)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_mode_crossroad',
    ])
    const burn = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_mode_crossroad', modeIndex: 0 })
    expect(burn.enemies[0].burn).toBe(5)
    const tempo = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_mode_crossroad', modeIndex: 1 })
    expect(tempo.player.momentum).toBe(3)
  })

  it('刻限の炎: どちらのモードでも消滅する (衝動4以上の消滅必須はカード単位)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_mode_deadline',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const a = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_mode_deadline', modeIndex: 1 })
    expect(a.enemies[0].burn).toBe(6)
    expect(a.player.exhaustPile.some((c) => c.def.id === 'red_mode_deadline')).toBe(true)
  })
})

describe('猛り火の点火瞬間 (2026-08-31 バーン縛りランの処方)', () => {
  it('ターン中にしきい値8を跨いだ瞬間、猛り火条件のターン開始置物が1回発火する (灰の外套)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    s = {
      ...s,
      player: {
        ...s.player,
        energy: 9,
        hand: [],
        permanents: [
          ...s.player.permanents,
          { uid: 'p_cloak', def: getCardDef('red_blaze_cloak') },
        ],
      },
    }
    s = withHand(s, ['red_ember', 'red_ember'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ember' }) // 延焼4 (未点火)
    expect(s.player.block).toBe(0)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_ember' }) // 延焼8 = 点火の瞬間
    expect(s.player.block).toBe(9) // 灰の外套が即座に発火
  })
})
