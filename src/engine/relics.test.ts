// エリートノードとレリック (2026-08-25。2026-08-28 マップ化) のテスト。
// 確定済みルール表「エリート挑戦オファー」「レリック」と docs/relics-design.md を固定する。
import { describe, expect, it } from 'vitest'
import { allRelics, buildRelicPermanent, getCardDef, getEventDef, getRelicDef, getEnemyDef, resolveEncounter } from './content.ts'
import { applyRunCommand, createRun, currentNode, drawRelicOptions, shopRemovalPrice, shopUpgradePrice, workshopFusePrice, campfireForgeAllowed } from './run.ts'
import type { RunState } from './run.ts'
import { applyCommand } from './state.ts'
import { startCombatWithOptions } from './combat.ts'
import { attackIntent, chooseToward, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

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

/** 最初のエリートノードに入るまで進める (途中の戦闘は forceWin、非戦闘はスキップ) */
function intoFirstElite(seed = 11): RunState {
  let run = createRun(seed, 'set-confirm')
  let guard = 0
  while (guard++ < 80) {
    if (run.phase === 'map') {
      run = chooseToward(run, 'elite')
      if (run.phase === 'combat' && run.currentElite) return run
    } else if (run.phase === 'combat') {
      run = forceWin(run)
    } else if (run.phase === 'campfire') {
      run = applyRunCommand(run, { type: 'CampfireRest' })
    } else if (run.phase === 'workshop') {
      run = applyRunCommand(run, { type: 'WorkshopSkip' })
    } else if (run.phase === 'relic-reward') {
      run = applyRunCommand(run, { type: 'SkipRelic' })
    } else if (run.phase === 'reward') {
      run = applyRunCommand(run, { type: 'SkipReward' })
    } else if (run.phase === 'shop') {
      run = applyRunCommand(run, { type: 'ShopLeave' })
    } else if (run.phase === 'event') {
      const ev = getEventDef(run.eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  throw new Error('エリートノードに到達できない')
}

/** 現在のラン状態から次の戦闘に入るまで非戦闘フェーズを消化する */
function intoBattle(run0: RunState): RunState {
  let run = run0
  let guard = 0
  while (run.phase !== 'combat' && guard++ < 40) {
    if (run.phase === 'map') run = chooseToward(run, 'battle')
    else if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
    else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
    else if (run.phase === 'shop') run = applyRunCommand(run, { type: 'ShopLeave' })
    else if (run.phase === 'event') {
      const ev = getEventDef(run.eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  return run
}

describe('エリートノード (マップ化。opt-inオファーは廃止)', () => {
  it('マップにエリートノードが4つあり、選んで入るとエリート戦になる', () => {
    const run = createRun(11, 'set-confirm')
    expect(run.map.flat().filter((n) => n.type === 'elite')).toHaveLength(4)
    const elite = intoFirstElite()
    expect(elite.phase).toBe('combat')
    expect(elite.currentElite).toBe(true)
  })

  it('エリートは専用敵 (2026-08-31)。補正も幕内深度スケールも掛けず素の値で出る', () => {
    // 緑Opusランで発見: depthHpScale が残っていて鬼軍曹82→45 と設計値の55%で出ていた
    const elite = intoFirstElite()
    const node = currentNode(elite)!
    expect(node.encounterId!.includes('elite')).toBe(true) // エリート専用プールから出る
    const members = resolveEncounter(node.encounterId!)
    const def = getEnemyDef(members[0].enemyId)
    const expectHp = Math.round(def.maxHp * (members[0].hpScale ?? 1))
    expect(elite.combat!.enemies[0].maxHp).toBe(expectHp)
    expect(elite.combat!.enemies[0].strength).toBe(0 + (members[0].strength ?? 0))
  })

  it('エリートに勝つとレリック3択 → 取得後にカード報酬へ', () => {
    let run = intoFirstElite()
    run = forceWin(run)
    expect(run.phase).toBe('relic-reward')
    expect(run.relicOptions).toHaveLength(3)
    const picked = run.relicOptions![0]
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.relics).toEqual([picked])
    expect(run.phase).toBe('reward') // カード報酬にも進む
  })

  it('通常戦闘の勝利ではレリック報酬は出ない', () => {
    let run = intoBattle(createRun(11, 'set-confirm'))
    expect(run.currentElite).toBe(false)
    run = forceWin(run)
    expect(run.phase).toBe('reward')
  })

  it('決定論: 同シードのレリック候補列は一致する', () => {
    const a = createRun(7, 'set-confirm')
    const b = createRun(7, 'set-confirm')
    expect(a.relicQueue).toEqual(b.relicQueue)
    expect(new Set(a.relicQueue).size).toBe(allRelics.length)
  })
})

describe('レリック効果', () => {
  /** 指定レリックを持った状態で次の戦闘を始める */
  function withRelicIntoBattle(relicId: string): RunState {
    let run = intoFirstElite()
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf(relicId)
    if (idx >= 0) {
      run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    } else {
      // 候補に無い場合はテスト用に直接注入
      run = { ...run, relics: [relicId], relicOptions: null, phase: 'reward', rewardOptions: [] }
    }
    if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    return intoBattle(run)
  }

  it('A型 (賢者の巻物): 戦闘開始時に1枚ドロー = 初手が1枚多い (2026-09-01 弱体化 2→1)', () => {
    const run = withRelicIntoBattle('relic_sage_scroll')
    const c = run.combat!
    expect(c.player.hand.length).toBe(c.player.drawPerTurn + 1)
  })

  it('A型 (先手の盾): 戦闘開始時にブロック+5 (2026-09-01 弱体化 8→5)', () => {
    const run = withRelicIntoBattle('relic_vanguard_shield')
    expect(run.combat!.player.block).toBe(5)
  })

  it('B型 (鉄の心臓): 取得時に最大HP+8 (現在HPも+8)', () => {
    let run = intoFirstElite()
    const hpBefore = run.hp
    const maxBefore = run.maxHp
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf('relic_iron_heart')
    if (idx < 0) return // このシードの候補に無ければ対象外 (決定論なので固定)
    run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    expect(run.maxHp).toBe(maxBefore + 8)
    expect(run.hp).toBe(Math.min(run.maxHp, hpBefore + 8))
  })

  it('B型 (収集家の鞄): 報酬ピックの候補+1', () => {
    let run = intoFirstElite()
    run = forceWin(run)
    const idx = run.relicOptions!.indexOf('relic_collectors_bag')
    if (idx < 0) return
    run = applyRunCommand(run, { type: 'PickRelic', index: idx })
    expect(run.rewardOptions).toHaveLength(3 + 1) // リーダー基本3 + 鞄1 (2026-09-03 曲線パッケージ)
  })

  it('レリック定義は全て A型 (effects) / B型 (bonus) / C型 (combatRule) のいずれかを持つ', () => {
    for (const r of allRelics) {
      const hasA = (r.effects?.length ?? 0) > 0
      const hasB = r.bonus !== undefined
      const hasC = r.combatRule !== undefined
      expect(hasA || hasB || hasC).toBe(true)
      expect(getRelicDef(r.id).name.length).toBeGreaterThan(0)
    }
  })

  it('在庫は37個・IDは一意 (第二弾拡充 2026-08-29・ボスレリック 2026-09-03)', () => {
    expect(allRelics).toHaveLength(36) // 2026-09-05 大工の道具を撤去 (1幕1回・100Gの下で死に枠) // 2026-09-03 ボスレリック+4 (王冠の欠片・呪いの鍵・賢者の石・鎖の首輪) // 2026-09-03 蜃気楼の面を撤去 (確認ウィンドウを「はい」ボタンに退化させる。独立3本一致)
    expect(new Set(allRelics.map((r) => r.id)).size).toBe(36)
  })
})

describe('B型レリックの最大HPが戦闘へ届く (2026-08-27 バグ修正)', () => {
  // プレイテスターの報告で発覚: launchCombat が run.hp しか渡しておらず、
  // 鉄の心臓 (取得時maxHp+8) が増やした run.maxHp が戦闘の player.maxHp に反映されていなかった。
  it('鉄の心臓の+8が次の戦闘の最大HPに乗る', () => {
    let run = createRun(29, 'set-confirm')
    const baseMax = run.maxHp
    // 鉄の心臓を直接付与してボーナスを適用した状態を作る
    run = { ...run, relics: ['relic_iron_heart'] }
    run = {
      ...run,
      maxHp: baseMax + 8,
      hp: Math.min(baseMax + 8, run.hp + 8),
    }
    run = intoBattle(run)
    expect(run.combat!.player.maxHp).toBe(baseMax + 8)
  })
})

// ---- 第二弾レリック (2026-08-29 在庫拡充。docs/relics-design.md §4.5) ----

/** 指定レリックを直接持たせてラン最初の戦闘へ (候補列の運に依存しない注入) */
function injectedIntoBattle(relicId: string, seed = 11): RunState {
  return intoBattle({ ...createRun(seed, 'set-confirm'), relics: [relicId] })
}

/** 現在のラン状態から最初の焚き火に入るまで進める */
function intoCampfire(run0: RunState): RunState {
  let run = run0
  let guard = 0
  while (guard++ < 80) {
    if (run.phase === 'campfire') return run
    if (run.phase === 'map') run = chooseToward(run, 'campfire')
    else if (run.phase === 'combat') run = forceWin(run)
    else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
    else if (run.phase === 'relic-reward') run = applyRunCommand(run, { type: 'SkipRelic' })
    else if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    else if (run.phase === 'shop') run = applyRunCommand(run, { type: 'ShopLeave' })
    else if (run.phase === 'event') {
      const ev = getEventDef(run.eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  throw new Error('焚き火に到達できない')
}

describe('第二弾レリック: 緑3本柱 + 汎用 (A型)', () => {
  it('成長の種: 戦闘開始時に成長+1 (2026-09-01 弱体化 2→1。リーダーパッシブとは別枠で加算)', () => {
    const base = injectedIntoBattle('relic_thorn_crown') // 成長に触らない対照 (このはの毎T+1のみ)
    const run = injectedIntoBattle('relic_growth_seed')
    expect(run.combat!.player.growth).toBe(base.combat!.player.growth + 1)
  })

  it('韋駄天の帯: 第1ターン開始時に勢い+1', () => {
    const run = injectedIntoBattle('relic_swift_sash')
    expect(run.combat!.player.momentum).toBe(1)
  })

  it('古根の杯: 戦闘開始時に上限+1。第1ターンの手持ちエナジーは増えない (ランプ即時利用廃止の既存則)', () => {
    const base = injectedIntoBattle('relic_thorn_crown') // 上限に触らない対照
    const run = injectedIntoBattle('relic_oldroot_cup')
    expect(run.combat!.player.energyMax).toBe(base.combat!.player.energyMax + 1)
    expect(run.combat!.player.energy).toBe(base.combat!.player.energy)
  })

  it('猛禽の眼: 戦闘開始時に敵全体へ急所1', () => {
    const run = injectedIntoBattle('relic_raptor_eye')
    for (const e of run.combat!.enemies) {
      if (e.hp > 0) expect(e.exposed).toBe(1)
    }
  })
})

describe('第二弾レリック: 伏せシナジー (符師の懐・静かな鈴・蜃気楼の面)', () => {
  it('符師の懐: カードを伏せるたび1枚ドロー (新トリガー onCardSet)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withHand(s, ['green_reaction_thorns'])
    s = {
      ...s,
      player: {
        ...s.player,
        permanents: [...s.player.permanents, buildRelicPermanent(getRelicDef('relic_talisman_pouch'))],
      },
    }
    const drawBefore = s.player.drawPile.length
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.setCards).toHaveLength(1)
    expect(s.player.hand).toHaveLength(1) // 伏せて0枚 → 1枚引いて1枚
    expect(s.player.drawPile.length).toBe(drawBefore - 1)
  })

  it('静かな鈴: 伏せ札がある間、敵の攻撃実値-1 (最低1クランプ)', () => {
    const setup = (withSet: boolean): GameState => {
      let s: GameState = { ...freshCombat('set-confirm', 'enemy_brute', 42), setDamageReduction: 1 }
      // 攻撃では誘発しない onEnemyBuffed のリアクションを伏せる = 確認ウィンドウを挟まず解決される
      if (withSet) {
        s = {
          ...s,
          player: {
            ...s.player,
            setCards: [{ uid: 'set0', def: getCardDef('green_reaction_resonance') }],
          },
        }
      }
      s = withIntent(withHand(s, []), attackIntent(10))
      return applyCommand(s, { type: 'EndTurn' })
    }
    const hpStart = freshCombat('set-confirm', 'enemy_brute', 42).player.hp
    expect(setup(false).player.hp).toBe(hpStart - 10) // 伏せなし: 素通し
    expect(setup(true).player.hp).toBe(hpStart - 9) // 伏せあり: -1
  })

  it('蜃気楼の面: 意図の実値が常時公開される (shownMin=shownMax=actual)', () => {
    const deck = Array.from({ length: 10 }, (_, i) => ({
      uid: `d${i}`,
      def: getCardDef('green_sweep'),
    }))
    const masked = startCombatWithOptions(7, 'set-confirm', 'enemy_wide_power', { deck })
    const revealed = startCombatWithOptions(7, 'set-confirm', 'enemy_wide_power', {
      deck,
      revealIntents: true,
    })
    // うねる獣の幅 (攻撃8〜15) は素では幅表示
    const m = masked.enemies[0].intent!
    expect(m.shownMax).toBeGreaterThan(m.shownMin)
    // 面があると実値へ畳まれる (同シードなので実値は同一)
    const r = revealed.enemies[0].intent!
    expect(r.shownMin).toBe(m.actual)
    expect(r.shownMax).toBe(m.actual)
    expect(r.actual).toBe(m.actual)
  })
})

describe('第二弾レリック: ラン経済 (商人の秤・鍛冶の砥石)', () => {
  it('商人の秤: 取得でボーナス+8、勝利ゴールドが同シード比で+8', () => {
    let runA = intoBattle(createRun(31, 'set-confirm'))
    // 取得経路 (applyRelicBonus) の検証
    let picked: RunState = { ...runA, phase: 'relic-reward', relicOptions: ['relic_merchant_scale'] }
    picked = applyRunCommand(picked, { type: 'PickRelic', index: 0 })
    expect(picked.goldPerVictoryBonus).toBe(8)
    // 勝利ゴールドの検証 (同シード・同rngなので基本ロールは同一)
    const runB: RunState = { ...runA, goldPerVictoryBonus: 8 }
    const goldA = forceWin(runA).gold
    const goldB = forceWin(runB).gold
    expect(goldB).toBe(goldA + 8)
  })

  it('鍛冶の砥石なし: 鍛えるは1枚で焚き火を出る', () => {
    let run = intoCampfire(createRun(11, 'set-confirm'))
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    expect(run.phase).toBe('map')
  })

  it('幕1でも鍛えるに通算制限は無い (2026-09-01 ユーザー指示「幕に対しての制限不要」で撤廃)', () => {
    // 別の焚き火なら幕1でも何度でも鍛えられる (焚き火1回につき1枚の原則は不変)
    let bare = intoCampfire(createRun(11, 'set-confirm'))
    bare = applyRunCommand(bare, { type: 'CampfireUpgrade', index: 0 })
    bare = { ...bare, phase: 'campfire' as const, campfireUpgradesUsed: 0 }
    bare = applyRunCommand(bare, { type: 'CampfireUpgrade', index: 1 })
    expect(bare.deck.filter((c) => c.def.name.endsWith('+'))).toHaveLength(2)
    // 砥石あり: 幕1でも追加分の2枚目まで
    let run = { ...intoCampfire(createRun(11, 'set-confirm')), campfireForgeBonus: 1 }
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 1 })
    expect(run.deck.filter((c) => c.def.name.endsWith('+'))).toHaveLength(2)
  })

  it('鍛冶の砥石あり (幕2以降): 2枚まで鍛えられる。除去との併用は不可 (1種類の原則)', () => {
    let run = { ...intoCampfire(createRun(11, 'set-confirm')), campfireForgeBonus: 1, act: 2 }
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    expect(run.phase).toBe('campfire') // 1枚目の後も留まる
    expect(() => applyRunCommand(run, { type: 'CampfireRemove', index: 1 })).toThrow('除去できない') // 2026-09-03 焚き火の除去は廃止
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 1 })
    expect(run.phase).toBe('map') // 2枚目で出る
    expect(run.deck.filter((c) => c.def.name.endsWith('+'))).toHaveLength(2)
  })

  it('鍛冶の砥石は1幕に1回だけ2枚 (2026-09-05 ユーザー裁定「砥石の調整」: 人間#6 焚き火9回・休む0回)', () => {
    let run = { ...intoCampfire(createRun(11, 'set-confirm')), campfireForgeBonus: 1, act: 2 }
    expect(campfireForgeAllowed(run)).toBe(2)
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 1 })
    expect(run.phase).toBe('map')
    expect(run.forgeBonusUsedAct).toBe(2)
    // 同じ幕の次の焚き火は1枚で出る
    let again: RunState = { ...run, phase: 'campfire', campfireUpgradesUsed: 0 }
    expect(campfireForgeAllowed(again)).toBe(1)
    again = applyRunCommand(again, { type: 'CampfireUpgrade', index: 2 })
    expect(again.phase).toBe('map')
    expect(again.forgeBonusUsedAct).toBe(2) // 1枚だけでは記録が動かない
    // 次の幕では再び2枚
    const nextAct: RunState = { ...again, phase: 'campfire', campfireUpgradesUsed: 0, act: 3 }
    expect(campfireForgeAllowed(nextAct)).toBe(2)
    // 砥石なしなら幕に関係なく1枚
    expect(campfireForgeAllowed({ ...nextAct, campfireForgeBonus: 0 })).toBe(1)
  })

  it('旧セーブ互換: 新フィールドが無いランでも勝利ゴールドと焚き火強化が壊れない (NaN汚染防止)', () => {
    const battle = intoBattle(createRun(11, 'set-confirm'))
    const { goldPerVictoryBonus: _g, campfireForgeBonus: _c, campfireUpgradesUsed: _u, ...rest } = battle
    const legacy = rest as RunState
    const won = forceWin(legacy)
    expect(Number.isFinite(won.gold)).toBe(true)
    expect(won.gold).toBeGreaterThan(battle.gold)
    let camp = intoCampfire(createRun(11, 'set-confirm'))
    const { campfireUpgradesUsed: _u2, campfireForgeBonus: _c2, ...campRest } = camp
    const upgraded = applyRunCommand(campRest as RunState, { type: 'CampfireUpgrade', index: 0 })
    expect(upgraded.phase).toBe('map')
  })
})

describe('レリックの層×供給源 (2026-09-03 本家式。docs/relic-redesign-proposal.md §3-1)', () => {
  const TIERS = new Set(['common', 'uncommon', 'rare', 'boss', 'shop', 'event'])
  it('全レリックが既知の層を持ち、boss 層に少なくとも4つある', () => {
    for (const r of allRelics) expect(TIERS.has(r.rarity ?? 'common'), r.id).toBe(true)
    expect(allRelics.filter((r) => r.rarity === 'boss').length).toBeGreaterThanOrEqual(4)
  })
  it('宝箱/エリート/イベントの抽選に boss・shop 層は決して混ざらず、ボスの3択は boss 層だけ', () => {
    for (const seed of [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) {
      const run = createRun(seed, 'set-confirm')
      for (const src of ['chest', 'elite', 'event'] as const) {
        const [opts] = drawRelicOptions(run, src)
        expect(opts.length).toBe(3)
        for (const id of opts) expect(['boss', 'shop']).not.toContain(getRelicDef(id).rarity ?? 'common')
      }
      const [boss] = drawRelicOptions(run, 'boss')
      expect(boss.length).toBe(3)
      for (const id of boss) expect(getRelicDef(id).rarity).toBe('boss')
    }
  })
  it('決定性: 同じ状態なら同じ提示。所持済みは出ない', () => {
    const run = createRun(5, 'set-confirm')
    expect(drawRelicOptions(run, 'chest')[0]).toEqual(drawRelicOptions(run, 'chest')[0])
    const owned = { ...run, relics: run.relicQueue.slice(0, 5) }
    for (const id of drawRelicOptions(owned, 'elite')[0]) expect(owned.relics).not.toContain(id)
  })
  it('王冠の欠片: 報酬提示-1 (最低1枚のクランプ)', () => {
    let run = createRun(7, 'set-confirm')
    run = { ...run, phase: 'relic-reward', relicOptions: ['relic_crown_shard'], combat: null }
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.rewardChoicesBonus).toBe(-1)
    expect(run.relics).toContain('relic_crown_shard')
  })
  it('呪いの鍵 (2026-09-04 供給源を問わず): 持っている状態でレリックを取ると烙印が1枚増える (鍵を取った瞬間は増えない。エリート勝利の3択・ショップも同じ)', () => {
    let run = createRun(7, 'set-confirm')
    run = { ...run, phase: 'relic-reward', relicOptions: ['relic_cursed_key'], combat: null }
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    const brands = (r: RunState) => r.deck.filter((c) => c.def.id === 'status_brand').length
    expect(brands(run)).toBe(0)
    run = { ...run, phase: 'relic-reward', relicOptions: ['relic_iron_heart'], combat: null }
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(brands(run)).toBe(1)
    // ショップのレリック購入でも
    run = { ...run, phase: 'shop', gold: 500, shop: { cards: [], relicId: 'relic_herb_pouch', relicPrice: 150 } }
    run = applyRunCommand(run, { type: 'ShopBuyRelic' })
    expect(brands(run)).toBe(2)
  })
  it('鎖の首輪: 通常戦には注入されず、エリート戦には注入される', () => {
    const normal = injectedIntoBattle('relic_slaver_collar', 11)
    expect(normal.combat!.player.permanents.some((p) => p.uid === 'relic_relic_slaver_collar')).toBe(false)
    let run = intoFirstElite(11)
    run = { ...run, relics: ['relic_slaver_collar'] }
    // エリートノードに入り直す (intoFirstElite は戦闘中なので、同じノードで launchCombat を再現)
    const node = currentNode(run)!
    expect(node.type).toBe('elite')
    const again = applyRunCommand({ ...run, phase: 'map', combat: null, row: run.row - 1, col: 0, map: run.map }, { type: 'ChooseNode', col: run.col })
    expect(again.combat!.player.permanents.some((p) => p.uid === 'relic_relic_slaver_collar')).toBe(true)
  })
  it('賢者の石: 敵全員が筋力+1で始まる', () => {
    const base = intoBattle(createRun(11, 'set-confirm'))
    const withStone = injectedIntoBattle('relic_philosopher_stone', 11)
    withStone.combat!.enemies.forEach((e, i) => expect(e.strength).toBe(base.combat!.enemies[i].strength + 1))
  })
})

describe('古根の杯=ボスレリック化 (2026-09-03 本家 Coffee Dripper 型の代償)', () => {
  it('層は boss、焚き火で休んでも回復しない (鍛えるは使える)', () => {
    expect(getRelicDef('relic_oldroot_cup').rarity).toBe('boss')
    expect(getRelicDef('relic_oldroot_cup').bonus?.noRest).toBe(true)
    let run = intoCampfire({ ...createRun(11, 'set-confirm'), relics: ['relic_oldroot_cup'] })
    run = { ...run, hp: 30 }
    const rested = applyRunCommand(run, { type: 'CampfireRest' })
    expect(rested.hp).toBe(30)
    expect(rested.phase).toBe('map')
    const forged = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    expect(forged.deck.some((c) => c.def.name.endsWith('+'))).toBe(true)
  })
})

const allRelicsHas = (id: string) => allRelics.some((r) => r.id === id)

describe('在庫拡充 第1波 (2026-09-03 docs/relic-redesign-proposal.md §3-3。既存の仕組みで作れる15個)', () => {
  it('ショップ系: 会員証=半額・砥石の欠片=鍛える-25・除去の鑿=逓増なし (大工の道具は2026-09-05 撤去: 1幕1回・100Gの下で死に枠)', () => {
    const base = createRun(3, 'set-confirm')
    expect(shopRemovalPrice({ ...base, relics: ['relic_membership_card'] })).toBe(25)
    expect(shopUpgradePrice({ ...base, relics: ['relic_membership_card'] })).toBe(50)
    expect(shopUpgradePrice({ ...base, relics: ['relic_whetstone_chip'] })).toBe(75)
    expect(shopRemovalPrice({ ...base, relics: ['relic_removal_chisel'], removalCount: 2 })).toBe(50) // 逓増なし (2026-09-03 +25化に伴い作り直し)
    expect(workshopFusePrice(base)).toBe(100)
    expect(allRelicsHas('relic_carpenter_tools')).toBe(false)
  })
  it('薬研: 実際に休んだ時だけ最大HP+2 (古根の杯で休めない時は増えない)', () => {
    const run = { ...intoCampfire({ ...createRun(11, 'set-confirm'), relics: ['relic_mortar'] }), hp: 40 }
    const rested = applyRunCommand(run, { type: 'CampfireRest' })
    expect(rested.maxHp).toBe(run.maxHp + 2)
    expect(rested.hp).toBe(Math.min(rested.maxHp, 40 + 2 + Math.floor(run.maxHp * run.campfireRatio)))
    const blocked = applyRunCommand({ ...run, relics: ['relic_mortar', 'relic_oldroot_cup'] }, { type: 'CampfireRest' })
    expect(blocked.maxHp).toBe(run.maxHp)
  })
  it('金の靴と戦利品袋: エリート勝利のゴールドが (基礎+20)×1.5 になる', () => {
    const run = intoFirstElite(11)
    const plain = forceWin(run)
    const boosted = forceWin({ ...run, relics: ['relic_golden_boots', 'relic_loot_bag'] })
    expect(boosted.gold - run.gold).toBe(Math.floor((plain.gold - run.gold + 20) * 1.5))
  })
  it('回収の紐: 回収が0E。大樹の心: 上限参照が読む値+1。収穫の鎌: 放出後に成長2が残る', () => {
    const cord = injectedIntoBattle('relic_retrieve_cord', 11).combat!
    expect(cord.retrieveFree).toBe(true)
    let c = withHand({ ...freshCombat('set-confirm', 'enemy_probe', 1), retrieveFree: true }, ['green_reaction_thorns'])
    const uid = c.player.hand[0].uid
    c = applyCommand(c, { type: 'SetCard', cardUid: uid })
    const e0 = c.player.energy
    c = applyCommand(c, { type: 'RetrieveSetCard', cardUid: uid })
    expect(c.player.energy).toBe(e0)
    const heart = injectedIntoBattle('relic_great_tree_heart', 11).combat!
    const control = intoBattle(createRun(11, 'set-confirm')).combat!
    // このはのパッシブは energyMax を即時に+1するが、参照値 (ターン開始スナップショット) はT1は素の3。心はそこに+1
    expect(heart.player.energyMaxAtTurnStart).toBe(control.player.energyMaxAtTurnStart + 1)
    const sickle = injectedIntoBattle('relic_harvest_sickle', 11).combat!
    expect(sickle.harvestKeep).toBe(2)
  })
  it('不動の根: HP半分以下で始めた戦闘だけ開幕ブロック10', () => {
    const low = intoBattle({ ...createRun(11, 'set-confirm'), relics: ['relic_steadfast_root'], hp: 30 })
    expect(low.combat!.player.block).toBe(10)
    const full = injectedIntoBattle('relic_steadfast_root', 11)
    expect(full.combat!.player.block).toBe(0)
  })
  it('魔女の秤: 提示+2・最大HP-10 (現在HPも-10)', () => {
    let run = createRun(7, 'set-confirm')
    const hp0 = run.hp
    run = { ...run, phase: 'relic-reward', relicOptions: ['relic_witch_scale'], combat: null }
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.rewardChoicesBonus).toBe(2)
    expect(run.maxHp).toBe(80 - 10)
    expect(run.hp).toBe(hp0 - 10)
  })
})

describe('経済レリックの供給は幕1〜2まで (2026-09-03 ユーザー裁定)', () => {
  it('actMax=2 の3種は幕3の抽選に出ない (幕1では出うる)', () => {
    const eco = ['relic_loot_bag', 'relic_whetstone_chip', 'relic_old_purse']
    for (const id of eco) expect(getRelicDef(id).actMax).toBe(2)
    for (const seed of [1, 2, 3, 4, 5, 6, 7, 8]) {
      const run = { ...createRun(seed, 'set-confirm'), act: 3 }
      for (const src of ['chest', 'elite', 'shop', 'event'] as const) {
        for (const id of drawRelicOptions(run, src, 5)[0]) expect(eco).not.toContain(id)
      }
    }
  })
})
