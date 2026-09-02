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
  green_canopy_shade: [{ trigger: 'onPlay', effect: 'gainBlock', amount: 4 }],
  // ---- per-Xダメージ参照のコスト強化封じ (2026-08-31) の受け皿: 同軸のおまけを足す ----
  green_surge_thrust: [{ trigger: 'onPlay', effect: 'addMomentum', amount: 3 }], // 換金前に勢い+3
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
  green_trunk_guard: [{ trigger: 'onPlay', effect: 'gainBlock', amount: 4 }],
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
export function upgradeTier(def: CardDef): 'amount' | 'cost' | 'unit' | 'bonus' | 'none' {
  const eff = allEffectsOf(def)
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
  // 選択式 (modes) は各モードを独立に鍛える (2026-09-03 人間ラン#3「道行きの選択+ が片方しか強化されない」)。
  // 量ティアで量が伸びなかったモード (成長+2 だけ等) には単位+1 を当てる = 両モードが必ず1段上がる
  if (tier === 'amount' && def.modes !== undefined) {
    def = {
      ...def,
      modes: def.modes.map((m) =>
        m.effects.some((e) => UPGRADABLE_EFFECTS.has(e.effect) && e.amount !== undefined)
          ? m
          : { ...m, effects: m.effects.map(boostUnit) },
      ),
    }
  }
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
  return { ...card, def }
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
