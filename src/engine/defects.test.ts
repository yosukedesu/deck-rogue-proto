// 2026-08-26 に発見・修正した実装欠陥の回帰テスト。
// いずれも「宣言済みの仕様が engine 側で黙殺されていた」型のバグで、
// 既存テストの網の外にあった (だから long-lived になっていた)。
import { describe, expect, it } from 'vitest'
import { startCombatWithOptions } from './combat.ts'
import { buildDeck, getCardDef } from './content.ts'
import { countedPermanents } from './effects.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { CardInstance, EnemyIntent, GameState } from './types.ts'

/** 敵フェーズを最後まで進めて次の自ターン (=意図の再宣言後) まで到達させる */
function toNextPlayerTurn(state: GameState): GameState {
  let s = applyCommand(state, { type: 'EndTurn' })
  let guard = 0
  while (s.phase === 'awaiting-reaction' && guard++ < 20) {
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
  }
  return s
}

/** リーダー付きの戦闘を作る (freshCombat はリーダーを注入しない) */
function combatWithLeader(deckId: string, leaderId: string): GameState {
  return startCombatWithOptions(42, 'set-confirm', 'enemy_brute', {
    deck: buildDeck(deckId),
    leaderId,
  })
}

/** 置物を直接場に置く (プレイ経路を通さずに盤面だけ作る) */
function withPermanents(state: GameState, cards: readonly CardInstance[]): GameState {
  return { ...state, player: { ...state.player, permanents: cards } }
}

describe('従者狩り (destroy-token) が到達不能だった件', () => {
  // 旧実装: `def.movesVsSet ?? def.movesVsTokens`。両テーブルを持つ唯一の敵 (罠壊し) では
  // movesVsSet が常に勝つため、movesVsTokens は production から一度も選ばれなかった。
  it('伏せが無く従者が居る時、罠壊しは従者反応 (tokens) の分岐を予告する', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 7, 'starter_white')
    s = withPermanents(s, [
      { uid: 'tok0', def: getCardDef('white_perm_squire'), token: true },
    ])
    // 意図は敵フェーズ→次ターン開始で宣言し直される
    s = toNextPlayerTurn(s)
    expect(s.player.setCards).toHaveLength(0)
    expect(s.enemies[0].intent?.conditionalOn).toBe('tokens')
  })

  it('伏せがある時は伏せ反応 (set) が優先される (伏せ反応 > 従者反応)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 7, 'starter_white')
    s = withPermanents(s, [
      { uid: 'tok0', def: getCardDef('white_perm_squire'), token: true },
    ])
    s = withHand(s, ['white_reaction_ward'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_white_reaction_ward' })
    // 罠壊しの本来の行動は伏せ破壊なので、素の攻撃に差し替えて伏せを次ターンまで残す
    s = withIntent(s, attackIntent(5))
    s = toNextPlayerTurn(s)
    expect(s.player.setCards).toHaveLength(1) // 温存したので伏せは残っている
    expect(s.enemies[0].intent?.conditionalOn).toBe('set')
  })

  it('伏せも従者も無い時は伏せ反応を既定にして「伏せれば変わる」を予告し続ける', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 7, 'starter_white')
    s = withPermanents(s, [])
    s = toNextPlayerTurn(s)
    expect(s.enemies[0].intent?.conditionalOn).toBe('set')
  })
})

describe('伏せ札の消滅が黙殺されていた件', () => {
  it('消滅を持つリアクション (毒針の囮) は、発動後に捨て札でなく消滅置き場へ行く', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), [
      'green_decoy_needle',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_decoy_needle' })
    s = withIntent(s, attackIntent(6))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_green_decoy_needle' })
    expect(s.player.exhaustPile.map((c) => c.def.id)).toEqual(['green_decoy_needle'])
    expect(s.player.discardPile.map((c) => c.def.id)).not.toContain('green_decoy_needle')
  })
})

