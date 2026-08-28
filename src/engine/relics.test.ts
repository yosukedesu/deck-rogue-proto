// エリートノードとレリック (2026-08-25。2026-08-28 マップ化) のテスト。
// 確定済みルール表「エリート挑戦オファー」「レリック」と docs/relics-design.md を固定する。
import { describe, expect, it } from 'vitest'
import { allRelics, getEventDef, getRelicDef, getEnemyDef, resolveEncounter } from './content.ts'
import { applyRunCommand, createRun, currentNode, depthHpScale } from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
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

/** 最初のエリートノードに入るまで進める (途中の戦闘は forceWin、非戦闘はスキップ) */
function intoFirstElite(seed = 11): RunState {
  let run = createRun(seed, 'set-confirm')
  let guard = 0
  while (guard++ < 80) {
    if (run.phase === 'map') {
      run = chooseToward(run, 'elite')
      if (run.phase === 'combat' && run.currentElite) return run
    } else if (run.phase === 'combat') {
      run = forceWin(run)
    } else if (run.phase === 'campfire') {
      run = applyRunCommand(run, { type: 'CampfireRest' })
    } else if (run.phase === 'workshop') {
      run = applyRunCommand(run, { type: 'WorkshopSkip' })
    } else if (run.phase === 'relic-reward') {
      run = applyRunCommand(run, { type: 'SkipRelic' })
    } else if (run.phase === 'reward') {
      run = applyRunCommand(run, { type: 'SkipReward' })
    } else if (run.phase === 'shop') {
      run = applyRunCommand(run, { type: 'ShopLeave' })
    } else if (run.phase === 'event') {
      const ev = getEventDef(run.map[run.row][run.col].eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  throw new Error('エリートノードに到達できない')
}

/** 現在のラン状態から次の戦闘に入るまで非戦闘フェーズを消化する */
function intoBattle(run0: RunState): RunState {
  let run = run0
  let guard = 0
  while (run.phase !== 'combat' && guard++ < 40) {
    if (run.phase === 'map') run = chooseToward(run, 'battle')
    else if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
    else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
    else if (run.phase === 'shop') run = applyRunCommand(run, { type: 'ShopLeave' })
    else if (run.phase === 'event') {
      const ev = getEventDef(run.map[run.row][run.col].eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  return run
}

describe('エリートノード (マップ化。opt-inオファーは廃止)', () => {
  it('マップにエリートノードが4つあり、選んで入るとエリート戦になる', () => {
    const run = createRun(11, 'set-confirm')
    expect(run.map.flat().filter((n) => n.type === 'elite')).toHaveLength(4)
    const elite = intoFirstElite()
    expect(elite.phase).toBe('combat')
    expect(elite.currentElite).toBe(true)
  })

  it('エリート補正 (強化+2・HP×1.35) が敵に乗る', () => {
    const elite = intoFirstElite()
    const node = currentNode(elite)!
    const members = resolveEncounter(node.encounterId!)
    const def = getEnemyDef(members[0].enemyId)
    const expectHp = Math.round(
      def.maxHp * depthHpScale(elite.row) * 1.35 * (members[0].hpScale ?? 1),
    )
    expect(elite.combat!.enemies[0].maxHp).toBe(expectHp)
    expect(elite.combat!.enemies[0].strength).toBe(2 + (members[0].strength ?? 0))
  })

  it('エリートに勝つとレリック3択 → 取得後にカード報酬へ', () => {
    let run = intoFirstElite()
    run = forceWin(run)
    expect(run.phase).toBe('relic-reward')
    expect(run.relicOptions).toHaveLength(3)
    const picked = run.relicOptions![0]
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.relics).toEqual([picked])
    expect(run.phase).toBe('reward') // カード報酬にも進む
  })

  it('通常戦闘の勝利ではレリック報酬は出ない', () => {
    let run = intoBattle(createRun(11, 'set-confirm'))
    expect(run.currentElite).toBe(false)
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
    let run = intoFirstElite()
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf(relicId)
    if (idx >= 0) {
      run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    } else {
      // 候補に無い場合はテスト用に直接注入
      run = { ...run, relics: [relicId], relicOptions: null, phase: 'reward', rewardOptions: [] }
    }
    if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    return intoBattle(run)
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
    let run = intoFirstElite()
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
    let run = intoFirstElite()
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf('relic_collectors_bag')
    if (idx < 0) return
    run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    expect(run.rewardOptions).toHaveLength(4 + 1) // リーダー基本4 + 鞄1
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

describe('B型レリックの最大HPが戦闘へ届く (2026-08-27 バグ修正)', () => {
  // プレイテスターの報告で発覚: launchCombat が run.hp しか渡しておらず、
  // 鉄の心臓 (取得時maxHp+8) が増やした run.maxHp が戦闘の player.maxHp に反映されていなかった。
  it('鉄の心臓の+8が次の戦闘の最大HPに乗る', () => {
    let run = createRun(29, 'set-confirm')
    const baseMax = run.maxHp
    // 鉄の心臓を直接付与してボーナスを適用した状態を作る
    run = { ...run, relics: ['relic_iron_heart'] }
    run = {
      ...run,
      maxHp: baseMax + 8,
      hp: Math.min(baseMax + 8, run.hp + 8),
    }
    run = intoBattle(run)
    expect(run.combat!.player.maxHp).toBe(baseMax + 8)
  })
})
