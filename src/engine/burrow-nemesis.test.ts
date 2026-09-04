// 潜伏 (Burrowed) と因縁 (Nemesis) — 2026-09-03 本家StS2 敵専用ギミックの移植 (ユーザー裁定「ok」)。
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { damageBreakdown, dealDamageToEnemy, isIntangibleTurn } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('潜伏 (burrow)', () => {
  it('殻が残る間はHPにダメージが通らず超過は捨てられる。貫通も殻に吸われる (2026-09-04 裁定A)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_rock_beetle', 5), [])
    const hp0 = s.enemies[0].hp
    expect(s.enemies[0].block).toBe(12)
    // 貫通10: 殻は土であってブロックではない = 殻が10減りHPは減らない
    let t = dealDamageToEnemy(s, 0, 10, true)
    expect(t.enemies[0].hp).toBe(hp0)
    expect(t.enemies[0].block).toBe(2)
    expect(t.enemies[0].burrowActive).toBe(true)
    // 20ダメ (非貫通): 殻2を割り、超過18は捨てる = HPは減らない
    t = dealDamageToEnemy(t, 0, 20)
    expect(t.enemies[0].hp).toBe(hp0)
    expect(t.enemies[0].burrowActive).toBe(false)
    // 割れた後は普通に通る (貫通も)
    t = dealDamageToEnemy(t, 0, 5, true)
    expect(t.enemies[0].hp).toBe(hp0 - 5)
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

describe('表示の内訳 (damageBreakdown) は潜伏・因縁を実処理と同じ手順で通す (2026-09-05 Opusラン Q: 表示10/実0)', () => {
  it('潜伏中: 貫通も非貫通も殻に吸われてHP損失0', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_rock_beetle', 5), [])
    expect(damageBreakdown(s, 0, 10, true)?.hpLoss).toBe(0)
    expect(damageBreakdown(s, 0, 20, false)?.hpLoss).toBe(0)
    expect(damageBreakdown(s, 0, 20, false)?.steps.some((st) => st.label.startsWith('潜伏の殻'))).toBe(true)
  })
  it('因縁の無形ターン: HP損失は1固定', () => {
    const s = freshCombat('set-confirm', 'enemy_nemesis_wraith', 7)
    expect(damageBreakdown(s, 0, 20, true)?.hpLoss).toBe(1)
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
    // T1の呪いの触れ (弱体1) を外して素の20で測る (2026-09-04 ローテ固定: 無形ターンは呪いの触れ)
    s = { ...s, player: { ...s.player, weak: 0 } }
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
