// 火種 (白 2026-09-20 夜。本家 Soul の白版) と放出の軸 (灯の壁・眩む閃光・灯の鍛冶・灯の手帳・残り火・灯の大炉・灯の大槌・灯の火皿) を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, buildRelicPermanent, getCardDef, getRelicDef } from './content.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import { hearthSparkMax } from './effects.ts'
import { startCombatWithOptions } from './combat.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardDef, CardInstance, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[], enemy = 'enemy_probe'): GameState => withHand(freshCombat('set-confirm', enemy, 42, 'starter_white'), ids)
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
const inst = (id: string): CardInstance => ({ uid: `x_${id}`, def: getCardDef(id) })
const withDef = (s: GameState, def: CardDef): GameState => ({ ...s, player: { ...s.player, hand: [{ uid: `d_${def.id}`, def }, ...s.player.hand] } })
// 2026-09-24 白のプール 108→84 (docs/white-pool-trim-proposal-2026-09-23.md): 撤去した札の機構は仮の札で固定
const LIGHT_WALL: CardDef = { id: 'test_light_wall', name: '試しの灯の壁', cost: 1, type: 'spell', color: 'white', effects: [{ trigger: 'onPlay', effect: 'gainBlockPerLight', amount: 3 }] }
const LIGHT_LEDGER: CardDef = { id: 'test_light_ledger', name: '試しの灯の手帳', cost: 1, type: 'spell', color: 'white', effects: [{ trigger: 'onPlay', effect: 'drawCardsPerLight', amount: 1, amountMax: 2 }] }
const FIRE_DISH: CardDef = { id: 'test_fire_dish', name: '試しの火皿', cost: 1, type: 'permanent', color: 'white', effects: [{ trigger: 'onLightDischarged', effect: 'drawCards', amount: 1 }] }
const SPARK = 'white_spark_token'
const sparksIn = (cards: readonly CardInstance[]): number => cards.filter((c) => c.def.id === SPARK).length

