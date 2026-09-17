// ギア (消耗品 2026-09-17。CLAUDE.md「部品（消耗品）」/ docs/parts-proposal-2026-09-17.md) の機械固定。
// 骨格: 拾って持ち歩き (10個)・自ターンに1個だけ「魔素」1で組む・幕で数値は伸びない。
// 台帳33種／レア度 C13・U14・R6／数値は本家の瓶並み、は裁定なのでここで固定する。
import { describe, expect, it } from 'vitest'
import { allGears, getGearDef } from './content.ts'
import {
  GEAR_CARRY_MAX,
  MANA_MAX,
  gearBlockedReason,
  gearCardChoices,
  makeGear,
  resolveGear,
} from './gears.ts'
import {
  GEAR_DROP_BASE,
  MANA_PER_ELITE_BOSS,
  MANA_PER_WIN,
  SHOP_GEAR_PRICE,
  SHOP_GEAR_SLOTS,
  SHOP_MANA_PRICE,
  applyRunCommand,
  createRun,
  gearsOf,
  manaOf,
  openShop,
} from './run.ts'
import type { RunState } from './run.ts'
import { applyCommand } from './state.ts'
import { attackIntent, createRunInBattle, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState, GearInstance } from './types.ts'

const gear = (id: string): GearInstance => makeGear(id, `t_${id}`)
/** 戦闘状態に持ち物を持たせたランを作る (戦闘の中身だけを見たい時の足場) */
function runWith(combat: GameState, gears: readonly string[], mana = 5): RunState {
  const run = createRun(777, 'set-confirm', 'leader_green')
  return { ...run, phase: 'combat', combat, gears: gears.map((id) => gear(id)), mana, seenGearIds: [...gears] }
}
const use = (run: RunState, index: number, extra: Record<string, unknown> = {}): RunState =>
  applyRunCommand(run, { type: 'UseGear', index, ...extra } as never)

describe('台帳 (裁定 2026-09-17: 33種・C13/U14/R6・数値は本家の瓶並み)', () => {
  it('33種で、id と名前が一意', () => {
    expect(allGears.length).toBe(33)
    expect(new Set(allGears.map((g) => g.id)).size).toBe(33)
    expect(new Set(allGears.map((g) => g.name)).size).toBe(33)
  })

  it('レア度の内訳は C13 / U14 / R6', () => {
    const by = (r: string) => allGears.filter((g) => g.rarity === r).length
    expect([by('common'), by('uncommon'), by('rare')]).toEqual([13, 14, 6])
  })

  it('回数つき (杖) は発条・歯車の2種だけ (裁定「杖は絞る」)', () => {
    expect(allGears.filter((g) => (g.charges ?? 1) > 1).map((g) => g.id)).toEqual(['gear_spring', 'gear_cog'])
  })

  it('全てのギアが説明文を持ち、special でないものは効果を持つ', () => {
    for (const g of allGears) {
      expect(g.text.length).toBeGreaterThan(0)
      if (g.special === undefined) expect(g.effects.length).toBeGreaterThan(0)
    }
  })

  it('幕でスケールしない (量は固定。幕の印を持つ欄が無い)', () => {
    for (const g of allGears) expect(Object.keys(g)).not.toContain('actScale')
  })
})

describe('使う条件: 自ターンに1個・魔素1・敵ターンには使えない', () => {
  it('魔素が無ければ組めない', () => {
    const s = freshCombat('set-confirm', 'enemy_probe')
    expect(gearBlockedReason(s, 0, gear('gear_spring'))).toBe('魔素がない')
    expect(gearBlockedReason(s, 1, gear('gear_spring'))).toBeNull()
  })

  it('このターンに既に組んでいたら2個目は組めない', () => {
    const s = freshCombat('set-confirm', 'enemy_probe')
    expect(gearBlockedReason({ ...s, gearUsedThisTurn: true }, 5, gear('gear_spring'))).toBe('このターンはもう組んだ')
  })

  it('敵の番には使えない (2026-09-17 裁定。後出しは罠の専売)', () => {
    const s = freshCombat('set-confirm', 'enemy_probe')
    expect(gearBlockedReason({ ...s, enemyPhase: true }, 5, gear('gear_spring'))).toBe('敵の番には使えない')
  })

  it('組むと魔素が1減り、1ターン1個の旗が立つ', () => {
    const run = runWith(freshCombat('set-confirm', 'enemy_probe'), ['gear_spring'], 3)
    const after = use(run, 0)
    expect(manaOf(after)).toBe(2)
    expect(after.combat!.gearUsedThisTurn).toBe(true)
    expect(() => use(after, 0)).toThrow(/このターンはもう組んだ/)
  })

  it('次の自ターンになれば また1個組める', () => {
    let run = runWith(freshCombat('set-confirm', 'enemy_probe'), ['gear_spring'], 5)
    run = use(run, 0)
    const nextTurn = applyCommand(withIntent(run.combat!, attackIntent(1)), { type: 'EndTurn' })
    expect(nextTurn.gearUsedThisTurn).toBeUndefined()
  })
})

