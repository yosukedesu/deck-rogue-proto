// 人形の灯り＝寿命と火勢 (2026-09-21。人間ラン#15「毎戦同じ」への再設計。docs/white-doll-life-proposal-2026-09-21.md)
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { dollGrowth, dollLifeLeft, rallyPreview } from './effects.ts'
import { fuseCards } from './fusion.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import { upgradeCard } from './upgrade.ts'
import type { CardDef, CardInstance, GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const fresh = (ids: readonly string[], enemy = 'enemy_probe'): GameState =>
  withHand(freshCombat('set-confirm', enemy, 42, 'starter_white'), ids)
const energy = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n, energyMax: n } })
const light = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, light: n } })
/** 手番を終えて次のターンへ (探り屋は殴るだけ。HP は十分)。手札は空にして偶然のプレイを避ける */
const nextTurn = (s: GameState): GameState => applyCommand({ ...s, player: { ...s.player, hp: 999, maxHp: 999 } }, { type: 'EndTurn' })
const dolls = (s: GameState): CardInstance[] => s.player.permanents.filter((p) => p.def.retainer === true && p.innate !== true)
const inst = (id: string): CardInstance => ({ uid: `x_${id}`, def: getCardDef(id) })
const withDef = (s: GameState, def: CardDef): GameState => ({ ...s, player: { ...s.player, hand: [{ uid: `d_${def.id}`, def }, ...s.player.hand] } })
// 2026-09-24 白のプール 108→84 (docs/white-pool-trim-proposal-2026-09-23.md): 撤去した札の機構は仮の札で固定
const TWIN: CardDef = { id: 'test_twin', name: '試しの二重', cost: 1, type: 'spell', color: 'white', effects: [{ trigger: 'onPlay', effect: 'twinNextRetainer', amount: 1 }] }
const MIRROR: CardDef = { id: 'test_mirror', name: '試しの鏡', cost: 2, type: 'permanent', color: 'white', effects: [{ trigger: 'onTurnStart', effect: 'copyLastRetainer', amount: 1 }] }

