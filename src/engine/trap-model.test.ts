// 罠モデル (2026-09-13 伏せの再定義。docs/set-trap-redesign-2026-09-13.md §3・§9 / CLAUDE.md「罠モデル（伏せの寿命）」) の機械固定。
// 伏せたターンは鳴らない (準備) → 翌・翌々ターンの敵フェーズだけ生きる (2窓) → 鳴らなければほどけて捨て札 (消滅持ちは消滅)。
// 回収は廃止。敵は伏せを見ない (残るのは罠壊し・道化の破壊分岐だけ)。数値は据え置きで発火に「形」を足す。
import { describe, expect, it } from 'vitest'
import { allEnemies, applyDebugOverrides, clearDebugOverrides, getCardDef, getEnemyDef } from './content.ts'
import { effectiveIntent, isTrapLive, trapAge, trapStatusText, trapWindowsLeft } from './effects.ts'
import { applyCommand } from './state.ts'
import {
  attackIntent,
  createRunInBattle,
  destroySetIntent,
  freshCombat,
  passTurn,
  setAndArm,
  withHand,
  withIntent,
} from './test-helpers.ts'
import type { GameEvent, GameState } from './types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)
const count = (log: readonly GameEvent[], type: GameEvent['type']) => log.filter((e) => e.type === type).length

/** 伏せ (set-confirm) → 敵の攻撃 → 窓が開けば発動、開かなければそのまま */
function attackAndFire(state: GameState, actual: number, cardUid?: string): GameState {
  let s = applyCommand(withIntent(state, attackIntent(actual)), { type: 'EndTurn' })
  let guard = 0
  while (s.phase === 'awaiting-reaction' && guard++ < 10) {
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, ...(cardUid ? { cardUid } : {}) })
  }
  return s
}

describe('準備ターン: 伏せたターンの敵フェーズでは鳴らない', () => {
  it('茨の返しを伏せた同じターンの攻撃には窓が開かず、setTurn に伏せたターンが入る (空振りにも数えない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.setCards[0].setTurn).toBe(1)
    expect(trapAge(s, s.player.setCards[0])).toBe(0)
    expect(isTrapLive(s, s.player.setCards[0])).toBe(false)
    expect(trapStatusText(s, s.player.setCards[0])).toContain('準備中')
    expect(trapWindowsLeft(s, s.player.setCards[0])).toBe(2)
    s = withIntent(s, attackIntent(12))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn') // 窓は開かない
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(types(s.eventLog)).not.toContain('ReactionWhiffed') // 準備ターンは統計に載らない
    expect(s.player.hp).toBe(s.player.maxHp - 12)
    expect(s.enemies[0].hp).toBe(s.enemies[0].maxHp)
    expect(s.player.setCards).toHaveLength(1)
  })

  it('set-auto でも準備ターンは自動発動しない', () => {
    let s = withHand(freshCombat('set-auto', 'enemy_brute'), ['green_reaction_thorns'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    s = applyCommand(withIntent(s, attackIntent(12)), { type: 'EndTurn' })
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(s.player.setCards).toHaveLength(1)
  })

  it('自己誘発 (反響の符=呪文プレイ) も伏せたターンは鳴らず、翌ターンから鳴る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), ['blue_echo_seal', 'blue_ponder'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_echo_seal' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ponder' })
    expect(s.player.setCards).toHaveLength(1) // 準備中は起爆しない
    expect(s.player.aether).toBe(0)
    s = withHand(passTurn(s), ['blue_ponder'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ponder' })
    expect(s.player.setCards).toHaveLength(0)
    expect(s.player.aether).toBe(2)
  })
})

describe('寿命2窓: 翌ターンと翌々ターンの敵フェーズだけ候補になる', () => {
  it('窓1 (翌ターン) で鳴る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    expect(s.turn).toBe(2)
    expect(trapAge(s, s.player.setCards[0])).toBe(1)
    expect(isTrapLive(s, s.player.setCards[0])).toBe(true)
    expect(trapWindowsLeft(s, s.player.setCards[0])).toBe(2)
    const hp0 = s.enemies[0].hp
    s = withIntent(s, attackIntent(11))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBe(hp0 - 10)
    expect(s.player.setCards).toHaveLength(0)
  })

  it('窓1で温存しても窓2 (翌々ターン) で鳴る。温存は空振りとして数える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    s = applyCommand(withIntent(s, attackIntent(5)), { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false }) // 温存
    expect(s.turn).toBe(3)
    expect(count(s.eventLog, 'ReactionWhiffed')).toBe(1)
    expect(s.player.setCards).toHaveLength(1)
    expect(trapWindowsLeft(s, s.player.setCards[0])).toBe(1)
    expect(trapStatusText(s, s.player.setCards[0])).toContain('あと1回')
    const hp0 = s.enemies[0].hp
    s = attackAndFire(s, 5)
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    expect(s.enemies[0].hp).toBe(hp0 - 10)
    expect(s.player.setCards).toHaveLength(0)
  })

  it('複数体戦では生きている窓の全ての行動が候補 (温存は何度でもできる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter'), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, intent: attackIntent(3) })) }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    expect(s.pendingWindow?.enemyIndex).toBe(0)
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.phase).toBe('awaiting-reaction')
    expect(s.pendingWindow?.enemyIndex).toBe(1)
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.phase).toBe('player-turn')
    expect(s.player.setCards).toHaveLength(1)
  })
})

