// engine/fusion.ts — カード合成 (工房)。確定済みルール表「カード合成（工房）」
// 異なる緑カード2枚 → 1枚の新カード。手書きレシピ (data/fusions.json) を優先し、
// それ以外は計算合成する。純関数・決定的 = 素材2枚の def だけから結果が決まる
// (リプレイ / Unity 移植に安全。RNG も時刻も使わない)。
import fusionsJson from '../data/fusions.json'
import { getCardDef } from './content.ts'
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
  const dmgs = effects.filter((e) => e.effect === 'dealDamage')
  const blk = effects.some((e) => e.effect === 'gainBlock' || e.effect === 'gainIceBlock')
  // 特性が名前に出る: 多段=乱撃 / 全体=嵐 / 貫通=穿ち
  if (dmgs.some((e) => e.target === 'all')) return '嵐'
  if (dmgs.length >= 2) return '乱撃'
  if (dmgs.some((e) => e.pierce)) return '穿ち'
  if (dmgs.length > 0 && blk) return '構え'
  if (blk) return '盾'
  if (dmgs.length > 0) return '一撃'
  return '祝福'
}

function recipeFor(a: CardDef, b: CardDef): CardDef | null {
  const hit = RECIPES.find(
    (r) => (r.a === a.id && r.b === b.id) || (r.a === b.id && r.b === a.id),
  )
  return hit ? hit.result : null
}

/** タイプの支配順位 (確定済みルール表「カード合成（工房）」): 置物 > リアクション > 呪文 > 物理 */
const TYPE_RANK: Record<string, number> = { permanent: 3, reaction: 2, spell: 1, physical: 0 }
const REACTION_WINDOWS = new Set([
  'onAttackIncoming',
  'onAttacked',
  'onEnemyAction',
  'onEnemyBuffed',
  'onEnemyDefended',
])
/** 置物として誘発できる窓 (hooks.ts が置物にディスパッチするのはこの2つだけ) */
const PERM_WINDOWS = new Set(['onAttackIncoming', 'onAttacked'])

/** 支配側 (結果タイプを与える側) と従属側を決める */
function dominance(a: CardInstance, b: CardInstance): [CardInstance, CardInstance] {
  return TYPE_RANK[a.def.type] >= TYPE_RANK[b.def.type] ? [a, b] : [b, a]
}

interface FusionOutcome {
  readonly def: CardDef
  readonly overBand: boolean
}

