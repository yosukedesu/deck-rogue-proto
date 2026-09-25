// カードデータの不変条件テスト。確定済みルール表の「0マナスペル」「正味エナジー増」を
// 全カードに対して機械的に固定する。新カード追加時にルール違反を自動で検出するための網。
import { describe, expect, it } from 'vitest'
import { allCards } from './content.ts'
import type { CardDef } from './types.ts'

/**
 * 札が持ちうる効果すべて (通常効果 + 選択式カードの全モード)。
 * 2026-08-26追加: 従来は def.effects しか見ておらず、modes の中身が全不変条件の死角だった
 * (陽光の恵みが確定ルール「上限ランプの消滅」に違反したまま検出されていなかった)。
 */
function allEffects(def: CardDef) {
  return [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
}

/**
 * 手札を補充する効果。これを持たない札は撃つたび手札が1枚減るので、
 * どれだけ安くても必ず停止する (循環が閉じない)。
 */
const REFILL_EFFECTS = [
  'addCardToHand', // トークン生成も手札の補充 (骨刃 2026-09-01)
  'drawCardsPerLight', // 灯の手帳 (白 2026-09-20 夜)
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
  'retrieveZeroCostFromDiscard', // 引き潮の帰還 (青 2026-09-25): 0E化すると2枚で互いを戻し合う
  'drawTypeFromDeck', // 仕掛け師の工房 (青 2026-09-25)
]

/**
 * この札の「正味の値段」。gainEnergy と discountNext はどちらも実質エナジーなので同じ通貨で数える。
 * 2026-08-26追加: discountNext を数えていなかったため、集中 (1E・1ドロー・次のカード-1) が
 * 「割引で自分が実質0マナ → 引き直して戻ってくる」完全な循環になり無限ループしていた
 * (deck_storm vs 用心深い影 seed7 でターン3が終わらない。実測)。
 */
function netEnergy(def: CardDef): number {
  const sum = (list: readonly { effect: string; amount?: number }[]) =>
    list
      .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
      .reduce((a, e) => a + (e.amount ?? 0), 0)
  const base = sum(def.effects)
  const modeMax = (def.modes ?? []).reduce((max, m) => Math.max(max, sum(m.effects)), 0)
  return base + modeMax - def.cost
}

/** gainEnergy だけの正味 (「タダマナ札」の判定。割引は次の1枚にしか効かないので別勘定) */
function netRawEnergy(def: CardDef): number {
  const sum = (list: readonly { effect: string; amount?: number }[]) =>
    list.filter((e) => e.effect === 'gainEnergy').reduce((a, e) => a + (e.amount ?? 0), 0)
  const modeMax = (def.modes ?? []).reduce((max, m) => Math.max(max, sum(m.effects)), 0)
  return sum(def.effects) + modeMax - def.cost
}

describe('カードデータの不変条件', () => {
  it('タダで撃てて手札も補充する札は必ず消滅する (2026-08-26改定。無限詠唱ループの禁止)', () => {
    // 「正味の値段が0以下」かつ「手札を補充する」= 撃っても資源も手札も減らない = 循環が閉じる。
    // 旧ルールは cost===0 しか見ておらず、割引で実質0マナになる集中を取り逃していた。
    // 逆に補充を伴わない0マナ札 (火花) は撃つたび手札が1枚減るので必ず停止する。
    // 2026-09-18 品質パス第3弾 (落ち葉の刃 0E・捨て1・6ダメ+1ドロー): 追加コストで手札を捨てる/消滅させる札は、
    // 補充が捨てた枚数以下なら撃つたび手札が減る (プレイ-1・捨て-1・ドロー+1 = -1) = 循環が閉じない
    const handCost = (c: CardDef) => (c.discardCost ?? 0) + (c.exhaustCost ?? 0)
    const refill = (c: CardDef) => allEffects(c).filter((e) => REFILL_EFFECTS.includes(e.effect)).reduce((a, e) => a + (e.amount ?? 1), 0)
    // 2026-09-23 灯の頁 (0E・灯2・2ドロー): 灯コストを払う札は例外 (ユーザー「灯の頁消滅つけないでいいよ」)。灯は戦闘内で有限で、
    // 0E で灯を産む札は消滅か X (エナジーを払う) を持つ = 下のテストで機械固定。よって灯を払って引く循環は必ず止まる
    const bad = allCards.filter(
      (c) =>
        netEnergy(c) >= 0 &&
        c.exhaust !== true &&
        c.xCost !== true && // X 札は必ずエナジーを1以上払う (灯り注ぎ 2026-09-24: X→灯2X＋Xドロー。撃つたびエナジーが空になる=循環は閉じない)
        (c.lightCost ?? 0) === 0 &&
        allEffects(c).some((e) => REFILL_EFFECTS.includes(e.effect)) &&
        !(handCost(c) > 0 && refill(c) <= handCost(c)),
    )
    expect(bad.map((c) => `${c.name}(正味${netEnergy(c)})`)).toEqual([])
  })

  it('0E で灯を産む札は消滅か X を持つ (灯コスト札の補充を例外にした根拠 2026-09-23)', () => {
    const LIGHT_MAKERS = ['addLight', 'addLightNextTurn', 'doubleLight', 'lightCarryHalf']
    const bad = allCards.filter(
      (c) => c.cost === 0 && c.type !== 'permanent' && c.exhaust !== true && c.xCost !== true && allEffects(c).some((e) => LIGHT_MAKERS.includes(e.effect)),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('4枚以上の衝動ドローは必ず消滅する (2026-08-30 ユーザー指摘)', () => {
    // 衝動は「このターン限り」なので一見自己完結だが、**山札を大量に掘る**ので
    // 毎シャッフル撃てると常に全札が見える = 一貫性が跳ね上がり、デッキ構築の悩みが消える。
    // 一度きりの爆発にすることで「いつ撃つか」の決断に変える (消滅の設計思想と同じ)。
    // 置物は場に残り続けるので消滅を付けられない = 毎ターンの供給量で抑える (鍛冶の炉は2枚)
    const bad = allCards.filter(
      (c) =>
        c.type !== 'permanent' &&
        c.exhaust !== true &&
        allEffects(c).some((e) => e.effect === 'impulseDraw' && (e.amount ?? 0) >= 4),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('正味エナジーが増える札は必ず消滅する (2026-08-26制定。無限マナループの禁止)', () => {
    // 抜け道の実例: 魔力変換 1E→一時マナ+2 は正味+1。集中(次のカード-1)と
    // 連鎖する思考(詠唱数ぶんドロー)を挟むとエナジーもドローも青天井になる
    // 灯コストの例外 (2026-09-24 灯の薪 0E・灯3→+1E・消滅なし): 灯り注ぎが 1E→灯2 で作るので、灯2につき1Eを超えない (lightCost > 2×gainEnergy)
    // 札なら「注ぎ→薪→注ぎ…」の循環でエナジーが毎周減る = 必ず止まる。灯2で+1E だと収支ゼロで無限になる
    const bad = allCards.filter((c) => netRawEnergy(c) > 0 && c.exhaust !== true && !((c.lightCost ?? 0) > 2 * (netRawEnergy(c) + c.cost)))
    expect(bad.map((c) => `${c.name}(${c.cost}E→+${netRawEnergy(c) + c.cost})`)).toEqual([])
  })

  it('リアクションタイプは onPlay 効果を持たない (伏せ専用の担保)', () => {
    const bad = allCards.filter(
      (c) => c.type === 'reaction' && c.effects.some((e) => e.trigger === 'onPlay'),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('しきい値カード (忘却の刻) は amountMax を必ず持つ', () => {
    const bad = allCards.filter((c) =>
      allEffects(c).some((e) => e.exhaustThreshold !== undefined && e.amountMax === undefined),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('召喚カードの summonId は実在する置物を指す', () => {
    const ids = new Set(allCards.map((c) => c.id))
    const bad = allCards.filter((c) =>
      allEffects(c).some(
        (e) =>
          e.effect === 'summonPermanent' &&
          (e.summonId === undefined || !ids.has(e.summonId)),
      ),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('エナジー上限を上げる札は消滅する (使い回しランプの禁止)', () => {
    // 例外: 選択式カードでモードの片方だけがランプする札 (陽光の恵み) は対象外。
    // ランプは2択の一方でしかなく、毎ターン確実に上限を上げ続けることはできないため
    // (2026-08-26 ユーザー裁定。確定済みルール表「上限ランプの消滅」)。
    const bad = allCards.filter(
      (c) => c.effects.some((e) => e.effect === 'gainEnergyMax') && c.exhaust !== true,
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('リアクションのコストは2E以下 (3エナジー制で3E伏せは温存不能。2026-08-29 裁定)', () => {
    // 計測ランで根の紡ぎ (旧3E) が幕1を通して一度も発動できなかった=温存コストが構造的に
    // 払えない、を受けた裁定。例外: 魔力盗み (青・凍結中) は解凍時に是正する
    const FROZEN_EXCEPTIONS = new Set(['blue_spell_steal'])
    const bad = allCards.filter(
      (c) => c.type === 'reaction' && c.cost > 2 && !FROZEN_EXCEPTIONS.has(c.id),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('成長の倍化 (doubleGrowth) を持つ札は消滅する (倍加は1回きりの決断)。勢いの倍化は対象外', () => {
    // 2026-08-25 裁定「倍加の使い回しが成長97%の主犯」を機械判定に昇格 (2026-08-29 倍化増刷+4と同時)。
    // それまで設計裁定だけで機械固定されていなかった穴。
    // 2026-09-18 品質パス第3弾: 勢いは自ターン終了で消えるので、倍化を使い回しても雪だるまにならない
    // (成長は永続なので雪だるまになる) = doubleMomentum は規約から外す (昂ぶる角笛 R が消滅なしに)
    // 2026-09-20 白の再設計: 灯 (doubleLight) も戦闘内持続なので成長と同じ規約 (灯の倍化 R は消滅)
    const bad = allCards.filter((c) => c.effects.some((e) => e.effect === 'doubleGrowth' || e.effect === 'doubleLight') && c.exhaust !== true)
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('灯コスト (lightCost) を持つ札は白だけ (灯は白の蓄積。2026-09-20 白の再設計)', () => {
    const bad = allCards.filter((c) => (c.lightCost ?? 0) > 0 && c.color !== 'white')
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('人形 (retainer) は灯り (life 1以上) か期限なし (lifePersist) を持つ (2026-09-21 人形の灯り)。人形でない札は持たない', () => {
    const noLife = allCards.filter((c) => c.retainer === true && !(c.lifePersist === true || (c.life ?? 0) >= 1))
    expect(noLife.map((c) => c.name)).toEqual([])
    const stray = allCards.filter((c) => c.retainer !== true && (c.life !== undefined || c.lifePersist !== undefined))
    expect(stray.map((c) => c.name)).toEqual([])
    // 段: 小さな人形3・1E の人形4・灯コストつき5・篝火は期限なし (提案書 §3-1 → 2026-09-21 Opus A/B「増える前に消える」で+1)
    const life = (id: string) => allCards.find((c) => c.id === id)?.life
    expect(life('white_perm_page')).toBe(3)
    expect(life('white_perm_squire')).toBe(4)
    expect(life('white_perm_choir')).toBe(5)
    expect(allCards.find((c) => c.id === 'white_perm_bonfire')?.lifePersist).toBe(true)
  })
})

describe('基本札の上位互換サイクル (2026-08-27。確定済みルール表「報酬プールの下限」)', () => {
  // 基本札 (打撃・防御) は報酬プールから除外されている = 「抜かれるためにある」。
  // その思想が成立するには、各色の報酬プールに基本札の完全上位互換が最低1枚ずつ要る
  // (ユーザー指摘: 「完全上位互換を入れないと矛盾している」)。
  // ここでは各色の後継カードを名指しで固定する。消えたり弱体化したらここで落ちる。
  const CYCLE: Record<string, { attack: string; guard: string }> = {
    green: { attack: 'green_tailwind', guard: 'green_entangle' }, // 追い風=6貫通(勢い5以上で0E) / モード:ブロック7 (荒角の一撃は2026-09-05 撤去)
    blue: { attack: 'blue_rapid_strike', guard: 'blue_thick_ice' }, // 6+1ドロー / 氷壁7
    red: { attack: 'red_ember_slash', guard: 'red_hearth_shield' }, // 6+衝動1 / 4+衝動1+延焼1
    white: { attack: 'white_shield_strike', guard: 'white_light_hoard' }, // 5+ブロック3 / ブロック5+灯2 (修繕の灯は 2026-09-24 に撤去)
    black: { attack: 'black_grave_bolt', guard: 'black_gravestone' }, // ミル1+6/12 / 5+燃料2
  }

  it('全色に、基本攻撃・基本防御それぞれの上位互換が1Eで存在する (消滅なし)', () => {
    for (const [color, pair] of Object.entries(CYCLE)) {
      for (const id of [pair.attack, pair.guard]) {
        const def = allCards.find((c) => c.id === id)
        expect(def, `${color}: ${id} が存在しない`).toBeDefined()
        expect(def!.cost, `${id} は1Eであること`).toBe(1)
        expect(def!.exhaust, `${id} は消滅しないこと (基本札の後継=常用札)`).not.toBe(true)
      }
    }
  })

  it('上位互換は基本札の主効果量を下回らない', () => {
    const amountOf = (id: string, effect: string): number => {
      const def = allCards.find((c) => c.id === id)!
      const all = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
      return Math.max(0, ...all.filter((e) => e.effect === effect).map((e) => e.amount ?? 0))
    }
    // 攻撃: 基本札のダメージ量以上
    expect(amountOf('green_tailwind', 'dealDamage')).toBeGreaterThanOrEqual(6)
    expect(amountOf('blue_rapid_strike', 'dealDamage')).toBeGreaterThanOrEqual(5)
    expect(amountOf('red_ember_slash', 'dealDamage')).toBeGreaterThanOrEqual(6)
    expect(amountOf('white_shield_strike', 'dealDamage')).toBeGreaterThanOrEqual(5)
    expect(amountOf('black_grave_bolt', 'dealDamage')).toBeGreaterThanOrEqual(6)
    // 防御: 基本札のブロック量以上 (青は氷壁が基本)
    expect(amountOf('green_entangle', 'gainBlock')).toBeGreaterThanOrEqual(5)
    expect(amountOf('blue_thick_ice', 'gainIceBlock')).toBeGreaterThanOrEqual(5)
    expect(amountOf('red_hearth_shield', 'gainBlock')).toBeGreaterThanOrEqual(4)
    expect(amountOf('white_light_hoard', 'gainBlock')).toBeGreaterThanOrEqual(5)
    expect(amountOf('black_gravestone', 'gainBlock')).toBeGreaterThanOrEqual(5)
  })

  // レアリティ (2026-08-29 本家踏襲導入)。割当済みの色は全札に明示があること。
  // 2026-08-30 青・黒も割当済み = 全5色を機械判定
  it('割当済みの色 (緑・白・赤) は全札に rarity があり、レアは希少なまま', () => {
    for (const color of ['green', 'white', 'red', 'blue', 'black'] as const) {
      const pool = allCards.filter((c) => c.color === color)
      const missing = pool.filter((c) => c.rarity === undefined).map((c) => c.id)
      expect(missing, `${color} に rarity 未割当`).toEqual([])
      const rare = pool.filter((c) => c.rarity === 'rare').length
      // レア = デッキの方針を一枚で定義する札。全体の1〜2割に収める
      expect(rare / pool.length, `${color} のレア比率`).toBeGreaterThanOrEqual(0.1)
      expect(rare / pool.length, `${color} のレア比率`).toBeLessThanOrEqual(0.25) // 2026-09-05 緑レア+3 (各軸の方針定義札。本家StS2は31%)
    }
  })

  // 亡骸プレイ (necroCost 2026-08-31): playNecro は playCard の簡約版なので、
  // 複雑な札 (選択式・X・追加コスト・コスト再利用・リアクション・置物) には付けられない
  it('necroCost 持ちは単純な札のみ (modes/xCost/追加コスト/再利用/リアクション/置物は不可)', () => {
    for (const c of allCards.filter((c) => c.necroCost !== undefined)) {
      expect(c.modes ?? [], `${c.name}: modes不可`).toEqual([])
      expect(c.xCost ?? false, `${c.name}: xCost不可`).toBe(false)
      expect(c.discardCost ?? 0, `${c.name}: 捨てコスト不可`).toBe(0)
      expect(c.exhaustCost ?? 0, `${c.name}: 消滅コスト不可`).toBe(0)
      expect(['spell', 'physical'], `${c.name}: 呪文か物理のみ`).toContain(c.type)
      const forbidden = c.effects.some(
        (e) => e.effect === 'retrieveFromExhaust' || e.effect === 'playFromExhaust',
      )
      expect(forbidden, `${c.name}: コスト再利用効果は不可`).toBe(false)
      // 消滅置き場に自然に届くこと = 消滅持ちであること (ミル頼みの死に札を防ぐ)
      expect(c.exhaust, `${c.name}: 亡骸プレイ持ちは消滅を持つこと`).toBe(true)
    }
  })

  // 亡骸効果 (onSelfExhausted): 発火は自動なので、対象を要求する効果は置けない…ではなく
  // 単体対象は「生存先頭に自動解決」される。ここでは規約として「亡骸効果は量を持つこと」を固定
  it('亡骸効果 (onSelfExhausted) は量を持つ宣言的効果のみ (negate等の量なしは不可)', () => {
    for (const c of allCards) {
      for (const e of c.effects.filter((e) => e.trigger === 'onSelfExhausted')) {
        expect(e.amount !== undefined, `${c.name}: 亡骸効果は amount 必須`).toBe(true)
      }
    }
  })
})
