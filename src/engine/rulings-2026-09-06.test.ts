// 2026-09-06 人間ラン#7a/#7b のメモへの裁定: 静かな鈴−2／罠師の茂み1E常在／歩き木の重圧は攻撃札だけ／代償なしのボスレリック2つ
import { describe, expect, it } from 'vitest'
import { getCardDef, getEnemyDef, getRelicDef } from './content.ts'
import { splitChildHp } from './summary.ts'
import { effectiveCost } from './effects.ts'
import { applyRunCommand, relicBonusSum } from './run.ts'
import type { RunState } from './run.ts'
import { createRunInBattle, freshCombat, withHand } from './test-helpers.ts'

describe('静かな鈴・罠師の茂み・歩き木 (2026-09-06 裁定)', () => {
  it('静かな鈴は−2', () => {
    expect(getRelicDef('relic_quiet_bell').combatRule?.setDamageReduction).toBe(2)
  })
  it('罠師の茂みは1E置物「伏せ枠+1」だけ (ブロック4を外した)', () => {
    const def = getCardDef('green_perm_trapper_grove')
    expect(def.cost).toBe(1)
    expect(def.effects).toEqual([{ trigger: 'onPlay', effect: 'gainSetSlot', amount: 1 }])
  })
  it('歩き木の重圧はダメージを与える札だけ+1 (防御は素のまま。モードにダメージがある絡み蔦は対象)', () => {
    expect(getEnemyDef('enemy_vine_walker').aura?.attacksOnly).toBe(true)
    const s = withHand(freshCombat('set-confirm', 'enemy_vine_walker', 42), ['green_strike', 'green_guard', 'green_entangle'])
    expect(effectiveCost(s, s.player.hand[0])).toBe(2)
    expect(effectiveCost(s, s.player.hand[1])).toBe(1)
    expect(effectiveCost(s, s.player.hand[2])).toBe(2)
  })
})

describe('代償なしのボスレリック (2026-09-06 人間#7b「全部デメリット付きだと嬉しくない」)', () => {
  it('小さな家: 取った時に最大HP+8・+60G・鍛えられる札を1枚ランダムに鍛える (ラン RNG を消費)', () => {
    const base = createRunInBattle(11, 'set-confirm', 'leader_green')
    const run: RunState = { ...base, phase: 'relic-reward', relicOptions: ['relic_tiny_house'], combat: null }
    const next = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(next.relics).toContain('relic_tiny_house')
    expect(next.maxHp).toBe(base.maxHp + 8)
    expect(next.gold).toBe(base.gold + 60)
    expect(next.deck.filter((c) => c.def.name.endsWith('+')).length).toBe(1)
    expect(next.rng.counter).toBeGreaterThan(base.rng.counter)
    expect(next.phase).toBe('map')
  })
  it('黒星の欠片: 強個体の3択から2つ取れる (1つ目の後も残り2つで relic-reward に留まり、2つ目でカード報酬へ)', () => {
    const base = createRunInBattle(11, 'set-confirm', 'leader_green')
    expect(relicBonusSum({ ...base, relics: ['relic_black_star'] }, 'eliteRelicPicks')).toBe(1)
    const run: RunState = {
      ...base,
      relics: ['relic_black_star'],
      phase: 'relic-reward',
      relicOptions: ['relic_growth_seed', 'relic_swift_boots', 'relic_shield_shard'],
      currentElite: true,
      relicPicksLeft: 2,
    }
    const one = applyRunCommand(run, { type: 'PickRelic', index: 1 })
    expect(one.phase).toBe('relic-reward')
    expect(one.relicOptions).toEqual(['relic_growth_seed', 'relic_shield_shard'])
    expect(one.relicPicksLeft).toBe(1)
    const two = applyRunCommand(one, { type: 'PickRelic', index: 0 })
    expect(two.relics).toEqual(['relic_black_star', 'relic_swift_boots', 'relic_growth_seed'])
    expect(two.phase).toBe('reward')
    expect(two.relicPicksLeft).toBeUndefined()
    // 黒星なし (relicPicksLeft 省略) は従来どおり1つで報酬へ
    const plain = applyRunCommand({ ...run, relics: [], relicPicksLeft: undefined }, { type: 'PickRelic', index: 0 })
    expect(plain.phase).toBe('reward')
  })
})

describe('残機の予告HP (2026-09-06 人間ラン#8「復活するときの体力の説明が違う」)', () => {
  it('分裂体の予告HPは素の値×親の倍率 (幕3ボス×2.4: 二の相55→132・三の相70→168)', () => {
    const c1 = getEnemyDef('enemy_chimera_1')
    const c2 = getEnemyDef('enemy_chimera_2')
    const c3 = getEnemyDef('enemy_chimera_3')
    expect(splitChildHp({ maxHp: Math.round(c1.maxHp * 2.4) }, c1, c2)).toBe(Math.round(c2.maxHp * 2.4))
    expect(splitChildHp({ maxHp: Math.round(c2.maxHp * 2.4) }, c2, c3)).toBe(Math.round(c3.maxHp * 2.4))
    expect(splitChildHp({ maxHp: c1.maxHp }, c1, c2)).toBe(c2.maxHp)
  })
})

describe('金羽の大鴉の作り直し (2026-09-06 ユーザー「そもそもエリートがやる行動じゃない」)', () => {
  it('盗み・逃走を持たず、舞い上がる(防御8+筋力1)→急降下14〜18(+4/回)→つつき の時計', () => {
    const def = getEnemyDef('enemy_elite_gold_raven')
    expect(def.moves.some((m) => m.kind === 'steal-gold' || m.kind === 'flee')).toBe(false)
    expect(def.sequence).toEqual(['soar', 'dive', 'peck'])
    const dive = def.moves.find((m) => m.id === 'dive')
    expect(dive?.growPerUse).toBe(4)
    expect(def.moves.find((m) => m.id === 'soar')?.alsoBuff).toBe(1)
    expect(def.startingBlock).toBeUndefined()
  })
})

describe('ドキュメントの空ファイル化を止める (2026-09-06 スクリプト事故で CLAUDE.md と unity-port.md が空になった)', () => {
  it('CLAUDE.md と docs/unity-port.md は100行以上ある', async () => {
    // src の tsconfig は node の型を持たないので、モジュール名を実行時に組み立てて型解決を避ける (vitest は node で動く)
    const { readFileSync } = (await import(/* @vite-ignore */ 'node:' + 'fs')) as { readFileSync: (p: string, enc: string) => string }
    expect(readFileSync('CLAUDE.md', 'utf-8').split('\n').length).toBeGreaterThan(100)
    expect(readFileSync('docs/unity-port.md', 'utf-8').split('\n').length).toBeGreaterThan(100)
  })
})

