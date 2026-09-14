// 行動グラフ第2段のデータ書き直し (2026-09-14 ユーザー裁定: 直訳2体・3連禁止・役割分化・召喚者) の機械固定。
import { describe, expect, it } from 'vitest'
import { getEnemyDef, resolveEncounter } from './content.ts'
import { tierFor } from './map.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** 敵0体目の宣言した技を turns ターンぶん集める (HPは無限扱い) */
function declared(id: string, seed: number, turns: number, mutate?: (s: GameState, t: number) => GameState): string[] {
  let s: GameState = freshCombat('set-confirm', id, seed)
  s = { ...s, player: { ...s.player, hp: 9999, maxHp: 9999 } }
  const out: string[] = []
  for (let t = 0; t < turns && s.phase === 'player-turn'; t++) {
    out.push(s.enemies[0].intentMoveId ?? '?')
    if (mutate) s = mutate(s, t)
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
  }
  return out
}

describe('直訳2体 (本家 Chomper / FrogKnight)', () => {
  it('金切り顎: 挟み→金切り→守りの3拍の後は乱択 (挟みは2連まで・金切りは連続不可)', () => {
    const def = getEnemyDef('enemy_chomper')
    expect(def.interrupts).toBeUndefined()
    for (const seed of [1, 2, 3, 4, 5, 6]) {
      const moves = declared('enemy_chomper', seed, 14)
      expect(moves.slice(0, 3)).toEqual(['clamp', 'screech', 'guard'])
      expect(moves.slice(3).every((m) => m === 'clamp' || m === 'screech')).toBe(true)
      expect(moves.join(' ')).not.toMatch(/clamp clamp clamp/)
      expect(moves.join(' ')).not.toMatch(/screech screech/)
    }
  })

  it('蛙の騎士: 3拍+構えを回し、HP半分未満で一度きりの突進 (28〜30) を挟み、二度目は来ない', () => {
    const def = getEnemyDef('enemy_frog_knight')
    expect(def.interrupts).toBeUndefined()
    const full = declared('enemy_frog_knight', 42, 8)
    expect(full).toEqual(['tongue_lash', 'strike_down', 'for_the_queen', 'guard', 'tongue_lash', 'strike_down', 'for_the_queen', 'guard'])
    // 4拍目 (構え) の後に半分を割っていると突進。その後は舌からの通常ローテに戻り、再び半分以下でも突進しない
    const low = declared('enemy_frog_knight', 42, 12, (s, t) => (t === 3 ? { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(e.maxHp * 0.3) })) } : s))
    expect(low.slice(0, 5)).toEqual(['tongue_lash', 'strike_down', 'for_the_queen', 'guard', 'beetle_charge'])
    expect(low.slice(5, 9)).toEqual(['tongue_lash', 'strike_down', 'for_the_queen', 'guard'])
    expect(low.filter((m) => m === 'beetle_charge')).toHaveLength(1)
  })
})

describe('3連禁止 (maxRepeat 2 = StS1 lastTwoMoves)', () => {
  it('罠壊し smash・道化 wild_swing・大苔スライム slam・苔の主 slam・石殻 shell_bash の腕に maxRepeat 2', () => {
    for (const [id, moveId] of [['enemy_set_breaker', 'smash'], ['enemy_joker', 'wild_swing'], ['enemy_big_slime', 'slam'], ['enemy_moss', 'slam'], ['enemy_shell_guard', 'shell_bash']] as const) {
      const def = getEnemyDef(id)
      const arms = Object.values(def.nodes).flatMap((n) => n.random ?? []).filter((a) => def.nodes[a.to]?.move === moveId)
      expect(arms.length, id).toBeGreaterThan(0)
      expect(arms.every((a) => a.maxRepeat === 2), id).toBe(true)
    }
  })

  it('うねる獣・大苔スライム: 同じ技が3連続しない (実挙動)', () => {
    for (const seed of [1, 2, 3, 4, 5, 6, 7, 8]) {
      const moves = declared('enemy_big_slime', seed, 12)
      expect(moves.join(' '), `seed ${seed}`).not.toMatch(/slam slam slam/)
    }
  })
})

describe('スロット役割分化 (startBySlot)', () => {
  it('噛みつく巻物・三巻/四巻: 1体目は噛み・2体目は咀嚼・3体目は歯を増やす・4体目は乱択から', () => {
    const def = getEnemyDef('enemy_biting_scroll')
    expect(def.startBySlot).toEqual(['chomp', 'chew', 'more_teeth', 'rand'])
    expect(resolveEncounter('enc_biting_scrolls_trio').every((m) => m.start === undefined)).toBe(true)
    const s = freshCombat('set-confirm', 'enc_biting_scrolls_trio', 42)
    expect(s.enemies.map((e) => e.intentMoveId)).toEqual(['chomp', 'chew', 'more_teeth'])
    const q = freshCombat('set-confirm', 'enc_biting_scrolls_quad', 42)
    expect(q.enemies.slice(0, 3).map((e) => e.intentMoveId)).toEqual(['chomp', 'chew', 'more_teeth'])
    expect(['chomp', 'chew', 'more_teeth', 'guard']).toContain(q.enemies[3].intentMoveId)
  })

  it('小泥の群れ: 1体目は体当たり・2体目は泥 (負傷)・3体目は守り。混成 (泥まとうものと小泥) は体当たり/泥で守り始まりを作らない', () => {
    expect(getEnemyDef('enemy_mudling').startBySlot).toEqual(['bump', 'slop', 'guard', 'rand'])
    const s = freshCombat('set-confirm', 'enc_mudling_trio', 42)
    expect(s.enemies.map((e) => e.intentMoveId)).toEqual(['bump', 'slop', 'guard'])
    const mixed = freshCombat('set-confirm', 'enc_mud_mudlings', 42)
    expect(mixed.enemies.slice(1).map((e) => e.intentMoveId)).toEqual(['bump', 'slop'])
  })
})

describe('召喚者: 苔の産み手 (幕2・本家 Fabricator/LouseProgenitor 型)', () => {
  it('味方が3体未満なら産み、産んだ苔スライムは即座に宣言して同じ敵フェーズから動く。3体になったら殴りに転じる', () => {
    const def = getEnemyDef('enemy_moss_spawner')
    expect(def.archetype).toBe('summoner')
    expect(def.moves.find((m) => m.kind === 'summon')?.summon).toEqual({ enemyId: 'enemy_moss_slime', count: 1 })
    let s = freshCombat('set-confirm', 'enc_moss_spawner_slime', 42)
    expect(s.enemies).toHaveLength(2)
    expect(s.enemies[0].intent).toEqual({ kind: 'summon', actual: 1 })
    s = { ...s, player: { ...s.player, hp: 9999, maxHp: 9999 } }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies).toHaveLength(3)
    expect(s.enemies[2].enemyId).toBe('enemy_moss_slime')
    expect(s.eventLog.some((e) => e.type === 'EnemySummoned' && e.count === 1)).toBe(true)
    expect(s.enemies[0].intentMoveId).toBe('lash') // 産んだ後は殴り
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].intentMoveId).toBe('guard')
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].intentMoveId).toBe('lash') // 3体いるので産まない
  })

  it('幕2の本帯プールに「苔の産み手と苔スライム」がある', () => {
    expect(tierFor(2, 5)).toContain('enc_moss_spawner_slime')
  })
})
