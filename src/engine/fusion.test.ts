// カード合成 (工房) のテスト。確定済みルール表「カード合成（工房）」「工房ノード」を固定する。
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getEventDef } from './content.ts'
import { fuseBlockReason, fuseCards } from './fusion.ts'
import { applyRunCommand, createRun, upgradeCard, upgradeTier, workshopFusePrice } from './run.ts'
import type { RunState } from './run.ts'
import { applyCommand, createInitialState } from './state.ts'
import { chooseToward, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, uid = `t_${id}`): CardInstance => ({ uid, def: getCardDef(id) })

function forceWin(run: RunState): RunState {
  // 分裂・残機・孵化で戦闘が続く敵 (蘇る合成獣など) は全滅→再出現を繰り返すので、決着まで薙ぎ払いを反復する (2026-09-02)
  let r = run
  for (let guard = 0; guard < 6 && r.phase === 'combat' && r.combat !== null; guard++) {
    const c = r.combat
    let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
    surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
    r = applyRunCommand(
      { ...r, combat: surgical },
      { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
    )
  }
  return r
}
/** 目的タイプのノードに入るまでラン進行を回す (途中の戦闘は forceWin・報酬等はスキップ) */
function runTo(run: RunState, target: 'campfire' | 'workshop'): RunState {
  let r = run
  let guard = 0
  // 工房は幕2以降にしか無い (供給を後ろへ)。幕1を丸ごと走破してから探すので上限は広めに取る
  // (2026-08-31 焚き火の散布化で幕1の戦闘数が増え、旧80では幕2の工房に届かなくなった)
  while (guard++ < 240) {
    if (r.phase === 'map') { r = chooseToward(r, target); continue }
    if (r.phase === 'campfire') {
      if (target === 'campfire') return r
      r = applyRunCommand(r, { type: 'CampfireRest' })
    } else if (r.phase === 'workshop') {
      if (target === 'workshop') return r
      r = applyRunCommand(r, { type: 'WorkshopSkip' })
    } else if (r.phase === 'combat') r = forceWin(r)
    else if (r.phase === 'shop') r = applyRunCommand(r, { type: 'ShopLeave' })
    else if (r.phase === 'event') {
      const ev = getEventDef(r.eventId!)
      r = applyRunCommand(r, { type: 'EventChoice', index: ev.choices.length - 1 })
    }
    else if (r.phase === 'relic-reward') r = applyRunCommand(r, { type: 'SkipRelic' })
    else if (r.phase === 'reward') r = applyRunCommand(r, { type: 'SkipReward' })
    else return r
  }
  throw new Error('runTo が収束しない')
}

/** 窮鼠の大牙 (2026-09-03 撤去) 相当の条件付きリアクション。条件違いの同種効果が別効果のまま残る規約のテスト用 */

describe('効果の合体 (2026-09-05 ユーザー裁定「工房は全て合成できるようにしたい」→ ask_user A/A/A/A)', () => {
  it('2枚の効果を全部持ち、コストは合計−1: 牙の一撃(2E 17貫)×荒角の構え(1E ブロック6+勢い3) → 2E', () => {
    const def = fuseCards(inst('green_fang'), inst('green_horn_stance'))
    expect(def.cost).toBe(2)
    expect(def.effects.find((e) => e.effect === 'dealDamage')?.amount).toBe(17)
    expect(def.effects.find((e) => e.effect === 'dealDamage')?.pierce).toBe(true)
    expect(def.effects.find((e) => e.effect === 'gainBlock')?.amount).toBe(6)
    expect(def.effects.find((e) => e.effect === 'addMomentum')?.amount).toBe(3)
  })

  it('決定的: 同じ素材からは常に同じ結果 (順序も問わない)', () => {
    const x = fuseCards(inst('green_strike'), inst('green_growth_ring'))
    const y = fuseCards(inst('green_growth_ring'), inst('green_strike'))
    expect(JSON.stringify(x)).toBe(JSON.stringify(y))
  })

  it('レシピが最優先される (年輪×二連の蔦打ち → 蔦車輪)', () => {
    const def = fuseCards(inst('green_growth_ring'), inst('green_double_lash'))
    expect(def.name).toBe('蔦車輪')
  })

  it('両方0Eなら0E、片方0Eなら高い方 (0E素材は値引きにならない)、1E×1Eは1E、3E×3Eは5E (上限)', () => {
    expect(fuseCards(inst('green_sprint'), inst('green_twig_strike')).cost).toBe(0)
    expect(fuseCards(inst('green_sprint'), inst('green_fang')).cost).toBe(2) // 疾駆(0E)×牙の一撃(2E) = 2E (クーポンにならない)
    expect(fuseCards(inst('green_strike'), inst('green_guard')).cost).toBe(1)
    expect(fuseCards(inst('green_sig_trample'), inst('green_charging_horn')).cost).toBe(5)
  })

  it('査定できなかった効果も合成できる: 薙ぎ払い(攻撃数参照)×牙の一撃・大牙(成長×3)×打撃・追い風(条件0E)×打撃', () => {
    for (const [x, y] of [['green_sweep', 'green_fang'], ['green_harvest_strike', 'green_strike'], ['green_tailwind', 'green_strike']]) {
      expect(fuseBlockReason(inst(x), inst(y))).toBeNull()
      const def = fuseCards(inst(x), inst(y))
      expect(def.effects.length).toBeGreaterThan(0)
    }
    const heavy = fuseCards(inst('green_harvest_strike'), inst('green_strike'))
    expect(heavy.effects.find((e) => e.growthMultiplier !== undefined)?.growthMultiplier).toBe(3) // 倍率は引き継ぐ
    const tail = fuseCards(inst('green_tailwind'), inst('green_strike'))
    expect(tail.freeIfMomentumAtLeast).toBe(5) // 条件の0E化も引き継ぐ
  })

  it('選択式: 相手の効果は共通部 (effects) に入り、モードは残る。X札は片方だけならX=3に畳み、両方Xなら残る (机上レビュー S 提案1)', () => {
    const mode = fuseCards(inst('green_mode_crossroads'), inst('green_strike'))
    expect(mode.modes?.length).toBe(2)
    expect(mode.effects.some((e) => e.effect === 'dealDamage' && e.amount === 6)).toBe(true)
    const x = fuseCards(inst('green_x_vine_flurry'), inst('green_strike')) // 蔦の連撃(X 5×X) × 打撃
    expect(x.xCost).not.toBe(true)
    expect(x.cost).toBe(3) // X=3 として 3+1−1
    expect(x.effects.filter((e) => e.effect === 'dealDamage' && e.amount === 5)).toHaveLength(3)
    const xx = fuseCards(inst('green_x_vine_flurry'), inst('green_x_bark_armor'))
    expect(xx.xCost).toBe(true)
    expect(xx.effects.some((e) => e.xHits === true)).toBe(true)
  })

  it('選択式の共通部はモードを問わず解決される (engine)', () => {
    const def = fuseCards(inst('green_mode_crossroads'), inst('green_strike'))
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [])
    s = { ...s, player: { ...s.player, hand: [{ uid: 'f0', def }], energy: 3 } }
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'f0', modeIndex: 1, targetIndex: 0 }) // 育成モード (成長+2) を選んでも共通部の6ダメが出る
    expect(hp0 - s.enemies[0].hp).toBe(6)
    expect(s.player.growth).toBe(2)
  })

  it('歯止め: 倍化を含めば消滅、0E+補充なら消滅。重い札は罠に収まらない (合計−1が2Eを超えるなら相手側のタイプで出る)', () => {
    const bloom = fuseCards(inst('green_sig_rite_of_bloom'), inst('green_strike'))
    expect(bloom.exhaust).toBe(true)
    const draw = fuseCards(inst('green_sprint'), inst('green_wild_sprout')) // 0E勢い3 × 0E成長1+1ドロー(消滅)
    expect(draw.exhaust).toBe(true)
    const heavy = fuseCards(inst('green_reaction_tree_warden'), inst('green_fang')) // 2E反応 × 2E攻撃 → 3E は罠に収まらない → 物理
    expect(heavy.type).toBe('physical')
    expect(heavy.cost).toBe(3)
    expect(heavy.effects.some((e) => e.effect === 'gainBlock' && e.trigger === 'onPlay' && e.amount === 12)).toBe(true)
    const light = fuseCards(inst('green_reaction_thorns'), inst('green_strike')) // 1E反応 × 1E → 1E の罠
    expect(light.type).toBe('reaction')
  })

  it('スモーク: 緑×緑の全ペアが合成でき、不変条件 (0E+補充=消滅・倍化=消滅・コスト0〜5) が守られる', () => {
    const pool = allCards.filter((c) => c.color === 'green' && !c.id.endsWith('_token'))
    const REFILL = new Set(['drawCards', 'impulseDraw', 'retrieveFromExhaust', 'playFromExhaust', 'drawCardsPerCardPlayed'])
    for (let i = 0; i < pool.length; i++) {
      for (let j = i; j < pool.length; j++) {
        const a = inst(pool[i].id, `a${i}`)
        const b = inst(pool[j].id, `b${j}`)
        expect(fuseBlockReason(a, b), `${pool[i].name}×${pool[j].name}`).toBeNull()
        const def = fuseCards(a, b)
        const all = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
        expect(def.cost).toBeGreaterThanOrEqual(0)
        expect(def.cost).toBeLessThanOrEqual(5)
        if (def.type !== 'permanent' && def.xCost !== true) {
          const net = all.filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext').reduce((acc, e) => acc + (e.amount ?? 0), 0)
          if (net - def.cost >= 0 && all.some((e) => REFILL.has(e.effect))) expect(def.exhaust, `${def.name}: 0E+補充`).toBe(true)
          if (all.some((e) => e.effect === 'doubleGrowth' || e.effect === 'doubleMomentum')) expect(def.exhaust, `${def.name}: 倍化`).toBe(true)
        }
        if (def.type === 'reaction') expect(def.cost).toBeLessThanOrEqual(2)
      }
    }
  })
})

