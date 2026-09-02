// マップ生成のテスト。確定済みルール表「マップ」を機械的に固定する。
// - 18行・行17=ボス固定・強制焚き火行は16のみ (2026-08-31 本家配置化。焚き火は部屋タイプ12%で散布)
// - 部屋タイプは本家の重みで員数化: 工房/ショップ=総ノードの5%・?=22%・焚き火=12%・エリートは員数固定4
// - 配置制約 (本家): 行0は全て戦闘 / エリートは行2以降 / 焚き火は行5以降 /
//   親と同タイプ禁止 (エリート・ショップ・工房・焚き火) / 兄弟と同タイプ禁止 (全種)
// - 戦闘数の保証は無い (2026-08-31 床8撤廃・本家完全準拠 =「何回戦うか」を選べる)
// - 分岐の補強: 出次数1のノードに非交差エッジを1本足す / エリートは全親に出口2以上の位置のみ
// - エッジは全ノード到達可能 (開始から到達でき、ボスへ到達できる)
import { describe, expect, it } from 'vitest'
import { createRng } from './rng.ts'
import { resolveEncounter } from './content.ts'
import { ACT_BOSSES, ACT_MAP_ROWS, BOSS_ROW, bossRowFor, ELITE_POOLS, FORCED_CAMPFIRE_ROWS, generateMap, MAP_ROWS, mapRowsFor, tierFor, TREASURE_ROW, treasureRowFor } from './map.ts'
import type { RunMap } from './map.ts'

const SEEDS = Array.from({ length: 40 }, (_, i) => i + 1)

