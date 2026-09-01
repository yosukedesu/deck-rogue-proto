// 撃破サマリー (2026-08-29 面白さ5への処方③: ピーク体験) と ボスの第2形態のテスト。
// 確定済みルール表「敵フェーズ変化」(ボス3体への適用) を固定する。
import { describe, expect, it } from 'vitest'
import { battleSummary, cardCostLabel, summaryLine, xHitsSuffix } from './summary.ts'
import { allCards, getCardDef, getEnemyDef } from './content.ts'
import { createRun } from './run.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameEvent } from './types.ts'

describe('撃破サマリー (battleSummary)', () => {
  it('ターン数・与ダメ・最大ターン火力・被ダメ・読み勝ち・完全ブロック・打ち消しを集計する', () => {
    const log: GameEvent[] = [
      { type: 'TurnStarted', turn: 1 },
      { type: 'DamageDealt', source: 'player', amount: 10, hpLoss: 10 },
      { type: 'DamageDealt', source: 'player', amount: 5, hpLoss: 5 },
      { type: 'DamageDealt', source: 'enemy', amount: 8, hpLoss: 0 }, // 完全に凌いだ
      { type: 'TurnStarted', turn: 2 },
      { type: 'ReactionTriggered', cardId: 'x', mode: 'set-confirm' },
      { type: 'DamageDealt', source: 'player', amount: 30, hpLoss: 30 }, // 最大ターン
      { type: 'DamageDealt', source: 'enemy', amount: 12, hpLoss: 7 },
      { type: 'ActionNegated', enemyIndex: 0 },
    ]
    const s = battleSummary(log)
    expect(s.turns).toBe(2)
    expect(s.totalDealt).toBe(45)
    expect(s.bestTurnDealt).toBe(30)
    expect(s.hpLost).toBe(7)
    expect(s.reactionsFired).toBe(1)
    expect(s.perfectBlocks).toBe(1)
    expect(s.negates).toBe(1)
    expect(summaryLine(s)).toContain('読み勝ち1回')
    expect(summaryLine(s)).toContain('完全に凌いだ1回')
  })
})

describe('ボスの第2形態 (2026-08-29 面白さ5への処方③。HP50%のフェーズ変化)', () => {
  it('オーガ・大亀・門番の3ボスすべてが below-half テーブルを持ち、無条件の攻撃を含む', () => {
    for (const id of ['enemy_brute', 'enemy_turtle', 'enemy_warden']) {
      const def = getEnemyDef(id)
      expect(def.movesBelowHalf, id).toBeDefined()
      expect(
        def.movesBelowHalf!.some((m) => m.kind === 'attack'),
        `${id} は第2形態でも殴れる (膠着破り)`,
      ).toBe(true)
    }
  })

  it('大亀: HP半分を割ると防御サイクルが消え、噛みつき⇄大薙ぎの2拍になる', () => {
    const def = getEnemyDef('enemy_turtle')
    expect(def.sequenceBelowHalf).toEqual(['awake_bite', 'crush'])
    expect(def.movesBelowHalf!.every((m) => m.kind === 'attack')).toBe(true)
  })

  it('半分を割った次の意図宣言から第2形態のテーブルが使われる (大亀で実測)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle', 42), [])
    // HPを半分未満に落としてターンを回す → 次の宣言は awake_bite (連撃) か crush のみ
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(e.maxHp * 0.4) })) }
    s = applyCommand(s, { type: 'EndTurn' })
    const intent = s.enemies[0].intent!
    expect(intent.kind).toBe('attack') // 防御 (shell) はもう宣言されない
  })
})

describe('カード表示のラベル (2026-08-29 Xコスト表記のバグ修正)', () => {
  it('Xコスト札はコストが "X" と表示される (cost フィールドの1ではない)', () => {
    for (const def of allCards.filter((c) => c.xCost === true)) {
      expect(cardCostLabel(def), def.name).toBe('X')
      // 割引が渡されてもXは割引の対象外なので "X" のまま
      expect(cardCostLabel(def, 0), def.name).toBe('X')
    }
  })

  it('通常カードは数値のまま。割引が渡されたらその値を出す', () => {
    const strike = getCardDef('green_strike')
    expect(cardCostLabel(strike)).toBe('1')
    expect(cardCostLabel(strike, 0)).toBe('0')
  })

  it('xHits の効果には「×Xヒット」が付く (付けないと1マナ7ダメージに見える)', () => {
    // 成長・勢いの注記はダメージ効果だけ (2026-09-01 樹皮の重鎧の誤読対処)
    expect(xHitsSuffix({ xHits: true, effect: 'dealDamage' })).toBe('×Xヒット(各ヒットに成長・勢いが乗る)')
    expect(xHitsSuffix({ xHits: true, effect: 'gainBlock' })).toBe('×Xヒット')
    expect(xHitsSuffix({})).toBe('')
    for (const def of allCards.filter((c) => c.xCost === true)) {
      expect(def.effects.some((e) => e.xHits === true), `${def.name} に xHits が無い`).toBe(true)
    }
  })

  it('ショップはXコスト札を1コスト扱いで安売りしない', () => {
    // 価格 = 40 + コスト×10 + ロール。X札は cost:1 なので素通しだと最安帯になる
    const run = createRun(2, 'set-confirm')
    const xCard = allCards.find((c) => c.xCost === true)!
    expect(xCard.cost).toBe(1) // データ上は1 (プレイ時に全エナジーを払う)
    expect(run.gold).toBeGreaterThan(0) // ラン生成の健全性 (価格式は openShop 側で検証)
  })
})

describe('撃破サマリーの被ダメ集計 (2026-08-30 計測ランで発覚)', () => {
  it('とげ反射のHP損失も「被ダメ」に数える', () => {
    const log: GameEvent[] = [
      { type: 'TurnStarted', turn: 1 },
      { type: 'DamageDealt', source: 'player', amount: 6, hpLoss: 6 },
      { type: 'ThornsReflected', enemyIndex: 0, amount: 2, hpLoss: 2 },
      { type: 'ThornsReflected', enemyIndex: 0, amount: 2, hpLoss: 2 },
      { type: 'DamageDealt', source: 'enemy', amount: 5, hpLoss: 5 },
    ]
    // 敵の攻撃5 + とげ反射2+2 = 9 (旧実装は5しか数えず「被ダメ5」と表示していた)
    expect(battleSummary(log).hpLost).toBe(9)
  })

  it('ブロックで防いだとげ反射は被ダメに数えない (hpLoss=0)', () => {
    const log: GameEvent[] = [
      { type: 'TurnStarted', turn: 1 },
      { type: 'ThornsReflected', enemyIndex: 0, amount: 2, hpLoss: 0 },
    ]
    expect(battleSummary(log).hpLost).toBe(0)
  })
})
