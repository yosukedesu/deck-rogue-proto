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

/** この札をプレイして正味で増えるエナジー (gainEnergy の合計 − コスト)。モードは最大値を採る */
function netEnergy(def: CardDef): number {
  const base = def.effects
    .filter((e) => e.effect === 'gainEnergy')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  const modeMax = (def.modes ?? []).reduce(
    (max, m) =>
      Math.max(
        max,
        m.effects.filter((e) => e.effect === 'gainEnergy').reduce((a, e) => a + (e.amount ?? 0), 0),
      ),
    0,
  )
  return base + modeMax - def.cost
}

/**
 * 手札またはエナジーを補充する効果。0マナ札がこれを持つと
 * 「撃つ → 補充 → また撃つ」が閉じてリシャッフル込みの無限ループになる。
 * 逆にこれらを持たない0マナ札は撃つたび手札が1枚減るので必ず停止する。
 */
const REFILL_EFFECTS = [
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
  'gainEnergy',
  'gainEnergyMax',
]

describe('カードデータの不変条件', () => {
  it('コスト0で手札かエナジーを補充する札は必ず消滅する (2026-08-26改定。無限詠唱ループの禁止)', () => {
    // 旧ルールは「0マナは一律消滅必須」。赤に速さの対価を渡すため、
    // ループが実際に閉じる条件 (補充を伴うこと) だけに絞った (確定済みルール表「0マナスペル」)。
    const bad = allCards.filter(
      (c) =>
        c.cost === 0 &&
        c.exhaust !== true &&
        allEffects(c).some((e) => REFILL_EFFECTS.includes(e.effect)),
    )
    expect(bad.map((c) => c.name)).toEqual([])
  })

  it('正味エナジーが増える札は必ず消滅する (2026-08-26制定。無限マナループの禁止)', () => {
    // 抜け道の実例: 魔力変換 1E→一時マナ+2 は正味+1。集中(次のカード-1)と
    // 連鎖する思考(詠唱数ぶんドロー)を挟むとエナジーもドローも青天井になる
    const bad = allCards.filter((c) => netEnergy(c) > 0 && c.exhaust !== true)
    expect(bad.map((c) => `${c.name}(${c.cost}E→+${netEnergy(c) + c.cost})`)).toEqual([])
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
})