// 生成はパスウォーク化で1枚数十msかかる。テストごとに引き直さずシード単位でキャッシュする
const mapCache = new Map<number, RunMap>()
function mapFor(seed: number): RunMap {
  let m = mapCache.get(seed)
  if (m === undefined) {
    m = generateMap(createRng(seed))[0]
    mapCache.set(seed, m)
  }
  return m
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

  it('18行・行17はボス単体・行16 (ボス前) は全ノード焚き火', () => {
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

  it('宝箱行 (行9) は全ノード宝箱で、他の行に宝箱は無い (2026-08-31)', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      expect(map[TREASURE_ROW].every((n) => n.type === 'treasure'), `seed${seed}`).toBe(true)
      map.forEach((row, r) => {
        if (r === TREASURE_ROW) return
        expect(row.some((n) => n.type === 'treasure'), `seed${seed} row${r} に宝箱`).toBe(false)
      })
    }
  })

  it('焚き火は本家配置: 散布12%・行5未満と行15に置かれない (2026-08-31)', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const total = map.flat().length
      // 散布分 = 強制行16を除いた焚き火の員数
      const scattered = map
        .filter((_, r) => !FORCED_CAMPFIRE_ROWS.has(r))
        .flat()
        .filter((n) => n.type === 'campfire')
      // 12%→8% (2026-08-31 ユーザー指示「焚き火減らして」。回復25%とセットのHP経済の絞り)
      expect(scattered, `seed${seed}`).toHaveLength(Math.round(total * 0.08))
      for (let r = 0; r < 5; r++) {
        expect(map[r].some((n) => n.type === 'campfire'), `seed${seed} row${r} に焚き火`).toBe(false)
      }
      // 行15は行16 (全ノード焚き火) の親なので、親同種禁止により焚き火が置かれない (本家の13階禁止)
      expect(map[15].some((n) => n.type === 'campfire'), `seed${seed} row15 に焚き火`).toBe(false)
    }
  })

  it('部屋タイプの員数: 工房=幕1は1個・ショップは固定3・?は18〜26%・エリートちょうど4', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const all = map.flat()
      const total = all.length
      // 工房: 幕1 (mapFor の既定) はちょうど1個 (2026-08-31 ユーザー指示「合成1幕に1個つけて」)
      expect(all.filter((n) => n.type === 'workshop'), `seed${seed}`).toHaveLength(1)
      expect(all.filter((n) => n.type === 'shop'), `seed${seed}`).toHaveLength(3) // 固定3/幕 (2026-09-02 StS2式)
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

  it('親と同タイプにならない (エリート・ショップ・工房・焚き火)。?と戦闘は縦に続いてよい', () => {
    const exclusive = new Set(['elite', 'shop', 'workshop', 'campfire'])
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

  it('焚き火ハシゴの上限: どのパスも焚き火4個以下 (ボス前行込み。2026-08-31 ユーザー裁定)', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      let v: number[] = map[0].map((n) => (n.type === 'campfire' ? 1 : 0))
      for (let r = 0; r < MAP_ROWS - 1; r++) {
        const next = new Array<number>(map[r + 1].length).fill(-Infinity)
        map[r].forEach((n, c) => {
          for (const to of n.next) {
            next[to] = Math.max(next[to], v[c] + (map[r + 1][to].type === 'campfire' ? 1 : 0))
          }
        })
        v = next
      }
      expect(v[0], `seed${seed}`).toBeLessThanOrEqual(4)
    }
  })

  it('エリートは直前で必ず避けられる (全ての親に出口2以上。2026-08-31 Opus検証への処方)', () => {
    for (const seed of SEEDS) {
      const map = mapFor(seed)
      const parents = parentsOf(map)
      for (let r = 1; r < MAP_ROWS - 1; r++) {
        map[r].forEach((n, c) => {
          if (n.type !== 'elite') return
          for (const p of parents[r][c]) {
            expect(
              map[r - 1][p].next.length,
              `seed${seed} row${r} のエリートが回避不能`,
            ).toBeGreaterThanOrEqual(2)
          }
        })
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
      expect(m[bossRowFor(act)][0].encounterId).toBe(ACT_BOSSES[act - 1])
    }
  })

  it('緑を含まないランでは工房ノードを生成しない (v1は緑同士のみのため)', () => {
    for (const seed of SEEDS.slice(0, 10)) {
      const [m] = generateMap(createRng(seed), 1, false)
      const all = m.flat()
      expect(all.filter((n) => n.type === 'workshop')).toHaveLength(0)
      expect(all.filter((n) => n.type === 'shop')).toHaveLength(3) // 固定3/幕 (2026-09-02 StS2式))
      // ?は色で非対称にならない (旧実装は緑ランだけ?が1個に縮退していた)
      const ratio = all.filter((n) => n.type === 'event').length / all.length
      expect(ratio, `seed${seed}`).toBeGreaterThanOrEqual(0.18)
    }
  })

  it('60シード×3幕すべてでマップ生成が収束する (throw しない)', { timeout: 60000 }, () => {
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

describe('幕別の行数 (2026-09-02 ユーザー裁定「StS2式 15/14/13」)', () => {
  it('行数は幕1=16・幕2=15・幕3=14 (部屋数15/14/13+ボス行)。宝箱行はボスの7行手前・ボス前行は全焚き火', () => {
    expect(ACT_MAP_ROWS).toEqual([16, 15, 14])
    for (let act = 1; act <= 3; act++) {
      for (const seed of [11, 22, 33]) {
        const [m] = generateMap(createRng(seed), act)
        expect(m).toHaveLength(mapRowsFor(act))
        const boss = bossRowFor(act)
        expect(m[boss]).toHaveLength(1)
        expect(m[boss][0].type).toBe('boss')
        expect(m[boss - 1].every((n) => n.type === 'campfire')).toBe(true)
        expect(treasureRowFor(act)).toBe(boss - 7)
        expect(m[treasureRowFor(act)].every((n) => n.type === 'treasure')).toBe(true)
        expect(m[0].every((n) => n.type === 'battle')).toBe(true)
      }
    }
  })
})

describe('StS2式の抽選改善 (2026-09-02 全体改善)', () => {
  it('Weak帯: 幕頭N行 (3/2/2) の戦闘は教師枠の弱プールからだけ出る', () => {
    const weakRows = [3, 2, 2]
    for (let act = 1; act <= 3; act++) {
      for (const seed of [5, 15, 25, 35]) {
        const [m] = generateMap(createRng(seed), act)
        for (let r = 0; r < weakRows[act - 1]; r++) {
          for (const n of m[r]) {
            if (n.type !== 'battle' || n.encounterId === null) continue
            expect(tierFor(act, r), `act${act} seed${seed} row${r}`).toContain(n.encounterId)
            expect(tierFor(act, r).length).toBeLessThanOrEqual(4) // 弱プール = 小さな教師枠
          }
        }
      }
    }
  })

  it('同族連続の回避: 同じ敵をメンバーに含む編成が2行連続しない (フォールバック除く)', () => {
    let violations = 0
    let checked = 0
    for (const seed of [1, 2, 3, 4, 5, 6, 7, 8]) {
      for (let act = 1; act <= 3; act++) {
        const [m] = generateMap(createRng(seed * 31 + act), act)
        const members = (id: string) => new Set(resolveEncounter(id).map((x) => x.enemyId))
        // Weak帯は教師枠の小プールなので隣接反復は既知として除外 (プレイヤーは1行1ノードしか踏まない)
        const weakRows = [3, 2, 2][act - 1]
        for (let r = weakRows + 1; r < m.length; r++) {
          for (const n of m[r]) {
            if (n.type !== 'battle' || n.encounterId === null) continue
            const my = members(n.encounterId)
            for (const prev of m[r - 1]) {
              if (prev.type !== 'battle' || prev.encounterId === null) continue
              checked++
              if ([...members(prev.encounterId)].some((x) => my.has(x))) violations++
            }
          }
        }
      }
    }
    expect(checked).toBeGreaterThan(100)
    expect(violations / checked).toBeLessThan(0.06) // プール枯渇のフォールバックのみ許容
  })

  it('ボス前3行に散布焚き火が無い (ボス前の全焚き火行を除く)', () => {
    for (let act = 1; act <= 3; act++) {
      for (const seed of [3, 13, 23]) {
        const [m] = generateMap(createRng(seed), act)
        const boss = bossRowFor(act)
        for (let r = boss - 3; r < boss - 1; r++) {
          expect(m[r].every((n) => n.type !== 'campfire'), `act${act} row${r}`).toBe(true)
        }
      }
    }
  })
})

describe('経済・マップの裁定 (2026-09-02 残件議論)', () => {
  it('ショップは固定3/幕で、行0のどの開始ノードからもショップを踏める経路が存在する', () => {
    for (let act = 1; act <= 3; act++) {
      for (const seed of [8, 18, 28, 38, 48]) {
        const [m] = generateMap(createRng(seed), act)
        const shops = m.flat().filter((n) => n.type === 'shop').length
        expect(shops, `act${act} seed${seed}`).toBe(3)
        // 到達保証: 各開始ノードから前向きに辿ってショップに届くか
        for (let c = 0; c < m[0].length; c++) {
          let frontier = new Set([c])
          let found = false
          for (let r = 0; r < m.length - 1 && !found; r++) {
            const next = new Set<number>()
            for (const i of frontier) {
              if (m[r][i].type === 'shop') found = true
              for (const to of m[r][i].next) next.add(to)
            }
            frontier = next
          }
          expect(found, `act${act} seed${seed} start${c} からショップに届かない`).toBe(true)
        }
      }
    }
  })

  it('幕1のエリートは行4以降 (幕2/3は行2以降)', () => {
    for (const seed of [8, 18, 28, 38]) {
      const [m1] = generateMap(createRng(seed), 1)
      m1.forEach((row, r) => row.forEach((n) => { if (n.type === 'elite') expect(r).toBeGreaterThanOrEqual(4) }))
      const [m2] = generateMap(createRng(seed), 2)
      m2.forEach((row, r) => row.forEach((n) => { if (n.type === 'elite') expect(r).toBeGreaterThanOrEqual(2) }))
    }
  })
})



