// 青の解凍 (2026-09-25 ユーザー裁定「全部推奨」・docs/blue-thaw-proposal-2026-09-25.md・card-power.md §90) を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, allLeaders, getCardDef, getDeckDef } from './content.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { applyCommand, createInitialState } from './state.ts'
import { attackIntent, freshCombat, passTurn, setAndArm, withHand, withIntent } from './test-helpers.ts'
import { upgradeCard } from './upgrade.ts'
import { fuseCards } from './fusion.ts'
import { isTrapLive } from './effects.ts'
import type { CardInstance, GameState } from './types.ts'

const up = (id: string) => upgradeCard({ uid: 'u', def: getCardDef(id) }).def
const inst = (id: string, n = 0): CardInstance => ({ uid: `f${n}_${id}`, def: getCardDef(id) })
const fireReaction = (s: GameState, attack: number): GameState => {
  let t = applyCommand(withIntent(s, attackIntent(attack)), { type: 'EndTurn' })
  if (t.phase === 'awaiting-reaction') t = applyCommand(t, { type: 'ConfirmReaction', fire: true })
  return t
}

describe('初期デッキとみぞれ', () => {
  it('ラン基本デッキ＝水撃3・渦流の鞭・氷盾3・潮の岐路・知恵の重み・対抗呪文 (2026-11-04 ユーザー裁定「緑・白と同じような構成に」: 潮の一滴→潮の岐路)。潮の一滴・氷の槍・思案・霜の帳は報酬プールへ', () => {
    const deck = getDeckDef('run_basic_blue')
    const ids = deck.cards.flatMap((c) => Array.from({ length: c.count }, () => c.cardId)).sort()
    expect(ids).toEqual(['blue_counterspell', 'blue_current_lash', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_mode_tide_crossroad', 'blue_strike', 'blue_strike', 'blue_strike', 'blue_weight_of_wisdom'])
    for (const id of ids) expect(REWARD_EXCLUDED.has(id), id).toBe(true)
    for (const id of ['blue_tide_drop', 'blue_ice_lance', 'blue_ponder', 'blue_frost_veil']) expect(REWARD_EXCLUDED.has(id), id).toBe(false)
    const blue = allCards.filter((c) => c.color === 'blue')
    expect(blue.length).toBe(96) // 2026-09-25 3本柱: +13 −5 → 95。2026-11-04 潮の岐路 +1
    expect(blue.filter((c) => !REWARD_EXCLUDED.has(c.id)).length).toBe(90) // 89＋潮の一滴 (初期デッキから報酬へ)。潮の岐路は初期専用
  })

  it('潮の岐路 (2026-11-04 新・初期専用): 1E 呪文のコモン。モードは「7ダメージ」か「氷壁4」', () => {
    const d = getCardDef('blue_mode_tide_crossroad')
    expect(d).toMatchObject({ cost: 1, type: 'spell', rarity: 'common', color: 'blue' })
    expect(d.modes?.map((m) => m.effects.map((e) => [e.effect, e.amount]))).toEqual([[['dealDamage', 7]], [['gainIceBlock', 4]]])
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_mode_tide_crossroad', 'blue_mode_tide_crossroad'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hp0 = s.enemies[0].hp
    const ice0 = s.player.iceBlock
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_mode_tide_crossroad', targetIndex: 0, modeIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(7)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_mode_tide_crossroad', modeIndex: 1 })
    expect(s.player.iceBlock - ice0).toBe(4)
  })

  it('みぞれは仕込み枠2・説明文は今の効果 (呪文を撃つたび氷壁+1)', () => {
    const miz = allLeaders.find((l) => l.id === 'leader_blue')!
    expect(miz.setSlots).toBe(2)
    expect(miz.description).toContain('呪文を出すたび氷壁+1')
    expect(miz.description).not.toContain('毎ターン開始時に氷壁+2')
    const s = applyCommand(createInitialState(42, 'set-confirm'), { type: 'StartCombat', seed: 42, enemyId: 'enemy_brute', deckId: 'run_basic_blue', leaderId: 'leader_blue' })
    expect(s.player.setSlots).toBe(2)
  })
})

describe('罠モデルの当てはめ', () => {
  it('霜の帳: この敵フェーズを完全に凌いだら霊気+2 (凌げなければ霊気1のまま)', () => {
    let s = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_frost_veil']), 't0_blue_frost_veil')
    const a = fireReaction(s, 5)
    expect(a.player.aether).toBe(3)
    s = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_frost_veil']), 't0_blue_frost_veil')
    const b = fireReaction(s, 30)
    expect(b.player.aether).toBe(1)
  })

  it('冷徹な観察: 受けた攻撃の実値が10以上ならさらに霊気+2', () => {
    let s = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_cold_reading']), 't0_blue_cold_reading')
    expect(fireReaction(s, 12).player.aether).toBe(4)
    s = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_cold_reading']), 't0_blue_cold_reading')
    expect(fireReaction(s, 6).player.aether).toBe(2)
  })

  it('白波の壁: 完全に凌いだら反復1 (次の自ターンの最初の呪文が2回)', () => {
    let s = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_reaction_surge']), 't0_blue_reaction_surge')
    s = fireReaction(s, 8)
    expect(s.phase).toBe('player-turn')
    expect(s.player.spellEchoes).toBe(1)
    let t = setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_reaction_surge']), 't0_blue_reaction_surge')
    t = fireReaction(t, 40)
    expect(t.player.spellEchoes).toBe(0)
  })

  it('魔力盗みは期限なし (4ターン置いても生きている)', () => {
    expect(getCardDef('blue_spell_steal').trapPersist).toBe(true)
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_spell_steal'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_spell_steal' })
    for (let i = 0; i < 4; i++) s = passTurn(s)
    expect(s.player.setCards).toHaveLength(1)
    expect(isTrapLive(s, s.player.setCards[0])).toBe(true)
  })
})

