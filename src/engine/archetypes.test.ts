// アーキタイプ (デッキ・シグネチャー効果) と敵特性 (ローテーション・強化) のテスト。
// 確定済みルール表の「デッキ選択」「貫通」「敵特性」をここで固定する。
import { describe, expect, it } from 'vitest'
import { allDecks, buildDeck, deckSize, getDeckDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent } from './types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)
type IntentDeclared = Extract<GameEvent, { type: 'EnemyIntentDeclared' }>
const declaredIntents = (log: readonly GameEvent[]) =>
  log.filter((e): e is IntentDeclared => e.type === 'EnemyIntentDeclared')

describe('プリセットデッキ', () => {
  it('全デッキが構築でき、枚数と uid 一意性が保たれる', () => {
    for (const deck of allDecks) {
      const cards = buildDeck(deck.id)
      expect(cards.length).toBe(deckSize(deck))
      expect(new Set(cards.map((c) => c.uid)).size).toBe(cards.length)
    }
  })

  it('StartCombat で deckId を指定できる (省略時はスターター15枚)', () => {
    const bigMana = freshCombat('set-confirm', 'enemy_brute', 42, 'deck_big_mana')
    const total = bigMana.player.hand.length + bigMana.player.drawPile.length
    expect(total).toBe(deckSize(getDeckDef('deck_big_mana')))
    const starter = freshCombat('set-confirm', 'enemy_brute', 42)
    expect(starter.player.hand.length + starter.player.drawPile.length).toBe(15)
  })

  it('未定義デッキは拒否される', () => {
    expect(() => freshCombat('set-confirm', 'enemy_brute', 1, 'no_such_deck')).toThrow(/未定義デッキ/)
  })
})

describe('シグネチャー効果', () => {
  it('貫通 (トランプル): 敵ブロックを無視してダメージが通る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle'), ['green_sig_trample'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 14 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_trample' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12) // ブロック14を無視して素通し (踏み荒らし 10→12)
    expect(s.enemies[0].block).toBe(14) // ブロックは削れもしない
  })

  it('貫通なしの攻撃は敵ブロックで軽減される (対照)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle'), ['green_strike'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 4 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 - 4))
  })

  it('森の大爆発: エナジー上限×4のダメージ (成長も乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_sig_overgrowth'])
    s = { ...s, player: { ...s.player, energy: 6, energyMax: 6, growth: 1 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_overgrowth' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 * 4 + 1))
  })

  it('開花の儀: 成長カウンターを2倍にする', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_sig_rite_of_bloom'])
    s = { ...s, player: { ...s.player, growth: 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_rite_of_bloom' })
    expect(s.player.growth).toBe(6)
  })

  it('蔦の乱舞: 5ヒットすべてに成長が乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_sig_vine_dance'])
    s = { ...s, player: { ...s.player, energy: 3, growth: 4 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_vine_dance' })
    expect(s.enemies[0].hp).toBe(hpBefore - (2 + 4) * 5)
  })
})

describe('勢い (トランプル再設計)', () => {
  it('勢いは同一ターン中の以降の攻撃に加算され、自ターン終了でリセットされる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), [
      'green_trample_charge',
      'green_strike',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trample_charge' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5) // 助走自身には勢いは乗らない (効果順: ダメージ→勢い+3)
    expect(s.player.momentum).toBe(3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5 - (6 + 3)) // 打撃に勢い+3が乗る
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.momentum).toBe(0) // ターン終了でリセット
  })

  it('勢いと成長は重ねて乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_strike'])
    s = { ...s, player: { ...s.player, growth: 2, momentum: 3 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 + 2 + 3))
  })
})

describe('一時マナ (自然の奔流)', () => {
  it('ターン終了までエナジー+2。energyMax は増えず、次ターンは通常回復', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_ritual_surge'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ritual_surge' })
    expect(s.player.energy).toBe(3 - 1 + 2) // 4/3 の一時超過
    expect(s.player.energyMax).toBe(3)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.energy).toBe(3) // 次ターンは energyMax まで
  })
})

describe('置物 (permanent)', () => {
  it('プレイすると場に残り、捨て札に行かない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_perm_growth_tree'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_growth_tree' })
    expect(s.player.permanents).toHaveLength(1)
    expect(s.player.discardPile.some((c) => c.uid === 't0_green_perm_growth_tree')).toBe(false)
    expect(types(s.eventLog)).toContain('PermanentPlayed')
  })

  it('年輪の大樹: 毎ターン開始時に成長+1', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_perm_growth_tree'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_growth_tree' })
    expect(s.player.growth).toBe(0) // 置いたターンはまだ
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.growth).toBe(1)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.growth).toBe(2)
    expect(s.player.permanents).toHaveLength(1) // 場に残り続ける
  })

  it('大角の群長: 攻撃カードをプレイするたび勢い+2 (そのカード自身には乗らない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), [
      'green_perm_herd_chief',
      'green_strike',
      'green_double_lash',
    ])
    s = { ...s, player: { ...s.player, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_herd_chief' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6) // 1発目は素の6
    expect(s.player.momentum).toBe(2)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_double_lash' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6 - (4 + 2) * 2) // 2発目は各ヒット+2 (二連 4×2)
    expect(s.player.momentum).toBe(4)
  })

  it('茨の茂み: 敵の攻撃のたび自動で返し4 (成長も乗る)。置物は場に残る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_perm_thorn_thicket'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_thorn_thicket' })
    s = { ...s, player: { ...s.player, growth: 1 } }
    const hpBefore = s.enemies[0].hp
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(hpBefore - (4 + 1)) // 自動で返し4+成長1
    expect(s.player.permanents).toHaveLength(1)
    // 判断は挟まらない (set-confirm でも置物は自動発火。伏せがなければ中断しない)
    expect(s.turn).toBe(2)
  })
})

