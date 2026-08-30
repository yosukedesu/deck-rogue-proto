// プレイテスト由来の修正 (2026-08-25) のテスト。
// 確定済みルール表「条件付き意図」「致死時の誘発」「がらくた」を固定する。
import { describe, expect, it } from 'vitest'
import { effectiveIntent } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('条件付き意図 (敵の反応が実行時の盤面で決まる)', () => {
  it('罠壊しは「伏せがあれば伏せ破壊 / なければ通常行動」の両分岐を宣言時に持つ', () => {
    const s = freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red')
    const intent = s.enemies[0].intent!
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt).toBeDefined()
    // 伏せが無い今は本体の分岐が有効
    expect(effectiveIntent(s, 0)!.kind).toBe(intent.kind)
  })

  it('同じターンに伏せると、その行動が即座に反応テーブル側へ切り替わる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter'), [
      'green_reaction_thorns',
    ])
    const before = effectiveIntent(s, 0)!.kind
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    const after = effectiveIntent(s, 0)!
    // alt (反応テーブル) 側に切り替わっている = 「今から伏せて誘導」が成立する
    expect(after.kind).toBe(s.enemies[0].intent!.alt!.kind)
    void before
  })
})

describe('致死時の誘発 (回復付きの返し札で生き延びる)', () => {
  it('致死ダメージでも被攻撃後の確認窓が開き、ドレイン返しで生存できる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_last_stand',
    ])
    s = { ...s, player: { ...s.player, hp: 8 } } // HP半分以下 = 死中の活の条件を満たす
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_last_stand' })
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    s = applyCommand(s, { type: 'EndTurn' })
    // HPは0以下になっているが、窓が開いている
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // 死中の活: 20ダメ + 10回復 → HP-2 から生還
    expect(s.player.hp).toBeGreaterThan(0)
    expect(s.phase).not.toBe('lost')
  })

  it('温存を選べばそのまま敗北になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_last_stand',
    ])
    s = { ...s, player: { ...s.player, hp: 8 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_last_stand' })
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.phase).toBe('lost')
  })
})

describe('がらくた (罠壊しの第2の特徴)', () => {
  it('山札に混ざり、使用不可の死に札として手札を圧迫する', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red')
    s = withIntent(s, {
      kind: 'attack',
      shownMin: 5,
      shownMax: 7,
      actual: 6,
      inflict: { status: 'junk', amount: 2 },
    })
    const drawBefore = s.player.drawPile.length
    s = applyCommand(s, { type: 'EndTurn' })
    const junkInDeck = [...s.player.drawPile, ...s.player.hand].filter(
      (c) => c.def.id === 'status_junk',
    )
    expect(junkInDeck.length).toBe(2)
    expect(s.player.drawPile.length + s.player.hand.length).toBeGreaterThan(drawBefore)
    expect(s.eventLog.some((e) => e.type === 'StatusInflicted' && e.status === 'junk')).toBe(true)
  })
})

describe('弱体の下限 (チップダメージが消えない)', () => {
  it('弱体中でも1ダメージの置物効果は0にならない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
    ])
    s = { ...s, player: { ...s.player, weak: 2 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    const hpBefore = s.enemies[0].hp
    s = withIntent(s, { kind: 'attack', shownMin: 3, shownMax: 3, actual: 3 })
    s = applyCommand(s, { type: 'EndTurn' })
    // 従者の2ダメは floor(2*0.75)=1 に減るが 0 にはならない
    expect(s.enemies[0].hp).toBe(hpBefore - 1)
  })
})

