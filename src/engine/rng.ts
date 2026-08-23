// engine/rng.ts — シード付き決定論 RNG (mulberry32 ベースの純関数版)
// 同じシード + 同じコマンド列 = 同じ結果 を保証するため、
// RNG の状態は GameState に持たせ、消費は必ず next() の戻り値で差し替える。

import type { RngState } from './types.ts'

export function createRng(seed: number): RngState {
  return { seed: seed >>> 0, counter: 0 }
}

/** [0, 1) の乱数を1つ消費。(値, 次の状態) を返す純関数 */
export function next(rng: RngState): readonly [number, RngState] {
  // mulberry32: seed + counter からストリームの counter 番目を直接計算する
  let t = (rng.seed + 0x6d2b79f5 * (rng.counter + 1)) >>> 0
  t = Math.imul(t ^ (t >>> 15), t | 1)
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
  const value = ((t ^ (t >>> 14)) >>> 0) / 4294967296
  return [value, { seed: rng.seed, counter: rng.counter + 1 }]
}

/** [min, max] の整数を1つ消費 (敵の幅威力ロールなどに使用) */
export function nextInt(rng: RngState, min: number, max: number): readonly [number, RngState] {
  const [v, nextState] = next(rng)
  return [min + Math.floor(v * (max - min + 1)), nextState]
}

/** 配列をシャッフル (Fisher–Yates)。デッキシャッフル用 */
export function shuffle<T>(rng: RngState, items: readonly T[]): readonly [readonly T[], RngState] {
  const result = [...items]
  let state = rng
  for (let i = result.length - 1; i > 0; i--) {
    const [j, s] = nextInt(state, 0, i)
    state = s
    ;[result[i], result[j]] = [result[j], result[i]]
  }
  return [result, state]
}