describe('期限切れ: 2窓目の敵フェーズ終端で鳴っていなければほどける', () => {
  it('3ターン目 (窓2) の終端で捨て札へ行き SetCardExpired {to:discard} が出る。空振りは2回だけ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns') // T1 伏せ → T2 (窓1)
    s = passTurn(s) // 窓1 空振り → T3 (窓2)
    expect(s.player.setCards).toHaveLength(1)
    expect(types(s.eventLog)).not.toContain('SetCardExpired')
    s = passTurn(s) // 窓2 空振り → 終端でほどける
    expect(s.turn).toBe(4)
    expect(s.player.setCards).toHaveLength(0)
    // 捨て札へ (T4 のドローで捨て札が切り直されるので山札・手札も含めて探す)
    const piles = [...s.player.discardPile, ...s.player.drawPile, ...s.player.hand]
    expect(piles.some((c) => c.uid === 't0_green_reaction_thorns')).toBe(true)
    expect(s.player.exhaustPile.some((c) => c.uid === 't0_green_reaction_thorns')).toBe(false)
    const expired = s.eventLog.find((e) => e.type === 'SetCardExpired')
    expect(expired).toMatchObject({ type: 'SetCardExpired', cardId: 'green_reaction_thorns', to: 'discard' })
    expect(count(s.eventLog, 'ReactionWhiffed')).toBe(2) // 準備ターンは数えない
    // 期限切れの札は setTurn を持ち越さない (次に伏せれば新しい罠)
    const back = piles.find((c) => c.uid === 't0_green_reaction_thorns')!
    expect(back.setTurn).toBeUndefined()
  })

  it('消滅持ちの札は消滅置き場へ行き、CardExhausted → onCardExhausted (亡者の合唱) → 亡骸が鳴る', () => {
    applyDebugOverrides({
      cards: [
        { id: 'test_trap_exhaust', name: 'テスト罠(消滅)', cost: 0, type: 'reaction', color: 'black', exhaust: true,
          effects: [
            { trigger: 'onAttacked', effect: 'counter', amount: 5 },
            { trigger: 'onSelfExhausted', effect: 'dealDamage', amount: 4 },
          ] },
      ],
    })
    try {
      let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), ['black_perm_chorus', 'test_trap_exhaust'])
      s = { ...s, player: { ...s.player, energy: 9 } }
      s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_perm_chorus' })
      s = setAndArm(s, 't1_test_trap_exhaust')
      s = passTurn(s)
      const hp0 = s.enemies[0].hp
      s = passTurn(s)
      expect(s.player.setCards).toHaveLength(0)
      expect(s.player.exhaustPile.some((c) => c.uid === 't1_test_trap_exhaust')).toBe(true)
      expect(s.player.discardPile.some((c) => c.uid === 't1_test_trap_exhaust')).toBe(false)
      expect(s.eventLog.find((e) => e.type === 'SetCardExpired')).toMatchObject({ to: 'exhaust' })
      expect(s.eventLog.some((e) => e.type === 'CardExhausted' && e.cardId === 'test_trap_exhaust')).toBe(true)
      // 亡者の合唱 (消滅ごと1ダメ) + 亡骸 (4ダメ) = 敵HP-5
      expect(hp0 - s.enemies[0].hp).toBe(1 + 4)
    } finally {
      clearDebugOverrides()
    }
  })

  it('回収の紐 (expireToHand): ほどけた札は捨て札でなく手札に戻り、全捨てを生き残る', () => {
    let s = withHand({ ...freshCombat('set-confirm', 'enemy_brute'), expireToHand: true }, ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    s = passTurn(s)
    s = passTurn(s)
    expect(s.player.setCards).toHaveLength(0)
    expect(s.eventLog.find((e) => e.type === 'SetCardExpired')).toMatchObject({ to: 'hand' })
    expect(s.player.hand.some((c) => c.uid === 't0_green_reaction_thorns')).toBe(true)
    expect(s.player.discardPile.some((c) => c.uid === 't0_green_reaction_thorns')).toBe(false)
    // 全捨てを生き残って次の自ターンの手札 (ターン開始時の手札) に載っている
    const started = s.eventLog.filter((e) => e.type === 'TurnStarted').at(-1)
    expect(started).toMatchObject({ turn: 4 })
    expect((started as { hand?: string[] }).hand).toContain('茨の返し')
  })

  it('大樹の守り手 (trapPersist) は期限が来ない。準備ターンは普通に鳴らない', () => {
    expect(getCardDef('green_reaction_tree_warden').trapPersist).toBe(true)
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_tree_warden'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_tree_warden' })
    expect(trapWindowsLeft(s, s.player.setCards[0])).toBeNull()
    expect(trapStatusText(s, s.player.setCards[0])).toContain('準備中')
    s = applyCommand(withIntent(s, attackIntent(10)), { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn') // 準備ターン
    for (let i = 0; i < 4; i++) s = passTurn(s)
    expect(s.turn).toBe(6)
    expect(s.player.setCards).toHaveLength(1)
    expect(types(s.eventLog)).not.toContain('SetCardExpired')
    expect(isTrapLive(s, s.player.setCards[0])).toBe(true)
    expect(trapStatusText(s, s.player.setCards[0])).toBe('ほどけない')
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // 齢5でも鳴る
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.growth).toBe(2)
  })

  it('弾け実の罠は期限切れでも弾ける (onSetDestroyed 相当。敵全体14)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter'), ['green_reaction_powder_pod'])
    s = setAndArm(s, 't0_green_reaction_powder_pod')
    s = passTurn(s)
    const hp = s.enemies.map((e) => e.hp)
    s = passTurn(s)
    expect(s.player.setCards).toHaveLength(0)
    expect(types(s.eventLog)).toContain('SetCardExpired')
    expect(s.enemies[0].hp).toBe(hp[0] - 14)
    expect(s.enemies[1].hp).toBe(hp[1] - 14)
  })

  it('期限切れ由来で敵が全滅したら決着する (won)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), ['green_reaction_powder_pod'])
    s = setAndArm(s, 't0_green_reaction_powder_pod')
    s = passTurn(s)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 10, block: 0 })) }
    s = passTurn(s)
    expect(s.phase).toBe('won')
  })
})

