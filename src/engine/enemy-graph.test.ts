// 敵の行動グラフ (2026-09-14 本家式の状態機械。確定済みルール表「敵の行動グラフ」) の機械固定。
// 第1段=等価移行: 旧形からの変換が同じ挙動になること・全84体のグラフが整合すること・
// 新しく書ける形 (固定の骨組みに決まった場所だけ揺らぎ・条件の節・割り込み) が動くこと。
import { afterEach, describe, expect, it } from 'vitest'
import { allEncounters, allEnemies, applyDebugOverrides, clearDebugOverrides, getCardDef, getEnemyDef } from './content.ts'
import { advanceCursor, describeGraph, firstMoveOf, graphFromLegacy, validateEnemyGraph } from './enemyGraph.ts'
import type { LegacyEnemyDef } from './enemyGraph.ts'
import { applyCommand } from './state.ts'
import { freshCombat, setAndArm, withHand } from './test-helpers.ts'
import type { EnemyDef, GameState } from './types.ts'

const base = { name: 'テスト', archetype: 'brute', maxHp: 200 } as const

/** N ターン回して宣言された技 id を集める (敵0体目) */
function declaredMoves(id: string, seed: number, turns: number): string[] {
  let s: GameState = freshCombat('set-confirm', id, seed)
  const out: string[] = []
  for (let t = 0; t < turns && s.phase === 'player-turn'; t++) {
    out.push(s.enemies[0].lastMoves?.[0] ?? '?')
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
  }
  return out
}

describe('データの整合 (全84体)', () => {
  it('全敵のグラフが検査を通り、start から技に着地する', () => {
    const errs: string[] = []
    for (const e of allEnemies) {
      for (const err of validateEnemyGraph(e)) errs.push(`${e.id}: ${err}`)
      if (firstMoveOf(e, e.start) === undefined) errs.push(`${e.id}: start から技に着地しない`)
    }
    expect(errs).toEqual([])
  })

  it('旧形のキー (sequence/opener/weight/movesBelowHalf/wakeOnDamage/setAlt) はデータに残っていない', () => {
    const legacyKeys = ['sequence', 'sequenceLoopFrom', 'opener', 'movesBelowHalf', 'sequenceBelowHalf', 'movesWhenAlone', 'sequenceWhenAlone', 'phaseAfterUses', 'wakeOnDamage']
    const offenders = allEnemies.flatMap((e) => legacyKeys.filter((k) => k in e).map((k) => `${e.id}.${k}`))
    expect(offenders).toEqual([])
    expect(allEnemies.some((e) => e.moves.some((m) => 'weight' in m || 'noRepeat' in m || 'once' in m || 'setAlt' in m))).toBe(false)
    expect(allEncounters.some((enc) => enc.members.some((m) => 'patternOffset' in m))).toBe(false)
  })

  it('編成の位相ずらしは member.start (探り屋の二人組: 2体目は構えから始まる)', () => {
    const pair = allEncounters.find((e) => e.id === 'enc_probe_pair')!
    expect(pair.members[1].start).toBe('guard')
    const s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    expect([s.enemies[0].lastMoves?.[0], s.enemies[1].lastMoves?.[0]]).toEqual(['poke', 'guard'])
  })

  it('分裂体の位相ずらしは子の startBySlot (苔スライム: 1体目は体当たり・2体目は泥から)', () => {
    const slime = getEnemyDef('enemy_moss_slime')
    expect(slime.startBySlot).toEqual(['bump', 'ooze'])
    let s = freshCombat('set-confirm', 'enemy_big_slime', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1 })), player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid, targetIndex: 0 })
    const kids = s.enemies.filter((e) => e.enemyId === 'enemy_moss_slime')
    expect(kids.map((k) => k.lastMoves?.[0])).toEqual(['bump', 'ooze'])
  })
})

