// 複数体戦闘 (2026-08-24) のテスト。
// エンカウンター編成・StS式ターゲティング・全体攻撃・混乱 (仲間割れ)・応援 (ラリー)。
// 確定済みルール表「戦闘形式」「ターゲティング」「全体攻撃」「混乱」「応援（ラリー）」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { EnemyIntent, GameState } from './types.ts'

/** 全敵の意図を差し替える (複数体テスト用) */
function withIntents(state: GameState, intents: readonly EnemyIntent[]): GameState {
  return {
    ...state,
    enemies: state.enemies.map((e, i) => (intents[i] ? { ...e, intent: intents[i] } : e)),
  }
}

const atk = (actual: number): EnemyIntent => ({
  kind: 'attack',
  shownMin: actual,
  shownMax: actual,
  actual,
})
const defend = (actual: number): EnemyIntent => ({
  kind: 'defend',
  shownMin: actual,
  shownMax: actual,
  actual,
})

describe('エンカウンター編成', () => {
  it('編成IDで複数体が出現し、群れ補正 (hpScale/strength/patternOffset) が個体に効く', () => {
    const s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    expect(s.enemies).toHaveLength(2)
    const pairHp = Math.round(90 * 0.5) // 探り屋 maxHp × 群れ補正 (2026-08-25 調整: 0.45→0.5)
    expect(s.enemies[0].maxHp).toBe(pairHp)
    expect(s.enemies[1].maxHp).toBe(pairHp)
    expect(s.enemies.every((e) => e.intent !== null)).toBe(true)
    // patternOffset: 2体目はローテーションがズレて開始 (同時lunge防止)
    expect(s.enemies[1].patternIndex).toBeGreaterThan(s.enemies[0].patternIndex)
  })

  it('敵ID直指定は従来どおりソロ編成 (後方互換)', () => {
    const s = freshCombat('set-confirm', 'enemy_brute', 42)
    expect(s.enemies).toHaveLength(1)
  })

  it('勝利は全滅 (1体倒しただけでは続行)', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, hp: 0 } : e)) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 1 })
    expect(s.phase).toBe('player-turn') // まだ1体生存
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 0 })) }
    s = withHand(s, ['green_strike'])
    // 全滅チェックは次のダメージ解決で発火するが、直接 checkCombatEnd を通る EndTurn でも確定する
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('won')
  })
})

describe('ターゲティング (StS式)', () => {
  it('2体以上生存時、単体対象カードは targetIndex 必須', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, ['green_strike'])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })).toThrow(
      /対象/,
    )
  })

  it('targetIndex で指定した敵にだけダメージが入る', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, ['green_strike'])
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 1 })
    expect(s.enemies[0].hp).toBe(hp0)
    expect(s.enemies[1].hp).toBe(hp1 - 6)
  })

  it('生存1体なら targetIndex 省略可 (従来互換)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, ['green_strike'])
    const hp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBe(hp - 6)
  })

  it('対象不要カード (防御等) は複数体でも targetIndex 不要', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, ['green_guard'])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_guard' })).not.toThrow()
  })
})

describe('全体攻撃 (target: all)', () => {
  it('生存する敵全体にダメージが入り、死亡済みはスキップ', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, ['red_flame_wave'])
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_flame_wave' })
    expect(s.enemies[0].hp).toBe(hp0 - 8) // 炎の波 6→8 (2026-08-27)
    expect(s.enemies[1].hp).toBe(hp1 - 8)
  })

  it('成長は対象ごとに乗る', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = { ...s, player: { ...s.player, growth: 3 } }
    s = withHand(s, ['red_flame_wave'])
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_flame_wave' })
    expect(s.enemies[0].hp).toBe(hp0 - 11) // 8 + 成長3
    expect(s.enemies[1].hp).toBe(hp1 - 11)
  })
})

describe('混乱 (仲間割れ)', () => {
  it('混乱した敵の攻撃は他の生存敵に向かい、プレイヤーは無傷。混乱は1減る', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, [])
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, confusion: 1 } : e)) }
    s = withIntents(s, [atk(10), defend(5)])
    const playerHp = s.player.hp
    const ally = s.enemies[1].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(playerHp) // 攻撃はプレイヤーに来ない
    expect(s.enemies[1].hp).toBe(ally - 10) // 仲間に命中 (防御行動のブロックは攻撃の後段のため素通し)
    expect(s.enemies[0].confusion).toBe(0)
  })

  it('ソロ戦では自分自身を殴る', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, [])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, confusion: 1, hp: 999 })) }
    s = withIntents(s, [atk(10)])
    const playerHp = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(playerHp)
    expect(s.enemies[0].hp).toBe(999 - 10)
  })

  it('攻撃以外の行動では混乱は消費されない', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, [])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, confusion: 1 })) }
    s = withIntents(s, [defend(5)])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].confusion).toBe(1)
  })

  it('幻惑の囁きで混乱を付与できる', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = withHand(s, ['blue_confuse'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_confuse', targetIndex: 0 })
    expect(s.enemies[0].confusion).toBe(1)
    expect(s.enemies[1].confusion).toBe(0)
  })
})

describe('応援 (ラリー)', () => {
  it('rally は生存する味方全体の強化に加算される', () => {
    let s = freshCombat('set-confirm', 'enc_wolf_drummer', 42)
    s = withHand(s, [])
    expect(s.enemies).toHaveLength(2)
    const before = s.enemies.map((e) => e.strength)
    s = withIntents(s, [defend(5), { kind: 'rally', shownMin: 2, shownMax: 2, actual: 2 }])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBe(before[0] + 2)
    expect(s.enemies[1].strength).toBe(before[1] + 2)
  })
})
