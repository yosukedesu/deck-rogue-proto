// scripts/migrate-enemy-graph.ts — enemies.json / encounters.json を行動グラフ (2026-09-14 本家式の状態機械) へ機械変換する。
// 第1段=等価移行: 旧形 (sequence/weight/opener/movesBelowHalf/…) を engine/enemyGraph.ts の graphFromLegacy で
// 同じ挙動のグラフに写す (RNG の消費順は不変)。編成の patternOffset は member.start に、分裂体の位相ずらしは
// 子の startBySlot に置き換える。使い方: npx tsx scripts/migrate-enemy-graph.ts
import { readFileSync, writeFileSync } from 'node:fs'
import { advanceCursor, graphFromLegacy, isGraphDef, validateEnemyGraph } from '../src/engine/enemyGraph.ts'
import type { LegacyEnemyDef } from '../src/engine/enemyGraph.ts'
import type { EncounterDef, EnemyDef } from '../src/engine/types.ts'

const ENEMIES = 'src/data/enemies.json'
const ENCOUNTERS = 'src/data/encounters.json'

const legacy = JSON.parse(readFileSync(ENEMIES, 'utf-8')) as readonly LegacyEnemyDef[]
const migrated: EnemyDef[] = legacy.map((e) => (isGraphDef(e) ? e : graphFromLegacy(e)))
const byId = new Map(migrated.map((e) => [e.id, e]))

// 分裂体の位相ずらし (旧: 子 k の初手は sequence[k]) → 子の startBySlot
for (const e of migrated) {
  const sp = e.splitInto
  if (sp === undefined || sp.stunned === true || sp.count <= 1) continue
  const child = byId.get(sp.enemyId)
  if (!child) throw new Error(`分裂先が無い: ${sp.enemyId}`)
  const starts = Array.from({ length: sp.count }, (_, k) => advanceCursor(child, k))
  if (starts.some((s) => s !== child.start)) {
    byId.set(child.id, { ...child, startBySlot: starts })
  }
}
const out = migrated.map((e) => byId.get(e.id)!)
for (const e of out) {
  const errs = validateEnemyGraph(e)
  if (errs.length > 0) throw new Error(`${e.id}: ${errs.join(' / ')}`)
}
// キーの並び: moves の直後に nodes/start/startBySlot/interrupts を置く (読みやすさ)
const ordered = out.map((e) => {
  const { moves, nodes, start, startBySlot, interrupts, movesVsSet, movesVsTokens, ...rest } = e as EnemyDef & Record<string, unknown>
  const head: Record<string, unknown> = {}
  for (const k of ['id', 'name', 'archetype', 'flavor', 'sprite', 'maxHp', 'hpRange']) if (k in rest) head[k] = (rest as Record<string, unknown>)[k]
  const tail: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(rest)) if (!(k in head)) tail[k] = v
  return {
    ...head,
    moves,
    nodes,
    start,
    ...(startBySlot !== undefined ? { startBySlot } : {}),
    ...(interrupts !== undefined ? { interrupts } : {}),
    ...(movesVsSet !== undefined ? { movesVsSet } : {}),
    ...(movesVsTokens !== undefined ? { movesVsTokens } : {}),
    ...tail,
  }
})
writeFileSync(ENEMIES, JSON.stringify(ordered, null, 2) + '\n')

// 編成: patternOffset → start (k 回目の宣言の節)
type LegacyMember = EncounterDef['members'][number] & { readonly patternOffset?: number }
const encounters = JSON.parse(readFileSync(ENCOUNTERS, 'utf-8')) as readonly (Omit<EncounterDef, 'members'> & { members: readonly LegacyMember[] })[]
let converted = 0
const encOut = encounters.map((enc) => ({
  ...enc,
  members: enc.members.map((m) => {
    const { patternOffset, ...rest } = m
    if (patternOffset === undefined || patternOffset === 0) return rest
    const def = byId.get(m.enemyId)
    if (!def) throw new Error(`編成 ${enc.id} の敵が無い: ${m.enemyId}`)
    converted++
    const { enemyId, ...others } = rest
    return { enemyId, start: advanceCursor(def, patternOffset), ...others }
  }),
}))
writeFileSync(ENCOUNTERS, JSON.stringify(encOut, null, 2) + '\n')
console.log(`enemies: ${out.length} 体を変換 / encounters: patternOffset ${converted} 件を start へ`)
