// 黒の拡張 (2026-08-25 +19枚) のテスト。
// 確定済みルール表「コスト再利用」「消滅コスト」「消滅の誘発」「自傷の換金」を固定する。
// 黒の第5の柱: 払ったコストの再利用ハック (HP→背徳の収穫 / 手札→闇市の帳簿 / エナジー→死者再生+血の儀式)
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('消滅コスト (exhaustCost)', () => {
  it('供物の火: 手札1枚を消滅させて11ダメージ。指定なしはエラー', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_offering',
      'black_strike',
    ])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_offering' })).toThrow()
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_black_offering',
      exhaustUids: ['t1_black_strike'],
    })
    expect(s.enemies[0].hp).toBe(enemyHp - 11)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_strike')).toBe(true)
    expect(s.player.hand).toHaveLength(0)
  })

  it('闇市の帳簿: 消滅コストを支払うたび1ドロー (払った手札が返ってくる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_ledger',
      'black_offering',
      'black_strike',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_ledger' })
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't1_black_offering',
      exhaustUids: ['t2_black_strike'],
    })
    // 手札3枚 → 帳簿・供物・コスト消滅で0枚 → 帳簿の1ドローで1枚
    expect(s.player.hand).toHaveLength(1)
  })
})

describe('消滅の誘発 (亡者の合唱)', () => {
  it('墓暴き: コスト1+自身1+忘却4 = 合唱が6回誘発して6ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_chorus',
      'black_grave_digger',
      'black_strike',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_chorus' })
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't1_black_grave_digger',
      exhaustUids: ['t2_black_strike'],
    })
    expect(s.enemies[0].hp).toBe(enemyHp - 6)
    // 消滅置き場: コスト1 + 墓暴き自身1 + 忘却4 = 6枚
    expect(s.player.exhaustPile).toHaveLength(6)
  })
})

describe('自傷の誘発と換金', () => {
  it('苦痛の芯: カード効果でHPを失うたび2ダメージ (影の刃で合計10)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_pain_core',
      'black_shadow_blade',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_pain_core' })
    const enemyHp = s.enemies[0].hp
    const hp = s.player.hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_shadow_blade' })
    // 影の刃: HP-2 (芯2ダメ) → 8ダメ
    expect(s.player.hp).toBe(hp - 2)
    expect(s.enemies[0].hp).toBe(enemyHp - 2 - 8)
    expect(s.player.selfHpLost).toBe(2)
  })

  it('背徳の収穫: この戦闘でカード効果により失ったHP×2ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_blood_price',
      'black_harvest_sin',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_blood_price' })
    expect(s.player.selfHpLost).toBe(4)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_harvest_sin' })
    expect(s.enemies[0].hp).toBe(enemyHp - 24 - 4 * 2)
  })
})

describe('回復の誘発 (血の月)', () => {
  it('ドレインの実回復で2ダメージが乗る。満タン時は誘発しない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_moon',
      'black_drain',
      'black_drain',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_moon' })
    const enemyHp = s.enemies[0].hp
    // 満タン: 回復0 → 血の月は誘発しない
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_drain' })
    expect(s.enemies[0].hp).toBe(enemyHp - 5)
    // HPを減らす → 実回復2 → 血の月2ダメ
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_black_drain' })
    expect(s.enemies[0].hp).toBe(enemyHp - 5 - 5 - 2)
    expect(s.player.hp).toBe(52)
  })
})

describe('コスト再利用 (死者再生・屍集め)', () => {
  it('死者再生: 消滅置き場の血の儀式をコストを支払わず直接プレイ (エナジーハック)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_raise_dead',
      'black_blood_rite',
    ])
    // 血の儀式を消滅置き場に細工
    const rite = s.player.hand.find((c) => c.def.id === 'black_blood_rite')!
    s = {
      ...s,
      player: {
        ...s.player,
        hand: s.player.hand.filter((c) => c.uid !== rite.uid),
        exhaustPile: [...s.player.exhaustPile, rite],
      },
    }
    const hp = s.player.hp
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_black_raise_dead',
      retrieveUid: rite.uid,
    })
    // エナジー: 3 - 再生2 + 儀式2 = 3。HP: 儀式の自傷-2
    expect(s.player.energy).toBe(3)
    expect(s.player.hp).toBe(hp - 2)
    // 儀式は消滅置き場に残る (燃料は減らない)。再生自身も消滅
    expect(s.player.exhaustPile.some((c) => c.uid === rite.uid)).toBe(true)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_raise_dead')).toBe(true)
    // 直接プレイも詠唱数に数える (再生1 + 儀式1)
    expect(s.player.cardsPlayedThisTurn).toBe(2)
  })

  it('死者再生: リアクションは直接プレイできない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_raise_dead',
      'black_reaction_curse',
    ])
    const curse = s.player.hand.find((c) => c.def.id === 'black_reaction_curse')!
    s = {
      ...s,
      player: {
        ...s.player,
        hand: s.player.hand.filter((c) => c.uid !== curse.uid),
        exhaustPile: [...s.player.exhaustPile, curse],
      },
    }
    expect(() =>
      applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_raise_dead', retrieveUid: curse.uid }),
    ).toThrow()
  })

  it('屍集め: 消滅置き場から1枚を手札に戻す (燃料は減る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_corpse_collect',
      'black_scythe',
    ])
    const scythe = s.player.hand.find((c) => c.def.id === 'black_scythe')!
    s = {
      ...s,
      player: {
        ...s.player,
        hand: s.player.hand.filter((c) => c.uid !== scythe.uid),
        exhaustPile: [...s.player.exhaustPile, scythe],
      },
    }
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_black_corpse_collect',
      retrieveUid: scythe.uid,
    })
    expect(s.player.hand.some((c) => c.uid === scythe.uid)).toBe(true)
    expect(s.player.exhaustPile.some((c) => c.uid === scythe.uid)).toBe(false)
  })
})

describe('黒の新リアクション', () => {
  it('死中の活: HP半分以下でのみ発動でき、20ダメ+10回復', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_last_stand',
    ])
    s = { ...s, player: { ...s.player, hp: 30 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_last_stand' })
    s = withIntent(s, attackIntent(6))
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBe(enemyHp - 20)
    expect(s.player.hp).toBe(30 - 6 + 10)
  })

  it('血の目覚め: 呪文プレイで起爆し、2枚消滅+消滅×1ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_awakening',
      'black_drain',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_awakening' })
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_drain' })
    // 生命吸収5 + 起爆 (忘却2 → 消滅2枚×1 = 2ダメ)
    expect(s.enemies[0].hp).toBe(enemyHp - 5 - 2)
    expect(s.player.exhaustPile).toHaveLength(2)
    expect(s.player.setCards).toHaveLength(0) // 起爆後は捨て札へ
  })
})
