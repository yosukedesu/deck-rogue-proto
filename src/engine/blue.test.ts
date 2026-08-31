// 青 (2色目) のテスト。青の柱: ドロー / 打ち消しの本家 / ストーム / 氷壁。
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, getDeckDef } from './content.ts'
import { applyRunCommand } from './run.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, attackIntent, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent, GameState } from './types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)

describe('カラーパイ', () => {
  it('カードは色を持つ (ファイル単位で付与)', () => {
    expect(allCards.filter((c) => c.color === 'green').length).toBeGreaterThan(0)
    expect(allCards.filter((c) => c.color === 'blue').length).toBeGreaterThan(0)
    expect(allCards.find((c) => c.id === 'blue_counterspell')?.color).toBe('blue')
    expect(allCards.find((c) => c.id === 'green_strike')?.color).toBe('green')
  })

  it('青のデッキがすべて構築できる', () => {
    for (const id of ['starter_blue', 'deck_storm', 'deck_permission', 'run_basic_blue']) {
      expect(buildDeck(id).length).toBeGreaterThan(0)
      expect(getDeckDef(id).color).toBe('blue')
    }
  })
})

describe('氷壁 (持ち越しブロック)', () => {
  it('氷壁はターン開始で消えず、通常ブロックの後に消費される', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_ice_wall',
      'green_guard', // 通常ブロック役 (氷盾は2026-08-25に氷壁化したため緑の防御を借りる)
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ice_wall' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_guard' })
    expect(s.player.iceBlock).toBe(15) // 2026-08-30 凍結遺産の下限引き上げ (13→15)
    expect(s.player.block).toBe(5)
    // 攻撃10: 通常ブロック5を先に消費し、残り5を氷壁で受ける
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(s.player.maxHp)
    expect(s.player.block).toBe(0) // 次ターン開始でリセット
    expect(s.player.iceBlock).toBe(10) // 15 - 5 が持ち越されている
  })
})

describe('ストーム (詠唱数参照)', () => {
  it('奔流の連撃はこのターンにプレイした他のカード×3 (2026-08-31 稼ぐ札と使う札の帯分離で1E×3化。自身は数えない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_guard',
      'blue_current_lash',
      'blue_storm_lash',
    ])
    s = { ...s, player: { ...s.player, energy: 6 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_guard' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_current_lash' }) // 2枚目 (7ダメージ。2026-08-30 引き上げ)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_blue_storm_lash' }) // 3枚目: 詠唱数2 ×3 = 6
    expect(s.enemies[0].hp).toBe(hpBefore - 7 - 6)
    // ターンをまたぐとリセット
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.cardsPlayedThisTurn).toBe(0)
  })

  it('嵐の残響: 詠唱数は敵フェーズ中も生きており、伏せた残響が詠唱数×3で返す (ストーム×伏せの橋)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_ponder',
      'blue_guard',
      'blue_storm_echo',
    ])
    s = { ...s, player: { ...s.player, energy: 6 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ponder' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_guard' })
    // 伏せはプレイではないので詠唱数に数えない
    s = applyCommand(s, { type: 'SetCard', cardUid: 't2_blue_storm_echo' })
    expect(s.player.cardsPlayedThisTurn).toBe(2)
    s = withIntent(s, attackIntent(5))
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    // set-confirm: 被攻撃後の確認ウィンドウ → 発動
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBe(enemyHp - 6) // 詠唱数2 ×3
  })
})

describe('青の打ち消し (本家)', () => {
  it('マナ漏出: 敵の行動の値が15以下なら打ち消せる (2026-08-31 ≤12→≤15。幕3打点帯で死んでいた是正)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute', 42, 'starter_blue'), ['blue_mana_leak'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_mana_leak' })
    s = withIntent(s, attackIntent(15))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(s.player.hp).toBe(s.player.maxHp)
  })

  it('マナ漏出: 16以上の攻撃は打ち消せず素通しになる (空振り)', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute', 42, 'starter_blue'), ['blue_mana_leak'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_mana_leak' })
    s = withIntent(s, attackIntent(16))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).not.toContain('ActionNegated')
    expect(s.player.hp).toBe(s.player.maxHp - 16)
    expect(s.player.setCards).toHaveLength(1) // 伏せたまま持続
  })

  it('冷徹な観察: 被攻撃後にドロー2とエナジー+1のテンポ回収', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute', 42, 'starter_blue'), ['blue_cold_reading'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_cold_reading' })
    const energyAfterSet = s.player.energy
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.turn).toBe(2)
    expect(energyAfterSet).toBe(2) // 伏せコストの確認のみ
  })
})

describe('青の置物', () => {
  it('賢者の泉: 毎ターン開始時に1枚ドロー (手札6枚スタートになる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_sage_spring',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_sage_spring' })
    s = withIntent(s, defendIntent(3))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hand).toHaveLength(6) // 5 + 泉の1枚
  })
})

