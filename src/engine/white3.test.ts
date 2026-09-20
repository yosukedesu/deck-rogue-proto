// 白の解凍 (2026-09-06。docs/white-synergy-audit.md の実装): 品質パス撤去4・本家型の条件札7・全体の床・従者軸5・新機構6
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { effectiveCost, resolveReactionEffects, retainerRequirementMet } from './effects.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, freshCombat, withHand } from './test-helpers.ts'
import { REWARD_EXCLUDED } from './run.ts'
import type { CardDef, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[]): GameState =>
  withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_white'), ids)
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })

describe('白の品質パス (撤去4・スターター差し替え)', () => {
  it('巡礼の鈴・城門の閂・燦光の槌・祈りの残光は存在しない。見習いは報酬プール外のトークン', () => {
    for (const id of ['white_perm_pilgrim_bell', 'white_gate_bar', 'white_radiant_hammer', 'white_flash_heal']) {
      expect(allCards.some((c) => c.id === id), id).toBe(false)
    }
    const page = getCardDef('white_perm_page')
    expect(page.retainer).toBe(true) // 召喚された個体は summonPermanent が token:true を付ける
    expect(REWARD_EXCLUDED.has('white_perm_page')).toBe(true)
  })
})

describe('白の参照シナジー (本家6型の条件札7)', () => {
  it('祈りの刃: 6ダメ。直前の敵フェーズを完全に凌いでいたら回復6', () => {
    let s = fresh(['white_blade_prayer'])
    s = { ...s, player: { ...s.player, hp: 50, perfectBlockLastPhase: true } }
    s = play(s, 't0_white_blade_prayer')
    expect(s.player.hp).toBe(56)
    let t = fresh(['white_blade_prayer'])
    t = { ...t, player: { ...t.player, hp: 50, perfectBlockLastPhase: false } }
    t = play(t, 't0_white_blade_prayer')
    expect(t.player.hp).toBe(50)
  })

  it('修繕の祈り: ブロック6。このターンに回復していたらさらに6 (過剰回復でも「回復した」に数える)', () => {
    let s = energy(fresh(['white_heal', 'white_mending']), 3)
    s = play(s, 't0_white_heal') // 満タンでの回復 = 過剰回復でも healsThisTurn+1
    s = play(s, 't1_white_mending')
    expect(s.player.block).toBe(12)
    const t = play(energy(fresh(['white_mending']), 3), 't0_white_mending')
    expect(t.player.block).toBe(6)
    // 置物の自動回復 (修道士を進軍の号令で今すぐ動かす) は「カードで回復」ではない = 条件は成立しない (Opusラン W の是正)
    let u = energy(fresh(['white_perm_monk', 'white_march_order', 'white_mending']), 9)
    u = { ...u, player: { ...u.player, hp: 50 } }
    u = play(u, 't0_white_perm_monk') // 手当ての人形 = 置物が場に出るたび回復1 (回し封じ 2026-09-20): 自分の登場で回復1 → 灯は回復+1 = 1 (登場の+1は無い 2026-09-20 夜)
    expect(u.player.light).toBe(1)
    u = { ...u, player: { ...u.player, light: 2 } }
    u = play(u, 't1_white_march_order') // 号令 (1E・灯2): 小さな人形を点灯 → その登場で手当ての人形が回復1 (灯+1)。号令自体は登場ごとを動かさない
    expect(u.player.hp).toBe(52)
    expect(u.player.light).toBe(1) // 灯2を払い、登場ごとの回復で+1 (号令の外の誘発なので灯を産む) = 合図の正味は灯1〜2
    u = play(u, 't2_white_mending')
    expect(u.player.block).toBe(6)
  })

  it('癒しの光: 回復6。HPが半分以下ならさらに4', () => {
    let low = fresh(['white_heal'])
    low = { ...low, player: { ...low.player, hp: 30 } }
    low = play(low, 't0_white_heal')
    expect(low.player.hp).toBe(40)
    let high = fresh(['white_heal'])
    high = { ...high, player: { ...high.player, hp: 60 } }
    high = play(high, 't0_white_heal')
    expect(high.player.hp).toBe(66)
  })

  it('freeIfHandAll: nonphysical (2026-09-06 裁定) は仮の札で固定 (大光壁は 2026-09-20 夜に撤去)', () => {
    const wall: CardDef = { id: 'test_wall', name: '試しの壁', cost: 2, type: 'physical', color: 'white', freeIfHandAll: 'nonphysical', effects: [{ trigger: 'onPlay', effect: 'gainBlock', amount: 14 }] }
    const withWall = (ids: readonly string[]): GameState => {
      const s0 = fresh(ids)
      return { ...s0, player: { ...s0.player, hand: [{ uid: 'w0', def: wall }, ...s0.player.hand] } }
    }
    // 置物・リアクションが混じっていても物理が無ければ0E
    const noPhys = withWall(['white_perm_squire', 'white_reaction_ward', 'white_heal'])
    expect(effectiveCost(noPhys, noPhys.player.hand[0])).toBe(0)
    const spells = withWall(['white_heal', 'white_light_ledger'])
    expect(effectiveCost(spells, spells.player.hand[0])).toBe(0)
    const mixed = withWall(['white_heal', 'white_strike'])
    expect(effectiveCost(mixed, mixed.player.hand[0])).toBe(2)
    const s = play(spells, 'w0')
    expect(s.player.block).toBe(14)
    expect(s.player.energy).toBe(spells.player.energy) // 0Eで撃てた
  })

  it('隊列の突き: 置物×3。とどめなら従者の少年を1体召喚', () => {
    let s = energy(fresh(['white_perm_squire', 'white_rank_thrust']), 5)
    s = play(s, 't0_white_perm_squire')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 3, block: 0 })) }
    s = play(s, 't1_white_rank_thrust')
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_squire').length).toBe(2)
    // とどめでなければ召喚しない
    let t = energy(fresh(['white_perm_squire', 'white_rank_thrust']), 5)
    t = play(t, 't0_white_perm_squire')
    t = play(t, 't1_white_rank_thrust')
    expect(t.player.permanents.filter((p) => p.def.id === 'white_perm_squire').length).toBe(1)
  })

  it('報復の光: 返し10。その攻撃を完全に防いでいたら返し+10 (効果ごとの条件をリアクション解決で判定)', () => {
    const base = fresh([])
    const card = { uid: 'r0', def: getCardDef('white_reaction_retribution') }
    const blocked: GameState = { ...base, lastAction: { enemyIndex: 0, kind: 'attack', hpLoss: 0, actual: 12 } }
    const hp0 = blocked.enemies[0].hp
    const a = resolveReactionEffects(blocked, card, 0)
    expect(hp0 - a.enemies[0].hp).toBe(20)
    const hurt: GameState = { ...base, lastAction: { enemyIndex: 0, kind: 'attack', hpLoss: 5, actual: 12 } }
    const b = resolveReactionEffects(hurt, card, 0)
    expect(hp0 - b.enemies[0].hp).toBe(10)
  })

  it('聖戦の号砲: 全体2＋このターンにプレイした攻撃×2 (薙ぎ払いの白版)', () => {
    let s = energy(fresh(['white_strike', 'white_war_horn']), 3)
    s = play(s, 't0_white_strike')
    const hp1 = s.enemies[0].hp
    s = play(s, 't1_white_war_horn')
    expect(hp1 - s.enemies[0].hp).toBe(2 + 2)
  })
})

