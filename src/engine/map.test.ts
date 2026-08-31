// マップ生成のテスト。確定済みルール表「マップ」を機械的に固定する。
// - 18行・行17=ボス固定・強制焚き火行5/11/16 (2026-08-29 ?マスの本家水準化に伴い16行から拡張)
// - 部屋タイプは本家の重みで員数化: 工房/ショップ=総ノードの5%・?=22%・エリートは員数固定4
// - 配置制約 (本家): 行0は全て戦闘 / エリートは行2以降 / 親と同タイプ禁止 (エリート・ショップ・工房) /
//   兄弟 (同じ親を持つ同行ノード) と同タイプ禁止 (全種)
// - どのパスも戦闘数8以上 (上限は設けない = 本家に戦闘数の保証は無い)
// - エッジは全ノード到達可能 (開始から到達でき、ボスへ到達できる)
import { describe, expect, it } from 'vitest'
import { createRng } from './rng.ts'
import { ACT_BOSSES, BOSS_ROW, ELITE_POOLS, FORCED_CAMPFIRE_ROWS, generateMap, MAP_ROWS, tierFor } from './map.ts'
import type { RunMap } from './map.ts'

const SEEDS = Array.from({ length: 40 }, (_, i) => i + 1)

function mapFor(seed: number): RunMap {
  return generateMap(createRng(seed))[0]
}

/** 各ノードの親 (前の行で自分に繋がる列) */
function parentsOf(map: RunMap): number[][][] {
  const parents = map.map((row) => row.map((): number[] => []))
  for (let r = 0; r < map.length - 1; r++) {
    map[r].forEach((n, c) => {
      for (const to of n.next) parents[r + 1][to].push(c)
    })
  }
  return parents
}

