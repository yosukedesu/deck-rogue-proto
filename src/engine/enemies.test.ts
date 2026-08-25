// 敵の作り込み (2026-08-24) のテスト。
// 状態異常 (弱体/脆弱/負傷)・連撃・再生・フェーズ変化・激昂・挑発。
// 確定済みルール表「敵の設計原則」「状態異常」「連撃」「再生」「敵フェーズ変化」「激昂」を固定する。
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const noHand = (s: GameState): GameState => withHand(s, [])

describe('弱体 (プレイヤーの与ダメ25%減)', () => {
  it('弱体中の攻撃はダメージ25%減 (切り捨て)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_strike'])
    s = { ...s, player: { ...s.player, weak: 2 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4) // 打撃6 → floor(6*0.75)=4
  })

  it('弱体は自ターン終了時に1減る', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, weak: 2 } }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.weak).toBe(1)
  })
})

describe('脆弱 (敵の攻撃ダメージ50%増)', () => {
  it('脆弱中は敵の攻撃が50%増 (切り捨て)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = withIntent(s, attackIntent(10))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 10 * 1.5
  })

  it('延焼を持つ敵にも脆弱は素の実値に掛かる (威嚇は撤去済み)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5, hp: 999 })) }
    s = withIntent(s, attackIntent(10))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 10 * 1.5
  })

  it('脆弱は敵フェーズ終了時に1減る (そのフェーズは有効)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.vulnerable).toBe(0)
  })
})

describe('負傷 (死に札の混入)', () => {
  it('負傷は使用不可カードを捨て札に加える', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'hex',
      shownMin: 0,
      shownMax: 0,
      actual: 0,
      inflict: { status: 'wound', amount: 2 },
    })
    s = applyCommand(s, { type: 'EndTurn' })
    const wounds = s.player.discardPile.filter((c) => c.def.id === 'status_wound')
    expect(wounds.length).toBe(2)
  })

  it('負傷は1戦闘の上限5枚 (ハメ防止)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'hex',
      shownMin: 0,
      shownMax: 0,
      actual: 0,
      inflict: { status: 'wound', amount: 99 },
    })
    s = applyCommand(s, { type: 'EndTurn' })
    const all = [...s.player.discardPile, ...s.player.drawPile, ...s.player.hand]
    expect(all.filter((c) => c.def.id === 'status_wound').length).toBe(5)
  })

  it('攻撃に付与された状態異常はダメージ後に適用される (攻撃+弱体)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'attack',
      shownMin: 6,
      shownMax: 9,
      actual: 7,
      inflict: { status: 'weak', amount: 2 },
    })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 7)
    expect(s.player.weak).toBe(2)
  })
})

describe('連撃 (multi-hit)', () => {
  it('連撃は1発の実値×ヒット数のダメージ', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15)
  })

  it('ブロックはヒット順に消費される', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = { ...s, player: { ...s.player, block: 7 } }
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 8) // 15 - 7
  })

  it('連撃は1発ずつ素の実値で解決される (威嚇は撤去済み)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5, hp: 999 })) }
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 5 × 3
  })
})

describe('再生とフェーズ変化 (苔まといの主)', () => {
  it('敵フェーズ終了時にHPが回復する (最大HPまで)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: maxHp - 10 })) }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    const regen = getEnemyDef('enemy_moss').regen ?? 0
    expect(regen).toBeGreaterThan(0)
    expect(s.enemies[0].hp).toBe(maxHp - 10 + regen)
  })

  it('HP50%以下では再生しない (スタール防止)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    const low = Math.floor(maxHp * 0.4)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: low })) }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(low)
  })

  it('HP50%以下では行動テーブルが牙をむく側に切り替わる', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(maxHp * 0.4) })) }
    s = withIntent(s, attackIntent(1))
    s = applyCommand(s, { type: 'EndTurn' })
    // 次ターンの意図宣言は movesBelowHalf (bite 14〜18) から選ばれる
    const intent = s.enemies[0].intent
    expect(intent).not.toBeNull()
    expect(intent!.kind).toBe('attack')
    expect(intent!.shownMin).toBeGreaterThanOrEqual(14)
  })
})

describe('激昂 (刻限の門番)', () => {
  it('敵フェーズ終了時に強化が自動で増える', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_warden', 42))
    expect(s.enemies[0].strength).toBe(0)
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    const enrage = getEnemyDef('enemy_warden').enrage ?? 0
    expect(enrage).toBeGreaterThan(0)
    expect(s.enemies[0].strength).toBe(enrage)
  })
})

describe('挑発 (嘲る道化)', () => {
  it('伏せが無いと大振り (通常テーブルは強攻撃のみ)', () => {
    const s = freshCombat('set-confirm', 'enemy_joker', 42)
    // 開始時は伏せ無し → moves (大振り) から宣言される
    expect(s.enemies[0].intent?.kind).toBe('attack')
    expect(s.enemies[0].intent!.shownMin).toBeGreaterThanOrEqual(15)
  })

  it('伏せがあると用心する (movesVsSet に大振りは無い)', () => {
    const def = getEnemyDef('enemy_joker')
    expect(def.movesVsSet).toBeDefined()
    const maxAttack = Math.max(...def.movesVsSet!.map((m) => m.max ?? 0))
    const wildMin = Math.min(...def.moves.filter((m) => m.kind === 'attack').map((m) => m.min ?? 99))
    expect(maxAttack).toBeLessThan(wildMin) // 用心時の最大値 < 大振りの最小値
  })
})
