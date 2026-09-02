// 緑のシナジー網拡張 (2026-08-29) のテスト。
// 「成長以外のアーキを選べるようにシナジー設計してカードを増やす」(ユーザー) を受けた
// トランプル/ビッグマナの網 (エンジン→倍加→刈り取り→換金) 8枚と新効果2つを固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { getCardDef } from './content.ts'
import { attackIntent, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
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
    s = { ...s, player: { ...s.player, energyMax: 5, energyMaxAtTurnStart: 5, energy: 5 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trunk_blow' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5 * 4)
  })

  it('大幹の構え: 上限×3ダメ+上限×3ブロックの攻防一体 (2026-08-29 ×2→×3 典型上限5裁定)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_trunk_stance'])
    s = { ...s, player: { ...s.player, energyMax: 4, energyMaxAtTurnStart: 4, energy: 4 } }
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
    s = { ...s, player: { ...s.player, energyMax: 4, energyMaxAtTurnStart: 4, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_canopy_shade' })
    expect(s.player.block).toBe(4 * 2)
  })
})

describe('Xコスト増刷 (2026-08-29 ユーザー指示「ランプの攻撃防御吐き先としてあと3種」)', () => {
  it('蔦の連撃: 7ダメ×Xヒット (貫通なしの入口。成長が各ヒットに乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_vine_flurry'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 1 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_vine_flurry' })
    expect(s.player.energy).toBe(0)
    expect(s.enemies[0].hp).toBe(hpBefore - (7 + 1) * 3)
  })

  it('樹皮の重鎧: ブロック6×X (ランプ中の無防備への吐き先)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_x_bark_armor'])
    s = { ...s, player: { ...s.player, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_bark_armor' })
    expect(s.player.energy).toBe(0)
    expect(s.player.block).toBe(6 * 4)
  })

  it('森羅の大嵐: 敵全体に5ダメ×Xヒット (全体×多段×ランプの派手枠)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42), ['green_x_sylvan_tempest'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 2 } }
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_x_sylvan_tempest' })
    expect(s.enemies[0].hp).toBe(hp0 - (5 + 2) * 3) // 成長は対象ごと・ヒットごとに乗る
    expect(s.enemies[1].hp).toBe(hp1 - (5 + 2) * 3)
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

describe('読み勝ちの換金 (2026-08-29 面白さ5への処方②。確定済みルール表「読み勝ちの換金」)', () => {
  /** 伏せ札と眼光を仕込み、敵の攻撃に post窓で発動するところまで進める */
  function fireReaction(setCardId: string, withGaze: boolean): GameState {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, [])
    s = {
      ...s,
      player: {
        ...s.player,
        setCards: [{ uid: 'set0', def: getCardDef(setCardId) }],
        permanents: withGaze
          ? [...s.player.permanents, { uid: 'perm0', def: getCardDef('green_perm_hunters_gaze') }]
          : s.player.permanents,
      },
    }
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 } } : e)) }
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase !== 'awaiting-reaction') throw new Error(`post窓が開いていない: ${s.phase}`)
    return applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 'set0' })
  }

  it('狩人の眼光: リアクションが発動するたび成長+2 (onReactionFired)', () => {
    const s = fireReaction('green_reaction_thorns', true)
    expect(s.player.growth).toBe(2)
  })

  it('ブラフで伏せただけ (温存) では眼光は誘発しない', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, [])
    s = {
      ...s,
      player: {
        ...s.player,
        setCards: [{ uid: 'set0', def: getCardDef('green_reaction_thorns') }],
        permanents: [...s.player.permanents, { uid: 'perm0', def: getCardDef('green_perm_hunters_gaze') }],
      },
    }
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 } } : e)) }
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false }) // 温存
    expect(s.player.growth).toBe(0)
  })

  it('跳ね返りの蔦: 返し6+勢い+3。敵フェーズに得た勢いは次の自ターンまで持続する', () => {
    const s = fireReaction('green_reaction_rebound', false)
    // 発動後、敵フェーズが解決し次の自ターンが始まっている (勢いは自ターン終了時にしかリセットされない)
    expect(s.phase).toBe('player-turn')
    expect(s.player.momentum).toBe(3)
  })

  it('見切りの構え: 返し4+次のカード-2 (読み勝ちがマナに変換され翌ターンへ持ち越せる)', () => {
    const s = fireReaction('green_reaction_parry_stance', false)
    expect(s.player.nextCardDiscount).toBe(2)
  })
})

