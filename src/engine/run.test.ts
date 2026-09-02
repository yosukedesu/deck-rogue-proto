// ドラフト連戦モード (マップラン) のテスト。「確定済みルール」表のラン関連項目をここで固定する。
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getEnemyDef, resolveEncounter, getEventDef } from './content.ts'
import { ACT_BOSS_POOLS, bossRowFor, ACT_COUNT, BOSS_ROW, ELITE_POOLS, generateMap, tierFor, TREASURE_ROW } from './map.ts'
import { createRng } from './rng.ts'
import {
  applyRunCommand,
  createDebugCheckpointRun,
  createRun,
  currentNode,
  DEFAULT_DIFFICULTY,
  depthHpScale,
  depthStrength,
  DIFFICULTY_TABLE,
  difficultyScale,
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
      const ev = getEventDef(r.eventId!)
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
  let guard = 0
  // ショップ・工房はHPに触れず素通りできる。イベント・焚き火は stepToward が原則避ける
  while (r.phase !== 'combat' && guard++ < 40) {
    if (r.phase === 'map') r = chooseToward(r, 'battle')
    else if (r.phase === 'shop') r = applyRunCommand(r, { type: 'ShopLeave' })
    else if (r.phase === 'workshop') r = applyRunCommand(r, { type: 'WorkshopSkip' })
    else break
  }
  return r
}

