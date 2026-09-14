// engine/enemyGraph.ts — 敵の行動グラフ (2026-09-14 本家式の状態機械。確定済みルール表「敵の行動グラフ」)。
// 節 (技/乱択/条件) と割り込みの評価・旧形 (sequence/weight/opener/movesBelowHalf/…) からの機械変換・
// 図鑑向けの1行化・整合性検査。純ロジック (DOM/React 依存なし)。C# 側は EnemyGraph.cs が同形。
import { weightedIndex } from './rng.ts'
import type {
  RngState,
  EnemyCondition,
  EnemyDef,
  EnemyInterrupt,
  EnemyMove,
  EnemyNode,
  EnemyRandomArm,
  EnemyState,
  GameState,
} from './types.ts'

/** 節が技の節ならその技の id */
export function nodeMoveId(def: EnemyDef, nodeId: string): string | undefined {
  return def.nodes[nodeId]?.move
}

export function moveById(def: EnemyDef, moveId: string): EnemyMove {
  const m = def.moves.find((x) => x.id === moveId)
  if (!m) throw new Error(`敵 ${def.id} に技 ${moveId} が無い`)
  return m
}

/** 個体の開始節: 編成の上書き > スロット別 > start */
export function startNodeFor(def: EnemyDef, slot: number, override?: string): string {
  if (override !== undefined) return override
  const bySlot = def.startBySlot?.[slot]
  return bySlot ?? def.start
}

/** 条件の評価 (条件の節・割り込みが共用)。書かれた項目を全部満たす時に真 */
export function evalCondition(state: GameState, enemyIndex: number, cond: EnemyCondition): boolean {
  const e = state.enemies[enemyIndex]
  const othersAlive = state.enemies.filter((o, j) => j !== enemyIndex && o.hp > 0).length
  if (cond.hpBelowHalf === true && !(e.hp <= e.maxHp * 0.5)) return false
  if (cond.alone === true && othersAlive > 0) return false
  if (cond.allyAlive === true && othersAlive === 0) return false
  if (cond.usesAtLeast !== undefined && (e.moveUses?.[cond.usesAtLeast.move] ?? 0) < cond.usesAtLeast.count) return false
  if (cond.damageTakenAtLeast !== undefined && (e.damageTakenTotal ?? 0) < cond.damageTakenAtLeast) return false
  if (cond.turnParity !== undefined && (state.turn % 2 === 1 ? 'odd' : 'even') !== cond.turnParity) return false
  if (cond.alliesFewerThan !== undefined && othersAlive + 1 >= cond.alliesFewerThan) return false
  return true
}

/** 割り込みの条件が立っているか (from の節にいるか・発火済みかは呼び出し側) */
export function interruptHolds(state: GameState, enemyIndex: number, it: EnemyInterrupt): boolean {
  const e = state.enemies[enemyIndex]
  switch (it.on) {
    case 'hpBelowHalf':
      return e.hp > 0 && e.hp <= e.maxHp * 0.5
    case 'damageTaken':
      return (e.damageTakenTotal ?? 0) >= (it.amount ?? 0)
    case 'allyDied':
      return state.enemies.some((o, j) => j !== enemyIndex && o.hp <= 0 && o.fled !== true)
    case 'alone':
      return !state.enemies.some((o, j) => j !== enemyIndex && o.hp > 0)
  }
}

/**
 * 割り込みを上から順に判定してカーソルを飛ばす (1戦闘1回ずつ)。引き金の種別で絞れる
 * (被弾の瞬間は hpBelowHalf/damageTaken だけ、仲間の死亡時は allyDied/alone だけ、宣言時は全部)。
 * 戻り値: 飛んだ後のカーソルと発火済み添字
 */