/** 合成の計算本体。blockReason と fuseCards が共有する (置物の帯超過を事前検査するため) */
function computeFusion(a: CardInstance, b: CardInstance): FusionOutcome {
  const sameName = a.def.id === b.def.id
  const [domi, sub] = dominance(a, b)
  const resultType = domi.def.type

  // --- 攻撃の特性を抽出して掛け合わせる (多段・貫通・全体の伝播) ---
  // 置物カードの dealDamage は既に毎ターン型なので抽出しない (一回きり分だけが変換対象)
  const dmgOf = (c: CardInstance) =>
    c.def.type === 'permanent' ? [] : c.def.effects.filter((e) => e.effect === 'dealDamage')
  const dmgA = dmgOf(a)
  const dmgB = dmgOf(b)
  const totalDmg = [...dmgA, ...dmgB].reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const hits = Math.min(5, Math.max(dmgA.length, dmgB.length))
  const pierce = [...dmgA, ...dmgB].some((e) => e.pierce === true)
  const allTarget = [...dmgA, ...dmgB].some((e) => e.target === 'all')

  // 結果タイプごとのダメージの置き場所:
  // 置物 → onTurnStart (÷3の毎ターン化) / リアクション → 支配側の主窓 / それ以外 → onPlay
  const primaryWindow =
    resultType === 'reaction'
      ? (domi.def.effects.find((e) => REACTION_WINDOWS.has(e.trigger))?.trigger ?? 'onAttacked')
      : 'onPlay'
  const damageEffects: DeclarativeEffect[] = []
  if (totalDmg > 0) {
    if (resultType === 'permanent') {
      // 置物化: 持続と引き換えに量÷3 (切り上げ)。従者の少年 (1E・毎ターン2ダメ) のラダーに揃う
      damageEffects.push({
        trigger: 'onTurnStart',
        effect: 'dealDamage',
        amount: Math.ceil(totalDmg / 3),
        ...(pierce ? { pierce: true } : {}),
        ...(allTarget ? { target: 'all' as const } : {}),
      })
    } else {
      const per = Math.ceil(totalDmg / hits)
      for (let i = 0; i < hits; i++) {
        damageEffects.push({
          trigger: primaryWindow,
          effect: 'dealDamage',
          amount: per,
          ...(pierce ? { pierce: true } : {}),
          ...(allTarget ? { target: 'all' as const } : {}),
        } as DeclarativeEffect)
      }
    }
  }

  // --- ダメージ以外: 素材カードのタイプに応じてトリガーを結果タイプへ変換 ---
  // 「支配側か」ではなく「素材がもう持続型か」で判定する。同名置物×置物のような
  // 「両方すでに毎ターン型」の合成で従属側まで÷3してしまう二重割引を防ぐ。
  const convert = (e: DeclarativeEffect, srcType: string): DeclarativeEffect | null => {
    if (e.effect === 'dealDamage' && srcType !== 'permanent') return null // 上のダメージ群で処理済み
    if (resultType === 'permanent' && srcType !== 'permanent') {
      // 一回きり → 毎ターン化は量÷3 (切り上げ)。置物が誘発できる窓 (hooks.ts の2窓) だけ残し、
      // それ以外 (onEnemyAction 等は置物にディスパッチされない) は onTurnStart に落とす
      // (量を持たない効果 [negate等] は置物化できない — fuseBlockReason で事前検査済み)
      const trigger = PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onTurnStart'
      return { ...e, trigger, amount: Math.ceil((e.amount ?? 0) / 3) } as DeclarativeEffect
    }
    if (resultType === 'reaction' && srcType !== 'reaction' && !REACTION_WINDOWS.has(e.trigger)) {
      return { ...e, trigger: primaryWindow } as DeclarativeEffect // onPlay効果が罠に吸収される
    }
    return { ...e } // 置物のonTurnStart・リアクションの窓・条件を保持
  }
  const merged: DeclarativeEffect[] = []
  const pushMerged = (raw: DeclarativeEffect | null) => {
    if (raw === null) return
    const twin = merged.find(
      (m) =>
        m.trigger === raw.trigger &&
        m.effect === raw.effect &&
        m.target === raw.target &&
        m.pierce === raw.pierce &&
        // 条件が違う同種効果は合算しない (無条件counterと「HP半分以下」counterが混ざる事故防止)
        JSON.stringify(m.condition) === JSON.stringify(raw.condition),
    )
    if (twin && twin.amount !== undefined && raw.amount !== undefined) {
      merged[merged.indexOf(twin)] = { ...twin, amount: twin.amount + raw.amount }
    } else if (twin && twin.amount === undefined && raw.amount === undefined) {
      // 量を持たない同種効果 (negate など) は重複させても意味がないので1つに畳む
    } else {
      merged.push(raw)
    }
  }
  for (const e of domi.def.effects) pushMerged(convert(e, domi.def.type))
  for (const e of sub.def.effects) pushMerged(convert(e, sub.def.type))
  // 合成札は「ダメージ群を1つと数えて」3効果までの派手枠。あふれたらVPの大きい順に残す
  merged.sort((x, y) => effectVp(y) - effectVp(x))
  const others = merged.slice(0, damageEffects.length > 0 ? 2 : 3)

  // --- 値付け: 置物は寿命込み (×3)。ただし onPlay の一回きり効果は等倍 (2026-08-29。
  // 品質パスで置物に登場時効果が付いたため、一回きり分まで×3する過大査定を防ぐ) ---
  const vpOf = (list: readonly DeclarativeEffect[], type: string): number =>
    list.reduce(
      (acc, e) => acc + effectVp(e) * (type === 'permanent' && e.trigger !== 'onPlay' ? 3 : 1),
      0,
    )

  // --- 価値の保存 (2026-08-30 修正。実プレイのログ解析で発覚した最大のバグ) ---
  // 旧実装は「伝播後の効果」からVPを出してコストを逆算していたため、全体化(×2)・貫通(×1.25)が
  // 合計ダメージ全体に無料で乗り、**合成が価値を増やしていた**
  // (巨獣の踏みつけ5E+薙ぎ払い2E=68VP → 牙蔦の嵐3E=101.5VP。価値+49%・コスト-57%)。
  // 素材の合計VPの85% (確定済みルール表の「合成ボーナス≈15%引き」) に収まるよう、
  // **ダメージ量の方を逆算して伝播の対価を払わせる**。特性の掛け合わせという設計は保つ
  // 価値は素材の合計を**保存**する (削らない)。確定済みルール表の「合成ボーナス≈15%引き」は
  // コスト逆算側の割引 (下の discounted) であって価値の削減ではない。
  // ユーザー指示「同名カードは倍率上げて強いカードが生成されるべき」とも整合する
  const targetVp = vpOf(a.def.effects, a.def.type) + vpOf(b.def.effects, b.def.type)
  if (damageEffects.length > 0) {
    const nonDmgVp = vpOf(others, resultType)
    const unitVp = vpOf([damageEffects[0]], resultType) / Math.max(1, damageEffects[0].amount ?? 1)
    const budget = Math.max(0, targetVp - nonDmgVp)
    // 1ヒットあたりの量を割り付け直す (最低1。多段の形と特性はそのまま維持する)
    const per = Math.max(1, Math.round(budget / Math.max(0.01, unitVp) / damageEffects.length))
    for (let i = 0; i < damageEffects.length; i++) damageEffects[i] = { ...damageEffects[i], amount: per }
  }
  const effects = [...damageEffects, ...others]

  const vp = vpOf(effects, resultType)
  const discounted = vp * 0.85
  // コスト上限を3E→5Eに開放 (2026-08-30)。3E頭打ちだと「7E相当の素材が3Eで出る」効率2.3倍が
  // 構造的に発生していた。緑は5E札 (巨獣の踏みつけ) を素で持つのでコスト帯としては既存の範囲
  const cost = Math.min(5, Math.max(1, Math.round((discounted - 2) / 6)))
  let exhaust = a.def.exhaust === true || b.def.exhaust === true
  let overBand = false
  if (discounted > ALLOW[cost] * 1.5) {
    if (resultType === 'permanent') {
      overBand = true // 置物は消滅で払えない (場に残り続けるため) → 合成不可として扱う
    } else {
      exhaust = true // 帯超過は消滅で払う
    }
  }
  const net = effects
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((acc, e) => acc + (e.amount ?? 0), 0)
  if (net - cost >= 0 && effects.some((e) => REFILL.has(e.effect))) exhaust = true

  const suffix =
    resultType === 'permanent' ? '大樹' : resultType === 'reaction' ? '罠' : suffixOf(effects)
  const name = sameName ? `真・${a.def.name}` : `${wordOf(a.def)}${wordOf(b.def)}の${suffix}`
  const ids = [a.def.id, b.def.id].sort()
  const def: CardDef = {
    id: `fused_${ids[0]}__${ids[1]}`,
    name,
    cost,
    type: resultType,
    color: a.def.color, // 同色同士しか合成できないので素材から継承する

    effects,
    ...(exhaust && resultType !== 'permanent' ? { exhaust: true } : {}),
    ...(a.def.discardCost || b.def.discardCost
      ? { discardCost: (a.def.discardCost ?? 0) + (b.def.discardCost ?? 0) }
      : {}),
    ...(a.def.exhaustCost || b.def.exhaustCost
      ? { exhaustCost: (a.def.exhaustCost ?? 0) + (b.def.exhaustCost ?? 0) }
      : {}),
  }
  return { def, overBand }
}