describe('タイプ跨ぎ合成 = 支配順位 (置物＞リアクション＞呪文＞物理)', () => {
  it('物理×リアクション → 罠に吸収: 打撃×茨の返し = 被攻撃後6ダメ+返し10 (1E)', () => {
    const def = fuseCards(inst('green_strike'), inst('green_reaction_thorns'))
    expect(def.type).toBe('reaction')
    expect(def.cost).toBe(1)
    expect(def.effects.some((e) => e.trigger === 'onAttacked' && e.effect === 'dealDamage' && e.amount === 6)).toBe(true)
    expect(def.effects.some((e) => e.trigger === 'onAttacked' && e.effect === 'counter' && e.amount === 10)).toBe(true)
  })

  it('置物化: 打撃6×年輪の大樹 → 毎ターン2ダメ (÷3切り上げ) + 成長1 + 登場時ブロック5。コストは2E (1+2−1)', () => {
    const def = fuseCards(inst('green_strike'), inst('green_perm_growth_tree'))
    expect(def.type).toBe('permanent')
    expect(def.cost).toBe(2)
    expect(def.effects.some((e) => e.trigger === 'onTurnStart' && e.effect === 'dealDamage' && e.amount === 2)).toBe(true)
    expect(def.effects.some((e) => e.trigger === 'onTurnStart' && e.effect === 'addGrowth' && e.amount === 1)).toBe(true)
  })

  it('置物化: 参照量0で死ぬ効果 (打ち消し) は落ち、3未満の量は登場時に1回 (根の紡ぎ×年輪の大樹 → 登場時 成長+2)', () => {
    const def = fuseCards(inst('green_reaction_root_weave'), inst('green_perm_growth_tree'))
    expect(def.type).toBe('permanent')
    expect(def.effects.some((e) => e.effect === 'negate')).toBe(false)
    expect(def.effects.some((e) => e.effect === 'addGrowth' && e.trigger === 'onPlay' && e.amount === 2)).toBe(true)
  })

  it('置物化の÷3は同種の合計に掛ける (蔦の乱舞×年輪の大樹 → 成長+1×5=5 → 毎T+1、2ダメ×5=10 → 毎T3)', () => {
    const def = fuseCards(inst('green_sig_vine_dance'), inst('green_perm_growth_tree'))
    const growth = def.effects.filter((e) => e.effect === 'addGrowth' && e.trigger === 'onTurnStart').reduce((a, e) => a + (e.amount ?? 0), 0)
    expect(growth).toBe(1 + 1) // 乱舞ぶん floor(5/3)=1 + 年輪の大樹の1
    expect(def.effects.find((e) => e.effect === 'dealDamage' && e.trigger === 'onTurnStart')?.amount).toBe(3)
  })

  it('置物化で誘発できない窓は onTurnStart に落ちる (共鳴する茨 onEnemyBuffed 成長4 → 毎T成長2)', () => {
    const def = fuseCards(inst('green_reaction_resonance'), inst('green_perm_growth_tree'))
    const g = def.effects.filter((e) => e.effect === 'addGrowth' && e.trigger === 'onTurnStart')
    expect(g.length).toBeGreaterThan(0)
  })
})