describe('回収は廃止 (RetrieveSetCard は常に拒否)', () => {
  it('伏せた札を手札に戻すコマンドは throw する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(() => applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_reaction_thorns' })).toThrow(/廃止/)
    expect(s.player.setCards).toHaveLength(1)
  })
})

describe('旧セーブ互換: setTurn の無い伏せ札は「前のターンに伏せた=生きた罠」扱い', () => {
  it('setTurn 欠落の札は齢1 で、そのターンの窓に載る (齢0 だと永久に鳴らず・ほどけない死に枠になる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute'), [])
    s = { ...s, player: { ...s.player, setCards: [{ uid: 'old0', def: getCardDef('green_reaction_thorns') }] } }
    const card = s.player.setCards[0]
    expect(trapAge(s, card)).toBe(1)
    expect(isTrapLive(s, card)).toBe(true)
    s = applyCommand(withIntent(s, attackIntent(10)), { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
  })
})

describe('かすみ (2枠): 準備中の札と生きた罠の重ね', () => {
  it('前のターンの罠だけが候補になり、今ターン仕込んだ札は候補に載らない', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_dimir')
    let s = withHand(run.combat!, ['black_reaction_curse'])
    s = setAndArm(s, 't0_black_reaction_curse') // T1 呪詛返し (被攻撃後)
    s = withHand(s, ['blue_frost_veil'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' }) // T2 霜の帳 (被攻撃前) = 準備中
    expect(s.player.setCards).toHaveLength(2)
    expect(s.player.setCards.map((c) => isTrapLive(s, c))).toEqual([true, false])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, intent: attackIntent(6) })) }
    s = applyCommand(s, { type: 'EndTurn' })
    // pre窓 (霜の帳) は準備中なので開かず、post窓 (呪詛返し) だけが開く
    expect(s.phase).toBe('awaiting-reaction')
    expect(s.pendingWindow?.stage).toBe('post')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_black_reaction_curse' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.player.setCards.map((c) => c.uid)).toEqual(['t0_blue_frost_veil']) // 帳は準備を終えて残る
    expect(isTrapLive(s, s.player.setCards[0])).toBe(true)
  })
})