describe('青のラン', () => {
  it('青ランは青の基本デッキで始まり、報酬は青のカードのみ (基本札除外)', () => {
    let run = createRunInBattle(31, 'set-confirm', 'leader_blue')
    expect(run.colors).toEqual(['blue'])
    expect(run.deck).toHaveLength(10)
    expect(run.deck.every((c) => c.def.color === 'blue')).toBe(true)
    // 外科的に勝利して報酬を確認
    const c = run.combat!
    let surgical: GameState = {
      ...c,
      // 先頭だけ残して全滅寸前に (編成戦でも単体攻撃1発で勝てる状態を作る)
      enemies: c.enemies.map((e, i) => ({ ...e, hp: i === 0 ? 1 : 0, block: 0 })),
    }
    surgical = withIntent(withHand(surgical, ['blue_current_lash']), defendIntent(0))
    run = applyRunCommand(
      { ...run, combat: surgical },
      // 複数体編成の可能性があるため対象を明示 (StS式ターゲティング)
      { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_blue_current_lash', targetIndex: 0 } },
    )
    expect(run.phase).toBe('reward')
    for (const cardId of run.rewardOptions!) {
      expect(cardId.startsWith('blue_')).toBe(true)
      expect(['blue_strike', 'blue_guard']).not.toContain(cardId)
    }
  })
})

describe('霊気 (妨害→フィニッシュ変換)', () => {
  it('対抗呪文: 打ち消しと同時に霊気+1が溜まる', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute', 42, 'starter_blue'), ['blue_counterspell'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_counterspell' })
    s = withIntent(s, attackIntent(15))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('ActionNegated')
    expect(s.player.aether).toBe(1)
  })

  it('霊気放出: 霊気×7ダメージを与えて霊気を全消費する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_aether_burst',
    ])
    s = { ...s, player: { ...s.player, aether: 4 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, armor: undefined })) } // 装甲を外して素値を測る
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_aether_burst' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4 * 7)
    expect(s.player.aether).toBe(0)
    expect(types(s.eventLog)).toContain('AetherDischarged')
  })

  it('霊気0の放出は不発 (何も起きない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_aether_burst',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_aether_burst' })
    expect(s.enemies[0].hp).toBe(hpBefore)
  })
})

describe('ストームの3系統', () => {
  it('渦の障壁: 詠唱数×4の氷壁を得る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_guard',
      'blue_ponder',
      'blue_storm_barrier',
    ])
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_guard' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ponder' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_blue_storm_barrier' }) // 詠唱数2 ×5 = 10
    expect(s.player.iceBlock).toBe(10 + 5 + 3) // 障壁10 + 氷盾5 + 思案の氷壁3
  })

  it('連鎖する思考: 詠唱数×1枚ドロー', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_guard',
      'blue_mana_convert',
      'blue_chain_thought',
    ])
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_guard' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_mana_convert' }) // +1枚ドロー
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_blue_chain_thought' }) // 詠唱数2 → 2枚
    expect(s.player.hand.length).toBe(handBefore - 1 + 2)
  })
})

describe('0マナスペルとマナ軽減', () => {
  it('魔力の火花: 0マナで「次のカード-2」を得て消滅する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_spark',
      'blue_storm_lash',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_spark' })
    expect(s.player.nextCardDiscount).toBe(2)
    expect(s.player.exhaustPile).toHaveLength(1) // 火花は消滅
    expect(s.player.energy).toBe(3) // 0マナ
    // 連撃 (コスト2) が0で撃てて、割引は消費される
    s = { ...s, player: { ...s.player, energy: 0 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_storm_lash' })
    expect(s.player.energy).toBe(0)
    expect(s.player.nextCardDiscount).toBe(0)
  })

  it('素のコスト0のカードは割引を消費しない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_spark',
      'blue_flash',
      'blue_current_lash',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_spark' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_flash' }) // 0マナ消滅ドロー
    expect(s.player.nextCardDiscount).toBe(2) // 割引は温存されている
    expect(s.player.exhaustPile).toHaveLength(2) // ひらめきも消滅
  })

  it('集中: 2ドローと「次のカード-2」。使用後は消滅し、割引はターンを跨いで持ち越す', () => {
    // 消滅は無限ループ対策 (2026-08-26)。割引で自分自身が実質0マナになり、
    // 引き直して戻ってくる完全な循環が閉じていた (deck_storm でターン3が終わらなかった)。
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_focus'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_focus' })
    expect(s.player.nextCardDiscount).toBe(2)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toContain('blue_focus')
    s = withIntent(s, defendIntent(3))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.nextCardDiscount).toBe(2) // 持ち越し
  })
})