describe('旧形からの機械変換 (graphFromLegacy)', () => {
  it('固定ローテ → 技の節の鎖 (同じ技の2回目は _2)。loopFrom は最後の next に出る', () => {
    const g = graphFromLegacy({ ...base, id: 't', moves: [{ id: 'a', kind: 'attack', min: 1, max: 1 }, { id: 'b', kind: 'defend', min: 1, max: 1 }], sequence: ['a', 'b', 'a'], sequenceLoopFrom: 1 })
    expect(g.start).toBe('a')
    expect(g.nodes).toEqual({ a: { move: 'a', next: 'b' }, b: { move: 'b', next: 'a_2' }, a_2: { move: 'a', next: 'b' } })
  })

  it('重み表 → 乱択の節 (腕は表の順・重み・noRepeat/once を引き継ぐ)。opener は start', () => {
    const g = graphFromLegacy({ ...base, id: 't', opener: 'b', moves: [{ id: 'a', kind: 'attack', min: 1, max: 1, weight: 3, noRepeat: true }, { id: 'b', kind: 'defend', min: 1, max: 1, weight: 1, once: true }] })
    expect(g.start).toBe('b')
    expect(g.nodes.rand).toEqual({ random: [{ to: 'a', weight: 3, noRepeat: true }, { to: 'b', weight: 1, once: true }] })
    expect(g.nodes.b).toEqual({ move: 'b', next: 'rand' })
  })

  it('HP半分・単独時・被弾覚醒は割り込みに、回数カウンタは条件の節になる', () => {
    const g = graphFromLegacy({
      ...base, id: 't',
      moves: [{ id: 'a', kind: 'attack', min: 1, max: 1 }, { id: 'x', kind: 'attack', min: 9, max: 9 }, { id: 'y', kind: 'buff', min: 1, max: 1 }],
      sequence: ['a', 'a', 'a'], sequenceBelowHalf: ['x'], movesWhenAlone: [{ id: 'y', kind: 'buff', min: 1, max: 1, weight: 1 }],
      wakeOnDamage: { damage: 5, resumeAt: 2 }, phaseAfterUses: { moveId: 'a', uses: 2, sequence: ['x'] },
    })
    expect(g.interrupts).toEqual([
      { on: 'hpBelowHalf', goto: 'half_x' },
      { on: 'alone', goto: 'alone_rand' },
      { on: 'damageTaken', amount: 5, from: ['a', 'a_2'], goto: 'a_3' },
    ])
    expect(g.nodes.a).toEqual({ move: 'a', next: 'a_check' })
    expect(g.nodes.a_check).toEqual({ if: { usesAtLeast: { move: 'a', count: 2 } }, then: 'p2_x', else: 'a_2' })
  })
})