describe('消滅 (exhaust)', () => {
  it('森の大爆発は使用後この戦闘から除外され、捨て札に行かない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_sig_overgrowth'])
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_overgrowth' })
    expect(s.player.exhaustPile).toHaveLength(1)
    expect(s.player.discardPile.some((c) => c.uid === 't0_green_sig_overgrowth')).toBe(false)
    expect(types(s.eventLog)).toContain('CardExhausted')
  })
})

describe('選択式カード (modes)', () => {
  it('陽光の恵み: モード0=上限+1&1ドロー、モード1=1ドロー&成長2 を選べる (2026-08-27)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_ramp_sunlight'])
    const s0 = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sunlight', modeIndex: 0 })
    expect(s0.player.energyMax).toBe(4)
    const handBefore = s.player.hand.length
    const s1 = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sunlight', modeIndex: 1 })
    expect(s1.player.energyMax).toBe(3)
    expect(s1.player.hand.length).toBe(handBefore - 1 + 1) // 本体を出して1枚引く
    expect(s1.player.growth).toBe(2)
  })

  it('選択式カードは modeIndex なしでは拒否される', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_entangle'])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_entangle' })).toThrow(
      /選択式/,
    )
  })

  it('絡み蔦: ブロック7 か 7ダメージ の二者択一', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_entangle'])
    const hpBefore = s.enemies[0].hp
    const sBlock = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_entangle', modeIndex: 0 })
    expect(sBlock.player.block).toBe(7)
    expect(sBlock.enemies[0].hp).toBe(hpBefore)
    const sAtk = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_entangle', modeIndex: 1 })
    expect(sAtk.player.block).toBe(0)
    expect(sAtk.enemies[0].hp).toBe(hpBefore - 7)
  })
})

describe('手札捨てコスト (discardCost)', () => {
  it('大蛇の丸呑み: 手札1枚を追加コストに捨てて20ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), [
      'green_serpent_gulp',
      'green_guard',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_green_serpent_gulp',
      discardUids: ['t1_green_guard'],
    })
    expect(s.enemies[0].hp).toBe(hpBefore - 20) // 2026-08-27 StSコモン帯へ
    expect(s.player.hand).toHaveLength(0)
    expect(s.player.discardPile.map((c) => c.uid)).toContain('t1_green_guard')
    expect(types(s.eventLog)).toContain('CardsDiscarded')
  })

  it('捨てる手札の指定がない・自分自身を捨てる指定は拒否される', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_brute'), [
      'green_serpent_gulp',
      'green_guard',
    ])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_serpent_gulp' })).toThrow(
      /追加コスト/,
    )
    expect(() =>
      applyCommand(s, {
        type: 'PlayCard',
        cardUid: 't0_green_serpent_gulp',
        discardUids: ['t0_green_serpent_gulp'],
      }),
    ).toThrow(/不正/)
  })
})

describe('敵特性 (StS参考)', () => {
  it('行動ローテーション: 探り屋は 探り→探り→本気の一突き を繰り返す', () => {
    let s = freshCombat('set-confirm', 'enemy_probe')
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } } // 行動観察のため耐える
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(declaredIntents(s.eventLog).map((d) => d.intent.shownMin)).toEqual([5, 5, 12]) // poke, poke, lunge
    s = applyCommand(s, { type: 'EndTurn' })
    expect(declaredIntents(s.eventLog)[3].intent.shownMin).toBe(5) // ループして poke に戻る
  })

  it('強化 (筋力): 雄叫び後の攻撃は実値も幅表示も上がる', () => {
    let s = freshCombat('set-confirm', 'enemy_brute') // ターン1の意図は必ず warcry (2〜4)
    expect(s.enemies[0].intent?.kind).toBe('buff')
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('StrengthGained')
    const str = s.enemies[0].strength
    expect(str).toBeGreaterThanOrEqual(2)
    expect(str).toBeLessThanOrEqual(4)
    const intent = s.enemies[0].intent! // ターン2は club (基礎 9〜13)
    expect(intent.kind).toBe('attack')
    expect(intent.shownMin).toBe(9 + str)
    expect(intent.shownMax).toBe(13 + str)
    expect(intent.actual).toBeGreaterThanOrEqual(9 + str)
    expect(intent.actual).toBeLessThanOrEqual(13 + str)
  })

  it('打ち消しは強化 (バフ行動) も無効化できる', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_root_weave'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_root_weave' })
    s = applyCommand(s, { type: 'EndTurn' }) // warcry に自動発動 → 無効化
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(s.enemies[0].strength).toBe(0)
  })

  it('チャージ大技: 大亀は 防御→防御→大薙ぎ(24〜32) の予告付き大技を打つ', () => {
    let s = freshCombat('set-confirm', 'enemy_turtle')
    s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
    expect(s.enemies[0].intent?.kind).toBe('defend')
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'EndTurn' })
    const intent = s.enemies[0].intent!
    expect(intent.kind).toBe('attack')
    expect(intent.shownMin).toBe(24)
    expect(intent.shownMax).toBe(32)
  })
})

describe('0マナ消滅 (緑)', () => {
  it('野生の萌芽: 0マナで成長+1、消滅する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_wild_sprout'])
    s = { ...s, player: { ...s.player, energy: 0 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_wild_sprout' })
    expect(s.player.growth).toBe(1)
    expect(s.player.exhaustPile).toHaveLength(1)
  })
})
