// 黒 (5色目) のテスト。黒の柱: ドレイン / 墓地 (ミル+捨て札参照) / 自傷ペイオフ / 呪いリアクション。
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, getDeckDef } from './content.ts'

import { applyCommand } from './state.ts'
import { createRunInBattle, attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('黒のカラーパイ', () => {
  it('黒のカードとデッキが揃っている (5色完成)', () => {
    expect(allCards.filter((c) => c.color === 'black').length).toBeGreaterThanOrEqual(19)
    for (const id of ['starter_black', 'deck_drain', 'deck_graveyard', 'run_basic_black']) {
      expect(buildDeck(id).length).toBeGreaterThan(0)
      expect(getDeckDef(id).color).toBe('black')
    }
  })

  it('黒ランは黒の基本デッキで始まり、色が保持される', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_black')
    expect(run.colors).toEqual(['black'])
    expect(run.deck.every((c) => c.def.color === 'black')).toBe(true)
  })
})

describe('ドレイン (黒の専売)', () => {
  it('生命吸収: 6ダメージを与え、HP3回復 (上限あり)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_drain',
    ])
    s = { ...s, player: { ...s.player, hp: 50 } }
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_drain' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6)
    expect(s.player.hp).toBe(53)
  })

  it('呪詛返し: 被攻撃後にドレイン6で返す', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_curse',
    ])
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_curse' })
    s = withIntent(s, attackIntent(6))
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBe(enemyHp - 6)
    expect(s.player.hp).toBe(50 - 6 + 3) // 被弾6 → ドレイン6の回復3
  })
})

describe('墓地 (セルフミル + 捨て札参照)', () => {
  it('忘却の霧: 山札の上4枚が消滅し、1枚引く (墓地=消滅置き場)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_mill',
    ])
    const drawBefore = s.player.drawPile.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_mill' })
    // 忘却4 (消滅)。ドローは2026-08-27の2効果整理でブロック5に置き換わった
    expect(s.player.drawPile.length).toBe(drawBefore - 4)
    expect(s.player.exhaustPile.length).toBe(4)
  })

  it('亡霊の槍: 忘却の刻 (消滅7枚以上) で6→12に強化される', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_grave_bolt',
      'black_grave_bolt',
    ])
    const enemyHp = s.enemies[0].hp
    // 刻の前: 基礎6
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_grave_bolt' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6)
    // 消滅を7枚に細工 → 刻の後: 12
    const pad = s.player.drawPile.slice(0, 7)
    s = {
      ...s,
      player: {
        ...s.player,
        drawPile: s.player.drawPile.slice(7),
        exhaustPile: [...s.player.exhaustPile, ...pad],
      },
    }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_black_grave_bolt' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6 - 12)
  })
})

describe('自傷ペイオフ', () => {
  it('痛みの対価: HP3を失い16ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_pain',
    ])
    const hp = s.player.hp
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_pain' })
    expect(s.player.hp).toBe(hp - 3)
    expect(s.enemies[0].hp).toBe(enemyHp - 16) // 2026-08-27 報酬プール底上げ
  })
})

describe('リーダーとばり', () => {
  it('パッシブ: 攻撃カードをプレイするたびHP1回復', () => {
    const run = createRunInBattle(3, 'set-confirm', 'leader_black')
    let s = run.combat!
    s = { ...s, player: { ...s.player, hp: 50 } }
    // プール変更で敵がとげ持ちになるシードがあるため、反射を外して回復だけを測る
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, thorns: 0 })) }
    s = withHand(s, ['black_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_strike', targetIndex: 0 })
    expect(s.player.hp).toBe(51)
  })
})
