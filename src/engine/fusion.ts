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
  ['applyBurn', '焔'],
  ['gainIceBlock', '氷'],
  ['negate', '封'],
  ['dealDamageDrain', '血'],
  ['gainHp', '光'],
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
  ['dealDamageRandom', '賭'],
  ['impulseDraw', '閃'],
]
/** 色別の語彙上書き (2026-08-31 白ラン指摘「白素材から牙葉の祭壇=緑語彙が生成」への是正) */
const COLOR_WORD: Record<string, readonly (readonly [string, string])[]> = {
  black: [
    ['dealDamageDrain', '血'],
    ['exhaustFromDeck', '墓'],
    ['loseHp', '贄'],
    ['gainBlock', '骨'],
    ['drawCards', '冥'],
    ['gainHp', '宵'],
    ['dealDamage', '影'],
  ],
  blue: [
    ['gainIceBlock', '氷'],
    ['negate', '封'],
    ['addAether', '霊'],
    ['addSpellEcho', '谺'],
    ['drawCards', '書'],
    ['dealDamage', '潮'],
  ],
  red: [
    ['applyBurn', '焔'],
    ['dealDamageRandom', '賭'],
    ['impulseDraw', '閃'],
    ['addMomentum', '烈'],
    ['gainEnergy', '儀'],
    ['gainBlock', '炭'],
    ['drawCards', '燼'],
    ['dealDamage', '火'],
  ],
  white: [
    ['summonPermanent', '旗'],
    ['dealDamagePerPermanent', '列'],
    ['gainHp', '光'],
    ['weakenEnemy', '威'],
    ['dealDamagePerBlock', '壁'],
    ['gainBlock', '盾'],
    ['dealDamage', '聖'],
    ['drawCards', '典'],
  ],
}
function wordOf(def: CardDef): string {
  for (const [eff, w] of COLOR_WORD[def.color ?? ''] ?? []) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  for (const [eff, w] of WORD) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  // フォールバックは色の語で (緑以外の合成が「樹」になる違和感への対処 2026-08-30)
  const FALLBACK: Record<string, string> = { red: '火', blue: '水', white: '光', black: '影' }
  return FALLBACK[def.color ?? ''] ?? '樹'
}
function suffixOf(effects: readonly DeclarativeEffect[]): string {
  const dmgs = effects.filter((e) => e.effect === 'dealDamage' || e.effect === 'dealDamageRandom')
  const blk = effects.some((e) => e.effect === 'gainBlock' || e.effect === 'gainIceBlock')
  // 特性が名前に出る: 多段=乱撃 / 全体=嵐 / 貫通=穿ち
  if (dmgs.some((e) => e.target === 'all')) return '嵐'
  if (dmgs.length >= 2) return '乱撃'
  if (dmgs.some((e) => e.pierce)) return '穿ち'
  if (dmgs.length > 0 && blk) return '構え'
  if (blk) return '盾'
  if (dmgs.length > 0) return '一撃'
  if (effects.some((e) => e.effect === 'applyBurn')) return '熾火'
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


/**
 * 合成の本体＝**効果の合体**（2026-09-05 ユーザー裁定「工房は全て合成できるようにしたい。設計から考え直そう」→ ask_user A/A/A/A）。
 * 旧・価値保存（VP査定から量とコストを逆算）は査定表に無い効果が出るたび合成不可を増やし、緑49%・黒46%のペアが
 * 合成できなくなっていた。新モデルは査定を使わない:
 *  - 結果は2枚の効果を全部持つ札。同種効果は量を合算（同名2枚＝「真・」化が自然に成立）
 *  - コスト＝合計−1（最低1・上限5。両方0Eなら0E。X札はX参照を保つ）＝本家形の「2枚を1枚に、1E得」
 *  - 特性の伝播（多段合算〔上限5〕・貫通・全体）と支配順位（置物＞リアクション＞呪文＞物理）は従来どおり
 *  - 置物化: 量のある効果は÷3で毎ターン化、量の無い効果（打ち消し・倍化・サーチ等）は「登場時に1回」
 *  - 選択式は相手の効果を共通部（モードを問わず解決する effects）に足す。両方が選択式ならモードを連結
 *  - 歯止めは現行のまま: 消滅の自動付与（0E/正味エナジー+補充・倍化・上限ランプ・衝動4以上・亡骸）・リアクション2E上限・
 *    工房産の誘発ごと置物は鍛え不可（upgrade.ts）。「合成不可」は同じ札・色違いだけ
 */
function mergeFusion(x: CardInstance, y: CardInstance): CardDef {
  // 引数の順序に依存しない (id順に正規化 = 決定性。効果の並びと名前の語順が入れ替わらない)
  const [a0, b0] = x.def.id <= y.def.id ? [x, y] : [y, x]
  const sameName = a0.def.id === b0.def.id
  // X札は「両方がX」の時だけXのまま。片方だけなら典型X=3の固定量に畳む (2026-09-05 机上レビュー S 提案1:
  // 非X素材の印字コストがXに消えて「1Eで5E札」になっていた。巨獣の踏みつけ×蔦の連撃=1Eで55ダメの実測)
  const bothX = a0.def.xCost === true && b0.def.xCost === true
  const materialize = (c: CardInstance): CardInstance =>
    c.def.xCost === true && !bothX
      ? { ...c, def: { ...c.def, xCost: undefined, cost: 3, effects: c.def.effects.flatMap((e) => (e.xHits === true ? [0, 1, 2].map(() => ({ ...e, xHits: undefined })) : [e])) } }
      : c
  const a = materialize(a0)
  const b = materialize(b0)
  const [domi, sub] = dominance(a, b)
  const resultType = domi.def.type
  const keepModes = TYPE_RANK[resultType] <= TYPE_RANK.spell

  const primaryWindow =
    resultType === 'reaction'
      ? (domi.def.effects.find((e) => REACTION_WINDOWS.has(e.trigger))?.trigger ?? 'onAttacked')
      : 'onPlay'
  // playCard 経路でしか解決できない効果 (山札/捨て札/手札の選択が要る): 置物なら登場時、リアクションでは落とす (提案5-1)
  const PLAYCARD_ONLY = new Set(['searchDeck', 'retrieveFromDiscard', 'upgradeInHand', 'addCopyToDiscard', 'growSelf', 'exhaustFromDeckChoose', 'retrieveFromExhaust', 'playFromExhaust', 'gainSetSlot'])
  // 敵フェーズでは死ぬ効果 (全捨て・全回復で消える): リアクションでは落とす (提案5-2)
  const DIES_IN_WINDOW = new Set(['drawCards', 'impulseDraw', 'gainEnergy', 'addCasts'])
  // 置物では参照量が0で死ぬ効果 (打ち消しは窓が無い・倍化/放出は登場時に参照0): 落とす (提案5-3)
  const DEAD_ON_PERMANENT = new Set(['negate', 'growSelf', 'momentumCarryHalf', 'doubleGrowth', 'doubleMomentum', 'dischargeGrowth', 'dischargeGrowthBlock', 'dischargeMomentumDamage', 'dischargeMomentumBlock', 'dischargeMomentumBurn', 'dischargeMomentumGrowth', 'dischargeMomentumVolley', 'dischargeAether', 'dischargeAetherDraw', 'dischargeBurn'])

  /** 素材1枚の効果を結果タイプへ変換する。置物化は同種を合算してから÷3 (切り捨て。0なら登場時に1回) (提案4) */
  const convertAll = (c: CardInstance): DeclarativeEffect[] => {
    const src = keepModes ? c.def.effects : [...c.def.effects, ...(c.def.modes ?? []).flatMap((m) => m.effects)]
    if (resultType === 'permanent' && c.def.type !== 'permanent') {
      const agg: DeclarativeEffect[] = []
      for (const e of src) {
        if (DEAD_ON_PERMANENT.has(e.effect)) continue
        const twin = agg.find((m) => m.effect === e.effect && m.target === e.target && m.pierce === e.pierce && m.summonId === e.summonId && JSON.stringify(m.condition) === JSON.stringify(e.condition) && m.growthMultiplier === e.growthMultiplier && m.momentumMultiplier === e.momentumMultiplier)
        if (twin && twin.amount !== undefined && e.amount !== undefined) agg[agg.indexOf(twin)] = { ...twin, amount: twin.amount + e.amount }
        else if (!(twin && twin.amount === undefined && e.amount === undefined)) agg.push({ ...e })
      }
      return agg.map((e) => {
        if (e.amount === undefined || PLAYCARD_ONLY.has(e.effect)) return { ...e, trigger: PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onPlay' } as DeclarativeEffect // 登場時に1回
        const third = Math.floor(e.amount / 3)
        if (third <= 0) return { ...e, trigger: PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onPlay' } as DeclarativeEffect // 3未満は毎ターン化せず登場時に1回
        const trigger = PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onTurnStart'
        return { ...e, trigger, amount: third, ...(e.amountMax !== undefined ? { amountMax: Math.max(third, Math.floor(e.amountMax / 3)) } : {}) } as DeclarativeEffect
      })
    }
    if (resultType === 'reaction' && c.def.type !== 'reaction') {
      const out: DeclarativeEffect[] = []
      for (const e of src) {
        if (REACTION_WINDOWS.has(e.trigger)) { out.push({ ...e }); continue }
        if (PLAYCARD_ONLY.has(e.effect) || DIES_IN_WINDOW.has(e.effect) || e.effect === 'growSelf') continue
        if (e.effect === 'gainBlock' || e.effect === 'gainIceBlock') out.push({ ...e, trigger: 'onAttackIncoming' } as DeclarativeEffect)
        else out.push({ ...e, trigger: primaryWindow } as DeclarativeEffect)
      }
      return out
    }
    return src.map((e) => ({ ...e }))
  }

  // --- 合体: 効果は混ぜずに並べる (提案3。特性の伝播=全体/貫通の無償配布は旧・価値保存モデルの名残) ---
  const merged: DeclarativeEffect[] = []
  const twinOf = (raw: DeclarativeEffect) =>
    merged.find(
      (m) =>
        m.trigger === raw.trigger && m.effect === raw.effect && m.target === raw.target && m.pierce === raw.pierce &&
        m.summonId === raw.summonId && JSON.stringify(m.condition) === JSON.stringify(raw.condition) &&
        m.growthMultiplier === raw.growthMultiplier && m.momentumMultiplier === raw.momentumMultiplier && m.xHits === raw.xHits,
    )
  let collapsedFlatVp = 0 // 量を持たない同種効果を1つに畳んだ分は最大の量効果へ振る (提案5-5)
  const FLAT_VP: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
  const pushMerged = (raw: DeclarativeEffect) => {
    // ダメージ行は行のまま並べる (多段の旨味=成長が回数ぶん乗る、は残る)
    if (raw.effect === 'dealDamage' && raw.trigger !== 'onTurnStart') { merged.push(raw); return }
    const twin = twinOf(raw)
    if (twin && twin.amount !== undefined && raw.amount !== undefined) {
      merged[merged.indexOf(twin)] = { ...twin, amount: twin.amount + raw.amount, ...(twin.amountMax !== undefined && raw.amountMax !== undefined ? { amountMax: twin.amountMax + raw.amountMax } : {}) }
    } else if (twin && twin.amount === undefined && raw.amount === undefined) {
      collapsedFlatVp += FLAT_VP[raw.effect] ?? 0
    } else {
      merged.push(raw)
    }
  }
  for (const e of convertAll(domi)) pushMerged(e)
  // 同名合成 (真・化) はダメージ行も対で合算する = 2枚ぶんの量を1枚に圧縮 (真・打撃=12、真・二連=10×2)。異名は行のまま並べる
  let dmgSeen = 0
  for (const e of convertAll(sub)) {
    if (sameName && e.effect === 'dealDamage' && e.trigger !== 'onTurnStart') {
      const rows = merged.map((m, i) => [m, i] as const).filter(([m]) => m.effect === 'dealDamage' && m.trigger === e.trigger)
      const hit = rows[dmgSeen++]
      if (hit && hit[0].pierce === e.pierce && hit[0].target === e.target && hit[0].amount !== undefined && e.amount !== undefined) {
        merged[hit[1]] = { ...hit[0], amount: hit[0].amount + e.amount }
        continue
      }
    }
    pushMerged(e)
  }
  const isShatter = (e: DeclarativeEffect) => e.effect === 'shatterBlock' || e.effect === 'shatterBlockConvert'
  const QUANTITY = new Set(['dealDamage', 'counter', 'gainBlock', 'gainIceBlock', 'gainHp', 'applyBurn', 'dealDamageDrain'])
  const boostLargest = (list: DeclarativeEffect[], delta: number): void => {
    let best = -1
    for (let i = 0; i < list.length; i++) if (QUANTITY.has(list[i].effect) && list[i].amount !== undefined && (best < 0 || (list[i].amount ?? 0) > (list[best].amount ?? 0))) best = i
    if (best >= 0) list[best] = { ...list[best], amount: Math.max(1, (list[best].amount ?? 0) + delta) }
  }
  const effects: DeclarativeEffect[] = [...merged.filter(isShatter), ...merged.filter((e) => !isShatter(e))]
  if (collapsedFlatVp > 0) boostLargest(effects, Math.round(collapsedFlatVp))

  // モード: 同名は各モードを対で合算 (真・化の趣旨)、異なる選択式同士は連結 (提案5-4)
  let modes: CardDef['modes'] | undefined
  if (keepModes && (domi.def.modes?.length || sub.def.modes?.length)) {
    if (sameName && a.def.modes) {
      modes = a.def.modes.map((m, i) => ({
        ...m,
        effects: m.effects.map((e, k) => {
          const o = b.def.modes?.[i]?.effects[k]
          return o && o.effect === e.effect && e.amount !== undefined && o.amount !== undefined ? { ...e, amount: e.amount + o.amount } : e
        }),
      }))
    } else modes = [...(domi.def.modes ?? []), ...(sub.def.modes ?? [])]
  }

  // --- コスト: 合計−1 (最低1・上限5)。両方0Eなら0E。X同士はXのまま ---
  const costAsMaterial = (d: CardDef) => (d.xCost === true ? 3 : d.cost)
  // 0E素材は値引きにならない (2026-09-05 Opusラン R: 「一番安い札×一番強い札」が常に最善手＝0E札が万能クーポンになっていた)。
  // −1 は両方が1E以上の時だけ。片方0Eなら高い方のコスト、両方0Eなら0E
  const ca = costAsMaterial(a.def)
  const cb = costAsMaterial(b.def)
  const rawSum = ca === 0 || cb === 0 ? Math.max(ca, cb) : Math.min(5, Math.max(1, ca + cb - 1))
  let cost = bothX ? 1 : rawSum
  if (resultType === 'reaction' && !bothX && cost > 2) {
    // リアクションのコスト上限2E: 切り下げた1Eにつき最大の量効果を−6 (=1E相当) して出力で払う (提案2)
    for (let cut = cost - 2; cut > 0; cut--) boostLargest(effects, -6)
    cost = 2
  }

  // --- 歯止め (現行のまま) ---
  const all = [...effects, ...(modes ?? []).flatMap((m) => m.effects)]
  let exhaust = a.def.exhaust === true || b.def.exhaust === true
  const necroCost = a.def.necroCost !== undefined || b.def.necroCost !== undefined ? Math.min(a.def.necroCost ?? 99, b.def.necroCost ?? 99) : undefined
  if (necroCost !== undefined) exhaust = true
  if (all.some((e) => e.effect === 'doubleGrowth' || e.effect === 'doubleMomentum')) exhaust = true
  if (effects.some((e) => e.effect === 'gainEnergyMax')) exhaust = true
  if (all.filter((e) => e.effect === 'impulseDraw').reduce((acc, e) => acc + (e.amount ?? 0), 0) >= 4) exhaust = true
  const net = all.filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext').reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const refills = all.some((e) => REFILL.has(e.effect))
  const freeIfPhysical = a.def.freeIfHandAllPhysical === true || b.def.freeIfHandAllPhysical === true
  const freeIfMomentum = [a.def.freeIfMomentumAtLeast, b.def.freeIfMomentumAtLeast].filter((v): v is number => v !== undefined)
  const conditionalFree = freeIfPhysical || freeIfMomentum.length > 0 // 条件0Eは消滅判定では0Eとして数える (提案6)
  if (!bothX && refills && (net - cost >= 0 || conditionalFree)) {
    if (resultType !== 'permanent') exhaust = true
    else while (net - cost >= 0 && cost < 5) cost++
  }
  if (resultType === 'permanent') exhaust = false

  const PERM_SUFFIX: Record<string, string> = { red: '炉', blue: '泉', white: '祭壇', black: '柩' }
  const suffix =
    resultType === 'permanent' ? (PERM_SUFFIX[a.def.color ?? ''] ?? '大樹') : resultType === 'reaction' ? '罠' : suffixOf(effects)
  // 工房産を素材にした時は、その名前の語幹を引き継いで語を足す (2026-09-05 Opusラン R: 素材「角牙の嵐+」と結果「角牙の嵐」が同名になる衝突)
  const stemOf = (d: CardDef): string =>
    d.id.startsWith('fused_') || d.id.startsWith('fusion_') ? d.name.replace(/^真・/, '').replace(/\+$/, '').split('の')[0].slice(0, 3) : wordOf(d)
  const wa = stemOf(a.def)
  const wb = stemOf(b.def)
  const stem = wa === wb ? `大${wa}` : `${wa}${wb}`.slice(0, 4)
  const name = sameName ? `真・${a.def.name}` : `${stem}の${suffix}`
  const ids = [a0.def.id, b0.def.id]
  const def: CardDef = {
    id: `fused_${ids[0]}__${ids[1]}`,
    name,
    cost,
    type: resultType,
    color: a.def.color,
    effects,
    ...(modes !== undefined ? { modes } : {}),
    ...(bothX ? { xCost: true } : {}),
    ...(exhaust ? { exhaust: true } : {}),
    ...((a.def.retain === true || b.def.retain === true) && resultType !== 'permanent' ? { retain: true } : {}),
    ...(a.def.discardCost || b.def.discardCost ? { discardCost: (a.def.discardCost ?? 0) + (b.def.discardCost ?? 0) } : {}),
    ...(a.def.exhaustCost || b.def.exhaustCost ? { exhaustCost: (a.def.exhaustCost ?? 0) + (b.def.exhaustCost ?? 0) } : {}),
    ...(necroCost !== undefined ? { necroCost } : {}),
    ...(freeIfPhysical ? { freeIfHandAllPhysical: true } : {}),
    ...(freeIfMomentum.length > 0 ? { freeIfMomentumAtLeast: Math.min(...freeIfMomentum) } : {}),
    ...(a.def.blazeDiscount !== undefined || b.def.blazeDiscount !== undefined ? { blazeDiscount: Math.max(a.def.blazeDiscount ?? 0, b.def.blazeDiscount ?? 0) } : {}),
    ...(a.def.exhaustUnlessExposedEnemy === true || b.def.exhaustUnlessExposedEnemy === true ? { exhaustUnlessExposedEnemy: true } : {}),
    ...(a.def.axis || b.def.axis ? { axis: [...new Set([...(a.def.axis ?? []), ...(b.def.axis ?? [])])] } : {}),
  }
  return def
}

/** 合成できない理由。null = 合成可。効果の合体モデルでは「同じ札」「色違い」だけが不可 */
export function fuseBlockReason(a: CardInstance, b: CardInstance): string | null {
  if (a.uid === b.uid) return '同じカードは選べない'
  if (recipeFor(a.def, b.def)) return null
  if (a.def.color !== b.def.color) return '合成は同じ色のカード同士のみ'
  return null
}

/**
 * 計算合成: 効果の合体 + 特性の掛け合わせ + タイプの支配順位 (確定済みルール表「カード合成（工房）」)。
 * 手書きレシピ (data/fusions.json) が最優先。純関数・決定的 (素材2枚の def だけから結果が決まる)
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  const recipe = recipeFor(a.def, b.def)
  if (recipe) return recipe
  return mergeFusion(a, b)
}

/**
 * 合成カードの定義をIDから復元する (見つからなければ null)。
 * 合成IDは決定的 (fused_<素材A>__<素材B>) なので、素材を引いて再合成すれば同じ定義が返る。
 * レシピ産 (fusion_*) はレシピ表から引く。
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
