// 緑のシナジー網拡張 (2026-08-29) のテスト。
// 「成長以外のアーキを選べるようにシナジー設計してカードを増やす」(ユーザー) を受けた
// トランプル/ビッグマナの網 (エンジン→倍加→刈り取り→換金) 8枚と新効果2つを固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { getCardDef } from './content.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

function withEnergy(s: GameState, energy: number): GameState {
  return { ...s, player: { ...s.player, energy } }
}

describe('トランプルの網', () => {
  it('荒野の呼び声: 毎ターン開始時に勢い+3 (エンジン)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_wild_call'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_wild_call' })
    expect(s.player.momentum).toBe(0) // 置いたターンはまだ
    s = applyCommand(s, { type: 'EndTurn' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) {
      s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    }
    expect(s.player.momentum).toBe(3) // 次の自ターン開始時に+3
  })

  it('三連の角: 5×3の貫通。勢いと成長が各ヒットに乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle', 42), ['green_triple_horn'])
    s = { ...s, player: { ...s.player, momentum: 2, growth: 1 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_triple_horn' })
    expect(s.enemies[0].hp).toBe(hpBefore - (5 + 2 + 1) * 3) // 貫通なのでブロック無視
    expect(s.enemies[0].block).toBe(10)
  })

  it('怒涛の突き上げ: 勢い×3ダメージ (勢いは消費せず、自身にも勢いが乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_surge_thrust'])
    s = { ...s, player: { ...s.player, momentum: 4 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_surge_thrust' })
    // 基礎 = 勢い4×3 = 12、そこに勢い4の加算も乗る = 16
    expect(s.enemies[0].hp).toBe(hpBefore - (4 * 3 + 4))
    expect(s.player.momentum).toBe(4) // 消費しない (ターン終了で自然に消える)
  })

  it('昂ぶる角笛: 勢い+2してから2倍・消滅 (2026-08-29 検証ランで空振り腐りが出たため+2を前置)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_flare'])
    s = { ...s, player: { ...s.player, momentum: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_flare' })
    expect(s.player.momentum).toBe((5 + 2) * 2)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toContain('green_horn_flare')
    // 勢い0で引いても最低4が立つ = 空振りしない
    let s2 = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_flare'])
    s2 = applyCommand(s2, { type: 'PlayCard', cardUid: 't0_green_horn_flare' })
    expect(s2.player.momentum).toBe(4)
  })

  it('荒角の構え: ブロック6+勢い+3 (トランプルの受け=守りが攻めの準備になる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [
      'green_horn_stance',
      'green_strike',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_stance' })
    expect(s.player.block).toBe(6)
    expect(s.player.momentum).toBe(3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 + 3)) // 防御札が後続の攻撃を+3する
  })
})

describe('Xコスト: 大角の暴走 (トリプルブリッジ。確定済みルール表「Xコスト」)', () => {
  it('現在の全エナジーを支払い、Xヒット×6の貫通で解決される (成長・勢いが各ヒットに乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle', 42), ['green_x_stampede'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 2, momentum: 1 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_stampede' })
    expect(s.player.energy).toBe(0) // 全部支払う
    expect(s.enemies[0].hp).toBe(hpBefore - (6 + 2 + 1) * 3) // 9×3ヒット・貫通
    expect(s.enemies[0].block).toBe(10)
  })

  it('エナジー0ではプレイできない。割引 (次のカード-1) の対象外で消費もしない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_stampede'])
    s = { ...s, player: { ...s.player, energy: 0 } }
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_stampede' })).toThrow(
      /エナジー不足/,
    )
    let s2 = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_stampede'])
    s2 = { ...s2, player: { ...s2.player, energy: 2, nextCardDiscount: 1 } }
    s2 = applyCommand(s2, { type: 'PlayCard', cardUid: 't0_green_x_stampede' })
    expect(s2.player.energy).toBe(0) // X=2 (割引は効かない)
    expect(s2.player.nextCardDiscount).toBe(1) // 消費もしない
  })
})

