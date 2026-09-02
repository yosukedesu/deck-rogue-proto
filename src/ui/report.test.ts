// レポート生成の頑健性 (2026-08-30)。
// 「書き出しがうまくいかない」の真犯人が「?マス発の戦闘が enemyId='unknown' でアーカイブされ、
// encounterName('unknown') が例外死する」だったための回帰テスト。
// レポートはプレイテストのデータ回収の道具なので、どんな状態でも絶対に落ちないことを固定する。
import { describe, expect, it } from 'vitest'
import { applyCardMark, archiveBattle, buildOverrideDefs, buildProposals, buildReport, buildRunSaveFile, cardDraftToDefJson, describeRunChoice, isEmptyMark, replayInitialRun, replayStates } from './report.ts'
import { getEnemyDef } from '../engine/content.ts'
import { applyRunCommand, createRun, nextChoices } from '../engine/run.ts'
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

describe('カード調整案の提案書 (2026-09-01。同日ユーザー裁定で生JSON化)', () => {
  it('構造化マーク (コスト/レア/消滅/数値) が現行def同梱のJSONになる', () => {
    const doc = JSON.parse(
      buildProposals({
        cardMarks: {
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
        newCards: '緑1E: 5ダメ+成長1の入口コモン',
        newCardDefs: [],
      }),
    )
    expect(doc.kind).toBe('deck-rogue-tuning-proposals')
    expect(doc.fingerprint).toContain('cards')
    expect(doc.cards.changes).toHaveLength(1)
    const c = doc.cards.changes[0]
    expect(c.id).toBe('green_strike')
    expect(c.current.effects[0].effect).toBe('dealDamage') // 現行defが丸ごと同梱される
    expect(c.proposal).toEqual({ cost: '0', rarity: 'uncommon', exhaust: true, fields: { 'e0.amount': 9 } })
    expect(c.note).toBe('基本札の上方修正の実験')
    expect(doc.cards.removals).toHaveLength(1)
    expect(doc.cards.removals[0].id).toBe('green_fang')
    expect(doc.memo).toBe('緑1E: 5ダメ+成長1の入口コモン')
    expect(JSON.stringify(doc.cards)).not.toContain('blue_strike') // 空マークは節に出ない (sourceDraft原本には残る=読み戻し用)
  })

  it('現行データに存在しないID (統合で消えた札の下書き) でも落ちず、その旨がcurrentに入る', () => {
    const doc = JSON.parse(
      buildProposals({ cardMarks: { black_agony_strike: { change: 'コスト下げたい' } }, newCards: '', newCardDefs: [] }),
    )
    expect(doc.cards.changes[0].current).toContain('現行データに存在しない')
  })

  it('isEmptyMark: 提案の無いマークだけが空と判定される', () => {
    expect(isEmptyMark({})).toBe(true)
    expect(isEmptyMark({ change: '  ' })).toBe(true)
    expect(isEmptyMark({ fields: {} })).toBe(true)
    expect(isEmptyMark({ cost: '0' })).toBe(false)
    expect(isEmptyMark({ remove: true })).toBe(false)
    expect(isEmptyMark({ fields: { 'e0.amount': 9 } })).toBe(false)
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

  it('blaze条件は {blaze:true} に落ち、新カード案と差し替え定義 (redef) がJSONに入る', () => {
    const draft = {
      name: '猛り火の試作',
      color: 'red',
      cost: 1,
      type: 'spell',
      rarity: 'common',
      effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 8, condKey: 'blaze' }],
    }
    const doc = JSON.parse(
      buildProposals({
        cardMarks: { green_strike: { redef: { ...draft, name: '打撃・改', color: 'green' } } },
        newCards: '',
        newCardDefs: [draft],
      }),
    )
    expect(doc.cards.new).toHaveLength(1)
    expect(doc.cards.new[0].name).toBe('猛り火の試作')
    expect(doc.cards.new[0].effects[0].condition).toEqual({ blaze: true })
    expect(doc.cards.changes[0].proposal.redef.name).toBe('打撃・改')
    expect(doc.memo).toBeUndefined()
  })
})

describe('戦闘評価のログ出力 (2026-09-01)', () => {
  it('評価が戦闘履歴テーブルの列と見出しに出る (未評価は空欄)', () => {
    const run = createRun(7, 'set-confirm')
    const combat = freshCombat('set-confirm', 'enemy_probe', 42)
    const rated = { ...archiveBattle({ ...combat, phase: 'won' as const }, 1, 'enemy_probe', false, 80, 10), rating: { strength: 4, fun: 5, note: '読み合いが濃かった' } }
    const unrated = archiveBattle({ ...combat, phase: 'won' as const }, 2, 'enemy_probe', false, 75, 10)
    const text = buildReport(run, null, [rated, unrated])
    expect(text).toContain('| 強さ | 面白さ | 敗因 | メモ |')
    expect(text).toContain('| 4 | 5 |  | 読み合いが濃かった |') // 敗因列 (2026-09-02) は勝利なので空
    expect(text).toContain('/ 評価: 強さ4 面白さ5「読み合いが濃かった」')
    expect(text).toContain('|  |  |  |') // 未評価行は空欄
  })
})

describe('敵・レリックの調整サイクル (2026-09-01)', () => {
  it('敵の数値マークが現行def同梱で出て、パスのキーがそのまま残る', () => {
    const probe = getEnemyDef('enemy_probe')
    const doc = JSON.parse(
      buildProposals({
        cardMarks: {},
        newCards: '',
        newCardDefs: [],
        enemyMarks: {
          enemy_probe: { fields: { maxHp: 50, 'm0.max': 9 }, change: '序盤の教師をやや強く' },
          enemy_wide_power: { remove: true },
        },
      }),
    )
    expect(doc.enemies.changes).toHaveLength(1)
    const c = doc.enemies.changes[0]
    expect(c.current.maxHp).toBe(probe.maxHp)
    expect(c.proposal.fields).toEqual({ maxHp: 50, 'm0.max': 9 })
    expect(c.note).toBe('序盤の教師をやや強く')
    expect(doc.enemies.removals[0].current.name).toBe('うねる獣')
  })

  it('レリックの数値マークと新規案 (敵・レリック) がJSONで出る', () => {
    const doc = JSON.parse(
      buildProposals({
        cardMarks: {},
        newCards: '',
        newCardDefs: [],
        relicMarks: { relic_vanguard_shield: { fields: { 'e0.amount': 6 } } },
        newEnemyDefs: [
          {
            name: '試作の骸骨', archetype: 'wide-power', maxHp: 44,
            moves: [{ id: 'slash', kind: 'attack', min: 7, max: 10, weight: 2, inflictStatus: 'weak', inflictAmount: 1 }],
            sequence: 'slash, slash', thorns: 2,
          },
        ],
        newRelicDefs: [
          { name: '試作の護符', description: '毎ターンブロック+1', effects: [{ trigger: 'onTurnStart', effect: 'gainBlock', amount: 1 }], maxHp: 5, setDamageReduction: 1 },
        ],
      }),
    )
    expect(doc.relics.changes[0].current.effects[0].amount).toBe(5) // 現行値はdef同梱から読める
    expect(doc.relics.changes[0].proposal.fields).toEqual({ 'e0.amount': 6 })
    const ne = doc.enemies.new[0]
    expect(ne.sequence).toEqual(['slash', 'slash'])
    expect(ne.thorns).toBe(2)
    expect(ne.moves[0].inflict).toEqual({ status: 'weak', amount: 1 })
    const nr = doc.relics.new[0]
    expect(nr.combatRule).toEqual({ setDamageReduction: 1 })
    expect(nr.bonus).toEqual({ maxHp: 5 })
  })
})

describe('リーダーの調整サイクル (2026-09-01)', () => {
  it('リーダーの数値マークと新規案がJSONで出る', () => {
    const doc = JSON.parse(
      buildProposals({
        cardMarks: {},
        newCards: '',
        newCardDefs: [],
        leaderMarks: { leader_green: { fields: { maxHp: 75, 'p0.amount': 2 }, change: '成長の初速を上げたい' } },
        newLeaderDefs: [
          {
            name: 'つばき', colors: ['red', 'white'], maxHp: 70, drawPerTurn: 5, energyMax: 3, rewardChoices: 4,
            description: 'ボロスの闘僧', passive: [{ trigger: 'onAttackPlayed', effect: 'gainBlock', amount: 1 }],
          },
        ],
      }),
    )
    const c = doc.leaders.changes[0]
    expect(c.current.maxHp).toBe(80)
    expect(c.current.passive[0].amount).toBe(1)
    expect(c.proposal.fields).toEqual({ maxHp: 75, 'p0.amount': 2 })
    const nl = doc.leaders.new[0]
    expect(nl.name).toBe('つばき')
    expect(nl.colors).toEqual(['red', 'white'])
    expect(nl.passive[0].trigger).toBe('onAttackPlayed')
  })
})

describe('セーブ機能 (2026-09-01 裁定で解禁拡張: 続きから+ファイル書き出し/読み込み)', () => {
  it('buildRunSaveFile: sim/play.ts互換のSaveFile形 (kind/run/logIndex) + UI拡張フィールド', () => {
    const run = createRun(7, 'set-confirm')
    const sf = JSON.parse(buildRunSaveFile(run, [], [{ at: '2026-09-01T00:00:00.000Z', context: 'x', text: 'y' }]))
    expect(sf.kind).toBe('run')
    expect(sf.run.seed).toBe(7)
    expect(sf.logIndex).toBe(0)
    expect(sf.fingerprint).toContain('cards')
    expect(sf.playNotes).toHaveLength(1)
  })
})

describe('調整案のライブ適用 (2026-09-01)', () => {
  it('applyCardMark: コスト/数値/条件のパスが定義に書き戻る (redefは完全形が勝つ)', () => {
    const def = { id: 'green_strike', name: '打撃', cost: 1, type: 'physical', color: 'green', rarity: 'common', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 6 }] }
    const out = applyCardMark(def as never, { cost: '0', exhaust: true, fields: { 'e0.amount': 9, 'e0.cond.minActionValue': 10 } })
    expect(out.cost).toBe(0)
    expect(out.exhaust).toBe(true)
    expect(out.effects[0].amount).toBe(9)
    expect(out.effects[0].condition).toEqual({ minActionValue: 10 })
    const re = applyCardMark(def as never, { fields: { 'e0.amount': 99 }, redef: { name: '打撃・改', color: 'green', cost: 2, type: 'physical', rarity: 'rare', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 12 }] } })
    expect(re.id).toBe('green_strike') // 差し替えでもidは維持
    expect(re.name).toBe('打撃・改')
    expect(re.effects[0].amount).toBe(12) // fieldsよりredefが勝つ
  })

  it('buildOverrideDefs: マーク済みだけが差し替え列に入り、削除案は入らず、新規はdebug_idが振られる', () => {
    const o = buildOverrideDefs({
      cardMarks: {
        green_strike: { fields: { 'e0.amount': 9 } },
        green_fang: { remove: true }, // 削除案は適用しない
        blue_strike: {},
      },
      newCards: '',
      newCardDefs: [{ name: '試作', color: 'green', cost: 1, type: 'physical', rarity: 'common', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 5 }] }],
      enemyMarks: { enemy_probe: { fields: { maxHp: 50, 'm0.max': 9 } } },
    })
    expect(o.cards).toHaveLength(2)
    expect(o.cards[0].effects[0].amount).toBe(9)
    expect(o.cards[1].id).toContain('debug_card_')
    expect(o.enemies[0].maxHp).toBe(50)
    expect(o.enemies[0].moves[0].max).toBe(9)
    expect(JSON.stringify(o)).not.toContain('green_fang')
  })
})