describe('火種 (トークン札)', () => {
  it('火種は 0E・消滅・2ドロー・灯+1 のトークン (2026-09-23 本家 Soul の2ドローに揃えた)。撃つたび sparksPlayedThisCombat が増え、報酬には出ない', () => {
    const def = getCardDef(SPARK)
    expect(def).toMatchObject({ cost: 0, exhaust: true, sparkToken: true, type: 'spell' })
    expect(REWARD_EXCLUDED.has(SPARK)).toBe(true)
    let s = energy(fresh(['white_spark_kindling']), 3)
    s = play(s, 't0_white_spark_kindling', { xAmount: 2 }) // 大焚き付け X=2 → 火種2を手札へ
    expect(sparksIn(s.player.hand)).toBe(2)
    const tok = s.player.hand.find((c) => c.def.id === SPARK)!
    const hand0 = s.player.hand.length
    const l0 = s.player.light ?? 0
    s = play(s, tok.uid)
    expect(s.player.hand.length).toBe(hand0 - 1 + 2) // 1枚撃って2枚引く (本家 Soul)
    expect(s.player.light).toBe(l0 + 1)
    expect(s.player.sparksPlayedThisCombat).toBe(1)
    expect(s.player.exhaustPile.some((c) => c.def.id === SPARK)).toBe(true) // 消滅 = 二度と戻らない
    expect(s.player.energy).toBe(3 - 2) // 火種は 0E
  })

  it('火種撒き=9ダメ+火種1を山札へ、火花散らし=3+火種3、火守りの盾=ブロック8+火種1 (2026-09-23 本家 Reave/CaptureSpirit/GraveWarden 並み。山札のランダムな位置=ランRNGを消費・決定的)', () => {
    let s = energy(fresh(['white_spark_scatter', 'white_spark_burst', 'white_spark_shield']), 9)
    const hp0 = s.enemies[0].hp
    const rng0 = s.rng
    s = play(s, 't0_white_spark_scatter')
    expect(hp0 - s.enemies[0].hp).toBe(9)
    expect(sparksIn(s.player.drawPile)).toBe(1)
    expect(s.rng).not.toEqual(rng0)
    expect(s.eventLog.some((e) => e.type === 'CardsAddedToDraw' && e.count === 1)).toBe(true)
    s = play(s, 't1_white_spark_burst')
    expect(sparksIn(s.player.drawPile)).toBe(4)
    s = play(s, 't2_white_spark_shield')
    expect(s.player.block).toBe(8)
    expect(sparksIn(s.player.drawPile)).toBe(5)
    // 同じシード・同じ手順なら同じ位置 (決定性)
    let t = energy(fresh(['white_spark_scatter', 'white_spark_burst', 'white_spark_shield']), 9)
    t = play(t, 't0_white_spark_scatter'); t = play(t, 't1_white_spark_burst'); t = play(t, 't2_white_spark_shield')
    expect(t.player.drawPile.map((c) => c.def.id)).toEqual(s.player.drawPile.map((c) => c.def.id))
  })

  it('灯火の炉=毎ターン開始に火種1を手札へ (点灯なし=道具)。火起こし=灯2を払って火種2を手札へ (LightSpent が残る)', () => {
    let s = light(energy(fresh(['white_perm_spark_furnace', 'white_spark_kindle']), 9), 2)
    s = play(s, 't0_white_perm_spark_furnace')
    expect(sparksIn(s.player.hand)).toBe(0) // 道具の置物 = 登場では鳴らない
    s = play(s, 't1_white_spark_kindle')
    expect(sparksIn(s.player.hand)).toBe(2)
    expect(s.player.light).toBe(0)
    expect(s.eventLog.some((e) => e.type === 'LightSpent' && e.amount === 2)).toBe(true)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(sparksIn(s.player.hand)).toBe(1) // 次のターン開始に炉が1枚
  })

  it('灯の火床: ターン終了時に選んだ枚数 (EndTurn.hearthSparks) だけ灯3につき火種1を山札へ。上限は灯÷3・省略=0 (2026-09-20 夜 裁定「枚数を選ぶ」)', () => {
    let s = light(energy(fresh(['white_perm_spark_hearth']), 9), 7)
    s = play(s, 't0_white_perm_spark_hearth')
    expect(hearthSparkMax(s)).toBe(2)
    const t = applyCommand(s, { type: 'EndTurn' }) // 省略=変えない
    expect(sparksIn([...t.player.drawPile, ...t.player.hand])).toBe(0)
    expect(t.player.light).toBe(7 + 0) // 灯匠のパッシブは単発戦闘に無い = 据え置き
    const u = applyCommand(s, { type: 'EndTurn', hearthSparks: 1 })
    expect(sparksIn([...u.player.drawPile, ...u.player.hand])).toBe(1)
    expect(u.player.light).toBe(4)
    const v = applyCommand(s, { type: 'EndTurn', hearthSparks: 9 }) // 上限で切る
    expect(sparksIn([...v.player.drawPile, ...v.player.hand])).toBe(2)
    expect(v.player.light).toBe(1)
    expect(v.hearthSparks).toBeUndefined() // 一時の旗は消える
    expect(() => applyCommand(s, { type: 'EndTurn', hearthSparks: -1 })).toThrow()
    // 火床が無ければ hearthSparks は無視 (上限0)
    const w = light(energy(fresh(['white_strike']), 9), 7)
    expect(hearthSparkMax(w)).toBe(0)
    expect(sparksIn(applyCommand(w, { type: 'EndTurn', hearthSparks: 2 }).player.drawPile)).toBe(0)
  })

  it('火種の嵐=8＋撃った火種×2。火の粉=火種を撃つたび敵全体に6・貫通 (2026-09-23 本家 Haunt 相当)。灯の継ぎ手=火種を撃つたび人形1体が動く (灯は産まない)', () => {
    let s = energy(fresh(['white_perm_embers', 'white_perm_spark_relay', 'white_perm_squire', 'white_spark_kindling', 'white_spark_storm'], 'enc_probe_pair'), 9)
    s = play(s, 't0_white_perm_embers')
    s = play(s, 't1_white_perm_spark_relay')
    s = play(s, 't2_white_perm_squire') // 点灯で2
    s = play(s, 't3_white_spark_kindling', { xAmount: 3 }) // 火種3
    const hp = s.enemies.map((e) => e.hp)
    const l0 = s.player.light ?? 0
    for (let i = 0; i < 3; i++) {
      const tok = s.player.hand.find((c) => c.def.id === SPARK)!
      s = play(s, tok.uid)
    }
    // 火種3枚: 火の粉 6×3 が両方に、剣の人形 3×3 が片方に (ランダム対象) = 合計 36+9
    const lost = s.enemies.map((e, i) => hp[i] - e.hp)
    expect(lost[0] + lost[1]).toBe(6 * 3 * 2 + 3 * 3)
    expect(lost[0]).toBeGreaterThanOrEqual(18)
    expect(lost[1]).toBeGreaterThanOrEqual(18)
    expect(s.player.light).toBe(l0 + 3) // 火種の灯+1×3 だけ (継ぎ手で動いた人形は灯を産まない)
    expect(s.player.sparksPlayedThisCombat).toBe(3)
    const hp2 = s.enemies[0].hp
    s = play(s, 't4_white_spark_storm')
    expect(hp2 - s.enemies[0].hp).toBe(8 + 3 * 2)
  })

  it('火種の祈り (灯りの一節の作り直し): 0E・消滅・回復2・火種1を手札へ', () => {
    const def = getCardDef('white_prayer_verse')
    expect(def).toMatchObject({ cost: 0, exhaust: true, name: '火種の祈り' })
    let s = energy(fresh(['white_prayer_verse']), 3)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = play(s, 't0_white_prayer_verse')
    expect(s.player.hp).toBe(52)
    expect(sparksIn(s.player.hand)).toBe(1)
  })

  it('鍛え: 火種撒き+ は「撃った火種×2」(参照の倍率+1 = 火種の嵐と同じ規則。2026-09-24 刈り取りを内蔵してから)。火守りの盾+ も同じ', () => {
    const up = upgradeCard(inst('white_spark_scatter')).def
    expect(up.effects.find((e) => e.effect === 'dealDamagePerSpark')?.amount).toBe(2)
    expect(up.effects.find((e) => e.effect === 'addCardToDraw')?.amount).toBe(1)
    expect(upgradeCard(inst('white_spark_shield')).def.effects.find((e) => e.effect === 'gainBlockPerSpark')?.amount).toBe(2)
    expect(upgradeCard(inst('white_spark_kindle')).def).toMatchObject({ cost: 1, lightCost: 1 })
  })
})