describe('回数つき (杖): 使い切ると壊れる', () => {
  it('発条は2回使えて、2回目で持ち物から消える', () => {
    let run = runWith(freshCombat('set-confirm', 'enemy_probe'), ['gear_spring'], 5)
    run = use(run, 0)
    expect(gearsOf(run)[0].charges).toBe(1)
    expect(run.combat!.player.block).toBe(10)
    // 次のターンへ
    run = { ...run, combat: { ...run.combat!, gearUsedThisTurn: false } }
    run = use(run, 0)
    expect(gearsOf(run).length).toBe(0)
  })
})

describe('効果 (代表)', () => {
  it('火薬: 敵全体に10 (成長は乗る = 与ダメ全ての既存則)', () => {
    const run = runWith(freshCombat('set-confirm', 'enc_probe_pair'), ['gear_powder'], 5)
    const before = run.combat!.enemies.map((e) => e.hp)
    const after = use(run, 0)
    after.combat!.enemies.forEach((e, i) => expect(before[i] - e.hp).toBe(10))
  })

  it('楔: 対象の宣言済みの行動が打ち消される', () => {
    const base = withIntent(freshCombat('set-confirm', 'enemy_probe'), attackIntent(9))
    const run = runWith(base, ['gear_wedge'], 5)
    const after = use(run, 0, { targetIndex: 0 })
    expect(after.combat!.enemies[0].actionNegated).toBe(true)
    const resolved = applyCommand(after.combat!, { type: 'EndTurn' })
    expect(resolved.eventLog.some((e) => e.type === 'ActionNegated')).toBe(true)
    expect(resolved.player.hp).toBe(base.player.hp)
  })

  it('清めの水: 自分の状態異常が全て消える', () => {
    const base = freshCombat('set-confirm', 'enemy_probe')
    const sick: GameState = { ...base, player: { ...base.player, weak: 3, vulnerable: 2, frail: 1, restrain: 2, mist: 1, slow: 1 } }
    const after = use(runWith(sick, ['gear_cleansing_water'], 5), 0)
    const p = after.combat!.player
    expect([p.weak, p.vulnerable, p.frail, p.restrain, p.mist ?? 0, p.slow ?? 0]).toEqual([0, 0, 0, 0, 0, 0])
  })

  it('灰落とし: 手札の負傷・がらくたが消滅置き場へ (黒の燃料になる)', () => {
    const base = withHand(freshCombat('set-confirm', 'enemy_probe'), ['status_wound', 'status_junk', 'green_strike'])
    const after = use(runWith(base, ['gear_ash_remover'], 5), 0)
    expect(after.combat!.player.hand.map((c) => c.def.id)).toEqual(['green_strike'])
    expect(after.combat!.player.exhaustPile.length).toBe(2)
  })

  it('蘇りの発条: 致死を一度だけ耐えて HP1 で立つ', () => {
    const base = freshCombat('set-confirm', 'enemy_probe')
    const low: GameState = { ...base, player: { ...base.player, hp: 3 } }
    const after = use(runWith(low, ['gear_revive_spring'], 5), 0)
    expect(after.combat!.gearDeathSave).toBe(true)
    const hit = applyCommand(withIntent(after.combat!, attackIntent(50)), { type: 'EndTurn' })
    expect(hit.player.hp).toBe(1)
    expect(hit.phase).not.toBe('lost')
    expect(hit.gearDeathSave).toBe(false)
  })

  it('締め紐: 対象の次の行動が隙になる / 時の歯車は全員', () => {
    const one = use(runWith(freshCombat('set-confirm', 'enc_probe_pair'), ['gear_tie_cord'], 5), 0, { targetIndex: 1 })
    expect(one.combat!.enemies.map((e) => e.staggeredNext === true)).toEqual([false, true])
    const all = use(runWith(freshCombat('set-confirm', 'enc_probe_pair'), ['gear_time_cog'], 5), 0)
    expect(all.combat!.enemies.every((e) => e.staggeredNext === true)).toBe(true)
  })

  it('砥ぎ油: 選んだ手札がこの戦闘中だけ鍛えられる', () => {
    const base = withHand(freshCombat('set-confirm', 'enemy_probe'), ['green_strike'])
    const choices = gearCardChoices(base, getGearDef('gear_whetstone_oil'))
    expect(choices.length).toBe(1)
    const after = use(runWith(base, ['gear_whetstone_oil'], 5), 0, { cardUid: choices[0].uid })
    expect(after.combat!.player.hand[0].def.name).toContain('+')
  })

  it('掘り出し: 捨て札から選んだ1枚が手札へ', () => {
    const base = freshCombat('set-confirm', 'enemy_probe')
    const moved: GameState = { ...base, player: { ...base.player, discardPile: [base.player.drawPile[0]], drawPile: base.player.drawPile.slice(1) } }
    const target = moved.player.discardPile[0]
    const after = use(runWith(moved, ['gear_dig_out'], 5), 0, { cardUid: target.uid })
    expect(after.combat!.player.hand.some((c) => c.uid === target.uid)).toBe(true)
    expect(after.combat!.player.discardPile.length).toBe(0)
  })

  it('過負荷の歯車: 一時マナ+3 と引き換えに次のターンのドローが2枚減る (両刃)', () => {
    const run = runWith(freshCombat('set-confirm', 'enemy_probe'), ['gear_overload_cog'], 5)
    const after = use(run, 0)
    expect(after.combat!.player.energy).toBe(run.combat!.player.energy + 3)
    expect(after.combat!.nextTurnDraw).toBe(-2)
  })

  it('無銘の部品: 拾ったことのあるギアにだけ化ける', () => {
    const base = freshCombat('set-confirm', 'enemy_probe')
    const run = { ...runWith(base, ['gear_nameless'], 5), seenGearIds: ['gear_nameless', 'gear_spring'] }
    expect(() => use(run, 0, { asGearId: 'gear_powder' })).toThrow(/拾ったことのない/)
    const after = use(run, 0, { asGearId: 'gear_spring' })
    expect(after.combat!.player.block).toBe(10)
  })

  it('無銘の部品は無銘の部品には化けられない', () => {
    const base = freshCombat('set-confirm', 'enemy_probe')
    expect(() => resolveGear(base, getGearDef('gear_nameless'), { asGearId: 'gear_nameless', seenGearIds: ['gear_nameless'] })).toThrow()
  })
})

