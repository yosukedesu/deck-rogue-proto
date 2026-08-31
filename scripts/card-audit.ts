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
  // per-X 参照は「典型的な参照量 × 単価」で数える (実測の中央値から)。
  // 被弾は「1ターンあたりのHP損失」の実測 2.5〜5 から典型4 (ブロックで防いだ分は数えない)。
  // カオスは1戦闘で撃つ乱数札の典型3枚
  dealDamagePerDamageTaken: 1.0 * 4,
  applyBurnPerDamageTaken: 1.5 * 4,
  dealDamagePerRandomPlayed: 1.0 * 3,
  // 勢いの変換器 (2026-08-30): 変換時の典型勢い6 (ひばなはプレイごと+2で1ターン8〜10、他リーダーは生成札依存)
  dischargeMomentumBurn: 1.5 * 6,
  dischargeMomentumBlock: 1.0 * 6,
  dischargeGrowth: 1.0 * 5, dischargeGrowthBlock: 1.0 * 5, // 放出時の典型成長5 (このは+1/T+種+2。全消費の手放し対価込み)
  // 青の参照・放出系 (2026-08-31 青の完成回)。詠唱典型3・氷壁典型10・霊気典型2.5・手札典型5
  dealDamagePerCardPlayed: 1.0 * 3,
  gainIceBlockPerCardPlayed: 1.3 * 3,
  drawCardsPerCardPlayed: 3.0 * 3,
  dealDamagePerCardPlayedTotal: 1.0 * 12, // 累計プレイの典型12 (撃つ頃の中盤想定)
  dealDamagePerIceBlock: 1.0 * 10,
  dischargeAether: 2.5, // 霊気典型2.5 × 倍率amount (全消費の手放し対価込み)
  dischargeAetherDraw: 3.0 * 2.5,
  dealDamagePerHandCard: 1.0 * 5,
  gainIceBlockPerHandCard: 1.3 * 5,
  addSpellEcho: 9.0, // 反復1トークン ≈ 典型的な1〜2E呪文のコピー価値9 (StS Burst=1E準拠)
  addCasts: 2.5, // 焚べる: 詠唱+1 ≈ 参照×2〜3の増分
  exhaustFromDeck: 0.6, // 忘却=墓地燃料1枚≈0.6VP (刻・亡骸の期待価値)
  addCardToHand: 3.0, // 骨のナイフ1枚 ≈ 4ダメ(0E)+消滅燃料0.6 の割引現在価値
  dealDamagePerBlock: 1.0 * 8, dealDamagePerPermanent: 1.0 * 3, gainBlockPerPermanent: 1.0 * 3, // 白の参照典型 (自前ブロック8・置物3体)
}
const VP_FLAT: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
const ALLOW = (cost: number) => 6 * cost + 2
/** 条件付き効果の期待値係数。猛り火は実測で「戦闘の55%程度で成立」なので 0.6 で数える */
const COND = (e: DeclarativeEffect) => (e.condition ? 0.6 : 1)

/**
 * 全体化係数の色レート (2026-08-30)。実測は平均1.44体・ソロ率60%・ボスは常にソロ。
 * 赤=全体火力の本家=×1.5 (実測の丸め) で査定し、1体あたりの数値を高く刷ってよい。
 * 他色は×2のまま (割高。§42「全体調整は不要」= 緑白黒の全体札には触らない)
 */
const AOE_MULT: Record<string, number> = { red: 1.5 }

function effectVp(e: DeclarativeEffect, type: string, color?: string): number {
  const aoe = e.target === 'all' ? (AOE_MULT[color ?? ''] ?? 2) : 1
  // 亡骸効果 (onSelfExhausted) はミル・消滅コスト経由でしか発火しない = 期待値0.5
  const necro = e.trigger === 'onSelfExhausted' ? 0.5 : 1
  const mult = aoe * (e.pierce ? 1.25 : 1) * COND(e) * necro
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
    (a, e) => a + effectVp(e, def.type, def.color) * (e.xHits === true ? TYPICAL_X : 1),
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
  const vpRaw = Math.max(...lists.map((l) => vpOfList(l, def)))
  // 追加コストの算入 (2026-09-01 ユーザー質問「消滅コストをメリットとして評価してる？」を機に整備):
  // これまで捨て/消滅コストは査定で罰にも旨味にも数えていなかった。
  // 捨て1枚 = 手札の機会費用 −1.5VP。消滅1枚 = −2VP (この戦闘で二度と使えない) だが、
  // 墓地燃料 (刻・亡骸・per-Exhaust の的) として +0.6VP 戻る = 正味 −1.4VP
  const vp = vpRaw - (def.discardCost ?? 0) * 1.5 - (def.exhaustCost ?? 0) * 1.4
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
