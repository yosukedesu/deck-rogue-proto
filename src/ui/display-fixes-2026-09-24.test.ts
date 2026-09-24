// 表示と文面の是正 (2026-09-24 Opus ひなた 4本の §7。docs/playtest-2026-09-24-opus-hinata-synthesis.md)。
// engine の処理は変えない。画面・ログ・用語解説・図鑑の文が実処理と同じことを言うかを固定する
import { describe, expect, it } from 'vitest'
import { allGears, allRelics, getEnemyDef } from '../engine/content.ts'
import { describeGraph } from '../engine/enemyGraph.ts'
import { applyRunCommand, createRun } from '../engine/run.ts'
import { enemyTraitTags, interruptBlockedNote } from '../engine/traits.ts'
import { freshCombat } from '../engine/test-helpers.ts'
import type { GameEvent, GameState } from '../engine/types.ts'
import { KEYWORD_HELP } from './keywordHelp.ts'
import { logLine, logLines } from './log.ts'
import { describeRunChoice } from './report.ts'
import { webVocab } from './vocab.ts'

describe('ログの順 (T12)・灯の火床 (T15)', () => {
  it('引く途中の切り直しは「切り直して N 枚ドロー」の1行。火床は「灯を払って火種を山札へ」の1行 (放出と書かない)', () => {
    const events: GameEvent[] = [
      { type: 'CardsDrawn', count: 2, cards: ['灯の閃撃', '白盾'] },
      { type: 'DeckShuffled' },
      { type: 'LightDischarged', spent: 9, sparks: 3 },
      { type: 'CardsAddedToDraw', cardId: 'white_spark_token', count: 3 },
      { type: 'LightDischarged', spent: 4 },
      { type: 'DeckShuffled' },
    ]
    expect(logLines(events).map((l) => l.text)).toEqual([
      '🔀 山札を切り直して2枚ドロー',
      '🕯 灯9を払って火種3を山札へ',
      '🕯 灯4を放出！',
      '山札を切り直した',
    ])
  })
})

describe('人形の語 (T3)', () => {
  it('敵の技 destroy-token とその結果は「人形狩り」。用語解説にも「従者」は残らない', () => {
    expect(logLine({ type: 'TokenDestroyed', cardId: 'white_perm_squire', uid: 'u1' })?.text).toBe('人形狩り: 剣の人形が壊された')
    expect(logLine({ type: 'RetainersDuplicated', count: 2 })?.text).toContain('人形2体')
    expect(KEYWORD_HELP['人形狩り']).toBeDefined()
    const leaked = Object.entries(KEYWORD_HELP).filter(([k, v]) => k.includes('従者') || v.includes('従者')).map(([k]) => k)
    expect(leaked).toEqual([])
  })
})

describe('用語解説 (T2・E11)', () => {
  it('火種: 火の粉は敵全体に6・貫通。撤去した札の名前は出さない', () => {
    expect(KEYWORD_HELP['火種']).toContain('敵全体に6・貫通')
    const removed = ['重ねる灯', '降霊', '光盾の点灯', '眩む閃光', '輝きの光', '人形の総突撃', '灯火の旗印', '灯の呼び声', '灯の捧げ', '二重の点灯', '鏡の灯籠']
    const hits = Object.entries(KEYWORD_HELP).flatMap(([k, v]) => removed.filter((n) => v.includes(n)).map((n) => `${k}:${n}`))
    expect(hits).toEqual([])
  })

  it('反復: 1回目で対象が倒れたら2回目は空振り (本家2と同じ・仕様は据え置き)', () => {
    expect(KEYWORD_HELP['反復']).toContain('2回目は空振り')
  })
})

describe('ブラウザ版の伏せ用語 (T5)', () => {
  it('回収の紐: 「期限切れのからくり（2回鳴らなかった仕込み札）」は「期限切れの伏せ札（2回発動しなかった伏せ札）」', () => {
    expect(webVocab('期限切れのからくり（2回鳴らなかった仕込み札）は捨て札でなく手札に戻る')).toBe('期限切れの伏せ札（2回発動しなかった伏せ札）は捨て札でなく手札に戻る')
    expect(webVocab('からくりを動かすたび、次のカード-1')).toBe('伏せ札を発動するたび、次のカード-1')
    // 太鼓や鐘が「鳴る」のは伏せの語ではない = 写さない
    expect(webVocab('太鼓が鳴るたび、みんな元気になる。')).toBe('太鼓が鳴るたび、みんな元気になる。')
  })

  it('レリックとギアの説明文を写すと、Unity の語 (からくり・仕込む・鳴らなかった) が残らない', () => {
    const texts = [...allRelics.map((r) => r.description), ...allGears.map((g) => g.text)]
    expect(texts.map(webVocab).filter((t) => /からくり|仕込|鳴らなかった/.test(t))).toEqual([])
  })
})

