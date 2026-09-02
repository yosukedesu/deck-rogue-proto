// engine/golden.ts — ゴールデンマスターの要約 (Unity移植の等価性検証。docs/unity-port.md §1)。
// ラン状態を「移植側が同じ手順で再計算できる小さな要約」に潰し、FNV-1a 32bit でハッシュする。
// 要約はキーの並びを固定した JSON (JSON.stringify のキー順 = ここで書いた順) を対象にする。
// C# 側は同じキー順・同じ値の JSON 文字列を作って同じハッシュを出すこと (文字列は UTF-8 バイト列)。
import type { RunState } from './run.ts'
import type { GameState } from './types.ts'

/** 戦闘状態の要約。数値と短い文字列だけ (浮動小数は含めない = 言語間の表現差を避ける) */
export interface CombatDigest {
  readonly turn: number
  readonly phase: string
  readonly hp: number
  readonly block: number
  readonly iceBlock: number
  readonly energy: number
  readonly energyMax: number
  readonly growth: number
  readonly momentum: number
  readonly hand: readonly string[]
  readonly draw: number
  readonly discard: number
  readonly exhaust: number
  readonly set: readonly string[]
  readonly permanents: readonly string[]
  readonly enemies: readonly { readonly id: string; readonly hp: number; readonly block: number; readonly strength: number; readonly burn: number }[]
  readonly rngSeed: number
  readonly rngCounter: number
  readonly events: number
  /** イベント型の列 (直近50件) — どの手で分岐したかを特定する手がかり */
  readonly eventTail: readonly string[]
}

export interface RunDigest {
  readonly act: number
  readonly row: number
  readonly phase: string
  readonly hp: number
  readonly maxHp: number
  readonly gold: number
  readonly deck: readonly string[]
  readonly relics: readonly string[]
  readonly rngSeed: number
  readonly rngCounter: number
  readonly combat: CombatDigest | null
}

export function combatDigest(s: GameState): CombatDigest {
  return {
    turn: s.turn,
    phase: s.phase,
    hp: s.player.hp,
    block: s.player.block,
    iceBlock: s.player.iceBlock,
    energy: s.player.energy,
    energyMax: s.player.energyMax,
    growth: s.player.growth,
    momentum: s.player.momentum,
    hand: s.player.hand.map((c) => c.def.id),
    draw: s.player.drawPile.length,
    discard: s.player.discardPile.length,
    exhaust: s.player.exhaustPile.length,
    set: s.player.setCards.map((c) => c.def.id),
    permanents: s.player.permanents.map((c) => c.def.id),
    enemies: s.enemies.map((e) => ({ id: e.enemyId, hp: e.hp, block: e.block, strength: e.strength, burn: e.burn })),
    rngSeed: s.rng.seed,
    rngCounter: s.rng.counter,
    events: s.eventLog.length,
    eventTail: s.eventLog.slice(-50).map((e) => e.type),
  }
}

export function runDigest(run: RunState): RunDigest {
  return {
    act: run.act,
    row: run.row,
    phase: run.phase,
    hp: run.hp,
    maxHp: run.maxHp,
    gold: run.gold,
    deck: run.deck.map((c) => c.def.id),
    relics: [...run.relics],
    rngSeed: run.rng.seed,
    rngCounter: run.rng.counter,
    combat: run.combat ? combatDigest(run.combat) : null,
  }
}

/** FNV-1a 32bit (UTF-8 バイト列)。C# 側と同じ定数・同じバイト列で一致する */
export function fnv1a32(text: string): number {
  const bytes = new TextEncoder().encode(text)
  let h = 0x811c9dc5
  for (const b of bytes) {
    h ^= b
    h = Math.imul(h, 0x01000193) >>> 0
  }
  return h >>> 0
}

/** ラン状態のハッシュ (16進8桁) */
export function runHash(run: RunState): string {
  return fnv1a32(JSON.stringify(runDigest(run))).toString(16).padStart(8, '0')
}