describe('グラフの評価 (新しく書ける形)', () => {
  afterEach(() => clearDebugOverrides())

  it('固定の骨組みに決まった場所だけ揺らぎ: 叩く→(乱択: 噛み/咀嚼)→叩く… (本家 Chomper 型)', () => {
    const def: EnemyDef = {
      ...base, id: 'test_mixed',
      moves: [{ id: 'chomp', kind: 'attack', min: 4, max: 4 }, { id: 'bite', kind: 'attack', min: 6, max: 6 }, { id: 'chew', kind: 'defend', min: 5, max: 5 }],
      start: 'chomp',
      nodes: {
        chomp: { move: 'chomp', next: 'r' },
        r: { random: [{ to: 'bite', weight: 1 }, { to: 'chew', weight: 1 }] },
        bite: { move: 'bite', next: 'chomp' },
        chew: { move: 'chew', next: 'chomp' },
      },
    }
    applyDebugOverrides({ enemies: [def] })
    expect(validateEnemyGraph(def)).toEqual([])
    const moves = declaredMoves('test_mixed', 3, 8)
    // 偶数拍は必ず chomp・奇数拍は bite か chew
    moves.forEach((m, i) => (i % 2 === 0 ? expect(m).toBe('chomp') : expect(['bite', 'chew']).toContain(m)))
  })

  it('maxRepeat: 同じ技は N 連続まで (StS1 lastTwoMoves 相当)。noRepeat は 2 連禁止', () => {
    const def: EnemyDef = {
      ...base, id: 'test_streak',
      moves: [{ id: 'a', kind: 'attack', min: 1, max: 1 }, { id: 'b', kind: 'defend', min: 1, max: 1 }],
      start: 'r',
      nodes: { r: { random: [{ to: 'a', weight: 99, maxRepeat: 2 }, { to: 'b', weight: 1 }] }, a: { move: 'a', next: 'r' }, b: { move: 'b', next: 'r' } },
    }
    applyDebugOverrides({ enemies: [def] })
    for (const seed of [1, 2, 3, 4, 5]) {
      const moves = declaredMoves('test_streak', seed, 12)
      expect(moves.join(' ')).not.toMatch(/a a a/)
      expect(moves.filter((m) => m === 'a').length).toBeGreaterThan(moves.length / 2) // 重み99なので a が主
    }
  })

  it('条件の節: ターンの偶奇で分岐 (本家 HauntedShip 型)', () => {
    const def: EnemyDef = {
      ...base, id: 'test_parity',
      moves: [{ id: 'odd', kind: 'attack', min: 1, max: 1 }, { id: 'even', kind: 'defend', min: 1, max: 1 }],
      start: 'c',
      nodes: { c: { if: { turnParity: 'odd' }, then: 'odd', else: 'even' }, odd: { move: 'odd', next: 'c' }, even: { move: 'even', next: 'c' } },
    }
    applyDebugOverrides({ enemies: [def] })
    expect(declaredMoves('test_parity', 1, 4)).toEqual(['odd', 'even', 'odd', 'even'])
  })

  it('割り込み: HP半分は宣言時に1回だけ飛び、回復しても戻らない (一方通行)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(e.maxHp * 0.4) })) }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].firedInterrupts).toEqual([0])
    expect(s.enemies[0].lastMoves?.[0]).toBe('rage_flurry') // 列の先頭から (旧実装は patternIndex の持ち越しで何拍目からかが偶然だった)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: e.maxHp })) }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(['rage_flurry', 'war_roar']).toContain(s.enemies[0].lastMoves?.[0])
  })

  it('advanceCursor: k 回目の宣言の節 (鎖はループ込み・乱択で止まる)', () => {
    const probe = getEnemyDef('enemy_probe')
    expect([0, 1, 2, 3, 4, 5].map((k) => advanceCursor(probe, k))).toEqual(['poke', 'guard', 'poke_2', 'lunge', 'poke', 'guard'])
    const wide = getEnemyDef('enemy_wide_power')
    expect([0, 1, 2].map((k) => advanceCursor(wide, k))).toEqual(['surge', 'rand', 'rand'])
  })

  it('describeGraph: 図鑑向けの1行 (技の表記・乱択・条件の1段展開・割り込み。2026-09-14 Opus 3本「生IDで読めない」の処方)', () => {
    const raw = (m: string) => m
    expect(describeGraph(getEnemyDef('enemy_probe'), raw)[0]).toBe('poke→guard→poke→lunge→(pokeへ戻る)')
    expect(describeGraph(getEnemyDef('enemy_probe'))[0]).toBe('⚔️5〜7→🛡️7〜10+筋1→⚔️5〜7→⚔️12〜16→(⚔️5〜7へ戻る)')
    expect(describeGraph(getEnemyDef('enemy_wide_power'), raw)[0]).toBe('surge→乱択{surge 3/coil 1}')
    const brute = describeGraph(getEnemyDef('enemy_brute'), raw)
    expect(brute[1]).toBe('HP半分で→rage_flurry→rage_flurry→war_roar→(rage_flurryへ戻る)')
    const egg = describeGraph(getEnemyDef('enemy_elite_iron_egg'), raw)
    expect(egg[1]).toBe('累計20被弾で→awaken→tail→(繰り返し)')
    // 条件の節は両側を1段展開 (蛙の騎士: 一度きりの突進が見える)
    expect(describeGraph(getEnemyDef('enemy_frog_knight'), raw)[0]).toContain('(HP半分以下? (beetle_chargeを1回使った? (tongue_lashへ戻る) : beetle_charge→(tongue_lashへ戻る)) : (tongue_lashへ戻る))')
    // 召喚者は条件から始まり、両側の輪が見える
    expect(describeGraph(getEnemyDef('enemy_moss_spawner'))[0]).toBe('(味方が3体未満? 👶苔スライム×1→⚔️9〜12→🛡️6〜9+筋1→(条件へ戻る) : ⚔️9〜12→🛡️6〜9+筋1→(条件へ戻る))')
  })

  it('旧形の定義 (テスト・調整モード) は読込時に同じ変換を受ける', () => {
    const legacy: LegacyEnemyDef = { ...base, id: 'test_legacy', moves: [{ id: 'a', kind: 'attack', min: 2, max: 2, weight: 1 }], sequence: ['a', 'a'] }
    applyDebugOverrides({ enemies: [legacy] })
    const def = getEnemyDef('test_legacy')
    expect(def.start).toBe('a')
    expect(def.nodes.a_2).toEqual({ move: 'a', next: 'a' })
    expect(declaredMoves('test_legacy', 1, 3)).toEqual(['a', 'a', 'a'])
  })
})