export function applyInterruptsTo(
  state: GameState,
  enemyIndex: number,
  cursor: string,
  fired: readonly number[],
  only?: readonly EnemyInterrupt['on'][],
  /** 宣言済みで未実行の意図の節 (自ターン中の割り込み判定に渡す)。from にその節が含まれていれば「まだその節にいる」扱い (Opus AB: 3拍目の眠りが起きなかった) */
  pendingNode?: string,
): { readonly cursor: string; readonly fired: readonly number[]; readonly firedNow: readonly number[] } {
  const def = getDef(state, enemyIndex)
  let cur = cursor
  let f = fired
  const now: number[] = []
  ;(def.interrupts ?? []).forEach((it, k) => {
    if (f.includes(k)) return
    if (only !== undefined && !only.includes(it.on)) return
    if (it.from !== undefined && !it.from.includes(cur) && !(pendingNode !== undefined && it.from.includes(pendingNode))) return
    if (!interruptHolds(state, enemyIndex, it)) return
    cur = it.goto
    f = [...f, k]
    now.push(k)
  })
  return { cursor: cur, fired: f, firedNow: now }
}

/** 乱択の腕が今引けるか (noRepeat / once / maxRepeat) */
function armUsable(def: EnemyDef, arm: EnemyRandomArm, enemy: EnemyState): boolean {
  const target = nodeMoveId(def, arm.to)
  if (target === undefined) return true
  if (arm.once === true && (enemy.usedOnce ?? []).includes(target)) return false
  const last = enemy.lastMoves ?? []
  const streak = (n: number): boolean => last.length >= n && last.slice(0, n).every((m) => m === target)
  if (arm.noRepeat === true && streak(1)) return false
  if (arm.maxRepeat !== undefined && streak(arm.maxRepeat)) return false
  return true
}

export interface WalkResult {
  /** 着地した技の節の id */
  readonly nodeId: string
  readonly move: EnemyMove
  readonly rng: RngState
  /** once の腕で着地した技 (使用済みに記録する) */
  readonly onceMoveIds: readonly string[]
}

/**
 * カーソルから技の節まで辿る。乱択は重み抽選 (RNG 1回。引ける腕が無ければ全腕から引く=スタール防止)、
 * 条件はその場で評価。技の節でない節を 32 回辿っても着地しなければ定義の誤り
 */
export function walkToMove(state: GameState, enemyIndex: number, cursor: string, rng: RngState): WalkResult {
  const def = getDef(state, enemyIndex)
  const enemy = state.enemies[enemyIndex]
  let cur = cursor
  let r = rng
  const onceMoveIds: string[] = []
  for (let guard = 0; guard < 32; guard++) {
    const node = def.nodes[cur]
    if (!node) throw new Error(`敵 ${def.id} の行動グラフに節 ${cur} が無い`)
    if (node.move !== undefined) return { nodeId: cur, move: moveById(def, node.move), rng: r, onceMoveIds }
    if (node.random !== undefined) {
      const usable = node.random.filter((a) => armUsable(def, a, enemy))
      const arms = usable.length > 0 ? usable : node.random
      const [idx, r2] = weightedIndex(r, arms.map((a) => a.weight))
      r = r2
      const arm = arms[idx]
      if (arm.once === true) {
        const target = nodeMoveId(def, arm.to)
        if (target !== undefined) onceMoveIds.push(target)
      }
      cur = arm.to
      continue
    }
    if (node.if !== undefined) {
      const ok = evalCondition(state, enemyIndex, node.if)
      const to = ok ? node.then : node.else
      if (to === undefined) throw new Error(`敵 ${def.id} の条件の節 ${cur} に then/else が無い`)
      cur = to
      continue
    }
    throw new Error(`敵 ${def.id} の節 ${cur} は技/乱択/条件のどれでもない`)
  }
  throw new Error(`敵 ${def.id} の行動グラフが ${cursor} から技に着地しない (循環)`)
}

/** 決定的に辿れる範囲で、カーソルから見て d 手目 (0=次の宣言) に来る技を返す。乱択に当たったら null */
export function peekMoves(def: EnemyDef, state: GameState, enemyIndex: number, cursor: string, depth: number): (string | null)[] {
  const out: (string | null)[] = []
  let cur = cursor
  for (let d = 0; d < depth; d++) {
    let guard = 0
    let landed: string | null = null
    while (guard++ < 32) {
      const node = def.nodes[cur]
      if (!node) return [...out, null]
      if (node.move !== undefined) {
        landed = node.move
        cur = node.next ?? cur
        break
      }
      if (node.random !== undefined) return [...out, null]
      if (node.if !== undefined) {
        cur = (evalCondition(state, enemyIndex, node.if) ? node.then : node.else) ?? cur
        continue
      }
      return [...out, null]
    }
    out.push(landed)
    if (landed === null) return out
  }
  return out
}