describe('白の従者軸 (ばらまき・倍加・対価・号令)', () => {
  it('見習いの列: 見習い (毎T1ダメ) を2体召喚。登場誘発 (軍楽隊) は2回', () => {
    let s = light(energy(fresh(['white_perm_band', 'white_page_rank']), 5), 2) // 鐘の人形は 1E・灯2 (2026-09-20)
    s = play(s, 't0_white_perm_band')
    const hand0 = s.player.hand.length
    s = play(s, 't1_white_page_rank')
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(2)
    expect(s.player.hand.length).toBe(hand0 - 1 + 2)
  })

  it('聖なる行列: 少年1体＋盾の乙女1体', () => {
    let s = energy(fresh(['white_holy_procession']), 3)
    s = play(s, 't0_white_holy_procession')
    expect(s.player.permanents.map((p) => p.def.id).sort()).toEqual(['white_perm_shieldmaiden', 'white_perm_squire'])
  })

  it('分列の奇跡: 場の従者1体につき同じ従者を1体召喚。複製は複製を産まない。消滅する', () => {
    let s = light(energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_perm_band', 'white_miracle_division']), 9), 2)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden') // 登場で灯は増えない (2026-09-20 夜)
    s = play(s, 't2_white_perm_band') // 灯2を払う
    const hand0 = s.player.hand.length
    s = play(s, 't3_white_miracle_division')
    expect(s.player.permanents.length).toBe(6)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_band').length).toBe(2)
    // 軍楽隊の登場誘発: 少年の複製で1 (軍楽隊1体)・乙女の複製で1・軍楽隊の複製で2 (自身も含め2体) = 4ドロー
    expect(s.player.hand.length).toBe(hand0 - 1 + 4)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'white_miracle_division')).toBe(true)
  })

  it('分列の奇跡: 複製同士は互いの登場に反応しない (2026-09-06 裁定。軍楽長が先頭にいても線形)', () => {
    let s = light(energy(fresh(['white_perm_bandleader', 'white_perm_squire', 'white_perm_shieldmaiden', 'white_miracle_division']), 9), 2) // 大鐘は 1E・灯2
    s = play(s, 't0_white_perm_bandleader')
    s = play(s, 't1_white_perm_squire')
    s = play(s, 't2_white_perm_shieldmaiden')
    const b0 = s.player.block
    s = play(s, 't3_white_miracle_division')
    // 軍楽長の複製: 元の軍楽長+2・自分自身+2 ／ 少年の複製: 元+2 (複製の軍楽長は反応しない) ／ 乙女の複製: 元+2 = 8
    // + 点灯の定義 (2026-09-20 白共通ルール): 複製の盾の人形が出た瞬間に1回動く (+2) = 10。剣の人形の複製は2ダメ (ブロックには乗らない)
    expect(s.player.block - b0).toBe(10)
  })

  it('殉教の誓い: 従者0ならプレイ不可。選んだ従者だけ消え、この置物がある間 従者+2', () => {
    const none = energy(fresh(['white_perm_martyr_vow']), 3)
    expect(retainerRequirementMet(none, none.player.hand[0])).toBe(false)
    expect(() => play(none, 't0_white_perm_martyr_vow', { permanentUid: 'x' })).toThrow('従者が1体以上')
    let s = light(energy(fresh(['white_perm_squire', 'white_page_rank', 'white_perm_martyr_vow', 'white_march_order']), 9), 2) // 合図の灯2
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_page_rank')
    const page = s.player.permanents.find((p) => p.def.id === 'white_perm_page')!
    expect(() => play(s, 't2_white_perm_martyr_vow')).toThrow('permanentUid')
    s = play(s, 't2_white_perm_martyr_vow', { permanentUid: page.uid })
    expect(s.player.permanents.some((p) => p.uid === page.uid)).toBe(false)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(1)
    expect(s.eventLog.some((e) => e.type === 'RetainerSacrificed')).toBe(true)
    // 点灯の合図 (2026-09-20): 小さな人形1体を点灯 (1+2=3) してから号令 = 剣2+2・小さな人形1+2 ×2体 = 10。誓い自身 (置物) は従者でないので鳴らない
    const hp0 = s.enemies[0].hp
    s = play(s, 't3_white_march_order')
    expect(hp0 - s.enemies[0].hp).toBe(3 + 4 + 3 + 3)
    expect(s.eventLog.some((e) => e.type === 'RetainersTriggered' && e.count === 3)).toBe(true)
  })

  it('進軍の号令: 従者だけを再誘発 (盾の乙女=ブロック・少年=ダメ)。innate 置物は対象外。従者0では空撃ちできない (Opusラン W)', () => {
    // 2026-09-20 裁定: 合図は人形0でも仕事をする (小さな人形1体を点灯してから号令)。灯2が要る
    const none = { ...energy(fresh(['white_march_order']), 9), player: { ...energy(fresh(['white_march_order']), 9).player, light: 2 } }
    expect(retainerRequirementMet(none, none.player.hand[0])).toBe(true)
    const n2 = play(none, 't0_white_march_order')
    expect(n2.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(1)
    let s = light(energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_march_order']), 9), 2)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    const hp0 = s.enemies[0].hp
    const b0 = s.player.block
    s = play(s, 't2_white_march_order')
    expect(hp0 - s.enemies[0].hp).toBe(1 + 2 + 1) // 小さな人形の点灯1 + 号令 (剣2・小さな人形1)
    expect(s.player.block - b0).toBe(2)
  })
})