describe('敵は伏せを見ない: 残るのは罠壊し・道化の破壊分岐だけ', () => {
  it('データ: movesVsSet を持つ敵は罠壊しと道化だけ。setAlt を持つ敵はいない', () => {
    const vsSet = allEnemies.filter((d) => (d.movesVsSet?.length ?? 0) > 0).map((d) => d.id).sort()
    expect(vsSet).toEqual(['enemy_joker', 'enemy_set_breaker'])
    const breaker = getEnemyDef('enemy_set_breaker')
    expect(breaker.movesVsSet!.map((m) => [m.id, m.kind, m.weight])).toEqual([
      ['break_trap', 'destroy-set', 2],
      ['smash', 'attack', 1],
    ])
    const joker = getEnemyDef('enemy_joker')
    expect(joker.movesVsSet!.map((m) => [m.id, m.kind, m.weight])).toEqual([
      ['cautious_jab', 'attack', 2],
      ['call_bluff', 'destroy-set', 1],
    ])
    const tables = (d: (typeof allEnemies)[number]) => [
      d.moves, d.movesBelowHalf ?? [], d.movesVsSet ?? [], d.movesVsTokens ?? [], d.movesWhenAlone ?? [],
    ]
    const withAlt = allEnemies.filter((d) => tables(d).some((t) => t.some((m) => m.setAlt !== undefined))).map((d) => d.id)
    expect(withAlt).toEqual([])
    expect(allEnemies.some((d) => 'vsSetIgnoreFreshness' in d)).toBe(false)
  })

  it('罠壊し: 準備中の札にも分岐が立ち (effectiveIntent は alt 側)、破壊は準備中の札も壊す', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter'), ['green_reaction_thorns'])
    expect(s.enemies[0].intent?.conditionalOn).toBe('set')
    expect(effectiveIntent(s, 0)!.kind).toBe(s.enemies[0].intent!.kind)
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(effectiveIntent(s, 0)!.kind).toBe(s.enemies[0].intent!.alt!.kind)
    s = withIntent(s, destroySetIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('SetCardDestroyed')
    expect(s.player.setCards).toHaveLength(0)
  })

  it('道化: 伏せ札があれば (生きた罠でも) 反応テーブル側の行動を宣言する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_joker', 42, 'starter'), ['green_reaction_thorns'])
    s = setAndArm(s, 't0_green_reaction_thorns')
    expect(s.enemies[0].intent?.conditionalOn).toBe('set')
    expect(['cautious_jab', 'call_bluff']).toContain(
      effectiveIntent(s, 0)!.kind === 'destroy-set' ? 'call_bluff' : 'cautious_jab',
    )
    expect(effectiveIntent(s, 0)).not.toEqual(expect.objectContaining({ kind: s.enemies[0].intent!.kind, alt: undefined }))
  })
})

