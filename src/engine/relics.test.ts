// エリートノードとレリック (2026-08-25。2026-08-28 マップ化) のテスト。
// 確定済みルール表「エリート挑戦オファー」「レリック」と docs/relics-design.md を固定する。
import { describe, expect, it } from 'vitest'
import { allRelics, buildRelicPermanent, getCardDef, getEventDef, getRelicDef, getEnemyDef, resolveEncounter } from './content.ts'
import { applyRunCommand, createRun, currentNode } from './run.ts'
import type { RunState } from './run.ts'
import { applyCommand } from './state.ts'
import { startCombatWithOptions } from './combat.ts'
import { attackIntent, chooseToward, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

function forceWin(run: RunState): RunState {
  const c = run.combat!
  let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
  surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
  surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
  return applyRunCommand(
    { ...run, combat: surgical },
    { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
  )
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

  it('A型 (賢者の巻物): 戦闘開始時に2枚ドロー = 初手が2枚多い', () => {
    const run = withRelicIntoBattle('relic_sage_scroll')
    const c = run.combat!
    expect(c.player.hand.length).toBe(c.player.drawPerTurn + 2)
  })

  it('A型 (先手の盾): 戦闘開始時にブロック+8', () => {
    const run = withRelicIntoBattle('relic_vanguard_shield')
    expect(run.combat!.player.block).toBe(8)
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
    expect(run.rewardOptions).toHaveLength(4 + 1) // リーダー基本4 + 鞄1
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

  it('在庫は18個・IDは一意 (第二弾拡充 2026-08-29)', () => {
    expect(allRelics).toHaveLength(18)
    expect(new Set(allRelics.map((r) => r.id)).size).toBe(18)
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
  it('成長の種: 戦闘開始時に成長+2 (リーダーパッシブとは別枠で加算)', () => {
    const base = injectedIntoBattle('relic_thorn_crown') // 成長に触らない対照 (このはの毎T+1のみ)
    const run = injectedIntoBattle('relic_growth_seed')
    expect(run.combat!.player.growth).toBe(base.combat!.player.growth + 2)
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

  it('鍛冶の砥石あり: 2枚まで鍛えられる。除去との併用は不可 (1種類の原則)', () => {
    let run = { ...intoCampfire(createRun(11, 'set-confirm')), campfireForgeBonus: 1 }
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 0 })
    expect(run.phase).toBe('campfire') // 1枚目の後も留まる
    expect(() => applyRunCommand(run, { type: 'CampfireRemove', index: 1 })).toThrow('すでに鍛えている')
    run = applyRunCommand(run, { type: 'CampfireUpgrade', index: 1 })
    expect(run.phase).toBe('map') // 2枚目で出る
    expect(run.deck.filter((c) => c.def.name.endsWith('+'))).toHaveLength(2)
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