describe('即時差し替え (2026-09-14 ユーザー裁定「原因限定で許す・既存のHP半分/被弾覚醒も即時」)', () => {
  it('オーガ: 自ターン中にHP半分を割ると、宣言済みの意図がその場で第2形態 (乱打) に変わり、取り消した技の回数は戻る', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    expect(s.enemies[0].intentMoveId).toBe('club')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(e.maxHp * 0.5) + 3 })), player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid, targetIndex: 0 })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(s.enemies[0].maxHp * 0.5)
    expect(s.enemies[0].intentMoveId).toBe('rage_flurry')
    expect(s.enemies[0].intent?.kind).toBe('attack')
    expect(s.enemies[0].intent?.hits).toBe(2)
    expect(s.enemies[0].moveUses?.club ?? 0).toBe(0) // 取り消した club は数えない
    expect(s.enemies[0].lastMoves?.[0]).toBe('rage_flurry')
    const ev = s.eventLog.find((e) => e.type === 'EnemyInterrupted')
    expect(ev && ev.type === 'EnemyInterrupted' ? [ev.trigger, ev.replaced] : null).toEqual(['hpBelowHalf', true])
  })

  it('敵フェーズ中に半分を割った (返し) 時はカーソルだけ飛び、意図は次の宣言から', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    // 返し10で半分を割る位置に。攻撃を受けて post 窓が開くよう意図を攻撃に
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(e.maxHp * 0.5) + 2, intent: { kind: 'attack' as const, actual: 9 } })) }
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_green_reaction_thorns' })
    const ev = s.eventLog.filter((e) => e.type === 'EnemyInterrupted')
    expect(ev.length).toBe(1)
    expect(ev[0].type === 'EnemyInterrupted' && ev[0].replaced).toBe(false)
    // 次の宣言 (自ターン開始) は第2形態
    expect(s.phase).toBe('player-turn')
    expect(s.enemies[0].intentMoveId).toBe('rage_flurry')
  })

  it('従士: 自ターン中に射手を倒すと、その場で弔い猛攻に差し替わる (Queen 式の随伴死亡)', () => {
    let s = freshCombat('set-confirm', 'enc_squire_archer', 42)
    expect(s.enemies[0].intentMoveId).toBe('shield_bash')
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 1 ? { ...e, hp: 1 } : e)), player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_sweep'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid })
    expect(s.enemies[1].hp).toBeLessThanOrEqual(0)
    expect(s.enemies[0].intentMoveId).toBe('avenging_rush')
    expect(s.enemies[0].intent?.kind).toBe('attack')
  })
})

