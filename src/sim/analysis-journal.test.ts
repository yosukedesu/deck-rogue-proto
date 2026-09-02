// ジャーナル → 戦闘行の復元 (2026-09-03 npm run analyze の CLI/Opusラン対応)。
// ボット (sim/run.ts chooseCommand) で最初の戦闘を1つ決着させたジャーナルから、
// UI (App.tsx) と同じ判定で行が1本だけ復元され、計測が実際の combat と一致することを固定する。
import { describe, expect, it } from 'vitest'
import { battleRowsFromJournal } from '../engine/analysis.ts'
import { applyRunCommand, replayInitialRun, type RunCommand, type RunJournal } from '../engine/run.ts'
import { chooseCommand } from './run.ts'

describe('battleRowsFromJournal', () => {
  it('ボットで1戦決着させたジャーナルから戦闘行が1本復元され、ターン数・結果が combat と一致する', () => {
    const origin = { kind: 'run' as const, seed: 11, leaderId: 'leader_green' }
    const commands: RunCommand[] = []
    let s = replayInitialRun(origin)
    const step = (c: RunCommand) => { s = applyRunCommand(s, c); commands.push(c) }
    step({ type: 'ChooseNode', col: 0 }) // 行0は必ず戦闘 (本家 floor1)
    expect(s.combat).not.toBeNull()
    for (let i = 0; i < 400 && s.combat && s.combat.phase !== 'won' && s.combat.phase !== 'lost'; i++) {
      step({ type: 'Combat', command: chooseCommand(s.combat) })
    }
    const combat = s.combat!
    expect(combat.phase === 'won' || combat.phase === 'lost').toBe(true)
    const journal: RunJournal = { origin, commands }
    const { rows, error } = battleRowsFromJournal(journal)
    expect(error).toBeNull()
    expect(rows).toHaveLength(1)
    expect(rows[0]).toMatchObject({ battleNo: 1, act: 1, boss: false, elite: false, result: combat.phase, hpAfter: combat.player.hp })
    expect(rows[0].metrics?.turns).toBe(combat.turn)
    expect(rows[0].metrics!.perTurn.length).toBe(combat.turn)
    // 決着後にランを進めても (報酬スキップ) 行は増えない
    const after = battleRowsFromJournal({ origin, commands: [...commands, { type: 'SkipReward' }] })
    expect(after.rows).toHaveLength(1)
  })
})
