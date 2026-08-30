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
  // per-X 参照・放出系は「典型参照量 × 単価」(scripts/card-audit.ts §49 と同じ前提。2026-08-30)
  dealDamagePerDamageTaken: 4.0, // 被弾の典型4 × 1.0
  applyBurnPerDamageTaken: 6.0, // 被弾4 × 延焼1.5
  dealDamagePerRandomPlayed: 3.0, // 乱数札の典型3枚 × 1.0
  dischargeMomentumBurn: 9.0, // 勢いの典型6 × 延焼1.5
  dischargeMomentumBlock: 6.0, // 勢いの典型6 × 1.0
  dischargeBurn: 6.0, // 放出時の延焼の典型6 × 1.0 (DoTを手放す対価込み)
  dealDamageCleave: 1.3, // 対象+倒したら別の敵に同値 (連鎖は条件付きなので+30%)
  exhaustFromDeck: 0.6, // 忘却=墓地燃料1枚≈0.6VP (刻・亡骸の期待価値。2026-08-31 ミル札の合成解禁)
  // 青の参照・放出系の解禁 (2026-08-31 ユーザー指示「工房の参照スケーリング札を解禁」)。
  // 典型値は scripts/card-audit.ts と同一 (詠唱3・氷壁10・霊気2.5・手札5)。
  // per-X効果は形を変えずそのまま引き継がれ、VPだけ典型値で数える = 条件付きダメージと同じ配管
  dealDamagePerCardPlayed: 3.0,
  gainIceBlockPerCardPlayed: 3.9,
  drawCardsPerCardPlayed: 9.0,
  dealDamagePerIceBlock: 10.0,
  dischargeAether: 2.5,
  dischargeAetherDraw: 7.5,
  dealDamagePerHandCard: 5.0,
  gainIceBlockPerHandCard: 6.5,
  addSpellEcho: 9.0, // 反復1トークン ≈ 典型コピー価値9
  // 白の参照系の解禁 (2026-08-31 白Opusラン指摘「集結・城壁砕きが全ペア合成不可 =
  // コミット型デッキほど工房の価値が下がる」)。典型: 自前ブロック8・置物3体
  dealDamagePerBlock: 8.0,
  dealDamagePerPermanent: 3.0,
  gainBlockPerPermanent: 3.0,
}
const VP_FLAT: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
/** コスト別の許容VP (§1)。ALLOW = 6×コスト + 2 (+2 = カード1枚の機会費用＝札束補正) */
const ALLOW: Record<number, number> = { 1: 8, 2: 14, 3: 20, 4: 26, 5: 32 }

