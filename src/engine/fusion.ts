// engine/fusion.ts — カード合成 (工房)。確定済みルール表「カード合成（工房）」
// 異なる緑カード2枚 → 1枚の新カード。手書きレシピ (data/fusions.json) を優先し、
// それ以外は計算合成する。純関数・決定的 = 素材2枚の def だけから結果が決まる
// (リプレイ / Unity 移植に安全。RNG も時刻も使わない)。
import fusionsJson from '../data/fusions.json'
import type { CardDef, CardInstance, DeclarativeEffect } from './types.ts'

interface FusionRecipe {
  readonly a: string
  readonly b: string
  readonly result: CardDef
}
const RECIPES = fusionsJson as readonly FusionRecipe[]

/**
 * 効果1点あたりのVP (docs/card-power.md §2 の機械化)。
 * 査定エンジンがそのまま値付けエンジンになる — 合成のコストはここから逆算する。
 */
const VP_PER: Record<string, number> = {
  dealDamage: 1.0,
  gainBlock: 1.0,
  gainIceBlock: 1.3,
  drawCards: 3.0,
  impulseDraw: 2.0,
  gainEnergy: 5.0,
  addGrowth: 4.0,
  addMomentum: 1.5,
  applyBurn: 1.5,
  addAether: 3.0,
  discountNext: 2.5,
  counter: 1.0,
  gainHp: 1.5,
  weakenEnemy: 5.0,
  dealDamageDrain: 1.5,
  exposeEnemy: 2.0,
  dealDamageRandom: 1.0,
  dealDamageExecute: 1.0,
  loseHp: -1.5,
}
const VP_FLAT: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
/** コスト別の許容VP (§1)。合成は 1〜3E に収める (costCap 対策) */
const ALLOW: Record<number, number> = { 1: 8, 2: 14, 3: 20 }

function effectVp(e: DeclarativeEffect): number {
  const mult = e.target === 'all' ? 2 : 1
  const per = VP_PER[e.effect]
  if (per !== undefined) return per * (e.amount ?? 0) * mult * (e.pierce ? 1.25 : 1)
  const flat = VP_FLAT[e.effect]
  if (flat !== undefined) return flat * mult
  return 0
}

/** 計算合成の対象にできない効果 (価値を機械査定できない = レシピでのみ扱う) */
function isComputable(e: DeclarativeEffect): boolean {
  return VP_PER[e.effect] !== undefined || VP_FLAT[e.effect] !== undefined
}

const REFILL = new Set([
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
])

/** 名前生成: 軸→語幹 (緑v1)。レシピ札は手書き名が優先される */
const WORD: readonly (readonly [string, string])[] = [
  ['addGrowth', '蔦'],
  ['doubleGrowth', '花'],
  ['addMomentum', '角'],
  ['gainEnergy', '樹'],
  ['gainBlock', '皮'],
  ['drawCards', '葉'],
  ['gainHp', '露'],
  ['counter', '棘'],
  ['weakenEnemy', '根'],
  ['dealDamage', '牙'],
]
function wordOf(def: CardDef): string {
  for (const [eff, w] of WORD) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  return '樹'
}
function suffixOf(effects: readonly DeclarativeEffect[]): string {
  const dmg = effects.some((e) => e.effect.startsWith('dealDamage') || e.effect === 'counter')
  const blk = effects.some((e) => e.effect === 'gainBlock' || e.effect === 'gainIceBlock')
  if (dmg && blk) return '構え'
  if (blk) return '盾'
  if (dmg) return '一撃'
  return '祝福'
}

function recipeFor(a: CardDef, b: CardDef): CardDef | null {
  const hit = RECIPES.find(
    (r) => (r.a === a.id && r.b === b.id) || (r.a === b.id && r.b === a.id),
  )
  return hit ? hit.result : null
}

/** 合成できない理由。null = 合成可 */
export function fuseBlockReason(a: CardInstance, b: CardInstance): string | null {
  if (a.uid === b.uid) return '同じカードは選べない'
  if (a.def.id === b.def.id) return '同名カード同士は合成できない'
  if (recipeFor(a.def, b.def)) return null // レシピは制約を免除 (手書きで裁定済み)
  if (a.def.color !== 'green' || b.def.color !== 'green') return '合成は緑カード同士のみ (v1)'
  if (a.def.type !== b.def.type) return 'タイプの違うカードはレシピでのみ合成できる'
  if (a.def.type === 'reaction') return 'リアクションはレシピでのみ合成できる'
  if (a.def.modes?.length || b.def.modes?.length) return '選択式カードはレシピでのみ合成できる'
  const all = [...a.def.effects, ...b.def.effects]
  if (!all.every(isComputable)) return 'この効果の組み合わせは合成できない'
  return null
}

/**
 * 計算合成: 効果を結合して同種を合算し、VP からコストを逆算する。
 * - 合成ボーナス ≈ 15%引き (2枚→1枚の投資に報いる)
 * - コストは 1〜3E にクランプ。許容VPの150%を超える場合は「消滅」を自動付与して合法化
 * - 0E+補充・正味エナジーの無限ループ規約もここで機械チェック (消滅付与で解決)
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  const recipe = recipeFor(a.def, b.def)
  if (recipe) return recipe

  // 同種効果 (trigger+effect+target+pierce が同じ) は量を合算
  const merged: DeclarativeEffect[] = []
  for (const e of [...a.def.effects, ...b.def.effects]) {
    const twin = merged.find(
      (m) =>
        m.trigger === e.trigger &&
        m.effect === e.effect &&
        m.target === e.target &&
        m.pierce === e.pierce,
    )
    if (twin && twin.amount !== undefined && e.amount !== undefined) {
      merged[merged.indexOf(twin)] = { ...twin, amount: twin.amount + e.amount }
    } else {
      merged.push({ ...e })
    }
  }
  // 合成札は3効果までの派手枠。あふれたらVPの大きい順に残す
  merged.sort((x, y) => effectVp(y) - effectVp(x))
  const effects = merged.slice(0, 3)

  const vp = effects.reduce((acc, e) => acc + effectVp(e), 0)
  const discounted = vp * 0.85 // 合成ボーナス
  const cost = Math.min(3, Math.max(1, Math.round((discounted - 2) / 6)))
  let exhaust = a.def.exhaust === true || b.def.exhaust === true
  if (discounted > ALLOW[cost] * 1.5) exhaust = true // 帯超過は消滅で払う
  // 無限ループ規約 (cardrules と同じ判定): 正味の値段が0以下 + 補充 → 消滅必須
  const net = effects
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((acc, e) => acc + (e.amount ?? 0), 0)
  if (net - cost >= 0 && effects.some((e) => REFILL.has(e.effect))) exhaust = true

  const name = `${wordOf(a.def)}${wordOf(b.def)}の${suffixOf(effects)}`
  const ids = [a.def.id, b.def.id].sort()
  return {
    id: `fused_${ids[0]}__${ids[1]}`,
    name,
    cost,
    type: a.def.type,
    color: 'green',
    effects,
    ...(exhaust ? { exhaust: true } : {}),
    ...(a.def.discardCost || b.def.discardCost
      ? { discardCost: (a.def.discardCost ?? 0) + (b.def.discardCost ?? 0) }
      : {}),
  }
}
