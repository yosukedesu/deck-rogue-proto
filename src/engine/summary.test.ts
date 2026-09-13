// 撃破サマリー (2026-08-29 面白さ5への処方③: ピーク体験) と ボスの第2形態のテスト。
// 確定済みルール表「敵フェーズ変化」(ボス3体への適用) を固定する。
import { describe, expect, it } from 'vitest'
import { chainFromStart } from './enemyGraph.ts'
import { battleSummary, cardCostLabel, displayedIntentValue, incomingFrom, incomingTotal, intentModifierNotes, setBranchNote, summaryLine, xHitsSuffix } from './summary.ts'
import { allCards, getCardDef, getEnemyDef, allEnemies } from './content.ts'
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
    expect(summaryLine(s)).toContain('敵の攻撃1回を完全に凌いだ')
  })
})

describe('ボスの第2形態 (2026-08-29 面白さ5への処方③。HP50%のフェーズ変化)', () => {
  it('オーガ・大亀・門番の3ボスすべてが below-half テーブルを持ち、無条件の攻撃を含む', () => {
    for (const id of ['enemy_brute', 'enemy_turtle', 'enemy_warden']) {
      const def = getEnemyDef(id)
      const half = def.interrupts?.find((it) => it.on === 'hpBelowHalf')
      expect(half, id).toBeDefined()
      // 第2形態の列 (行動グラフ 2026-09-14: 割り込みの goto から辿る)
      const moves = chainFromStart(def, 8, half!.goto)
      expect(
        moves.some((m) => def.moves.find((x) => x.id === m)?.kind === 'attack'),
        `${id} は第2形態でも殴れる (膠着破り)`,
      ).toBe(true)
    }
  })

  it('大亀: HP半分を割ると防御サイクルが消え、噛みつき⇄大薙ぎの2拍になる', () => {
    const def = getEnemyDef('enemy_turtle')
    const half = def.interrupts!.find((it) => it.on === 'hpBelowHalf')!
    expect(chainFromStart(def, 4, half.goto)).toEqual(['awake_bite', 'crush', 'awake_bite', 'crush'])
    expect(['awake_bite', 'crush'].every((id) => def.moves.find((m) => m.id === id)?.kind === 'attack')).toBe(true)
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

describe('setBranchNote: 伏せ分岐の型の注記 (2026-09-03 Opusラン F 指摘)', () => {
  it('固定ローテ+重み抽選の反応テーブルを持つ敵にだけ「順番を崩す」が付く (2026-09-13 罠モデル以降は該当なし)', () => {
    // 探り屋の反応テーブルは罠モデルで撤去 (敵は伏せを見ない) = 注記も付かない
    const probe = getEnemyDef('enemy_probe')
    expect(probe.movesVsSet).toBeUndefined()
    expect(setBranchNote(probe)).toBeNull()
    // 判定は「乱択の節なし × movesVsSet 2件以上」だけで決まる。残る反応テーブル持ち (罠壊し・道化) は乱択なので対象外
    for (const d of allEnemies) {
      const expected = !Object.values(d.nodes).some((n) => n.random !== undefined) && (d.movesVsSet?.length ?? 0) >= 2
      expect(setBranchNote(d) !== null, d.id).toBe(expected)
    }
    expect(allEnemies.filter((d) => setBranchNote(d) !== null)).toEqual([])
    // 機構は残る: 固定ローテ+2件以上の反応テーブルを合成すれば付く
    const synthetic = { ...probe, movesVsSet: getEnemyDef('enemy_set_breaker').movesVsSet }
    expect(setBranchNote(synthetic)).toContain('順番を崩す')
  })
})

describe('実値公開のライブ表示 (2026-09-14 本家形): 意図の数字は宣言した実値に威圧・脆弱・重りを掛けた値', () => {
  it('displayedIntentValue は攻撃だけ補正し、注記はかかっている補正だけを列挙する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [])
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'attack' as const, actual: 12 } } : e)) }
    expect(displayedIntentValue(s, 0, s.enemies[0].intent!)).toBe(12)
    expect(intentModifierNotes(s, 0, s.enemies[0].intent!)).toEqual([])
    // 脆弱: +50% (切り捨て) → 18。威圧: -25% → 9 → 脆弱で 13 (実処理と同順: 威圧→脆弱)
    const vuln = { ...s, player: { ...s.player, vulnerable: 1 } }
    expect(displayedIntentValue(vuln, 0, vuln.enemies[0].intent!)).toBe(18)
    expect(intentModifierNotes(vuln, 0, vuln.enemies[0].intent!)).toEqual(['脆弱+50%'])
    const both = { ...vuln, enemies: vuln.enemies.map((e, i) => (i === 0 ? { ...e, weak: 1 } : e)) }
    expect(displayedIntentValue(both, 0, both.enemies[0].intent!)).toBe(13)
    expect(intentModifierNotes(both, 0, both.enemies[0].intent!)).toEqual(['威圧-25%', '脆弱+50%'])
    // 防御の意図は補正されない
    const guard = { ...both, enemies: both.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'defend' as const, actual: 9 } } : e)) }
    expect(displayedIntentValue(guard, 0, guard.enemies[0].intent!)).toBe(9)
    expect(intentModifierNotes(guard, 0, guard.enemies[0].intent!)).toEqual([])
  })

  it('incomingFrom は表示と同じ式×ヒット数 (被ダメ予測は実際に受ける量)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [])
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'attack' as const, actual: 7, hits: 3 } } : e)), player: { ...s.player, vulnerable: 2 } }
    expect(incomingFrom(s, 0)).toBe(Math.floor(7 * 1.5) * 3)
    expect(incomingTotal(s)).toBe(incomingFrom(s, 0))
  })
})