describe('図鑑の行動 (T4)', () => {
  it('候補1つの乱択は「どちらか{…}」と書かず技だけ出す', () => {
    const squirrel = describeGraph(getEnemyDef('enemy_thorn_squirrel'))
    expect(squirrel[0]).not.toContain('どちらか')
    expect(squirrel[0]).toContain('同じ技を繰り返す')
    // 苔まといの主: HP半分の後の乱択 (候補1つ) も技だけ
    expect(describeGraph(getEnemyDef('enemy_moss'))[1]).not.toContain('どちらか')
  })
})

describe('敵のタグ (E5・E7)', () => {
  it('育つ技の「いまは」: 宣言した直後は1回前の回数で数える (宣言した技は宣言の前の回数ぶん育っている)', () => {
    const s = freshCombat('set-confirm', 'enemy_set_breaker', 1)
    const e = s.enemies[0]
    expect(e.intentMoveId).toBe('smash') // 開始の節 = 罠壊しの殴り (使うたび+3)
    expect(e.moveUses?.smash).toBe(1)
    expect(enemyTraitTags(s, 0).find((t) => t.startsWith('育つ技'))).toContain('いまは+0')
    // 宣言していない時は、次に使う時の上がり幅 (使った回数ぶん)
    const idle: GameState = { ...s, enemies: s.enemies.map((x, i) => (i === 0 ? { ...x, intent: null, intentMoveId: undefined } : x)) }
    expect(enemyTraitTags(idle, 0).find((t) => t.startsWith('育つ技'))).toContain('いまは+3')
  })

  it('鎮めの錘で割り込みを止めた敵には、HP半分の予告も眠りのタグも出さない', () => {
    const egg = freshCombat('set-confirm', 'enemy_elite_iron_egg', 1)
    expect(enemyTraitTags(egg, 0).some((t) => t.startsWith('眠り'))).toBe(true)
    const eggBlocked: GameState = { ...egg, enemies: egg.enemies.map((x, i) => (i === 0 ? { ...x, interruptBlocked: true } : x)) }
    expect(enemyTraitTags(eggBlocked, 0).some((t) => t.startsWith('眠り'))).toBe(false)
    const moss = freshCombat('set-confirm', 'enemy_moss', 1)
    expect(enemyTraitTags(moss, 0).some((t) => t.includes('以下になると'))).toBe(true)
    const mossBlocked: GameState = { ...moss, enemies: moss.enemies.map((x, i) => (i === 0 ? { ...x, interruptBlocked: true } : x)) }
    expect(enemyTraitTags(mossBlocked, 0).some((t) => t.includes('以下になると'))).toBe(false)
    // 代わりの一文 (CLI の行動欄・Web のチップが共用): その敵が持つ引き金だけを言う
    expect(interruptBlockedNote(getEnemyDef('enemy_moss'), moss.enemies[0])).toBeNull()
    expect(interruptBlockedNote(getEnemyDef('enemy_moss'), mossBlocked.enemies[0])).toBe('鎮めの錘: この戦闘中はHPが半分を切っても行動が変わらない')
    expect(interruptBlockedNote(getEnemyDef('enemy_elite_iron_egg'), eggBlocked.enemies[0])).toBe('鎮めの錘: この戦闘中はダメージを受けても行動が変わらない')
  })
})

describe('イベントの結果の HP (T6)', () => {
  it('最大HPも増えた選択は、HP を選択肢の数字どおりに出して増えた分を注記する (C# Report.cs と同じ規則)', () => {
    const base = createRun(7, 'set-confirm')
    const altar = { ...base, phase: 'event' as const, eventId: 'event_forgotten_altar', hp: 50 }
    const cmd = { type: 'EventChoice' as const, index: 0 } // 身を捧げる（最大HP+8・HP-14）
    const line = describeRunChoice(altar, cmd, applyRunCommand(altar, cmd))!
    expect(line.text).toContain('HP-14（最大HP+8 で HP も+8）')
    expect(line.text).toContain('・最大HP+8')
    // 最大HPだけ増える選択: HP の項目は出さない (HP も+7 は最大HP の注記で足りる)
    const hollow = { ...base, phase: 'event' as const, eventId: 'event_ancient_hollow', hp: 50 }
    const cmd2 = { type: 'EventChoice' as const, index: 0 } // 樹液を浴びる（最大HP+7）
    const line2 = describeRunChoice(hollow, cmd2, applyRunCommand(hollow, cmd2))!
    expect(line2.text).toContain('最大HP+7')
    expect(line2.text).not.toMatch(/HP[+-]\d+（|HP\+0/)
  })
})
