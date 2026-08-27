// ドラフト連戦モードのテスト。「確定済みルール」表のラン関連項目をここで固定する。
import { describe, expect, it } from 'vitest'
import { getCardDef, getEnemyDef, resolveEncounter } from './content.ts'
import { applyRunCommand, createRun, depthHpScale, depthStrength, isUpgraded, RUN_BATTLES, upgradeCard } from './run.ts'
import type { RunState } from './run.ts'
import { defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** offerフェーズなら通常戦闘を選んで進める */
function declineOffer(run: RunState): RunState {
  return run.phase === 'offer' ? applyRunCommand(run, { type: 'ChooseElite', elite: false }) : run
}

/** 現在の戦闘を外科的に「全滅寸前」にして薙ぎ払い (全体攻撃) で勝つ (プレイヤーHPは維持される) */
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

describe('ラン構造', () => {
  it('15戦・段階制の敵並び (序盤/中盤/終盤/ボス) がシードで確定する', () => {
    const run = createRun(42, 'set-confirm')
    expect(run.enemyIds).toHaveLength(RUN_BATTLES)
    const tier1 = ['enemy_probe', 'enemy_wide_power', 'enc_probe_pair']
    const tier2 = [
      'enemy_set_wary',
      'enemy_set_breaker',
      'enemy_hexer',
      'enemy_joker',
      'enc_probe_trio',
      'enc_joker_drummer',
    ]
    const tier3 = [
      'enemy_brute',
      'enemy_wolf',
      'enemy_moss',
      'enemy_set_breaker',
      'enc_wolf_drummer',
      'enc_hexer_shadow',
      'enc_breaker_hexer',
    ]
    const boss = ['enemy_brute', 'enemy_turtle', 'enemy_warden']
    run.enemyIds.forEach((id, i) => {
      const pool = i < 5 ? tier1 : i < 10 ? tier2 : i < 14 ? tier3 : boss
      expect(pool).toContain(id)
    })
  })

  it('決定論: 同じシードは同じラン (敵並び・初戦の状態が一致)', () => {
    const a = createRun(7, 'set-confirm')
    const b = createRun(7, 'set-confirm')
    expect(a.enemyIds).toEqual(b.enemyIds)
    expect(JSON.stringify(a.combat)).toBe(JSON.stringify(b.combat))
  })

  it('初期デッキは run_basic の10枚 (エンジンの種入り構成)、HPは全快スタート', () => {
    const run = createRun(1, 'set-confirm')
    expect(run.deck).toHaveLength(10)
    expect(run.hp).toBe(run.maxHp)
    expect(run.combat!.player.hand.length + run.combat!.player.drawPile.length).toBe(10)
  })

  it('深度スケーリング: 若い個体補正は撤廃 (人間基準化)。強化はボスのみ+1、HPは緩ランプ', () => {
    expect(depthStrength(0)).toBe(0)
    expect(depthStrength(6)).toBe(0)
    expect(depthStrength(9)).toBe(0)
    expect(depthStrength(14)).toBe(1) // ボスのみ (15戦目)
    expect(depthHpScale(0)).toBeCloseTo(0.55) // 2026-08-26 再校正 (StS Act1帯へ)
    expect(depthHpScale(14)).toBeCloseTo(1.0)
    // 初戦から素の強さで登場 (編成の場合は先頭メンバーで検証。群れ補正 hpScale は深度と乗算)
    const run = createRun(5, 'set-confirm')
    const members = resolveEncounter(run.enemyIds[0])
    const def = getEnemyDef(members[0].enemyId)
    expect(run.combat!.enemies[0].maxHp).toBe(
      Math.round(def.maxHp * 0.55 * (members[0].hpScale ?? 1)),
    )
    expect(run.combat!.enemies[0].strength).toBe(0 + (members[0].strength ?? 0))
  })
})

describe('報酬ピック', () => {
  it('勝利で4枚提示 (重複なし・基本札除外)。ピックでデッキが増えて次戦へ', () => {
    let run = createRun(11, 'set-confirm')
    run = forceWin(run)
    expect(run.phase).toBe('reward')
    expect(run.rewardOptions).toHaveLength(4) // 2026-08-26: 3→4枚
    expect(new Set(run.rewardOptions!).size).toBe(4)
    expect(run.rewardOptions).not.toContain('green_strike')
    expect(run.rewardOptions).not.toContain('green_guard')
    const picked = run.rewardOptions![0]
    run = applyRunCommand(run, { type: 'PickReward', index: 0 })
    // 2戦目はエリートオファー対象 → 避ければ通常戦闘へ
    expect(run.phase).toBe('offer')
    run = applyRunCommand(run, { type: 'ChooseElite', elite: false })
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
  it('戦闘で受けたダメージは持ち越される (勝利ごとの自動回復なし = StS踏襲)', () => {
    let run = createRun(13, 'set-confirm')
    // 被弾した状態を作ってから勝つ
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 27 } } }
    run = forceWin(run)
    run = declineOffer(applyRunCommand(run, { type: 'SkipReward' }))
    expect(run.combat!.player.hp).toBe(27)
  })

  it('3戦目クリア後は焚き火フェーズに入り、回復は自動で入る (選択と排他にしない)', () => {
    let run = createRun(17, 'set-confirm')
    run = forceWin(run)
    run = declineOffer(applyRunCommand(run, { type: 'SkipReward' }))
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    // 3戦目 (battleIndex 2): HP20で勝つ → 焚き火の二択が開く
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 20 } } }
    run = forceWin(run)
    expect(run.phase).toBe('campfire')
    // 2026-08-26: 回復は焚き火に到達した時点で自動。実測で「回復か強化か」の二択にすると
    // 到達時HPが常に危機的なため全員が回復しか選べず、強化・除去が死に機能になっていた
    expect(run.hp).toBe(20 + Math.floor(80 * 0.3))
    run = applyRunCommand(run, { type: 'CampfireRest' }) // 「何もしない」
    expect(run.phase).toBe('reward')
  })

  it('焚き火でカード除去を選んでも回復は受け取れる (HPと排他ではない)', () => {
    let run = createRun(17, 'set-confirm')
    run = forceWin(run)
    run = declineOffer(applyRunCommand(run, { type: 'SkipReward' }))
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 20 } } }
    run = forceWin(run)
    const before = run.deck.length
    const removed = run.deck[0].uid
    const hpAfterHeal = run.hp
    run = applyRunCommand(run, { type: 'CampfireRemove', index: 0 })
    expect(run.deck).toHaveLength(before - 1)
    expect(run.deck.some((c) => c.uid === removed)).toBe(false)
    expect(run.hp).toBe(hpAfterHeal) // 回復は到達時に済んでいる
    expect(run.phase).toBe('reward')
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
  it('15戦すべて勝つとラン勝利。ボス戦の敵は強化+1・HP等倍', () => {
    let run = createRun(23, 'set-confirm')
    for (let i = 0; i < RUN_BATTLES; i++) {
      if (i === RUN_BATTLES - 1) {
        // ボス戦開始時の深度スケーリングを確認
        expect(run.combat!.enemies[0].strength).toBe(1)
        const def = getEnemyDef(run.enemyIds[RUN_BATTLES - 1])
        expect(run.combat!.enemies[0].maxHp).toBe(def.maxHp)
      }
      run = forceWin(run)
      if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
      if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
      if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
      run = declineOffer(run)
    }
    expect(run.phase).toBe('won')
  })
})