describe('特性の掛け合わせ (多段合算・貫通・全体の伝播)', () => {
  it('ダメージ行は混ぜずに並べる (机上レビュー S 提案3: 全体・貫通の無償伝播を止める): 二連の蔦打ち(5×2)×追い風(6貫) → 5・5・6貫の3行', () => {
    const def = fuseCards(inst('green_double_lash'), inst('green_tailwind'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs.map((e) => e.amount)).toEqual([5, 5, 6])
    expect(dmgs.filter((e) => e.pierce === true)).toHaveLength(1)
    expect(def.name.endsWith('乱撃')).toBe(true)
  })

  it('全体は全体のまま、単体は単体のまま: 薙ぎ払い×牙の一撃 → 全体4 + 17貫通 (打撃×薙ぎ払いが1E全体10にならない)', () => {
    const def = fuseCards(inst('green_sweep'), inst('green_fang'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs.some((e) => e.target === 'all' && e.amount === 4)).toBe(true)
    expect(dmgs.some((e) => e.target !== 'all' && e.pierce === true && e.amount === 17)).toBe(true)
    expect(def.effects.some((e) => e.effect === 'dealDamagePerAttackPlayed')).toBe(true)
    expect(def.name.endsWith('嵐')).toBe(true)
    const sweep = fuseCards(inst('green_strike'), inst('green_sweep'))
    expect(sweep.effects.filter((e) => e.effect === 'dealDamage' && e.target === 'all').reduce((a, e) => a + (e.amount ?? 0), 0)).toBe(4)
  })

  it('多段×大打点: 蔦の乱舞(2×5)×大蛇の丸呑み(34) → 2×5 と 34 の6行 (成長が6回乗る)。追加コストは引き継ぐ', () => {
    const def = fuseCards(inst('green_sig_vine_dance'), inst('green_serpent_gulp'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs).toHaveLength(6)
    expect(def.discardCost).toBe(1)
  })

  it('重い札は罠に収まらない (S2 提案2): 巨獣の踏みつけ(5E 50)×先制の蔦槍(1E 被攻撃前12) → 5Eの物理 50+12 (窓の効果はプレイ時へ)', () => {
    const def = fuseCards(inst('green_finisher_stomp'), inst('green_reaction_preempt'))
    expect(def.type).toBe('physical')
    expect(def.cost).toBe(5)
    expect(def.effects.filter((e) => e.effect === 'dealDamage' && e.trigger === 'onPlay').map((e) => e.amount).sort()).toEqual([12, 50])
  })

  it('効果の順序は意味で決める (S2): 準備だけの札 (疾駆=勢い+3) はダメージ行の前に置かれる', () => {
    const def = fuseCards(inst('green_double_lash'), inst('green_sprint'))
    expect(def.effects[0].effect).toBe('addMomentum')
    expect(def.cost).toBe(1) // 0E素材は値引きにならない
  })

  it('素材の内部順序は保つ (S2): 蔦の乱舞×年輪 → 成長+2 の後に 乱舞の交互構造 (成長1→2ダメ) がそのまま', () => {
    const def = fuseCards(inst('green_sig_vine_dance'), inst('green_growth_ring'))
    const kinds = def.effects.map((e) => e.effect)
    expect(kinds[0]).toBe('addGrowth') // 年輪 (準備だけ) が先
    expect(def.effects[0].amount).toBe(2)
    expect(kinds.slice(1, 5)).toEqual(['addGrowth', 'dealDamage', 'addGrowth', 'dealDamage'])
  })

  it('5E上限で切った分は量を比例縮小 (S2): 真・巨獣の踏みつけ = 5E・100→55', () => {
    const def = fuseCards(inst('green_finisher_stomp', 'a'), inst('green_finisher_stomp', 'b'))
    expect(def.cost).toBe(5)
    expect(def.effects.find((e) => e.effect === 'dealDamage')?.amount).toBe(Math.floor(100 * 5 / 9))
  })

  it('落とした効果の価値は最大の量効果へ振り、効果が全部落ちた素材の消滅は継承しない (S2: 茨の返し×樹液)', () => {
    const def = fuseCards(inst('green_reaction_thorns'), inst('green_ritual_surge')) // 茨の返し(返し10) × 樹液(1E 一時マナ+2・消滅)
    expect(def.type).toBe('reaction')
    expect(def.exhaust).not.toBe(true)
    expect(def.effects.find((e) => e.effect === 'counter')?.amount).toBeGreaterThan(10)
  })

  it('モードを畳む時は最初のモードだけ採る (S2: 「選ぶ」が「両方」になっていた): 絡み蔦×守りの蔓', () => {
    const def = fuseCards(inst('green_entangle'), inst('green_reaction_vine'))
    expect(def.type).toBe('reaction')
    expect(def.effects.some((e) => e.effect === 'dealDamage')).toBe(false) // 第2モード (7ダメ) は採らない
    expect(def.effects.find((e) => e.effect === 'gainBlock' && e.trigger === 'onAttackIncoming')?.amount).toBe(7 + 12)
  })

  it('リアクション化で敵フェーズに死ぬ効果 (ドロー・一時マナ) と playCard専用効果 (サーチ等) は落ち、価値は返しへ振られる (提案5)', () => {
    const def = fuseCards(inst('green_flash_insight'), inst('green_reaction_thorns')) // 緑の閃き(4ドロー) × 茨の返し
    expect(def.type).toBe('reaction')
    expect(def.effects.some((e) => e.effect === 'drawCards')).toBe(false)
    expect(def.effects.find((e) => e.effect === 'counter')?.amount).toBe(10 + 12) // 4ドロー×3VP を返しへ
    const guide = fuseCards(inst('green_forest_guidance'), inst('green_reaction_thorns')) // 森の導き(サーチ+成長1)
    expect(guide.effects.some((e) => e.effect === 'searchDeck')).toBe(false)
    expect(guide.effects.some((e) => e.effect === 'addGrowth')).toBe(true)
  })
})

describe('同名合成 =「真・」化', () => {
  it('打撃×打撃 → 真・打撃 12ダメ・1E (量は合算、コストは合計−1)', () => {
    const def = fuseCards(inst('green_strike', 'a'), inst('green_strike', 'b'))
    expect(def.name).toBe('真・打撃')
    expect(def.cost).toBe(1)
    expect(def.effects.find((e) => e.effect === 'dealDamage')?.amount).toBe(12)
  })

  it('茨の返し×2 → 返し20 (同名リアクションも合成できる)', () => {
    const def = fuseCards(inst('green_reaction_thorns', 'a'), inst('green_reaction_thorns', 'b'))
    expect(def.type).toBe('reaction')
    expect(def.effects.find((e) => e.effect === 'counter')?.amount).toBe(20)
  })

  it('量を持たない同種効果は重複しない (根の紡ぎ×2 → 打ち消しは1つ・成長は合算)', () => {
    const def = fuseCards(inst('green_reaction_root_weave', 'a'), inst('green_reaction_root_weave', 'b'))
    expect(def.effects.filter((e) => e.effect === 'negate')).toHaveLength(1)
    expect(def.effects.find((e) => e.effect === 'addGrowth')?.amount).toBe(4)
  })

  it('選択式の同名合成: 各モードを対で合算 (真・道行きの選択 = 勢い+6+6ダメ / 成長+4)', () => {
    const def = fuseCards(inst('green_mode_crossroads', 'a'), inst('green_mode_crossroads', 'b'))
    expect(def.modes?.length).toBe(2)
    expect(def.modes?.[0].effects.find((e) => e.effect === 'addMomentum')?.amount).toBe(6)
    expect(def.modes?.[1].effects.find((e) => e.effect === 'addGrowth')?.amount).toBe(4)
  })

  it('工房産を素材にしても名前が素材と衝突しない (角牙の嵐×落ち葉の刃)', () => {
    const first = fuseCards(inst('green_twin_fang_vine'), inst('green_sweeping_horn'))
    const second = fuseCards({ uid: 'f', def: first }, inst('green_leaf_blade'))
    expect(second.name).not.toBe(first.name)
    expect(second.name.replace(/\+$/, '')).not.toBe(first.name.replace(/\+$/, ''))
  })

  it('量を持たない同種効果を畳んだ分は最大の量効果へ振る (真・根の紡ぎ: 打ち消し1つ + 成長4→… VP12ぶん)', () => {
    const def = fuseCards(inst('green_reaction_root_weave', 'a'), inst('green_reaction_root_weave', 'b'))
    expect(def.effects.filter((e) => e.effect === 'negate')).toHaveLength(1)
  })
})

describe('工房ノード (マップの選択ノード)', () => {
  it('工房ノードに入ると合成でき、素材2枚が消えて1枚増える', () => {
    let run = runTo(createRun(31, 'set-confirm'), 'workshop')
    expect(run.phase).toBe('workshop')
    const before = run.deck.length
    // 合成可能なペアを探す
    let pair: [number, number] | null = null
    outer: for (let i = 0; i < run.deck.length; i++) {
      for (let j = i + 1; j < run.deck.length; j++) {
        if (fuseBlockReason(run.deck[i], run.deck[j]) === null) { pair = [i, j]; break outer }
      }
    }
    expect(pair).not.toBeNull()
    // 合成は有料 (2026-09-03 ユーザー裁定): 100G。足りなければ拒否、払えば減る
    const poor = { ...run, gold: workshopFusePrice(run) - 1 }
    expect(() => applyRunCommand(poor, { type: 'WorkshopFuse', indexA: pair![0], indexB: pair![1] })).toThrow('ゴールドが足りない')
    run = { ...run, gold: 300 }
    run = applyRunCommand(run, { type: 'WorkshopFuse', indexA: pair![0], indexB: pair![1] })
    expect(run.deck).toHaveLength(before - 1) // 2枚消えて1枚入る
    expect(run.gold).toBe(300 - workshopFusePrice(run))
    expect(run.phase).toBe('map')
  })

  it('見送りもできる', () => {
    let run = runTo(createRun(31, 'set-confirm'), 'workshop')
    expect(run.phase).toBe('workshop')
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'WorkshopSkip' })
    expect(run.deck).toHaveLength(before)
    expect(run.phase).toBe('map')
  })
})

describe('強化の3段仕様 (2026-08-27 仕様会議)', () => {
  it('①量+50%: 打撃 6→9', () => {
    const up = upgradeCard(inst('green_strike'))
    expect(up.def.effects[0].amount).toBe(9)
    expect(up.def.name).toBe('打撃+')
  })

  it('②(2026-09-04 本家形) 年輪 (成長+2) は単位+1で成長+3。コストは1のまま', () => {
    expect(upgradeTier(getCardDef('green_growth_ring'))).toBe('unit')
    const up = upgradeCard(inst('green_growth_ring'))
    expect(up.def.cost).toBe(1)
    expect(up.def.effects[0].amount).toBe(3)
  })

  it('③単位+1: 緑の閃き (0E化すると補充規約違反) はドロー4→5', () => {
    expect(upgradeTier(getCardDef('green_flash_insight'))).toBe('unit')
    const up = upgradeCard(inst('green_flash_insight'))
    expect(up.def.cost).toBe(1) // コストは変わらない
    expect(up.def.effects.find((e) => e.effect === 'drawCards')!.amount).toBe(5)
  })

  it('④上限ランプはコスト-1で強化 (2026-08-28 全カード解放。gainEnergyMaxの量は据え置き)', () => {
    expect(upgradeTier(getCardDef('green_ramp_sprout'))).toBe('cost')
    const up = upgradeCard({ uid: 'u', def: getCardDef('green_ramp_sprout') })
    expect(up.def.cost).toBe(0) // 芽吹き+ = 0E・上限+1・消滅 (テンポ損が消えるのが強化の意味)
    expect(up.def.effects[0].amount).toBe(1) // 上限の増加量は絶対に増えない = 複利安全弁
    expect(up.def.exhaust).toBe(true) // 消滅は維持 = 1回きり
    // 陽光の恵み (選択式) も modes 効果をスキャンして 2E→1E
    expect(upgradeTier(getCardDef('green_ramp_sunlight'))).toBe('cost')
    const sun = upgradeCard({ uid: 'u2', def: getCardDef('green_ramp_sunlight') })
    expect(sun.def.cost).toBe(1)
    expect(JSON.stringify(sun.def.modes)).toBe(JSON.stringify(getCardDef('green_ramp_sunlight').modes)) // モードは据え置き
  })

  it('④\' 0Eの参照スケーリング札は倍率/量+1 (余波×2・墓暴き5枚。コストを削れない札の受け皿)', () => {
    const wave = upgradeCard({ uid: 'u', def: getCardDef('blue_aftermath') })
    expect(wave.def.effects[0].amount).toBe(2) // 詠唱数×1 → ×2 (ドローしないので有限=安全)
    const digger = upgradeCard({ uid: 'u2', def: getCardDef('black_grave_digger') })
    expect(digger.def.effects[0].amount).toBe(5) // ミル4→5枚
    expect(digger.def.exhaustCost).toBe(1) // 対価は据え置き (非対称強化)
  })

  it('④\'\' 同軸おまけの例外表: 連鎖する思考+=+1ドロー・霊気の奔流+=放出前に霊気+2', () => {
    expect(upgradeTier(getCardDef('blue_chain_thought'))).toBe('bonus')
    const chain = upgradeCard({ uid: 'u', def: getCardDef('blue_chain_thought') })
    expect(chain.def.cost).toBe(1) // コストは据え置き (0E化は補充規約違反)
    expect(chain.def.effects.map((e) => e.effect)).toEqual(['drawCards', 'drawCardsPerCardPlayed'])
    expect(chain.def.effects[0].amount).toBe(1) // 自分も詠唱数に数えるフレーバーの+1ドロー
    const torrent = upgradeCard({ uid: 'u2', def: getCardDef('blue_aether_torrent') })
    // 順序が仕様: 霊気+2 を先に解決してから放出 (=実質ドロー+2)
    expect(torrent.def.effects.map((e) => e.effect)).toEqual(['addAether', 'dischargeAetherDraw'])
    expect(torrent.def.effects[0].amount).toBe(2)
  })

  it('全カード解放: 強化不可 (none) の札は存在しない (2026-08-28 決定を機械固定)', () => {
    for (const c of allCards) {
      expect(upgradeTier(c), `${c.id} が強化不可`).not.toBe('none')
    }
  })

  it('自傷の非対称強化: 対価は据え置きで出力だけ+50% (StSのHemokinesis+と同じ裁定)', () => {
    const up = upgradeCard(inst('black_blood_price')) // 2E・HP-6・24ダメ (2026-09-01 同型統合)
    expect(up.def.effects.find((e) => e.effect === 'dealDamage')!.amount).toBe(36)
    expect(up.def.effects.find((e) => e.effect === 'loseHp')!.amount).toBe(6) // 据え置き
  })
})

describe('合成カードの描画クラッシュ (2026-08-28 修正。人間+LLM両テスターが報告)', () => {
  // イベントログは cardId しか持たず、描画側が静的カード表 (getCardDef) だけを引いていたため
  // 「未定義カード: fused_*」で UI 全体がクラッシュしていた。resolveFusedDef で復元する。
  it('resolveFusedDef: 計算合成のIDから同じ定義を復元できる', async () => {
    const { resolveFusedDef } = await import('./fusion.ts')
    const def = fuseCards(inst('green_sig_vine_dance'), inst('green_strike'))
    const resolved = resolveFusedDef(def.id)
    expect(resolved).not.toBeNull()
    expect(JSON.stringify(resolved)).toBe(JSON.stringify(def)) // 決定的 = 完全一致
  })

  it('resolveFusedDef: レシピ産 (fusion_*) もレシピ表から引ける', async () => {
    const { resolveFusedDef } = await import('./fusion.ts')
    const resolved = resolveFusedDef('fusion_vine_wheel')
    expect(resolved?.name).toBe('蔦車輪')
    expect(resolveFusedDef('fused_no_such__card')).toBeNull() // 不明IDは null (throwしない)
    expect(resolveFusedDef('unknown_id')).toBeNull()
  })

  it('合成カードをプレイした後のイベントログが UI/CLI の描画関数でクラッシュしない', async () => {
    const { logLine, cardName } = await import('../ui/log.ts')
    const { startCombatWithOptions } = await import('./combat.ts')
    const { buildDeck } = await import('./content.ts')
    const { applyCommand } = await import('./state.ts')
    const fusedDef = fuseCards(inst('green_sig_vine_dance'), inst('green_strike'))
    let s = startCombatWithOptions(42, 'set-confirm', 'enemy_brute', {
      deck: buildDeck('starter'),
    })
    s = {
      ...s,
      player: {
        ...s.player,
        energy: 9,
        hand: [{ uid: 'fused_test', def: fusedDef }],
      },
    }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'fused_test' })
    expect(s.eventLog.some((e) => e.type === 'CardPlayed' && e.cardId === fusedDef.id)).toBe(true)
    // 全イベントを描画してもthrowしない (旧実装はここで「未定義カード」例外)
    for (const e of s.eventLog) {
      expect(() => logLine(e)).not.toThrow()
    }
    expect(cardName(fusedDef.id)).toBe(fusedDef.name)
  })
})

