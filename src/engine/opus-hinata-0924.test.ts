// Opus ひなた4本 (2026-09-24・シード5601・難易度5・3幕通し) の答え合わせで直した不具合と裁定の機械固定。
// docs/playtest-2026-09-24-opus-hinata-synthesis.md §7〜§8 / card-power.md §86
import { describe, expect, it } from 'vitest'
import { BRAND_DEF, WOUND_DEF, allCards, getCardDef, getDeckDef, getEnemyDef } from './content.ts'
import { reactionActionValue, reactionMatches } from './effects.ts'
import { previewMoves } from './enemyGraph.ts'
import { makeGear } from './gears.ts'
import { REWARD_EXCLUDED, applyRunCommand, canTransformCard, defaultEventCardIndex, transformCardAt } from './run.ts'
import type { RunState } from './run.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, restIntent, setAndArm, withHand, withIntent, createRunAtMap as createRun } from './test-helpers.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, uid = `x_${id}`): CardInstance => ({ uid, def: getCardDef(id) })
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
/** 剣の人形 (毎ターン開始時に3ダメ) を場に出す */
function withSword(s0: GameState): GameState {
  let s = energy(withHand(s0, ['white_perm_squire']), 9)
  s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire', targetIndex: 0 })
  return { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
}
/** 自ターン開始で出た出来事 (最後の TurnStarted 以降) */
const sinceTurnStart = (s: GameState) => s.eventLog.slice(s.eventLog.map((e) => e.type).lastIndexOf('TurnStarted'))

describe('E1 ターン開始の誘発で意図が二重に宣言されない', () => {
  it('ターン開始の人形で HP 半分を割ると、第2形態の1手目がそのまま宣言される (旧: 直後の通常の宣言で上書きされて1手目が飛んだ)', () => {
    let s = withSword(freshCombat('set-confirm', 'enemy_brute', 7))
    const def = getEnemyDef('enemy_brute')
    const half = Math.floor(s.enemies[0].maxHp / 2)
    // 剣の人形の点灯で削れた分を戻して、次のターン開始の一撃 (3+火勢1) で半分を割る所に置く
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: half + 2 })) }
    s = withIntent(s, restIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.turn).toBe(2)
    expect(s.enemies[0].hp).toBeLessThanOrEqual(half)
    const decl = sinceTurnStart(s).filter((e) => e.type === 'EnemyIntentDeclared' && e.enemyIndex === 0)
    expect(decl).toHaveLength(1) // 1回だけ
    const firstOfHalf = previewMoves(def, def.interrupts![0].goto, 1)[0]
    expect(s.enemies[0].intentMoveId).toBe(firstOfHalf.id)
    // 前のターンに実行済みの意図を「差し替えた」とは言わない
    const intr = sinceTurnStart(s).find((e) => e.type === 'EnemyInterrupted')
    expect(intr).toBeDefined()
    expect(intr && 'replaced' in intr ? intr.replaced : undefined).toBe(false)
  })

  it('ターン開始の人形で残機の敵 (合成獣) を倒すと、次の形態は出てきたターン「隙」のまま (旧: 攻撃7×3 で上書きされて殴ってきた)', () => {
    let s = withSword(freshCombat('set-confirm', 'enemy_chimera_1', 7))
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 2, block: 0 })) }
    s = withIntent(s, restIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.turn).toBe(2)
    const child = s.enemies.findIndex((e) => e.enemyId === 'enemy_chimera_2' && e.hp > 0)
    expect(child).toBeGreaterThanOrEqual(0)
    expect(s.enemies[child].intent?.kind).toBe('rest')
    expect(sinceTurnStart(s).filter((e) => e.type === 'EnemyIntentDeclared' && e.enemyIndex === child)).toHaveLength(1)
  })

  it('打ち消された逃走の次のターンは、2度続けて逃走を宣言しない (前の意図の種別は prevIntentKind に残る)', () => {
    let s = freshCombat('set-confirm', 'enemy_thief', 3)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, stolenGold: 10, intent: { kind: 'flee', actual: 0 } })), negateNextAction: true }
    s = applyCommand({ ...s, player: { ...s.player, hp: 999, maxHp: 999 } }, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBeGreaterThan(0)
    expect(s.enemies[0].intent?.kind).not.toBe('flee')
  })
})