function effectVp(e: DeclarativeEffect): number {
  const mult = e.target === 'all' ? 2 : 1
  const per = VP_PER[e.effect]
  // 幅を持つ量 (乱数・処刑) は平均値で数える (2026-08-30。最小値査定だと
  // 火運の賭け2〜16が「2」で値付けされ、合成が350%の1E札を生む価値漏れになっていた)
  const amt = e.amountMax !== undefined ? ((e.amount ?? 0) + e.amountMax) / 2 : (e.amount ?? 0)
  // 条件付き効果は期待値係数0.6 (scripts/card-audit.ts と同じ前提。2026-08-30)。
  // 3効果の枠にあぶれた条件付きダメージは無条件ダメージへ換金されるが、
  // 満額(1.0)だと条件の対価がタダで外れる水増しになる — 0.6掛けなら公正な換金
  const cond = e.condition !== undefined ? 0.6 : 1
  // 亡骸効果 (onSelfExhausted) はミル・消滅コスト経由でしか発火しない = 期待値0.5で数える
  const necro = e.trigger === 'onSelfExhausted' ? 0.5 : 1
  if (per !== undefined) return per * amt * mult * (e.pierce ? 1.25 : 1) * cond * necro
  const flat = VP_FLAT[e.effect]
  if (flat !== undefined) return flat * mult * cond * necro
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
  // 条件付き (猛り火など) のダメージは抽出しない = convert 側で条件ごと引き継ぐ (2026-08-30。
  // 旧実装は条件を落として無条件ダメージに合算しており、423ペアで猛り火の条件が消えていた)
  const dmgOf = (c: CardInstance) =>
    c.def.type === 'permanent'
      ? []
      : c.def.effects.filter(
          (e) => e.effect === 'dealDamage' && e.condition === undefined && e.trigger === 'onPlay',
        )
  const dmgA = dmgOf(a)
  const dmgB = dmgOf(b)
  const totalDmg = [...dmgA, ...dmgB].reduce((acc, e) => acc + (e.amount ?? 0), 0)
  // ヒット合算 (2026-08-30 ユーザー裁定。旧「最大ヒット数に按分」は多段×多段が
  // ヒット減+消滅の下位互換になり、デッキが強いほど工房が無価値になっていた)。
  // どちらかが多段ならヒット数を合算する (上限5) = 三連の角×二連の蔦打ち → 5ヒットの派手枠。
  // VP保存は不変なので1ヒットあたりの量は下がるが、多段の伝播 (成長・勢いが全ヒットに乗る) は膨らむ
  const anyMulti = dmgA.length > 1 || dmgB.length > 1
  const hits = anyMulti ? Math.min(5, dmgA.length + dmgB.length) : 1
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
    if (
      e.effect === 'dealDamage' &&
      srcType !== 'permanent' &&
      e.condition === undefined &&
      e.trigger === 'onPlay'
    ) {
      return null // 上のダメージ群で処理済み (条件付き・亡骸 onSelfExhausted は通す)
    }
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
  // コスト逆算側の割引 (定価125%帯での値付け) であって価値の削減ではない。
  // ユーザー指示「同名カードは倍率上げて強いカードが生成されるべき」とも整合する
  const targetVp = vpOf(a.def.effects, a.def.type) + vpOf(b.def.effects, b.def.type)
  const othersArr = [...others]
  // 「量」の効果 = 予算に合わせて削れるもの。焚き火の「鍛える」が触れるのと同じ集合に揃える
  // (ドロー・成長・勢い等の単位効果と参照スケーリングには触らない)
  const BLOCKY = new Set([
    'dealDamage', // 無条件分は抽出済みなので、ここに来るのは条件付き (猛り火等) のみ
    'gainBlock',
    'gainIceBlock',
    'counter',
    'gainHp',
    'applyBurn',
    'dealDamageDrain',
    'dealDamageRandom',
  ])
  const blockIdx = othersArr.flatMap((e, i) => (BLOCKY.has(e.effect) ? [i] : []))
  const unitVp =
    damageEffects.length > 0 ? vpOf([{ ...damageEffects[0], amount: 1 }], resultType) : 0
  const canScale = unitVp > 0 || blockIdx.length > 0

  /** VP予算に収まるよう「量」の効果を割り付け直す (多段の形・特性・単位効果は維持) */
  const fitTo = (budgetVp: number): void => {
    if (unitVp > 0) {
      const budget = Math.max(0, budgetVp - vpOf(othersArr, resultType))
      const per = Math.max(1, Math.round(budget / unitVp / damageEffects.length))
      for (let i = 0; i < damageEffects.length; i++) {
        damageEffects[i] = { ...damageEffects[i], amount: per }
      }
      return
    }
    if (blockIdx.length === 0) return
    const isBlock = (i: number) => blockIdx.includes(i)
    const fixedVp = vpOf(
      othersArr.filter((_, i) => !isBlock(i)),
      resultType,
    )
    const blockVp = vpOf(
      othersArr.filter((_, i) => isBlock(i)),
      resultType,
    )
    const ratio = Math.max(0, (budgetVp - fixedVp) / Math.max(0.01, blockVp))
    for (const i of blockIdx) {
      const amt = Math.max(1, Math.round((othersArr[i].amount ?? 0) * ratio))
      othersArr[i] = { ...othersArr[i], amount: amt }
    }
  }
  // 初回は**ダメージだけ**を素材の合計VPへ合わせる — 特性の伝播 (全体×2・貫通×1.25・多段) の
  // 対価をダメージ量で払わせるための手順であって、防御量まで動かす意図はない
  if (unitVp > 0) fitTo(targetVp)
  let effects = [...damageEffects, ...othersArr]

  // 合成札は「報酬札と同じ定価125%帯」で値付けする (2026-08-30)。
  // VP表は「カード1枚の機会費用 +2VP」を含む (ALLOW = 6×コスト + 2) ため、2枚を1枚にすると
  // 許容VPが2減る。定価100%で値付けし直すと 34%のペアでコストが素材の合計より上がっていた
  // (合成ラボで「打撃1E×牙の一撃2E→4E」として可視化された)。125%帯なら報酬札の水準
  // (docs/card-power.md「報酬札は定価115〜135%」) とも整合する。
  // コスト上限は3E→5E (2026-08-30)。3E頭打ちだと「7E相当の素材が3Eで出る」効率2.3倍が構造的に出る
  const costOf = (list: readonly DeclarativeEffect[]): number =>
    Math.min(5, Math.max(1, Math.round((vpOf(list, resultType) / 1.25 - 2) / 6)))
  let cost = costOf(effects)

  // --- 圧縮なのに重くなる、を禁じる (2026-08-30。合成ラボが可視化した穴) ---
  // 2枚を1枚にする機構でコストが素材の合計を超えると、プレイヤーから見て素直に損になる
  // (別々に撃てば安く同じ量が出る)。超える場合は **出力を削って払う** —
  // 「合成不可」を増やさずに契約 (コスト ≤ 素材コストの合計) を守る
  const costAsMaterial = (d: CardDef) => (d.xCost === true ? 3 : d.cost)
  const sumCost = Math.min(5, Math.max(1, costAsMaterial(a.def) + costAsMaterial(b.def)))
  let clampExhaust = false
  if (cost > sumCost && canScale) {
    cost = sumCost
    // 強ペアの価値粉砕を止める (2026-08-30 Opusテスターの指摘「良い札同士だと必ず劣化する = 工房が
    // ゴミ圧縮機」)。圧縮の床を125%帯でなく**150%帯+消滅**に緩める — 帯超過を消滅で払う既存契約の
    // 再利用で、「強い2枚 → 強い1枚だが一度きり」の決断に変わる。置物は消滅で払えないので従来通り
    // 圧縮の損が25%以内なら従来どおり圧縮する (使い回しを守る)。それを超える強ペアだけ
    // 価値を150%帯まで残し、超過を消滅で払う — 境界ケースまで消滅にすると
    // 「わずかな超過で一回きり化」という別の理不尽が生まれるため
    const normal = ALLOW[cost] * 1.25
    if (resultType !== 'permanent' && normal < targetVp * 0.75) {
      const capped = ALLOW[cost] * 1.5 * 1.25
      fitTo(Math.min(targetVp, capped))
      clampExhaust = true // 125%帯を超えて残した価値は消滅で払う
    } else {
      fitTo(normal)
    }
    effects = [...damageEffects, ...othersArr]
    cost = Math.min(cost, costOf(effects)) // 削りすぎて安く収まるならその安い方を採る
  }

  // リアクションのコスト上限2E (確定済みルール表「リアクションのコスト上限」。2026-08-31
  // 黒Opusランの指摘: 呪詛返し×怨嗟が3E/reactionを生んで機械判定をすり抜けていた)。
  // 素材コスト超過と同じ「出力を削って払う」方式で2Eに収める
  let reactionOverCap = false
  if (resultType === 'reaction' && cost > 2) {
    if (canScale) {
      cost = 2
      const normal = ALLOW[2] * 1.25
      if (normal < targetVp * 0.75) {
        fitTo(Math.min(targetVp, ALLOW[2] * 1.5 * 1.25))
        clampExhaust = true
      } else {
        fitTo(normal)
      }
      effects = [...damageEffects, ...othersArr]
    } else {
      reactionOverCap = true // 削れる量が無いのに2Eを超える (ほぼ到達不能) = 合成不可
    }
  }

  let vp = vpOf(effects, resultType)
  let exhaust = a.def.exhaust === true || b.def.exhaust === true || clampExhaust
  if (clampExhaust && vp <= ALLOW[cost] * 1.25) exhaust = a.def.exhaust === true || b.def.exhaust === true // 圧縮後に125%帯へ収まったなら消滅は不要
  let overBand = false
  const bandCap = () => ALLOW[cost] * 1.5 * 1.25 // 帯超過の判定線 (VP換算)
  if (vp > bandCap()) {
    if (resultType !== 'permanent') exhaust = true // 帯超過は消滅で払う
    else {
      // 置物は消滅で払えない (場に残り続けるため)。量を圧縮して帯に収める。
      // 圧縮できる量が無い置物 (成長のみ等) はコストを上げて収める
      if (canScale) {
        fitTo(bandCap())
        effects = [...damageEffects, ...othersArr]
        vp = vpOf(effects, resultType)
      }
      while (vp > bandCap() && cost < 5) cost++
      overBand = vp > bandCap() // 5Eでも収まらない置物だけが合成不可
    }
  }
  overBand = overBand || reactionOverCap
  const net = effects
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((acc, e) => acc + (e.amount ?? 0), 0)
  if (net - cost >= 0 && effects.some((e) => REFILL.has(e.effect))) exhaust = true

  const PERM_SUFFIX: Record<string, string> = { red: '炉', blue: '泉', white: '祭壇', black: '柩' }
  const suffix =
    resultType === 'permanent'
      ? (PERM_SUFFIX[a.def.color ?? ''] ?? '大樹')
      : resultType === 'reaction'
        ? '罠'
        : suffixOf(effects)
  // 同じ語の畳語 (焔焔・牙牙) は日本語として不自然なので「大焔」の形に畳む (2026-08-30)
  const wa = wordOf(a.def)
  const wb = wordOf(b.def)
  const stem = wa === wb ? `大${wa}` : `${wa}${wb}`
  const name = sameName ? `真・${a.def.name}` : `${stem}の${suffix}`
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
  if (a.def.necroCost !== undefined || b.def.necroCost !== undefined) {
    return '亡骸プレイ持ちは計算合成できない (一度きりの再演は査定不能)'
  }
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
