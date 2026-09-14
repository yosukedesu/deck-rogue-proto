// port-parity.test.ts — C# 移植側に手で複製しているデータ表が TS とずれていないかを機械固定する (2026-09-14)。
// 発端: 人間ラン#10 (スマホ) のジャーナルを TS で再生すると幕2の地図で分岐した。原因は MapGen.cs の ACT_POOLS に
// 2026-09-14 の「苔の産み手と苔スライム」が無く、幕2のプールが1つ短い = 抽選の添字が別の編成を指していた
// (ゴールデン8本は幕2の地図の抽選でその添字を踏まなかった)。表の複製は必ずここで照合する。
import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'
import { ACT_BOSS_POOLS, ELITE_POOLS, ACT_MUST_APPEAR, encounterPoolsForParity } from '../engine/map.ts'
import { REWARD_EXCLUDED } from '../engine/run.ts'

const cs = readFileSync(new URL('../../unity/Packages/com.deckrogue.engine/Runtime/MapGen.cs', import.meta.url), 'utf8')
const csRun = readFileSync(new URL('../../unity/Packages/com.deckrogue.engine/Runtime/Run.cs', import.meta.url), 'utf8')

/** C# の `NAME = new IReadOnlyList<string>[] { new[] {...}, ... }` から、幕ごとの ID 列を読む (コメントは除く) */
function csPools(name: string): string[][] {
  const start = cs.indexOf(`${name} = new`)
  expect(start, `MapGen.cs に ${name} がある`).toBeGreaterThan(0)
  const end = cs.indexOf('};', start)
  const body = cs.slice(start, end).replace(/\/\/.*$/gm, '')
  const acts = [...body.matchAll(/new(?:\s*string)?\[\]\s*\{([^}]*)\}/g)].map((m) => [...m[1].matchAll(/"([a-z0-9_]+)"/g)].map((x) => x[1]))
  return acts
}

describe('C# 移植の表は TS と同じ (MapGen.cs)', () => {
  it('ACT_POOLS (幕ごとの通常戦闘プール。並びも同じ = 抽選の添字が同じ編成を指す)', () => {
    expect(csPools('ACT_POOLS')).toEqual(encounterPoolsForParity.ACT_POOLS.map((a) => [...a]))
  })
  it('WEAK_POOLS', () => {
    expect(csPools('WEAK_POOLS')).toEqual(encounterPoolsForParity.WEAK_POOLS.map((a) => [...a]))
  })
  it('ELITE_POOLS', () => {
    expect(csPools('ELITE_POOLS')).toEqual(ELITE_POOLS.map((a) => [...a]))
  })
  it('ACT_BOSS_POOLS', () => {
    expect(csPools('ACT_BOSS_POOLS')).toEqual(ACT_BOSS_POOLS.map((a) => [...a]))
  })
  it('ACT_MUST_APPEAR', () => {
    expect(csPools('ACT_MUST_APPEAR')).toEqual(ACT_MUST_APPEAR.map((a) => [...a]))
  })
  it('REWARD_EXCLUDED (Run.cs。集合なので並びは問わない)', () => {
    const start = csRun.indexOf('REWARD_EXCLUDED = new HashSet<string>')
    expect(start).toBeGreaterThan(0)
    const body = csRun.slice(start, csRun.indexOf('};', start)).replace(/\/\/.*$/gm, '')
    const ids = [...body.matchAll(/"([a-z0-9_]+)"/g)].map((x) => x[1]).sort()
    expect(ids).toEqual([...REWARD_EXCLUDED].sort())
  })
})
