// sim/golden-driver.ts — ゴールデンマスター生成用の決定的なラン駆動 (Unity移植 P0.5。docs/unity-port.md §1)。
// ボット (chooseCommand) と単純なラン方針で1ランを最後まで進め、ジャーナル (origin+commands) と
// 各コマンド後の状態ハッシュ (engine/golden.ts) を残す。方針は「機能を踏む」ことを優先し (鍛える・合成・購入・イベント)、
// 不正な手は安全な既定 (休む・見送り・立ち去る) へ倒す。純ロジック (Node単体で動く)。
import { getEventDef, getCardDef } from '../engine/content.ts'
import { fuseBlockReason } from '../engine/fusion.ts'
import { runHash, runDigest } from '../engine/golden.ts'
import type { RunDigest } from '../engine/golden.ts'
import { applyRunCommand, canUpgradeCard, createRun, eventChoiceNeedsCard, nextChoices } from '../engine/run.ts'
import type { ReplayOrigin, RunCommand, RunState } from '../engine/run.ts'
import { chooseCommand } from './run.ts'

const ACTIVE = new Set(['combat', 'reward', 'map', 'campfire', 'workshop', 'shop', 'event', 'relic-reward'])

/** 現在のフェーズに対するボットの次の一手 (終了フェーズなら null)。候補は優先順に並べ、不正なら次へ倒す */
export function botRunCandidates(run: RunState): readonly RunCommand[] {
  switch (run.phase) {
    case 'combat':
      return run.combat ? [{ type: 'Combat', command: chooseCommand(run.combat) }] : []
    case 'map': {
      const cands = nextChoices(run)
      if (cands.length === 0) return []
      const typeOf = (c: number) => run.map[run.row + 1][c].type
      const wantCamp = run.hp < run.maxHp * 0.6
      const pick =
        (wantCamp ? cands.find((c) => typeOf(c) === 'campfire') : undefined) ??
        (!wantCamp ? cands.find((c) => typeOf(c) === 'elite') : undefined) ??
        cands.find((c) => typeOf(c) === 'battle' || typeOf(c) === 'boss') ??
        cands[0]
      return [{ type: 'ChooseNode', col: pick }]
    }
    case 'reward':
      return (run.rewardOptions?.length ?? 0) > 0 ? [{ type: 'PickReward', index: 0 }, { type: 'SkipReward' }] : [{ type: 'SkipReward' }]
    case 'relic-reward':
      return (run.relicOptions?.length ?? 0) > 0 ? [{ type: 'PickRelic', index: 0 }, { type: 'SkipRelic' }] : [{ type: 'SkipRelic' }]
    case 'campfire': {
      const up = run.deck.findIndex((c) => canUpgradeCard(c))
      return up >= 0 && run.hp >= run.maxHp * 0.6 ? [{ type: 'CampfireUpgrade', index: up }, { type: 'CampfireRest' }] : [{ type: 'CampfireRest' }]
    }
    case 'workshop': {
      for (let i = 0; i < run.deck.length; i++) {
        for (let j = i + 1; j < run.deck.length; j++) {
          if (fuseBlockReason(run.deck[i], run.deck[j]) === null) return [{ type: 'WorkshopFuse', indexA: i, indexB: j }, { type: 'WorkshopSkip' }]
        }
      }
      return [{ type: 'WorkshopSkip' }]
    }
    case 'shop':
      return [{ type: 'ShopBuyCard', index: 0 }, { type: 'ShopLeave' }]
    case 'event': {
      const def = run.eventId ? getEventDef(run.eventId) : null
      const choices = def?.choices ?? []
      const out: RunCommand[] = []
      choices.forEach((ch, i) => {
        out.push(eventChoiceNeedsCard(ch) ? { type: 'EventChoice', index: i, cardIndex: 0 } : { type: 'EventChoice', index: i })
      })
      return out.length > 0 ? [out[0], out[out.length - 1]] : []
    }
    default:
      return []
  }
}

export interface GoldenRun {
  readonly version: 1
  readonly origin: ReplayOrigin
  readonly commands: readonly RunCommand[]
  /** commands[i] を適用した後のハッシュ (16進8桁) */
  readonly hashes: readonly string[]
  readonly final: RunDigest
}

/** 1ランを決定的に進めてゴールデンを作る。maxSteps で打ち切り (途中まででも照合には使える) */
export function generateGolden(seed: number, leaderId: string, maxSteps = 6000): GoldenRun {
  const origin: ReplayOrigin = { kind: 'run', seed, leaderId }
  let run = createRun(seed, 'set-confirm', leaderId)
  const commands: RunCommand[] = []
  const hashes: string[] = []
  let steps = 0
  while (ACTIVE.has(run.phase) && steps < maxSteps) {
    const cands = botRunCandidates(run)
    if (cands.length === 0) break
    let applied: RunState | null = null
    let used: RunCommand | null = null
    for (const cmd of cands) {
      try {
        applied = applyRunCommand(run, cmd)
        used = cmd
        break
      } catch {
        /* 次の候補へ */
      }
    }
    if (applied === null || used === null) break
    run = applied
    commands.push(used)
    hashes.push(runHash(run))
    steps++
    // 戦闘中の膠着セーフガード (ボットの無限ループ検知は loop.test が担う)
    if (run.combat && run.combat.turn > 60) break
  }
  return { version: 1, origin, commands, hashes, final: runDigest(run) }
}

/** ゴールデンを再生して照合。最初に食い違ったコマンド番号 (1始まり) を返す。全一致なら null */
export function verifyGolden(g: GoldenRun, replay: (origin: ReplayOrigin, commands: readonly RunCommand[]) => { states: RunState[]; error: string | null }): { ok: boolean; at: number | null; error: string | null } {
  const { states, error } = replay(g.origin, g.commands)
  for (let i = 0; i < g.hashes.length; i++) {
    const st = states[i + 1]
    if (!st) return { ok: false, at: i + 1, error: error ?? `states[${i + 1}] が無い` }
    if (runHash(st) !== g.hashes[i]) return { ok: false, at: i + 1, error: `hash ${runHash(st)} ≠ ${g.hashes[i]} (${JSON.stringify(g.commands[i])})` }
  }
  return { ok: true, at: null, error }
}

// 型の整合だけ確認 (getCardDef は将来の方針拡張用に温存)
void getCardDef
