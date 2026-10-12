// 複数体戦闘 (2026-08-24) のテスト。
// エンカウンター編成・StS式ターゲティング・全体攻撃・混乱 (仲間割れ)・応援 (ラリー)。
// 確定済みルール表「戦闘形式」「ターゲティング」「全体攻撃」「混乱」「応援（ラリー）」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { getEnemyDef } from './content.ts'
import { freshCombat, withHand, hpWithin } from './test-helpers.ts'
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
  actual,
})
const defend = (actual: number): EnemyIntent => ({
  kind: 'defend',
  actual,
})

describe('エンカウンター編成', () => {
  it('編成IDで複数体が出現し、群れ補正 (hpScale/strength/patternOffset) が個体に効く', () => {
    const s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    expect(s.enemies).toHaveLength(2)
    // 探り屋 hpRange × 群れ補正 0.5 (HPの幅 2026-09-14: 個体ごとにロールするので区間で見る)
    expect(hpWithin(s.enemies[0].maxHp, getEnemyDef('enemy_probe'), 0.5)).toBe(true)
    expect(hpWithin(s.enemies[1].maxHp, getEnemyDef('enemy_probe'), 0.5)).toBe(true)
    expect(s.enemies.every((e) => e.intent !== null)).toBe(true)
    // member.start: 2体目はローテーションがズレて開始 (同時lunge防止。行動グラフ 2026-09-14)
    expect(s.enemies[0].node).not.toBe(s.enemies[1].node)
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
    expect(s.enemies[0].hp).toBe(hp0 - 12) // 炎の波 12 (2026-08-30 全体化係数の色レート化: 赤=本家×1.5)
    expect(s.enemies[1].hp).toBe(hp1 - 12)
  })

  it('成長は対象ごとに乗る', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = { ...s, player: { ...s.player, growth: 3 } }
    s = withHand(s, ['red_flame_wave'])
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_flame_wave' })
    expect(s.enemies[0].hp).toBe(hp0 - 15) // 12 + 成長3
    expect(s.enemies[1].hp).toBe(hp1 - 15)
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
    s = withIntents(s, [defend(5), { kind: 'rally', actual: 2 }])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBe(before[0] + 2)
    expect(s.enemies[1].strength).toBe(before[1] + 2)
  })
})