describe('E2 静かな鈴は行動の開始で固定する', () => {
  it('罠が1枚だけの時に発動しても、確認の窓に出た「鈴で-2」のまま解決する (旧: 罠が無くなって鈴が外れ26で解決)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 5), ['white_reaction_ward'])
    s = { ...s, setDamageReduction: 2, player: { ...s.player, hp: 99, maxHp: 99 } }
    s = setAndArm(s, 't0_white_reaction_ward')
    s = withIntent(s, attackIntent(26))
    s = { ...s, player: { ...s.player, block: 0 } }
    const hp0 = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // 26 − 鈴2 = 24、護りの灯印のブロック12 → HP −12
    expect(hp0 - s.player.hp).toBe(12)
  })
})

describe('E3 混乱した敵の攻撃には被攻撃前の罠の窓を開かない', () => {
  it('混乱中の攻撃 (自分か仲間に向かう) は onAttackIncoming と照合しない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 5), ['white_reaction_ward'])
    s = setAndArm(s, 't0_white_reaction_ward')
    s = withIntent(s, attackIntent(20))
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, confusion: 1, hp: 200, maxHp: 200 })) }
    const ward = s.player.setCards[0]
    expect(reactionMatches(s, ward, { stage: 'pre', kind: 'attack', actual: 20, confused: true })).toBe(false)
    expect(reactionMatches(s, ward, { stage: 'pre', kind: 'attack', actual: 20 })).toBe(true)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).not.toBe('awaiting-reaction')
    expect(s.player.setCards.some((c) => c.def.id === 'white_reaction_ward')).toBe(true) // 鳴らずに残る
  })
})

describe('E10 罠の「敵の行動の値N以上/以下」は合計 (1発×ヒット数) で判定する', () => {
  it('9×4 の攻撃は「10以上」を満たす (旧: 1発の9で判定して誓いの盾の灯が付かなかった)', () => {
    let s = freshCombat('set-confirm', 'enemy_probe', 5)
    s = withIntent(s, { kind: 'attack', actual: 9, hits: 4 })
    expect(reactionActionValue(s, 0)).toBe(36)
    const oath = inst('white_oath_shield')
    const win = { stage: 'pre' as const, kind: 'attack' as const, actual: reactionActionValue(s, 0) }
    expect(reactionMatches(s, oath, win)).toBe(true)
    // 攻撃以外は実値のまま
    s = withIntent(s, { kind: 'defend', actual: 14 })
    expect(reactionActionValue(s, 0)).toBe(14)
  })
})

describe('E4 掘り出し (ギア) は指定した札が捨て札に無ければ弾く', () => {
  it('捨て札が空で山札の札を指定すると、ギアも魔素も消えずにエラー', () => {
    const combat = freshCombat('set-confirm', 'enemy_probe', 5)
    const inDeck = combat.player.drawPile[0]
    const empty = { ...combat, player: { ...combat.player, discardPile: [] } }
    const run: RunState = { ...createRun(777, 'set-confirm', 'leader_white'), phase: 'combat', combat: empty, gears: [makeGear('gear_dig_out', 't_dig')], mana: 50, seenGearIds: ['gear_dig_out'] }
    expect(() => applyRunCommand(run, { type: 'UseGear', index: 0, cardUid: inDeck.uid } as never)).toThrow()
  })
})