describe('リプレイ (2026-09-01 ジャーナル方式)', () => {
  it('replayStates: origin+コマンド列から状態列が完全再現される (決定性の実証)', () => {
    const origin = { kind: 'run' as const, seed: 7, leaderId: 'leader_green', difficulty: 4 }
    let r = replayInitialRun(origin)
    expect(r.difficulty).toBe(4)
    const commands = []
    const c1 = { type: 'ChooseNode' as const, col: nextChoices(r)[0] }
    r = applyRunCommand(r, c1)
    commands.push(c1)
    expect(r.phase).toBe('combat') // 行0は必ず戦闘 (本家準拠)
    const c2 = { type: 'Combat' as const, command: { type: 'EndTurn' as const } }
    r = applyRunCommand(r, c2)
    commands.push(c2)
    const { states, error } = replayStates({ origin, commands })
    expect(error).toBeNull()
    expect(states).toHaveLength(3)
    expect(JSON.stringify(states[2])).toBe(JSON.stringify(r)) // バイト単位で一致
  })

  it('不正なコマンド (データ変更で分岐) は error で打ち切り、throwしない', () => {
    const { states, error } = replayStates({
      origin: { kind: 'run', seed: 7, leaderId: 'leader_green' },
      commands: [{ type: 'ChooseNode', col: 99 }],
    })
    expect(error).toContain('分岐')
    expect(states).toHaveLength(1)
  })

  it('セーブファイルに journal が同梱され、無ければフィールドごと落ちる', () => {
    const run = createRun(7, 'set-confirm')
    const withJ = JSON.parse(buildRunSaveFile(run, [], [], { origin: { kind: 'run', seed: 7, leaderId: 'leader_green' }, commands: [] }))
    expect(withJ.journal.origin.seed).toBe(7)
    const noJ = JSON.parse(buildRunSaveFile(run))
    expect(noJ.journal).toBeUndefined()
  })
})

