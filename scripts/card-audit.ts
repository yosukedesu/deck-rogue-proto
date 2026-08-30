// scripts/card-audit.ts — カードの機械査定 (docs/card-power.md §1-2 の VP 表を実装)。
// 「定価100%」= コスト帯の許容VP (ALLOW = 6×コスト + 2)。報酬札の狙いは 115〜135%。
// 純関数なので engine には置かず scripts に置く (査定はゲームルールではなく開発の物差し)。
import { allCards } from '../src/engine/content.ts'
import type { CardDef, DeclarativeEffect } from '../src/engine/types.ts'

const VP_PER: Record<string, number> = {
  dealDamage: 1.0, gainBlock: 1.0, gainIceBlock: 1.3, drawCards: 3.0, impulseDraw: 2.0,
  gainEnergy: 5.0, addGrowth: 4.0, addMomentum: 1.5, applyBurn: 1.5, addAether: 3.0,
  discountNext: 2.5, counter: 1.0, gainHp: 1.5, weakenEnemy: 5.0, dealDamageDrain: 1.5,
  exposeEnemy: 2.0, dealDamageRandom: 1.0, dealDamageExecute: 1.0, loseHp: -1.5,
}
const VP_FLAT: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
const ALLOW = (cost: number) => 6 * cost + 2
/** 条件付き効果の期待値係数。猛り火は実測で「戦闘の55%程度で成立」なので 0.6 で数える */
const COND = (e: DeclarativeEffect) => (e.condition ? 0.6 : 1)

function effectVp(e: DeclarativeEffect, type: string): number {
  const mult = (e.target === 'all' ? 2 : 1) * (e.pierce ? 1.25 : 1) * COND(e)
  // 置物は寿命込み (×3)。ただし onPlay の一回きり効果は等倍
  const life = type === 'permanent' && e.trigger !== 'onPlay' ? 3 : 1
  const per = VP_PER[e.effect]
  if (per !== undefined) {
    const amt = e.amountMax !== undefined ? (e.amount! + e.amountMax) / 2 : (e.amount ?? 0)
    return per * amt * mult * life
  }
  const flat = VP_FLAT[e.effect]
  return flat !== undefined ? flat * mult * life : 0
}
/** Xコストの典型 X=3 (docs/card-power.md §41)。xHits は効果をX回複製する */
const TYPICAL_X = 3
function vpOfList(list: readonly DeclarativeEffect[], def: CardDef): number {
  return list.reduce(
    (a, e) => a + effectVp(e, def.type) * (e.xHits === true ? TYPICAL_X : 1),
    0,
  )
}
export function assess(def: CardDef): { vp: number; pct: number; computable: boolean } {
  const modes = def.modes ?? []
  // 選択式は「柔軟性の上乗せ」を別に置き、絶対値としては最も高いモードで測る (§37)
  const lists = modes.length > 0 ? modes.map((m) => m.effects) : [def.effects]
  const computable = lists
    .flat()
    .every((e) => VP_PER[e.effect] !== undefined || VP_FLAT[e.effect] !== undefined)
  const vp = Math.max(...lists.map((l) => vpOfList(l, def)))
  // 猛り火の軽減は「そのぶん安く撃てる」= 実効コストが下がる。期待値ぶんだけ帯を絞る
  const cost = (def.xCost === true ? 3 : def.cost) - (def.blazeDiscount ?? 0) * 0.6
  return { vp, pct: (vp / ALLOW(Math.max(0.5, cost))) * 100, computable }
}

// CLI としてだけ出力する (import しただけで全件が流れるのを防ぐ)
if (process.argv[1]?.endsWith('card-audit.ts')) {
  const only = process.argv[2]
  const rows = allCards
    .filter((c) => (only ? c.color === only : true))
    .map((c) => ({ c, a: assess(c) }))
    .filter((r) => r.a.computable)
    .sort((x, y) => y.a.pct - x.a.pct)
  for (const { c, a } of rows) {
    const flag = a.pct > 165 ? '⚠高' : a.pct < 100 ? '↓低' : '  '
    console.log(`${flag} ${String(Math.round(a.pct)).padStart(4)}%  ${c.color} ${c.cost}E ${c.name}`)
  }
  const pcts = rows.map((r) => r.a.pct).sort((a, b) => a - b)
  console.log(`n=${rows.length} 中央${Math.round(pcts[pcts.length >> 1])}% 165%超${pcts.filter((p) => p > 165).length} 100%未満${pcts.filter((p) => p < 100).length}`)
}
