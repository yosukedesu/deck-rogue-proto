// 潜伏 (Burrowed) と因縁 (Nemesis) — 2026-09-03 本家StS2 敵専用ギミックの移植 (ユーザー裁定「ok」)。
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { dealDamageToEnemy, isIntangibleTurn } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('潜伏 (burrow)', () => {
  it('殻が残る間はHPにダメージが通らず超過は捨てられる。貫通は通る', () => {
    const s0 = freshCombat('set-confirm', 'enemy_rock_beetle', 5)
    const e0 = s0.enemies[0]
    expect(e0.burrowActive).toBe(true)
    expect(e0.block).toBe(12)
    // 20ダメ (非貫通): 殻12を割り、超過8は捨てる = HPは減らない
    const s1 = dealDamageToEnemy(s0, 0, 20)
    expect(s1.enemies[0].hp).toBe(e0.hp)
    expect(s1.enemies[0].block).toBe(0)
    expect(s1.enemies[0].burrowActive).toBe(false)
    expect(s1.eventLog.some((ev) => ev.type === 'BurrowBroken')).toBe(true)
    // 貫通10: 殻は残りHPが減る
    const s2 = dealDamageToEnemy(s0, 0, 10, true)
    expect(s2.enemies[0].hp).toBe(e0.hp - 10)
    expect(s2.enemies[0].block).toBe(12)
    expect(s2.enemies[0].burrowActive).toBe(true)
  })
  it('自ターン中に粉砕で殻が割れると、その場で意図が噛みつきに差し替わる', () => {
    let s = freshCombat('set-confirm', 'enemy_rock_beetle', 5)
    s = withHand(s, ['green_vine_wedge']) // 蔦の楔: 粉砕+5
    const uid = s.player.hand[0].uid
    s = applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0 })
    const e = s.enemies[0]
    expect(e.burrowActive).toBe(false)
    expect(e.biteNext).toBe(false)
    const bite = getEnemyDef('enemy_rock_beetle').moves.find((m) => m.id === 'bite')!
    expect(e.intent?.kind).toBe('attack')
    expect(e.intent!.shownMin).toBeGreaterThanOrEqual(bite.min ?? 0)
  })
})

describe('潜伏の殻はターンをまたいで残る (2026-09-04 Opusラン O のバグ修正)', () => {
  it('敵フェーズ開始のブロック失効に巻き込まれず、割れるまで殻が残る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_rock_beetle', 5), [])
    expect(s.enemies[0].block).toBe(12)
    s = applyCommand(s, { type: 'EndTurn' })
    // 殻12は残り、甲虫自身の防御 (攻防一体) が上に積まれる
    expect(s.enemies[0].block).toBeGreaterThanOrEqual(12)
    expect(s.enemies[0].burrowActive).toBe(true)
    // 殻を超える非貫通ダメージで割れる (超過は捨てる=HPは減らない)
    const hp0 = s.enemies[0].hp
    const t = dealDamageToEnemy(s, 0, s.enemies[0].block + 5)
    expect(t.enemies[0].burrowActive).toBe(false)
    expect(t.enemies[0].hp).toBe(hp0)
  })
})

describe('因縁 (nemesis)', () => {
  it('奇数ターンは1ヒットのHP損失が1に固定され、偶数ターンは普通に通る', () => {
    let s = freshCombat('set-confirm', 'enemy_nemesis_wraith', 7)
    expect(isIntangibleTurn(s)).toBe(true) // T1
    const hp0 = s.enemies[0].hp
    s = dealDamageToEnemy(s, 0, 20, true)
    expect(s.enemies[0].hp).toBe(hp0 - 1)
    expect(s.eventLog.some((ev) => ev.type === 'DamageDealt' && ev.source === 'player' && ev.nemesisCut === 19)).toBe(true)
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.turn).toBe(2)
    expect(isIntangibleTurn(s)).toBe(false)
    const hp1 = s.enemies[0].hp
    s = dealDamageToEnemy(s, 0, 20, true)
    expect(s.enemies[0].hp).toBe(hp1 - 20)
  })
  it('延焼は無形ターンでも通る (装甲と同じ裁定)', () => {
    let s = freshCombat('set-confirm', 'enemy_nemesis_wraith', 7)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 10 })) }
    const hp0 = s.enemies[0].hp
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(hp0 - s.enemies[0].hp).toBeGreaterThanOrEqual(10)
  })
})