describe('鍛えるの本家形 (青) と例外', () => {
  it('本家形: 知恵の重み ×2→×3・水撃 8ダメ+2ドロー', () => {
    expect(up('blue_weight_of_wisdom').effects).toEqual([{ trigger: 'onPlay', effect: 'dealDamagePerHandCard', amount: 3 }])
    expect(up('blue_strike').effects.map((e) => [e.effect, e.amount])).toEqual([['dealDamage', 8], ['drawCards', 2]])
  })

  it('例外: 何でも消せる打ち消しはコスト-1 (霊気は伸びない。凍る静寂は 2026-09-25 に撤去)', () => {
    for (const id of ['blue_counterspell', 'blue_spell_steal']) {
      const d = up(id)
      expect(d.cost, id).toBe(getCardDef(id).cost - 1)
      expect(d.effects, id).toEqual(getCardDef(id).effects)
    }
  })

  it('例外: 条件つき打ち消しは範囲を広げる (マナ漏出 15以下→20以下・逆巻き 12以上→8以上)', () => {
    expect(up('blue_mana_leak').effects.every((e) => e.condition?.maxActionValue === 20)).toBe(true)
    expect(up('blue_mana_leak').cost).toBe(1)
    expect(up('blue_undertow').effects.every((e) => e.condition?.minActionValue === 8)).toBe(true)
    expect(up('blue_undertow').cost).toBe(1)
  })

  it('例外: 潮汐の書見台・懐深き外套はコスト-1 (毎Tの生成は伸びない)', () => {
    expect(up('blue_perm_tidal_lectern')).toMatchObject({ cost: 1, effects: getCardDef('blue_perm_tidal_lectern').effects })
    expect(up('blue_perm_deep_cloak')).toMatchObject({ cost: 1, effects: getCardDef('blue_perm_deep_cloak').effects })
  })

  it('旧3段のまま: 氷の槍 (2Eのまま・氷壁4を足してから撃つ＝1E 化は 2026-08-31 に封じた)・魔力の火花 (次のカード-3)', () => {
    expect(up('blue_ice_lance')).toMatchObject({ cost: 2 })
    expect(up('blue_ice_lance').effects.map((e) => [e.effect, e.amount])).toEqual([['gainIceBlock', 3], ['dealDamagePerIceBlock', 1]])
    expect(up('blue_spark').effects).toEqual([{ trigger: 'onPlay', effect: 'discountNext', amount: 3 }])
  })
})

describe('工房: 触媒2とレシピ6', () => {
  it('谺の雫 (echo)・栞の氷 (retain) はコモンの触媒', () => {
    expect(getCardDef('blue_catalyst_echo')).toMatchObject({ cost: 1, rarity: 'common', fusionCatalyst: 'echo' })
    expect(getCardDef('blue_catalyst_retain')).toMatchObject({ cost: 1, rarity: 'common', fusionCatalyst: 'retain' })
    expect(fuseCards(inst('blue_rapid_strike'), inst('blue_catalyst_echo', 1)).echo).toBe(true)
    expect(fuseCards(inst('blue_rapid_strike'), inst('blue_catalyst_retain', 1)).retain).toBe(true)
  })

  it('レシピ6件が青の素材で出る', () => {
    const pairs: ReadonlyArray<[string, string, string]> = [
      ['blue_strike', 'blue_guard', '潮の構え'],
      ['blue_weight_of_wisdom', 'blue_tome_shield', '読書家の構え'],
      ['blue_counterspell', 'blue_perm_vortex_ring', '渦潮の見張り'],
      ['blue_weight_of_wisdom', 'blue_ponder', '沈思の一撃'],
      ['blue_counterspell', 'blue_undertow', '深き逆潮'],
      ['blue_ice_lance', 'blue_thick_ice', '氷山崩し'],
    ]
    for (const [a, b, name] of pairs) expect(fuseCards(inst(a), inst(b, 1)).name, `${a}×${b}`).toBe(name)
    expect(fuseCards(inst('blue_counterspell'), inst('blue_undertow', 1)).trapPersist).toBe(true)
    expect(fuseCards(inst('blue_ice_lance'), inst('blue_thick_ice', 1)).retain).toBe(true)
  })
})
