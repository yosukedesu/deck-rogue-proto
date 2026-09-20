// 灯 (白の再設計 2026-09-20。docs/white-redesign-proposal-2026-09-20.md): 供給3経路・放出・号令・点灯の定義・鍛え・合成を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getLeaderDef } from './content.ts'
import { fuseCards } from './fusion.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { rallyPreview } from './effects.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, freshCombat, withHand } from './test-helpers.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardInstance, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[], enemy = 'enemy_probe'): GameState =>
  withHand(freshCombat('set-confirm', enemy, 42, 'starter_white'), ids)
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
const inst = (id: string): CardInstance => ({ uid: `x_${id}`, def: getCardDef(id) })
const gained = (s: GameState, source: string) => s.eventLog.filter((e) => e.type === 'LightGained' && e.source === source).length

describe('灯の供給 (回復するたび・人形が場に出るたび・ひなたのパッシブ)', () => {
  it('ひなたは毎ターン開始に灯+1 (T1 から)。パッシブは addLight で駆けつけの効果は持たない', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_white')
    const s = run.combat!
    expect(s.player.light).toBe(1)
    expect(gained(s, 'passive')).toBe(1)
    const def = getLeaderDef('leader_white')
    expect(def.passive.map((e) => e.effect)).toEqual(['addLight'])
  })

  it('回復するたび灯+1。過剰回復 (満タン) でも溜まる (癒しの光は明示の灯+1 も持つ = 回し封じの相殺 2026-09-20)', () => {
    let s = energy(fresh(['white_heal', 'white_heal']), 9)
    s = play(s, 't0_white_heal') // 満タン: 回復+1・明示+1
    expect(s.player.light).toBe(2)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = play(s, 't1_white_heal')
    expect(s.player.light).toBe(4)
    expect(gained(s, 'heal')).toBe(2)
    expect(gained(s, 'card')).toBe(2)
  })

  it('人形の登場では灯は溜まらない (2026-09-20 夜 廃止: 人形は灯を使う側。手張り・点灯=召喚・道具のどれでも)', () => {
    let s = energy(fresh(['white_perm_squire', 'white_muster', 'white_perm_chalice']), 9)
    s = play(s, 't0_white_perm_squire')
    expect(s.player.light).toBe(0)
    s = play(s, 't1_white_muster') // 2体
    expect(s.player.light).toBe(0)
    s = play(s, 't2_white_perm_chalice') // 道具
    expect(s.player.light).toBe(0)
    expect(gained(s, 'retainer')).toBe(0)
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(3) // 点灯 (1回動く) はそのまま
  })

  it('戦闘開始時から場にあるリーダーパッシブ・レリック (innate) は「登場」ではない = 灯は溜まらない', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_green')
    expect(run.combat!.player.light ?? 0).toBe(0)
  })

  it('灯芯の人形: 出た瞬間に動く (点灯の定義) ので自分の効果で灯+1 (登場の+1は無い)', () => {
    let s = energy(fresh(['white_perm_wick']), 9)
    s = play(s, 't0_white_perm_wick')
    expect(s.player.light).toBe(1)
  })
})

describe('点灯の定義 (白共通ルール: 人形は場に出た瞬間に1回動く。旧・ひなたの駆けつけの格上げ)', () => {
  it('ひなた以外 (スターター単発) でも剣の人形が出た瞬間に2ダメ。鐘の人形 (登場ごとだけ) は二重に鳴らない', () => {
    let s = energy(fresh(['white_perm_squire', 'white_perm_band']), 9)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_perm_squire')
    expect(hp0 - s.enemies[0].hp).toBe(2)
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(1)
    const hand0 = s.player.hand.length
    s = light(s, 2) // 鐘の人形は 1E・灯2 (2026-09-20 灯と人形の結び)
    s = play(s, 't1_white_perm_band')
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(1)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 自分の登場で1ドローだけ
    expect(s.player.light).toBe(0) // 灯を吸って点灯 = 登場の灯+1 は産まない
  })

  it('ギルド (あさひ) の開幕人形も点灯で出た瞬間に動く (灯は増えない)', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_boros')
    const s = run.combat!
    expect(s.player.permanents.some((p) => p.def.retainer === true && p.innate !== true)).toBe(true)
    expect(s.player.light ?? 0).toBe(0)
    expect(s.eventLog.some((e) => e.type === 'RetainerRushed')).toBe(true)
  })
})