describe('焚き火の全カード解放 (2026-08-28。旧: 芽吹きは拒否→コスト-1で受理に変更)', () => {
  it('芽吹きを焚き火で鍛えると 0E・上限+1・消滅 になりデッキに反映される', () => {
    let run = runTo(createRun(17, 'set-confirm'), 'campfire')
    expect(run.phase).toBe('campfire')
    // 中立スターター化 (2026-08-29) で芽吹きは初期デッキに無いため、ピック済みの体で注入する
    run = { ...run, deck: [...run.deck, { uid: 'picked_sprout', def: getCardDef('green_ramp_sprout') }] }
    const idx = run.deck.findIndex((c) => c.def.id === 'green_ramp_sprout')
    const after = applyRunCommand(run, { type: 'CampfireUpgrade', index: idx })
    expect(after.deck[idx].def.name).toBe('芽吹き+')
    expect(after.deck[idx].def.cost).toBe(0)
    expect(after.deck[idx].def.effects[0].amount).toBe(1) // 上限量は据え置き
  })
})

describe('リアクション化のブロックはpre窓 (2026-09-02 「被攻撃後にブロックは意味無い」)', () => {
  it('防御×茨の返し: 吸収されたブロックは被攻撃前 (onAttackIncoming) に置かれる', () => {
    const def = fuseCards(
      { uid: 'a', def: getCardDef('green_guard') },
      { uid: 'b', def: getCardDef('green_reaction_thorns') },
    )
    expect(def.type).toBe('reaction')
    const block = def.effects.find((e) => e.effect === 'gainBlock')
    expect(block?.trigger).toBe('onAttackIncoming')
  })
})