describe('ひなたのパッシブ「駆けつけ」(2026-09-06 ユーザー裁定: 従者が場に出た時、すぐに1回動く)', () => {
  const hinata = (): GameState => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_white')
    const s = run.combat!
    expect(s.player.permanents.some((p) => p.def.id === 'leader_white_passive')).toBe(true)
    return { ...s, player: { ...s.player, energy: 9, energyMax: 9 } }
  }

  it('従者の少年を出すとその場で2ダメ。白銀の号令があればアンセム込みで3。道具 (光の聖杯) では何も起きない', () => {
    let s = withHand(hinata(), ['white_perm_squire', 'white_perm_warcry', 'white_perm_squire', 'white_perm_chalice'])
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_perm_squire')
    expect(hp0 - s.enemies[0].hp).toBe(2)
    s = play(s, 't1_white_perm_warcry')
    s = play(s, 't2_white_perm_squire')
    expect(hp0 - s.enemies[0].hp).toBe(2 + 3)
    const before = s.eventLog.filter((e) => e.type === 'RetainerRushed').length
    s = play(s, 't3_white_perm_chalice')
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(before)
    expect(hp0 - s.enemies[0].hp).toBe(5)
  })

  it('見習いの列 (召喚2体) は2回駆けつける = 1+1ダメ。鐘の人形 (登場ごとのみ) は自分の登場で1回鳴るだけ = 駆けつけで二重にはならない', () => {
    let s = withHand(hinata(), ['white_page_rank', 'white_perm_band'])
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_page_rank')
    expect(hp0 - s.enemies[0].hp).toBe(2)
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(2)
    const hand0 = s.player.hand.length
    s = light(s, 2) // 鐘は 1E・灯2 (登場では灯は増えないので ひなたの灯1 では足りない)
    s = play(s, 't1_white_perm_band')
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(2)
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 自分の登場で1ドローだけ (駆けつけの分は無い)
  })

  it('攻撃ごとの従者も出た時に1回動く (2026-09-19 ユーザー「従者の能力は全て出た時に誘発させるパッシブ」): 犬=2ダメ・旗=ブロック2・燭=回復1', () => {
    let s = withHand(hinata(), ['white_perm_hound', 'white_perm_banneret', 'white_perm_candle'])
    s = { ...s, player: { ...s.player, hp: 50 } }
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_perm_hound')
    expect(hp0 - s.enemies[0].hp).toBe(2)
    const b0 = s.player.block
    s = play(s, 't1_white_perm_banneret')
    expect(s.player.block - b0).toBe(2)
    s = play(s, 't2_white_perm_candle')
    expect(s.player.hp).toBe(51)
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(3)
  })

  it('旧パッシブ (毎T回復1・回復ごとブロック1) は無い = 修繕の祈りの条件は自動では成立しない', () => {
    const s = withHand(hinata(), ['white_mending'])
    const t = play(s, 't0_white_mending')
    expect(t.player.block).toBe(6)
  })
})

