// 白の解凍 (2026-09-06。docs/white-synergy-audit.md の実装): 品質パス撤去4・本家型の条件札7・全体の床・従者軸5・新機構6
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { effectiveCost, resolveReactionEffects, retainerRequirementMet } from './effects.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, freshCombat, withHand } from './test-helpers.ts'
import { REWARD_EXCLUDED } from './run.ts'
import type { GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[]): GameState =>
  withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_white'), ids)
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
    u = play(u, 't0_white_perm_monk')
    u = play(u, 't1_white_march_order')
    expect(u.player.hp).toBe(51)
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

  it('大城壁: U・2E・ブロック14。自身を除く手札に物理が無ければ0E (2026-09-06 裁定: 2ラン9戦で0回だった「呪文だけ」を緩和)', () => {
    const def = getCardDef('white_fortress')
    expect(def.rarity).toBe('uncommon')
    expect(def.freeIfHandAll).toBe('nonphysical')
    // 置物・リアクションが混じっていても物理が無ければ0E
    const noPhys = fresh(['white_fortress', 'white_perm_squire', 'white_reaction_ward', 'white_heal'])
    expect(effectiveCost(noPhys, noPhys.player.hand[0])).toBe(0)
    const spells = fresh(['white_fortress', 'white_heal', 'white_mercy_staff'])
    expect(effectiveCost(spells, spells.player.hand[0])).toBe(0)
    const mixed = fresh(['white_fortress', 'white_heal', 'white_strike'])
    expect(effectiveCost(mixed, mixed.player.hand[0])).toBe(2)
    const s = play(spells, 't0_white_fortress')
    expect(s.player.block).toBe(14)
    expect(s.player.energy).toBe(spells.player.energy) // 0Eで撃てた
  })

  it('白光の矢: 5ダメ+威圧1。対象の意図が攻撃以外なら+1ドロー', () => {
    let s = fresh(['white_light_arrow'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, intent: e.intent ? { ...e.intent, kind: 'defend' as const } : e.intent })) }
    const hand0 = s.player.hand.length
    s = play(s, 't0_white_light_arrow')
    expect(s.player.hand.length).toBe(hand0 - 1 + 1) // 1枚撃って1枚引いた
    expect(s.enemies[0].weak).toBe(1)
    let t = fresh(['white_light_arrow'])
    t = { ...t, enemies: t.enemies.map((e) => ({ ...e, intent: e.intent ? { ...e.intent, kind: 'attack' as const } : e.intent })) }
    t = play(t, 't0_white_light_arrow')
    expect(t.player.hand.length).toBe(hand0 - 1)
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
    let s = energy(fresh(['white_perm_band', 'white_page_rank']), 5)
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
    let s = energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_perm_band', 'white_miracle_division']), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    s = play(s, 't2_white_perm_band')
    const hand0 = s.player.hand.length
    s = play(s, 't3_white_miracle_division')
    expect(s.player.permanents.length).toBe(6)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_band').length).toBe(2)
    // 軍楽隊の登場誘発: 少年の複製で1 (軍楽隊1体)・乙女の複製で1・軍楽隊の複製で2 (自身も含め2体) = 4ドロー
    expect(s.player.hand.length).toBe(hand0 - 1 + 4)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'white_miracle_division')).toBe(true)
  })

  it('分列の奇跡: 複製同士は互いの登場に反応しない (2026-09-06 裁定。軍楽長が先頭にいても線形)', () => {
    let s = energy(fresh(['white_perm_bandleader', 'white_perm_squire', 'white_perm_shieldmaiden', 'white_miracle_division']), 9)
    s = play(s, 't0_white_perm_bandleader')
    s = play(s, 't1_white_perm_squire')
    s = play(s, 't2_white_perm_shieldmaiden')
    const b0 = s.player.block
    s = play(s, 't3_white_miracle_division')
    // 軍楽長の複製: 元の軍楽長+2・自分自身+2 ／ 少年の複製: 元+2 (複製の軍楽長は反応しない) ／ 乙女の複製: 元+2 = 8
    expect(s.player.block - b0).toBe(8)
  })

  it('殉教の誓い: 従者0ならプレイ不可。選んだ従者だけ消え、この置物がある間 従者+2', () => {
    const none = energy(fresh(['white_perm_martyr_vow']), 3)
    expect(retainerRequirementMet(none, none.player.hand[0])).toBe(false)
    expect(() => play(none, 't0_white_perm_martyr_vow', { permanentUid: 'x' })).toThrow('従者が1体以上')
    let s = energy(fresh(['white_perm_squire', 'white_page_rank', 'white_perm_martyr_vow', 'white_march_order']), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_page_rank')
    const page = s.player.permanents.find((p) => p.def.id === 'white_perm_page')!
    expect(() => play(s, 't2_white_perm_martyr_vow')).toThrow('permanentUid')
    s = play(s, 't2_white_perm_martyr_vow', { permanentUid: page.uid })
    expect(s.player.permanents.some((p) => p.uid === page.uid)).toBe(false)
    expect(s.player.permanents.filter((p) => p.def.id === 'white_perm_page').length).toBe(1)
    expect(s.eventLog.some((e) => e.type === 'RetainerSacrificed')).toBe(true)
    // 進軍の号令: 従者のターン開始効果を今すぐ (少年2+2・見習い1+2 = 7)。誓い自身 (置物) は従者でないので鳴らない
    const hp0 = s.enemies[0].hp
    s = play(s, 't3_white_march_order')
    expect(hp0 - s.enemies[0].hp).toBe(4 + 3)
    expect(s.eventLog.some((e) => e.type === 'RetainersTriggered' && e.count === 2)).toBe(true)
  })

  it('進軍の号令: 従者だけを再誘発 (盾の乙女=ブロック・少年=ダメ)。innate 置物は対象外。従者0では空撃ちできない (Opusラン W)', () => {
    const none = energy(fresh(['white_march_order']), 9)
    expect(retainerRequirementMet(none, none.player.hand[0])).toBe(false)
    expect(() => play(none, 't0_white_march_order')).toThrow('従者が1体以上')
    let s = energy(fresh(['white_perm_squire', 'white_perm_shieldmaiden', 'white_march_order']), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    const hp0 = s.enemies[0].hp
    const b0 = s.player.block
    s = play(s, 't2_white_march_order')
    expect(hp0 - s.enemies[0].hp).toBe(2)
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

  it('見習いの列 (召喚2体) は2回駆けつける = 1+1ダメ。軍楽隊 (登場誘発のみ・ターン開始効果なし) は動かない', () => {
    let s = withHand(hinata(), ['white_page_rank', 'white_perm_band'])
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_page_rank')
    expect(hp0 - s.enemies[0].hp).toBe(2)
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(2)
    s = play(s, 't1_white_perm_band')
    expect(s.eventLog.filter((e) => e.type === 'RetainerRushed').length).toBe(2)
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
    expect(getCardDef('white_march_order').requiresRetainer).toBe(true)
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
    // 盾の乙女を出して (駆けつけ無し=ひなた不在) 進軍の号令で今すぐ動かす: 置物由来のブロックは怒らない
    let t = withHand(start(), ['white_perm_shieldmaiden', 'white_march_order'])
    const str1 = t.enemies[0].strength
    t = play(t, 't0_white_perm_shieldmaiden')
    t = play(t, 't1_white_march_order')
    expect(t.player.block).toBe(2)
    expect(t.enemies[0].strength).toBe(str1)
    // 白盾 (1効果) は従来どおり+1
    let u = withHand(start(), ['white_guard'])
    const str2 = u.enemies[0].strength
    u = play(u, 't0_white_guard')
    expect(u.enemies[0].strength).toBe(str2 + 1)
  })
})

