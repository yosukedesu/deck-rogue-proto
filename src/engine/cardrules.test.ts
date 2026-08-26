// カードデータの不変条件テスト。確定済みルール表の「0マナスペル」「正味エナジー増」を
// 全カードに対して機械的に固定する。新カード追加時にルール違反を自動で検出するための網。
import { describe, expect, it } from 'vitest'
import { allCards } from './content.ts'
import type { CardDef } from './types.ts'

/** この札をプレイして正味で増えるエナジー (gainEnergy の合計 − コスト) */
function netEnergy(def: CardDef): number {
  const gain = def.effects
    .filter((e) => e.effect === 'gainEnergy')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  return gain - def.cost
}

describe('カードデータの不変条件', () => {
  it('コスト0の札は必ず消滅する (リシャッフルと組んだ無限詠唱ループの禁止)', () => {
    const bad = allCards.filter((c) => c.cost === 0 && c.exhaust !== true)
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('正味エナジーが増える札は必ず消滅する (2026-08-26制定。無限マナループの禁止)', () => {
    // 抜け道の実例: 魔力変換 1E→一時マナ+2 は正味+1。集中(次のカード-1)と
    // 連鎖する思考(詠唱数ぶんドロー)を挟むとエナジーもドローも青天井になる
    const bad = allCards.filter((c) => netEnergy(c) > 0 && c.exhaust !== true)
    expect(bad.map((c) => `${c.name}(${c.cost}E→+${netEnergy(c) + c.cost})`)).toEqual([])
  })

  it('リアクションタイプは onPlay 効果を持たない (伏せ専用の担保)', () => {
    const bad = allCards.filter(
      (c) => c.type === 'reaction' && c.effects.some((e) => e.trigger === 'onPlay'),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('しきい値カード (忘却の刻) は amountMax を必ず持つ', () => {
    const bad = allCards.filter((c) =>
      c.effects.some((e) => e.exhaustThreshold !== undefined && e.amountMax === undefined),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('召喚カードの summonId は実在する置物を指す', () => {
    const ids = new Set(allCards.map((c) => c.id))
    const bad = allCards.filter((c) =>
      c.effects.some(
        (e) =>
          e.effect === 'summonPermanent' &&
          (e.summonId === undefined || !ids.has(e.summonId)),
      ),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })
})
