// ドラフト連戦モード (マップラン) のテスト。「確定済みルール」表のラン関連項目をここで固定する。
import { describe, expect, it } from 'vitest'
import { getCardDef, getEnemyDef, resolveEncounter, getEventDef } from './content.ts'
import { BOSS_ROW, MAP_ROWS, tierForRow } from './map.ts'
import {
  applyRunCommand,
  createRun,
  currentNode,
  depthHpScale,
  depthStrength,
  isUpgraded,
  upgradeCard,
} from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

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

/** 目的タイプのノードに入るまでラン進行を回す (途中の戦闘は forceWin・報酬等はスキップ) */
function runTo(run: RunState, target: 'campfire' | 'workshop' | 'elite' | 'boss'): RunState {
  let r = run
  let guard = 0
  while (guard++ < 80) {
    if (r.phase === 'map') {
      r = chooseToward(r, target)
      if (r.phase === 'combat' && target === 'elite' && r.currentElite) return r
      if (r.phase === 'combat' && target === 'boss' && currentNode(r)?.type === 'boss') return r
      continue
    }
    if (r.phase === 'campfire') {
      if (target === 'campfire') return r
      r = applyRunCommand(r, { type: 'CampfireRest' })
    } else if (r.phase === 'workshop') {
      if (target === 'workshop') return r
      r = applyRunCommand(r, { type: 'WorkshopSkip' })
    } else if (r.phase === 'combat') {
      r = forceWin(r)
    } else if (r.phase === 'shop') {
      r = applyRunCommand(r, { type: 'ShopLeave' })
    } else if (r.phase === 'event') {
      const node = r.map[r.row][r.col]
      const ev = getEventDef(node.eventId!)
      r = applyRunCommand(r, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else if (r.phase === 'relic-reward') {
      r = applyRunCommand(r, { type: 'SkipRelic' })
    } else if (r.phase === 'reward') {
      r = applyRunCommand(r, { type: 'SkipReward' })
    } else {
      return r
    }
  }
  throw new Error('runTo が収束しない')
}

/** 最初の戦闘に入る */
function intoFirstBattle(run: RunState): RunState {
  let r = run
  while (r.phase === 'map') r = chooseToward(r, 'battle')
  return r
}

describe('ラン構造 (マップ)', () => {
  it('戦闘ノードの敵は行の帯 (Act1/2/3/ボス) のプールから出る', () => {
    const run = createRun(42, 'set-confirm')
    run.map.forEach((row, r) => {
      for (const node of row) {
        if (node.encounterId !== null) {
          expect(tierForRow(r), `row ${r}`).toContain(node.encounterId)
        }
      }
    })
  })

  it('決定論: 同じシードは同じマップ・同じ初戦', () => {
    const a = createRun(7, 'set-confirm')
    const b = createRun(7, 'set-confirm')
    expect(JSON.stringify(a.map)).toBe(JSON.stringify(b.map))
    const a1 = intoFirstBattle(a)
    const b1 = intoFirstBattle(b)
    expect(JSON.stringify(a1.combat)).toBe(JSON.stringify(b1.combat))
  })

  it('初期デッキは run_basic の10枚 (エンジンの種入り構成)、HPは全快スタート', () => {
    const run = createRun(1, 'set-confirm')
    expect(run.deck).toHaveLength(10)
    expect(run.hp).toBe(run.maxHp)
    expect(run.phase).toBe('map') // 開始はマップで行0のノードを選ぶ
    const r = intoFirstBattle(run)
    expect(r.combat!.player.hand.length + r.combat!.player.drawPile.length).toBe(10)
  })

  it('深度スケーリング: 強化はボスのみ+1、HPは緩ランプ (行基準)', () => {
    expect(depthStrength(0)).toBe(0)
    expect(depthStrength(9)).toBe(0)
    expect(depthStrength(BOSS_ROW)).toBe(1) // ボスのみ (行15)
    expect(depthHpScale(0)).toBeCloseTo(0.55)
    expect(depthHpScale(14)).toBeCloseTo(0.95)
    expect(depthHpScale(BOSS_ROW)).toBeCloseTo(1.0)
    // 初戦から素の強さで登場 (編成の場合は先頭メンバーで検証。群れ補正 hpScale は深度と乗算)
    const run = intoFirstBattle(createRun(5, 'set-confirm'))
    const members = resolveEncounter(currentNode(run)!.encounterId!)
    const def = getEnemyDef(members[0].enemyId)
    expect(run.combat!.enemies[0].maxHp).toBe(
      Math.round(def.maxHp * 0.55 * (members[0].hpScale ?? 1)),
    )
    expect(run.combat!.enemies[0].strength).toBe(0 + (members[0].strength ?? 0))
  })
})

describe('報酬ピック', () => {
  it('勝利で4枚提示 (重複なし・基本札除外)。ピックでデッキが増えてマップへ戻る', () => {
    let run = intoFirstBattle(createRun(11, 'set-confirm'))
    run = forceWin(run)
    expect(run.phase).toBe('reward')
    expect(run.rewardOptions).toHaveLength(4) // 2026-08-26: 3→4枚
    expect(new Set(run.rewardOptions!).size).toBe(4)
    expect(run.rewardOptions).not.toContain('green_strike')
    expect(run.rewardOptions).not.toContain('green_guard')
    const picked = run.rewardOptions![0]
    run = applyRunCommand(run, { type: 'PickReward', index: 0 })
    expect(run.phase).toBe('map') // ピック後はマップで次のノードを選ぶ
    expect(run.deck).toHaveLength(11)
    expect(run.picks).toEqual([picked])
    expect(run.battlesWon).toBe(1)
  })

  it('報酬はランの色のカードのみ (カラーパイを無視しない)', () => {
    let run = intoFirstBattle(createRun(11, 'set-confirm')) // 緑ラン
    run = forceWin(run)
    expect(run.rewardOptions!.length).toBeGreaterThan(0)
    for (const cardId of run.rewardOptions!) {
      expect(cardId.startsWith('green_')).toBe(true)
    }
  })

  it('スキップするとデッキは増えずマップへ', () => {
    let run = intoFirstBattle(createRun(11, 'set-confirm'))
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    expect(run.phase).toBe('map')
    expect(run.deck).toHaveLength(10)
  })

  it('マップフェーズの PickReward は拒否される', () => {
    const run = createRun(3, 'set-confirm')
    expect(() => applyRunCommand(run, { type: 'PickReward', index: 0 })).toThrow(/報酬フェーズ/)
  })

  it('進めないノードへの ChooseNode は拒否される', () => {
    const run = createRun(3, 'set-confirm')
    expect(() => applyRunCommand(run, { type: 'ChooseNode', col: 9 })).toThrow(/進めないノード/)
  })
})

describe('HP持ち越しと焚き火', () => {
  it('戦闘で受けたダメージは持ち越される (勝利ごとの自動回復なし = StS踏襲)', () => {
    let run = intoFirstBattle(createRun(13, 'set-confirm'))
    // 被弾した状態を作ってから勝つ
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 27 } } }
    run = forceWin(run)
    run = applyRunCommand(run, { type: 'SkipReward' })
    run = intoFirstBattle(run) // 次の戦闘へ
    expect(run.combat!.player.hp).toBe(27)
  })

  it('強制焚き火行 (行5) に入ると回復が自動で入る (選択と排他にしない)', () => {
    const run = runTo(createRun(17, 'set-confirm'), 'campfire')
    expect(run.phase).toBe('campfire')
    expect(run.row).toBe(5) // 最初の強制焚き火行
    // HP20で到達した状況を作り直して回復量を確認する
    let r2 = createRun(17, 'set-confirm')
    let guard = 0
    while (guard++ < 60) {
      if (r2.phase === 'map') {
        r2 = chooseToward(r2, 'campfire')
      } else if (r2.phase === 'combat') {
        r2 = { ...r2, combat: { ...r2.combat!, player: { ...r2.combat!.player, hp: 20 } } }
        r2 = forceWin(r2)
      } else if (r2.phase === 'reward') {
        r2 = applyRunCommand(r2, { type: 'SkipReward' })
      } else if (r2.phase === 'relic-reward') {
        r2 = applyRunCommand(r2, { type: 'SkipRelic' })
      } else if (r2.phase === 'workshop') {
        r2 = applyRunCommand(r2, { type: 'WorkshopSkip' })
      } else if (r2.phase === 'shop') {
        r2 = applyRunCommand(r2, { type: 'ShopLeave' })
      } else if (r2.phase === 'event') {
        const ev = getEventDef(r2.map[r2.row][r2.col].eventId!)
        r2 = applyRunCommand(r2, { type: 'EventChoice', index: ev.choices.length - 1 })
      } else break
    }
    expect(r2.phase).toBe('campfire')
    expect(r2.hp).toBe(20 + Math.floor(r2.maxHp * 0.3))
    r2 = applyRunCommand(r2, { type: 'CampfireRest' }) // 「何もしない」
    expect(r2.phase).toBe('map')
  })

  it('焚き火でカード除去を選んでも回復は受け取れる (HPと排他ではない)', () => {
    let run = runTo(createRun(17, 'set-confirm'), 'campfire')
    const before = run.deck.length
    const removed = run.deck[0].uid
    const hpAfterHeal = run.hp
    run = applyRunCommand(run, { type: 'CampfireRemove', index: 0 })
    expect(run.deck).toHaveLength(before - 1)
    expect(run.deck.some((c) => c.uid === removed)).toBe(false)
    expect(run.hp).toBe(hpAfterHeal) // 回復は到達時に済んでいる
    expect(run.phase).toBe('map')
  })

  it('敗北でランは終了する', () => {
    let run = intoFirstBattle(createRun(19, 'set-confirm'))
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
  it('ボス (行15) を倒すとラン勝利。ボス戦の敵は強化+1・HP等倍', () => {
    let run = runTo(createRun(23, 'set-confirm'), 'boss')
    expect(run.phase).toBe('combat')
    expect(currentNode(run)!.type).toBe('boss')
    expect(run.combat!.enemies[0].strength).toBe(1)
    const def = getEnemyDef(currentNode(run)!.encounterId!)
    expect(run.combat!.enemies[0].maxHp).toBe(def.maxHp)
    run = forceWin(run)
    expect(run.phase).toBe('won')
    // マップ保証: 走破時の戦闘数はボス込みで11〜13 (通常戦10〜12+ボス)
    expect(run.battlesWon).toBeGreaterThanOrEqual(11)
    expect(run.battlesWon).toBeLessThanOrEqual(13)
  })
})

describe('プレイテスト由来の調整 (2026-08-26)', () => {
  it('中盤以降の敵は3行連続で同じにならない (同型の連戦を防ぐ)', () => {
    // 行0〜4はプールが3種と小さく回避しきれないことがあるため、中盤以降で検証する
    for (let seed = 1; seed <= 40; seed++) {
      const map = createRun(seed, 'set-confirm').map
      for (let r = 8; r < MAP_ROWS; r++) {
        const ids = (row: number) =>
          map[row].map((n) => n.encounterId).filter((x): x is string => x !== null)
        for (const id of ids(r)) {
          expect(ids(r - 1).includes(id) && ids(r - 2).includes(id), `seed${seed} row${r}`).toBe(
            false,
          )
        }
      }
    }
  })

  it('報酬の最後の1枠は重み付けなし = 軸外の札にも乗り換えの機会が残る', () => {
    // 緑スターターの軸は成長・ランプ。全ての提示が軸内で埋まるランは存在しないはず
    let sawOffAxis = false
    for (let seed = 1; seed <= 40 && !sawOffAxis; seed++) {
      let run = intoFirstBattle(createRun(seed, 'set-confirm'))
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
    let run = runTo(createRun(17, 'set-confirm'), 'campfire')
    expect(run.phase).toBe('campfire')
    const idx = run.deck.findIndex((c) => c.def.id === 'green_strike')
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: idx })
    expect(run.deck[idx].def.name).toBe('打撃+')
    expect(run.deck.filter((c) => c.def.name === '打撃+')).toHaveLength(1)
    expect(run.phase).toBe('map')
  })
})
