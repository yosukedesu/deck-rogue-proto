// ゴールデンマスターの要約・ハッシュ (Unity移植の等価性契約) の機械固定
import { describe, expect, it } from 'vitest'
import { fnv1a32, runHash } from '../engine/golden.ts'
import { replayStates } from '../engine/run.ts'
import { generateGolden, verifyGolden } from './golden-driver.ts'

describe('FNV-1a 32bit (C# 側と同じ定数・UTF-8 バイト列)', () => {
  it('既知ベクトル: 空文字=0x811c9dc5・"a"=0xe40c292c・日本語も UTF-8 バイトで', () => {
    expect(fnv1a32('')).toBe(0x811c9dc5)
    expect(fnv1a32('a')).toBe(0xe40c292c)
    expect(fnv1a32('foobar')).toBe(0xbf9cf968)
    expect(fnv1a32('伏せ')).toBe(fnv1a32('伏せ')) // 決定的
  })
})

describe('ゴールデンの生成と照合', () => {
  it('同じシードのランは同じハッシュ列になり、再生で全一致する', { timeout: 60000 }, () => {
    const g = generateGolden(9001, 'leader_green', 300)
    expect(g.commands.length).toBeGreaterThan(50)
    expect(g.hashes).toHaveLength(g.commands.length)
    const again = generateGolden(9001, 'leader_green', 300)
    expect(again.hashes).toEqual(g.hashes)
    const r = verifyGolden(g, (origin, commands) => replayStates({ origin, commands }))
    expect(r.ok, r.error ?? '').toBe(true)
  })

  it('ハッシュは状態の差に反応する (HPを1変えると変わる)', () => {
    const g = generateGolden(9002, 'leader_green', 20)
    const { states } = replayStates({ origin: g.origin, commands: g.commands })
    const last = states[states.length - 1]
    expect(runHash(last)).toBe(g.hashes[g.hashes.length - 1])
    expect(runHash({ ...last, hp: last.hp - 1 })).not.toBe(g.hashes[g.hashes.length - 1])
  })
})