describe('ビッグマナの網', () => {
  it('幹撃: エナジー上限×4ダメージ (中型ペイオフ。2026-08-29 ×3→×4 典型上限5裁定)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_trunk_blow'])
    s = { ...s, player: { ...s.player, energyMax: 5, energy: 5 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trunk_blow' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5 * 4)
  })

  it('大幹の構え: 上限×3ダメ+上限×3ブロックの攻防一体 (2026-08-29 ×2→×3 典型上限5裁定)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_trunk_stance'])
    s = { ...s, player: { ...s.player, energyMax: 4, energy: 4 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trunk_stance' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4 * 3)
    expect(s.player.block).toBe(4 * 3)
  })

  it('大樹の脈: 毎ターン開始時に次のカード-1 (らいこパッシブと同機構)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_tree_pulse'])
    s = withEnergy(s, 3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_tree_pulse' })
    s = applyCommand(s, { type: 'EndTurn' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) {
      s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    }
    expect(s.player.nextCardDiscount).toBe(1) // 次の自ターン開始時に割引1
  })

  it('木陰の守り: 上限×2ブロック (巨木の盾の小型ラダー)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_canopy_shade'])
    s = { ...s, player: { ...s.player, energyMax: 4, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_canopy_shade' })
    expect(s.player.block).toBe(4 * 2)
  })
})

describe('Xコスト増刷 (2026-08-29 ユーザー指示「ランプの攻撃防御吐き先としてあと3種」)', () => {
  it('蔦の連撃: 4ダメ×Xヒット (貫通なしの入口。成長が各ヒットに乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_vine_flurry'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 1 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_vine_flurry' })
    expect(s.player.energy).toBe(0)
    expect(s.enemies[0].hp).toBe(hpBefore - (4 + 1) * 3)
  })

  it('樹皮の重鎧: ブロック6×X (ランプ中の無防備への吐き先)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_bark_armor'])
    s = { ...s, player: { ...s.player, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_bark_armor' })
    expect(s.player.energy).toBe(0)
    expect(s.player.block).toBe(6 * 4)
  })

  it('森羅の大嵐: 敵全体に3ダメ×Xヒット (全体×多段×ランプの派手枠)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42), ['green_x_sylvan_tempest'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 2 } }
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_sylvan_tempest' })
    expect(s.enemies[0].hp).toBe(hp0 - (3 + 2) * 3) // 成長は対象ごと・ヒットごとに乗る
    expect(s.enemies[1].hp).toBe(hp1 - (3 + 2) * 3)
  })
})

describe('モード札増刷 (2026-08-29 ユーザー指示「モード系は魅力的なのでもっと刷っていい」)', () => {
  it('森の裁定: 《牙》14ダメ /《殻》ブロック14 (絡み蔦のラダー)', () => {
    const mk = () => {
      let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_mode_verdict'])
      return { ...s, player: { ...s.player, energy: 3 } }
    }
    const fang = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_verdict', modeIndex: 0 })
    expect(fang.enemies[0].hp).toBe(mk().enemies[0].hp - 14)
    const shell = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_verdict', modeIndex: 1 })
    expect(shell.player.block).toBe(14)
  })

  it('道行きの選択: 《野生》勢い+3+3ダメ /《育成》成長+2 (アーキ分岐)', () => {
    const mk = () => withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_mode_crossroads'])
    const wild = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_crossroads', modeIndex: 0 })
    expect(wild.player.momentum).toBe(3)
    const nurture = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_crossroads', modeIndex: 1 })
    expect(nurture.player.growth).toBe(2)
  })

  it('大樹の岐路: 《天光》上限+1+2ドロー /《豊穣》成長+3+ブロック6。片モードランプなので消滅しない (陽光の恵み裁定)', () => {
    expect(getCardDef('green_mode_great_fork').exhaust).not.toBe(true)
    const mk = () => {
      let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_mode_great_fork'])
      return { ...s, player: { ...s.player, energy: 3 } }
    }
    const light = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_great_fork', modeIndex: 0 })
    expect(light.player.energyMax).toBe(4)
    expect(light.player.energy).toBe(0) // 上限は次ターンから (ランプ即時利用廃止)
    const bounty = applyCommand(mk(), { type: 'PlayCard', cardUid: 't0_green_mode_great_fork', modeIndex: 1 })
    expect(bounty.player.growth).toBe(3)
    expect(bounty.player.block).toBe(6)
  })
})