describe('マップ生成の構造', () => {
  it('決定論: 同じシードは同じマップ', () => {
    expect(JSON.stringify(mapFor(7))).toBe(JSON.stringify(mapFor(7)))
  })

  it('18行・行17はボス単体・強制焚き火行 (5/11/16) は全ノード焚き火', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      expect(map).toHaveLength(MAP_ROWS)
      expect(map[BOSS_ROW]).toHaveLength(1)
      expect(map[BOSS_ROW][0].type).toBe('boss')
      for (const r of FORCED_CAMPFIRE_ROWS) {
        expect(map[r].every((n) => n.type === 'campfire'), `seed${seed} row${r}`).toBe(true)
      }
    }
  })

  it('部屋タイプの員数: 工房/ショップは総ノードの5%・?は18〜26%・エリートちょうど4', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const all = map.flat()
      const total = all.length
      const expected5 = Math.round(total * 0.05)
      expect(all.filter((n) => n.type === 'workshop'), `seed${seed}`).toHaveLength(expected5)
      expect(all.filter((n) => n.type === 'shop'), `seed${seed}`).toHaveLength(expected5)
      expect(all.filter((n) => n.type === 'elite'), `seed${seed}`).toHaveLength(4)
      // ?は本家の22%を員数式にしたもの (自由ノードにだけ配るので実測は21.7%)
      const eventRatio = all.filter((n) => n.type === 'event').length / total
      expect(eventRatio, `seed${seed} ?の構成比`).toBeGreaterThanOrEqual(0.18)
      expect(eventRatio, `seed${seed} ?の構成比`).toBeLessThanOrEqual(0.26)
    }
  })

  it('行0は全ノードが通常戦闘 (本家 floor1 準拠)', () => {
    for (const seed of SEEDS) {
      expect(mapFor(seed)[0].every((n) => n.type === 'battle'), `seed${seed}`).toBe(true)
    }
  })

  it('エリートは行2以降にしか出ない', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      for (let r = 0; r < 2; r++) {
        expect(map[r].some((n) => n.type === 'elite'), `seed${seed} row${r}`).toBe(false)
      }
    }
  })

  it('親と同タイプにならない (エリート・ショップ・工房)。?と戦闘は縦に続いてよい', () => {
    const exclusive = new Set(['elite', 'shop', 'workshop'])
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const parents = parentsOf(map)
      for (let r = 1; r < MAP_ROWS; r++) {
        map[r].forEach((n, c) => {
          if (!exclusive.has(n.type)) return
          for (const p of parents[r][c]) {
            expect(map[r - 1][p].type, `seed${seed} row${r} col${c} が親と同タイプ`).not.toBe(n.type)
          }
        })
      }
    }
  })

  it('兄弟 (同じ親を共有する同行ノード) が同じ特別ノードにならない', () => {
    // battle は員数を配った残余の既定なので対象外 (3列の行では必然的に重なる)
    const special = new Set(['elite', 'shop', 'workshop', 'event'])
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const parents = parentsOf(map)
      for (let r = 1; r < BOSS_ROW; r++) {
        if (FORCED_CAMPFIRE_ROWS.has(r)) continue // 焚き火行は全ノード同種が仕様
        for (let a = 0; a < map[r].length; a++) {
          for (let b = a + 1; b < map[r].length; b++) {
            if (!special.has(map[r][a].type)) continue
            const shared = parents[r][a].some((p) => parents[r][b].includes(p))
            if (shared) {
              expect(map[r][a].type, `seed${seed} row${r} の兄弟 ${a},${b} が同タイプ`).not.toBe(
                map[r][b].type,
              )
            }
          }
        }
      }
    }
  })

  it('全ノードが開始から到達可能で、全ノードからボスへ到達できる', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      // 前向き到達
      let reachable = map[0].map(() => true)
      for (let r = 0; r < MAP_ROWS - 1; r++) {
        const next = map[r + 1].map(() => false)
        map[r].forEach((node, c) => {
          if (!reachable[c]) return
          for (const to of node.next) next[to] = true
        })
        // 全ノードに入り口がある
        next.forEach((ok, c) => expect(ok, `seed${seed} row${r + 1} col${c} に入り口が無い`).toBe(true))
        reachable = next
      }
      // 後ろ向き: 全ノードに出口がある (ボス行以外)
      for (let r = 0; r < MAP_ROWS - 1; r++) {
        map[r].forEach((node, c) => {
          expect(node.next.length, `seed${seed} row${r} col${c} に出口が無い`).toBeGreaterThan(0)
        })
      }
    }
  })

  it('本家式パスウォーク: 格子列は昇順・移動は±1以内・エッジは格子空間で交差しない', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      for (let r = 0; r < MAP_ROWS; r++) {
        const cols = map[r].map((n) => n.col ?? -1)
        for (const c of cols) {
          expect(c, `seed${seed} row${r} に格子列が無い`).toBeGreaterThanOrEqual(0)
          expect(c).toBeLessThan(7)
        }
        for (let i = 1; i < cols.length; i++) {
          expect(cols[i], `seed${seed} row${r} の格子列が昇順でない`).toBeGreaterThan(cols[i - 1])
        }
      }
      for (let r = 0; r < MAP_ROWS - 2; r++) {
        // ボス行への合流 (全ノード→1点) は除く
        const es: (readonly [number, number])[] = []
        map[r].forEach((n) => {
          for (const to of n.next) {
            const a = n.col ?? 0
            const b = map[r + 1][to].col ?? 0
            expect(Math.abs(b - a), `seed${seed} row${r} で±1を超える移動`).toBeLessThanOrEqual(1)
            es.push([a, b])
          }
        })
        for (const [a1, b1] of es) {
          for (const [a2, b2] of es) {
            expect((a1 - a2) * (b1 - b2) < 0, `seed${seed} row${r} でエッジが交差`).toBe(false)
          }
        }
      }
    }
  })

  it('エリート供給の保証: 3個以上踏める経路が存在する (狙えば拾える)', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      let maxE = map[0].map(() => 0)
      for (let r = 0; r < MAP_ROWS - 1; r++) {
        const next = new Array<number>(map[r + 1].length).fill(-Infinity)
        map[r].forEach((n, c) => {
          for (const to of n.next) {
            const gain = map[r + 1][to].type === 'elite' ? 1 : 0
            next[to] = Math.max(next[to], maxE[c] + gain)
          }
        })
        maxE = next
      }
      expect(maxE[0], `seed${seed}`).toBeGreaterThanOrEqual(3)
    }
  })

  it('行0は2ノード以上 (最初の2本のパスは別の列から = 開始の選択が必ずある)', () => {
    for (const seed of SEEDS) {
      expect(mapFor(seed)[0].length, `seed${seed}`).toBeGreaterThanOrEqual(2)
    }
  })

  it('幕ごとにボスが固定される (オーガ→大亀→門番の難度順)', () => {
    const { createRng: mk } = { createRng }
    for (let act = 1; act <= 3; act++) {
      const [m] = generateMap(mk(7), act)
      expect(m[BOSS_ROW][0].encounterId).toBe(ACT_BOSSES[act - 1])
    }
  })

  it('緑を含まないランでは工房ノードを生成しない (v1は緑同士のみのため)', () => {
    for (const seed of SEEDS.slice(0, 10)) {
      const [m] = generateMap(createRng(seed), 1, false)
      const all = m.flat()
      expect(all.filter((n) => n.type === 'workshop')).toHaveLength(0)
      expect(all.filter((n) => n.type === 'shop')).toHaveLength(Math.round(all.length * 0.05))
      // ?は色で非対称にならない (旧実装は緑ランだけ?が1個に縮退していた)
      const ratio = all.filter((n) => n.type === 'event').length / all.length
      expect(ratio, `seed${seed}`).toBeGreaterThanOrEqual(0.18)
    }
  })

  it('60シード×3幕すべてでマップ生成が収束する (throw しない)', () => {
    for (let seed = 1; seed <= 60; seed++) {
      for (let act = 1; act <= 3; act++) {
        expect(() => generateMap(createRng(seed), act)).not.toThrow()
      }
    }
  })

  it('戦闘ノードの敵は行の帯のプールから出る (焚き火・工房は敵なし)', () => {
    for (const seed of SEEDS.slice(0, 10)) {
      const map = mapFor(seed)
      map.forEach((row, r) => {
        for (const node of row) {
          if (node.type === 'battle' || node.type === 'elite' || node.type === 'boss') {
            expect(node.encounterId).not.toBeNull()
            const pool = node.type === 'elite' ? ELITE_POOLS[0] : tierFor(1, r)
            expect(pool).toContain(node.encounterId)
          } else {
            expect(node.encounterId).toBeNull()
          }
        }
      })
    }
  })
})
