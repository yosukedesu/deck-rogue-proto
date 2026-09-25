// 青の3本柱 (2026-09-25 ユーザー裁定。docs/blue-archetypes-proposal-2026-09-25.md・card-power.md §91):
// 罠使い (仕込んで鳴らして霊気で刈る) と 潮読み (占術で読んで抱えて放つ) の新しい仕組みを機械固定
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { deckChoosePool } from './combat.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, passTurn, setAndArm, withHand, withIntent } from './test-helpers.ts'
import { effectiveCost, isTrapLive, trapStatusText, trapWindowsLeft } from './effects.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, n: number): CardInstance => ({ uid: `d${n}_${id}`, def: getCardDef(id) })
const withDraw = (s: GameState, ids: readonly string[]): GameState => ({ ...s, player: { ...s.player, drawPile: ids.map((id, i) => inst(id, i)), discardPile: [] } })
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n } })
const perm = (s: GameState, id: string): GameState => ({ ...s, player: { ...s.player, permanents: [...s.player.permanents, { uid: `p_${id}`, def: getCardDef(id) }] } })
const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState => applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const base = (hand: readonly string[]) => energy(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), hand), 9)

describe('潮読み: 占術', () => {
  it('波見の一撃=7ダメ＋占術3: 選ぶまで他の操作はできず、選んだ札だけ捨て札へ (残りは山札の並びのまま)', () => {
    let s = withDraw(base(['blue_wave_read_strike', 'blue_strike']), ['blue_guard', 'status_wound', 'blue_ponder', 'blue_tide_drop'])
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_blue_wave_read_strike')
    expect(hp0 - s.enemies[0].hp).toBe(7)
    expect(s.pendingScry).toEqual({ count: 3, times: 1 })
    expect(() => play(s, 't1_blue_strike')).toThrow('占術')
    expect(() => applyCommand(s, { type: 'ResolveScry', discardUids: ['d3_blue_tide_drop'] })).toThrow('占術で見ている札ではない')
    s = applyCommand(s, { type: 'ResolveScry', discardUids: ['d1_status_wound'] })
    expect(s.pendingScry).toBeUndefined()
    expect(s.player.drawPile.map((c) => c.def.id)).toEqual(['blue_guard', 'blue_ponder', 'blue_tide_drop'])
    expect(s.player.discardPile.map((c) => c.def.id)).toContain('status_wound')
    expect(s.player.scriedThisCombat).toBe(3)
    expect(s.eventLog.some((e) => e.type === 'Scried')).toBe(true)
  })

  it('読み切り=この戦闘で占術で見た枚数×1。渦見の鏡=占術するたび氷壁3', () => {
    let s = perm(withDraw(base(['blue_foresight_shield', 'blue_read_through']), ['blue_guard', 'blue_guard', 'blue_guard', 'blue_guard']), 'blue_perm_eddy_mirror')
    const ice0 = s.player.iceBlock
    s = play(s, 't0_blue_foresight_shield')
    s = applyCommand(s, { type: 'ResolveScry', discardUids: [] })
    expect(s.player.iceBlock - ice0).toBe(6 + 3)
    const hp0 = s.enemies[0].hp
    s = play(s, 't1_blue_read_through')
    expect(hp0 - s.enemies[0].hp).toBe(3)
  })

  it('山札が空なら占術は保留しない (空の窓を出さない)', () => {
    let s = withDraw(base(['blue_wave_read_strike']), [])
    s = play(s, 't0_blue_wave_read_strike')
    expect(s.pendingScry).toBeUndefined()
  })

  it('潮見の塔=ターン開始 (手札を引いた後) に占術3', () => {
    let s = perm(base([]), 'blue_perm_tide_tower')
    s = withDraw(s, ['blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'status_wound', 'blue_strike', 'blue_strike'])
    s = applyCommand(withIntent(s, attackIntent(1)), { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn')
    expect(s.pendingScry).toEqual({ count: 3, times: 1 })
  })
})

describe('潮読み: 満ち潮の書庫と潮見の大波', () => {
  it('満ち潮の書庫=ターン終了時に選んだ手札を4枚まで残す (書庫が無い・5枚は拒否)', () => {
    let s = base(['blue_strike', 'blue_guard', 'blue_ponder', 'blue_tide_drop', 'blue_rapid_strike'])
    expect(() => applyCommand(s, { type: 'EndTurn', retainUids: ['t0_blue_strike'] })).toThrow('0枚まで')
    s = perm(s, 'blue_perm_flood_archive')
    expect(() => applyCommand(s, { type: 'EndTurn', retainUids: ['t0_blue_strike', 't1_blue_guard', 't2_blue_ponder', 't3_blue_tide_drop', 't4_blue_rapid_strike'] })).toThrow('4枚まで')
    s = applyCommand(withIntent(s, attackIntent(1)), { type: 'EndTurn', retainUids: ['t0_blue_strike', 't2_blue_ponder'] })
    expect(s.phase).toBe('player-turn')
    expect(s.player.hand.map((c) => c.uid)).toEqual(expect.arrayContaining(['t0_blue_strike', 't2_blue_ponder']))
    expect(s.player.hand.some((c) => c.uid === 't1_blue_guard')).toBe(false)
    expect(s.retainUids).toBeUndefined()
  })

  it('潮見の大波=手札×2を敵全体', () => {
    let s = energy(withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_blue'), ['blue_tide_surge', 'blue_strike', 'blue_strike', 'blue_guard']), 9)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_blue_tide_surge')
    expect(s.enemies.map((e, i) => hp[i] - e.hp)).toEqual([6, 6])
  })
})

describe('罠使い', () => {
  it('仕掛けの反響=4＋この戦闘で罠が鳴った回数×2。からくり時計=罠が鳴るたび霊気+1', () => {
    let s = perm(setAndArm(base(['blue_frost_veil']), 't0_blue_frost_veil'), 'blue_perm_trap_clock')
    s = applyCommand(withIntent(s, attackIntent(30)), { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.trapsFiredThisCombat).toBe(1)
    expect(s.player.aether).toBe(1 + 1) // 霜の帳の霊気1 + からくり時計1
    s = energy(withHand(s, ['blue_trap_echo']), 9)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_blue_trap_echo')
    expect(hp0 - s.enemies[0].hp).toBe(4 + 2)
  })

  it('潮待ち=仕込んでいる罠の期限+1 (3回の敵ターンを生きる)。仕込み直すと延長は消える', () => {
    let s = base(['blue_mana_leak', 'blue_tide_wait'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_mana_leak' })
    s = play(s, 't1_blue_tide_wait')
    const trap = () => s.player.setCards[0]
    expect(trap().trapLifeBonus).toBe(1)
    expect(trapWindowsLeft(s, trap())).toBe(3)
    for (let i = 0; i < 3; i++) {
      s = passTurn(s)
      expect(s.player.setCards).toHaveLength(1)
      expect(isTrapLive(s, trap())).toBe(true)
    }
    expect(trapStatusText(s, trap())).toContain('あと1回')
    s = passTurn(s)
    expect(s.player.setCards).toHaveLength(0)
  })

  it('ほどけ泡=罠が期限切れになるたび霊気+2と次のターン1ドロー', () => {
    let s = perm(base(['blue_mana_leak']), 'blue_perm_unravel_foam')
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_mana_leak' })
    for (let i = 0; i < 3; i++) s = passTurn(s) // 準備・窓1・窓2 の3ターン目の終わりに期限切れ
    expect(s.player.setCards).toHaveLength(0)
    expect(s.eventLog.some((e) => e.type === 'SetCardExpired')).toBe(true)
    expect(s.player.aether).toBe(2)
  })

  it('深き仕掛け=罠は期限切れにならない', () => {
    let s = perm(base(['blue_mana_leak']), 'blue_perm_deep_trap')
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_mana_leak' })
    for (let i = 0; i < 5; i++) s = passTurn(s)
    expect(s.player.setCards).toHaveLength(1)
    expect(trapStatusText(s, s.player.setCards[0])).toBe('期限なし')
  })

  it('仕掛けの手配=山札から罠だけを選んで手札へ＋霊気2', () => {
    let s = withDraw(base(['blue_trap_fetch']), ['blue_strike', 'blue_mana_leak', 'blue_guard', 'blue_undertow'])
    expect(deckChoosePool(s, getCardDef('blue_trap_fetch')).map((c) => c.def.id)).toEqual(['blue_mana_leak', 'blue_undertow'])
    expect(() => play(s, 't0_blue_trap_fetch', { deckUids: ['d0_blue_strike'] })).toThrow()
    s = play(s, 't0_blue_trap_fetch', { deckUids: ['d3_blue_undertow'] })
    expect(s.player.hand.map((c) => c.def.id)).toContain('blue_undertow')
    expect(s.player.aether).toBe(2)
  })
})

describe('緑と同じ89枚にする追加9枚 (2026-09-25)', () => {
  it('嵐の目=1ターンに4枚目をプレイした時だけ1ドローと一時マナ+1 (8枚目では鳴らない＝無限に回らない)', () => {
    let s = perm(withDraw(base(['blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop', 'blue_tide_drop']), ['blue_guard', 'blue_guard', 'blue_guard']), 'blue_perm_storm_eye')
    const e0 = s.player.energy
    const h = (n: number) => s.player.hand.filter((c) => c.def.id === 'blue_tide_drop')[n].uid
    for (let i = 0; i < 3; i++) s = play(s, h(0))
    expect(s.player.energy).toBe(e0)
    s = play(s, h(0))
    expect(s.player.energy).toBe(e0 + 1)
    const drawn = s.player.drawPile.length
    for (let i = 0; i < 4; i++) s = play(s, h(0))
    expect(s.player.energy).toBe(e0 + 1)
    expect(s.player.drawPile.length).toBe(drawn)
  })

  it('引き潮の帰還=捨て札のコスト0の札 (状態異常を除く) を全て手札へ＋詠唱+1。鍛えても0Eにならない', () => {
    let s = base(['blue_ebb_return'])
    s = { ...s, player: { ...s.player, discardPile: [inst('blue_tide_drop', 0), inst('blue_aftermath', 1), inst('blue_strike', 2), inst('status_wound', 3)] } }
    s = play(s, 't0_blue_ebb_return')
    expect(s.player.hand.map((c) => c.def.id).sort()).toEqual(['blue_aftermath', 'blue_tide_drop'])
    expect(s.player.discardPile.map((c) => c.def.id)).toEqual(expect.arrayContaining(['blue_strike', 'status_wound', 'blue_ebb_return']))
    expect(upgradeCard({ uid: 'u', def: getCardDef('blue_ebb_return') }).def.cost).toBe(1)
  })

  it('仕掛け師の工房=ターン開始時に山札の上から最初の仕込み札を1枚手札へ', () => {
    let s = perm(base([]), 'blue_perm_trap_workshop')
    s = withDraw(s, ['blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_strike', 'blue_mana_leak', 'blue_undertow'])
    s = applyCommand(withIntent(s, attackIntent(1)), { type: 'EndTurn' })
    expect(s.player.hand.map((c) => c.def.id)).toContain('blue_mana_leak')
    expect(s.player.drawPile.map((c) => c.def.id)).toContain('blue_undertow')
  })

  it('霊気の器=霊気を放出しても半分 (切り捨て) が残る', () => {
    let s = perm(base(['blue_aether_lance']), 'blue_perm_aether_vessel')
    s = { ...s, player: { ...s.player, aether: 7 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_blue_aether_lance')
    expect(hp0 - s.enemies[0].hp).toBeGreaterThan(20) // 霊気7×4 (敵の守りで少し吸われる)
    expect(s.player.aether).toBe(3)
  })

  it('潮読みの極み=占術すると1ドロー (1ターンに1回だけ)', () => {
    let s = perm(withDraw(base(['blue_shallow_read', 'blue_foresight_shield']), ['blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard']), 'blue_perm_tide_master')
    const hand0 = s.player.hand.length
    s = play(s, 't0_blue_shallow_read')
    s = applyCommand(s, { type: 'ResolveScry', discardUids: [] })
    expect(s.player.hand.length).toBe(hand0 - 1 + 1)
    s = play(s, 't1_blue_foresight_shield')
    s = applyCommand(s, { type: 'ResolveScry', discardUids: [] })
    expect(s.player.hand.length).toBe(hand0 - 2 + 1)
  })

  it('潮溜まり=敵ターンの後も手札に残った札はコスト-1 (手札を離れると元に戻る)', () => {
    let s = perm(perm(base(['blue_strike', 'blue_guard']), 'blue_perm_tide_pool'), 'blue_perm_flood_archive')
    s = applyCommand(withIntent(s, attackIntent(1)), { type: 'EndTurn', retainUids: ['t0_blue_strike'] })
    const kept = s.player.hand.find((c) => c.uid === 't0_blue_strike')!
    expect(kept.retainDiscount).toBe(1)
    expect(effectiveCost(s, kept)).toBe(0)
    s = play(s, 't0_blue_strike')
    expect(s.player.discardPile.find((c) => c.uid === 't0_blue_strike')?.retainDiscount).toBeUndefined()
  })

  it('見張りの鈴=敵がどんな行動をしても霊気+2と氷壁3・浅瀬読み=0E 占術2＋氷壁3・渦の足場=氷壁5＋詠唱+1', () => {
    expect(getCardDef('blue_reaction_watch_bell').effects.every((e) => e.trigger === 'onEnemyAction')).toBe(true)
    expect(getCardDef('blue_shallow_read')).toMatchObject({ cost: 0 })
    let s = base(['blue_eddy_foothold'])
    s = play(s, 't0_blue_eddy_foothold')
    expect(s.player.cardsPlayedThisTurn).toBe(1 + 1)
  })
})