function getDef(state: GameState, enemyIndex: number): EnemyDef {
  return enemyDefLookup(state.enemies[enemyIndex].enemyId)
}

/** content.ts が起動時に差し込む (enemyGraph → content の循環 import を避ける) */
let enemyDefLookup: (id: string) => EnemyDef = () => {
  throw new Error('enemyGraph: getEnemyDef が未設定')
}
export function bindEnemyDefLookup(fn: (id: string) => EnemyDef): void {
  enemyDefLookup = fn
}

// ---- 旧形からの機械変換 (第1段=等価移行。移行スクリプトと normalizeEnemyDef が共用) ----

/** 旧形の技 (重み・制約が技に付いていた) */
export type LegacyEnemyMove = EnemyMove & {
  readonly weight?: number
  readonly noRepeat?: boolean
  readonly once?: boolean
}

/** 旧形の定義 (2026-09-14 以前の enemies.json / テスト / 調整モードの新規敵) */
export type LegacyEnemyDef = Omit<EnemyDef, 'moves' | 'nodes' | 'start' | 'movesVsSet' | 'movesVsTokens'> & {
  readonly moves: readonly LegacyEnemyMove[]
  readonly nodes?: Readonly<Record<string, EnemyNode>>
  readonly start?: string
  readonly sequence?: readonly string[]
  readonly sequenceLoopFrom?: number
  readonly opener?: string
  readonly movesBelowHalf?: readonly LegacyEnemyMove[]
  readonly sequenceBelowHalf?: readonly string[]
  readonly sequenceBelowHalfLoopFrom?: number
  readonly movesWhenAlone?: readonly LegacyEnemyMove[]
  readonly sequenceWhenAlone?: readonly string[]
  readonly phaseAfterUses?: { readonly moveId: string; readonly uses: number; readonly sequence: readonly string[] }
  readonly wakeOnDamage?: { readonly damage: number; readonly resumeAt: number }
  readonly movesVsSet?: readonly LegacyEnemyMove[] | readonly EnemyRandomArm[]
  readonly movesVsTokens?: readonly LegacyEnemyMove[] | readonly EnemyRandomArm[]
}

export function isGraphDef(def: LegacyEnemyDef | EnemyDef): def is EnemyDef {
  return (def as EnemyDef).nodes !== undefined && (def as EnemyDef).start !== undefined
}

function stripLegacyMove(m: LegacyEnemyMove): EnemyMove {
  const { weight: _w, noRepeat: _n, once: _o, ...rest } = m
  return rest
}

/**
 * 旧形 → 行動グラフ。RNG の消費順を変えない (重み抽選の敵は同じ順・同じ重みの腕を1つの乱択の節に)。
 * 意図した差は「HP半分・単独時の切替は列の先頭から始まり一方通行」だけ (旧実装は patternIndex を持ち越していた)
 */
