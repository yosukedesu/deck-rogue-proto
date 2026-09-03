// カード合成 (工房) のテスト。確定済みルール表「カード合成（工房）」「工房ノード」を固定する。
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getEventDef } from './content.ts'
import { fuseBlockReason, fuseCards } from './fusion.ts'
import { applyRunCommand, createRun, upgradeCard, upgradeTier, workshopFusePrice } from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
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
const cornered = () => ({ uid: 'cornered', def: { ...getCardDef('green_reaction_thorns'), id: 'test_cornered', name: '窮鼠(テスト)', effects: [{ trigger: 'onAttacked' as const, condition: { hpAtOrBelowRatio: 0.5 }, effect: 'counter' as const, amount: 20 }] } })

describe('計算合成', () => {
  it('同種効果は量が合算され、コストはVPから逆算される (打撃系2枚 → 1枚)', () => {
    const def = fuseCards(inst('green_fang'), inst('green_serpent_gulp')) // 14貫通 + 20(捨て1)
    expect(def.effects.some((e) => e.effect === 'dealDamage')).toBe(true)
    expect(def.cost).toBeGreaterThanOrEqual(1)
    expect(def.cost).toBeLessThanOrEqual(5) // 2026-08-30: 3E頭打ちを5Eへ開放 (価値保存とセット)
    expect(def.discardCost).toBe(1) // 追加コストは引き継ぐ
    expect(def.color).toBe('green')
  })

  it('決定的: 同じ素材からは常に同じ結果 (順序も問わない)', () => {
    const ab = fuseCards(inst('green_fang'), inst('green_bark_armor'))
    const ab2 = fuseCards(inst('green_fang'), inst('green_bark_armor'))
    expect(JSON.stringify(ab)).toBe(JSON.stringify(ab2))
  })

  it('レシピが最優先される (年輪×二連の蔦打ち → 蔦車輪)', () => {
    const def = fuseCards(inst('green_growth_ring'), inst('green_double_lash'))
    expect(def.id).toBe('fusion_vine_wheel')
    const rev = fuseCards(inst('green_double_lash'), inst('green_growth_ring'))
    expect(rev.id).toBe('fusion_vine_wheel') // 順序を問わない
  })

  it('タイプ跨ぎ合成は許可される (2026-08-28 支配順位で解禁。旧仕様の同タイプ制限は撤廃)', () => {
    expect(fuseBlockReason(inst('green_strike', 'u1'), inst('green_strike', 'u2'))).toBeNull() // 同名=真・化
    expect(fuseBlockReason(inst('green_strike'), inst('green_flash_insight'))).toBeNull() // 物理×呪文
    expect(
      fuseBlockReason(inst('green_reaction_thorns'), cornered()),
    ).toBeNull() // 異名リアクション同士も計算合成できる (条件は別効果のまま保持)
    expect(fuseBlockReason(inst('green_strike'), inst('green_reaction_thorns'))).toBeNull() // 物理×リアクション
    expect(fuseBlockReason(inst('green_strike'), inst('green_perm_growth_tree'))).toBeNull() // 物理×置物
  })

  it('選択式 (modes) と機械査定できない効果は引き続き合成不可', () => {
    expect(fuseBlockReason(inst('green_strike'), inst('green_ramp_sprout'))).not.toBeNull() // gainEnergyMax
  })

  it('スモーク: 緑×緑の全組み合わせで不変条件が守られる', () => {
    const greens = allCards.filter((c) => c.color === 'green')
    const REFILL = new Set([
      'drawCards', 'drawCardsPerCardPlayed', 'dischargeAetherDraw', 'impulseDraw',
      'retrieveFromExhaust', 'playFromExhaust',
    ])
    let fusable = 0
    for (let i = 0; i < greens.length; i++) {
      for (let j = i + 1; j < greens.length; j++) {
        const a = { uid: `a${i}`, def: greens[i] }
        const b = { uid: `b${j}`, def: greens[j] }
        if (fuseBlockReason(a, b) !== null) continue
        fusable++
        const def = fuseCards(a, b)
        // コストは1〜5E (2026-08-30: 3E頭打ちだと「7E相当の素材が3Eで出る」効率2.3倍が
        // 構造的に発生していた。レシピは手書き裁定なので別枠)
        const isRecipe = def.id.startsWith('fusion_')
        if (!isRecipe) {
          expect(def.cost, def.id).toBeGreaterThanOrEqual(1)
          expect(def.cost, def.id).toBeLessThanOrEqual(5)
          // 派手枠は3効果まで (多段ヒットのダメージ群は1つと数える)
          const dmgCount = def.effects.filter((e) => e.effect === 'dealDamage').length
          const conceptual = def.effects.length - dmgCount + (dmgCount > 0 ? 1 : 0)
          expect(conceptual, def.id).toBeLessThanOrEqual(3)
        }
        // 無限ループ規約: 正味の値段が0以上 + 補充 → 消滅必須
        const net = def.effects
          .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
          .reduce((acc, e) => acc + (e.amount ?? 0), 0)
        if (net - def.cost >= 0 && def.effects.some((e) => REFILL.has(e.effect))) {
          expect(def.exhaust, `${def.id} はループ規約により消滅必須`).toBe(true)
        }
        // gainEnergyMax を持つなら消滅必須
        if (def.effects.some((e) => e.effect === 'gainEnergyMax')) {
          expect(def.exhaust, def.id).toBe(true)
        }
      }
    }
    expect(fusable).toBeGreaterThan(100) // 合成可能な組が十分にある
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

  it('②コスト-1: 年輪 (成長+2) は 1E→0E になり、量は据え置き', () => {
    expect(upgradeTier(getCardDef('green_growth_ring'))).toBe('cost')
    const up = upgradeCard(inst('green_growth_ring'))
    expect(up.def.cost).toBe(0)
    expect(up.def.effects[0].amount).toBe(2)
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

describe('タイプ跨ぎ合成 = 支配順位 (2026-08-28。置物＞リアクション＞呪文＞物理)', () => {
  it('物理×呪文 → 呪文 (魔力が混ざれば呪文。確定済み定義と整合)', () => {
    const def = fuseCards(inst('green_strike'), inst('green_growth_ring'))
    expect(def.type).toBe('spell')
    expect(def.effects.find((e) => e.effect === 'dealDamage')!.trigger).toBe('onPlay')
    expect(def.effects.find((e) => e.effect === 'addGrowth')!.amount).toBe(2)
  })

  it('物理×リアクション → 罠に吸収: 打撃×茨の返し = 被攻撃後10ダメ+返し10 (onPlay効果が残らない)', () => {
    const def = fuseCards(inst('green_strike'), inst('green_reaction_thorns'))
    expect(def.type).toBe('reaction')
    expect(def.name.endsWith('の罠')).toBe(true)
    // 全効果がリアクション窓 = 伏せて発動する札として合法 (onPlayが残ると設定できない札になる)
    expect(def.effects.every((e) => e.trigger !== 'onPlay')).toBe(true)
    const dmg = def.effects.find((e) => e.effect === 'dealDamage')!
    expect(dmg.trigger).toBe('onAttacked') // 支配側 (茨の返し) の主窓に吸収
    expect(dmg.amount).toBe(10) // 合成プレミアム×1.25 (2026-09-01) で 6→10
    expect(def.effects.find((e) => e.effect === 'counter')!.amount).toBe(10)
  })

  it('異名リアクション同士: 条件の異なる同種効果は合算されない (無条件9と HP半分以下20 は別のまま)', () => {
    const def = fuseCards(inst('green_reaction_thorns'), cornered())
    expect(def.type).toBe('reaction')
    const counters = def.effects.filter((e) => e.effect === 'counter')
    expect(counters).toHaveLength(2)
    // 2026-08-30: コストは素材の合計 (1E+1E) を超えない。条件付きは期待値係数0.6で
    // 数えるため圧縮が浅くなる。条件違いが**別の効果のまま**であることは不変。
    // 合成プレミアム×1.25 (2026-09-01) で 8→10 / 16→20
    expect(def.cost).toBe(2)
    expect(counters.some((e) => e.amount === 10 && e.condition === undefined)).toBe(true)
    expect(counters.some((e) => e.amount === 20 && e.condition?.hpAtOrBelowRatio === 0.5)).toBe(true)
  })

  it('置物化: 打撃6×年輪の大樹 → 毎ターンダメ (÷3切り上げ) + 成長1。窓の変換則を固定', () => {
    const def = fuseCards(inst('green_strike'), inst('green_perm_growth_tree'))
    expect(def.type).toBe('permanent')
    expect(def.name.endsWith('の大樹')).toBe(true)
    const dmg = def.effects.find((e) => e.effect === 'dealDamage')!
    expect(dmg.trigger).toBe('onTurnStart')
    expect(dmg.amount).toBe(4) // ceil(6/3)=2 に合成プレミアム×1.25 (2026-09-01。プレミアムはスケール可能な量へ集中配分される)
    const growth = def.effects.find((e) => e.effect === 'addGrowth')!
    expect(growth.trigger).toBe('onTurnStart')
    expect(growth.amount).toBe(1) // 置物側は既に毎ターン型なので÷3しない
    expect(def.exhaust).toBeUndefined() // 置物に消滅は付かない
    expect(def.cost).toBeGreaterThanOrEqual(2) // 寿命込み (×3) の値付けで安売りしない
  })

  it('置物の値付けは寿命込み: 真・年輪の大樹 (毎ターン成長+2) は4E (一回きり価格の穴を塞いだ)', () => {
    const def = fuseCards(
      inst('green_perm_growth_tree', 'u1'),
      inst('green_perm_growth_tree', 'u2'),
    )
    expect(def.name).toBe('真・年輪の大樹')
    expect(def.effects.find((e) => e.effect === 'addGrowth')!.amount).toBe(2) // 1+1 (÷3の二重割引なし)
    expect(def.cost).toBe(4) // (4VP×2×寿命3)×0.85 → 4E (2026-08-30 コスト上限の開放で頭打ちが外れた)
  })

  it('置物化の帯超過は「合成不可」でなく量の圧縮で解消される (2026-08-30 価値保存)', () => {
    // 旧実装は 巨獣の踏みつけ×年輪の大樹 を「置物に収まらない」で弾いていた。
    // 価値保存で素材VPの85%に収まるよう量を逆算するので、弾かずに成立する
    // (ユーザー方針: 合成不可は増やさない)
    expect(fuseBlockReason(inst('green_finisher_stomp'), inst('green_perm_growth_tree'))).toBeNull()
    const def = fuseCards(inst('green_finisher_stomp'), inst('green_perm_growth_tree'))
    expect(def.type).toBe('permanent')
    expect(def.exhaust).toBeUndefined() // 置物に消滅は付かない
    expect(def.cost).toBeLessThanOrEqual(5)
  })

  it('置物化の禁則②: 量を持たない効果 (negate) は毎ターン化できず合成不可 (根の紡ぎ×年輪の大樹)', () => {
    expect(
      fuseBlockReason(inst('green_reaction_root_weave'), inst('green_perm_growth_tree')),
    ).toContain('置物化できない')
  })

  it('置物化で誘発できない窓は onTurnStart に落ちる (共鳴する茨 onEnemyBuffed は置物で誘発しない)', () => {
    const def = fuseCards(inst('green_reaction_vine'), inst('green_perm_growth_tree'))
    // 守りの蔓 (onAttackIncoming ブロック12) → 置物はこの窓で誘発できるので窓を保持し÷3
    const blk = def.effects.find((e) => e.effect === 'gainBlock' && e.trigger === 'onAttackIncoming')!
    expect(blk.amount).toBe(4) // ceil(12/3)=4 → 素材合計3Eへの圧縮で3 → プレミアム×1.25で4へ復帰
    expect(def.effects.find((e) => e.effect === 'addGrowth')!.amount).toBe(1) // 置物側は据え置き
  })

  it('スモーク: 緑×緑の全組み合わせでタイプ跨ぎの不変条件が守られる', () => {
    const greens = allCards.filter((c) => c.color === 'green')
    const RANK: Record<string, number> = { permanent: 3, reaction: 2, spell: 1, physical: 0 }
    // 置物にディスパッチされないトリガー = 置物結果が持っていたら死に効果
    // (onPlay はプレイ時に解決されるので置物でも生きている。2026-08-29 大樹の登場時ブロック対応)
    const PERM_DEAD = new Set(['onEnemyAction', 'onEnemyBuffed', 'onEnemyDefended'])
    // 予算に合わせて削れる「量」の効果 (engine/fusion.ts の BLOCKY + ダメージ)
    const SCALABLE = new Set([
      'dealDamage',
      'gainBlock',
      'gainIceBlock',
      'counter',
      'gainHp',
      'applyBurn',
      'dealDamageDrain',
      'dealDamageRandom',
    ])
    for (let i = 0; i < greens.length; i++) {
      for (let j = i + 1; j < greens.length; j++) {
        const a = { uid: `a${i}`, def: greens[i] }
        const b = { uid: `b${j}`, def: greens[j] }
        if (fuseBlockReason(a, b) !== null) continue
        const def = fuseCards(a, b)
        if (def.id.startsWith('fusion_')) continue // レシピは手書き裁定
        // 結果タイプ = 支配順位の高い側
        expect(RANK[def.type], def.id).toBe(Math.max(RANK[greens[i].type], RANK[greens[j].type]))
        // リアクション結果に onPlay 効果が残らない (伏せ札の合法性)
        if (def.type === 'reaction') {
          expect(def.effects.every((e) => e.trigger !== 'onPlay'), def.id).toBe(true)
        }
        // 置物結果は死に効果 (置物にディスパッチされないトリガー) と消滅を持たない
        if (def.type === 'permanent') {
          expect(def.effects.every((e) => !PERM_DEAD.has(e.trigger)), def.id).toBe(true)
          expect(def.exhaust, def.id).toBeUndefined()
        }
        // 圧縮なのに重くなる、を禁じる (2026-08-30)。削れる「量」を持つ合成はコストが
        // 素材コストの合計を超えない (超える分は出力を削って払う)
        // 置物は除く: 値付けが寿命込み (×3) なので、素材より重くなるのが正しい姿
        if (def.type !== 'permanent' && def.effects.some((e) => SCALABLE.has(e.effect))) {
          const sum = Math.max(1, greens[i].cost + greens[j].cost) // 0E同士は下限1E
          expect(def.cost, def.id).toBeLessThanOrEqual(sum)
        }
      }
    }
  })
})

describe('赤の工房 (2026-08-30 全色開放後の赤対応)', () => {
  it('猛り火条件付きダメージは条件ごと引き継がれる (平坦化しない)', () => {
    // 猛り火の一撃 (5 + {猛}7) × 火弾 (6): 無条件分だけが合算され、{猛}7は条件のまま残る
    const def = fuseCards(inst('red_blaze_strike'), inst('red_strike'))
    const uncond = def.effects.filter((e) => e.effect === 'dealDamage' && e.condition === undefined)
    const blaze = def.effects.filter((e) => e.effect === 'dealDamage' && e.condition?.blaze === true)
    expect(uncond.length).toBeGreaterThan(0)
    expect(blaze).toHaveLength(1)
    expect(blaze[0].amount).toBe(7)
  })

  it('乱数札は平均値で値付けされる (最小値査定の350%価値漏れの再発防止)', () => {
    // 火運の賭け (乱2〜16 = 平均9) × とどめの一撃 (処刑10〜28 = 平均19)
    const def = fuseCards(inst('red_gamble'), inst('red_final_blow'))
    expect(def.cost).toBeGreaterThanOrEqual(3) // 旧実装は最小値査定で1Eになっていた
  })

  it('今日の新効果 (火移し・一擲乾坤・業腹) が合成可能 (VP表に典型参照量で登録)', () => {
    expect(fuseBlockReason(inst('red_fire_shift'), inst('red_strike'))).toBeNull()
    expect(fuseBlockReason(inst('red_all_in'), inst('red_strike'))).toBeNull()
    expect(fuseBlockReason(inst('red_spite'), inst('red_strike'))).toBeNull()
  })

  it('名前に赤語彙が出る (緑v1の 樹/牙 だけにならない)', () => {
    const def = fuseCards(inst('red_ignite'), inst('red_ember'))
    expect(def.name).toContain('焔') // 着火×くすぶる残り火 = 延焼が主役
  })

  it('赤×赤の全組み合わせでコスト契約と支配順位が守られる', () => {
    const reds = allCards.filter((c) => c.color === 'red')
    const RANK: Record<string, number> = { permanent: 3, reaction: 2, spell: 1, physical: 0 }
    for (let i = 0; i < reds.length; i++) {
      for (let j = i + 1; j < reds.length; j++) {
        const a = { uid: `a${i}`, def: reds[i] }
        const b = { uid: `b${j}`, def: reds[j] }
        if (fuseBlockReason(a, b) !== null) continue
        const def = fuseCards(a, b)
        if (def.id.startsWith('fusion_')) continue
        expect(RANK[def.type], def.id).toBe(Math.max(RANK[reds[i].type], RANK[reds[j].type]))
        if (def.type === 'reaction') {
          expect(def.effects.every((e) => e.trigger !== 'onPlay'), def.id).toBe(true)
        }
        // 圧縮なのに重くなる、を禁じる (置物は寿命込み値付けなので除外)
        const SCALABLE = new Set(['dealDamage', 'gainBlock', 'gainIceBlock', 'counter', 'gainHp', 'applyBurn', 'dealDamageDrain', 'dealDamageRandom'])
        if (def.type !== 'permanent' && def.effects.some((e) => SCALABLE.has(e.effect))) {
          const sum = Math.max(1, reds[i].cost + reds[j].cost)
          expect(def.cost, def.id).toBeLessThanOrEqual(sum)
        }
      }
    }
  })
})

describe('特性の掛け合わせ (2026-08-27。「合成なんだから特性を掛け合わせたい」)', () => {
  it('多段×貫通: 二連の蔦打ち(4×2)×荒角の一撃(7貫通) → 貫通の多段ヒット', () => {
    const def = fuseCards(inst('green_double_lash'), inst('green_horn_strike'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs).toHaveLength(3) // ヒット合算 (2026-08-30): 2+1=3ヒット (旧: 最大側の2に按分)
    expect(dmgs.every((e) => e.pierce === true)).toBe(true) // 貫通が全ヒットへ伝播
    expect(dmgs[0].amount).toBe(6) // 価値保存×プレミアム1.25: ヒットが増えた上で per-hit も戻る (成長の乗り先が3回に)
  })

  it('全体×貫通: 薙ぎ払い(全体6)×牙の一撃(14貫通) → 全体・貫通の一撃', () => {
    const def = fuseCards(inst('green_sweep'), inst('green_fang'))
    const dmg = def.effects.find((e) => e.effect === 'dealDamage')!
    expect(dmg.target).toBe('all')
    expect(dmg.pierce).toBe(true)
    // 価値保存 (2026-08-30): 全体×2と貫通×1.25が無料で乗っていたのを是正 (24→14)。
    // 素材コスト合計 (2E+2E) への圧縮で11 → 合成プレミアム×1.25 (2026-09-01) で14へ復帰
    expect(def.cost).toBe(4)
    expect(dmg.amount).toBe(14)
    // 薙ぎ払いの成長+1も引き継がれる
    expect(def.effects.some((e) => e.effect === 'addGrowth')).toBe(true)
  })

  it('多段×大打点: 蔦の乱舞(2×5)×大蛇の丸呑み(20) → 5ヒットに按分 (成長が5回乗る)', () => {
    const def = fuseCards(inst('green_sig_vine_dance'), inst('green_serpent_gulp'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs).toHaveLength(5)
    expect(dmgs[0].amount).toBe(12) // 12×5 (2026-09-02 蔦の乱舞の定義札化=成長+1×5のVPが素材側に乗った。旧11)
    expect(def.discardCost).toBe(1) // 追加コストは引き継ぐ
  })

  it('特性が名前に出る (多段=乱撃・全体=嵐)', () => {
    const multi = fuseCards(inst('green_double_lash'), inst('green_horn_strike'))
    expect(multi.name.endsWith('乱撃')).toBe(true)
    const aoe = fuseCards(inst('green_sweep'), inst('green_fang'))
    expect(aoe.name.endsWith('嵐')).toBe(true)
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

describe('同名合成 =「真・」化 (2026-08-28 ユーザー指示「同名カードは倍率上げて強いカードが生成されるべき」)', () => {
  it('打撃×打撃 → 真・打撃 (量が2枚ぶんに圧縮され、コストはVP逆算で1E)', () => {
    const def = fuseCards(inst('green_strike', 'u1'), inst('green_strike', 'u2'))
    expect(def.name).toBe('真・打撃')
    expect(def.effects.find((e) => e.effect === 'dealDamage')!.amount).toBe(15) // (6+6)×プレミアム1.25
    expect(def.cost).toBe(1) // コストはプレミアム前のVPから逆算 = 据え置き (工房の目玉)
  })

  it('蔦の乱舞×蔦の乱舞 → 5ヒットのまま量が倍 (4×5)', () => {
    const def = fuseCards(inst('green_sig_vine_dance', 'u1'), inst('green_sig_vine_dance', 'u2'))
    const dmgs = def.effects.filter((e) => e.effect === 'dealDamage')
    expect(dmgs).toHaveLength(5)
    expect(dmgs[0].amount).toBe(7) // 2026-09-02 定義札化後 (成長+1×5が合算されるぶん量も伸びる。旧5=(10+10)×1.25/5)
  })

  it('同名リアクションも合成できる (茨の返し×2 → 返し25。トリガー・条件が同一なので安全)', () => {
    const a = inst('green_reaction_thorns', 'u1')
    const b = inst('green_reaction_thorns', 'u2')
    expect(fuseBlockReason(a, b)).toBeNull()
    const def = fuseCards(a, b)
    expect(def.type).toBe('reaction')
    expect(def.effects.find((e) => e.effect === 'counter')!.amount).toBe(25) // (10+10)×1.25
    expect(def.name).toBe('真・茨の返し')
  })

  it('量を持たない同種効果は重複しない (根の紡ぎ×2 → 打ち消しは1つ・成長は合算)', () => {
    const def = fuseCards(inst('green_reaction_root_weave', 'u1'), inst('green_reaction_root_weave', 'u2'))
    expect(def.effects.filter((e) => e.effect === 'negate')).toHaveLength(1)
    expect(def.effects.find((e) => e.effect === 'addGrowth')!.amount).toBe(4) // 根の紡ぎ2E化 (2026-08-29) で3+3→2+2
  })

  it('選択式カードは同名でも合成不可 (モード構造を計算合成で作れないため)', () => {
    expect(fuseBlockReason(inst('green_ramp_sunlight', 'u1'), inst('green_ramp_sunlight', 'u2'))).not.toBeNull()
  })

  it('スモーク: 緑全カードの同名合成で不変条件が守られる', () => {
    const greens = allCards.filter((c) => c.color === 'green')
    for (const g of greens) {
      const a = { uid: 'sa', def: g }
      const b = { uid: 'sb', def: g }
      if (fuseBlockReason(a, b) !== null) continue
      const def = fuseCards(a, b)
      expect(def.cost, def.id).toBeGreaterThanOrEqual(1)
      expect(def.cost, def.id).toBeLessThanOrEqual(5) // 2026-08-30 コスト上限の開放
      if (def.effects.some((e) => e.effect === 'gainEnergyMax')) {
        expect(def.exhaust, def.id).toBe(true)
      }
      const net = def.effects
        .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
        .reduce((acc, e) => acc + (e.amount ?? 0), 0)
      const REFILL2 = ['drawCards', 'impulseDraw', 'drawCardsPerCardPlayed']
      if (net - def.cost >= 0 && def.effects.some((e) => REFILL2.includes(e.effect))) {
        expect(def.exhaust, def.id).toBe(true)
      }
    }
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