/** 合成できない理由。null = 合成可 */
export function fuseBlockReason(a: CardInstance, b: CardInstance): string | null {
  if (a.uid === b.uid) return '同じカードは選べない'
  if (recipeFor(a.def, b.def)) return null // レシピは制約を免除 (手書きで裁定済み)
  // 2026-08-30 全色開放 (ユーザー指示「工房は全部に許可」)。合成ロジックは元から色に依存しておらず、
  // v1が緑限定だったのは検証範囲を絞るためだった。**同じ色同士**に限る (多色はカラーパイの越境になるので別途)
  if (a.def.color !== b.def.color) return '合成は同じ色のカード同士のみ'
  if (a.def.modes?.length || b.def.modes?.length) return '選択式カードはレシピでのみ合成できる'
  if (a.def.xCost === true || b.def.xCost === true) return 'Xコスト札は計算合成できない (X参照は査定不能)'
  const all = [...a.def.effects, ...b.def.effects]
  if (!all.every(isComputable)) return 'この効果の組み合わせは合成できない'
  // 置物化する場合、従属側に量を持たない効果 (negate等) があると毎ターン化できない
  // (毎行動negateのような壊れた自動置物が生成されるのを防ぐ)
  const [domi, sub] = dominance(a, b)
  if (domi.def.type === 'permanent' && sub.def.type !== 'permanent') {
    const flats = sub.def.effects.filter((e) => e.amount === undefined)
    if (flats.length > 0) return 'この効果は置物化できない (量を持たないため)'
  }
  if (computeFusion(a, b).overBand) return '強力すぎて置物に収まらない'
  return null
}

