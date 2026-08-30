// 赤=速さの色への再設計 (2026-08-26) のテスト。
// 人間プレイテストで「赤は緑と同じ攻撃力＋劣る防御＝純粋な下位互換」と判明したのを受け、
// 確定済みルール表「赤の柱⑤速さで対価を払う / ⑥防御は割高のままだが腐らせない」を固定する。
import { describe, expect, it } from 'vitest'
import { startCombatWithOptions } from './combat.ts'
import { buildDeck, getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** リーダー付きの戦闘 (freshCombat はリーダーを注入しない) */
function startCombatWithLeader(deckId: string, leaderId: string): GameState {
  return startCombatWithOptions(42, 'set-confirm', 'enemy_probe', {
    deck: buildDeck(deckId),
    leaderId,
  })
}

/** 山札の中身を差し替える (衝動ドローの対象を決定的にするため) */
function withDrawPile(state: GameState, cardIds: readonly string[]): GameState {
  return {
    ...state,
    player: {
      ...state.player,
      drawPile: cardIds.map((id, i) => ({ uid: `d${i}_${id}`, def: getCardDef(id) })),
    },
  }
}

describe('赤の0マナ攻撃 (火花)', () => {
  it('0マナ・4ダメージで、消滅せず捨て札に行く (何度でも回ってくる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_spark'])
    const energy = s.player.energy
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spark' })
    expect(s.enemies[0].hp).toBe(enemyHp - 4) // 2026-08-30 緑の0E帯へ引き上げ
    expect(s.player.energy).toBe(energy) // エナジーを1点も使わない
    expect(s.player.discardPile.map((c) => c.def.id)).toContain('red_spark')
    expect(s.player.exhaustPile).toHaveLength(0) // 消滅必須ルールの対象外 (補充を伴わないため)
  })

  it('手数が増えるので「攻撃プレイ後」誘発が余分に乗る (残り火で延焼+1)', () => {
    // 赤が速さで対価を受け取る核心: 火花は火弾より低火力だが、延焼・勢い・パッシブを1回多く誘発させる
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_perm_ember',
      'red_spark',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_ember' })
    expect(s.enemies[0].burn).toBe(0)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_spark' })
    expect(s.enemies[0].burn).toBe(2) // 0マナの一手が延焼を2点積む (2026-08-30 残り火を引き上げ)
  })

  it('連打しても手札が尽きて必ず止まる (補充を伴わない0マナはループしない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_spark',
      'red_spark',
      'red_spark',
    ])
    for (let i = 0; i < 3; i++) s = applyCommand(s, { type: 'PlayCard', cardUid: `t${i}_red_spark` })
    expect(s.player.hand).toHaveLength(0)
    expect(s.player.energy).toBe(3) // エナジーは減らないが、手札という別の在庫が尽きる
  })
})

describe('灰の盾 (防御に衝動ドローを付けて腐らせない)', () => {
  it('ブロック4 ＋ 山札の一番上を「このターン限り」で手札に加える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_guard'])
    s = withDrawPile(s, ['red_spark', 'red_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' })
    expect(s.player.block).toBe(4)
    expect(s.player.hand.map((c) => c.def.id)).toEqual(['red_spark'])
    expect(s.player.impulseUids).toEqual(['d0_red_spark']) // 衝動 = 未使用ならターン終了時に消滅
  })

  it('引いたのが0マナ攻撃なら、同じ1エナジーで防御と攻撃が同時に成立する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), ['red_guard'])
    s = withDrawPile(s, ['red_spark'])
    const energy = s.player.energy
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'd0_red_spark' })
    expect(s.player.block).toBe(4)
    expect(s.enemies[0].hp).toBe(enemyHp - 4)
    expect(s.player.energy).toBe(energy - 1) // 灰の盾の1Eだけ。攻防一体
  })
})

describe('熾火の一閃 (火弾の上位互換: 6ダメ+衝動1。2026-08-27 報酬プール底上げ)', () => {
  it('6ダメージ＋衝動2 (このターン限りのドロー)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red'), [
      'red_ember_slash',
    ])
    s = withDrawPile(s, ['red_strike', 'red_spark'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ember_slash' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6) // 火弾と同値+衝動2
    expect(s.player.hand).toHaveLength(2)
    expect(s.player.impulseUids).toHaveLength(2) // 衝動 = このターン限り (赤のドローは衝動のみ)
  })
})

describe('ひばなのパッシブ = 手数を勢いに変える (2026-08-27)', () => {
  // 「守らない色」なのに勝ち筋のバーンが時間を要求する、という思想の自己矛盾への回答。
  // 主軸を「時間をかけて焼く」から「手数で加速して短期決着する」へ移した。
  // いぶき(グルール)の「攻撃ごと勢い+1」との差別化は、種類を問わない点と量。
  it('攻撃カードをプレイするたび勢い+2。防御札では乗らない (2026-08-31 攻撃特化へ変更)', () => {
    // 旧「カードを1枚プレイするたび+2」は 0マナ×衝動×多段と組んでn²の火力曲線を作り、
    // 速さを「唯一の柱」にしていた (5色計測で赤の面白さ最下位の主因)。
    // 攻撃だけが勢いを産む = 攻撃の連鎖だけが加速する。ユーザー案
    let s = startCombatWithLeader('starter_red', 'leader_red')
    expect(s.player.momentum).toBe(0)
    s = withHand(s, ['red_guard']) // 防御札 (ブロック4+衝動ドロー1)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' })
    expect(s.player.momentum).toBe(0) // 防御では乗らない
    s = withHand(s, ['red_spark'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spark' })
    expect(s.player.momentum).toBe(2) // 攻撃 (0マナ火花も攻撃) で乗る
  })

  it('手数を重ねるほど後続の攻撃が加速する (0マナ攻撃が加速装置になる)', () => {
    let s = startCombatWithLeader('starter_red', 'leader_red')
    s = withHand(s, ['red_spark', 'red_spark', 'red_strike'])
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_spark' }) // 4ダメ (勢い0で解決→勢い+2)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_spark' }) // 4+2=6ダメ →勢い+2
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_red_strike' }) // 6+4=10ダメ
    expect(hp0 - s.enemies[0].hp).toBe(4 + 6 + 10)
    expect(s.player.momentum).toBe(6)
  })
})
