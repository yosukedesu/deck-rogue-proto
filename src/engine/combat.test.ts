// 戦闘フローのテスト。「確定済みルール」表の項目をここで固定する (仕様＝テスト)。
import { describe, expect, it } from 'vitest'
import { selectMoveTable } from './combat.ts'
import { getEnemyDef } from './content.ts'
import { applyCommand, createInitialState } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent } from './types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)

describe('StartCombat', () => {
  it('デッキ15枚・5枚ドロー・エナジー3/3・敵HP・意図宣言が揃う', () => {
    const s = freshCombat('set-auto', 'enemy_brute')
    expect(s.turn).toBe(1)
    expect(s.phase).toBe('player-turn')
    expect(s.player.hand).toHaveLength(5)
    expect(s.player.drawPile).toHaveLength(10) // 初期デッキは15種×1枚 (確定済みルール)
    expect(s.player.energy).toBe(3)
    expect(s.player.energyMax).toBe(3)
    expect(s.enemies[0].hp).toBe(getEnemyDef('enemy_brute').maxHp)
    expect(s.enemies[0].intent).not.toBeNull()
  })

  it('敵の意図: 実値は幅表示の範囲内にある (幅あり表示ルール)', () => {
    for (const seed of [1, 2, 3, 4, 5]) {
      const s = freshCombat('set-auto', 'enemy_wide_power', seed)
      const intent = s.enemies[0].intent!
      expect(intent.actual).toBeGreaterThanOrEqual(intent.shownMin)
      expect(intent.actual).toBeLessThanOrEqual(intent.shownMax)
    }
  })

  it('決定論: 同じシード+同じコマンド列=同じ結果 (リプレイの土台)', () => {
    const run = () => {
      let s = freshCombat('set-auto', 'enemy_brute', 123)
      s = applyCommand(s, { type: 'EndTurn' })
      s = applyCommand(s, { type: 'EndTurn' })
      s = applyCommand(s, { type: 'EndTurn' })
      return s
    }
    expect(JSON.stringify(run())).toBe(JSON.stringify(run()))
  })
})

describe('ターン構造 (StS準拠)', () => {
  it('手札は敵ターン終了後に全捨てされ、次ターンに5枚引き直す', () => {
    const s1 = freshCombat('set-auto', 'enemy_brute')
    const handUids = s1.player.hand.map((c) => c.uid)
    const s2 = applyCommand(s1, { type: 'EndTurn' })
    expect(s2.turn).toBe(2)
    expect(s2.player.hand).toHaveLength(5)
    // 前ターンの手札は捨て札にある
    for (const uid of handUids) {
      expect(s2.player.discardPile.some((c) => c.uid === uid)).toBe(true)
    }
  })

  it('ブロックは自ターン開始時に0リセット、エナジーは全回復', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_guard'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_guard' })
    expect(s.player.block).toBe(5)
    expect(s.player.energy).toBe(2)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.block).toBe(0)
    expect(s.player.energy).toBe(3)
  })

  it('ブロックは敵の攻撃を軽減する', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_guard'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_guard' })
    s = withIntent(s, attackIntent(12))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(s.player.maxHp - (12 - 5))
  })

  it('数ターン回してもカード総数15が保存される (捨て札リシャッフル)', () => {
    let s = freshCombat('set-auto', 'enemy_wide_power')
    // カード循環だけを見たいので、敵の攻撃で倒れないよう HP を盛っておく
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
    for (let i = 0; i < 4; i++) s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn')
    const p = s.player
    const total = p.hand.length + p.drawPile.length + p.discardPile.length + p.setCards.length
    expect(total).toBe(15)
    expect(p.hand).toHaveLength(5)
  })
})

describe('カードプレイ', () => {
  it('ランプ: 上限のみ増え、恩恵は次ターンから (即時利用は廃止)。戦闘ごとにリセット', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_ramp_sprout'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sprout' })
    expect(s.player.energyMax).toBe(4)
    expect(s.player.energy).toBe(2) // 3 - コスト1。即時の払い戻しはない
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.energy).toBe(4) // 次ターンから上限4で全回復
    // 新しい戦闘でリセット
    const s2 = applyCommand(s, { type: 'StartCombat', seed: 9, enemyId: 'enemy_brute' })
    expect(s2.player.energyMax).toBe(3)
  })

  it('成長カウンター: 与ダメージ全てに加算 (確定済みルール)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_growth_ring', 'green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_growth_ring' })
    expect(s.player.growth).toBe(2)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8) // 6 + 成長2
  })

  it('多段ヒットは1ヒットごとに成長が乗る', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_growth_ring', 'green_double_lash'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_growth_ring' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_double_lash' })
    expect(s.enemies[0].hp).toBe(hpBefore - (4 + 2) * 2) // (基礎4+成長2)×2ヒット (二連 4×2)
  })

  it('エナジー不足・手札にないカード・リアクション専用カードのプレイは拒否', () => {
    const s = withHand(freshCombat('set-auto', 'enemy_brute'), [
      'green_finisher_stomp',
      'green_reaction_thorns',
    ])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_finisher_stomp' })).toThrow(
      /エナジー不足/,
    )
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 'no_such' })).toThrow(/手札にない/)
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_reaction_thorns' })).toThrow(
      /プレイ不可/,
    )
  })

  it('敵を倒すと勝利し CombatEnded が記録される', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_probe'), ['green_fang'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 9 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    expect(s.phase).toBe('won')
    expect(types(s.eventLog)).toContain('CombatEnded')
  })
})

describe('敵の行動テーブル', () => {
  it('伏せがあると movesVsSet を使う (伏せ警戒型・伏せ破壊型)', () => {
    const wary = getEnemyDef('enemy_set_wary')
    expect(selectMoveTable(wary, true)).toBe(wary.movesVsSet)
    expect(selectMoveTable(wary, false)).toBe(wary.moves)
    const brute = getEnemyDef('enemy_brute') // movesVsSet を持たない敵は常に moves
    expect(selectMoveTable(brute, true)).toBe(brute.moves)
  })
})

describe('applyCommand の基本則', () => {
  it('元の状態を変更しない (イミュータブル)', () => {
    const s0 = createInitialState(42, 'set-auto')
    applyCommand(s0, { type: 'StartCombat', seed: 42, enemyId: 'enemy_brute' })
    expect(s0.turn).toBe(0)
    expect(s0.eventLog).toHaveLength(0)
  })
})