describe('供給 (§4): 魔素・ドロップ・持ち歩き', () => {
  it('開始時は魔素0・持ち物0・pity は基礎値', () => {
    const run = createRun(1234, 'set-confirm', 'leader_green')
    expect(manaOf(run)).toBe(0)
    expect(gearsOf(run).length).toBe(0)
    expect(run.gearPity).toBe(GEAR_DROP_BASE)
  })

  it('通常戦の勝利で魔素+1 (エリート・幕ボスは+2)', () => {
    expect(MANA_PER_WIN).toBe(1)
    expect(MANA_PER_ELITE_BOSS).toBe(2)
    const run = createRunInBattle(2468, 'set-confirm', 'leader_green')
    expect(manaOf(run)).toBe(0)
    // 勝たせる: 敵のHPを0にした盤面を流し込んで決着させる
    const combat = run.combat!
    const won: GameState = { ...combat, enemies: combat.enemies.map((e) => ({ ...e, hp: 0 })) }
    const normal = applyRunCommand({ ...run, combat: won, currentElite: false }, { type: 'Combat', command: { type: 'EndTurn' } })
    expect(manaOf(normal)).toBe(MANA_PER_WIN)
    const elite = applyRunCommand({ ...run, combat: won, currentElite: true }, { type: 'Combat', command: { type: 'EndTurn' } })
    expect(manaOf(elite)).toBe(MANA_PER_ELITE_BOSS)
  })

  it('魔素は上限10で止まる', () => {
    const run = { ...createRun(99, 'set-confirm', 'leader_green'), mana: MANA_MAX }
    expect(manaOf(run)).toBe(MANA_MAX)
    expect(MANA_MAX).toBe(10)
  })

  it('持ち歩きは10個まで。満杯で取るには入れ替えるギアを選ぶ', () => {
    const full = Array.from({ length: GEAR_CARRY_MAX }, (_, i) => makeGear('gear_spring', `g${i}`))
    const run: RunState = { ...createRun(31, 'set-confirm', 'leader_green'), phase: 'reward', rewardOptions: [], gears: full, gearOption: 'gear_powder' }
    expect(() => applyRunCommand(run, { type: 'TakeGear' })).toThrow(/満杯/)
    const swapped = applyRunCommand(run, { type: 'TakeGear', discardIndex: 0 })
    expect(gearsOf(swapped).length).toBe(GEAR_CARRY_MAX)
    expect(gearsOf(swapped).some((g) => g.gearId === 'gear_powder')).toBe(true)
  })

  it('取ったギアは持ち物に入り、拾った記録 (無銘の部品の候補) にも残る', () => {
    const run: RunState = { ...createRun(32, 'set-confirm', 'leader_green'), phase: 'reward', rewardOptions: [], gearOption: 'gear_hammer' }
    const after = applyRunCommand(run, { type: 'TakeGear' })
    expect(gearsOf(after).map((g) => g.gearId)).toEqual(['gear_hammer'])
    expect(after.seenGearIds).toContain('gear_hammer')
    expect(after.gearOption).toBeNull()
  })

  it('見送るとギアの提示が消える (札のピックとは独立)', () => {
    const run: RunState = { ...createRun(33, 'set-confirm', 'leader_green'), phase: 'reward', rewardOptions: [], gearOption: 'gear_hammer' }
    const after = applyRunCommand(run, { type: 'SkipGear' })
    expect(after.gearOption).toBeNull()
    expect(gearsOf(after).length).toBe(0)
  })
})