export function graphFromLegacy(legacy: LegacyEnemyDef): EnemyDef {
  if (isGraphDef(legacy)) return legacy
  const {
    moves: legacyMoves,
    sequence,
    sequenceLoopFrom,
    opener,
    movesBelowHalf,
    sequenceBelowHalf,
    sequenceBelowHalfLoopFrom,
    movesWhenAlone,
    sequenceWhenAlone,
    phaseAfterUses,
    wakeOnDamage,
    movesVsSet,
    movesVsTokens,
    nodes: _nodes,
    start: _start,
    ...rest
  } = legacy
  // 技の定義を1つの辞書に (同じ id は同じ本体でなければならない)
  const moves: EnemyMove[] = []
  const legacyById = new Map<string, LegacyEnemyMove>()
  const addMoves = (table: readonly LegacyEnemyMove[] | undefined): void => {
    for (const m of table ?? []) {
      const prev = legacyById.get(m.id)
      const body = JSON.stringify(stripLegacyMove(m))
      if (prev !== undefined) {
        if (JSON.stringify(stripLegacyMove(prev)) !== body) throw new Error(`敵 ${legacy.id} の技 ${m.id} が表ごとに違う本体を持つ`)
        continue
      }
      legacyById.set(m.id, m)
      moves.push(stripLegacyMove(m))
    }
  }
  const isArms = (t: readonly LegacyEnemyMove[] | readonly EnemyRandomArm[] | undefined): t is readonly EnemyRandomArm[] =>
    t !== undefined && t.length > 0 && (t[0] as EnemyRandomArm).to !== undefined
  addMoves(legacyMoves)
  addMoves(movesBelowHalf)
  addMoves(movesWhenAlone)
  if (!isArms(movesVsSet)) addMoves(movesVsSet)
  if (!isArms(movesVsTokens)) addMoves(movesVsTokens)

  const nodes: Record<string, EnemyNode> = {}
  const used = new Set<string>()
  const fresh = (base: string): string => {
    let id = base
    for (let n = 2; used.has(id); n++) id = `${base}_${n}`
    used.add(id)
    return id
  }
  const armsOf = (table: readonly LegacyEnemyMove[], targetOf: (m: LegacyEnemyMove) => string): EnemyRandomArm[] =>
    table.map((m) => ({
      to: targetOf(m),
      weight: m.weight ?? 1,
      ...(m.noRepeat === true ? { noRepeat: true } : {}),
      ...(m.once === true ? { once: true } : {}),
    }))
  /** 固定列 → 技の節の鎖。戻り: 各拍の節 id */
  const chain = (seq: readonly string[], loopFrom: number, prefix: string): string[] => {
    const ids = seq.map((moveId) => fresh(`${prefix}${moveId}`))
    ids.forEach((id, i) => {
      const nextId = i + 1 < ids.length ? ids[i + 1] : ids[Math.min(loopFrom, ids.length - 1)]
      nodes[id] = { move: seq[i], next: nextId }
    })
    return ids
  }
  /** 重み表 → 乱択の節 + 各技の節 (next は乱択へ戻る)。戻り: 乱択の節 id */
  const table = (moves_: readonly LegacyEnemyMove[], prefix: string): string => {
    const randId = fresh(`${prefix}rand`)
    const targets = new Map<string, string>()
    for (const m of moves_) targets.set(m.id, fresh(`${prefix}${m.id}`))
    for (const m of moves_) nodes[targets.get(m.id)!] = { move: m.id, next: randId }
    nodes[randId] = { random: armsOf(moves_, (m) => targets.get(m.id)!) }
    return randId
  }

  let start: string
  const interrupts: EnemyInterrupt[] = []
  let mainChain: string[] | undefined
  if (sequence !== undefined && sequence.length > 0) {
    mainChain = chain(sequence, sequenceLoopFrom ?? 0, '')
    start = mainChain[0]
    if (phaseAfterUses !== undefined) {
      const p2 = chain(phaseAfterUses.sequence, 0, 'p2_')
      // 対象の技を規定回数宣言したら、次の宣言から新しい列の先頭 (旧: keyMoveUses が uses に達した瞬間 patternIndex=0)
      mainChain.forEach((id) => {
        const node = nodes[id]
        if (node.move !== phaseAfterUses.moveId) return
        const check = fresh(`${phaseAfterUses.moveId}_check`)
        nodes[check] = { if: { usesAtLeast: { move: phaseAfterUses.moveId, count: phaseAfterUses.uses } }, then: p2[0], else: node.next! }
        nodes[id] = { move: node.move, next: check }
      })
    }
  } else {
    const randId = table(legacyMoves, '')
    start = opener !== undefined ? `${opener}` : randId
    if (opener !== undefined && nodes[start]?.move !== opener) throw new Error(`敵 ${legacy.id} の opener が未定義の行動を参照: ${opener}`)
  }
  // HP半分の豹変 (優先度は最上位 = 割り込みの先頭)
  if (sequenceBelowHalf !== undefined && sequenceBelowHalf.length > 0) {
    const ids = chain(sequenceBelowHalf, sequenceBelowHalfLoopFrom ?? 0, 'half_')
    interrupts.push({ on: 'hpBelowHalf', goto: ids[0] })
  } else if (movesBelowHalf !== undefined && movesBelowHalf.length > 0) {
    interrupts.push({ on: 'hpBelowHalf', goto: table(movesBelowHalf, 'half_') })
  }
  // 単独時の転職
  if (sequenceWhenAlone !== undefined && sequenceWhenAlone.length > 0) {
    const ids = chain(sequenceWhenAlone, 0, 'alone_')
    interrupts.push({ on: 'alone', goto: ids[0] })
  } else if (movesWhenAlone !== undefined && movesWhenAlone.length > 0) {
    interrupts.push({ on: 'alone', goto: table(movesWhenAlone, 'alone_') })
  }
  // 被弾覚醒 (旧: 被弾時に判定。HP半分/単独時より後ろ = 旧優先度と同じ)
  if (wakeOnDamage !== undefined && mainChain !== undefined) {
    const from = mainChain.slice(0, wakeOnDamage.resumeAt)
    const gotoId = mainChain[Math.min(wakeOnDamage.resumeAt, mainChain.length - 1)]
    interrupts.push({ on: 'damageTaken', amount: wakeOnDamage.damage, from, goto: gotoId })
  }
  const vsSet = isArms(movesVsSet) ? movesVsSet : movesVsSet !== undefined ? armsOf(movesVsSet, (m) => m.id) : undefined
  const vsTokens = isArms(movesVsTokens) ? movesVsTokens : movesVsTokens !== undefined ? armsOf(movesVsTokens, (m) => m.id) : undefined
  return {
    ...(rest as Omit<EnemyDef, 'moves' | 'nodes' | 'start' | 'movesVsSet' | 'movesVsTokens' | 'interrupts'>),
    moves,
    nodes,
    start,
    ...(interrupts.length > 0 ? { interrupts } : {}),
    ...(vsSet !== undefined ? { movesVsSet: vsSet } : {}),
    ...(vsTokens !== undefined ? { movesVsTokens: vsTokens } : {}),
  }
}