describe('E6 盗みは所持金−すでに盗まれた額で頭打ち (本家 Looter)', () => {
  it('所持金5Gなら盗みの宣言は5Gまで。盗める額が0なら盗みは成立しない', () => {
    for (const [gold, expected] of [[5, 5], [0, 0]] as const) {
      let s = freshCombat('set-confirm', 'enemy_thief', 11) // 忍び寄る → 盗み (12〜20)
      s = { ...s, goldAvailable: gold, player: { ...s.player, hp: 999, maxHp: 999 } }
      s = applyCommand(s, { type: 'EndTurn' })
      const it = s.enemies[0].intent!
      expect(it.kind).toBe('steal-gold')
      expect(it.actual).toBe(expected)
      expect(s.enemies[0].stolenGold ?? 0).toBe(expected)
    }
  })
})

describe('T14 状態異常・烙印は変成できない (本家2と同じく変成は呪いを消す手段にならない)', () => {
  it('canTransformCard・?の変成の既定の対象・変成そのもの', () => {
    expect(canTransformCard({ uid: 'w', def: WOUND_DEF })).toBe(false)
    expect(canTransformCard({ uid: 'b', def: BRAND_DEF })).toBe(false)
    expect(canTransformCard(inst('white_guard'))).toBe(true)
    const base = createRun(777, 'set-confirm', 'leader_white')
    const run: RunState = { ...base, deck: [{ uid: 'w1', def: WOUND_DEF }, ...base.deck] }
    const choice = { label: '祈る', transformCard: true } as const
    expect(defaultEventCardIndex(run, choice)).toBe(1) // 負傷 (0番) は飛ばす
    expect(() => transformCardAt(run, 0, false)).toThrow()
  })
})

describe('データの裁定 (2026-09-24 Opus ひなた)', () => {
  it('重ねる灯 (反復の触媒) は撤去。白の触媒は無い', () => {
    expect(allCards.some((c) => c.id === 'white_catalyst_echo')).toBe(false)
    expect(allCards.filter((c) => c.color === 'white' && c.fusionCatalyst !== undefined)).toEqual([])
  })

  it('白の初期デッキ = 灯の閃撃3・灯の護光・白盾2・灯の小盾・灯の岐路・灯の矢・護りの灯印 (10枚)。灯の小盾は報酬に出ない', () => {
    const d = getDeckDef('run_basic_white')
    const count = (id: string) => d.cards.find((c) => c.cardId === id)?.count ?? 0
    expect(count('white_guard')).toBe(2)
    expect(count('white_light_buckler')).toBe(1)
    expect(d.cards.reduce((a, c) => a + c.count, 0)).toBe(10)
    expect(REWARD_EXCLUDED.has('white_light_buckler')).toBe(true)
  })

  it('火種撒き＝9＋撃った火種×1＋火種1・火守りの盾＝ブロック8＋撃った火種×1＋火種1 (作る札に刈り取りを内蔵)。火花散らしは貫通なし (§82)', () => {
    expect(getCardDef('white_spark_scatter').effects.map((e) => [e.effect, e.amount])).toEqual([
      ['dealDamage', 9], ['dealDamagePerSpark', 1], ['addCardToDraw', 1],
    ])
    expect(getCardDef('white_spark_shield').effects.map((e) => [e.effect, e.amount])).toEqual([
      ['gainBlock', 8], ['gainBlockPerSpark', 1], ['addCardToDraw', 1],
    ])
    expect(getCardDef('white_spark_burst').effects.some((e) => e.pierce === true)).toBe(false)
  })

  it('gainBlockPerSpark: この戦闘で撃った火種の数×N のブロック (0枚なら何もしない)', () => {
    let s = energy(withHand(freshCombat('set-confirm', 'enemy_probe', 5), ['white_spark_shield', 'white_spark_shield']), 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_spark_shield' })
    expect(s.player.block).toBe(8) // 撃った火種0
    s = { ...s, player: { ...s.player, block: 0, sparksPlayedThisCombat: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_spark_shield' })
    expect(s.player.block).toBe(8 + 5)
  })
})