/**
 * 計算合成: **特性の掛け合わせ + タイプの支配順位** (確定済みルール表「カード合成（工房）」)。
 * 支配順位 = 置物 > リアクション > 呪文 > 物理。結果はより「持続する」側のタイプになる:
 * - 置物化: 従属側の量÷3で毎ターン化 (打撃6×年輪の大樹 → 毎ターン2ダメ+成長1)
 * - 罠に吸収: onPlay効果がリアクションの窓に移る (打撃×茨の返し → 被攻撃後6ダメ+返し9)
 * - 呪文優位: 魔力が混ざれば呪文 (確定済み定義「呪文=魔力の行使」と整合)
 * 多段・貫通・全体の特性伝播と、VP逆算の値付け (置物は寿命×3) は共通。
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  const recipe = recipeFor(a.def, b.def)
  if (recipe) return recipe
  return computeFusion(a, b).def
}

/**
 * 合成カードの定義をIDから復元する (見つからなければ null)。
 * 合成IDは決定的 (fused_<素材A>__<素材B>) なので、素材を引いて再合成すれば同じ定義が返る。
 * レシピ産 (fusion_*) はレシピ表から引く。
 * イベントログは cardId しか持たないため、描画側はこの解決器で合成カードの名前を引く
 * (2026-08-28 修正: 静的カード表だけ引いていたため「未定義カード: fused_*」で描画がクラッシュした)。
 */
export function resolveFusedDef(id: string): CardDef | null {
  const recipe = RECIPES.find((r) => r.result.id === id)
  if (recipe) return recipe.result
  const m = /^fused_(.+)__(.+)$/.exec(id)
  if (!m) return null
  try {
    const a = getCardDef(m[1])
    const b = getCardDef(m[2])
    return fuseCards({ uid: 'resolve_a', def: a }, { uid: 'resolve_b', def: b })
  } catch {
    return null
  }
}