describe('放出の軸 (灯の使い道を4形に)', () => {
  it('灯2につきブロック3 (gainBlockPerLight。灯は失わない)・灯2につき1ドロー (drawCardsPerLight。上限2) = 灯の壁・灯の手帳は 2026-09-24 に撤去、機構は仮の札', () => {
    let s = light(energy(withDef(withDef(fresh([]), LIGHT_LEDGER), LIGHT_WALL), 9), 7)
    s = play(s, 'd_test_light_wall')
    expect(s.player.block).toBe(9)
    expect(s.player.light).toBe(7)
    const hand0 = s.player.hand.length
    s = play(s, 'd_test_light_ledger')
    expect(s.player.hand.length).toBe(hand0 - 1 + 2) // floor(7/2)=3 → 上限2
    expect(s.player.light).toBe(7)
  })

  it('残り火: 全て放出しても灯の半分 (切り捨て) が残る。onLightDischarged (灯の火皿は 2026-09-24 に撤去=仮の札): 放出するたび・灯を払うたび1ドロー (2026-09-23 裁定B)。灯の大炉: 灯を得るたびさらに+1 (1段)', () => {
    let s = light(energy(withDef(fresh(['white_perm_embers_last', 'white_light_burst', 'white_light_bolt']), FIRE_DISH), 9), 7)
    s = play(s, 't0_white_perm_embers_last')
    s = play(s, 'd_test_fire_dish')
    const hand0 = s.player.hand.length
    s = play(s, 't1_white_light_burst') // 灯7を全て放出 → 3 が残り、火皿で1ドロー
    expect(s.player.light).toBe(3)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1)
    const hand1 = s.player.hand.length
    s = play(s, 't2_white_light_bolt') // 灯1を払う攻撃でも火皿は鳴る (残り火は全放出だけ)
    expect(s.player.light).toBe(2)
    expect(s.player.hand.length).toBe(hand1 - 1 + 1)
    let u = light(energy(fresh(['white_perm_great_furnace', 'white_light_hoard']), 9), 0)
    u = play(u, 't0_white_perm_great_furnace')
    u = play(u, 't1_white_light_hoard') // 灯+2 → 大炉で +1 (灯を得るたび1回。再誘発はしない)
    expect(u.player.light).toBe(3)
  })

  it('白は 86種 (報酬77。2026-09-24 プールを削る −24・CSV の裁定で −7・灯の薪 +1・Opus ひなた裁定で重ねる灯 −1・灯の小盾は初期札へ・2026-09-25 燭の人形 −1・2026-09-26 竜と獅子の人形 +2)。2026-09-20 夜の撤去10 (眩ます灯印・白光の矢・眩みの障壁・眩みの槌・光壁の反撃・大光壁・光門閉鎖・灯りの杖・灯の身代わり・灯りの庭) は無い', () => {
    const removed = ['white_menace', 'white_light_arrow', 'white_reaction_holy_wall', 'white_verdict_hammer', 'white_rampart_riposte', 'white_fortress', 'white_gate_close', 'white_mercy_staff', 'white_reaction_martyr', 'white_reaction_sanctuary']
    for (const id of removed) expect(allCards.some((c) => c.id === id), id).toBe(false)
    const white = allCards.filter((c) => c.color === 'white')
    expect(white.length).toBe(86) // 2026-09-26 竜と獅子の人形 +2 ← 2026-09-24 白のプール 108→84 (A8+B7+C3+D6 の24枚を撤去) → CSV の裁定で −7 (呼び声・光盾の点灯・眩む閃光・降霊・輝きの光・総突撃・旗印)・灯の薪 +1 → Opus ひなた裁定で重ねる灯 (反復の触媒) −1 → 2026-09-25 燭の人形 −1 (人形の回復を半分にしたら癒しの人形と同じ札になった)
    expect(white.filter((c) => !REWARD_EXCLUDED.has(c.id)).length).toBe(77) // 灯の小盾は初期札になり報酬から外れた (2026-09-24)
  })
})

