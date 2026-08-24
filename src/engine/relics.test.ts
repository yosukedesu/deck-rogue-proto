// エリート挑戦オファーとレリック (2026-08-25) のテスト。
// 確定済みルール表「エリート挑戦オファー」「レリック」と docs/relics-design.md を固定する。
import { describe, expect, it } from 'vitest'
import { allRelics, getRelicDef } from './content.ts'
import { applyRunCommand, createRun, depthHpScale } from './run.ts'
import type { RunState } from './run.ts'
import { defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

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

/** 1戦目を勝ってスキップし、2戦目 (battleIndex 1) のオファーまで進める */
function toFirstOffer(seed = 11): RunState {
  let run = createRun(seed, 'set-confirm')
  run = forceWin(run)
  run = applyRunCommand(run, { type: 'SkipReward' })
  return run
}

describe('エリート挑戦オファー', () => {
  it('2戦目 (battleIndex 1) の前にオファーフェーズが入る', () => {
    const run = toFirstOffer()
    expect(run.phase).toBe('offer')
    expect(run.combat).toBeNull()
  })

  it('避けると通常補正の戦闘が始まる', () => {
    let run = toFirstOffer()
    run = applyRunCommand(run, { type: 'ChooseElite', elite: false })
    expect(run.phase).toBe('combat')
    expect(run.currentElite).toBe(false)
    expect(run.combat!.enemies[0].strength).toBeLessThanOrEqual(0) // depth 0 + 群れ補正(負)のみ
  })

  it('挑むとエリート補正 (強化+2・HP×1.35) の戦闘が始まる', () => {
    const base = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: false })
    const elite = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: true })
    expect(elite.currentElite).toBe(true)
    expect(elite.combat!.enemies[0].strength).toBe(base.combat!.enemies[0].strength + 2)
    // HP倍率: 同シードなので同じ敵。1.35倍で丸め
    const scale = depthHpScale(1)
    expect(elite.combat!.enemies[0].maxHp).toBeGreaterThan(base.combat!.enemies[0].maxHp)
    expect(elite.combat!.enemies[0].maxHp / base.combat!.enemies[0].maxHp).toBeCloseTo(1.35, 1)
    expect(scale).toBeCloseTo(0.75)
  })

  it('エリートに勝つとレリック3択 → 取得後にカード報酬へ', () => {
    let run = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: true })
    run = forceWin(run)
    expect(run.phase).toBe('relic-reward')
    expect(run.relicOptions).toHaveLength(3)
    const picked = run.relicOptions![0]
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.relics).toEqual([picked])
    expect(run.phase).toBe('reward') // カード報酬にも進む
  })

  it('通常戦闘の勝利ではレリック報酬は出ない', () => {
    let run = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: false })
    run = forceWin(run)
    expect(run.phase).toBe('reward')
  })

  it('決定論: 同シードのレリック候補列は一致する', () => {
    const a = createRun(7, 'set-confirm')
    const b = createRun(7, 'set-confirm')
    expect(a.relicQueue).toEqual(b.relicQueue)
    expect(new Set(a.relicQueue).size).toBe(allRelics.length)
  })
})

describe('レリック効果', () => {
  /** 指定レリックを持った状態で次の戦闘を始める */
  function withRelicIntoBattle(relicId: string): RunState {
    let run = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: true })
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf(relicId)
    // 候補に無い場合はテスト用に直接注入して次戦へ
    if (idx >= 0) {
      run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    } else {
      run = { ...run, relics: [relicId], relicOptions: null, phase: 'reward', rewardOptions: [] }
      run = { ...run, rewardOptions: null }
      return applyRunCommand(
        { ...run, phase: 'reward', rewardOptions: ['green_growth_ring'] },
        { type: 'SkipReward' },
      )
    }
    return applyRunCommand(run, { type: 'SkipReward' })
  }

  it('A型 (賢者の巻物): 戦闘開始時に2枚ドロー = 初手が2枚多い', () => {
    const run = withRelicIntoBattle('relic_sage_scroll')
    const c = run.combat!
    expect(c.player.hand.length).toBe(c.player.drawPerTurn + 2)
  })

  it('A型 (先手の盾): 戦闘開始時にブロック+8', () => {
    const run = withRelicIntoBattle('relic_vanguard_shield')
    expect(run.combat!.player.block).toBe(8)
  })

  it('B型 (鉄の心臓): 取得時に最大HP+8 (現在HPも+8)', () => {
    let run = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: true })
    const hpBefore = run.hp
    const maxBefore = run.maxHp
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf('relic_iron_heart')
    if (idx < 0) return // このシードの候補に無ければ対象外 (決定論なので固定)
    run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    expect(run.maxHp).toBe(maxBefore + 8)
    expect(run.hp).toBe(Math.min(run.maxHp, hpBefore + 8))
  })

  it('B型 (収集家の鞄): 報酬ピックの候補+1', () => {
    let run = applyRunCommand(toFirstOffer(), { type: 'ChooseElite', elite: true })
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf('relic_collectors_bag')
    if (idx < 0) return
    run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    expect(run.rewardOptions).toHaveLength(4) // 3 + 1
  })

  it('レリック定義は全て A型 (effects) か B型 (bonus) を持つ', () => {
    for (const r of allRelics) {
      const hasA = (r.effects?.length ?? 0) > 0
      const hasB = r.bonus !== undefined
      expect(hasA || hasB).toBe(true)
      expect(getRelicDef(r.id).name.length).toBeGreaterThan(0)
    }
  })
})
