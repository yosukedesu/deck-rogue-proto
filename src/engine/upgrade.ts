// engine/upgrade.ts — カードを鍛える (焚き火・ショップ・手札で鍛える) の純関数。
// 2026-09-02 run.ts から移設: 戦闘内の「手札で鍛える」(upgradeInHand) が combat.ts から呼ぶため、
// run.ts (combat.ts を import する) との循環を避けて独立モジュールにした。run.ts は再エクスポートで互換維持
import type { CardDef, CardInstance, DeclarativeEffect } from './types.ts'

/**
 * 強化の対象になる「量」の効果 (確定済みルール表「焚き火」)。
 * 単位効果 (ドロー・成長など) はティア③で+1、per-X はティア②のコスト-1で強化される。
 */
const UPGRADABLE_EFFECTS = new Set([
  'dealDamage',
  'gainBlock',
  'gainIceBlock',
  'applyBurn',
  'counter',
  'gainHp',
  'dealDamageDrain',
  'dealDamageRandom',
  'dealDamageExecute',
])

/**
 * ティア③で+1する「単位」の効果。
 * dealDamagePerCardPlayed / exhaustFromDeck は 0E でコストを削れない参照札 (余波・墓暴き) の
 * 受け皿として追加 (2026-08-28 全カード解放)。ドローしない×Nは有限なので倍率+1でも安全。
 * drawCardsPerCardPlayed 等のドロー×Nは入れない (倍率+1=×2ドローは無限ループの危険地帯) — ④の例外表で受ける
 */
const UNIT_EFFECTS = new Set([
  'addCardToHand', // 骨刃の舞+ = ナイフ+1 (本家の+準拠)
  'empowerShivs', // 急所読み+ = 常在+1
  'drawCards',
  'impulseDraw',
  'addGrowth',
  'addMomentum',
  'addAether',
  'gainEnergy',
  'discountNext',
  'dealDamagePerCardPlayed',
  'exhaustFromDeck',
])

/**
 * ティア④: 同軸おまけの手書き例外表 (2026-08-28 全カード解放)。
 * ②コスト-1が規約違反 (0E+補充=消滅必須) になり、③の対象効果も持たない補充参照札の受け皿。
 * おまけは札自身の軸から外れない (カラーパイ・報酬抽選の軸判定を動かさない)。先頭に挿入する
 * (霊気の奔流は「霊気+2 → 放出」の順で解決されることに意味がある)
 */
const BONUS_UPGRADES: Record<string, readonly DeclarativeEffect[]> = {
  // 連鎖する思考+: 自分自身も詠唱数に数えるフレーバーの +1ドロー
  blue_chain_thought: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  // 霊気の奔流+: 放出の前に霊気+2 (実質ドロー+2)
  blue_aether_torrent: [{ trigger: 'onPlay', effect: 'addAether', amount: 2 }],
  // 木陰の守り+: 固定ブロック+4を追加 (上限参照のコスト-1は0Eに落とさない裁定の受け皿。
  // 倍率には触れない安全弁を守りつつ、上限5で 10→14 ≈ 量+50%相当)
  // ---- per-Xダメージ参照のコスト強化封じ (2026-08-31) の受け皿: 同軸のおまけを足す ----
  blue_storm_lash: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 5 }], // 固定の初撃5
  // 抱え込み (2026-08-31): ドローは手札=弾を増やす同軸のおまけ
  blue_weight_of_wisdom: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  blue_knowledge_torrent: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  blue_ripple_blade: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 3 }],
  blue_storm_echo: [{ trigger: 'onAttacked', effect: 'dealDamage', amount: 4 }],
  blue_ice_lance: [{ trigger: 'onPlay', effect: 'gainIceBlock', amount: 4 }], // 氷壁を足してから撃つ
  red_all_in: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 6 }],
  white_rally: [{ trigger: 'onPlay', effect: 'gainBlock', amount: 4 }], // 隊列を組んでから撃つ
  // プール拡充 (2026-08-31): per-X参照でコスト強化を封じた札の受け皿
  blue_page_wind: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  blue_rolling_wave: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  black_grave_pressure: [{ trigger: 'onPlay', effect: 'exhaustFromDeck', amount: 2 }], // 自分で燃料を足してから刈る
  white_rank_thrust: [{ trigger: 'onPlay', effect: 'gainBlock', amount: 4 }],
  red_streak_bet: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 3 }], // 固定の床3 (茨の報い型)
  // 刃の葬列+ = ナイフをもう1枚 (per-Exhaust参照はコストに触れない裁定の受け皿)
  black_blade_procession: [
    { trigger: 'onPlay', effect: 'addCardToHand', amount: 1, summonId: 'black_shiv_token' },
  ],
  // 滾る血汐+ = ドレイン4を追加 (回復回数の参照はコストに触れない裁定の受け皿。自分で1回鳴らせる)
  black_seething_blood: [{ trigger: 'onPlay', effect: 'dealDamageDrain', amount: 4 }],
  // 上限参照の1E札はコストを0Eへ落とさない裁定 (2026-08-30) の受け皿
  green_sapling_strike: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 4 }],
}