describe('答え合わせの是正 (2026-09-20 Opus 火種 A/B/C)', () => {
  it('鎮めの錘 (interruptBlocked) は宣言時の経路でも割り込みを止める (C: 組んでもオーガが次の宣言で豹変していた)', () => {
    let s = fresh(['white_strike'], 'enemy_brute')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, interruptBlocked: true })) }
    const half = Math.floor(s.enemies[0].maxHp / 2)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: half - 1 })) }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'EnemyInterrupted')).toBe(false)
    expect(s.enemies[0].firedInterrupts ?? []).toEqual([])
    // 錘が無ければ次の宣言で豹変する (対照)
    let t = fresh(['white_strike'], 'enemy_brute')
    t = { ...t, enemies: t.enemies.map((e) => ({ ...e, hp: half - 1 })) }
    t = applyCommand(t, { type: 'EndTurn' })
    expect((t.enemies[0].firedInterrupts ?? []).length).toBeGreaterThan(0)
  })

  it('打ち消し済み (楔 actionNegated) の行動には確認ウィンドウが開かない (A: 起きない行動に護りの灯印を切らせていた)', () => {
    let s = fresh(['white_reaction_ward'], 'enemy_brute')
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_white_reaction_ward' })
    s = applyCommand(s, { type: 'EndTurn' }) // 準備ターン
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, actionNegated: true, intent: e.intent ? { ...e.intent, kind: 'attack' as const, actual: 12 } : e.intent })) }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.slice(-40).some((e) => e.type === 'ActionNegated')).toBe(true)
    expect(s.phase).not.toBe('awaiting-reaction')
  })
})

describe('火種の本家寄せ (2026-09-23 ユーザー「火種についてももっと本家を参照してほしい」→ ask_user: 断ち切り=Severance・降霊=Seance)', () => {
  it('断ち切り: 13ダメ＋火種を山札・捨て札・手札に1枚ずつ (新効果 addCardToDiscard)', () => {
    let s = energy(fresh(['white_severance']), 9)
    const hp0 = s.enemies[0].hp
    const d0 = sparksIn(s.player.drawPile), c0 = sparksIn(s.player.discardPile), h0 = sparksIn(s.player.hand)
    s = play(s, 't0_white_severance')
    expect(hp0 - s.enemies[0].hp).toBe(13)
    expect(sparksIn(s.player.drawPile)).toBe(d0 + 1)
    expect(sparksIn(s.player.discardPile)).toBe(c0 + 1)
    expect(sparksIn(s.player.hand)).toBe(h0 + 1)
    expect(s.eventLog.some((e) => e.type === 'CardsAddedToDiscard' && e.count === 1)).toBe(true)
  })

})

