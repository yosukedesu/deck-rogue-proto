// 決定論の土台テスト: 同じシード = 同じ結果 (CLAUDE.md「アーキテクチャ原則 3」)
import { describe, expect, it } from 'vitest'
import { createRng, next, nextInt, shuffle } from './rng.ts'

describe('シード付きRNG', () => {
  it('同じシードから同じ乱数列が出る', () => {
    let a = createRng(42)
    let b = createRng(42)
    for (let i = 0; i < 100; i++) {
      const [va, na] = next(a)
      const [vb, nb] = next(b)
      expect(va).toBe(vb)
      a = na
      b = nb
    }
  })

  it('異なるシードは異なる列を出す', () => {
    const [va] = next(createRng(1))
    const [vb] = next(createRng(2))
    expect(va).not.toBe(vb)
  })

  it('next は元の状態を変更しない (イミュータブル)', () => {
    const rng = createRng(7)
    next(rng)
    expect(rng.counter).toBe(0)
  })

  it('nextInt は [min, max] に収まる', () => {
    let rng = createRng(123)
    for (let i = 0; i < 500; i++) {
      const [v, s] = nextInt(rng, 6, 12) // 敵の意図の幅表示 (攻撃6〜12) を想定
      expect(v).toBeGreaterThanOrEqual(6)
      expect(v).toBeLessThanOrEqual(12)
      expect(Number.isInteger(v)).toBe(true)
      rng = s
    }
  })

  it('shuffle は同じシードで同じ並び・要素は不変', () => {
    const deck = ['a', 'b', 'c', 'd', 'e', 'f', 'g']
    const [r1] = shuffle(createRng(99), deck)
    const [r2] = shuffle(createRng(99), deck)
    expect(r1).toEqual(r2)
    expect([...r1].sort()).toEqual([...deck].sort())
    expect(deck).toEqual(['a', 'b', 'c', 'd', 'e', 'f', 'g'])
  })
})