/** 旧形なら変換、新形ならそのまま (content.ts の読込・テスト・調整モードの新規敵) */
export function normalizeEnemyDef(def: LegacyEnemyDef | EnemyDef): EnemyDef {
  return isGraphDef(def) ? def : graphFromLegacy(def as LegacyEnemyDef)
}

/**
 * start から技の節を k 回進めた節 (= k 回目の宣言で辿り始める節)。旧 patternOffset / 分裂体の位相ずらしの置換に使う。
 * 乱択・条件の節に当たったらそこで止まる (その先は宣言時にしか決まらない)
 */
export function advanceCursor(def: EnemyDef, k: number): string {
  let cur = def.start
  for (let i = 0; i < k; i++) {
    const node = def.nodes[cur]
    if (!node || node.move === undefined) break
    cur = node.next ?? cur
  }
  return cur
}

/** start から技の節を辿った技 id の列 (depth 手ぶん)。乱択・条件に当たったらそこまで (テスト・図鑑向け) */
export function chainFromStart(def: EnemyDef, depth: number, from: string = def.start): string[] {
  const out: string[] = []
  let cur = from
  for (let i = 0; i < depth; i++) {
    const node = def.nodes[cur]
    if (!node || node.move === undefined) break
    out.push(node.move)
    cur = node.next ?? cur
  }
  return out
}

/** 乱択の節を1つ返す (重み表の敵のテスト向け。無ければ undefined) */
export function randomNodeOf(def: EnemyDef, from: string = def.start): EnemyNode | undefined {
  let cur = from
  for (let i = 0; i < 32; i++) {
    const node = def.nodes[cur]
    if (!node) return undefined
    if (node.random !== undefined) return node
    if (node.move !== undefined) {
      if (node.next === undefined || node.next === cur) return undefined
      cur = node.next
      continue
    }
    return undefined
  }
  return undefined
}

/** 節から決定的に辿って最初に着地する技 (乱択ならその乱択の腕の技を列挙して先頭)。予告チップ向け */
export function firstMoveOf(def: EnemyDef, nodeId: string): EnemyMove | undefined {
  let cur = nodeId
  for (let i = 0; i < 32; i++) {
    const node = def.nodes[cur]
    if (!node) return undefined
    if (node.move !== undefined) return def.moves.find((m) => m.id === node.move)
    if (node.random !== undefined) {
      const first = node.random[0]
      return first !== undefined ? firstMoveOf(def, first.to) : undefined
    }
    if (node.if !== undefined) {
      cur = node.then ?? cur
      continue
    }
    return undefined
  }
  return undefined
}