describe('火種の本家相当 (2026-09-23 ユーザー「本家相当にしてほしい」: 火の粉=Haunt・灯の面=Funerary Mask・忘れられた灯=Forgotten Soul・亡者の壺=Pot of Ghouls・忘れられた墓=Grave of the Forgotten)', () => {
  it('火の粉: 火種を撃つたび敵全体に6・貫通 (敵ブロックを無視)', () => {
    let s = energy(fresh(['white_perm_embers', 'white_spark_kindling'], 'enc_probe_pair'), 9)
    s = play(s, 't0_white_perm_embers')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    s = play(s, 't1_white_spark_kindling', { xAmount: 1 })
    const hp = s.enemies.map((e) => e.hp)
    const tok = s.player.hand.find((c) => c.def.id === SPARK)!
    s = play(s, tok.uid)
    expect(s.enemies.map((e, i) => hp[i] - e.hp)).toEqual([6, 6])
    expect(s.enemies.every((e) => e.block === 10)).toBe(true) // 貫通 = ブロックを削らない
  })

  // 2026-09-24 人間ラン#19「火種ヤバいかも〜」: 本家 Funerary Mask は「At the start of each combat」= 戦闘開始時だけ。§83 は毎ターンと読み違えていた
  it('灯の面 (レリック・白限定): 戦闘開始時にだけ火種3枚を山札へ (ターンの終わりには足さない)。忘れられた灯: 札が消滅するたび敵に1', () => {
    const withRelic = withHand(
      startCombatWithOptions(42, 'set-confirm', 'enemy_probe', {
        deck: buildDeck('starter_white'),
        relicPermanents: ['relic_funerary_mask', 'relic_forgotten_light'].map((id) => buildRelicPermanent(getRelicDef(id))),
      }),
      ['white_spark_kindling'],
    )
    expect(sparksIn(withRelic.player.drawPile)).toBe(3)
    let t = energy(withRelic, 9)
    const hp0 = t.enemies[0].hp
    t = play(t, 't0_white_spark_kindling', { xAmount: 1 }) // 消滅 → 忘れられた灯で1
    expect(hp0 - t.enemies[0].hp).toBe(1)
    const tok = t.player.hand.find((c) => c.def.id === SPARK)!
    t = play(t, tok.uid) // 火種も消滅 → さらに1
    expect(hp0 - t.enemies[0].hp).toBe(2)
    const before = sparksIn([...t.player.drawPile, ...t.player.hand, ...t.player.discardPile])
    t = applyCommand(t, { type: 'EndTurn' })
    expect(sparksIn([...t.player.drawPile, ...t.player.hand, ...t.player.discardPile])).toBe(before) // ターンの終わりには増えない
  })
})

describe('灯の永久コンボ (2026-09-25 人間ラン#20 → ユーザー裁定「このまま認める」。card-power.md §89)', () => {
  // 山札と捨て札を 灯の薪・火起こし の2枚に絞り (残りは手札に抱える＝手札に上限が無い)、灯の大炉+ を置くと
  // 薪 (灯3→一時マナ+1) → 火起こし (1E・灯2→火種2) → 火種2 (各 2ドロー・灯+1 → 大炉+ で +3) の1周が 灯+1・エナジー±0 で閉じる。
  // 規約③「無限ループは作らない」の例外として認めた。薪・火起こし・火種・大炉の数字や手札の上限を変えてこのテストが落ちたら、
  // 例外を続けるか閉じるかをユーザーに確認すること (閉じる候補＝薪を1ターン1回。§89)
  it('灯の大炉+ があると 薪→火起こし→火種→火種 の1周で 灯+1・エナジー±0・2枚が手札に戻る', () => {
    const furnace = upgradeCard(inst('white_perm_great_furnace'))
    expect(furnace.def.effects.find((e) => e.trigger === 'onLightGained')?.amount).toBe(2)
    let s = energy(fresh(['white_light_kindling', 'white_spark_kindle']), 1)
    s = { ...s, player: { ...s.player, drawPile: [], discardPile: [], permanents: [...s.player.permanents, furnace] } }
    s = light(s, 10)
    s = play(s, 't0_white_light_kindling')
    s = play(s, 't1_white_spark_kindle')
    const sparks = s.player.hand.filter((c) => c.def.id === SPARK)
    expect(sparks.length).toBe(2)
    for (const k of sparks) s = play(s, k.uid)
    expect(s.player.light).toBe(11)
    expect(s.player.energy).toBe(1)
    expect(s.player.hand.map((c) => c.def.id).sort()).toEqual(['white_light_kindling', 'white_spark_kindle'])
    // 鍛えていない大炉 (+1) なら 1周で 灯−1＝長いが必ず止まる
    let u = energy(fresh(['white_light_kindling', 'white_spark_kindle']), 1)
    u = light({ ...u, player: { ...u.player, drawPile: [], discardPile: [], permanents: [...u.player.permanents, inst('white_perm_great_furnace')] } }, 10)
    u = play(u, 't0_white_light_kindling')
    u = play(u, 't1_white_spark_kindle')
    for (const k of u.player.hand.filter((c) => c.def.id === SPARK)) u = play(u, k.uid)
    expect(u.player.light).toBe(9)
  })
})
