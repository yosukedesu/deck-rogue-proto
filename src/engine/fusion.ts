// engine/fusion.ts — カード合成 (工房)。確定済みルール表「カード合成（工房）」
// 異なる緑カード2枚 → 1枚の新カード。手書きレシピ (data/fusions.json) を優先し、
// それ以外は計算合成する。純関数・決定的 = 素材2枚の def だけから結果が決まる
// (リプレイ / Unity 移植に安全。RNG も時刻も使わない)。
import fusionsJson from '../data/fusions.json'
import { getCardDef } from './content.ts'
import { axesOf } from './run.ts'
import { isUpgraded, upgradeCard, upgradeTier } from './upgrade.ts'
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
  // 引数の順序に依存しない (id順に正規化 = 決定性)
  const [a0, b0] = x.def.id <= y.def.id ? [x, y] : [y, x]
  const sameName = a0.def.id === b0.def.id
  // X札は「両方がX」の時だけXのまま。片方だけなら典型X=3の固定量に畳む (机上レビュー S 提案1)
  const bothX = a0.def.xCost === true && b0.def.xCost === true
  const materialize = (c: CardInstance): CardInstance =>
    c.def.xCost === true && !bothX
      ? { ...c, def: { ...c.def, xCost: undefined, cost: 3, effects: c.def.effects.flatMap((e) => (e.xHits === true ? [0, 1, 2].map(() => ({ ...e, xHits: undefined })) : [e])) } }
      : c
  const a = materialize(a0)
  const b = materialize(b0)
  const [domi0, sub0] = dominance(a, b)

  // --- コスト: 合計−1 (最低1・上限5)。0E素材は値引きにならない (Opusラン R)。X同士はXのまま ---
  const costAsMaterial = (d: CardDef) => (d.xCost === true ? 3 : d.cost)
  const ca = costAsMaterial(a.def)
  const cb = costAsMaterial(b.def)
  const rawSumUncapped = ca === 0 || cb === 0 ? Math.max(ca, cb) : Math.max(1, ca + cb - 1)
  const rawSum = Math.min(5, rawSumUncapped)
  // 重い札は罠に収まらない (机上レビュー S2 提案2): 合計−1 が 2E を超えるならリアクション化せず、
  // 相手側のタイプで出してリアクションの効果をプレイ時へ変換する (旧「切り下げ分を量で払う」は量の無い効果の罠が無償で2Eになる穴)
  let domi = domi0
  let sub = sub0
  if (domi0.def.type === 'reaction' && sub0.def.type !== 'reaction' && !bothX && rawSum > 2) {
    domi = sub0
    sub = domi0
  }
  const resultType = domi.def.type
  const keepModes = TYPE_RANK[resultType] <= TYPE_RANK.spell

  const primaryWindow =
    resultType === 'reaction'
      ? (domi.def.effects.find((e) => REACTION_WINDOWS.has(e.trigger))?.trigger ?? 'onAttacked')
      : 'onPlay'
  const PLAYCARD_ONLY = new Set(['searchDeck', 'retrieveFromDiscard', 'upgradeInHand', 'addCopyToDiscard', 'exhaustFromDeckChoose', 'retrieveFromExhaust', 'playFromExhaust', 'gainSetSlot'])
  const DIES_IN_WINDOW = new Set(['drawCards', 'impulseDraw', 'gainEnergy', 'addCasts'])
  const DEAD_ON_PERMANENT = new Set(['negate', 'growSelf', 'momentumCarryHalf', 'doubleGrowth', 'doubleMomentum', 'dischargeGrowth', 'dischargeGrowthBlock', 'dischargeMomentumDamage', 'dischargeMomentumBlock', 'dischargeMomentumBurn', 'dischargeMomentumGrowth', 'dischargeMomentumVolley', 'dischargeAether', 'dischargeAetherDraw', 'dischargeBurn'])
  // 落とした効果の価値は最大の量効果へ振る (S2: 効果が落ちて素材より劣化する64件の是正。「合成不可」は増やさない)
  const DROP_VP: Record<string, number> = { gainEnergy: 5, drawCards: 3, impulseDraw: 2, addCasts: 2.5, negate: 12, doubleGrowth: 8, doubleMomentum: 6, growSelf: 4, searchDeck: 6, retrieveFromDiscard: 5, upgradeInHand: 6, addCopyToDiscard: 3, exhaustFromDeckChoose: 3, retrieveFromExhaust: 5, playFromExhaust: 8, gainSetSlot: 6, momentumCarryHalf: 8 }
  let droppedVp = 0
  const drop = (e: DeclarativeEffect) => { droppedVp += (DROP_VP[e.effect] ?? 4) * (e.amount !== undefined && DROP_VP[e.effect] !== undefined && ['gainEnergy', 'drawCards', 'impulseDraw', 'addCasts'].includes(e.effect) ? e.amount : 1) }

  /** 素材1枚の効果列を結果タイプへ変換する (列の内部順序は保つ = 蔦の乱舞の交互構造を畳まない) */
  const convertAll = (c: CardInstance): DeclarativeEffect[] => {
    // モードを畳む時 (置物/リアクション化) は最初のモードだけ採る (S2: 「選ぶ」が「両方」になっていた)
    const src = keepModes ? c.def.effects : [...c.def.effects, ...(c.def.modes?.[0]?.effects ?? [])]
    if (resultType === 'permanent' && c.def.type !== 'permanent') {
      const agg: DeclarativeEffect[] = []
      for (const e of src) {
        if (DEAD_ON_PERMANENT.has(e.effect)) { drop(e); continue }
        const twin = agg.find((m) => m.effect === e.effect && m.target === e.target && m.pierce === e.pierce && m.summonId === e.summonId && JSON.stringify(m.condition) === JSON.stringify(e.condition) && m.growthMultiplier === e.growthMultiplier && m.momentumMultiplier === e.momentumMultiplier)
        if (twin && twin.amount !== undefined && e.amount !== undefined) agg[agg.indexOf(twin)] = { ...twin, amount: twin.amount + e.amount }
        else if (!(twin && twin.amount === undefined && e.amount === undefined)) agg.push({ ...e })
      }
      return agg.map((e) => {
        if (e.amount === undefined || PLAYCARD_ONLY.has(e.effect)) return { ...e, trigger: PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onPlay' } as DeclarativeEffect
        const third = Math.floor(e.amount / 3)
        if (third <= 0) return { ...e, trigger: PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onPlay' } as DeclarativeEffect
        const trigger = PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onTurnStart'
        return { ...e, trigger, amount: third, ...(e.amountMax !== undefined ? { amountMax: Math.max(third, Math.floor(e.amountMax / 3)) } : {}) } as DeclarativeEffect
      })
    }
    if (resultType === 'reaction' && c.def.type !== 'reaction') {
      const out: DeclarativeEffect[] = []
      for (const e of src) {
        if (REACTION_WINDOWS.has(e.trigger)) { out.push({ ...e }); continue }
        if (PLAYCARD_ONLY.has(e.effect) || DIES_IN_WINDOW.has(e.effect) || e.effect === 'growSelf' || e.effect === 'momentumCarryHalf') { drop(e); continue }
        if (e.effect === 'gainBlock' || e.effect === 'gainIceBlock') out.push({ ...e, trigger: 'onAttackIncoming' } as DeclarativeEffect)
        else out.push({ ...e, trigger: primaryWindow } as DeclarativeEffect)
      }
      return out
    }
    if (resultType === 'reaction' && c.def.type === 'reaction' && c !== domi) {
      // 従属側のリアクションの窓は支配側の主窓へ揃える (T2: 敵行動時に撃った罠に被攻撃前の効果も乗っていた)
      return src.map((e) => (REACTION_WINDOWS.has(e.trigger) ? ({ ...e, trigger: primaryWindow } as DeclarativeEffect) : { ...e }))
    }
    if (resultType !== 'reaction' && c.def.type === 'reaction') {
      // 重い札に吸われたリアクション: 窓の効果をプレイ時へ (返し→ダメージ・窓ブロック→ブロック・打ち消しは意味が無いので落とす)
      const out: DeclarativeEffect[] = []
      for (const e of src) {
        if (e.effect === 'negate') { drop(e); continue }
        if (e.effect === 'counter') { out.push({ ...e, effect: 'dealDamage', trigger: 'onPlay' } as DeclarativeEffect); continue }
        out.push({ ...e, trigger: 'onPlay', condition: undefined } as DeclarativeEffect)
      }
      return out
    }
    return src.map((e) => ({ ...e }))
  }

  // --- 合体: 列は素材の内部順序を保ち、ブロック単位で並べる。「準備 (成長・勢い・急所等) だけの札」を先に置く
  //     (S2: id順で勢いがダメージ行の後ろに落ち、素材より弱い合成品が183件) ---
  const blocks = [convertAll(domi), convertAll(sub)]
  const hasDamage = (list: readonly DeclarativeEffect[]) => list.some((e) => e.effect === 'dealDamage' && e.trigger === 'onPlay')
  const ordered = blocks[1].length > 0 && !hasDamage(blocks[1]) && hasDamage(blocks[0]) ? [blocks[1], blocks[0]] : blocks
  const merged: DeclarativeEffect[] = []
  const ownerOf: number[] = []
  const twinOf = (raw: DeclarativeEffect, block: number) =>
    merged.findIndex(
      (m, k) =>
        ownerOf[k] !== block && // 同じ素材の中では畳まない (交互構造を保つ)
        m.trigger === raw.trigger && m.effect === raw.effect && m.target === raw.target && m.pierce === raw.pierce &&
        m.summonId === raw.summonId && JSON.stringify(m.condition) === JSON.stringify(raw.condition) &&
        m.growthMultiplier === raw.growthMultiplier && m.momentumMultiplier === raw.momentumMultiplier && m.xHits === raw.xHits,
    )
  let collapsedFlatVp = 0
  const FLAT_VP: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
  let dmgSeen = 0
  ordered.forEach((list, block) => {
    // ダメージ行を持つ札の効果は他方へ畳まない (交互構造・「ダメージの前に成長」の意味を保つ)。準備だけの札は相手の同種へ合算する
    const canMerge = !hasDamage(list)
    for (const raw of list) {
      if (raw.effect === 'dealDamage' && raw.trigger !== 'onTurnStart') {
        if (sameName && block === 1) {
          // 同名合成 (真・化) はダメージ行を対で合算 = 2枚ぶんを圧縮 (真・打撃12・真・二連10×2)
          const rows = merged.map((m, k) => [m, k] as const).filter(([m]) => m.effect === 'dealDamage' && m.trigger === raw.trigger)
          const hit = rows[dmgSeen++]
          if (hit && hit[0].pierce === raw.pierce && hit[0].target === raw.target && hit[0].amount !== undefined && raw.amount !== undefined) {
            merged[hit[1]] = { ...hit[0], amount: hit[0].amount + raw.amount }
            continue
          }
        }
        merged.push(raw); ownerOf.push(block); continue
      }
      const t = canMerge ? twinOf(raw, block) : -1
      if (t >= 0 && merged[t].amount !== undefined && raw.amount !== undefined) {
        merged[t] = { ...merged[t], amount: (merged[t].amount ?? 0) + raw.amount, ...(merged[t].amountMax !== undefined && raw.amountMax !== undefined ? { amountMax: (merged[t].amountMax ?? 0) + raw.amountMax } : {}) }
      } else if (t >= 0 && merged[t].amount === undefined && raw.amount === undefined) {
        collapsedFlatVp += FLAT_VP[raw.effect] ?? 0
      } else {
        merged.push(raw); ownerOf.push(block)
      }
    }
  })
  const isShatter = (e: DeclarativeEffect) => e.effect === 'shatterBlock' || e.effect === 'shatterBlockConvert'
  const QUANTITY = new Set(['dealDamage', 'counter', 'gainBlock', 'gainIceBlock', 'gainHp', 'applyBurn', 'dealDamageDrain'])
  const boostLargest = (list: DeclarativeEffect[], delta: number): void => {
    let best = -1
    for (let i = 0; i < list.length; i++) if (QUANTITY.has(list[i].effect) && list[i].amount !== undefined && (best < 0 || (list[i].amount ?? 0) > (list[best].amount ?? 0))) best = i
    if (best >= 0) list[best] = { ...list[best], amount: Math.max(1, (list[best].amount ?? 0) + delta) }
  }
  // 軸一致ボーナス (2026-09-05 ユーザー裁定 A): 同じ軸の札同士を溶かすと、その軸の小さなおまけが乗る =
  // 「何と何を溶かすか」にデッキの軸の型が出る (S2: 得の95%が「1E札を重い札にタダで貼る」1パターンだった)
  const AXIS_BONUS: Record<string, DeclarativeEffect> = {
    growth: { trigger: 'onPlay', effect: 'addGrowth', amount: 1 },
    trample: { trigger: 'onPlay', effect: 'addMomentum', amount: 2 },
    ramp: { trigger: 'onPlay', effect: 'discountNext', amount: 1 },
    burn: { trigger: 'onPlay', effect: 'applyBurn', amount: 2 },
    ice: { trigger: 'onPlay', effect: 'gainIceBlock', amount: 2 },
    aether: { trigger: 'onPlay', effect: 'addAether', amount: 1 },
    storm: { trigger: 'onPlay', effect: 'addCasts', amount: 1 },
    heal: { trigger: 'onPlay', effect: 'gainHp', amount: 2 },
    fortress: { trigger: 'onPlay', effect: 'gainBlock', amount: 3 },
    retinue: { trigger: 'onPlay', effect: 'gainBlock', amount: 2 },
    graveyard: { trigger: 'onPlay', effect: 'exhaustFromDeck', amount: 1 },
  }
  const sharedAxis = axesOf(a.def).find((ax) => axesOf(b.def).includes(ax) && AXIS_BONUS[ax] !== undefined)
  if (sharedAxis !== undefined) {
    const bonus = { ...AXIS_BONUS[sharedAxis], trigger: resultType === 'reaction' ? primaryWindow : 'onPlay' } as DeclarativeEffect
    const k = merged.findIndex((m) => m.trigger === bonus.trigger && m.effect === bonus.effect && m.condition === undefined && m.target === undefined && m.amount !== undefined)
    if (k >= 0) merged[k] = { ...merged[k], amount: (merged[k].amount ?? 0) + (bonus.amount ?? 0) }
    else { merged.unshift(bonus); ownerOf.unshift(-1) }
  }
  const effects: DeclarativeEffect[] = [...merged.filter(isShatter), ...merged.filter((e) => !isShatter(e))]
  let unpaidVp = collapsedFlatVp + droppedVp
  let costCut = 0
  if (unpaidVp > 0) {
    if (resultType === 'permanent') {
      // 置物の補償: 登場時 (onPlay) の量効果へ等倍。無ければコストで返し (下限=素材の高い方。T2: 打ち消しが消えてコストだけ上がる下位互換)、
      // 余りは登場時ブロックで返す (毎トリガー効果には乗せない。T3: 棘の蔓の毎攻撃ブロック2が6になっていた)
      const onPlayQ = effects.findIndex((e) => e.trigger === 'onPlay' && QUANTITY.has(e.effect) && e.amount !== undefined)
      if (onPlayQ >= 0) { effects[onPlayQ] = { ...effects[onPlayQ], amount: (effects[onPlayQ].amount ?? 0) + Math.round(unpaidVp) }; unpaidVp = 0 }
      else {
        const slack = bothX ? 0 : Math.max(0, rawSum - Math.max(ca, cb))
        costCut = Math.min(slack, Math.floor(unpaidVp / 6))
        unpaidVp -= costCut * 6
        if (unpaidVp >= 3) { effects.push({ trigger: 'onPlay', effect: 'gainBlock', amount: Math.round(unpaidVp) } as DeclarativeEffect) }
        unpaidVp = 0
      }
    } else if (effects.some((e) => QUANTITY.has(e.effect) && e.amount !== undefined)) {
      boostLargest(effects, Math.round(unpaidVp)); unpaidVp = 0
    }
  }
  // 5E上限で切った分は量を比例縮小して払う (S2: 真・巨獣の踏みつけ=5Eで100ダメ)
  if (!bothX && rawSumUncapped > 5) {
    const ratio = 5 / rawSumUncapped
    for (let i = 0; i < effects.length; i++) if (QUANTITY.has(effects[i].effect) && effects[i].amount !== undefined) effects[i] = { ...effects[i], amount: Math.max(1, Math.floor((effects[i].amount ?? 0) * ratio)) }
  }

  // モード: 同名は各モードを対で合算、異なる選択式同士は連結。片方だけ選択式なら相手の効果は共通部に入る (上で並べ済み)
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

  let cost = bothX ? 1 : rawSum
  if (resultType === 'reaction' && !bothX && cost > 2) {
    // リアクション同士で2Eを超えた分は出力で払う (切り下げ1Eにつき最大の量効果−6)
    for (let cut = cost - 2; cut > 0; cut--) boostLargest(effects, -6)
    cost = 2
  }
  // 補償先の量効果が無い時はコストで返す (T2: 打ち消しが跡形もなく消えてコストだけ上がる下位互換)。下限は素材の高い方のコスト
  if (unpaidVp >= 6 && !bothX) cost = Math.max(Math.max(ca, cb), cost - Math.floor(unpaidVp / 6))
  if (costCut > 0) cost = Math.max(Math.max(ca, cb), cost - costCut)

  // --- 歯止め (現行のまま) ---
  const all = [...effects, ...(modes ?? []).flatMap((m) => m.effects)]
  // 消滅の継承: 効果が1つも残らなかった素材の消滅は引き継がない (S2: 茨の返し×樹液=茨の返し+消滅の劣化)
  const contributed = (c: CardInstance) => convertAll(c).length > 0
  let exhaust = (a.def.exhaust === true && contributed(a)) || (b.def.exhaust === true && contributed(b))
  const necroCost = a.def.necroCost !== undefined || b.def.necroCost !== undefined ? Math.min(a.def.necroCost ?? 99, b.def.necroCost ?? 99) : undefined
  if (necroCost !== undefined) exhaust = true
  if (all.some((e) => e.effect === 'doubleGrowth' || e.effect === 'doubleMomentum')) exhaust = true
  if (effects.some((e) => e.effect === 'gainEnergyMax')) exhaust = true
  if (all.filter((e) => e.effect === 'impulseDraw').reduce((acc, e) => acc + (e.amount ?? 0), 0) >= 4) exhaust = true
  const net = all.filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext').reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const refills = all.some((e) => REFILL.has(e.effect))
  const freeIfPhysical = a.def.freeIfHandAllPhysical === true || b.def.freeIfHandAllPhysical === true
  const freeIfMomentum = [a.def.freeIfMomentumAtLeast, b.def.freeIfMomentumAtLeast].filter((v): v is number => v !== undefined)
  const conditionalFree = freeIfPhysical || freeIfMomentum.length > 0
  if (!bothX && refills && (net - cost >= 0 || conditionalFree)) {
    if (resultType !== 'permanent') exhaust = true
    else while (net - cost >= 0 && cost < 5) cost++
  }
  if (resultType === 'permanent') exhaust = false

  const PERM_SUFFIX: Record<string, string> = { red: '炉', blue: '泉', white: '祭壇', black: '柩' }
  const suffix =
    resultType === 'permanent' ? (PERM_SUFFIX[a.def.color ?? ''] ?? '大樹') : resultType === 'reaction' ? '罠' : suffixOf(effects)
  const stemOf = (d: CardDef): string =>
    d.id.startsWith('fused_') || d.id.startsWith('fusion_') ? d.name.replace(/^真・/, '').replace(/\+$/, '').split('の')[0].slice(0, 3) : wordOf(d)
  const wa = stemOf(a.def)
  const wb = stemOf(b.def)
  const uniq = [...new Set([...wa, ...wb])].join('')
  // 語の重複は畳む (T1: 角牙牙の乱撃)。相手の語が何も足さない時は「大」を冠して素材と同名になるのを避ける (角牙の嵐×落ち葉の刃=大角牙の嵐)
  const stem = (wa === wb || uniq === wa || uniq === wb ? `大${uniq}` : uniq).slice(0, 4)
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
  if (a.def.id.startsWith('status_') || b.def.id.startsWith('status_')) return '負傷・呪い・火傷は合成できない (使えない札)' // T1: 「色違い」と出ていた
  if (a.def.color !== b.def.color) return '合成は同じ色のカード同士のみ'
  return null
}

/**
 * 計算合成: 効果の合体 + 特性の掛け合わせ + タイプの支配順位 (確定済みルール表「カード合成（工房）」)。
 * 手書きレシピ (data/fusions.json) が最優先。純関数・決定的 (素材2枚の def だけから結果が決まる)
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  // 鍛えの引き継ぎ (2026-09-05 ユーザー裁定 C。T3 で作り直し): 素材のどちらかが鍛え済み (+) なら、
  // 鍛えていない方の素材を先に鍛えてから合体し、結果を鍛え済み (+) として出す = 鍛えた値は消えず、鍛えが結果全体に乗る。
  // (旧「素に戻して結果を1回鍛える」は結果のティアが倍率/単位を拾うとダメージ行が素に戻る穴 = T3 不具合b)
  const anyUpgraded = isUpgraded(a) || isUpgraded(b)
  const lift = (c: CardInstance): CardInstance => (anyUpgraded && !isUpgraded(c) && upgradeTier(c.def) !== 'none' ? upgradeCard(c) : c)
  const recipe = recipeFor(a.def, b.def)
  if (recipe) {
    // レシピ産にも鍛えを引き継ぐ (T3 不具合a: 守りの蔓+×茨の返し=茨の砦が素材1枚より弱かった)
    return anyUpgraded && upgradeTier(recipe) !== 'none' ? upgradeCard({ uid: 'recipe', def: recipe }).def : recipe
  }
  const merged = mergeFusion(lift(a), lift(b))
  if (!anyUpgraded) return merged
  return merged.name.endsWith('+') ? merged : { ...merged, name: `${merged.name}+` }
}

/**
 * 合成の注記 (2026-09-05 T2/T3: 「効果の合体」と言いつつ量が黙って変わる・軸一致がなぜ乗ったか分からない)。
 * CLI の FusePreview と UI の工房プレビューが同じ文言を出す
 */
export function fusionNotes(a: CardInstance, b: CardInstance): string[] {
  const notes: string[] = []
  if (recipeFor(a.def, b.def)) notes.push('⭐レシピ: 手書きの一品')
  const shared = axesOf(a.def).find((ax) => axesOf(b.def).includes(ax))
  const AXIS_JA: Record<string, string> = { growth: '成長+1', trample: '勢い+2', ramp: '次のカード-1', burn: '延焼+2', ice: '氷壁+2', aether: '霊気+1', storm: '詠唱+1', heal: '回復+2', fortress: 'ブロック+3', retinue: 'ブロック+2', graveyard: 'ミル1' }
  if (shared && AXIS_JA[shared]) notes.push(`軸一致 (${shared}): ${AXIS_JA[shared]} のおまけ`)
  if (isUpgraded(a) || isUpgraded(b)) notes.push('鍛えの引き継ぎ: 鍛えていない側の素材も鍛えてから合体 (結果は+)')
  const ca = a.def.xCost === true ? 3 : a.def.cost
  const cb = b.def.xCost === true ? 3 : b.def.cost
  if ((a.def.xCost === true) !== (b.def.xCost === true)) notes.push('X札は片方だけなら X=3 の固定量に畳む')
  if (ca === 0 || cb === 0) notes.push('0E素材は値引きにならない (高い方のコスト)')
  if (ca + cb - 1 > 5 && ca > 0 && cb > 0) notes.push('5E上限: 超えた分だけ量を比例縮小')
  const types = [a.def.type, b.def.type]
  if ((a.def.modes?.length || b.def.modes?.length) && (types.includes('permanent') || types.includes('reaction'))) notes.push('選択式は置物化・罠化では最初のモードだけを採る')
  if (types.includes('permanent') && !(a.def.type === 'permanent' && b.def.type === 'permanent')) notes.push('置物化: 量は同種を合計して÷3で毎ターン化 (3未満は登場時1回)。打ち消し・倍化・放出は落ちて価値を振り替え')
  if (types.includes('reaction') && !(a.def.type === 'reaction' && b.def.type === 'reaction')) {
    notes.push(ca + cb - 1 > 2 ? '重い札は罠に収まらない: 合計−1が2Eを超えるので相手側のタイプで出る (返し→ダメージ・窓ブロック→ブロック)' : 'リアクション化: 相手の効果は罠の窓で解決 (ドロー・一時マナ・サーチは落ちて価値を振り替え)')
  }
  if (a.def.type === 'reaction' && b.def.type === 'reaction') notes.push('罠同士: 窓は支配側の主窓に揃う。2Eを超える分は量で払う')
  return notes
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