// ---- 図鑑・CLI 向けの1行化 ----

function condText(def: EnemyDef, c: EnemyCondition, label: (moveId: string) => string): string {
  const parts: string[] = []
  if (c.hpBelowHalf) parts.push('HP半分以下')
  if (c.alone) parts.push('仲間が全滅')
  if (c.allyAlive) parts.push('仲間が生存')
  if (c.usesAtLeast) parts.push(`${def.moves.some((m) => m.id === c.usesAtLeast!.move) ? label(c.usesAtLeast.move) : c.usesAtLeast.move}を${c.usesAtLeast.count}回使った`)
  if (c.damageTakenAtLeast !== undefined) parts.push(`累計${c.damageTakenAtLeast}被弾`)
  if (c.turnParity) parts.push(c.turnParity === 'odd' ? '奇数ターン' : '偶数ターン')
  if (c.alliesFewerThan !== undefined) parts.push(`味方が${c.alliesFewerThan}体未満`)
  return parts.join('かつ') || '常に'
}

/** 状態異常の日本語名 (engine 側の表示用。UI の STATUS_LABEL と同じ語) */
export const STATUS_JA: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', frail: '虚弱', wound: '負傷', junk: 'がらくた', scald: '火傷', restrain: '拘束', mist: '霞み', slow: '重り' }

/**
 * 技の既定の表記「⚔️12〜16×2+虚弱1」(2026-09-14 Opus AB/AB2/AB3: 図鑑の行動欄が生IDで読めなかった)。
 * strength を渡すと攻撃の幅に今の筋力を足す (予告チップ: 未宣言の技は幅・宣言済みは実値、の2層)
 */
export function moveLabel(def: EnemyDef, moveId: string, strength = 0): string {
  const m = def.moves.find((x) => x.id === moveId)
  if (!m) return moveId
  const mark: Record<string, string> = { attack: '⚔️', defend: '🛡️', buff: '💪', rally: '📣', hex: '🧿', heal: '💚', 'steal-gold': '💰', flee: '🏃', rest: '😮‍💨', hatch: '🐣', mill: '📖', 'destroy-set': '💥', 'destroy-token': '🪓', summon: '👶' }
  const add = m.kind === 'attack' ? strength : 0
  const lo = m.min !== undefined ? Math.max(m.kind === 'attack' ? 1 : m.min, m.min + add) : undefined
  const hi = m.max !== undefined ? Math.max(m.kind === 'attack' ? 1 : m.max, m.max + add) : undefined
  const range = lo !== undefined ? (lo === hi ? `${lo}` : `${lo}〜${hi}`) : ''
  const sign = m.kind === 'buff' || m.kind === 'rally' ? '+' : ''
  const hits = m.mirrorHits === true ? '×手数' : (m.hits ?? 1) > 1 ? `×${m.hits}` : ''
  const inflict = m.inflict ? `+${STATUS_JA[m.inflict.status] ?? m.inflict.status}${m.inflict.amount}` : ''
  const riders = `${m.alsoDefend !== undefined ? `+盾${m.alsoDefend}` : ''}${m.alsoBuff !== undefined ? `+筋${m.alsoBuff}` : ''}${m.alsoDestroySet === true ? '+壊し' : ''}${m.growPerUse !== undefined ? `(+${m.growPerUse}/回)` : ''}${m.growHitsPerUse !== undefined ? `(ヒット+${m.growHitsPerUse}/回)` : ''}`
  const summon = m.summon ? `${summonName(m.summon.enemyId)}×${m.summon.count}` : ''
  return `${mark[m.kind] ?? m.kind}${sign}${range}${hits}${inflict}${riders}${summon}`
}

function summonName(enemyId: string): string {
  try {
    return enemyDefLookup(enemyId).name
  } catch {
    return enemyId
  }
}

