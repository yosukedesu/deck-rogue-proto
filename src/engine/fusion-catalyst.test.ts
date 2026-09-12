// 合成の触媒 (2026-09-12 ユーザー案「素材にするとリターンが大きい札」): 軽くなる/反復/保持の3種と、提示1回に触媒1枚の供給規則
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { fuseCards, fusionNotes } from './fusion.ts'
import { startCombatWithOptions } from './combat.ts'
import { applyCommand } from './state.ts'
import { createRun } from './run.ts'
import { withHand } from './test-helpers.ts'
import type { CardInstance, GameState } from './types.ts'

const ci = (id: string): CardInstance => ({ uid: `t_${id}`, def: getCardDef(id) })

describe('合成の触媒', () => {
  it('軽石の盾 (軽くなる触媒): 打撃と合成すると 1+1−1=1 からさらに−1 で 0E。0E規約に触れない (補充なし) ので消滅は付かない', () => {
    const r = fuseCards(ci('green_strike'), ci('green_catalyst_light'))
    expect(r.cost).toBe(0)
    expect(r.exhaust).toBeUndefined()
    expect(r.fusionCatalyst).toBeUndefined() // 触媒の印は結果に残らない
    expect(r.effects.map((e) => e.effect).sort()).toEqual(['dealDamage', 'gainBlock'])
    expect(fusionNotes(ci('green_strike'), ci('green_catalyst_light')).some((n) => n.includes('軽くなる触媒'))).toBe(true)
  })

  it('軽くなる触媒でも 0E+補充は消滅が付く (歯止めは不変)', () => {
    const r = fuseCards(ci('green_dappled_light'), ci('green_catalyst_light')) // 木漏れ日=ブロック7+1ドロー
    expect(r.cost).toBe(0)
    expect(r.exhaust).toBe(true)
  })

  it('谺の種 (反復の触媒): 結果が反復内蔵になり、プレイ時の効果を2回解決する。置物なら誘発ごとに2回', () => {
    const r = fuseCards(ci('green_strike'), ci('green_catalyst_echo'))
    expect(r.echo).toBe(true)
    let s: GameState = startCombatWithOptions(7, 'set-confirm', 'enemy_probe', { deck: [ci('green_strike')] })
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 999, maxHp: 999, block: 0 })) }
    s = { ...withHand(s, []), player: { ...s.player, hand: [{ uid: 'f', def: r }], energy: 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'f' })
    expect(s.enemies[0].hp).toBe(999 - 6 * 2 - 4 * 2)
    // 置物: 年輪の大樹 (毎T成長+1) × 谺の種 → 毎ターン成長+1 が2回
    const perm = allCards.find((c) => c.color === 'green' && c.type === 'permanent' && c.effects.some((e) => e.trigger === 'onTurnStart' && e.effect === 'addGrowth'))!
    const p = fuseCards(ci(perm.id), ci('green_catalyst_echo'))
    expect(p.type).toBe('permanent')
    expect(p.echo).toBe(true)
    // リアクション化では反復は付かない
    const trap = fuseCards(ci('green_reaction_thorns'), ci('green_catalyst_echo'))
    expect(trap.type).toBe('reaction')
    expect(trap.echo).toBeUndefined()
  })

  it('根付きの盾 (保持の触媒): 結果が保持を持つ (置物には付かない)', () => {
    const r = fuseCards(ci('green_fang'), ci('green_catalyst_root'))
    expect(r.retain).toBe(true)
    const perm = allCards.find((c) => c.color === 'green' && c.type === 'permanent')!
    expect(fuseCards(ci(perm.id), ci('green_catalyst_root')).retain).toBeUndefined()
  })

  it('触媒同士も合成できる (両方の恩恵が乗る)', () => {
    const r = fuseCards(ci('green_catalyst_light'), ci('green_catalyst_echo'))
    expect(r.cost).toBe(0)
    expect(r.echo).toBe(true)
  })

  it('供給: 1回の提示に触媒は1枚まで (200シードのランで機械確認)', () => {
    let violations = 0
    let offers = 0
    for (let seed = 1; seed <= 200; seed++) {
      const run = createRun(seed, 'set-confirm')
      // 報酬ロールは戦闘勝利後に走るので、rewardPool と同じ抽選を直接叩く代わりにチェックポイントから1戦勝つのは重い。
      // ここでは rollRewards を非公開のまま、報酬フェーズを持つ createRun の派生 (SkipReward 経由) を避け、私的関数を通さずに
      // 「触媒が2枚並ぶ提示が無い」ことを rewardOptions の実物で見る = 戦闘に入って即勝利させる
      let r = run
      for (let guard = 0; guard < 40 && r.phase !== 'reward'; guard++) {
        if (r.phase === 'map') {
          const cands = r.map[r.row + 1].map((_, c) => c).filter((c) => (r.row < 0 ? true : r.map[r.row][r.col].next.includes(c)))
          r = { ...r }
          r = applyRunCommandSafe(r, cands)
        } else if (r.phase === 'combat' && r.combat) {
          const c = r.combat
          r = { ...r, phase: 'reward', rewardOptions: null }
          r = rollOnce({ ...r, combat: { ...c, phase: 'won' } })
        } else break
      }
      if (r.phase !== 'reward' || !r.rewardOptions) continue
      offers++
      const n = r.rewardOptions.filter((id) => getCardDef(id).fusionCatalyst !== undefined).length
      if (n > 1) violations++
    }
    expect(offers).toBeGreaterThan(50)
    expect(violations).toBe(0)
  })
})

import { applyRunCommand, type RunState } from './run.ts'
function applyRunCommandSafe(r: RunState, cands: readonly number[]): RunState {
  for (const c of cands) {
    try { return applyRunCommand(r, { type: 'ChooseNode', col: c }) } catch { /* 次の候補 */ }
  }
  return r
}
/** 勝利直後の報酬ロールを SkipRelic/Combat を経ずに得る: 決着済みの戦闘を Combat コマンド経由で流し込む */
function rollOnce(r: RunState): RunState {
  const c = r.combat!
  const surgical: GameState = { ...c, phase: 'player-turn', enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
  const hand = { uid: 't0_green_sweep', def: getCardDef('green_sweep') }
  const s2: GameState = { ...surgical, player: { ...surgical.player, hand: [hand], energy: 9 }, enemies: surgical.enemies.map((e) => ({ ...e, intent: { kind: 'defend', shownMin: 0, shownMax: 0, actual: 0 } })) }
  let out = applyRunCommand({ ...r, phase: 'combat', combat: s2 }, { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } })
  if (out.phase === 'relic-reward') out = applyRunCommand(out, { type: 'SkipRelic' })
  return out
}