describe('ラン構造 (マップ)', () => {
  it('戦闘ノードの敵は行の帯 (Act1/2/3/ボス) のプールから出る', () => {
    const run = createRun(42, 'set-confirm')
    run.map.forEach((row, r) => {
      for (const node of row) {
        if (node.encounterId !== null) {
          // エリートノードは専用プール (2026-08-31)。それ以外は行の帯
          const pool = node.type === 'elite' ? ELITE_POOLS[0] : tierFor(1, r)
          expect(pool, `row ${r}`).toContain(node.encounterId)
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
    // 2026-08-29 テンポ再校正②: 打ち据え (Bash枠=急所の乗算) がスターターに1枚入る。
    // sim実測で通常敵平均 -1.3T の最大レバー。報酬プールには出ない基本札
    expect(run.deck.filter((c) => c.def.id === 'green_basic_bash')).toHaveLength(1)
    // 2026-08-31 個性注入: 打撃1・二連の蔦打ち1 (多段=成長パッシブ直結)・蔦の楔1 (火花型の派生)
    expect(run.deck.filter((c) => c.def.id === 'green_strike')).toHaveLength(1)
    expect(run.hp).toBe(run.maxHp)
    expect(run.phase).toBe('map') // 開始はマップで行0のノードを選ぶ
    const r = intoFirstBattle(run)
    expect(r.combat!.player.hand.length + r.combat!.player.drawPile.length).toBe(10)
  })

  it('深度スケーリング: 強化はボスのみ+1、HPは緩ランプ (行基準)', () => {
    expect(depthStrength(0)).toBe(0)
    expect(depthStrength(9)).toBe(0)
    expect(depthStrength(BOSS_ROW)).toBe(1) // ボスのみ (行15)
    expect(depthHpScale(0, 1)).toBeCloseTo(0.62) // 2026-09-02 幕1+0.07 (StS2対照の幕1増強)
    expect(depthHpScale(14, 1)).toBeCloseTo(0.72) // 幕内後半 (2段スケール。2026-09-02 +0.07)
    expect(depthHpScale(0, 3)).toBeCloseTo(1.2) // 2026-09-01 幕2/3を+0.15 (完成デッキに2Tで溶ける谷の受け)
    expect(depthHpScale(BOSS_ROW, 1)).toBeCloseTo(1.0) // 幕ボスは素のHP
    // 初戦から素の強さで登場 (編成の場合は先頭メンバーで検証。群れ補正 hpScale は深度と乗算)
    const run = intoFirstBattle(createRun(5, 'set-confirm'))
    const members = resolveEncounter(currentNode(run)!.encounterId!)
    const def = getEnemyDef(members[0].enemyId)
    expect(run.combat!.enemies[0].maxHp).toBe(
      Math.round(def.maxHp * 0.62 * (members[0].hpScale ?? 1)), // 2026-09-02 幕1+0.07
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

describe('宝箱行 (2026-08-31)', () => {
  it('宝箱行に入るとレリック3択になり、選んでもカード報酬は付かずマップへ戻る', () => {
    let r = createRun(21, 'set-confirm')
    let guard = 0
    while (r.row < TREASURE_ROW && guard++ < 120) {
      if (r.phase === 'map') r = chooseToward(r, 'treasure')
      else if (r.phase === 'combat') r = forceWin(r)
      else if (r.phase === 'reward') r = applyRunCommand(r, { type: 'SkipReward' })
      else if (r.phase === 'relic-reward') r = applyRunCommand(r, { type: 'SkipRelic' })
      else if (r.phase === 'campfire') r = applyRunCommand(r, { type: 'CampfireRest' })
      else if (r.phase === 'shop') r = applyRunCommand(r, { type: 'ShopLeave' })
      else if (r.phase === 'workshop') r = applyRunCommand(r, { type: 'WorkshopSkip' })
      else if (r.phase === 'event') {
        const ev = getEventDef(r.eventId!)
        r = applyRunCommand(r, { type: 'EventChoice', index: ev.choices.length - 1 })
      } else break
    }
    expect(r.row).toBe(TREASURE_ROW)
    expect(r.phase).toBe('relic-reward')
    const before = r.relics.length
    r = applyRunCommand(r, { type: 'PickRelic', index: 0 })
    expect(r.relics).toHaveLength(before + 1)
    expect(r.phase).toBe('map') // カード報酬は付かない (宝箱はレリックのみ)
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

  it('焚き火では「休む」を選ぶと回復する (2026-08-29 本家式の排他三択。2026-08-31 散布配置)', () => {
    const run = runTo(createRun(17, 'set-confirm'), 'campfire')
    expect(run.phase).toBe('campfire')
    expect(run.row).toBeGreaterThanOrEqual(5) // 焚き火は行5以降にしか置かれない (本家準拠)
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
        const ev = getEventDef(r2.eventId!)
        r2 = applyRunCommand(r2, { type: 'EventChoice', index: ev.choices.length - 1 })
      } else break
    }
    expect(r2.phase).toBe('campfire')
    expect(r2.hp).toBe(20) // 進入時の自動回復は廃止 (2026-08-29)
    r2 = applyRunCommand(r2, { type: 'CampfireRest' }) // 「休む」= ここで初めて回復
    expect(r2.hp).toBe(20 + Math.floor(r2.maxHp * 0.25)) // 30%→25% (2026-08-31 様子見)
    expect(r2.phase).toBe('map')
  })

  it('焚き火でカード除去を選ぶと回復は受け取れない (排他三択。2026-08-29 復帰)', () => {
    let run = runTo(createRun(17, 'set-confirm'), 'campfire')
    const before = run.deck.length
    const removed = run.deck[0].uid
    const hpBefore = run.hp
    run = applyRunCommand(run, { type: 'CampfireRemove', index: 0 })
    expect(run.deck).toHaveLength(before - 1)
    expect(run.deck.some((c) => c.uid === removed)).toBe(false)
    expect(run.hp).toBe(hpBefore) // 除去を選んだので回復なし
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

describe('ラン走破 (3幕構成)', () => {
  it('1幕ボスを倒すと全回復+レリック3択、次の幕のマップが生成される', () => {
    let run = runTo(createRun(23, 'set-confirm'), 'boss')
    expect(run.phase).toBe('combat')
    expect(currentNode(run)!.type).toBe('boss')
    expect(currentNode(run)!.encounterId).toBe('enemy_brute') // 1幕ボス=オーガ
    expect(run.combat!.enemies[0].strength).toBe(1)
    const def = getEnemyDef(currentNode(run)!.encounterId!)
    expect(run.combat!.enemies[0].maxHp).toBe(Math.round(def.maxHp * 1.35)) // 幕1ボス×1.35 (2026-09-02 本家最弱ボス水準)
    // 被弾した状態でボスを倒す → 全回復を確認
    run = { ...run, combat: { ...run.combat!, player: { ...run.combat!.player, hp: 12 } } }
    run = forceWin(run)
    expect(run.hp).toBe(run.maxHp) // 幕ボス撃破で全回復
    expect(run.phase).toBe('relic-reward') // 本家のボスレリック相当
    run = applyRunCommand(run, { type: 'SkipRelic' })
    expect(run.phase).toBe('reward')
    run = applyRunCommand(run, { type: 'SkipReward' })
    expect(run.act).toBe(2) // 次の幕へ
    expect(run.phase).toBe('map')
    expect(run.row).toBe(-1)
    expect(ACT_BOSS_POOLS[1]).toContain(run.map[bossRowFor(2)][0].encounterId) // 2026-09-02 幕2ボスは大亀/巨蟹の抽選 // 2幕ボス=大亀
  })

  it('3幕すべてのボスを倒すとラン走破。戦闘数は幕あたり9〜15×3 (2026-08-29 18行化+?増設)', () => {
    let run = createRun(23, 'set-confirm')
    // ボスの幕スケール (確定済みルール表「マップ」): HP×1.0/1.6/2.4・強化+1/+1/+2
    const bossHpScale = [1.35, 2.3, 2.4] // 2026-09-02 本家対照: 幕2 1.6→2.3 (大亀実効322=本家幕2ボス帯下端) // 2026-08-29 幕1ボス×1.25 (ユーザー体感「ボスが弱い」)
    const bossStr = [1, 1, 2]
    for (let act = 1; act <= ACT_COUNT; act++) {
      expect(run.act).toBe(act)
      run = runTo(run, 'boss')
      const def = getEnemyDef(resolveEncounter(currentNode(run)!.encounterId!)[0].enemyId) // 編成ボス (血族/巨蟹) は先頭メンバーで検証
      expect(run.combat!.enemies[0].maxHp).toBe(Math.round(def.maxHp * bossHpScale[act - 1]))
      expect(run.combat!.enemies[0].strength).toBe(bossStr[act - 1])
      run = forceWin(run)
      if (act < ACT_COUNT) {
        if (run.phase === 'relic-reward') run = applyRunCommand(run, { type: 'SkipRelic' })
        if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
      }
    }
    expect(run.phase).toBe('won')
    // 15/14/13行化 (2026-09-02) + ショップ固定3 + 到達保証で非戦闘ノードが増えた = ボットの経路で
    // 幕あたり7〜15戦。戦闘数の保証は本家準拠で撤廃済みなので下限は緩く
    expect(run.battlesWon).toBeGreaterThanOrEqual(21)
    expect(run.battlesWon).toBeLessThanOrEqual(45)
  })
})

describe('レアリティ抽選 (2026-08-29。確定済みルール表「レアリティ」)', () => {
  it('報酬はコモン優勢・レアは希少 (スロット3%)。デッキ内容は抽選に影響しない', () => {
    const rarityOf = (id: string) => allCards.find((c) => c.id === id)?.rarity ?? 'common'
    const counts = { common: 0, uncommon: 0, rare: 0 }
    let screens = 0
    for (let seed = 1; seed <= 120; seed++) {
      let run = intoFirstBattle(createRun(seed, 'set-confirm'))
      run = forceWin(run)
      if (run.phase !== 'reward') continue
      screens++
      for (const id of run.rewardOptions ?? []) counts[rarityOf(id)]++
    }
    const total = counts.common + counts.uncommon + counts.rare
    expect(screens).toBeGreaterThan(100)
    expect(counts.common / total).toBeGreaterThan(0.45) // 期待60%
    expect(counts.uncommon / total).toBeGreaterThan(0.25) // 期待37%
    expect(counts.rare / total).toBeLessThan(0.1) // 期待3%
    expect(counts.rare).toBeGreaterThan(0) // レアも出る
  })
})

describe('中立スターター (2026-08-29 道の選択制を撤回。確定済みルール表「ラン初期デッキ」)', () => {
  it('スターターは個性注入10枚 (2026-08-31: 基本札2〜4枚をパッシブ直結の個性札に差し替え)', () => {
    // 赤のレシピの横展開: パッシブと直結する個性札が最初の戦闘から見えること。
    // 緑=二連の蔦打ち (多段×成長)+絡み蔦 (モード)。エンジンの種 (年輪・芽吹き) は入れない方針は維持
    const run = createRun(5, 'set-confirm', 'leader_green')
    expect(run.deck).toHaveLength(10)
    const count = (id: string) => run.deck.filter((c) => c.def.id === id).length
    expect(count('green_strike')).toBe(1)
    expect(count('green_vine_wedge')).toBe(1)
    expect(count('green_basic_bash')).toBe(1)
    expect(count('green_double_lash')).toBe(1)
    expect(count('green_guard')).toBe(3)
    expect(count('green_entangle')).toBe(1)
    expect(count('green_reaction_thorns')).toBe(1)
    expect(count('green_reaction_vine')).toBe(1)
  })

  it('リーダーが許可しない初期デッキは拒否される (道は廃止済み)', () => {
    expect(() => createRun(5, 'set-confirm', 'leader_green', 'run_trample')).toThrow(/選べない初期デッキ/)
  })
})

describe('プレイテスト由来の調整 (2026-08-26)', () => {
  it('敵の3行連続はプール枯渇時のフォールバックを除きほぼ発生しない (同型の連戦を防ぐ)', () => {
    // 回避は「直前2行と同じ敵を避ける。プールが小さくて避けられない場合はそのまま」の
    // ベストエフォート仕様 (確定済みルール表「ランの敵並び」)。幅3の行×プール6種では
    // ごく稀に枯渇フォールバックが起きるため、発生率1%未満を機械固定する (実測0.3%)
    let violations = 0
    let checked = 0
    for (let seed = 1; seed <= 40; seed++) {
      const [map] = generateMap(createRng(seed), 2)
      for (let r = 2; r < map.length; r++) {
        const ids = (row: number) =>
          map[row].map((n) => n.encounterId).filter((x): x is string => x !== null)
        for (const id of ids(r)) {
          checked++
          if (ids(r - 1).includes(id) && ids(r - 2).includes(id)) violations++
        }
      }
    }
    expect(violations / checked).toBeLessThan(0.01)
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

describe('上限参照札の強化は0Eに落とさない (2026-08-30 裁定)', () => {
  it('木陰の守り+ はコスト1のまま固定ブロック+4が付く (0E・タダ盾の退化ケースを塞ぐ)', () => {
    const up = upgradeCard({ uid: 't', def: getCardDef('green_canopy_shade') })
    expect(up.def.cost).toBe(1) // 0Eにならない
    expect(up.def.effects.some((e) => e.effect === 'gainBlock' && e.amount === 4)).toBe(true)
    expect(up.def.effects.some((e) => e.effect === 'gainBlockPerEnergyMax' && e.amount === 2)).toBe(true)
  })

  it('2E以上の上限参照札 (幹撃) は従来どおりコスト-1', () => {
    const up = upgradeCard({ uid: 't', def: getCardDef('green_trunk_blow') })
    expect(up.def.cost).toBe(1)
  })
})

describe('スターター札は報酬プールに出ない (2026-08-30 中立スターター化の追随)', () => {
  it('報酬候補にスターター5種 (打撃/打ち据え/防御/茨の返し/守りの蔓) が出ない', () => {
    // 40戦ぶんの報酬を回して1枚も出ないことを確認する
    const STARTERS = [
      'green_strike',
      'green_basic_bash',
      'green_guard',
      'green_reaction_thorns',
      'green_reaction_vine',
    ]
    for (let seed = 1; seed <= 10; seed++) {
      let run = intoFirstBattle(createRun(seed, 'set-confirm'))
      run = forceWin(run)
      for (const id of STARTERS) {
        expect(run.rewardOptions, `seed${seed}`).not.toContain(id)
      }
    }
  })
})

describe('難易度10段階 (確定済みルール表「難易度」2026-09-01)', () => {
  it('表の固定: 10段・段3=×1.0/×1.0・段10=HP×1.35/打点×3.0・単調非減少・打点優先', () => {
    expect(DIFFICULTY_TABLE).toHaveLength(10)
    expect(DEFAULT_DIFFICULTY).toBe(3)
    expect(DIFFICULTY_TABLE[2]).toEqual({ hp: 1.0, atk: 1.0 })
    expect(DIFFICULTY_TABLE[9]).toEqual({ hp: 1.35, atk: 3.0 })
    expect(DIFFICULTY_TABLE[0].hp).toBeLessThan(1) // 1〜2は現状より易しい側
    for (let i = 1; i < 10; i++) {
      expect(DIFFICULTY_TABLE[i].hp).toBeGreaterThanOrEqual(DIFFICULTY_TABLE[i - 1].hp)
      expect(DIFFICULTY_TABLE[i].atk).toBeGreaterThanOrEqual(DIFFICULTY_TABLE[i - 1].atk)
    }
    // 打点優先 (ユーザー選択): 4以上の段では打点倍率がHP倍率以上
    for (let i = 3; i < 10; i++) {
      expect(DIFFICULTY_TABLE[i].atk).toBeGreaterThanOrEqual(DIFFICULTY_TABLE[i].hp)
    }
  })

  it('difficultyScale: 旧セーブの欠落 (undefined) は既定3・範囲外は表の端へ丸める', () => {
    expect(difficultyScale(undefined)).toEqual({ hp: 1.0, atk: 1.0 })
    expect(difficultyScale(0)).toEqual(DIFFICULTY_TABLE[0])
    expect(difficultyScale(99)).toEqual(DIFFICULTY_TABLE[9])
  })

  it('createRun の既定は3 (現状維持) で、範囲外指定は丸めて保存する', () => {
    expect(createRun(1, 'set-confirm').difficulty).toBe(DEFAULT_DIFFICULTY)
    expect(createRun(1, 'set-confirm', 'leader_green', undefined, 15).difficulty).toBe(10)
  })

  it('難易度10は最初の戦闘から敵HP・打点が上がる (段3との同シード比較)', () => {
    const first = (d: number) =>
      intoFirstBattle(createRun(42, 'set-confirm', 'leader_green', undefined, d)).combat!
    const base = first(3)
    const hard = first(10)
    expect(hard.enemies[0].enemyId).toBe(base.enemies[0].enemyId) // 同シード=同じ敵
    // HP×1.35 (丸めは combat 側で1回だけ)
    expect(hard.enemies[0].maxHp / base.enemies[0].maxHp).toBeCloseTo(1.35, 1)
    expect(hard.enemies[0].atkScale).toBe(3.0) // 幕1通常敵: 1 × 3.0
    expect(base.enemies[0].atkScale).toBeUndefined() // 段3=×1.0 は現状と完全一致 (無印)
  })

  it('全敵一律 (ユーザー選択): ボス・エリートにも難易度倍率が掛かる', () => {
    const to = (d: number, target: 'boss' | 'elite') =>
      runTo(createRun(7, 'set-confirm', 'leader_green', undefined, d), target)
    expect(to(10, 'boss').combat!.enemies[0].atkScale).toBe(3.0)
    // エリート: 素の値×難易度のみ (幕内深度スケールを掛けない既存裁定は維持)
    const e3 = to(3, 'elite').combat!.enemies[0]
    const e10 = to(10, 'elite').combat!.enemies[0]
    expect(e10.enemyId).toBe(e3.enemyId) // 難易度はRNG列に影響しない=同じ敵
    expect(e10.maxHp / e3.maxHp).toBeCloseTo(1.35, 1)
    expect(e10.atkScale).toBe(3.0)
    expect(e3.atkScale).toBeUndefined()
  })
})

describe('チェックポイント開始 (2026-09-01 デバッグ機能)', () => {
  it('幕2から代表デッキ+レリックで開始し、B型ボーナス・難易度・幕スケールが効く', () => {
    const run = createDebugCheckpointRun(7, 'set-confirm', 'leader_green', {
      act: 2,
      deckId: 'run_basic',
      relicIds: ['relic_iron_heart', 'relic_growth_seed'],
      hpRatio: 0.6,
      gold: 200,
      difficulty: 5,
    })
    expect(run.act).toBe(2)
    expect(run.maxHp).toBe(88) // 鉄の心臓のB型 (+8) が適用される
    expect(run.hp).toBe(Math.round(88 * 0.6))
    expect(run.relics).toEqual(['relic_iron_heart', 'relic_growth_seed'])
    expect(run.relicQueue).not.toContain('relic_iron_heart') // 候補列から除かれる = 再提示されない
    expect(run.gold).toBe(200)
    expect(run.difficulty).toBe(5)
    expect(run.battlesWon).toBe(10)
    // 最初の戦闘は幕2のプール・幕2の深度スケール+難易度倍率
    const r = intoFirstBattle(run)
    expect(tierFor(2, 0)).toContain(currentNode(r)!.encounterId)
    expect(r.combat!.enemies[0].atkScale).toBeCloseTo(1.15 * 1.35) // 幕2打点+15% × 難易度5
  })
})

describe('査定パス (2026-09-02 段6人間プレイの指摘)', () => {
  it('年輪の大樹の鍛えるはコスト-1 (2E→1E)。成長エンジンの正しい伸び方', () => {
    const inst = { uid: 't', def: getCardDef('green_perm_growth_tree') }
    const up = upgradeCard(inst)
    expect(up.def.cost).toBe(1)
    expect(up.def.effects).toEqual(getCardDef('green_perm_growth_tree').effects) // 量は据え置き
  })

  it('毒針の囮は返し5+急所1 (帯割れの是正・上昇)', () => {
    const def = getCardDef('green_decoy_needle')
    expect(def.effects).toEqual([
      { trigger: 'onAttacked', effect: 'counter', amount: 5 },
      { trigger: 'onAttacked', effect: 'exposeEnemy', amount: 1 },
    ])
  })
})
