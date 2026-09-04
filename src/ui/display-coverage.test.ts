// 予告 (フェアネス) 表示の網羅性 (2026-09-02 残件議論の採択 #120)。
// EnemyDef のギミック系キーごとに ①KEYWORD_HELP に用語解説がある ②CLIのタグ関数がそのキーを言語化する
// ことを機械固定する。新しいギミックを足したら、用語解説とタグの両方を書かないとここで落ちる。
import { afterEach, describe, expect, it } from 'vitest'
import { applyDebugOverrides, clearDebugOverrides } from '../engine/content.ts'
import { ENEMY_GIMMICK_KEYS, GIMMICK_KEYWORDS, enemyTraitTags } from '../engine/traits.ts'
import { freshCombat } from '../engine/test-helpers.ts'
import type { EnemyDef } from '../engine/types.ts'
import { KEYWORD_HELP } from './keywordHelp.ts'

/** キーごとの最小サンプル値 (タグが出る値) */
const SAMPLE: Record<string, Partial<EnemyDef>> = {
  enrage: { enrage: 2 },
  enrageEveryCards: { enrage: 2, enrageEveryCards: 8 },
  enrageEveryDamage: { enrage: 2, enrageEveryDamage: 80 },
  regen: { regen: 5 },
  regenBreak: { regen: 5, regenBreak: 30 },
  burnResist: { burnResist: 2 },
  thorns: { thorns: 2 },
  armor: { armor: 20 },
  startingBlock: { startingBlock: 10 },
  angerOnBlock: { angerOnBlock: 1 },
  guardian: { guardian: true },
  bondStrength: { bondStrength: 2 },
  opener: { opener: 'poke' },
  phaseAfterUses: { phaseAfterUses: { moveId: 'poke', uses: 2, sequence: ['poke'] } },
  splitInto: { splitInto: { enemyId: 'enemy_moss_slime', count: 2 } },
  hatchInto: { hatchInto: { enemyId: 'enemy_raptor_chick' } },
  mournStrength: { mournStrength: 3 },
  aura: { aura: { costUp: 1 } },
  turnArmor: { turnArmor: 30 },
  artifact: { artifact: 1 },
  wakeOnDamage: { wakeOnDamage: { damage: 10, resumeAt: 2 }, sequence: ['poke', 'poke', 'poke'] },
  burrow: { burrow: { block: 8, bite: 'poke' } },
  nemesis: { nemesis: true },
  imbalanced: { imbalanced: true },
}

describe('予告表示の網羅性 (display-coverage)', () => {
  afterEach(() => clearDebugOverrides())

  it('全ギミックキーに用語解説 (KEYWORD_HELP) と CLI タグの両方がある', () => {
    const missingHelp: string[] = []
    const missingTag: string[] = []
    for (const key of ENEMY_GIMMICK_KEYS) {
      const term = GIMMICK_KEYWORDS[key]
      if (term !== null && KEYWORD_HELP[term] === undefined) missingHelp.push(`${key}→${term}`)
      if (term === null) continue // ローテの器は意図表示側で見える
      const def: EnemyDef = {
        id: `test_cov_${key}`,
        name: 'テスト',
        archetype: 'brute',
        maxHp: 50,
        moves: [{ id: 'poke', kind: 'attack', min: 3, max: 5, weight: 1 }],
        sequence: ['poke', 'poke'],
        ...(SAMPLE[key] ?? {}),
      }
      applyDebugOverrides({ enemies: [def] })
      const s = freshCombat('set-confirm', def.id, 1)
      const tags = enemyTraitTags(s, 0)
      // 状態に依存して消えるタグ (再生=HP半分超のみ・眠り=前奏中のみ) も初期状態では出る
      if (!tags.some((t) => t.includes(term))) missingTag.push(`${key}→${term}: [${tags.join(' | ')}]`)
      clearDebugOverrides()
    }
    expect(missingHelp, 'KEYWORD_HELP に用語解説が無いギミック').toEqual([])
    expect(missingTag, 'CLIタグがそのギミックを言語化していない').toEqual([])
  })

  it('育つ技 (growPerUse) の用語解説がある', () => {
    expect(KEYWORD_HELP['育つ技']).toBeDefined()
  })
})