describe('煙玉 (2026-09-17 ユーザー裁定: 幕ボス以外・エリート可・報酬なし)', () => {
  it('通常戦からは逃げられる (HPはそのまま・報酬は無し・マップへ戻る)', () => {
    const run0 = createRunInBattle(555, 'set-confirm', 'leader_green')
    const run = { ...run0, gears: [gear('gear_smoke')], mana: 3, seenGearIds: ['gear_smoke'] }
    const hp = run.combat!.player.hp
    const after = use(run, 0)
    expect(after.phase).toBe('map')
    expect(after.combat).toBeNull()
    expect(after.hp).toBe(hp)
    expect(after.battlesWon).toBe(run.battlesWon)
    expect(gearsOf(after).length).toBe(0)
    expect(manaOf(after)).toBe(2)
  })
})

describe('ショップ (3枠 + 魔素)', () => {
  it('店を開くとギアの棚 (最大3枠) と魔素の値段が並ぶ', () => {
    const base = createRun(8642, 'set-confirm', 'leader_green')
    const shop = openShop(base)
    expect(shop.phase).toBe('shop')
    const shelf = shop.shop!.gears ?? []
    expect(shelf.length).toBeGreaterThan(0)
    expect(shelf.length).toBeLessThanOrEqual(SHOP_GEAR_SLOTS)
    for (const item of shelf) expect(item.price).toBe(SHOP_GEAR_PRICE[getGearDef(item.id).rarity])
    expect(shop.shop!.manaPrice).toBe(SHOP_MANA_PRICE)
  })

  it('魔素を買うと所持金が減って魔素が1増える', () => {
    const base = createRun(8643, 'set-confirm', 'leader_green')
    const run: RunState = {
      ...base,
      phase: 'shop',
      gold: 500,
      shop: { cards: [], relicId: null, relicPrice: 0, gears: [{ id: 'gear_powder', price: 40 }], manaPrice: 30 },
    }
    const after = applyRunCommand(run, { type: 'ShopBuyMana' })
    expect(after.gold).toBe(470)
    expect(manaOf(after)).toBe(1)
    const bought = applyRunCommand(after, { type: 'ShopBuyGear', index: 0 })
    expect(bought.gold).toBe(430)
    expect(gearsOf(bought).map((g) => g.gearId)).toEqual(['gear_powder'])
    expect(() => applyRunCommand(bought, { type: 'ShopBuyGear', index: 0 })).toThrow()
  })
})