describe('倍化の増刷 (2026-08-29 ユーザー指示「成長・勢いの倍化カードを増やしてほしい」)', () => {
  it('株分け: 成長2倍+ブロック6・消滅 (守りながら倍加)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_division'])
    s = { ...s, player: { ...s.player, growth: 4, energy: 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_division' })
    expect(s.player.growth).toBe(8)
    expect(s.player.block).toBe(6)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toContain('green_division')
  })

  it('疾風の一撃: 勢い2倍→6ダメ (倍化後の勢いが乗る。勢い0でも6ダメ保証=空振りしない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_gale_strike'])
    s = { ...s, player: { ...s.player, momentum: 4, energy: 3 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_gale_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 + 8))
    let s2 = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_gale_strike'])
    s2 = { ...s2, player: { ...s2.player, energy: 3 } }
    const hp2 = s2.enemies[0].hp
    s2 = applyCommand(s2, { type: 'PlayCard', cardUid: 't0_green_gale_strike' })
    expect(s2.enemies[0].hp).toBe(hp2 - 6)
  })

})

describe('ランプ即時利用の廃止が上限参照札にも効く (2026-08-30 仕様違反の修正)', () => {
  // 計測ラン(seed3000)で発覚: ルール表は「上限増加は次の自ターンから」だが、実装は
  // エナジー補充にしか効いておらず、幹撃等が同ターンのランプを即座に数えていた。
  // 「今ランプするか今殴るか」の悩み=ランプの対価、を実装に揃える
  it('同ターンに撃ったランプは上限参照札に乗らない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [
      'green_ramp_sprout',
      'green_trunk_blow',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sprout' })
    expect(s.player.energyMax).toBe(4) // 上限自体は上がっている
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_trunk_blow' })
    expect(s.enemies[0].hp).toBe(hpBefore - 3 * 4) // ターン開始時の上限3で計算 (4×4=16ではない)
  })

  it('次の自ターンからは新しい上限で数える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_ramp_sprout'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sprout' })
    s = applyCommand(s, { type: 'EndTurn' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) {
      s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    }
    expect(s.player.energyMaxAtTurnStart).toBe(4) // 次ターン開始でスナップショット更新
  })
})

// --- 赤からの移管 (2026-08-30 カラーパイ再編 Phase 1) ---
// ユーザー判断「逆上は緑のカラーパイ / 粉砕は緑に渡したい」。逆上は素のまま渡すと
// 中立スターターで中央値2ダメ・57%が2以下と分散が極端なので、固定5の床を付けて渡した。
describe('赤からの移管: 被弾の換金と粉砕', () => {
  it('茨の報い: 固定5 + 直前の敵フェーズで受けたダメージ×1', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withIntent(s, attackIntent(8))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.damageTakenLastEnemyPhase).toBe(8)
    s = withHand(s, ['green_thorn_repay'])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_thorn_repay' })
    expect(s.enemies[0].hp).toBe(hpBefore - 13) // 床5 + 被弾8
  })

  it('茨の報い: 被弾0のターンでも床の5は出る (緑のスターターで腐らせない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_thorn_repay'])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_thorn_repay' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5)
  })

  it('根喰らいの蔓: 破壊した値をダメージに換金する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [])
    s = withIntent(s, defendIntent(10))
    s = applyCommand(s, { type: 'EndTurn' }) // 敵がブロック10を得る
    s = withHand(s, ['green_devour_vine'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_devour_vine' })
    expect(s.enemies[0].block).toBe(0)
    expect(s.enemies[0].hp).toBe(hpBefore - 15) // 破壊値10 + 基礎5
  })
})
