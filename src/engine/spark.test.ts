// 火種 (白 2026-09-20 夜。本家 Soul の白版) と放出の軸 (灯の壁・眩む閃光・灯の鍛冶・灯の手帳・残り火・灯の大炉・灯の大槌・灯の火皿) を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import { hearthSparkMax } from './effects.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardInstance, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[], enemy = 'enemy_probe'): GameState => withHand(freshCombat('set-confirm', enemy, 42, 'starter_white'), ids)
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
const inst = (id: string): CardInstance => ({ uid: `x_${id}`, def: getCardDef(id) })
const SPARK = 'white_spark_token'
const sparksIn = (cards: readonly CardInstance[]): number => cards.filter((c) => c.def.id === SPARK).length

describe('火種 (トークン札)', () => {
  it('火種は 0E・消滅・1ドロー・灯+1 のトークン。撃つたび sparksPlayedThisCombat が増え、報酬には出ない', () => {
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
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 1枚撃って1枚引く
    expect(s.player.light).toBe(l0 + 1)
    expect(s.player.sparksPlayedThisCombat).toBe(1)
    expect(s.player.exhaustPile.some((c) => c.def.id === SPARK)).toBe(true) // 消滅 = 二度と戻らない
    expect(s.player.energy).toBe(3 - 2) // 火種は 0E
  })

  it('火種撒き=7ダメ+火種1を山札へ、火花散らし=3貫通+火種2、火守りの盾=ブロック7+火種1 (山札のランダムな位置=ランRNGを消費・決定的)', () => {
    let s = energy(fresh(['white_spark_scatter', 'white_spark_burst', 'white_spark_shield']), 9)
    const hp0 = s.enemies[0].hp
    const rng0 = s.rng
    s = play(s, 't0_white_spark_scatter')
    expect(hp0 - s.enemies[0].hp).toBe(7)
    expect(sparksIn(s.player.drawPile)).toBe(1)
    expect(s.rng).not.toEqual(rng0)
    expect(s.eventLog.some((e) => e.type === 'CardsAddedToDraw' && e.count === 1)).toBe(true)
    s = play(s, 't1_white_spark_burst')
    expect(sparksIn(s.player.drawPile)).toBe(3)
    s = play(s, 't2_white_spark_shield')
    expect(s.player.block).toBe(7)
    expect(sparksIn(s.player.drawPile)).toBe(4)
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

  it('火種の嵐=8＋撃った火種×2。火の粉=火種を撃つたび敵全体に1。灯の継ぎ手=火種を撃つたび人形1体が動く (灯は産まない)', () => {
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
    // 火種3枚: 火の粉 1×3 が両方に、剣の人形 2×3 が片方に (ランダム対象) = 合計 6+6
    const lost = s.enemies.map((e, i) => hp[i] - e.hp)
    expect(lost[0] + lost[1]).toBe(3 * 2 + 3 * 2)
    expect(lost[0]).toBeGreaterThanOrEqual(3)
    expect(lost[1]).toBeGreaterThanOrEqual(3)
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

  it('鍛え: 火種撒き+ は火種2 (生成枚数+1)。合成: 火種の軸一致は火種1のおまけ', () => {
    const up = upgradeCard(inst('white_spark_scatter')).def
    expect(up.effects.find((e) => e.effect === 'addCardToDraw')?.amount).toBe(2)
    expect(upgradeCard(inst('white_spark_kindle')).def).toMatchObject({ cost: 1, lightCost: 1 })
  })
})

describe('放出の軸 (灯の使い道を4形に)', () => {
  it('灯の壁=灯2につきブロック3 (灯は失わない)。灯の手帳=灯2につき1ドロー (上限2)', () => {
    let s = light(energy(fresh(['white_light_wall', 'white_light_ledger']), 9), 7)
    s = play(s, 't0_white_light_wall')
    expect(s.player.block).toBe(9)
    expect(s.player.light).toBe(7)
    const hand0 = s.player.hand.length
    s = play(s, 't1_white_light_ledger')
    expect(s.player.hand.length).toBe(hand0 - 1 + 2) // floor(7/2)=3 → 上限2
    expect(s.player.light).toBe(7)
  })

  it('眩む閃光: 灯を全て放出し、灯3につき敵全体に威圧1。灯3未満なら不発 (灯は減らない)', () => {
    let s = light(energy(fresh(['white_light_flash', 'white_light_flash'], 'enc_probe_pair'), 9), 7)
    s = play(s, 't0_white_light_flash')
    expect(s.enemies.map((e) => e.weak ?? 0)).toEqual([2, 2])
    expect(s.player.light).toBe(0)
    expect(s.eventLog.some((e) => e.type === 'LightDischarged' && e.spent === 7)).toBe(true)
    s = light(s, 2)
    s = play(s, 't1_white_light_flash')
    expect(s.player.light).toBe(2) // 不発
  })

  it('灯の鍛冶: 0E・灯4 の追加コスト。手札の全てをこの戦闘中鍛える (レア・工房産は除く)。灯3なら出せない (2026-09-20 夜 裁定「0E 灯4」)', () => {
    expect(getCardDef('white_light_forge')).toMatchObject({ cost: 0, lightCost: 4 })
    let s = light(energy(fresh(['white_light_forge', 'white_strike', 'white_guard']), 9), 6)
    const e0 = s.player.energy
    s = play(s, 't0_white_light_forge')
    expect(s.player.light).toBe(2) // 灯4だけ払う (全部は失わない)
    expect(s.player.energy).toBe(e0)
    expect(s.player.hand.map((c) => c.def.name)).toEqual(expect.arrayContaining(['灯火の一撃+', '白盾+']))
    const t = light(energy(fresh(['white_light_forge', 'white_strike']), 9), 3)
    expect(() => play(t, 't0_white_light_forge')).toThrow('灯が足りない')
  })

  it('残り火: 放出しても灯の半分 (切り捨て) が残る。灯の火皿: 放出するたび1ドロー。灯の大炉: 灯を得るたびさらに+1 (1段)', () => {
    let s = light(energy(fresh(['white_perm_embers_last', 'white_perm_fire_dish', 'white_light_bolt']), 9), 7)
    s = play(s, 't0_white_perm_embers_last')
    s = play(s, 't1_white_perm_fire_dish')
    const hand0 = s.player.hand.length
    s = play(s, 't2_white_light_bolt') // 灯7を放出 → 3 が残り、火皿で1ドロー
    expect(s.player.light).toBe(3)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1)
    let u = light(energy(fresh(['white_perm_great_furnace', 'white_light_hoard']), 9), 0)
    u = play(u, 't0_white_perm_great_furnace')
    u = play(u, 't1_white_light_hoard') // 灯+2 → 大炉で +1 (灯を得るたび1回。再誘発はしない)
    expect(u.player.light).toBe(3)
  })

  it('灯の大槌: 12ダメ。灯5以上なら敵の体勢を崩す (次の行動が隙)', () => {
    let s = light(energy(fresh(['white_light_maul']), 9), 5)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_maul')
    expect(hp0 - s.enemies[0].hp).toBe(12)
    expect(s.enemies[0].staggeredNext).toBe(true)
    let t = light(energy(fresh(['white_light_maul']), 9), 4)
    t = play(t, 't0_white_light_maul')
    expect(t.enemies[0].staggeredNext ?? false).toBe(false)
  })

  it('白は 91種 (報酬83)。撤去10 (眩ます灯印・白光の矢・眩みの障壁・眩みの槌・光壁の反撃・大光壁・光門閉鎖・灯りの杖・灯の身代わり・灯りの庭) は無い', () => {
    const removed = ['white_menace', 'white_light_arrow', 'white_reaction_holy_wall', 'white_verdict_hammer', 'white_rampart_riposte', 'white_fortress', 'white_gate_close', 'white_mercy_staff', 'white_reaction_martyr', 'white_reaction_sanctuary']
    for (const id of removed) expect(allCards.some((c) => c.id === id), id).toBe(false)
    const white = allCards.filter((c) => c.color === 'white')
    expect(white.length).toBe(91)
    expect(white.filter((c) => !REWARD_EXCLUDED.has(c.id)).length).toBe(83)
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
