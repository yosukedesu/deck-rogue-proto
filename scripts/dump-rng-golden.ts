// scripts/dump-rng-golden.ts — RNGゴールデンマスターのダンプ (Unity移植 P0スパイク)
// TSのrng.tsが生む系列を整数としてダンプし、C#移植 (unity/) が全一致することを検証する。
// next() の値は value*2^32 (= mulberry32の内部uint32) で吐くため浮動小数点比較を回避できる。
// 実行: npx tsx scripts/dump-rng-golden.ts > goldens/rng-golden.json

import { createRng, next, nextInt, shuffle, weightedIndex } from '../src/engine/rng.ts'
import type { RngState } from '../src/engine/types.ts'

const NEXT_SEEDS = [42, 7, 20260824, 123456789]
const NEXT_COUNT = 25000 // 4シード×25,000 = 100,000個

function dumpNext(seed: number): number[] {
  let rng: RngState = createRng(seed)
  const out: number[] = []
  for (let i = 0; i < NEXT_COUNT; i++) {
    const [v, s] = next(rng)
    rng = s
    out.push(v * 4294967296) // 内部uint32を復元 (正確に整数)
  }
  return out
}

function dumpNextInt(): number[] {
  const ranges: readonly (readonly [number, number])[] = [
    [1, 6],
    [0, 99],
    [5, 7],
    [0, 1],
    [3, 17],
  ]
  let rng: RngState = createRng(1001)
  const out: number[] = []
  for (let i = 0; i < 10000; i++) {
    const [min, max] = ranges[i % ranges.length]
    const [v, s] = nextInt(rng, min, max)
    rng = s
    out.push(v)
  }
  return out
}

function dumpWeighted(): number[] {
  const tables: readonly (readonly number[])[] = [[1], [1, 1], [3, 1], [1, 2, 1], [5, 1, 1, 1]]
  let rng: RngState = createRng(2002)
  const out: number[] = []
  for (let i = 0; i < 10000; i++) {
    const [v, s] = weightedIndex(rng, tables[i % tables.length])
    rng = s
    out.push(v)
  }
  return out
}

function dumpShuffle(): number[][] {
  let rng: RngState = createRng(3003)
  const out: number[][] = []
  for (let size = 1; size <= 40; size++) {
    for (let rep = 0; rep < 5; rep++) {
      const items = Array.from({ length: size }, (_, i) => i)
      const [shuffled, s] = shuffle(rng, items)
      rng = s
      out.push([...shuffled])
    }
  }
  return out
}

const golden = {
  meta: {
    generated: '2026-08-24',
    source: 'src/engine/rng.ts (mulberry32)',
    note: 'next は value*2^32 (整数)。C#側は unity/EngineTests で照合',
  },
  next: Object.fromEntries(NEXT_SEEDS.map((s) => [String(s), dumpNext(s)])),
  nextInt: dumpNextInt(),
  weightedIndex: dumpWeighted(),
  shuffle: dumpShuffle(),
}

console.log(JSON.stringify(golden))