// Presentation payload is a receipt of existing aggregate resolution, not a new rule.
describe('混乱攻撃の実損失・吸収 receipt', () => {
  const cases = [
    { name: 'other', encounter: 'enc_probe_pair', attacker: 0, target: 1, amount: 10, hits: 1, block: 0, hp: 30, loss: 10, endHp: 20 },
    { name: 'self', encounter: 'enemy_probe', attacker: 0, target: 0, amount: 10, hits: 1, block: 0, hp: 30, loss: 10, endHp: 20 },
    { name: 'multi aggregate', encounter: 'enc_probe_pair', attacker: 0, target: 1, amount: 4, hits: 3, block: 0, hp: 30, loss: 12, endHp: 18 },
    { name: 'full guard', encounter: 'enc_probe_pair', attacker: 1, target: 0, amount: 10, hits: 1, block: 15, hp: 30, loss: 0, endHp: 30 },
    { name: 'partial guard', encounter: 'enc_probe_pair', attacker: 1, target: 0, amount: 10, hits: 1, block: 6, hp: 30, loss: 4, endHp: 26 },
    { name: 'heavy partial guard', encounter: 'enc_probe_pair', attacker: 1, target: 0, amount: 20, hits: 1, block: 19, hp: 30, loss: 1, endHp: 29 },
    { name: 'lethal other', encounter: 'enc_probe_pair', attacker: 0, target: 1, amount: 10, hits: 1, block: 0, hp: 8, loss: 10, endHp: -2 },
    { name: 'lethal self', encounter: 'enemy_probe', attacker: 0, target: 0, amount: 10, hits: 1, block: 0, hp: 8, loss: 10, endHp: -2 },
    { name: 'zero', encounter: 'enemy_probe', attacker: 0, target: 0, amount: 0, hits: 1, block: 0, hp: 30, loss: 0, endHp: 30 },
  ]
  for (const c of cases) it(c.name, () => {
    let s = withHand(freshCombat('set-auto', c.encounter, 42), [])
    // Directly replacing intents is a synthetic receipt fixture, not a real declaration.
    // Discard only the new provenance annotation so no old graph move is borrowed.
    s = { ...s, eventLog: s.eventLog.map(e => e.type === 'EnemyIntentDeclared' ? { ...e, presentationMove: undefined } : e),
      player: { ...s.player, hp: 75, setCards: [], permanents: [] }, enemies: s.enemies.map((e, i) => ({
      ...e, hp: i === c.target ? c.hp : 30, maxHp: 30, block: 0, confusion: i === c.attacker ? 1 : 0,
      burn: 0, thorns: 0,
      intent: i === c.attacker ? { kind: 'attack', actual: c.amount, hits: c.hits } : c.block > 0 ? defend(c.block) : { kind: 'rest', actual: 0 },
    })) }
    const from = s.eventLog.length
    const targetId = s.enemies[c.target].enemyId
    const sourceId = s.enemies[c.attacker].enemyId
    s = applyCommand(s, { type: 'EndTurn' })
    const events = s.eventLog.slice(from)
    expect(events.filter((e) => e.type === 'ConfusedAttack')).toEqual([{
      type: 'ConfusedAttack', enemyIndex: c.attacker, targetIndex: c.target,
      amount: c.amount * c.hits, hpLoss: c.loss, blocked: Math.min(c.block, c.amount * c.hits),
      // Confusion remains one aggregate receipt; synthetic overwritten intents are explicitly unresolved.
      presentationEnemyAction: {
        version: 1, enemyId: sourceId, unresolvedReason: 'missing_or_invalid_latest_declaration',
        executionEventIndex: s.eventLog.findIndex((e, i) => i >= from && e.type === 'EnemyActionExecuting' && e.enemyIndex === c.attacker),
      },
      presentationReceipt: {
        version: 1, target: 'enemy', enemyIndex: c.target, enemyId: targetId,
        before: { hp: c.hp, maxHp: 30, block: c.block },
        after: { hp: c.endHp, maxHp: 30, block: Math.max(0, c.block - c.amount * c.hits) },
      },
    }])
    expect(s.enemies[c.target].hp).toBe(c.endHp)
    expect(s.enemies[c.target].block).toBe(Math.max(0, c.block - c.amount * c.hits))
    expect(s.player.hp).toBe(75)
    expect(s.enemies[c.attacker].confusion).toBe(0)
    expect(events.some((e) => e.type === 'DamageDealt' || e.type === 'EnemyDied')).toBe(false)
    expect(s.phase).toBe(c.encounter === 'enemy_probe' && c.endHp <= 0 ? 'won' : 'player-turn')
  })

  it('normal enemy multihit retains one aggregate DamageDealt', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_probe', 42), [])
    s = { ...s, player: { ...s.player, hp: 75, block: 10, setCards: [], permanents: [] },
      enemies: s.enemies.map((e) => ({ ...e, confusion: 0, intent: { kind: 'attack', actual: 4, hits: 3 } })) }
    const from = s.eventLog.length
    s = applyCommand(s, { type: 'EndTurn' })
    const damage = s.eventLog.slice(from).filter((e) => e.type === 'DamageDealt')
    expect(damage).toHaveLength(1)
    expect(damage[0]).toMatchObject({ source: 'enemy', amount: 12, hpLoss: 2, blocked: 10 })
    expect(s.eventLog.slice(from).some((e) => e.type === 'ConfusedAttack')).toBe(false)
    expect(s.player.hp).toBe(73)
  })
})