describe('召喚 (2026-09-14 kind:summon。本家 Fabricator/Reptomancer 型)', () => {
  afterEach(() => clearDebugOverrides())

  const summoner: EnemyDef = {
    ...base, id: 'test_summoner', maxHp: 100,
    moves: [
      { id: 'call', kind: 'summon', summon: { enemyId: 'enemy_moss_slime', count: 2 } },
      { id: 'hit', kind: 'attack', min: 5, max: 5 },
    ],
    start: 'c',
    // 味方が3体未満なら召喚、満杯なら殴る (Fabricator 型の条件の節)
    nodes: { c: { if: { alliesFewerThan: 3 }, then: 'call', else: 'hit' }, call: { move: 'call', next: 'c' }, hit: { move: 'hit', next: 'c' } },
  }

  it('召喚の意図は出す体数。解決すると子が出て即座に宣言し、場が上限 (4体) なら出ない', () => {
    applyDebugOverrides({ enemies: [summoner] })
    let s = freshCombat('set-confirm', 'test_summoner', 42)
    expect(s.enemies[0].intent).toEqual({ kind: 'summon', actual: 2 })
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies).toHaveLength(3)
    expect(s.enemies.slice(1).every((e) => e.enemyId === 'enemy_moss_slime' && e.intent !== null)).toBe(true)
    expect(s.eventLog.some((e) => e.type === 'EnemySummoned' && e.count === 2)).toBe(true)
    // 3体いるので次は殴り (条件の節)
    expect(s.enemies[0].intent?.kind).toBe('attack')
    // 上限: 4体いる状態で召喚しても出ない
    let t: GameState = { ...s, enemies: [...s.enemies, { ...s.enemies[1] }], player: { ...s.player, hp: 999 } }
    t = { ...t, enemies: t.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'summon' as const, actual: 2 }, intentMoveId: 'call' } : e)) }
    t = withHand(t, [])
    t = applyCommand(t, { type: 'EndTurn' })
    expect(t.enemies.filter((e) => e.hp > 0)).toHaveLength(4)
    expect(t.eventLog.some((e) => e.type === 'EnemySummoned' && e.count === 0)).toBe(true)
  })

  it('召喚は打ち消せる', () => {
    applyDebugOverrides({ enemies: [summoner] })
    let s = withHand(freshCombat('set-confirm', 'test_summoner', 42), ['green_reaction_root_weave'])
    s = setAndArm(s, 't0_green_reaction_root_weave')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, intent: { kind: 'summon' as const, actual: 2 }, intentMoveId: 'call' })) }
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_green_reaction_root_weave' })
    expect(s.eventLog.some((e) => e.type === 'ActionNegated')).toBe(true)
    expect(s.enemies).toHaveLength(1)
  })
})

describe('敵フェーズ中に出現した敵はそのフェーズでは動かない (2026-09-14 Opus AB2: 召喚された子が同じフェーズに殴り被ダメ予測が嘘になった)', () => {
  afterEach(() => clearDebugOverrides())

  it('召喚された苔スライムは意図なしで現れ、次の自ターン開始で宣言する = 被ダメ予測は実被ダメと一致する', () => {
    applyDebugOverrides({
      enemies: [{
        ...base, id: 'test_summoner2', maxHp: 100,
        moves: [{ id: 'call', kind: 'summon', summon: { enemyId: 'enemy_moss_slime', count: 2 } }, { id: 'hit', kind: 'attack', min: 5, max: 5 }],
        start: 'call', nodes: { call: { move: 'call', next: 'hit' }, hit: { move: 'hit', next: 'hit' } },
      }],
    })
    let s = freshCombat('set-confirm', 'test_summoner2', 42)
    s = { ...s, player: { ...s.player, hp: 500, maxHp: 500, block: 0 } }
    const hpBefore = s.player.hp
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies).toHaveLength(3)
    expect(hpBefore - s.player.hp).toBe(0) // 召喚のターンは誰も殴らない (被ダメ予測0と一致)
    expect(s.enemies.slice(1).every((e) => e.intent !== null)).toBe(true) // 次の自ターン開始で宣言済み
    expect(s.eventLog.filter((e) => e.type === 'DamageDealt' && e.source === 'enemy')).toHaveLength(0)
  })
})

