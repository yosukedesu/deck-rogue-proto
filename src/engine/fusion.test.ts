// カード合成 (工房) のテスト。確定済みルール表「カード合成（工房）」「工房ノード」を固定する。
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { fuseBlockReason, fuseCards } from './fusion.ts'
import { applyRunCommand, createRun, upgradeCard, upgradeTier } from './run.ts'
import type { RunState } from './run.ts'
import { defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, uid = `t_${id}`): CardInstance => ({ uid, def: getCardDef(id) })

function forceWin(run: RunState): RunState {
  const c = run.combat!
  let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
  surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
  surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
  return applyRunCommand(
    { ...run, combat: surgical },
    { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
  )
}
function advance(run: RunState): RunState {
  let r = run
  if (r.phase === 'campfire') r = applyRunCommand(r, { type: 'CampfireRest' })
  if (r.phase === 'relic-reward') r = applyRunCommand(r, { type: 'SkipRelic' })
  if (r.phase === 'reward') r = applyRunCommand(r, { type: 'SkipReward' })
  if (r.phase === 'offer') r = applyRunCommand(r, { type: 'ChooseElite', elite: false })
  return r
}

describe('計算合成', () => {
  it('同種効果は量が合算され、コストはVPから逆算される (打撃系2枚 → 1枚)', () => {
    const def = fuseCards(inst('green_fang'), inst('green_serpent_gulp')) // 14貫通 + 20(捨て1)
    expect(def.effects.some((e) => e.effect === 'dealDamage')).toBe(true)
    expect(def.cost).toBeGreaterThanOrEqual(1)
    expect(def.cost).toBeLessThanOrEqual(3)
    expect(def.discardCost).toBe(1) // 追加コストは引き継ぐ
    expect(def.color).toBe('green')
  })

  it('決定的: 同じ素材からは常に同じ結果 (順序も問わない)', () => {
    const ab = fuseCards(inst('green_fang'), inst('green_bark_armor'))
    const ab2 = fuseCards(inst('green_fang'), inst('green_bark_armor'))
    expect(JSON.stringify(ab)).toBe(JSON.stringify(ab2))
  })

  it('レシピが最優先される (年輪×二連の蔦打ち → 蔦車輪)', () => {
    const def = fuseCards(inst('green_growth_ring'), inst('green_double_lash'))
    expect(def.id).toBe('fusion_vine_wheel')
    const rev = fuseCards(inst('green_double_lash'), inst('green_growth_ring'))
    expect(rev.id).toBe('fusion_vine_wheel') // 順序を問わない
  })

  it('同名2枚・タイプ違い・リアクションは計算合成できない (レシピは例外)', () => {
    expect(fuseBlockReason(inst('green_strike', 'u1'), inst('green_strike', 'u2'))).not.toBeNull()
    expect(fuseBlockReason(inst('green_strike'), inst('green_flash_insight'))).not.toBeNull() // 物理×呪文
    expect(
      fuseBlockReason(inst('green_reaction_thorns'), inst('green_reaction_cornered')),
    ).not.toBeNull()
    // レシピの守りの蔓×茨の返し (リアクション同士) は許可される
    expect(fuseBlockReason(inst('green_reaction_vine'), inst('green_reaction_thorns'))).toBeNull()
  })

  it('スモーク: 緑×緑の全組み合わせで不変条件が守られる', () => {
    const greens = allCards.filter((c) => c.color === 'green')
    const REFILL = new Set([
      'drawCards', 'drawCardsPerCardPlayed', 'dischargeAetherDraw', 'impulseDraw',
      'retrieveFromExhaust', 'playFromExhaust',
    ])
    let fusable = 0
    for (let i = 0; i < greens.length; i++) {
      for (let j = i + 1; j < greens.length; j++) {
        const a = { uid: `a${i}`, def: greens[i] }
        const b = { uid: `b${j}`, def: greens[j] }
        if (fuseBlockReason(a, b) !== null) continue
        fusable++
        const def = fuseCards(a, b)
        // コストは1〜3E (costCap対策。レシピは手書き裁定なので4Eまで許容)
        const isRecipe = def.id.startsWith('fusion_')
        if (!isRecipe) {
          expect(def.cost, def.id).toBeGreaterThanOrEqual(1)
          expect(def.cost, def.id).toBeLessThanOrEqual(3)
          expect(def.effects.length, def.id).toBeLessThanOrEqual(3) // 派手枠は3効果まで
        }
        // 無限ループ規約: 正味の値段が0以上 + 補充 → 消滅必須
        const net = def.effects
          .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
          .reduce((acc, e) => acc + (e.amount ?? 0), 0)
        if (net - def.cost >= 0 && def.effects.some((e) => REFILL.has(e.effect))) {
          expect(def.exhaust, `${def.id} はループ規約により消滅必須`).toBe(true)
        }
        // gainEnergyMax を持つなら消滅必須
        if (def.effects.some((e) => e.effect === 'gainEnergyMax')) {
          expect(def.exhaust, def.id).toBe(true)
        }
      }
    }
    expect(fusable).toBeGreaterThan(100) // 合成可能な組が十分にある
  })
})

describe('工房ノード (5・10戦目クリア後)', () => {
  it('5戦目クリア後に工房が入り、合成すると素材2枚が消えて1枚増える', () => {
    let run = createRun(31, 'set-confirm')
    for (let i = 0; i < 4; i++) run = advance(forceWin(run))
    run = forceWin(run) // 5戦目 (battleIndex 4) をクリア
    expect(run.phase).toBe('workshop')
    const before = run.deck.length
    // 合成可能なペアを探す
    let pair: [number, number] | null = null
    outer: for (let i = 0; i < run.deck.length; i++) {
      for (let j = i + 1; j < run.deck.length; j++) {
        if (fuseBlockReason(run.deck[i], run.deck[j]) === null) { pair = [i, j]; break outer }
      }
    }
    expect(pair).not.toBeNull()
    run = applyRunCommand(run, { type: 'WorkshopFuse', indexA: pair![0], indexB: pair![1] })
    expect(run.deck).toHaveLength(before - 1) // 2枚消えて1枚入る
    expect(run.phase).toBe('reward')
  })

  it('見送りもできる', () => {
    let run = createRun(31, 'set-confirm')
    for (let i = 0; i < 4; i++) run = advance(forceWin(run))
    run = forceWin(run)
    expect(run.phase).toBe('workshop')
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'WorkshopSkip' })
    expect(run.deck).toHaveLength(before)
    expect(run.phase).toBe('reward')
  })
})

