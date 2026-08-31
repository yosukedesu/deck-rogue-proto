// 黒の拡張 (2026-08-25 +19枚) のテスト。
// 確定済みルール表「コスト再利用」「消滅コスト」「消滅の誘発」「自傷の換金」を固定する。
// 黒の第5の柱: 払ったコストの再利用ハック (HP→背徳の収穫 / 手札→闇市の帳簿 / エナジー→死者再生+血の儀式)
import { describe, expect, it } from 'vitest'
import { effectiveCost } from './effects.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('消滅コスト (exhaustCost)', () => {
  it('供物の火: 手札1枚を消滅させて13ダメージ。指定なしはエラー', () => {
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
    // 2026-09-01 亡骸の面配布: コストで消滅した影の一撃の亡骸 (2ダメ) も発火する
    expect(s.enemies[0].hp).toBe(enemyHp - 13 - 2)
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
    // 忘却の刻 7→5 (2026-08-30) で合唱の強化 (1→3) がミルの途中から乗る。
    // 2026-08-31 プレイ札の消滅は解決後 (limbo) に変更: コスト1 → ミル4枚が置かれた後に
    // 誘発4回 (刻5到達済み=3×4) → 自身3 = 合計16。
    // 2026-09-01 亡骸の面配布: コスト消滅した影の一撃の亡骸2ダメが加わり18
    expect(s.enemies[0].hp).toBe(enemyHp - 18)
    // 消滅置き場: コスト1 + 墓暴き自身1 + 忘却4 = 6枚
    expect(s.player.exhaustPile).toHaveLength(6)
  })
})

describe('自傷の誘発と換金', () => {
  it('苦痛の芯: カード効果でHPを失うたび3ダメージ (2026-08-31 量2→3。影の刃で合計11)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_pain_core',
      'black_shadow_blade',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_pain_core' })
    const enemyHp = s.enemies[0].hp
    const hp = s.player.hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_shadow_blade' })
    // 影の刃: HP-2 (芯3ダメ) → 8ダメ
    expect(s.player.hp).toBe(hp - 2)
    expect(s.enemies[0].hp).toBe(enemyHp - 3 - 8)
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
    expect(s.player.selfHpLost).toBe(6) // 血の代償 HP-4→-6 (2026-08-31 許可済みナーフ)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_harvest_sin' })
    expect(s.enemies[0].hp).toBe(enemyHp - 24 - 6 * 2)
  })
})

describe('回復の誘発 (血の月)', () => {
  it('ドレインの回復で2ダメージが乗る。満タンの過剰回復でも誘発する (2026-08-31)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_moon',
      'black_drain',
      'black_drain',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_moon' })
    const enemyHp = s.enemies[0].hp
    // 満タン: 実回復0でも「回復した」として誘発する (満タン沈黙3割への処方)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_drain' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6 - 2) // 生命吸収6 + 血の月2
    // HPを減らした実回復でも当然誘発する
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_black_drain' })
    expect(s.enemies[0].hp).toBe(enemyHp - 8 - 6 - 2)
    expect(s.player.hp).toBe(53)
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
    // 2026-08-31 rework: 戻した札はこの戦闘中0E (亡骸との住み分け = 任意の札+テンポ)
    const back = s.player.hand.find((c) => c.uid === scythe.uid)!
    expect(back.freeThisCombat).toBe(true)
    expect(effectiveCost(s, back)).toBe(0)
    // 割引トークンも消費しない (素の0E札と同じ扱い)
    const energyBefore = s.player.energy
    s = { ...s, player: { ...s.player, nextCardDiscount: 1 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: scythe.uid, targetIndex: 0 })
    expect(s.player.energy).toBe(energyBefore)
    expect(s.player.nextCardDiscount).toBe(1)
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

  it('血の目覚め: 呪文プレイで起爆し、2枚消滅+3ダメージ (刻の前)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_awakening',
      'black_drain',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_awakening' })
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_drain' })
    // 生命吸収6 + 起爆 (忘却2 + 基礎3ダメ。消滅2枚では刻に届かない)
    expect(s.enemies[0].hp).toBe(enemyHp - 6 - 3)
    expect(s.player.exhaustPile).toHaveLength(2)
    expect(s.player.setCards).toHaveLength(0) // 起爆後は捨て札へ
  })
})

describe('ドレインの回復基準 (プレイテスト発見の不整合)', () => {
  it('弱体で与ダメが減ったら回復量も同じ基準で減る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_big_drain',
    ])
    s = { ...s, player: { ...s.player, hp: 30, weak: 2, energy: 9 } }
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_big_drain' })
    // 貪り喰らう16 → 弱体で floor(16*0.75)=12 ダメージ、回復も 12/2=6 (旧実装は素の値基準だった)
    expect(s.enemies[0].hp).toBe(enemyHp - 12)
    expect(s.player.hp).toBe(36)
  })
})

