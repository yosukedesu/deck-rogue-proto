// 3体目の幕ボス (2026-09-27 ユーザー「敵の種類も本家より少なくない？」→ 各幕に本家の型のボスを1体ずつ)。
// 朧の大鹿＝朧 (Vantom。当初は月影の大蛞蝓、ユーザー「ナメクジキモい」で鹿に) / 巻き上げ機の番人＝技封じの絡繰 (Bronze Automaton) / 熾を喰う古炉＝HPで痛む一撃 (Hexaghost)
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { getCardDef, getEnemyDef } from './content.ts'
import { chainFromStart } from './enemyGraph.ts'
import { ACT_BOSS_POOLS } from './map.ts'
import { freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { CardInstance, GameState } from './types.ts'

const card = (id: string, uid: string): CardInstance => ({ uid, def: getCardDef(id) })

describe('3体目の幕ボスがプールにいる', () => {
  it('幕1 朧の大鹿・幕2 巻き上げ機の番人・幕3 熾を喰う古炉', () => {
    expect(ACT_BOSS_POOLS[0]).toContain('enemy_haze_stag')
    expect(ACT_BOSS_POOLS[1]).toContain('enemy_winch_warden')
    expect(ACT_BOSS_POOLS[2]).toContain('enemy_ember_furnace')
  })
})

describe('朧の大鹿: 朧 (HPに届く当たりの最初の9回は1)', () => {
  const hit = (s: GameState): GameState => {
    s = withHand(s, ['green_strike'])
    return applyCommand({ ...s, player: { ...s.player, energy: 9 } }, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
  }
  it('9回までは1ダメージで1ずつ減り、10回目から素通し', () => {
    let s = freshCombat('set-confirm', 'enemy_haze_stag', 42)
    expect(s.enemies[0].slippery).toBe(9)
    const hp0 = s.enemies[0].hp
    for (let k = 0; k < 9; k++) s = hit(s)
    expect(s.enemies[0].hp).toBe(hp0 - 9)
    expect(s.enemies[0].slippery).toBe(0)
    const before = s.enemies[0].hp
    s = hit(s)
    expect(before - s.enemies[0].hp).toBe(6) // 打撃6がそのまま通る
  })

  it('敵のブロックで全部止まった当たりは数えない', () => {
    let s = freshCombat('set-confirm', 'enemy_haze_stag', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 20 })) }
    s = hit(s)
    expect(s.enemies[0].slippery).toBe(9)
    expect(s.enemies[0].block).toBe(14)
  })

  it('ログに朧で消えた量が載る', () => {
    const s = hit(freshCombat('set-confirm', 'enemy_haze_stag', 42))
    const d = s.eventLog.filter((e) => e.type === 'DamageDealt').at(-1)
    expect(d && 'slipperyCut' in d ? d.slipperyCut : 0).toBe(5)
  })

  it('行動は 角で小突く→蹄の連打→角の突進(負傷3)→霧に紛れる(防御+筋力2) の4手番', () => {
    const def = getEnemyDef('enemy_haze_stag')
    expect(chainFromStart(def, 5)).toEqual(['antler_jab', 'hoof_flurry', 'antler_charge', 'fade_in_mist', 'antler_jab'])
    expect(def.moves.find((m) => m.id === 'antler_charge')?.inflict).toEqual({ status: 'wound', amount: 3 })
  })
})

describe('巻き上げ機の番人: 技封じの絡繰', () => {
  it('1手番目に絡繰を2体呼ぶ', () => {
    let s = freshCombat('set-confirm', 'enemy_winch_warden', 42)
    expect(s.enemies[0].intent?.kind).toBe('summon')
    s = applyCommand({ ...s, player: { ...s.player, hp: 999, maxHp: 999 } }, { type: 'EndTurn' })
    expect(s.enemies.filter((e) => e.enemyId === 'enemy_seal_orb' && e.hp > 0)).toHaveLength(2)
  })

  it('山札でいちばんレアな札を封じ、その絡繰を倒すと手札に戻る', () => {
    let s = freshCombat('set-confirm', 'enemy_seal_orb', 42)
    const rare = card('green_sig_vine_dance', 'r1')
    expect(rare.def.rarity).toBe('rare')
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999, drawPile: [card('green_strike', 'c1'), rare, card('green_guard', 'c2'), card('green_strike', 'c3'), card('green_strike', 'c4'), card('green_strike', 'c5')] } }
    s = withIntent(s, { kind: 'seal', actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].sealed?.map((c) => c.uid)).toEqual(['r1'])
    expect([...s.player.drawPile, ...s.player.hand, ...s.player.discardPile].some((c) => c.uid === 'r1')).toBe(false)
    // 倒すと手札に戻る
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand({ ...s, player: { ...s.player, energy: 3 } }, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
    expect(s.player.hand.some((c) => c.uid === 'r1')).toBe(true)
    expect(s.eventLog.some((e) => e.type === 'CardUnsealed')).toBe(true)
  })

  it('山札が空なら捨て札から封じる', () => {
    let s = freshCombat('set-confirm', 'enemy_seal_orb', 42)
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999, drawPile: [], discardPile: [card('green_strike', 'd1')] } }
    s = withIntent(s, { kind: 'seal', actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].sealed?.map((c) => c.uid)).toEqual(['d1'])
  })
})

describe('熾を喰う古炉: HPで痛む一撃', () => {
  it('2手番目の一撃は「宣言した時のあなたのHP÷12＋1」×6', () => {
    let s = freshCombat('set-confirm', 'enemy_ember_furnace', 42)
    expect(s.enemies[0].intent?.kind).toBe('defend')
    s = { ...s, player: { ...s.player, hp: 80, maxHp: 99 } }
    s = applyCommand(s, { type: 'EndTurn' })
    const it = s.enemies[0].intent!
    expect(it.kind).toBe('attack')
    expect(it.hits).toBe(6)
    expect(it.actual).toBe(Math.floor(s.player.hp / 12) + 1 + s.enemies[0].strength)
  })

  it('その後は 焼き付け→体当たり→焼き付け→煽る→体当たり→焼き付け→業火 の輪 (一撃は繰り返さない)', () => {
    const def = getEnemyDef('enemy_ember_furnace')
    expect(chainFromStart(def, 10)).toEqual(['stoke', 'devour_ember', 'sear', 'tackle', 'sear', 'inflame', 'tackle', 'sear', 'inferno', 'sear'])
  })
})
