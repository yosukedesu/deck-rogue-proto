// リアクション3方式のテスト (本プロジェクトの主役)。
// 方式ごとの挙動差と、方式共通ルール (同時1枚・空振り持続・打ち消し対象) をここで固定する。
import { getCardDef } from '../content.ts'
import { describe, expect, it } from 'vitest'
import { applyCommand } from '../state.ts'
import {
  attackIntent,
  defendIntent,
  destroySetIntent,
  freshCombat,
  withHand,
  withIntent,
} from '../test-helpers.ts'
import type { GameEvent } from '../types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)

describe('set-auto (セット式)', () => {
  it('伏せ→条件成立で自動発動。返しはダメージを受けた後に発動する (確定済みルール)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.energy).toBe(2) // コスト事前払い
    expect(s.player.setCards).toHaveLength(1)
    s = withIntent(s, attackIntent(12))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - 10) // 茨の返し10
    expect(s.player.hp).toBe(s.player.maxHp - 12) // 先にダメージを受けている
    expect(s.player.setCards).toHaveLength(0) // 発動後は捨て札へ
  })

  it('軽減リアクション (守りの蔓) は被攻撃前トリガーで、被弾直前にブロック12を得る', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_vine'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_vine' })
    s = withIntent(s, attackIntent(15))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(s.player.maxHp - (15 - 12))
  })

  it('伏せは同時1枚まで (確定済みルール)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), [
      'green_reaction_thorns',
      'green_reaction_vine',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't1_green_reaction_vine' })).toThrow()
  })

  it('reaction カテゴリ以外は伏せられない', () => {
    const s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_strike'])
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't0_green_strike' })).toThrow()
  })

  it('空振りした伏せは無期限に持続する (確定済みルール)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, defendIntent(5)) // 攻撃されなかった → onAttacked は不成立
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ReactionWhiffed')
    expect(s.player.setCards).toHaveLength(1) // 次ターンも伏せたまま
    expect(s.player.setCards[0].uid).toBe('t0_green_reaction_thorns')
  })

  it('打ち消し (根の紡ぎ) は攻撃を無効化する', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_root_weave'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_root_weave' })
    s = withIntent(s, attackIntent(14))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(s.player.hp).toBe(s.player.maxHp) // ノーダメージ
  })

  it('打ち消しの対象は任意の行動: 伏せ破壊も無効化できる (確定済みルール)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_set_breaker'), ['green_reaction_root_weave'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_root_weave' })
    s = withIntent(s, destroySetIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(types(s.eventLog)).not.toContain('SetCardDestroyed')
  })

  it('伏せ破壊は素直に通る (2026-08-30 逃がしルール廃止。回収で事前に引き上げるのが後継)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_set_breaker'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, destroySetIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('SetCardDestroyed')
    expect(s.player.setCards).toHaveLength(0)
  })
})

describe('set-confirm (ハイブリッド)', () => {
  it('条件成立で「発動/温存」確認に中断し、発動を選ぶと解決される', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, attackIntent(11))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    expect(s.pendingWindow).toEqual({ enemyIndex: 0, stage: 'post' }) // 返しはダメージ後の窓
    expect(s.player.hp).toBe(s.player.maxHp - 11) // 確認時点で既にダメージを受けている
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - 10)
    expect(s.player.hp).toBe(s.player.maxHp - 11)
    expect(s.turn).toBe(2) // 敵フェーズが最後まで解決され次ターンへ
  })

  it('温存を選ぶと伏せたまま残り、敵の行動は実行される (ブラフの種)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = withIntent(s, attackIntent(11))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(s.player.setCards).toHaveLength(1) // 温存
    expect(s.player.hp).toBe(s.player.maxHp - 11)
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp)
    expect(s.turn).toBe(2)
  })

  it('確認待ち以外での ConfirmReaction は拒否される', () => {
    const s = freshCombat('set-confirm', 'enemy_brute')
    expect(() => applyCommand(s, { type: 'ConfirmReaction', fire: true })).toThrow()
  })
})

describe('hold-manual (構え式)', () => {
  it('敵の行動前に割り込みウィンドウが開き、手札から発動できる (コストは発動時払い)', () => {
    let s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    expect(s.player.energy).toBe(3)
    s = withIntent(s, attackIntent(13))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // 余剰エナジー3で発動可能なため
    s = applyCommand(s, { type: 'ReactManual', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.energy).toBe(3) // 敵ターン中に2へ減った後、次ターン開始で全回復
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - 10)
    expect(s.player.hp).toBe(s.player.maxHp - 13)
    expect(s.turn).toBe(2)
    // 発動時にエナジーを払った痕跡: ReactionTriggered がログにある
    expect(types(s.eventLog)).toContain('ReactionTriggered')
  })

  it('パスすると発動せず、手札のリアクションは空振りとして計上される', () => {
    let s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    s = withIntent(s, attackIntent(13))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(types(s.eventLog)).toContain('ReactionWhiffed')
    expect(s.player.hp).toBe(s.player.maxHp - 13)
  })

  it('余剰エナジーがなければウィンドウは開かない (エナジー持ち越しルールの裏面)', () => {
    let s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, energy: 0 } } // 自ターンで使い切った想定
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.turn).toBe(2) // 中断なしで敵フェーズが解決された
    expect(s.player.hp).toBe(s.player.maxHp - 10)
  })

  it('トリガー不一致 (敵が攻撃以外) なら onAttacked リアクションのウィンドウは開かない', () => {
    let s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    s = withIntent(s, defendIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.turn).toBe(2)
  })

  it('hold-manual では伏せ (SetCard) を受け付けない', () => {
    const s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })).toThrow()
  })

  it('リアクションにも成長カウンターが乗る (与ダメージ全てに加算)', () => {
    let s = withHand(freshCombat('hold-manual', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, growth: 3 } }
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ReactManual', cardUid: 't0_green_reaction_thorns' })
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - (10 + 3))
  })
})