describe('選択履歴 (2026-09-01「何をピックしたか・鍛錬の結果が分かりにくい」)', () => {
  it('describeRunChoice: 進路・報酬ピック(見送り込み)が言語化される', () => {
    let r = createRun(7, 'set-confirm')
    const c1 = { type: 'ChooseNode' as const, col: nextChoices(r)[0] }
    const r1 = applyRunCommand(r, c1)
    const line1 = describeRunChoice(r, c1, r1)!
    expect(line1.text).toContain('進路:')
    expect(line1.at).toBe('幕1 行1')
    // 報酬フェーズを外科的に作ってピックの言語化を確認
    const withReward = { ...r1, phase: 'reward' as const, rewardOptions: ['green_fang', 'green_sweep', 'green_growth_seedling'] }
    const cmd = { type: 'PickReward' as const, index: 0 }
    const after = applyRunCommand(withReward, cmd)
    const line2 = describeRunChoice(withReward, cmd, after)!
    expect(line2.text).toContain('牙の一撃 を獲得')
    expect(line2.text).toContain('見送り:')
  })

  it('選択履歴がレポートとセーブに同梱される', () => {
    const run = createRun(7, 'set-confirm')
    const choices = [{ at: '幕1 行3', text: '焚き火: 鍛えた 打撃 → 打撃+' }]
    const text = buildReport(run, null, [], '', [], choices)
    expect(text).toContain('## 選択履歴（1件')
    expect(text).toContain('鍛えた 打撃 → 打撃+')
    const sf = JSON.parse(buildRunSaveFile(run, [], [], null, choices))
    expect(sf.choices).toHaveLength(1)
  })
})
