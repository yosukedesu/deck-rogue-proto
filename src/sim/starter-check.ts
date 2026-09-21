// 初期デッキの単発戦闘の物差し (2026-09-21 白の初期デッキの見直し): 幕1 Weak帯+本帯の敵に対する
// 勝率・平均ターン・T1 の与ダメ・被ダメ (素の敵値 = 幕スケール無し)。
// 使い方: npx tsx src/sim/starter-check.ts <leaderId> <deckId|cards:<id,...>> [対戦数] [seed]
import { applyCommand, createInitialState } from '../engine/state.ts'
import { chooseCommand } from './run.ts'
import { encounterPoolsForParity } from '../engine/map.ts'
import type { GameState } from '../engine/types.ts'

const leaderId = process.argv[2] ?? 'leader_white'
const deckArg = process.argv[3] ?? 'run_basic_white'
const n = Number(process.argv[4] ?? 20)
const seed0 = Number(process.argv[5] ?? 1)
const enemies = [...encounterPoolsForParity.WEAK_POOLS[0], ...encounterPoolsForParity.ACT_POOLS[0]]
let wins = 0, total = 0, turnsSum = 0, t1dmgSum = 0, lostHpSum = 0, unplayedSum = 0, handSum = 0
const perEnemy: Record<string, { w: number; n: number; t: number }> = {}
for (const enemyId of enemies) {
  for (let i = 0; i < n; i++) {
    const seed = (seed0 + i * 7919) >>> 0
    const start: any = { type: 'StartCombat', seed, enemyId, leaderId }
    if (deckArg.startsWith('cards:')) start.cardIds = deckArg.slice(6).split(',')
    else start.deckId = deckArg
    let s: GameState = applyCommand(createInitialState(seed, 'set-confirm'), start)
    let actions = 0
    while (s.phase !== 'won' && s.phase !== 'lost' && s.turn <= 50 && ++actions < 3000) s = applyCommand(s, chooseCommand(s))
    const won = s.phase === 'won'
    total++; if (won) wins++
    turnsSum += Math.min(s.turn, 50)
    // T1 の与ダメ = ターン1の DamageDealt (敵への) の合計
    let t = 0, t1 = 0
    for (const e of s.eventLog as any[]) {
      if (e.type === 'TurnStarted') t = e.turn
      if (e.type === 'DamageDealt' && t === 1 && e.enemyIndex !== undefined && e.enemyIndex >= 0) t1 += e.amount ?? 0
      if (e.type === 'TurnEnded' && Array.isArray(e.unplayed)) { unplayedSum += e.unplayed.length; handSum += 5 }
    }
    t1dmgSum += t1
    lostHpSum += (s.player.maxHp - s.player.hp)
    const pe = (perEnemy[enemyId] ??= { w: 0, n: 0, t: 0 }); pe.n++; if (won) pe.w++; pe.t += Math.min(s.turn, 50)
  }
}
console.log(`${leaderId} ${deckArg}: 勝率 ${(wins / total * 100).toFixed(1)}% 平均T ${(turnsSum / total).toFixed(2)} T1与ダメ ${(t1dmgSum / total).toFixed(1)} 失ったHP ${(lostHpSum / total).toFixed(1)} 未使用札/T ${(unplayedSum / Math.max(1, handSum) * 5).toFixed(2)}`)
const weak = Object.entries(perEnemy).filter(([id]) => id === id && (perEnemy[id].w / perEnemy[id].n) < 0.6).map(([id, v]) => `${id} ${(v.w / v.n * 100).toFixed(0)}%/${(v.t / v.n).toFixed(1)}T`)
if (weak.length) console.log('  苦手:', weak.join(' '))
