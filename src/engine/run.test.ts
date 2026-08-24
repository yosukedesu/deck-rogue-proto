// ドラフト連戦モードのテスト。「確定済みルール」表のラン関連項目をここで固定する。
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyRunCommand, createRun, depthHpScale, depthStrength, RUN_BATTLES } from './run.ts'
import type { RunState } from './run.ts'
import { defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** 現在の戦闘を外科的に「あと一撃」にして打撃で勝つ (プレイヤーHPは維持される) */
function forceWin(run: RunState): RunState {
  const c = run.combat!
  let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
  surgical = withIntent(withHand(surgical, ['green_strike']), defendIntent(0))
  return applyRunCommand(
    { ...run, combat: surgical },
    { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_strike' } },
  )
}

describe('ラン構造', () => {
  it('10戦・段階制の敵並び (序盤/中盤/終盤/ボス) がシードで確定する', () => {
    const run = createRun(42, 'set-confirm')
    expect(run.enemyIds).toHaveLength(RUN_BATTLES)
    const tier1 = ['enemy_probe', 'enemy_wide_power']
    const tier2 = ['enemy_set_wary', 'enemy_set_breaker']
    const tier3 = [...tier2, 'enemy_brute', 'enemy_turtle']
    const boss = ['enemy_brute', 'enemy_turtle']
    run.enemyIds.forEach((id, i) => {
      const pool = i < 3 ? tier1 : i < 6 ? tier2 : i < 9 ? tier3 : boss
      expect(pool).toContain(id)
    })
  })

  it('決定論: 同じシードは同じラン (敵並び・初戦の状態が一致)', () => {
    const a = createRun(7, 'set-confirm')
    const b = createRun(7, 'set-confirm')
    expect(a.enemyIds).toEqual(b.enemyIds)
    expect(JSON.stringify(a.combat)).toBe(JSON.stringify(b.combat))
  })

  it('初期デッキは run_basic の10枚 (打撃5/防御4/茨1)、HPは全快スタート', () => {
    const run = createRun(1, 'set-confirm')
    expect(run.deck).toHaveLength(10)
    expect(run.hp).toBe(run.maxHp)
    expect(run.combat!.player.hand.length + run.combat!.player.drawPile.length).toBe(10)
  })

  it('深度スケーリング: 序盤は「若い個体」(弱体) で、ボスに向かって強くなる', () => {
    expect(depthStrength(0)).toBe(-4)
    expect(depthStrength(6)).toBe(-3)
    expect(depthStrength(9)).toBe(-1)
    expect(depthHpScale(0)).toBeCloseTo(0.4)
    expect(depthHpScale(9)).toBeCloseTo(0.67)
    // 初戦の敵は弱体状態で登場
    const run = createRun(5, 'set-confirm')
    const def = getEnemyDef(run.enemyIds[0])
    expect(run.combat!.enemies[0].maxHp).toBe(Math.round(def.maxHp * 0.4))
    expect(run.combat!.enemies[0].strength).toBe(-4)
  })
})

describe('報酬ピック', () => {
  it('勝利で3枚提示 (重複なし・基本札除外)。ピックでデッキが増えて次戦へ', () => {
    let run = createRun(11, 'set-confirm')
    run = forceWin(run)
    expect(run.phase).toBe('reward')
    expect(run.rewardOptions).toHaveLength(3)
    expect(new Set(run.rewardOptions!).size).toBe(3)
    expect(run.rewardOptions).not.toContain('green_strike')
    expect(run.rewardOptions).not.toContain('green_guard')
    const picked = run.rewardOptions![0]
    run = applyRunCommand(run, { type: 'PickReward', index: 0 })
    expect(run.phase).toBe('combat')
    expect(run.battleIndex).toBe(1)
    expect(run.deck).toHaveLength(11)
    expect(run.picks).toEqual([picked])
  })

  it('報酬はランの色のカードのみ (カラーパイを無視しない)', () => {
    let run = createRun(11, 'set-confirm') // 緑ラン
    run = forceWin(run)
    expect(run.rewardOptions!.length).toBeGreaterThan(0)
    for (const cardId of run.rewardOptions!) {
      expect(cardId.startsWith('green_')).toBe(true)
    }
  })

  it('スキップするとデッキは増えず次戦へ', () => {
    let run = createRun(11, 'set-confirm')
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    expect(run.battleIndex).toBe(1)
    expect(run.deck).toHaveLength(10)
  })

  it('戦闘中の PickReward は拒否される', () => {
    const run = createRun(3, 'set-confirm')
    expect(() => applyRunCommand(run, { type: 'PickReward', index: 0 })).toThrow(/報酬フェーズ/)
  })
})

describe('HP持ち越しと焚き火', () => {
  it('戦闘で受けたダメージは持ち越される (勝利ごとの小休止+10のみ回復)', () => {
    let run = createRun(13, 'set-confirm')
    // 被弾した状態を作ってから勝つ
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 27 } } }
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    expect(run.combat!.player.hp).toBe(27 + 10)
  })

  it('3戦目クリア後は小休止+10に加え焚き火で最大HPの30%回復 (上限あり)', () => {
    let run = createRun(17, 'set-confirm')
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    // 3戦目 (battleIndex 2): HP20で勝つ → 小休止+10 + 焚き火+15 (50の30%)
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 20 } } }
    run = forceWin(run)
    expect(run.hp).toBe(20 + 10 + 16) // 焚き火は最大HP55の30% (緑リーダー)
  })

  it('敗北でランは終了する', () => {
    let run = createRun(19, 'set-confirm')
    const c = run.combat!
    // 敵の攻撃で確実に死ぬ状態を作る
    let surgical: GameState = { ...c, player: { ...c.player, hp: 1, block: 0, hand: [] } }
    surgical = withIntent(surgical, { kind: 'attack', shownMin: 5, shownMax: 5, actual: 5 })
    run = { ...run, combat: surgical }
    run = applyRunCommand(run, { type: 'Combat', command: { type: 'EndTurn' } })
    expect(run.phase).toBe('lost')
  })
})

describe('ラン走破', () => {
  it('10戦すべて勝つとラン勝利。ボス戦の敵は強化+3', () => {
    let run = createRun(23, 'set-confirm')
    for (let i = 0; i < RUN_BATTLES; i++) {
      if (i === RUN_BATTLES - 1) {
        // ボス戦開始時の深度スケーリングを確認
        expect(run.combat!.enemies[0].strength).toBe(-1)
        const def = getEnemyDef(run.enemyIds[9])
        expect(run.combat!.enemies[0].maxHp).toBe(Math.round(def.maxHp * 0.67))
      }
      run = forceWin(run)
      if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    }
    expect(run.phase).toBe('won')
  })
})