describe('Opus AB の挙動の疑い2件 (2026-09-14)', () => {
  it('鉄卵: 3拍目の眠り (カーソルは既に awaken) でも累計20で目覚め、その場で意図が awaken に変わる', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_iron_egg', 42)
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
    for (let t = 0; t < 2; t++) { s = withHand(s, []); s = applyCommand(s, { type: 'EndTurn' }) }
    expect(s.enemies[0].intentMoveId).toBe('sleep')
    expect(s.enemies[0].node).toBe('awaken') // カーソルは次の節へ進んでいる
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0, damageTakenTotal: 15 })), player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid, targetIndex: 0 })
    expect(s.enemies[0].intentMoveId).toBe('awaken')
    expect(s.enemies[0].intent?.kind).toBe('buff')
  })

  it('打ち消し: 窓の敵が行動前に倒れたら旗はその行動に使い切り、次の敵の行動には飛ばない (pre 窓の根の紡ぎ→成長→棘葉の全体で敵0が死ぬ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42), ['green_reaction_root_weave'])
    s = setAndArm(s, 't0_green_reaction_root_weave')
    // 棘葉の茂み (成長を得るたび敵全体に2) を場に・敵0 は HP2・両方とも攻撃の意図
    const thorn = { uid: 'perm_thorn', def: getCardDef('green_perm_thorn_leaves') }
    s = {
      ...s,
      player: { ...s.player, permanents: [...s.player.permanents, thorn], hp: 999, maxHp: 999 },
      enemies: s.enemies.map((e, i) => ({ ...e, block: 0, hp: i === 0 ? 2 : e.hp, intent: { kind: 'attack' as const, actual: 5 } })),
    }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_green_reaction_root_weave' })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0) // 棘葉で死んだ
    expect(s.eventLog.filter((e) => e.type === 'ActionNegated')).toHaveLength(0) // 死んだ敵の行動は無いので打ち消しは空振り
    expect(s.negateNextAction).toBe(false)
    // 敵1 は普通に殴ってくる
    expect(s.eventLog.some((e) => e.type === 'DamageDealt' && e.source === 'enemy' && e.enemyIndex === 1)).toBe(true)
  })
})

describe('筋力ライブ (2026-09-14 ユーザー裁定「本家どおり」): 宣言済みの攻撃の実値は筋力が動いた瞬間に引き直す', () => {
  it('応援役が先に動くと、同じフェーズの味方の攻撃がその場で強くなる', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42)
    s = { ...s, player: { ...s.player, hp: 500, maxHp: 500, block: 0 } }
    const str1 = s.enemies[1].strength
    s = {
      ...s,
      enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, intent: { kind: 'rally' as const, actual: 3 } } : { ...e, intent: { kind: 'attack' as const, actual: 5 + str1, base: 5 } })),
    }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    const hits = s.eventLog.filter((e) => e.type === 'DamageDealt' && e.source === 'enemy' && e.enemyIndex === 1)
    expect(hits.length).toBe(1)
    expect(hits[0].type === 'DamageDealt' ? hits[0].amount : 0).toBe(5 + str1 + 3) // 素5 + 筋力 + 応援3 (旧: 宣言時固定で応援は乗らない)
  })

  it('自ターン中の筋力上昇 (鬼軍曹のブロック反応) は宣言済みの実値にその場で乗る', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_sergeant', 42)
    s = { ...s, player: { ...s.player, energy: 9 } }
    const before = s.enemies[0].intent!
    expect(before.kind).toBe('attack')
    s = withHand(s, ['green_guard'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid })
    expect(s.enemies[0].strength).toBe(1)
    expect(s.enemies[0].intent!.actual).toBe(before.actual + 1)
    expect(s.enemies[0].intent!.base).toBe(before.base)
  })

  it('連携 (双牙の狼): 相方を倒した瞬間に宣言済みの実値が素へ戻る', () => {
    let s = freshCombat('set-confirm', 'enc_fang_twins', 42)
    const it0 = s.enemies[0].intent!
    expect(it0.kind).toBe('attack')
    expect(it0.actual).toBe(it0.base! + 2)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 1 ? { ...e, hp: 1 } : e)), player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid, targetIndex: 1 })
    expect(s.enemies[1].hp).toBeLessThanOrEqual(0)
    expect(s.enemies[0].intent!.actual).toBe(it0.base) // 連携+2 が落ちた
  })
})