describe('置物数参照がリーダーパッシブ・レリックを数えていた件', () => {
  it('リーダーパッシブは置物数に数えない (パッシブは置物ではない)', () => {
    const s = combatWithLeader('starter_white', 'leader_white')
    // ひなたのパッシブ置物は場にあるが、数えるのは0体
    expect(s.player.permanents.length).toBeGreaterThan(0)
    expect(countedPermanents(s)).toBe(0)
  })

  it('パッシブが召喚したトークンや、プレイした置物は数える', () => {
    let s = combatWithLeader('starter_white', 'leader_white')
    const innateCount = s.player.permanents.length
    s = withHand(s, ['white_perm_squire'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    expect(s.player.permanents).toHaveLength(innateCount + 1)
    expect(countedPermanents(s)).toBe(1)
  })

  it('集結のダメージは「場に出た置物」の数だけで決まる', () => {
    let s = combatWithLeader('starter_white', 'leader_white')
    s = withPermanents(s, [
      ...s.player.permanents, // リーダーパッシブ (innate) を残したまま
      { uid: 'p1', def: getCardDef('white_perm_squire') },
      { uid: 'p2', def: getCardDef('white_perm_squire') },
    ])
    s = withHand(s, ['white_rally'])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_rally' })
    const rally = getCardDef('white_rally').effects.find(
      (e) => e.effect === 'dealDamagePerPermanent',
    )
    expect(hpBefore - s.enemies[0].hp).toBe(2 * (rally?.amount ?? 0)) // パッシブ抜きの2体ぶん
  })
})

describe('条件付き意図とリアクションの相互作用 (2026-08-26 プレイテスト発見)', () => {
  // 旧実装は executeEnemyAction で分岐を再計算していたため、pre窓でリアクションを発動して
  // 伏せ枠が空になると「伏せ札あり」の弱い分岐から「なし」の強い分岐へ化けていた。
  // 確認ウィンドウが攻撃8と表示したのに16で解決される = 窓が嘘をつく状態だった。
  it('pre窓でリアクションを発動して伏せ枠が空になっても、分岐は行動開始時のまま固定される', () => {
    // 「伏せ札あり→攻撃8 / なし→攻撃16」の条件付き意図を直接作る
    const conditional: EnemyIntent = {
      kind: 'attack',
      shownMin: 15,
      shownMax: 19,
      actual: 16,
      conditionalOn: 'set',
      alt: { kind: 'attack', shownMin: 7, shownMax: 10, actual: 8 },
    }
    let s = withHand(freshCombat('set-confirm', 'enemy_joker', 3, 'starter'), [
      'green_reaction_vine', // 1E・被攻撃前にブロック12 (pre窓)
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_vine' })
    s = withIntent(s, conditional)
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // 伏せがあるので pre窓が開く
    s = applyCommand(s, {
      type: 'ConfirmReaction',
      fire: true,
      cardUid: 't0_green_reaction_vine',
    })
    expect(s.player.setCards).toHaveLength(0) // 発動して伏せ枠は空になった
    // ブロック12 は 分岐後の攻撃8 を完封する。
    // バグがあると「伏せ札なし」の16で解決され、12を引いた4が漏れる
    expect(hpBefore - s.player.hp).toBe(0)
  })
})

describe('複数体戦の条件付き意図 (2026-08-28 seed601プレイテストの「窓が嘘をつく」報告の裁定)', () => {
  // 報告: 「伏せたのに敵が伏せなし分岐(攻撃12)を実行した」→ ログ精査の結果、エンジンは正しかった。
  // 伏せ札が1体目の行動(post窓)で発動して伏せ枠が空き、2体目の行動開始時に「伏せなし」分岐が
  // 正規に確定していた。分岐は「その敵の行動開始時」の盤面で確定する仕様どおり。
  // このテストは発動/温存の両ケースでクロス敵のセマンティクスを固定する。
  function twoEnemyState(): GameState {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter')
    s = withHand(s, ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    const conditional: EnemyIntent = {
      kind: 'attack',
      shownMin: 8,
      shownMax: 12,
      actual: 12,
      conditionalOn: 'set',
      alt: { kind: 'defend', shownMin: 6, shownMax: 9, actual: 6 },
    }
    s = {
      ...s,
      enemies: s.enemies.map((e, i) => ({
        ...e,
        hp: 50,
        maxHp: 50,
        intent: i === 0 ? attackIntent(6) : conditional,
      })),
    }
    return s
  }

  it('1体目の攻撃に返し札を「発動」すると、2体目は行動開始時に伏せなし分岐 (攻撃) で確定する', () => {
    let s = twoEnemyState()
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // 1体目の攻撃6の post窓
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_green_reaction_thorns' })
    // 伏せ枠が空いたので、2体目の行動開始時には「伏せなし」= 攻撃12 が選ばれる
    expect(s.player.setCards).toHaveLength(0)
    expect(hpBefore - s.player.hp).toBe(6 + 12)
  })

  it('温存すれば伏せは残り、2体目は伏せあり分岐 (防御) で確定する', () => {
    let s = twoEnemyState()
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    // 温存 → 伏せあり → 2体目は防御分岐。被ダメは1体目の6だけ
    expect(s.player.setCards).toHaveLength(1)
    expect(hpBefore - s.player.hp).toBe(6)
  })
})