describe('SetCard のエラーメッセージ (2026-08-29。汎用文言に化けていた裁定)', () => {
  it('エナジー不足の伏せは「エナジー不足」と具体的に拒否される', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, energy: 0 } }
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })).toThrow(
      /エナジー不足/,
    )
  })
})

describe('誘発タイミング: 返しはダメージの後 (2026-08-24 変更)', () => {
  it('返しで敵が倒れても、攻撃は先に受けている', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 8 })) }
    s = withIntent(s, attackIntent(14))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('won') // 返し9で敵は倒れる
    expect(s.player.hp).toBe(s.player.maxHp - 14) // だが攻撃は受けた後
  })
})

describe('新しい誘発条件 (条件きつく・効果派手)', () => {
  it('打ち消し (敵行動時) は従来通り実行前に働き、ダメージを受けない', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_root_weave'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_root_weave' })
    s = withIntent(s, attackIntent(16))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(s.player.hp).toBe(s.player.maxHp)
  })

  it('窮鼠の大牙: HPが半分を超えていると発動しない', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, hand: [{ uid: 't0_green_reaction_cornered', def: { ...getCardDef('green_reaction_thorns'), id: 'test_cornered', name: '窮鼠(テスト)', effects: [{ trigger: 'onAttacked' as const, condition: { hpAtOrBelowRatio: 0.5 }, effect: 'counter' as const, amount: 20 }] } }] } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_cornered' })
    s = withIntent(s, attackIntent(5)) // 被弾後もHP45 > 25
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(s.player.setCards).toHaveLength(1) // 空振りして伏せたまま
  })

  it('窮鼠の大牙: 被弾後にHP半分以下なら返し20が発動する', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, hand: [{ uid: 't0_green_reaction_cornered', def: { ...getCardDef('green_reaction_thorns'), id: 'test_cornered', name: '窮鼠(テスト)', effects: [{ trigger: 'onAttacked' as const, condition: { hpAtOrBelowRatio: 0.5 }, effect: 'counter' as const, amount: 20 }] } }] } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_cornered' })
    s = { ...s, player: { ...s.player, hp: 30 } }
    s = withIntent(s, attackIntent(10)) // 被弾後HP20 ≤ 25
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - 20)
  })

  it('共鳴する茨: 敵の強化に反応して成長+4', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_resonance'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_resonance' })
    // 2026-08-30 T1は club になったので、雄叫び (buff) を意図に細工して検証する
    s = withIntent(s, { kind: 'buff', shownMin: 2, shownMax: 4, actual: 3 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('StrengthGained') // 強化自体は通る
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.player.growth).toBe(4)
  })

  it('根穿ち: 敵の防御に反応して貫通12 (得たブロックを素通しする)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_root_pierce'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_root_pierce' })
    s = withIntent(s, defendIntent(14))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp - 12) // 貫通なのでブロック無視
    expect(s.enemies[0].block).toBe(14) // ブロックは削れもしない
  })

  it('行動値X以上の条件 (minActionValue): 実値が10未満なら発動しない、10以上なら返し24 (逆襲の蔦は2026-09-02撤去。機構を合成defで固定)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    const base = s.player.hand[0]
    const def = { ...base.def, id: 'test_backlash', name: '逆襲(テスト)', cost: 2, effects: [{ trigger: 'onAttacked' as const, condition: { minActionValue: 10 }, effect: 'counter' as const, amount: 24 }] }
    s = { ...s, player: { ...s.player, hand: [{ uid: 't0_green_reaction_backlash', def }] } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_backlash' })
    const small = applyCommand(withIntent(s, attackIntent(9)), { type: 'EndTurn' })
    expect(types(small.eventLog)).not.toContain('ReactionTriggered')
    const big = applyCommand(withIntent(s, attackIntent(10)), { type: 'EndTurn' })
    expect(types(big.eventLog)).toContain('ReactionTriggered')
    expect(big.enemies[0].hp).toBe(big.enemies[0].maxHp - 24)
  })
})

describe('上限ランプの消滅 (2026-08-24 決定)', () => {
  it('芽吹き・深根は使用後に消滅の山へ行き、捨て札には行かない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), [
      'green_ramp_sprout',
      'green_ramp_deep_roots',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_ramp_sprout' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_ramp_deep_roots' })
    expect(s.player.exhaustPile).toHaveLength(2)
    expect(s.player.discardPile).toHaveLength(0)
    expect(s.player.energyMax).toBe(6)
  })
})