/** 手札を補充する効果 (0E+補充=消滅必須、の規約判定。cardrules.test.ts と同じ定義) */
const REFILL_FOR_UPGRADE = new Set([
  'addCardToHand', // トークン生成も手札の補充 (0E化の無限ループ規約対象)
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
])

function allEffectsOf(def: CardDef): readonly DeclarativeEffect[] {
  return [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
}

/** コスト-1すると無限ループ規約 (0E+補充=消滅必須 / 正味エナジー) に違反するか */
function costCutViolates(def: CardDef): boolean {
  if (def.exhaust === true) return false
  const newCost = def.cost - 1
  const eff = allEffectsOf(def)
  const refill = eff.some((e) => REFILL_FOR_UPGRADE.has(e.effect))
  if (!refill) return false
  const net = eff
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  return net - newCost >= 0
}

/**
 * どのティアで強化されるか。'none' = 強化不可。
 * 2026-08-28 全カード解放: gainEnergyMax の一律ブロックを撤廃 (上限ランプはコスト-1で強化。
 * gainEnergyMax は UPGRADABLE / UNIT のどちらにも無いので量は絶対に増えない = 複利安全弁は
 * 「量を強化しない」形で維持)。現行データでは全カードがいずれかのティアに落ちる
 * (テストで機械固定)。'none' は将来のデータ追加への防衛用に残す
 */
export type UpgradeTier = 'amount' | 'cost' | 'unit' | 'bonus' | 'none' | 'mult' | 'threshold'

/**
 * 本家形の鍛える (2026-09-04 ユーザー裁定「ok」。StS2 507枚の OnUpgrade 集計: 量+1が最多185・ダメ102・
 * コスト-1はパワー/スキル52・参照札は倍率そのもの〔Heavy Blade ×3→×5・Rampage +5→+8〕)。
 * その札のアイデンティティの数字を伸ばす: ①参照倍率+1 → ②単位+1 (勢いは+2) と量≥5の+50% →
 * ③しきい値-1 と量+50% → ④量+50% → ⑤コスト-1 (置物・呪文で数字の無い札)。
 * 緑で先行 (id が green_)。他色は解凍時に切り替える = 旧3段仕様のまま
 */
const MULT_EFFECTS = new Set([
  'dealDamagePerBlock', 'dealDamagePerPermanent', 'gainBlockPerPermanent',
  'dealDamagePerEnergyMax', 'gainBlockPerEnergyMax', 'dealDamagePerAttackPlayed',
  'dealDamagePerWeak', 'dealDamagePerNegStrength', 'dealDamagePerDamageTaken', 'applyBurnPerDamageTaken',
  'dealDamagePerRandomPlayed', 'dealDamagePerHandCard', 'gainIceBlockPerHandCard',
  'dischargeGrowth', 'dischargeGrowthBlock', 'dischargeMomentumDamage', 'dischargeMomentumBlock', 'dischargeMomentumBurn', 'dischargeMomentumVolley',
  'dealDamagePerCardPlayed', 'dealDamagePerExhaust', 'dealDamageDrainPerExhaust', 'gainBlockPerExhaust', 'dealDamagePerSelfHpLost', 'dealDamagePerHeal',
])
const UNIT_EFFECTS_V2 = new Set([
  'drawCards', 'impulseDraw', 'addGrowth', 'addMomentum', 'addAether', 'addCasts', 'gainEnergy',
  'exposeEnemy', 'weakenEnemy', 'summonPermanent', 'upgradeInHand', 'addCardToHand', 'empowerShivs', 'exhaustFromDeck',
])
const AMOUNT_V2 = new Set([...UPGRADABLE_EFFECTS, 'growSelf'])
const hasMult = (e: DeclarativeEffect) =>
  (MULT_EFFECTS.has(e.effect) && e.amount !== undefined) || e.growthMultiplier !== undefined || e.momentumMultiplier !== undefined
const hasThreshold = (e: DeclarativeEffect) => e.condition?.minGrowth !== undefined || e.condition?.minMomentum !== undefined
const isGreenRule = (def: CardDef) => def.id.startsWith('green_')

/** 効果列1つぶんの本家形ティア (モードごとにも使う) */
function tierV2(effects: readonly DeclarativeEffect[], def?: CardDef): 'mult' | 'unit' | 'threshold' | 'amount' | 'none' {
  if (effects.some(hasMult)) return 'mult'
  if (effects.some((e) => UNIT_EFFECTS_V2.has(e.effect) && e.amount !== undefined)) return 'unit'
  if (effects.some(hasThreshold) || def?.freeIfMomentumAtLeast !== undefined) return 'threshold'
  if (effects.some((e) => AMOUNT_V2.has(e.effect) && e.amount !== undefined)) return 'amount'
  return 'none'
}

/** 効果列に本家形の強化を当てる (ティアは列ごとに判定 = 選択式は各モードが独立に上がる) */
function applyV2(effects: readonly DeclarativeEffect[], def: CardDef): readonly DeclarativeEffect[] {
  const tier = tierV2(effects, def)
  const boost50 = (e: DeclarativeEffect, min: number): DeclarativeEffect =>
    AMOUNT_V2.has(e.effect) && e.amount !== undefined && e.amount >= min
      ? { ...e, amount: Math.ceil(e.amount * 1.5), ...(e.amountMax !== undefined ? { amountMax: Math.ceil(e.amountMax * 1.5) } : {}) }
      : e
  if (tier === 'mult') {
    // ×1 の参照を含む札は先頭の参照だけ+1 (×1→×2 は+100%。幹の構え=上限×1ダメ+×1ブロックの両方を倍にすると
    // 1Eで200%になる。本家 Body Slam+ が ×2 でなくコスト0なのと同じく「倍にしない」側で揃える)
    const onlyFirst = effects.some((e) => MULT_EFFECTS.has(e.effect) && e.amount === 1)
    let done = false
    return effects.map((e) => {
      if (onlyFirst && done) return e
      let n = e
      if (MULT_EFFECTS.has(e.effect) && e.amount !== undefined) n = { ...n, amount: e.amount + 1 }
      if (e.growthMultiplier !== undefined) n = { ...n, growthMultiplier: e.growthMultiplier + 1 }
      if (e.momentumMultiplier !== undefined) n = { ...n, momentumMultiplier: e.momentumMultiplier + 1 }
      if (n !== e) done = true
      return n
    })
  }
  if (tier === 'unit') {
    let done = false
    return effects.map((e) => {
      if (!done && UNIT_EFFECTS_V2.has(e.effect) && e.amount !== undefined) {
        done = true
        return { ...e, amount: e.amount + (e.effect === 'addMomentum' ? 2 : 1) }
      }
      return boost50(e, 5)
    })
  }
  if (tier === 'threshold') {
    return effects.map((e) => {
      const c = e.condition
      const n =
        c !== undefined && (c.minGrowth !== undefined || c.minMomentum !== undefined)
          ? {
              ...e,
              condition: {
                ...c,
                ...(c.minGrowth !== undefined ? { minGrowth: Math.max(1, c.minGrowth - 1) } : {}),
                ...(c.minMomentum !== undefined ? { minMomentum: Math.max(1, c.minMomentum - 1) } : {}),
              },
            }
          : e
      return boost50(n, 1)
    })
  }
  if (tier === 'amount') return effects.map((e) => boost50(e, 1))
  return effects
}

export function upgradeTier(def: CardDef): UpgradeTier {
  const eff = allEffectsOf(def)
  if (isGreenRule(def)) {
    // 上限ランプはコスト-1が正史 (複利安全弁: gainEnergyMax の量は増えない)
    if (eff.some((e) => e.effect === 'gainEnergyMax') && def.cost >= 1 && !costCutViolates(def)) return 'cost'
    const t = tierV2(eff, def)
    if (t !== 'none') return t
    if (def.cost >= 1 && !costCutViolates(def)) return 'cost'
    if (BONUS_UPGRADES[def.id] !== undefined) return 'bonus'
    return 'none'
  }
  // 上限ランプはコスト-1が正史 (確定済みルール表「焚き火」)。2026-08-29 品質パスで
  // ランプ札に副次効果 (ブロック等) が付いたため、amount ティアに吸われて
  // 「0E化の当たり枠」が「副次+50%のハズレ枠」に化けるのを防ぐ
  if (eff.some((e) => e.effect === 'gainEnergyMax') && def.cost >= 1 && !costCutViolates(def)) {
    return 'cost'
  }
  // 成長エンジン置物 (2026-09-02 段6人間プレイ「年輪の大樹+はブロック3伸ばされましても」):
  // カードの魂=毎T成長は単位効果で量ティアに乗らず、おまけのブロックだけが+50%されていた。
  // コスト-1 (2E→1E) = 「軽くなって置きやすい」が成長置物の正しい伸び方
  if (def.id === 'green_perm_growth_tree' && def.cost >= 1 && !costCutViolates(def)) return 'cost'
  if (eff.some((e) => UPGRADABLE_EFFECTS.has(e.effect) && e.amount !== undefined)) return 'amount'
  // 上限参照札 (per-EnergyMax) のコスト-1強化は0Eまで落とさない (2026-08-30 裁定)。
  // 木陰の守り+ が 0E・非消滅・上限×2ブロック = 引くたびタダで盾、の退化ケースを塞ぐ。
  // 1E札は同軸おまけ (BONUS_UPGRADES) の受け皿へ
  // per-Xダメージ参照はコスト強化で1E以下に落とさない (2026-08-31 ユーザー許可。上限参照裁定の拡張)。
  // 氷の槍 (2E・氷壁×1) が焚き火のコスト強化で1E化し「消費しない参照×毎ターン補充」の
  // 連射砲 = 幕を勝つボタンになっていた実測への処方。2E以下のper-Xはコストに触れない
  const perXDmg = eff.some(
    (e) => e.effect.startsWith('dealDamagePer') && e.effect !== 'dealDamagePerEnergyMax', // 上限参照は既存裁定 (capRef) に委ねる
  )
  // 0E札は既存の「倍率/量+1」ティア (④') に委ねる — 有限参照なので安全と裁定済み
  if (perXDmg && def.cost >= 1 && def.cost <= 2) return BONUS_UPGRADES[def.id] !== undefined ? 'bonus' : 'none'
  const capRef = eff.some(
    (e) => e.effect === 'dealDamagePerEnergyMax' || e.effect === 'gainBlockPerEnergyMax',
  )
  if (capRef && def.cost === 1) return BONUS_UPGRADES[def.id] !== undefined ? 'bonus' : 'none'
  if (def.cost >= 1 && !costCutViolates(def)) return 'cost'
  if (eff.some((e) => UNIT_EFFECTS.has(e.effect) && e.amount !== undefined)) return 'unit'
  if (BONUS_UPGRADES[def.id] !== undefined) return 'bonus'
  return 'none'
}

/** すでに鍛えられているか (同じカードは1回だけ) */
export function isUpgraded(card: CardInstance): boolean {
  return card.def.name.endsWith('+')
}

/** この札は鍛えられるか (UI のボタン活性判定) */
export function canUpgradeCard(card: CardInstance): boolean {
  return !isUpgraded(card) && upgradeTier(card.def) !== 'none'
}

/**
 * カードを鍛える (確定済みルール表「焚き火」の3段仕様)。
 * ①量+50%切り上げ → ②コスト-1 → ③単位+1。名前に「+」が付く。
 * 自傷 (loseHp) などの対価は据え置き = 非対称強化を仕様として認める (StSのHemokinesis+と同じ)。
 * def を作り直すので engine 側に強化用の分岐は要らない (id は据え置き = 軸判定も不変)。
 */
export function upgradeCard(card: CardInstance): CardInstance {
  const tier = upgradeTier(card.def)
  if (isGreenRule(card.def) && tier !== 'cost' && tier !== 'bonus' && tier !== 'none') {
    const base = card.def
    let def: CardDef = {
      ...base,
      name: `${base.name}+`,
      effects: applyV2(base.effects, base),
      ...(base.modes !== undefined ? { modes: base.modes.map((m) => ({ ...m, effects: applyV2(m.effects, base) })) } : {}),
      ...(base.freeIfMomentumAtLeast !== undefined && tierV2(base.effects, base) === 'threshold'
        ? { freeIfMomentumAtLeast: Math.max(1, base.freeIfMomentumAtLeast - 1) }
        : {}),
    }
    return { ...card, def: legalizeUpgrade(def) }
  }
  const boostAmount = (e: DeclarativeEffect): DeclarativeEffect => {
    if (!UPGRADABLE_EFFECTS.has(e.effect) || e.amount === undefined) return e
    return {
      ...e,
      amount: Math.ceil(e.amount * 1.5),
      ...(e.amountMax !== undefined ? { amountMax: Math.ceil(e.amountMax * 1.5) } : {}),
    }
  }
  const boostUnit = (e: DeclarativeEffect): DeclarativeEffect => {
    if (!UNIT_EFFECTS.has(e.effect) || e.amount === undefined) return e
    return { ...e, amount: e.amount + 1 }
  }
  const mapEffects = (fn: (e: DeclarativeEffect) => DeclarativeEffect) => ({
    effects: card.def.effects.map(fn),
    ...(card.def.modes !== undefined
      ? { modes: card.def.modes.map((m) => ({ ...m, effects: m.effects.map(fn) })) }
      : {}),
  })
  const patch =
    tier === 'amount'
      ? mapEffects(boostAmount)
      : tier === 'cost'
        ? { cost: card.def.cost - 1 }
        : tier === 'unit'
          ? mapEffects(boostUnit)
          : tier === 'bonus'
            ? {
                // 同種効果は合算する (2026-08-31 青ラン指摘: 巻き波+ が「1ドロー、詠唱×2、1ドロー」と分裂表示)
                effects: (BONUS_UPGRADES[card.def.id] ?? []).reduce(
                  (acc: DeclarativeEffect[], b) => {
                    const i = acc.findIndex(
                      (e) =>
                        e.effect === b.effect &&
                        e.trigger === b.trigger &&
                        e.target === b.target &&
                        e.amount !== undefined &&
                        b.amount !== undefined,
                    )
                    if (i >= 0) {
                      acc[i] = { ...acc[i], amount: (acc[i].amount ?? 0) + (b.amount ?? 0) }
                      return acc
                    }
                    return [b, ...acc]
                  },
                  [...card.def.effects],
                ),
              }
            : {}
  let def: CardDef = { ...card.def, name: `${card.def.name}+`, ...patch }
  def = legalizeUpgradeModes(def, tier, boostUnit)
  return { ...card, def: legalizeUpgrade(def) }
}

/** 選択式 (modes) の旧3段仕様: 量ティアで量が伸びなかったモードには単位+1 (2026-09-03 人間ラン#3) */
function legalizeUpgradeModes(def: CardDef, tier: UpgradeTier, boostUnit: (e: DeclarativeEffect) => DeclarativeEffect): CardDef {
  if (tier === 'amount' && def.modes !== undefined) {
    return {
      ...def,
      modes: def.modes.map((m) =>
        m.effects.some((e) => UPGRADABLE_EFFECTS.has(e.effect) && e.amount !== undefined)
          ? m
          : { ...m, effects: m.effects.map(boostUnit) },
      ),
    }
  }
  return def
}

/** 正味エナジー増の規約を強化後の派生にも守らせる (違反したら消滅を自動付与) */
function legalizeUpgrade(def0: CardDef): CardDef {
  let def = def0
  // 正味エナジー増の規約 (確定済みルール表「正味エナジー増」) を強化後の派生にも守らせる
  // (2026-08-31 青Opusラン発見: 水鏡の書庫+ = 5ドロー+一時マナ2 = 正味0マナの補充札が
  // 非消滅で生成され「毎ターン実質タダで5ドロー」の壊れ性能だった)。
  // 合成 (fusion.ts) と同じ処方 = 違反したら消滅を自動付与して合法化する
  const REFILL_FOR_LEGALITY = [
    'drawCards',
    'drawCardsPerCardPlayed',
    'dischargeAetherDraw',
    'impulseDraw',
    'retrieveFromExhaust',
    'playFromExhaust',
  ]
  const allEffects = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
  const netGain = allEffects
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  const refills = allEffects.some((e) => REFILL_FOR_LEGALITY.includes(e.effect))
  if (netGain - def.cost >= 0 && refills && def.exhaust !== true) {
    def = { ...def, exhaust: true }
  }
  return def
}

/**
 * 手札で鍛える (研ぎ澄まし 2026-09-02 ユーザー裁定): レアと工房産 (fused_/fusion_) は対象外。
 * Opusラン A で「森の導き(サーチ)+研ぎ澄まし(一時強化)+工房の一点物」が1枚コンボに収束したため、一点物への一時強化を切る
 */
export function canUpgradeInHand(card: CardInstance): boolean {
  if (card.def.rarity === 'rare') return false
  if (card.def.id.startsWith('fused_') || card.def.id.startsWith('fusion_')) return false
  return canUpgradeCard(card)
}