describe('致死時の窓の絞り込み (2026-08-26)', () => {
  it('回復を伴わない返し札しかない致死状態では、確認窓を開かず即敗北になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), [
      'green_reaction_thorns', // 茨の返し: 返し9のみ・回復なし
    ])
    s = { ...s, player: { ...s.player, hp: 5 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, { kind: 'attack', shownMin: 12, shownMax: 12, actual: 12 })
    s = applyCommand(s, { type: 'EndTurn' })
    // 「もう詰んでいるのに確認が出る」を防ぐ = 窓を開かず lost へ
    expect(s.phase).toBe('lost')
  })

  it('回復量が不足分に届く返し札があれば窓が開く', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_curse', // 呪詛返し: ドレイン6 = 回復3
    ])
    s = { ...s, player: { ...s.player, hp: 5 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_curse' })
    s = withIntent(s, { kind: 'attack', shownMin: 7, shownMax: 7, actual: 7 })
    s = applyCommand(s, { type: 'EndTurn' }) // HP-2。回復3で1に届く = 救える
    expect(s.phase).toBe('awaiting-reaction')
  })

  it('回復量が不足分に届かない札では窓を開かない (2026-08-31 黒Opusラン指摘: HP-27に回復3を期待させて殺した)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_curse', // 回復3では HP-9 (不足10) を救えない
    ])
    s = { ...s, player: { ...s.player, hp: 5 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_curse' })
    s = withIntent(s, { kind: 'attack', shownMin: 14, shownMax: 14, actual: 14 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('lost')
  })
})

describe('見切りと回収 (2026-08-30 A2。伏せの概念監査)', () => {
  it('置きっぱなしの伏せ札は敵の分岐を変えない (蓋の対処 = 敵は新しい札にだけ反応する)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter'), [
      'green_reaction_thorns',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    // 伏せた直後 = 新しい札なので alt (反応テーブル) 側
    expect(effectiveIntent(s, 0)!.kind).toBe(s.enemies[0].intent!.alt!.kind)
    // 発動せず敵の防御行動を素通しして次ターンへ → 置きっぱなし = 織り込み済み
    s = withIntent(s, { kind: 'defend', shownMin: 1, shownMax: 1, actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.setCards).toHaveLength(1)
    expect(s.player.setCards[0].setFresh).toBe(false)
    const intent = s.enemies[0].intent!
    if (intent.conditionalOn === 'set' && intent.alt && intent.alt.kind !== 'destroy-set') {
      // 敵の伏せ反応は基準側 (伏せなし) のまま = 蓋にならない
      expect(effectiveIntent(s, 0)!.kind).toBe(intent.kind)
    }
  })

  it('破壊 (destroy-set) の判定だけは鮮度を問わない (晒し続けた札は壊されには行かれる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter'), [
      'green_reaction_thorns',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, { kind: 'defend', shownMin: 1, shownMax: 1, actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' }) // 札は古くなる
    // 破壊分岐の条件付き意図を細工: alt が destroy-set なら古い札でも「あり」側
    s = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? {
              ...e,
              intent: {
                kind: 'attack',
                shownMin: 5,
                shownMax: 5,
                actual: 5,
                conditionalOn: 'set',
                alt: { kind: 'destroy-set', shownMin: 0, shownMax: 0, actual: 0 },
              },
            }
          : e,
      ),
    }
    expect(effectiveIntent(s, 0)!.kind).toBe('destroy-set')
  })

  it('回収は自ターンのみ・1E必要 (エナジー0では回収できない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), [
      'green_reaction_thorns',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = { ...s, player: { ...s.player, energy: 0 } }
    expect(() =>
      applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_reaction_thorns' }),
    ).toThrow(/エナジー不足/)
  })
})

describe('回収ターンの伏せ直し0E (2026-08-30 死に機構への処方)', () => {
  it('回収した同じ札は、そのターン中なら0Eで伏せ直せる (実質1Eの伏せ替え)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), [
      'green_reaction_thorns',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    const e0 = s.player.energy
    s = applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_reaction_thorns' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.energy).toBe(e0 - 1) // 回収の1Eだけ。伏せ直しは無料
    expect(s.player.setCards[0].setFresh).toBe(true) // 伏せ直しは「新しい札」= 敵は反応する
  })
})

describe('盗んだ敵は次の宣言で必ず逃走 (2026-08-30。1ターン以内に倒せのレース)', () => {
  it('stolenGold を抱えた敵の次の意図は flee で固定される', () => {
    let s = freshCombat('set-confirm', 'enemy_thief', 42)
    s = {
      ...s,
      enemies: s.enemies.map((e) => ({ ...e, stolenGold: 20 })),
    }
    s = withIntent(s, { kind: 'defend', shownMin: 1, shownMax: 1, actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'player-turn') {
      expect(s.enemies[0].intent?.kind).toBe('flee')
    }
  })
})