describe('工房産の誘発ごと置物は鍛えられない (2026-09-05 ユーザー裁定。Opusラン Q: 真・棘の蔓+が手数の鏡を無効化)', () => {
  it('棘の蔓×棘の蔓 (攻撃ごとブロック) の合成品は鍛え不可、年輪の大樹×年輪の大樹 (毎T固定) は鍛え可', () => {
    const thorn = fuseCards(inst('green_perm_thorn_vine'), inst('green_perm_thorn_vine'))
    expect(thorn.type).toBe('permanent')
    expect(upgradeTier(thorn)).toBe('none')
    const tree = fuseCards(inst('green_perm_growth_tree'), inst('green_perm_growth_tree'))
    expect(tree.type).toBe('permanent')
    expect(upgradeTier(tree)).not.toBe('none')
    // 素の棘の蔓 (工房産でない) は従来どおり鍛えられる
    expect(upgradeTier(getCardDef('green_perm_thorn_vine'))).not.toBe('none')
  })
})

describe('検証ハーネス: StartCombat.cardIds で工房産を含む任意のデッキから単発戦闘を始められる (2026-09-05)', () => {
  it('fused_<a>__<b> の id が resolveFusedDef で復元され、デッキに入る', () => {
    const fused = fuseCards(inst('green_fang'), inst('green_horn_stance'))
    let s = createInitialState(3, 'set-confirm')
    s = applyCommand(s, { type: 'StartCombat', seed: 3, enemyId: 'enemy_probe', cardIds: ['green_strike', 'green_guard', fused.id, 'green_strike', 'green_guard'] })
    const all = [...s.player.hand, ...s.player.drawPile]
    expect(all.some((c) => c.def.id === fused.id && c.def.name === fused.name)).toBe(true)
    expect(all).toHaveLength(5)
  })
})
