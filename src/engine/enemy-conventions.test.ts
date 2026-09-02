// 裁定済みの敵設計規約の機械固定 (2026-09-02 StS2解析の全体改善・第8波)。
// offenders 空配列方式: 規約に触れる敵を追加すると名指しで落ちる。
import { describe, expect, it } from 'vitest'
import { allCards, allEnemies, resolveEncounter } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import { tierFor } from './map.ts'
import { ENEMY_GIMMICK_KEYS } from './traits.ts'

describe('敵設計規約の機械固定 (enemy-conventions)', () => {
  it('とげ持ちは防御行動を持たない (2026-08-30 裁定「とげの問いは殴らないで待つ、ではない」)', () => {
    const offenders = allEnemies
      .filter((e) => (e.thorns ?? 0) > 0)
      .filter((e) =>
        [...e.moves, ...(e.movesBelowHalf ?? [])].some((m) => m.kind === 'defend'),
      )
      .map((e) => e.id)
    expect(offenders).toEqual([])
  })

  it('応援役 (rally) は固定ローテーションを持ち、応援が2連続しない (2026-08-28 本家準拠の間欠化)', () => {
    const offenders: string[] = []
    for (const e of allEnemies) {
      const hasRally = e.moves.some((m) => m.kind === 'rally')
      if (!hasRally) continue
      const seq = e.sequence
      if (!seq || seq.length === 0) {
        offenders.push(`${e.id}: sequenceなし`)
        continue
      }
      const kindOf = (id: string) => e.moves.find((m) => m.id === id)?.kind
      for (let i = 0; i < seq.length; i++) {
        const next = seq[(i + 1) % seq.length]
        if (kindOf(seq[i]) === 'rally' && kindOf(next) === 'rally') {
          offenders.push(`${e.id}: rally 2連続`)
          break
        }
      }
    }
    expect(offenders).toEqual([])
  })

  it('盗み持ちは全員、盗み成立後の次の宣言が逃走になる (2026-08-30 レース保証。合成逃走を実挙動で検証)', () => {
    const thieves = allEnemies.filter((e) => e.moves.some((m) => m.kind === 'steal-gold'))
    expect(thieves.length).toBeGreaterThan(0)
    for (const def of thieves) {
      let s = freshCombat('set-confirm', def.id, 42)
      // 盗み成立済みの状態を外科的に作る (宣言即成立の仕様)
      s = {
        ...s,
        enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, stolenGold: 15 } : e)),
      }
      s = withHand(s, [])
      s = applyCommand(s, { type: 'EndTurn' })
      if (s.phase !== 'player-turn') continue // 実行フェーズで逃走済みならそれで正
      const it = s.enemies[0]
      expect(
        it.fled === true || it.intent?.kind === 'flee',
        `${def.id}: 盗んだのに逃げる気配がない`,
      ).toBe(true)
    }
  })

  it('幕1のデバフ密度の床: 幕1プールの1割以上が状態異常の付与源 (退行防止)', () => {
    const pool = tierFor(1, 5)
    let carriers = 0
    for (const encId of pool) {
      const has = resolveEncounter(encId).some((mem) => {
        const d = allEnemies.find((e) => e.id === mem.enemyId)
        if (!d) return false
        const tables = [d.moves, d.movesVsSet ?? [], d.movesBelowHalf ?? []]
        return tables.some((t) =>
          t.some((m) => m.inflict !== undefined || m.setAlt?.inflict !== undefined),
        )
      })
      if (has) carriers++
    }
    expect(carriers / pool.length).toBeGreaterThanOrEqual(0.1)
  })

  it('タイマー敵の規約: 敵定義のフィールドは既知のホワイトリストのみ (即死・敗北条件型の新機構は必ずここで衝突する)', () => {
    // StS2 TheInsatiable (タイマー切れ即死) 炎上の教訓 (docs/sts2-reference.md §7・balance-policy.md)。
    // 2026-09-02 レビュー是正: 旧断言 typeof(e.enrage ?? 2)==='number' は恒真で何も検証していなかった。
    // キーのホワイトリスト走査に差し替え = 「countdownで敗北」等の新フィールドを足すと名指しで落ち、
    // 規約 (罰は筋力の漸増か予告付き大技のみ) との突き合わせを強制する
    const KNOWN = new Set([
      'id', 'name', 'archetype', 'flavor', 'maxHp', 'moves', 'sequence', 'sequenceLoopFrom',
      'movesBelowHalf', 'sequenceBelowHalf', 'sequenceBelowHalfLoopFrom',
      'movesVsSet', 'movesVsTokens', 'movesWhenAlone', 'sequenceWhenAlone',
      ...ENEMY_GIMMICK_KEYS, // ギミック系は traits.ts と共有 (表示網羅テストと同じ一次資料)
    ])
    const offenders: string[] = []
    for (const e of allEnemies) {
      for (const k of Object.keys(e)) {
        if (!KNOWN.has(k)) offenders.push(`${e.id}.${k}`)
      }
    }
    expect(offenders).toEqual([])
  })
})

describe('威圧の全色アクセス保証 (2026-08-26 裁定の機械固定)', () => {
  it('各色のカードプールに weakenEnemy を持つ非レア札が1枚以上ある', () => {
    const colors = ['green', 'blue', 'red', 'white', 'black'] as const
    for (const color of colors) {
      const carriers = allCards.filter(
        (c) =>
          c.color === color &&
          c.rarity !== 'rare' &&
          JSON.stringify(c.effects).includes('weakenEnemy'),
      )
      expect(carriers.length, `${color} に威圧の非レア札がない`).toBeGreaterThanOrEqual(1)
    }
  })
})

describe('延焼ティックと与ダメ系カウンタの相互作用 (2026-09-02 明文化+固定)', () => {
  it('延焼ティックは regenBreak に算入される = バーンで再生を止められる', () => {
    // 苔まといの主: regen5・regenBreak30。延焼30を積んでターンを渡すとティック30が閾値に届く
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 30, hp: e.maxHp - 1 })) }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'RegenBroken')).toBe(true)
  })

  it('延焼ティックは enrageEveryDamage の壁を跨いで筋力を進める = バーンはタイマーの代償を払う', () => {
    // 門番: enrageEveryDamage 80。延焼85のティックで壁を1回跨ぐ
    let s = freshCombat('set-confirm', 'enemy_warden', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 85, maxHp: 500, hp: 500 })) }
    const str0 = s.enemies[0].strength
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBeGreaterThan(str0)
    expect(
      s.eventLog.some((e) => e.type === 'StrengthGained' && e.reason === 'enrage-damage'),
    ).toBe(true)
  })
})