/** 節から決定的に辿れる最初の n 手の技 (乱択・条件に当たったらそこまで)。予告チップ向け */
export function previewMoves(def: EnemyDef, nodeId: string, n: number): EnemyMove[] {
  const out: EnemyMove[] = []
  let cur = nodeId
  for (let i = 0; i < n; i++) {
    const first = firstMoveOf(def, cur)
    if (!first) break
    out.push(first)
    // 次の節へ (技の節を辿った先。乱択/条件ならそこで止まる)
    const node = def.nodes[cur]
    if (!node || node.move === undefined) break
    if (node.next === undefined || node.next === cur) break
    cur = node.next
  }
  return out
}

/**
 * start (または任意の節) から辿った行動の並びを文字列に。乱択は候補を「乱択{a 2/b 1}」・条件は両側を1段展開して
 * 「(条件? A→… : B→…)」・既に通った節に戻ったら「→(◯へ戻る)」で止める。技は既定で moveLabel の表記
 * (2026-09-14: 生IDと条件節が「条件」止まりで読めなかった、への処方)
 */
export function describeGraphFrom(def: EnemyDef, from: string, label: (moveId: string) => string = (m) => moveLabel(def, m), seen: Set<string> = new Set(), budget = 10): string {
  const out: string[] = []
  let cur = from
  for (let guard = 0; guard < budget; guard++) {
    const node = def.nodes[cur]
    if (!node) return `${out.join('→')}→?${cur}`
    if (seen.has(cur)) {
      out.push(`(${labelOfNode(def, cur, label)}へ戻る)`)
      break
    }
    seen.add(cur)
    if (node.move !== undefined) {
      out.push(label(node.move))
      if (node.next === undefined || node.next === cur) {
        out.push('(繰り返し)')
        break
      }
      cur = node.next
      continue
    }
    if (node.random !== undefined) {
      const arms = node.random.map((a) => {
        const flags = [a.noRepeat ? '連続不可' : '', a.once ? '1回' : '', a.maxRepeat !== undefined ? `${a.maxRepeat}連まで` : ''].filter(Boolean)
        return `${labelOfNode(def, a.to, label)} ${a.weight}${flags.length > 0 ? `(${flags.join('・')})` : ''}`
      })
      out.push(`乱択{${arms.join('/')}}`)
      // 乱択の先は各腕の技 (次は乱択へ戻るのが普通) = ここで止める
      break
    }
    if (node.if !== undefined) {
      const then = node.then !== undefined ? describeGraphFrom(def, node.then, label, new Set(seen), 4) : '?'
      const els = node.else !== undefined ? describeGraphFrom(def, node.else, label, new Set(seen), 4) : '?'
      out.push(`(${condText(def, node.if, label)}? ${then} : ${els})`)
      break
    }
    break
  }
  return out.join('→')
}

function labelOfNode(def: EnemyDef, nodeId: string, label: (moveId: string) => string): string {
  const n = def.nodes[nodeId]
  if (!n) return nodeId
  if (n.move !== undefined) return label(n.move)
  if (n.random !== undefined) return '乱択'
  return '条件'
}

const TRIGGER_TEXT: Record<EnemyInterrupt['on'], string> = {
  hpBelowHalf: 'HP半分で',
  damageTaken: '累計被弾で',
  allyDied: '仲間が倒れると',
  alone: '仲間が全滅すると',
}

/** 図鑑の行動欄: 「並び」と「割り込み→並び」 */
export function describeGraph(def: EnemyDef, label?: (moveId: string) => string): string[] {
  const lines = [describeGraphFrom(def, def.start, label)]
  for (const it of def.interrupts ?? []) {
    const trig = it.on === 'damageTaken' ? `累計${it.amount ?? 0}被弾で` : TRIGGER_TEXT[it.on]
    lines.push(`${trig}→${describeGraphFrom(def, it.goto, label)}`)
  }
  return lines
}

/** 割り込みの引き金の文言 (UI チップ・CLI タグ) */
export function interruptTriggerText(it: EnemyInterrupt): string {
  return it.on === 'damageTaken' ? `累計${it.amount ?? 0}ダメで` : TRIGGER_TEXT[it.on]
}

