// レポート生成の頑健性 (2026-08-30)。
// 「書き出しがうまくいかない」の真犯人が「?マス発の戦闘が enemyId='unknown' でアーカイブされ、
// encounterName('unknown') が例外死する」だったための回帰テスト。
// レポートはプレイテストのデータ回収の道具なので、どんな状態でも絶対に落ちないことを固定する。
import { describe, expect, it } from 'vitest'
import { archiveBattle, buildCardProposals, buildReport, cardDraftToDefJson, isEmptyMark } from './report.ts'
import { createRun } from '../engine/run.ts'
import { freshCombat } from '../engine/test-helpers.ts'

describe('レポートは絶対に落ちない', () => {
  it('未知の敵ID (旧バックアップの unknown 等) を含む履歴でも buildReport が throw しない', () => {
    const run = createRun(7, 'set-confirm')
    const combat = freshCombat('set-confirm', 'enemy_probe', 42)
    const legacy = archiveBattle({ ...combat, phase: 'won' }, 1, 'unknown', false, 80, 10)
    const text = buildReport(run, null, [legacy])
    expect(text).toContain('unknown') // 落ちずに生のIDで出力される
  })

  it('通常の履歴では敵名が解決される', () => {
    const run = createRun(7, 'set-confirm')
    const combat = freshCombat('set-confirm', 'enemy_probe', 42)
    const a = archiveBattle({ ...combat, phase: 'won' }, 1, 'enemy_probe', false, 80, 10)
    const text = buildReport(run, null, [a])
    expect(text).toContain('探り屋')
  })
})

describe('プレイメモの同梱 (2026-09-01)', () => {
  it('メモがある時だけ「## プレイメモ」節が出て、文脈と本文が並ぶ', () => {
    const run = createRun(7, 'set-confirm')
    const notes = [
      { at: '2026-09-01T12:34:56.000Z', context: '幕1 行3 combat T2 2勝 HP70', text: '樽の負傷が地味に痛い' },
    ]
    const text = buildReport(run, null, [], '', notes)
    expect(text).toContain('## プレイメモ（1件')
    expect(text).toContain('[12:34 幕1 行3 combat T2 2勝 HP70] 樽の負傷が地味に痛い')
    // メモ無しなら節ごと出ない
    expect(buildReport(run, null, [])).not.toContain('## プレイメモ')
  })
})

describe('カード調整案の提案書 (2026-09-01 カード調整サイクル)', () => {
  it('構造化マーク (コスト/レア/消滅/数値) が「現行→提案」の差分行になる', () => {
    const text = buildCardProposals(
      {
        green_strike: {
          cost: '0',
          rarity: 'uncommon',
          exhaust: true,
          fields: { 'e0.amount': 9 },
          change: '基本札の上方修正の実験',
        },
        green_fang: { remove: true, change: '成長とダブつく' },
        blue_strike: {}, // マーク無し = 無視される
      },
      '緑1E: 5ダメ+成長1の入口コモン',
    )
    expect(text).toContain('## 変更案（1件）')
    expect(text).toContain('`green_strike`')
    expect(text).toContain('コスト: 1 → 0')
    expect(text).toContain('レアリティ: common → uncommon')
    expect(text).toContain('消滅: なし → あり')
    expect(text).toContain('効果1〔onPlay/dealDamage〕の量: 6 → 9')
    expect(text).toContain('補足: 基本札の上方修正の実験')
    expect(text).toContain('## 削除案（1件）')
    expect(text).toContain('`green_fang`')
    expect(text).toContain('補足: 成長とダブつく')
    expect(text).toContain('緑1E: 5ダメ+成長1の入口コモン')
    expect(text).not.toContain('blue_strike')
    // 現行スペックが機械可読で入る (レビューの根拠)
    expect(text).toContain('"dealDamage"')
  })

  it('isEmptyMark: 提案の無いマークだけが空と判定される', () => {
    expect(isEmptyMark({})).toBe(true)
    expect(isEmptyMark({ change: '  ' })).toBe(true)
    expect(isEmptyMark({ fields: {} })).toBe(true)
    expect(isEmptyMark({ cost: '0' })).toBe(false)
    expect(isEmptyMark({ remove: true })).toBe(false)
    expect(isEmptyMark({ fields: { 'e0.amount': 9 } })).toBe(false)
  })

  it('現行データに存在しないID (統合で消えた札の下書き) でも落ちない', () => {
    const text = buildCardProposals({ black_agony_strike: { change: 'コスト下げたい' } }, '')
    expect(text).toContain('現行データに存在しない')
  })
})

describe('カードビルダー (実データとして作れるレベル 2026-09-01)', () => {
  it('cardDraftToDefJson: 下書きが cards.*.json のエントリ形に落ちる (未使用フィールドは落とす)', () => {
    const j = cardDraftToDefJson({
      name: '試作の一撃',
      color: 'green',
      cost: 2,
      type: 'physical',
      rarity: 'uncommon',
      exhaust: true,
      discardCost: 1,
      effects: [
        { trigger: 'onPlay', effect: 'dealDamage', amount: 12, pierce: true, condKey: 'minActionValue', condValue: 10 },
        { trigger: 'onPlay', effect: 'addGrowth', amount: 2 },
      ],
    })
    expect(j).toEqual({
      id: 'green_TODO_命名',
      name: '試作の一撃',
      cost: 2,
      type: 'physical',
      rarity: 'uncommon',
      effects: [
        { trigger: 'onPlay', effect: 'dealDamage', amount: 12, pierce: true, condition: { minActionValue: 10 } },
        { trigger: 'onPlay', effect: 'addGrowth', amount: 2 },
      ],
      exhaust: true,
      discardCost: 1,
      color: 'green',
    })
  })

  it('blaze条件は {blaze:true} に落ち、新カード案と差し替え定義がJSONブロックでmdに入る', () => {
    const draft = {
      name: '猛り火の試作',
      color: 'red',
      cost: 1,
      type: 'spell',
      rarity: 'common',
      effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 8, condKey: 'blaze' }],
    }
    const text = buildCardProposals(
      { green_strike: { redef: { ...draft, name: '打撃・改', color: 'green' } } },
      '',
      [draft],
    )
    expect(text).toContain('"blaze": true')
    expect(text).toContain('## 新カード案（1件）')
    expect(text).toContain('### 猛り火の試作')
    expect(text).toContain('定義ごと差し替え')
    expect(text).toContain('打撃・改')
  })
})