describe('Opusラン W の裁定 (2026-09-06)', () => {
  it('進軍の号令は素で1E (2Eは従者3体で1E札相当の損。1E化は鍛えるで得られる設計だったが素で候補に上がらない)', () => {
    expect(getCardDef('white_march_order').cost).toBe(1)
    expect(getCardDef('white_march_order').lightCost).toBe(2) // 2026-09-20: 1E・灯2。人形0でも小さな人形を点灯するので requiresRetainer は無い
    expect(getCardDef('white_march_order').requiresRetainer).toBeUndefined()
  })

  it('鬼軍曹の怒りはカードのプレイ由来のブロックだけ・1枚のプレイで1回: 修繕の祈り(ブロック6+条件6)=+1、従者の自動ブロック=+0', () => {
    const start = (): GameState => {
      const s = withHand(freshCombat('set-confirm', 'enemy_elite_sergeant', 42, 'starter_white'), [])
      return energy(s, 9)
    }
    // 修繕の祈り: 癒しの光で条件を立ててから撃つ = ブロック12 だが怒りは+1
    let s = withHand(start(), ['white_heal', 'white_mending'])
    const str0 = s.enemies[0].strength
    s = play(s, 't0_white_heal')
    s = play(s, 't1_white_mending')
    expect(s.player.block).toBe(12)
    expect(s.enemies[0].strength).toBe(str0 + 1)
    // 盾の人形を出して (点灯の定義で出た瞬間に+2) 点灯の合図 (1E・灯2) で今すぐ動かす: 置物由来のブロックは怒らない
    let t = withHand(start(), ['white_perm_shieldmaiden', 'white_march_order'])
    const str1 = t.enemies[0].strength
    t = play(t, 't0_white_perm_shieldmaiden')
    t = { ...t, player: { ...t.player, light: 2 } }
    t = play(t, 't1_white_march_order')
    expect(t.player.block).toBe(4)
    expect(t.enemies[0].strength).toBe(str1)
    // 白盾 (1効果) は従来どおり+1
    let u = withHand(start(), ['white_guard'])
    const str2 = u.enemies[0].strength
    u = play(u, 't0_white_guard')
    expect(u.enemies[0].strength).toBe(str2 + 1)
  })
})