describe('灯り (寿命): 点灯したターンを1と数え、最後のターンの敵フェーズが終わると消える', () => {
  it('剣の人形 (寿命4): T1 に出すと あと4→3→2→1、T4 の敵フェーズの終わりに消える (RetainerExpired)。T2〜T4 の開始に動く', () => {
    let s = energy(fresh(['white_perm_squire']), 9)
    s = play(s, 't0_white_perm_squire')
    expect(dollLifeLeft(s, dolls(s)[0])).toBe(4)
    expect(dolls(s)[0].enteredTurn).toBe(1)
    s = nextTurn(s) // T2
    expect(dollLifeLeft(s, dolls(s)[0])).toBe(3)
    s = nextTurn(s) // T3
    expect(dollLifeLeft(s, dolls(s)[0])).toBe(2)
    s = nextTurn(s) // T4
    expect(dollLifeLeft(s, dolls(s)[0])).toBe(1)
    expect(s.eventLog.filter((e) => e.type === 'RetainerExpired').length).toBe(0)
    s = nextTurn(s) // T4 の敵フェーズが終わる → 消える
    expect(dolls(s)).toHaveLength(0)
    expect(s.eventLog.filter((e) => e.type === 'RetainerExpired')).toEqual([{ type: 'RetainerExpired', cardId: 'white_perm_squire', uid: 't0_white_perm_squire' }])
    // 人形壊しの誘発 (TokenDestroyed) ではない
    expect(s.eventLog.some((e) => e.type === 'TokenDestroyed')).toBe(false)
  })

  it('段: 小さな人形3・灯コストつき (癒し) 5・篝火は期限なし (null)', () => {
    let s = light(energy(fresh(['white_perm_page', 'white_perm_choir', 'white_perm_bonfire']), 9), 9)
    s = play(s, 't0_white_perm_page')
    s = play(s, 't1_white_perm_choir')
    s = play(s, 't2_white_perm_bonfire')
    const [page, choir, bonfire] = dolls(s)
    expect(dollLifeLeft(s, page)).toBe(3)
    expect(dollLifeLeft(s, choir)).toBe(5)
    expect(dollLifeLeft(s, bonfire)).toBeNull()
    s = nextTurn(s)
    s = nextTurn(s)
    s = nextTurn(s) // T3 の終わりで小さな人形が消える
    expect(dolls(s).map((p) => p.def.id)).toEqual(['white_perm_choir', 'white_perm_bonfire'])
    for (let i = 0; i < 6; i++) s = nextTurn(s)
    expect(dolls(s).map((p) => p.def.id)).toEqual(['white_perm_bonfire']) // 篝火は消えない
  })

  it('育ち: 人形のダメージ・ブロックは点灯してからのターン数ぶん増える (T1 3 → T2 4 → T3 5)。回復は増えない。号令でも同じ値', () => {
    let s = light(energy(fresh(['white_perm_squire', 'white_perm_candle', 'white_strike', 'white_march_order']), 9), 9)
    s = play(s, 't0_white_perm_squire') // 点灯 3
    s = play(s, 't1_white_perm_candle')
    expect(dollGrowth(s, dolls(s)[0])).toBe(0)
    const hp1 = s.enemies[0].hp
    s = nextTurn(s) // T2 開始: 剣 3+1
    expect(hp1 - s.enemies[0].hp).toBe(4)
    expect(dollGrowth(s, dolls(s)[0])).toBe(1)
    s = light(energy(withHand({ ...s, player: { ...s.player, hp: 50, maxHp: 999 } }, ['white_strike', 'white_march_order']), 9), 9)
    const hp2 = s.enemies[0].hp
    s = play(s, 't0_white_strike') // 灯火の一撃5 → 燭が回復1 (増えない)
    expect(hp2 - s.enemies[0].hp).toBe(5)
    expect(s.player.hp).toBe(51)
    const hp3 = s.enemies[0].hp
    s = play(s, 't1_white_march_order') // 小さな人形 (点灯 2) + 号令: 剣 3+1・小さな人形 2・燭は回復1
    expect(hp3 - s.enemies[0].hp).toBe(2 + 4 + 2)
    expect(rallyPreview(s, 0).damage).toBe(4 + 2)
  })

  it('コピー (写し灯) は残りの灯りを写す: T2 に T1 の人形を写すと あと3 で出て、元と同じターンに消える。点灯で1回動く (火勢込み)', () => {
    let s = energy(fresh(['white_perm_squire', 'white_copy_light']), 9)
    s = play(s, 't0_white_perm_squire')
    s = energy(withHand(nextTurn(s), ['white_copy_light']), 9)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_copy_light', { permanentUid: 't0_white_perm_squire' })
    expect(dolls(s)).toHaveLength(2)
    expect(dolls(s)[1].enteredTurn).toBe(1)
    expect(dollLifeLeft(s, dolls(s)[1])).toBe(3)
    expect(hp0 - s.enemies[0].hp).toBe(4) // 点灯: 3+火勢1
    expect(s.eventLog.some((e) => e.type === 'RetainerCopied' && e.fromUid === 't0_white_perm_squire')).toBe(true)
    s = nextTurn(s)
    s = nextTurn(s)
    s = nextTurn(s)
    expect(dolls(s)).toHaveLength(0)
  })

  it('写し灯・継ぎ火・永遠の灯は人形の指定 (permanentUid) が要り、人形0では撃てない (requiresRetainer)', () => {
    const s = light(energy(fresh(['white_copy_light', 'white_relight', 'white_eternal_light']), 9), 9)
    expect(() => play(s, 't0_white_copy_light')).toThrow()
    const t = play(energy(withHand(s, ['white_perm_squire', 'white_copy_light']), 9), 't0_white_perm_squire')
    expect(() => play(t, 't1_white_copy_light')).toThrow(/permanentUid/)
    expect(() => play(t, 't1_white_copy_light', { permanentUid: 'nope' })).toThrow()
    for (const id of ['white_copy_light', 'white_relight', 'white_eternal_light']) expect(getCardDef(id).requiresRetainer).toBe(true)
  })

  it('継ぎ火 (1E・灯2・2026-09-24 ユーザー案): 場の人形すべての期限を2ターン延ばす。永遠の灯 (1E・灯2・消滅): 尽きなくなる。期限なしの人形に継ぎ火は何も起きない', () => {
    let s = light(energy(fresh(['white_perm_squire', 'white_relight', 'white_eternal_light', 'white_relight']), 9), 9)
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_relight')
    expect(dollLifeLeft(s, dolls(s)[0])).toBe(6)
    expect(s.player.light).toBe(7)
    expect(s.eventLog.some((e) => e.type === 'RetainerLifeExtended' && e.amount === 2)).toBe(true)
    s = play(s, 't2_white_eternal_light', { permanentUid: 't0_white_perm_squire' })
    expect(dollLifeLeft(s, dolls(s)[0])).toBeNull()
    expect(s.player.light).toBe(5)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'white_eternal_light')).toBe(true)
    s = play(s, 't3_white_relight')
    expect(dolls(s)[0].lifeBonus).toBe(2) // 変わらない (期限なし)
    for (let i = 0; i < 8; i++) s = nextTurn(s)
    expect(dolls(s)).toHaveLength(1)
    expect(dollGrowth(s, dolls(s)[0])).toBe(8) // 火勢は続く
  })

  it('二重の点灯 (機構 twinNextRetainer。札は 2026-09-24 に撤去): 次に出す人形が2体になる (ターンをまたいで持ち越す)。片割れは同じ残り寿命で点灯し、もう倍にはならない', () => {
    let s = energy(withDef(fresh(['white_perm_squire', 'white_perm_squire']), TWIN), 9)
    s = play(s, 'd_test_twin')
    expect(s.nextRetainerTwin).toBe(1)
    s = energy(withHand(nextTurn(s), ['white_perm_squire', 'white_perm_squire']), 9)
    const hp0 = s.enemies[0].hp
    s = play(s, 't0_white_perm_squire')
    expect(dolls(s)).toHaveLength(2)
    expect(s.nextRetainerTwin).toBe(0)
    expect(hp0 - s.enemies[0].hp).toBe(3 + 3) // 両方が点灯
    expect(dollLifeLeft(s, dolls(s)[1])).toBe(4)
    s = play(s, 't1_white_perm_squire')
    expect(dolls(s)).toHaveLength(3) // 2枚目は1体
  })

  it('鏡の灯籠の機構 copyLastRetainer (札は 2026-09-24 に撤去。置物・人形ではない): 毎ターン開始時に最後に点灯した人形を1体コピー。号令・アンセム・人形壊しの対象にならない', () => {
    let s = energy(withDef(fresh(['white_perm_squire', 'white_perm_shieldmaiden']), MIRROR), 9)
    s = play(s, 'd_test_mirror')
    expect(MIRROR.retainer).toBeUndefined()
    s = play(s, 't0_white_perm_squire')
    s = play(s, 't1_white_perm_shieldmaiden')
    s = nextTurn(s) // T2: 盾の人形 (最後に点灯) のコピー
    expect(dolls(s).map((p) => p.def.id)).toEqual(['white_perm_squire', 'white_perm_shieldmaiden', 'white_perm_shieldmaiden'])
    expect(dolls(s)[2].enteredTurn).toBe(1) // 残り寿命を写す
    expect(rallyPreview(s, 0).count).toBe(3) // 鏡の灯籠は数えない
  })

  it('人形の分列 (既存R) も残り寿命を写す', () => {
    let s = energy(fresh(['white_perm_squire', 'white_miracle_division']), 9)
    s = play(s, 't0_white_perm_squire')
    s = energy(withHand(nextTurn(s), ['white_miracle_division']), 9)
    s = play(s, 't0_white_miracle_division')
    expect(dolls(s).map((p) => dollLifeLeft(s, p))).toEqual([3, 3])
  })

  it('出力: 剣3・盾3・小さな人形2・一斉点灯3体 (大点灯は 2026-09-24 に撤去)。灯コストつきの人形と篝火は据え置き', () => {
    expect(getCardDef('white_perm_squire').effects[0].amount).toBe(3)
    expect(getCardDef('white_perm_shieldmaiden').effects[0].amount).toBe(3)
    expect(getCardDef('white_perm_page').effects[0].amount).toBe(2)
    expect(getCardDef('white_muster').effects[0].amount).toBe(3)
    expect(getCardDef('white_perm_hound').effects[0].amount).toBe(2)
    expect(getCardDef('white_perm_bonfire').effects[0].amount).toBe(3)
  })

  it('鍛え: 写し灯+=2体・継ぎ火+=灯1・永遠の灯+=灯1 (二重の点灯・鏡の灯籠は 2026-09-24 に撤去)', () => {
    const up = (id: string) => upgradeCard(inst(id)).def
    expect(up('white_copy_light').effects[0].amount).toBe(2)
    expect(up('white_relight').lightCost).toBe(1)
    expect(up('white_eternal_light').lightCost).toBe(1)
  })

  it('合成: 人形×人形は長い方の寿命、篝火が混ざれば期限なし。人形×道具は人形の寿命', () => {
    const a = fuseCards(inst('white_perm_squire'), inst('white_perm_choir'))
    expect(a?.life).toBe(5)
    const b = fuseCards(inst('white_perm_squire'), inst('white_perm_bonfire'))
    expect(b?.lifePersist).toBe(true)
    const c = fuseCards(inst('white_perm_squire'), inst('white_perm_chalice'))
    expect(c?.retainer).toBe(true)
    expect(c?.life).toBe(4)
  })

  it('白は 85種 (報酬76): 残った新規3枚は報酬プール', () => {
    const white = allCards.filter((c) => c.color === 'white')
    expect(white.length).toBe(85) // 2026-09-24 プールを削る −24 (108→84)・同日 CSV の裁定で −7・灯の薪 +1・Opus ひなた裁定で重ねる灯 −1
    for (const id of ['white_copy_light', 'white_relight', 'white_eternal_light']) {
      expect(white.some((c) => c.id === id), id).toBe(true)
    }
  })
})

describe('鍛え・是正 (2026-09-21 Opus A/B/C)', () => {
  it('人形の鍛えは「登場時 灯+1」より量 (ダメージ・ブロック) を先に伸ばす (B: 工房産の祭壇が灯だけ伸びた)', () => {
    const def = { ...getCardDef('white_perm_squire'), id: 'fused_test_doll', name: '試しの祭壇', effects: [
      { trigger: 'onTurnStart', effect: 'dealDamage', amount: 2, target: 'all' },
      { trigger: 'onPlay', effect: 'addLight', amount: 1 },
    ] } as typeof getCardDef extends (id: string) => infer R ? R : never
    const up = upgradeCard({ uid: 'x', def }).def
    expect(up.effects[0].amount).toBe(3)
    expect(up.effects[1].amount).toBe(1)
    // 灯芯の人形 (灯+1 だけ) は従来どおり単位+1
    expect(upgradeCard(inst('white_perm_wick')).def.effects[0].amount).toBe(2)
  })
})