describe('発火の形 (数値据え置き・副次効果を1つ)', () => {
  it('茨の返し: 返し10。受けた攻撃の実値が10以上なら急所2、9以下なら付かない', () => {
    const arm = () => setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_thorns']), 't0_green_reaction_thorns')
    const small = attackAndFire(arm(), 9)
    expect(types(small.eventLog)).toContain('ReactionTriggered')
    expect(small.enemies[0].exposed ?? 0).toBe(0)
    const big = attackAndFire(arm(), 10)
    expect(big.enemies[0].exposed).toBe(2)
    expect(big.enemies[0].maxHp - big.enemies[0].hp).toBe(10)
  })

  it('守りの蔓: ブロック12。この敵フェーズを完全に凌いだら次のターンのドローが1枚増える', () => {
    const arm = () => setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_vine']), 't0_green_reaction_vine')
    const perfect = attackAndFire(arm(), 10) // 12 ≥ 10 = 完全に凌ぐ
    expect(perfect.player.hp).toBe(perfect.player.maxHp)
    expect(perfect.player.hand).toHaveLength(6)
    const leaked = attackAndFire(arm(), 15) // 3漏れる
    expect(leaked.player.hp).toBe(leaked.player.maxHp - 3)
    expect(leaked.player.hand).toHaveLength(5)
  })

  it('先制の蔦槍: 16貫通。倒せなければその敵に急所2、倒せば付かず攻撃も来ない', () => {
    const arm = () => setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter'), ['green_reaction_preempt']), 't0_green_reaction_preempt')
    let s = arm()
    const hp0 = s.enemies[0].hp
    s = attackAndFire(s, 10)
    expect(hp0 - s.enemies[0].hp).toBe(16)
    expect(s.enemies[0].exposed).toBe(2)
    expect(s.player.hp).toBe(s.player.maxHp - 10)
    let k = arm()
    k = { ...k, enemies: k.enemies.map((e) => ({ ...e, hp: 5 })) }
    k = attackAndFire(k, 10)
    expect(k.phase).toBe('won')
    expect(k.enemies[0].exposed ?? 0).toBe(0)
    expect(k.player.hp).toBe(k.player.maxHp)
  })

  it('蔦の陣: ブロック10+返し8。完全に凌いだら体勢を崩し (staggerEnemy)、次の宣言が隙になる', () => {
    const arm = () => setAndArm(withHand(freshCombat('set-confirm', 'enemy_brute'), ['green_reaction_vine_formation']), 't0_green_reaction_vine_formation')
    const perfect = attackAndFire(arm(), 8)
    expect(perfect.player.hp).toBe(perfect.player.maxHp)
    expect(perfect.enemies[0].intent?.kind).toBe('rest')
    expect(perfect.enemies[0].staggeredNext).toBe(false)
    const leaked = attackAndFire(arm(), 15)
    expect(leaked.player.hp).toBe(leaked.player.maxHp - 5)
    expect(leaked.enemies[0].intent?.kind).not.toBe('rest')
  })
})