/** 眠り (被弾で目覚める割り込み) の残り: カーソルが from にいて未発火なら、その割り込み */
export function sleepingInterrupt(def: EnemyDef, e: EnemyState): EnemyInterrupt | undefined {
  // カーソルは次の節へ進んでいるので、構えている意図の節 (intentNode) も from の照合に入れる (3拍目の眠りにタグが消えていた)
  const at = (from: readonly string[]): boolean => from.includes(e.node) || (e.intentNode !== undefined && from.includes(e.intentNode))
  return (def.interrupts ?? []).find(
    (it, k) => it.on === 'damageTaken' && !(e.firedInterrupts ?? []).includes(k) && (it.from === undefined || at(it.from)),
  )
}

// ---- 整合性検査 (テスト・content の読込で使う) ----

export function validateEnemyGraph(def: EnemyDef): string[] {
  const errs: string[] = []
  const moveIds = new Set(def.moves.map((m) => m.id))
  const nodeIds = new Set(Object.keys(def.nodes))
  if (!nodeIds.has(def.start)) errs.push(`start ${def.start} が節に無い`)
  for (const s of def.startBySlot ?? []) if (!nodeIds.has(s)) errs.push(`startBySlot ${s} が節に無い`)
  for (const [id, n] of Object.entries(def.nodes)) {
    const kinds = [n.move !== undefined, n.random !== undefined, n.if !== undefined].filter(Boolean).length
    if (kinds !== 1) errs.push(`節 ${id} は技/乱択/条件のどれか1つだけを持つ`)
    if (n.move !== undefined && !moveIds.has(n.move)) errs.push(`節 ${id} の技 ${n.move} が無い`)
    if (n.next !== undefined && !nodeIds.has(n.next)) errs.push(`節 ${id} の next ${n.next} が無い`)
    if (n.random !== undefined) {
      if (n.random.length === 0) errs.push(`節 ${id} の乱択に腕が無い`)
      for (const a of n.random) {
        if (!nodeIds.has(a.to)) errs.push(`節 ${id} の腕 ${a.to} が無い`)
        else if ((a.noRepeat || a.once || a.maxRepeat !== undefined) && def.nodes[a.to].move === undefined) errs.push(`節 ${id} の腕 ${a.to} は技の節でないので noRepeat/once/maxRepeat を付けられない`)
        if (!(a.weight > 0)) errs.push(`節 ${id} の腕 ${a.to} の重みが0以下`)
      }
    }
    if (n.if !== undefined) {
      if (n.then === undefined || !nodeIds.has(n.then)) errs.push(`節 ${id} の then が無い`)
      if (n.else === undefined || !nodeIds.has(n.else)) errs.push(`節 ${id} の else が無い`)
      if (n.if.usesAtLeast !== undefined && !moveIds.has(n.if.usesAtLeast.move)) errs.push(`節 ${id} の usesAtLeast の技 ${n.if.usesAtLeast.move} が無い`)
    }
  }
  ;(def.interrupts ?? []).forEach((it, k) => {
    if (!nodeIds.has(it.goto)) errs.push(`割り込み${k} の goto ${it.goto} が節に無い`)
    for (const f of it.from ?? []) if (!nodeIds.has(f)) errs.push(`割り込み${k} の from ${f} が節に無い`)
    if (it.on === 'damageTaken' && (it.amount === undefined || it.amount <= 0)) errs.push(`割り込み${k} (damageTaken) に amount が無い`)
  })
  for (const arms of [def.movesVsSet, def.movesVsTokens]) {
    for (const a of arms ?? []) if (!moveIds.has(a.to)) errs.push(`反応テーブルの腕 ${a.to} が技に無い`)
  }
  // 技・乱択・条件を 32 回辿って技に着地しない節 (条件は両側とも辿れないと決められないので乱択/条件の自己循環だけ見る)
  for (const id of nodeIds) {
    let cur = id
    const seen = new Set<string>()
    for (let i = 0; i < 32; i++) {
      const n = def.nodes[cur]
      if (!n || n.move !== undefined) break
      if (seen.has(cur)) {
        errs.push(`節 ${id} から技に着地しない循環がある`)
        break
      }
      seen.add(cur)
      if (n.random !== undefined) cur = n.random[0]?.to ?? cur
      else if (n.if !== undefined) cur = n.then ?? cur
    }
  }
  return errs
}
