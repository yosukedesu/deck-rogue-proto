// マップ生成のテスト。確定済みルール表「マップ」を機械的に固定する。
// - 16行・行15=ボス固定・強制焚き火行5/10/14
// - 工房×2 (行4〜12)・エリート×4 (行2〜13)。1行につき特別ノードは1つまで=必ず戦闘の代替がある
// - どのパスも戦闘数10〜12 (DPで全パスの最小/最大を検証)
// - エッジは全ノード到達可能 (開始から到達でき、ボスへ到達できる)
import { describe, expect, it } from 'vitest'
import { createRng } from './rng.ts'
import { BOSS_ROW, FORCED_CAMPFIRE_ROWS, generateMap, MAP_ROWS, tierForRow } from './map.ts'
import type { RunMap } from './map.ts'

const SEEDS = Array.from({ length: 40 }, (_, i) => i + 1)

function mapFor(seed: number): RunMap {
  return generateMap(createRng(seed))[0]
}

describe('マップ生成の構造', () => {
  it('決定論: 同じシードは同じマップ', () => {
    expect(JSON.stringify(mapFor(7))).toBe(JSON.stringify(mapFor(7)))
  })

  it('16行・行15はボス単体・強制焚き火行 (5/10/14) は全ノード焚き火', () => {
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

  it('工房×2・エリート×4。特別ノードの行には必ず戦闘の代替がある', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const all = map.flat()
      expect(all.filter((n) => n.type === 'workshop')).toHaveLength(2)
      expect(all.filter((n) => n.type === 'elite')).toHaveLength(4)
      map.forEach((row, r) => {
        const specials = row.filter((n) => n.type === 'workshop' || n.type === 'elite')
        if (specials.length > 0) {
          expect(specials, `seed${seed} row${r}`).toHaveLength(1) // 1行に1つまで
          expect(row.some((n) => n.type === 'battle'), `seed${seed} row${r}`).toBe(true)
        }
      })
    }
  })

  it('どのパスも戦闘数 (エリート込み・ボス除く) が10〜12に収まる', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      // DP: 各ノードに到達するパスの戦闘数の最小/最大
      let minC: number[] = map[0].map((n) => (n.type === 'battle' || n.type === 'elite' ? 1 : 0))
      let maxC: number[] = [...minC]
      for (let r = 0; r < BOSS_ROW; r++) {
        const nextMin = map[r + 1].map(() => Infinity)
        const nextMax = map[r + 1].map(() => -Infinity)
        map[r].forEach((node, c) => {
          for (const to of node.next) {
            const t = map[r + 1][to].type
            const combat = t === 'battle' || t === 'elite' ? 1 : 0
            nextMin[to] = Math.min(nextMin[to], minC[c] + combat)
            nextMax[to] = Math.max(nextMax[to], maxC[c] + combat)
          }
        })
        minC = nextMin
        maxC = nextMax
      }
      expect(minC[0], `seed${seed} 最少戦闘パス`).toBeGreaterThanOrEqual(10)
      expect(maxC[0], `seed${seed} 最多戦闘パス`).toBeLessThanOrEqual(12)
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

  it('戦闘ノードの敵は行の帯のプールから出る (焚き火・工房は敵なし)', () => {
    for (const seed of SEEDS.slice(0, 10)) {
      const map = mapFor(seed)
      map.forEach((row, r) => {
        for (const node of row) {
          if (node.type === 'battle' || node.type === 'elite' || node.type === 'boss') {
            expect(node.encounterId).not.toBeNull()
            expect(tierForRow(r)).toContain(node.encounterId)
          } else {
            expect(node.encounterId).toBeNull()
          }
        }
      })
    }
  })
})