describe('放出 (dischargeLight: 灯×N のダメージで灯を0に)', () => {
  it('灯の矢: 灯4 → 8ダメ・灯0。灯0なら不発 (消費も出来事もない)', () => {
    let s = light(energy(fresh(['white_light_bolt', 'white_light_bolt']), 9), 4)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(8)
    expect(s.player.light).toBe(0)
    const hp1 = s.enemies[0].hp
    s = play(s, 't1_white_light_bolt')
    expect(s.enemies[0].hp).toBe(hp1)
    expect(s.eventLog.filter((e) => e.type === 'LightDischarged').length).toBe(1)
  })

  it('単体の放出は灯1につき1ヒット (2026-09-20 裁定: 装甲を分けて越える) = 成長はヒットごとに乗る', () => {
    let s = light(energy(fresh(['white_light_bolt']), 9), 3)
    s = { ...s, player: { ...s.player, growth: 2 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(3 * (2 + 2))
    expect(s.eventLog.filter((e) => e.type === 'DamageDealt').length).toBe(3)
  })

  it('装甲5の敵に灯4の灯の矢 (×2): 4ヒット×2 = 8 が丸ごと通る (一括なら8→5に切られていた)', () => {
    let s = light(energy(fresh(['white_light_bolt'], 'enemy_iron_clam'), 9), 4)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(8)
  })

  it('眩光の大放出: 全体に灯×2 を一括 (2体目も同じ量) + 全体に威圧1', () => {
    let s = light(energy(fresh(['white_light_burst'], 'enc_probe_pair'), 9), 3)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_white_light_burst')
    expect(hp[0] - s.enemies[0].hp).toBe(6)
    expect(hp[1] - s.enemies[1].hp).toBe(6)
    expect(s.enemies.every((e) => e.weak === 1)).toBe(true)
    expect(s.player.light).toBe(0)
  })

  it('灯の輪: 回復6 (灯+1) が先に乗ってから灯×2 を放出', () => {
    let s = light(energy(fresh(['white_praise_chorus']), 9), 2)
    s = { ...s, player: { ...s.player, hp: 50 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_praise_chorus')
    expect(s.player.hp).toBe(56)
    expect(hp0 - s.enemies[0].hp).toBe(6) // 灯3×2
    expect(s.player.light).toBe(0)
  })

  it('灯の倍化: 灯3 → 6 (獲得の誘発が乗る)。消滅を持つ', () => {
    let s = light(energy(fresh(['white_light_double']), 9), 3)
    s = play(s, 't0_white_light_double')
    expect(s.player.light).toBe(6)
    expect(getCardDef('white_light_double').exhaust).toBe(true)
  })
})

describe('しきい値 (minLight) と灯を得るたびの誘発', () => {
  it('灼く光: 8ダメ。灯5以上ならさらに8 (解決の時点で判定)', () => {
    let s = light(energy(fresh(['white_judgment']), 9), 4)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_judgment')
    expect(hp0 - s.enemies[0].hp).toBe(8)
    let t = light(energy(fresh(['white_judgment']), 9), 5)
    const hp1 = t.enemies[0].hp
    t = play(t, 't0_white_judgment')
    expect(hp1 - t.enemies[0].hp).toBe(16)
    expect(t.player.light).toBe(5) // 参照するだけで消費しない
  })

  it('光の裁き: 10ダメ。灯5以上なら威圧2', () => {
    let s = light(energy(fresh(['white_light_verdict']), 9), 5)
    s = play(s, 't0_white_light_verdict')
    expect(s.enemies[0].weak).toBe(2)
    let t = light(energy(fresh(['white_light_verdict']), 9), 4)
    t = play(t, 't0_white_light_verdict')
    expect(t.enemies[0].weak ?? 0).toBe(0)
  })

  it('灯の弩: 灯を得るたび敵全体に1 (回復・明示の札のどれでも。人形の登場では鳴らない)', () => {
    let s = energy(fresh(['white_perm_light_ballista', 'white_heal', 'white_perm_squire']), 9)
    s = play(s, 't0_white_perm_light_ballista')
    const hp0 = s.enemies[0].hp
    s = play(s, 't1_white_heal') // 回復の灯+1 → 弩1、明示の灯+1 → 弩1
    expect(hp0 - s.enemies[0].hp).toBe(2)
    s = play(s, 't2_white_perm_squire') // 登場では灯は増えない (弩は鳴らない)、点灯で剣の人形2
    expect(hp0 - s.enemies[0].hp).toBe(2 + 2)
  })
})

describe('号令 (点灯の合図 1E・灯2) と灯火の大行列', () => {
  it('灯コストはエナジーと別に払う。足りなければプレイ不可。全人形がトリガーを問わず (登場ごとを除く) 1回動く', () => {
    let s = light(energy(fresh(['white_perm_squire', 'white_perm_hound', 'white_perm_band', 'white_march_order']), 9), 2)
    s = play(s, 't0_white_perm_squire') // 2ダメ (登場で灯は増えない)
    s = play(s, 't1_white_perm_hound') // 点灯で2ダメ
    s = play(s, 't2_white_perm_band') // 灯2を払って 0・1ドロー
    expect(s.player.light).toBe(0)
    expect(s.eventLog.filter((e) => e.type === 'LightSpent').length).toBe(1) // 支払いはログに残る (2026-09-20 夜)
    s = light(s, 3)
    const hp0 = s.enemies[0].hp
    const hand0 = s.player.hand.length
    const e0 = s.player.energy
    s = play(s, 't3_white_march_order')
    // 2026-09-20 裁定: 合図は小さな人形1体を点灯 (1ダメ) してから号令 = 剣2 + 犬2 + 小さな人形1。鐘 (登場ごと) は鳴らない
    expect(hp0 - s.enemies[0].hp).toBe(1 + 2 + 2 + 1)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 合図の小さな人形の登場で鐘が1ドロー (登場ごと)
    expect(s.player.light).toBe(3 - 2) // 小さな人形の登場で灯は戻らない (2026-09-20 夜 廃止) = 合図の正味は灯2
    expect(s.player.energy).toBe(e0 - 1)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(1)
    expect(getCardDef('white_march_order').requiresRetainer).toBeUndefined() // 人形0でも仕事をする (A の処方)
    let t = light(energy(fresh(['white_perm_squire', 'white_march_order']), 9), 1)
    t = play(t, 't0_white_perm_squire')
    expect(() => play(t, 't1_white_march_order')).toThrow('灯が足りない')
  })

  it('灯火の大行列: 灯を全て放出し、灯1につき全人形が1回動く。人形0なら撃てない', () => {
    let s = energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_grand_charge']), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    s = light(s, 3)
    const hp0 = s.enemies[0].hp
    const b0 = s.player.block
    s = play(s, 't2_white_grand_charge')
    expect(hp0 - s.enemies[0].hp).toBe(2 * 3)
    expect(s.player.block - b0).toBe(2 * 3)
    expect(s.player.light).toBe(0)
    expect(getCardDef('white_grand_charge').requiresRetainer).toBe(true)
  })

  it('号令・大行列の中では灯を産まない (2026-09-20 裁定): 灯芯の人形・癒しの人形が動いても灯は戻らない', () => {
    let s = light(energy(fresh(['white_perm_wick', 'white_perm_choir', 'white_grand_charge']), 9), 1)
    s = play(s, 't0_white_perm_wick') // 灯2 (自分の効果で+1)
    s = play(s, 't1_white_perm_choir') // 灯2を払って0 → 点灯の回復2で灯1
    expect(s.player.light).toBe(1)
    s = { ...s, player: { ...s.player, hp: 50 }, }
    s = light(s, 4)
    s = play(s, 't2_white_grand_charge') // 灯4を放出 → 各人形が4回動く。灯芯の+1×4 も 癒しの回復2×4 の+1×4 も鳴らない
    expect(s.player.light).toBe(0)
    expect(s.player.hp).toBe(58)
  })

  it('人形の単体ダメージはランダムな生存敵へ (2026-09-20 裁定)。合計は変わらず、2体戦で RNG を1回ずつ消費する', () => {
    let s = energy(fresh(['white_perm_squire', 'white_perm_squire', 'white_perm_squire'], 'enc_probe_pair'), 9)
    const hp = s.enemies.map((e) => e.hp)
    const rng0 = s.rng
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_squire')
    s = play(s, 't2_white_perm_squire')
    const lost = s.enemies.map((e, i) => hp[i] - e.hp)
    expect(lost[0] + lost[1]).toBe(6)
    expect(s.rng).not.toEqual(rng0)
    // 同じシード・同じ手順なら同じ配分 (決定性)
    let t = energy(fresh(['white_perm_squire', 'white_perm_squire', 'white_perm_squire'], 'enc_probe_pair'), 9)
    t = play(t, 't0_white_perm_squire')
    t = play(t, 't1_white_perm_squire')
    t = play(t, 't2_white_perm_squire')
    expect(t.enemies.map((e, i) => hp[i] - e.hp)).toEqual(lost)
  })
})

describe('鍛える・合成・供給', () => {
  it('鍛え: 合図+=灯コスト1 (1Eのまま)・灯の矢+=×3・光の奔流+=×4・灼く光+=灯4以上で12+12・灯り溜め+=灯+3・ブロック8・大行列+=1E', () => {
    expect(upgradeCard(inst('white_march_order')).def).toMatchObject({ cost: 1, lightCost: 1 })
    expect(upgradeCard(inst('white_light_bolt')).def.effects[0].amount).toBe(3)
    expect(upgradeCard(inst('white_light_torrent')).def.effects[0].amount).toBe(4)
    const j = upgradeCard(inst('white_judgment')).def
    expect(j.effects.map((e) => e.amount)).toEqual([12, 12])
    expect(j.effects[1].condition?.minLight).toBe(4)
    const h = upgradeCard(inst('white_light_hoard')).def
    expect(h.effects.map((e) => e.amount)).toEqual([8, 3])
    expect(upgradeCard(inst('white_grand_charge')).def.cost).toBe(1)
  })

  it('合成: 灯コストは合算。放出は置物では落ちる', () => {
    const f = fuseCards(inst('white_march_order'), inst('white_light_strike'))
    expect(f.lightCost).toBe(2)
    const p = fuseCards(inst('white_light_bolt'), inst('white_perm_squire'))
    expect(p.type).toBe('permanent')
    expect(p.effects.some((e) => e.effect === 'dischargeLight')).toBe(false)
  })

  it('灯の矢はスターター専用 (報酬プール外)、光壁砕きは報酬プールへ。灯コスト持ちは白だけ', () => {
    expect(REWARD_EXCLUDED.has('white_light_bolt')).toBe(true)
    expect(REWARD_EXCLUDED.has('white_bodyslam')).toBe(false)
    expect(allCards.filter((c) => (c.lightCost ?? 0) > 0).every((c) => c.color === 'white')).toBe(true)
    const removed = ['white_holy_oil', 'white_healing_verse', 'white_perm_spring', 'white_perm_bell', 'white_wall_jab', 'white_shield_bash', 'white_perm_ballista', 'white_decree', 'white_cowering_light', 'white_seal_light', 'white_glory_chain', 'white_perm_pavilion', 'white_rank_shield']
    for (const id of removed) expect(allCards.some((c) => c.id === id), id).toBe(false)
  })
})

describe('灯と人形の結び (2026-09-20 夜 ユーザー「灯と人形の関連付けが薄い」→ 案B「強い人形に灯コスト」)', () => {
  it('癒し・鐘・大鐘は 1E・灯2。灯篭 U1E・灯2 (答え合わせで R→U・灯3→2)・篝火 R2E・灯4 は報酬プールの人形 (白 82種)', () => {
    for (const id of ['white_perm_choir', 'white_perm_band', 'white_perm_bandleader']) {
      expect(getCardDef(id), id).toMatchObject({ cost: 1, lightCost: 2, retainer: true, rarity: 'uncommon' })
    }
    expect(getCardDef('white_perm_lantern')).toMatchObject({ cost: 1, lightCost: 2, retainer: true, rarity: 'uncommon' })
    expect(getCardDef('white_perm_bonfire')).toMatchObject({ cost: 2, lightCost: 4, retainer: true, rarity: 'rare' })
    expect(REWARD_EXCLUDED.has('white_perm_lantern')).toBe(false)
    expect(REWARD_EXCLUDED.has('white_perm_bonfire')).toBe(false)
    expect(allCards.filter((c) => c.color === 'white').length).toBe(91) // 82 −撤去10 +火種10+トークン1 +放出8 (2026-09-20 夜)
    // 灯コストを持つ人形は5体だけ (コモンの人形・灯芯は据え置き)
    expect(allCards.filter((c) => c.retainer === true && (c.lightCost ?? 0) > 0).map((c) => c.id).sort()).toEqual(
      ['white_perm_band', 'white_perm_bandleader', 'white_perm_bonfire', 'white_perm_choir', 'white_perm_lantern'],
    )
  })

  it('灯コストの人形: 灯を払って出し、点灯 (出た瞬間に1回動く) はそのまま。支払いは LightSpent に残る。足りなければ出せない', () => {
    let s = light(energy(fresh(['white_perm_choir', 'white_perm_squire']), 9), 2)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = play(s, 't0_white_perm_choir')
    expect(s.player.hp).toBe(52) // 点灯: 攻撃ごと回復2 が出た瞬間に1回
    expect(s.player.light).toBe(1) // 灯2を払って0 → 点灯の回復で+1
    expect(s.eventLog.filter((e) => e.type === 'LightSpent').map((e) => (e.type === 'LightSpent' ? [e.amount, e.cardId] : null))).toEqual([[2, 'white_perm_choir']])
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(1)
    s = play(s, 't1_white_perm_squire') // コモンの人形も登場では灯を産まない (2026-09-20 夜 廃止)
    expect(gained(s, 'retainer')).toBe(0)
    expect(s.player.light).toBe(1)
    let t = light(energy(fresh(['white_perm_band']), 9), 1)
    expect(() => play(t, 't0_white_perm_band')).toThrow('灯が足りない')
  })

  it('灯篭の人形: 毎ターン開始に敵全体へ灯2につき1ダメ (切り捨て・灯は失わない)。放出すると暗くなる', () => {
    let s = light(energy(fresh(['white_perm_lantern', 'white_light_bolt'], 'enc_probe_pair'), 9), 6)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_white_perm_lantern') // 灯2を払って4 → 点灯: 4÷2=2 を全体に
    expect(s.player.light).toBe(4)
    expect(s.enemies.map((e, i) => hp[i] - e.hp)).toEqual([2, 2])
    expect(gained(s, 'retainer')).toBe(0)
    s = play(s, 't1_white_light_bolt') // 灯4を放出 → 灯0 = 暗い
    expect(s.player.light).toBe(0)
    const hp2 = s.enemies.map((e) => e.hp)
    s = applyCommand(s, { type: 'EndTurn' })
    // 次の自ターン開始: 灯は 0 (スターター単発=パッシブ無し) → 灯篭は打たない (DamageDealt を出さない)
    const dealt = s.eventLog.filter((e) => e.type === 'DamageDealt' && e.source === 'player' && e.sourceUid === 't0_white_perm_lantern')
    expect(dealt.length).toBe(2) // 点灯の2体ぶんだけ
    expect(s.enemies.map((e, i) => hp2[i] - e.hp)).toEqual([0, 0])
    // 灯を溜めれば明るく: 灯6 なら 3 を全体に
    let u = light(energy(fresh(['white_perm_lantern'], 'enc_probe_pair'), 9), 8)
    const h0 = u.enemies.map((e) => e.hp)
    u = play(u, 't0_white_perm_lantern')
    expect(u.enemies.map((e, i) => h0[i] - e.hp)).toEqual([3, 3])
  })

  it('篝火の人形: 2E・灯4。毎ターン開始に敵全体3＋ブロック3 (点灯で出た瞬間にも)', () => {
    let s = light(energy(fresh(['white_perm_bonfire'], 'enc_probe_pair'), 9), 4)
    const hp = s.enemies.map((e) => e.hp)
    const e0 = s.player.energy
    s = play(s, 't0_white_perm_bonfire')
    expect(s.player.energy).toBe(e0 - 2)
    expect(s.player.light).toBe(0)
    expect(s.enemies.map((e, i) => hp[i] - e.hp)).toEqual([3, 3])
    expect(s.player.block).toBe(3)
  })

  it('鍛え: 灯コストの人形は灯-1 (癒し+=1E・灯1・回復2のまま、灯篭+=灯1、篝火+=灯3)。合成: 人形×人形は灯コスト合算', () => {
    expect(upgradeCard(inst('white_perm_choir')).def).toMatchObject({ cost: 1, lightCost: 1 })
    expect(upgradeCard(inst('white_perm_choir')).def.effects[0].amount).toBe(2)
    expect(upgradeCard(inst('white_perm_lantern')).def).toMatchObject({ cost: 1, lightCost: 1 })
    expect(upgradeCard(inst('white_perm_bonfire')).def).toMatchObject({ cost: 2, lightCost: 3 })
    const f = fuseCards(inst('white_perm_band'), inst('white_perm_bandleader'))
    expect(f.type).toBe('permanent')
    expect(f.lightCost).toBe(4)
    expect(f.retainer).toBe(true) // 人形は溶かしても人形 (点灯・号令の対象のまま)
    // 真・剣の人形 (人形×人形) も人形。軸一致のおまけ (onPlay ブロック2) は点灯・号令では回さない
    const t = fuseCards(inst('white_perm_squire'), inst('white_perm_squire'))
    expect(t.retainer).toBe(true)
    let s = energy(fresh([]), 9)
    s = { ...s, player: { ...s.player, hand: [{ uid: 'f0', def: t }] } }
    const hp0 = s.enemies[0].hp
    s = play(s, 'f0')
    expect(hp0 - s.enemies[0].hp).toBe(4) // 点灯: 毎T4ダメが1回 (onPlay のブロックは2回にならない)
    expect(s.player.block).toBe(2)
    // 灯篭 × コモンの人形: 灯参照は置物のまま残る
    const g = fuseCards(inst('white_perm_lantern'), inst('white_perm_squire'))
    expect(g.effects.some((e) => e.effect === 'dealDamagePerLight')).toBe(true)
    expect(g.lightCost).toBe(2)
  })

  it('アンセム (輝き増し・灯の捧げ) はダメージ・ブロック・回復の量にだけ乗る (2026-09-20 夜 裁定): 灯芯の灯+1・灯篭の率・鐘のドローは据え置き', () => {
    // 灯り増し (常在+1) を置いてから 剣 (2ダメ→3)・灯芯 (灯+1のまま)・鐘 (登場ごと1ドローのまま)・灯篭 (灯2につき1のまま)
    let s = light(energy(fresh(['white_perm_warcry', 'white_perm_squire', 'white_perm_wick', 'white_perm_lantern', 'white_perm_band']), 9), 6)
    s = play(s, 't0_white_perm_warcry')
    const hp0 = s.enemies[0].hp
    s = play(s, 't1_white_perm_squire')
    expect(hp0 - s.enemies[0].hp).toBe(3) // 2+1
    const l0 = s.player.light ?? 0
    s = play(s, 't2_white_perm_wick')
    expect(s.player.light).toBe(l0 + 1) // 灯+1 (+1は乗らない)
    const hp1 = s.enemies[0].hp
    const lBefore = s.player.light ?? 0
    s = play(s, 't3_white_perm_lantern') // 灯2を払う → floor((lBefore-2)/2)×1 (率は+1されない)
    expect(hp1 - s.enemies[0].hp).toBe(Math.floor((lBefore - 2) / 2))
    const hand0 = s.player.hand.length
    s = { ...s, player: { ...s.player, light: 5 } }
    s = play(s, 't4_white_perm_band')
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 登場ごと1ドローのまま (3ドローにならない)
  })

  it('号令の予告 (rallyPreview): 1周の与ダメ・ブロック・回復の概算。アンセム込み・灯篭は解決時の灯で読む', () => {
    let s = light(energy(fresh(['white_perm_warcry', 'white_perm_squire', 'white_perm_shieldmaiden', 'white_perm_lantern'], 'enc_probe_pair'), 9), 4)
    s = play(s, 't0_white_perm_warcry')
    s = play(s, 't1_white_perm_squire')
    s = play(s, 't2_white_perm_shieldmaiden')
    s = play(s, 't3_white_perm_lantern') // 灯4→2
    const rp0 = rallyPreview(s, 0) // 大行列 (灯0で動く): 剣3・盾3・灯篭0
    expect(rp0).toEqual({ count: 3, damage: 3, block: 3, heal: 0 })
    const rp6 = rallyPreview(s, 6, ['white_perm_page']) // 合図: 灯6で灯篭は 3×2体、小さな人形 (1+1) 込み
    expect(rp6).toEqual({ count: 4, damage: 3 + 3 * 2 + 2, block: 3, heal: 0 })
  })
})