describe('強化の3段仕様 (2026-08-27 仕様会議)', () => {
  it('①量+50%: 打撃 6→9', () => {
    const up = upgradeCard(inst('green_strike'))
    expect(up.def.effects[0].amount).toBe(9)
    expect(up.def.name).toBe('打撃+')
  })

  it('②コスト-1: 年輪 (成長+2) は 1E→0E になり、量は据え置き', () => {
    expect(upgradeTier(getCardDef('green_growth_ring'))).toBe('cost')
    const up = upgradeCard(inst('green_growth_ring'))
    expect(up.def.cost).toBe(0)
    expect(up.def.effects[0].amount).toBe(2)
  })

  it('③単位+1: 緑の閃き (0E化すると補充規約違反) はドロー4→5', () => {
    expect(upgradeTier(getCardDef('green_flash_insight'))).toBe('unit')
    const up = upgradeCard(inst('green_flash_insight'))
    expect(up.def.cost).toBe(1) // コストは変わらない
    expect(up.def.effects.find((e) => e.effect === 'drawCards')!.amount).toBe(5)
  })

  it('④上限ランプ (芽吹き) は強化不可', () => {
    expect(upgradeTier(getCardDef('green_ramp_sprout'))).toBe('none')
  })

  it('自傷の非対称強化: 対価は据え置きで出力だけ+50% (StSのHemokinesis+と同じ裁定)', () => {
    const up = upgradeCard(inst('black_pain')) // 1E・HP-3・16ダメ
    expect(up.def.effects.find((e) => e.effect === 'dealDamage')!.amount).toBe(24)
    expect(up.def.effects.find((e) => e.effect === 'loseHp')!.amount).toBe(3) // 据え置き
  })
})