describe('プレイテスト由来の調整 (2026-08-26)', () => {
  it('敵の並びは直前2戦と同じ敵を避ける (同型の連戦を防ぐ)', () => {
    // プールが小さくて避けられない場合を除き、3連続の同一敵は出ない
    for (let seed = 1; seed <= 60; seed++) {
      const ids = createRun(seed, 'set-confirm').enemyIds
      for (let i = 2; i < ids.length; i++) {
        expect(ids[i] === ids[i - 1] && ids[i] === ids[i - 2]).toBe(false)
      }
    }
  })

  it('報酬の最後の1枠は重み付けなし = 軸外の札にも乗り換えの機会が残る', () => {
    // 緑スターターの軸は成長・ランプ。全ての提示が軸内で埋まるランは存在しないはず
    let sawOffAxis = false
    for (let seed = 1; seed <= 40 && !sawOffAxis; seed++) {
      let run = createRun(seed, 'set-confirm')
      run = forceWin(run)
      if (run.phase !== 'reward') continue
      const axisIds = ['green_growth_ring', 'green_ramp_sprout', 'green_double_lash']
      if ((run.rewardOptions ?? []).some((id) => !axisIds.includes(id))) sawOffAxis = true
    }
    expect(sawOffAxis).toBe(true)
  })
})

describe('焚き火の強化 (2026-08-26。StSの休憩所 Smith 相当)', () => {
  it('量の効果は+50%(切り上げ)になり、名前に「+」が付く', () => {
    const strike = { uid: 'u1', def: getCardDef('green_strike') } // 1E・6ダメージ
    const up = upgradeCard(strike)
    expect(up.def.name).toBe('打撃+')
    expect(up.def.effects[0].amount).toBe(9) // StS の Strike+ と同値
    expect(isUpgraded(up)).toBe(true)
    const guard = upgradeCard({ uid: 'u2', def: getCardDef('green_guard') }) // 1E・ブロック5
    expect(guard.def.effects[0].amount).toBe(8) // StS の Defend+ と同値
  })

  it('「単位」の効果と参照スケーリングは強化しない (engineの倍率に触れない安全弁)', () => {
    const sprout = upgradeCard({ uid: 'u3', def: getCardDef('green_ramp_sprout') }) // 上限+1
    expect(sprout.def.effects[0].amount).toBe(1) // gainEnergyMax は据え置き
    const ring = upgradeCard({ uid: 'u4', def: getCardDef('green_growth_ring') }) // 成長+2
    expect(ring.def.effects[0].amount).toBe(2) // addGrowth は据え置き
  })

  it('焚き火で鍛えるとデッキのその1枚だけが強くなる (同じ札は1回だけ)', () => {
    let run = createRun(17, 'set-confirm')
    run = forceWin(run)
    run = declineOffer(applyRunCommand(run, { type: 'SkipReward' }))
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    run = forceWin(run)
    expect(run.phase).toBe('campfire')
    const idx = run.deck.findIndex((c) => c.def.id === 'green_strike')
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: idx })
    expect(run.deck[idx].def.name).toBe('打撃+')
    expect(run.deck.filter((c) => c.def.name === '打撃+')).toHaveLength(1)
    expect(run.phase).toBe('reward')
  })
})