describe('引導 (2026-08-31 選択消滅。exhaustFromDeckChoose)', () => {
  it('山札から選んだ札が消滅し、亡骸 (onSelfExhausted) が発火する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_last_rites',
    ])
    // 山札の先頭に爆ぜる骸 (亡骸: 全体3ダメ) を仕込み、それを狙い撃ちで消滅させる
    const corpse = s.player.drawPile.find((c) => c.def.id === 'black_bursting_corpse')
    let target = corpse
    if (!target) {
      // スターターに無ければ手札から山札へ移して作る
      s = withHand(s, ['black_last_rites', 'black_bursting_corpse'])
      const inHand = s.player.hand.find((c) => c.def.id === 'black_bursting_corpse')!
      s = {
        ...s,
        player: {
          ...s.player,
          hand: s.player.hand.filter((c) => c.uid !== inHand.uid),
          drawPile: [inHand, ...s.player.drawPile],
        },
      }
      target = inHand
    }
    const enemyHp = s.enemies[0].hp
    const rites = s.player.hand.find((c) => c.def.id === 'black_last_rites')!
    s = applyCommand(s, { type: 'PlayCard', cardUid: rites.uid, deckUids: [target.uid] })
    expect(s.player.exhaustPile.some((c) => c.uid === target!.uid)).toBe(true)
    expect(s.player.drawPile.some((c) => c.uid === target!.uid)).toBe(false)
    // 爆ぜる骸の亡骸: 全体3ダメが発火している
    expect(s.enemies[0].hp).toBeLessThanOrEqual(enemyHp - 3)
  })

  it('deckUids 無しはエラー。山札も捨て札も空なら選択なしでプレイできる (ドローだけ解決)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_last_rites',
    ])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_last_rites' })).toThrow(
      /deckUids/,
    )
    // 両山を空にすると選択なしで通る (ドロー1だけ空振りせず解決...山札0なのでドローも0枚)
    s = { ...s, player: { ...s.player, drawPile: [], discardPile: [] } }
    const played = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_last_rites' })
    expect(played.player.discardPile.some((c) => c.def.id === 'black_last_rites')).toBe(true)
  })
})

describe('魂の薪 (2026-09-01 消滅時にエナジーを生む札)', () => {
  it('引導で狙い撃ちミル → 亡骸で一時マナ+1が自ターン中に即使える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_last_rites',
      'black_soul_kindling',
    ])
    const k = s.player.hand.find((c) => c.def.id === 'black_soul_kindling')!
    s = {
      ...s,
      player: {
        ...s.player,
        hand: s.player.hand.filter((c) => c.uid !== k.uid),
        drawPile: [k, ...s.player.drawPile],
      },
    }
    const rites = s.player.hand.find((c) => c.def.id === 'black_last_rites')!
    // エナジー3 → 引導1E で2 → 薪の亡骸+1 で3
    s = applyCommand(s, { type: 'PlayCard', cardUid: rites.uid, deckUids: [k.uid] })
    expect(s.player.energy).toBe(3)
    expect(s.player.exhaustPile.some((c) => c.uid === k.uid)).toBe(true)
  })

  it('プレイ経路: 1E払って一時マナ+2 (正味+1) で消滅。亡骸は発火しない (プレイ=仕事済み)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_soul_kindling',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_soul_kindling' })
    expect(s.player.energy).toBe(3 - 1 + 2) // 亡骸+1は乗らない
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_soul_kindling')).toBe(true)
  })
})

describe('骨刃 (2026-09-01 本家Shivの黒移植)', () => {
  it('骨刃の舞: 骨のナイフ3枚が手札に加わり、打つと4ダメ+消滅置き場へ (合唱が鳴る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_perm_chorus',
      'black_shiv_dance',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_chorus' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_shiv_dance' })
    const shivs = s.player.hand.filter((c) => c.def.id === 'black_shiv_token')
    expect(shivs).toHaveLength(3)
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: shivs[0].uid })
    // ナイフ4ダメ + 消滅で合唱1ダメ
    expect(s.enemies[0].hp).toBe(enemyHp - 4 - 1)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_shiv_token')).toBe(true)
  })

  it('急所読み (empowerShivs): ナイフの与ダメが常在で+3される (StS Accuracy)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_accuracy',
      'black_quick_shiv',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_accuracy' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_quick_shiv' })
    const shiv = s.player.hand.find((c) => c.def.id === 'black_shiv_token')!
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: shiv.uid })
    expect(s.enemies[0].hp).toBe(enemyHp - (4 + 3))
  })

  it('果てなき骨刃: 毎ターン開始にナイフ1枚 (置物)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_infinite_blades',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_infinite_blades' })
    s = withIntent(s, { kind: 'defend', shownMin: 0, shownMax: 0, actual: 0 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hand.some((c) => c.def.id === 'black_shiv_token')).toBe(true)
  })
})
