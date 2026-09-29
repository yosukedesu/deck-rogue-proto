// 灯 (白の再設計 2026-09-20。docs/white-redesign-proposal-2026-09-20.md): 供給3経路・放出・号令・点灯の定義・鍛え・合成を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, allDecks, getCardDef, getLeaderDef } from './content.ts'
import { fuseCards } from './fusion.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { rallyPreview } from './effects.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, freshCombat, withHand } from './test-helpers.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardDef, CardInstance, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[], enemy = 'enemy_probe'): GameState =>
  withHand(freshCombat('set-confirm', enemy, 42, 'starter_white'), ids)
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
const inst = (id: string): CardInstance => ({ uid: `x_${id}`, def: getCardDef(id) })
const withDef = (s: GameState, def: CardDef): GameState => ({ ...s, player: { ...s.player, hand: [{ uid: `d_${def.id}`, def }, ...s.player.hand] } })
// 2026-09-24 白のプール 108→84 (docs/white-pool-trim-proposal-2026-09-23.md): 撤去した札の機構は仮の札で固定
const JUDGE: CardDef = { id: 'test_judge', name: '試しの灼く光', cost: 2, type: 'spell', color: 'white', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 8 }, { trigger: 'onPlay', effect: 'dealDamage', amount: 8, condition: { minLight: 5 } }] }
const BALLISTA: CardDef = { id: 'test_ballista', name: '試しの灯の弩', cost: 2, type: 'permanent', color: 'white', effects: [{ trigger: 'onLightGained', effect: 'dealDamage', amount: 1, target: 'all' }] }
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

  it('回復するたび灯+1。過剰回復 (満タン) でも溜まる (大いなる癒しは明示の灯+3 も持つ。癒しの光は 2026-09-24 に撤去)', () => {
    let s = energy(fresh(['white_mass_heal', 'white_mass_heal']), 9)
    s = play(s, 't0_white_mass_heal') // 満タン: 回復+1・明示+3
    expect(s.player.light).toBe(4)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = play(s, 't1_white_mass_heal')
    expect(s.player.light).toBe(8)
    expect(gained(s, 'heal')).toBe(2)
    expect(gained(s, 'card')).toBe(2) // 明示の灯は1回の獲得 (量3) ×2枚
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
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(4) // 点灯 (1回動く) はそのまま
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
    expect(hp0 - s.enemies[0].hp).toBe(3)
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
  it('灯の矢: 灯1を払って4ダメ×3 (2026-09-23 裁定B「放出を灯Nを払うに」・同日「本家リージェント並み」で×2→×3)。払った残りは持ち越し、2枚目も撃てる。灯0では出せない', () => {
    let s = light(energy(fresh(['white_light_bolt', 'white_light_bolt', 'white_light_bolt']), 9), 2)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(12)
    expect(s.player.light).toBe(1)
    const hp1 = s.enemies[0].hp
    s = play(s, 't1_white_light_bolt')
    expect(hp1 - s.enemies[0].hp).toBe(12)
    expect(s.player.light).toBe(0)
    expect(s.eventLog.filter((e) => e.type === 'LightSpent').length).toBe(2)
    expect(s.eventLog.filter((e) => e.type === 'LightDischarged').length).toBe(0)
    expect(() => play(s, 't2_white_light_bolt')).toThrow('灯が足りない')
  })

  it('灯を払う攻撃は普通の多段 = 成長はヒットごとに乗る (単体の全放出「灯1につき1ヒット」は 2026-09-23 に R の大放出だけ=全体一括になり、engine の規則は残置)', () => {
    let s = light(energy(fresh(['white_light_bolt']), 9), 3)
    s = { ...s, player: { ...s.player, growth: 2 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(3 * (4 + 2))
    expect(s.eventLog.filter((e) => e.type === 'DamageDealt').length).toBe(3)
    expect(s.player.light).toBe(2)
  })

  it('装甲5の敵に灯の矢 (4×3): 各ヒットが上限以下なので 12 が丸ごと通る', () => {
    let s = light(energy(fresh(['white_light_bolt'], 'enemy_iron_clam'), 9), 1)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_light_bolt')
    expect(hp0 - s.enemies[0].hp).toBe(12)
  })

  it('眩光の大放出: 全体に灯×4 を一括 (2体目も同じ量。2026-09-23 ×2→×3・2026-09-24 CSV の裁定で ×4) + 全体に威圧1', () => {
    let s = light(energy(fresh(['white_light_burst'], 'enc_probe_pair'), 9), 3)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_white_light_burst')
    expect(hp[0] - s.enemies[0].hp).toBe(12)
    expect(hp[1] - s.enemies[1].hp).toBe(12)
    expect(s.enemies.every((e) => e.weak === 1)).toBe(true)
    expect(s.player.light).toBe(0)
  })

  it('灯の輪: 灯2を払い、回復6 (灯+1) と 6ダメ×2 (2026-09-23 裁定B。回復は「殴った分だけ」の形なので消滅なし)', () => {
    let s = light(energy(fresh(['white_praise_chorus']), 9), 2)
    s = { ...s, player: { ...s.player, hp: 50 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_praise_chorus')
    expect(s.player.hp).toBe(56)
    expect(hp0 - s.enemies[0].hp).toBe(12)
    expect(s.player.light).toBe(1) // 2を払って0、回復で+1
  })

  it('灯の倍化: 灯3 → 6 (獲得の誘発が乗る)。消滅を持つ', () => {
    let s = light(energy(fresh(['white_light_double']), 9), 3)
    s = play(s, 't0_white_light_double')
    expect(s.player.light).toBe(6)
    expect(getCardDef('white_light_double').exhaust).toBe(true)
  })
})

describe('しきい値 (minLight) と灯を得るたびの誘発', () => {
  it('minLight (灼く光・光の裁きは 2026-09-24 に撤去=仮の札): 8ダメ。灯5以上ならさらに8 (解決の時点で判定)', () => {
    let s = light(energy(withDef(fresh([]), JUDGE), 9), 4)
    const hp0 = s.enemies[0].hp
    s = play(s, 'd_test_judge')
    expect(hp0 - s.enemies[0].hp).toBe(8)
    let t = light(energy(withDef(fresh([]), JUDGE), 9), 5)
    const hp1 = t.enemies[0].hp
    t = play(t, 'd_test_judge')
    expect(hp1 - t.enemies[0].hp).toBe(16)
    expect(t.player.light).toBe(5) // 参照するだけで消費しない
  })

  it('onLightGained (灯の弩は 2026-09-24 に撤去=仮の札): 灯を得るたび敵全体に1 (回復・明示の札のどれでも。人形の登場では鳴らない)', () => {
    let s = energy(withDef(fresh(['white_mass_heal', 'white_perm_squire']), BALLISTA), 9)
    s = play(s, 'd_test_ballista')
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_mass_heal') // 回復の灯+1 → 弩1、明示の灯+3 (1回の獲得) → 弩1
    expect(hp0 - s.enemies[0].hp).toBe(2)
    s = play(s, 't1_white_perm_squire') // 登場では灯は増えない (弩は鳴らない)、点灯で剣の人形2
    expect(hp0 - s.enemies[0].hp).toBe(2 + 3)
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
    expect(hp0 - s.enemies[0].hp).toBe(2 + 3 + 2 + 2)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 合図の小さな人形の登場で鐘が1ドロー (登場ごと)
    expect(s.player.light).toBe(3 - 2) // 小さな人形の登場で灯は戻らない (2026-09-20 夜 廃止) = 合図の正味は灯2
    expect(s.player.energy).toBe(e0 - 1)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(1)
    expect(getCardDef('white_march_order').requiresRetainer).toBeUndefined() // 人形0でも仕事をする (A の処方)
    let t = light(energy(fresh(['white_perm_squire', 'white_march_order']), 9), 1)
    t = play(t, 't0_white_perm_squire')
    expect(() => play(t, 't1_white_march_order')).toThrow('灯が足りない')
  })

  it('灯火の大行列: 灯を全て放出し、灯2につき全人形が1回動く (2026-09-24 ユーザー「灯2につき」。旧: 灯1につき)。人形0なら撃てない', () => {
    let s = energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_grand_charge']), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    s = light(s, 3)
    const hp0 = s.enemies[0].hp
    const b0 = s.player.block
    s = play(s, 't2_white_grand_charge') // 灯3 → 1回 (端数は放出されるだけ)
    expect(hp0 - s.enemies[0].hp).toBe(3)
    expect(s.player.block - b0).toBe(3)
    expect(s.player.light).toBe(0)
    expect(getCardDef('white_grand_charge').requiresRetainer).toBe(true)
  })

  it('号令・大行列の中では灯を産まない (2026-09-20 裁定): 灯芯の人形・癒しの人形が動いても灯は戻らない', () => {
    let s = light(energy(fresh(['white_perm_wick', 'white_perm_choir', 'white_grand_charge']), 9), 1)
    s = play(s, 't0_white_perm_wick') // 灯2 (自分の効果で+1)
    s = play(s, 't1_white_perm_choir') // 灯2を払って0 → 点灯の回復1で灯1
    expect(s.player.light).toBe(1)
    s = { ...s, player: { ...s.player, hp: 50 }, }
    s = light(s, 4)
    s = play(s, 't2_white_grand_charge') // 灯4を放出 → 各人形が2回動く。灯芯の+1×2 も 癒しの回復1×2 (2026-09-25 半分) の+1×2 も鳴らない
    expect(s.player.light).toBe(0)
    expect(s.player.hp).toBe(52)
  })

  it('人形の単体ダメージはランダムな生存敵へ (2026-09-20 裁定)。合計は変わらず、2体戦で RNG を1回ずつ消費する', () => {
    let s = energy(fresh(['white_perm_squire', 'white_perm_squire', 'white_perm_squire'], 'enc_probe_pair'), 9)
    const hp = s.enemies.map((e) => e.hp)
    const rng0 = s.rng
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_squire')
    s = play(s, 't2_white_perm_squire')
    const lost = s.enemies.map((e, i) => hp[i] - e.hp)
    expect(lost[0] + lost[1]).toBe(9)
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
  it('鍛え: 合図+=灯コスト1 (1Eのまま)・灯の矢+=×3・光の奔流+=×4・灯り溜め+=灯+3・ブロック8・大行列+=1E (灼く光は 2026-09-24 に撤去)', () => {
    expect(upgradeCard(inst('white_march_order')).def).toMatchObject({ cost: 1, lightCost: 1 })
    expect(upgradeCard(inst('white_light_bolt')).def).toMatchObject({ lightCost: 0 }) // 2026-09-23 裁定B: 灯を払う攻撃の鍛えは灯コスト−1 (灯の矢+=灯0の4×2)
    expect(upgradeCard(inst('white_light_torrent')).def).toMatchObject({ lightCost: 2 })
    expect(upgradeCard(inst('white_light_torrent')).def.effects[0].amount).toBe(9)
    const h = upgradeCard(inst('white_light_hoard')).def
    expect(h.effects.map((e) => e.amount)).toEqual([12, 3]) // 灯り溜め 5→8 (2026-09-24 CSV) の +50%
    expect(upgradeCard(inst('white_grand_charge')).def.cost).toBe(1)
  })

  it('合成: 灯コストは合算。放出は置物では落ちる', () => {
    const f = fuseCards(inst('white_march_order'), inst('white_light_bolt')) // 合図の灯2 + 灯の矢の灯1 (灯集めの一撃は 2026-09-24 に撤去)
    expect(f.lightCost).toBe(3)
    const p = fuseCards(inst('white_light_bolt'), inst('white_perm_squire'))
    expect(p.type).toBe('permanent')
    expect(p.effects.some((e) => e.effect === 'dischargeLight')).toBe(false)
  })

  it('灯の矢がスターター専用 (報酬プール外)、継ぎ火は報酬プールへ (2026-09-21 夜 ユーザー裁定「継ぎ火を消して灯の矢に」)。灯コスト持ちは白だけ', () => {
    expect(REWARD_EXCLUDED.has('white_light_bolt')).toBe(true) // 灯の矢 = 初期デッキの灯の教材 (放出)。継ぎ火は人形0体では死に札だった
    expect(REWARD_EXCLUDED.has('white_relight')).toBe(false)
    expect(allDecks.find((d) => d.id === 'run_basic_white')!.cards.map((c) => c.cardId)).toContain('white_light_bolt')
    expect(allDecks.find((d) => d.id === 'run_basic_white')!.cards.map((c) => c.cardId)).not.toContain('white_relight')
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
    expect(allCards.filter((c) => c.color === 'white').length).toBe(86) // 2026-09-26 竜と獅子の人形 +2・2026-09-25 燭の人形 −1・2026-09-24 灯の薪 +1・ 白のプール 108→84 (−24)・同日 CSV の裁定で −7・Opus ひなた裁定で重ねる灯 −1
    // 灯コストを持つ人形は5体だけ (コモンの人形・灯芯は据え置き)
    expect(allCards.filter((c) => c.retainer === true && (c.lightCost ?? 0) > 0).map((c) => c.id).sort()).toEqual(
      ['white_perm_band', 'white_perm_bandleader', 'white_perm_bonfire', 'white_perm_choir', 'white_perm_lantern'],
    )
  })

  it('灯コストの人形: 灯を払って出し、点灯 (出た瞬間に1回動く) はそのまま。支払いは LightSpent に残る。足りなければ出せない', () => {
    let s = light(energy(fresh(['white_perm_choir', 'white_perm_squire']), 9), 2)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = play(s, 't0_white_perm_choir')
    expect(s.player.hp).toBe(51) // 点灯: 攻撃ごと回復1 (2026-09-25「人形の回復だけ半分」＝旧2) が出た瞬間に1回
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
    let s = light(energy(fresh(['white_perm_lantern', 'white_light_burst'], 'enc_probe_pair'), 9), 6)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_white_perm_lantern') // 灯2を払って4。点灯だけは払う前の灯6で解決 (2026-09-21) → 6÷2=3 を全体に
    expect(s.player.light).toBe(4)
    expect(s.enemies.map((e, i) => hp[i] - e.hp)).toEqual([3, 3])
    expect(gained(s, 'retainer')).toBe(0)
    s = play(s, 't1_white_light_burst') // 灯4を全て放出 (2026-09-23 裁定B 以後、全放出は R の大放出と大行列だけ) → 灯0 = 暗い
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
    u = play(u, 't0_white_perm_lantern') // 灯8 → 点灯は8÷2=4 (払った後は6)
    expect(u.enemies.map((e, i) => h0[i] - e.hp)).toEqual([4, 4])
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

  it('鍛え: 灯コストの人形は灯-1 (癒し+=1E・灯1・回復1のまま〔2026-09-25 半分＝旧2〕、灯篭+=灯1、篝火+=灯3)。合成: 人形×人形は灯コスト合算', () => {
    expect(upgradeCard(inst('white_perm_choir')).def).toMatchObject({ cost: 1, lightCost: 1 })
    expect(upgradeCard(inst('white_perm_choir')).def.effects[0].amount).toBe(1)
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
    expect(hp0 - s.enemies[0].hp).toBe(6) // 点灯: 毎T4ダメが1回 (onPlay のブロックは2回にならない)
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
    expect(hp0 - s.enemies[0].hp).toBe(4) // 3+1
    const l0 = s.player.light ?? 0
    s = play(s, 't2_white_perm_wick')
    expect(s.player.light).toBe(l0 + 1) // 灯+1 (+1は乗らない)
    const hp1 = s.enemies[0].hp
    const lBefore = s.player.light ?? 0
    s = play(s, 't3_white_perm_lantern') // 点灯は払う前の灯で解決 (2026-09-21) → floor(lBefore/2)×1 (率は+1されない)
    expect(hp1 - s.enemies[0].hp).toBe(Math.floor(lBefore / 2))
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
    const rp0 = rallyPreview(s, 0) // 大行列 (灯0で動く): 剣3+1・盾3+1・灯篭0
    expect(rp0).toEqual({ count: 3, damage: 4, block: 4, heal: 0 })
    const rp6 = rallyPreview(s, 6, ['white_perm_page']) // 合図: 灯6で灯篭は 3×2体、小さな人形 (2+1) 込み (これから出るので育ちは0)
    expect(rp6).toEqual({ count: 4, damage: 4 + 3 * 2 + 3, block: 4, heal: 0 })
  })
})

describe('灯の変換札 (2026-09-23 ユーザー「灯からマナに変換するカードなど面白いカードを増やして」→ ask_user: 燃料・炉心・注ぎ＋灯だけで打てる0Eをたくさん)', () => {
  it('灯の燃料: 0E・灯3。一時マナ+2＋1ドロー・消滅 (本家 Alignment 0E★3=+2E。消滅は灯り注ぎとの循環止め)', () => {
    let s = light(energy(fresh(['white_light_fuel', 'white_strike']), 3), 3)
    const hand0 = s.player.hand.length
    s = play(s, 't0_white_light_fuel')
    expect(s.player.energy).toBe(5)
    expect(s.player.light).toBe(0)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toEqual(['white_light_fuel'])
  })

  it('灯の薪 (2026-09-24 ユーザー「0マナ灯コストでエナジー加えるカードほしい」): 0E・灯3・一時マナ+1・消滅なし (灯2だと灯り注ぎと収支ゼロの無限ループ)', () => {
    let s = light(energy(fresh(['white_light_kindling', 'white_light_kindling']), 3), 4)
    s = play(s, 't0_white_light_kindling')
    expect(s.player.energy).toBe(4)
    expect(s.player.light).toBe(1)
    expect(() => play(s, 't1_white_light_kindling')).toThrow('灯が足りない')
    expect(s.player.exhaustPile).toHaveLength(0)
  })

  it('灯の炉心 (2026-09-24 CSV ユーザー案「払えなかったら墓地」): 毎ターン開始時に灯3未満なら置物が場を離れて捨て札へ (dismissUnlessLight)', () => {
    let s = light(energy(fresh(['white_perm_light_core']), 9), 2)
    s = play(s, 't0_white_perm_light_core')
    s = applyCommand(s, { type: 'EndTurn' }) // ターン開始で灯2 (+パッシブ無し) < 3
    expect(s.player.permanents.some((p) => p.def.id === 'white_perm_light_core')).toBe(false)
    expect(s.player.discardPile.some((c) => c.def.id === 'white_perm_light_core')).toBe(true)
    expect(s.eventLog.some((e) => e.type === 'PermanentDismissed')).toBe(true)
    expect(s.player.energy).toBe(s.player.energyMax) // 一時マナは出ない
  })

  it('灯の炉心: 毎ターン開始時、灯3以上なら灯3を払って一時マナ+1。灯3未満なら何もしない (consumeLight に amount)', () => {
    let s = light(energy(fresh(['white_perm_light_core']), 9), 5)
    s = play(s, 't0_white_perm_light_core')
    expect(s.player.light).toBe(5) // 人形ではないので置いた瞬間には動かない
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.energy).toBe(s.player.energyMax + 1)
    expect(s.player.light).toBe(2)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.energy).toBe(s.player.energyMax)
    expect(s.player.light).toBe(2)
  })

  it('灯り注ぎ: 0E・X。エナジーを全て払い X×2 の灯 (エナジー→灯)', () => {
    let s = light(energy(fresh(['white_light_pour']), 3), 1)
    s = play(s, 't0_white_light_pour')
    expect(s.player.energy).toBe(0)
    expect(s.player.light).toBe(1 + 6)
  })

  it('灯だけで打てる0E (本家並み 2026-09-23): 火矢 灯2で10・小盾 灯1でブロック7・火粉 灯3で全体7・頁 灯2で3ドロー (消滅なし。灯の呼び声は 2026-09-24 に撤去)', () => {
    let s = light(energy(fresh(['white_light_dart', 'white_light_buckler', 'white_light_cinders', 'white_light_page'], 'enc_probe_pair'), 1), 10)
    const hp = s.enemies.map((e) => e.hp)
    s = play(s, 't0_white_light_dart')
    expect(hp[0] - s.enemies[0].hp).toBe(10)
    s = play(s, 't1_white_light_buckler')
    expect(s.player.block).toBe(7)
    const hp2 = s.enemies.map((e) => e.hp)
    s = play(s, 't2_white_light_cinders')
    expect(s.enemies.map((e, i) => hp2[i] - e.hp)).toEqual([7, 7])
    expect(s.player.energy).toBe(1) // ここまでエナジーは1枚も使っていない
    expect(s.player.light).toBe(10 - 2 - 1 - 3)
    const hand0 = s.player.hand.length
    s = play(s, 't3_white_light_page')
    expect(s.player.hand.length).toBe(hand0 - 1 + 3)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toEqual([]) // 消滅なし (ユーザー裁定 2026-09-23。灯コスト札は 0E+補充の規約の例外)
    expect(s.player.light).toBe(2)
  })
})

describe('灯の供給札 (2026-09-23 ユーザー「供給カードを増やしたほうが良くない？」→ ask_user 6枚。本家 Glow/HiddenCache/ShiningStrike/KnockoutBlow/Convergence/Radiate の形)', () => {
  it('灯の埋め火: 灯+1、次のターン開始にさらに灯+2 (addLightNextTurn)。灯の集約: 保持・次のターン開始にエナジー+1と灯+2', () => {
    let s = light(energy(fresh(['white_light_ember_cache', 'white_light_convergence']), 9), 0)
    s = play(s, 't0_white_light_ember_cache')
    expect(s.player.light).toBe(1)
    s = play(s, 't1_white_light_convergence')
    expect(s.nextTurnLight).toBe(4)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.light).toBe(1 + 4) // スターター単発=パッシブ無し。敵フェーズで減らない
    expect(s.player.energy).toBe(s.player.energyMax + 1)
    expect(s.nextTurnLight).toBeUndefined()
    expect(s.eventLog.filter((e) => e.type === 'LightGained' && e.source === 'card').length).toBe(2)
  })

  it('輝きの一撃 (8+灯2。消滅は 2026-09-24 CSV で撤去)・灯の余光 (0E 3+灯2・消滅)・灯の大砕き (3E 30+灯5)・灯の輝き (灯+2+2ドロー)', () => {
    let s = light(energy(fresh(['white_shining_strike', 'white_light_radiance', 'white_light_knockout', 'white_light_glow']), 9), 0)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_shining_strike')
    s = play(s, 't1_white_light_radiance')
    expect(hp0 - s.enemies[0].hp).toBe(11)
    expect(s.player.light).toBe(4)
    expect(s.player.exhaustPile.map((c) => c.def.id).sort()).toEqual(['white_light_radiance'])
    const hp1 = s.enemies[0].hp
    s = play(s, 't2_white_light_knockout')
    expect(hp1 - s.enemies[0].hp).toBe(30)
    expect(s.player.light).toBe(9)
    const hand0 = s.player.hand.length
    s = play(s, 't3_white_light_glow')
    expect(s.player.light).toBe(11)
    expect(s.player.hand.length).toBe(hand0 - 1 + 2)
  })
})
