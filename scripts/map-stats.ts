// scripts/map-stats.ts — マップ生成の分布計測 (本家StSの水準と突き合わせるための物差し)
//
//   npx tsx scripts/map-stats.ts [シード数=200]
//
// 出すもの:
//   - ノード種別の構成比 (本家の抽選ウェイトと比べる。本家は Monster45%/?22%/Elite16%/Rest12%/Shop3%/Treasure2%)
//   - **1本のパスを通した時の内訳**の分布 (戦闘数・?の数)。マップは「選べるのはリスクの種類」なので、
//     総数より「1ランで実際に何を踏むか」が本質
//   - 各パス指標の最小/中央値/最大 (最小値は保証の検証に使う)

import { generateMap, MAP_ROWS, BOSS_ROW } from '../src/engine/map.ts'
import type { RunMap } from '../src/engine/map.ts'
import { createRng } from '../src/engine/rng.ts'

const SEEDS = Number(process.argv[2] ?? 200)

/** 全パスを走査して「1パスあたりの種別ごとの踏む数」の最小・最大を DP で出す */
function pathExtremes(map: RunMap, pick: (t: string) => number): { min: number; max: number } {
  // 行0から各ノードへ到達する経路の累積値 (min/max) を前向きに畳む
  let min = map[0].map((n) => pick(n.type))
  let max = map[0].map((n) => pick(n.type))
  for (let r = 0; r < MAP_ROWS - 1; r++) {
    const nextMin = new Array<number>(map[r + 1].length).fill(Infinity)
    const nextMax = new Array<number>(map[r + 1].length).fill(-Infinity)
    for (let c = 0; c < map[r].length; c++) {
      for (const to of map[r][c].next) {
        const w = pick(map[r + 1][to].type)
        nextMin[to] = Math.min(nextMin[to], min[c] + w)
        nextMax[to] = Math.max(nextMax[to], max[c] + w)
      }
    }
    min = nextMin
    max = nextMax
  }
  return { min: Math.min(...min), max: Math.max(...max) }
}

const pct = (n: number, d: number) => `${((n / d) * 100).toFixed(1)}%`
const median = (xs: number[]) => [...xs].sort((a, b) => a - b)[Math.floor(xs.length / 2)]

function main(): void {
  for (const act of [1, 2, 3]) {
    const typeCount: Record<string, number> = {}
    let totalNodes = 0
    const battleMins: number[] = []
    const battleMaxs: number[] = []
    const eventMaxs: number[] = []
    const eventMins: number[] = []
    for (let s = 0; s < SEEDS; s++) {
      const [map] = generateMap(createRng(1000 + s), act, true)
      for (let r = 0; r <= BOSS_ROW; r++) {
        for (const n of map[r]) {
          typeCount[n.type] = (typeCount[n.type] ?? 0) + 1
          totalNodes++
        }
      }
      // 戦闘 = battle / elite / boss (ボスは全パス共通なので比較時は -1 して読む)
      const b = pathExtremes(map, (t) => (t === 'battle' || t === 'elite' || t === 'boss' ? 1 : 0))
      battleMins.push(b.min)
      battleMaxs.push(b.max)
      const e = pathExtremes(map, (t) => (t === 'event' ? 1 : 0))
      eventMins.push(e.min)
      eventMaxs.push(e.max)
    }
    console.log(`\n=== 第${act}幕 (${SEEDS}シード) ===`)
    console.log(`総ノード ${(totalNodes / SEEDS).toFixed(1)}/幕`)
    const order = ['battle', 'elite', 'event', 'campfire', 'shop', 'workshop', 'boss']
    console.log(
      '構成比: ' +
        order
          .filter((t) => typeCount[t])
          .map((t) => `${t} ${pct(typeCount[t], totalNodes)}(${(typeCount[t] / SEEDS).toFixed(1)})`)
          .join(' / '),
    )
    console.log(
      `1パスの戦闘数: 最小 ${Math.min(...battleMins)} / 中央(最小値の) ${median(battleMins)} / 最大 ${Math.max(...battleMaxs)}`,
    )
    console.log(
      `1パスの?の数: 最小 ${Math.min(...eventMins)} / 中央(最大値の) ${median(eventMaxs)} / 最大 ${Math.max(...eventMaxs)}`,
    )
  }
}

main()
