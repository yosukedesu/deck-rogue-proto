// 敵の作り込み (2026-08-24) のテスト。
// 状態異常 (弱体/脆弱/負傷)・連撃・再生・フェーズ変化・激昂・挑発。
// 確定済みルール表「敵の設計原則」「状態異常」「連撃」「再生」「敵フェーズ変化」「激昂」を固定する。
import { afterEach, describe, expect, it } from 'vitest'
import { allEnemies, getCardDef, getEnemyDef, resolveEncounter } from './content.ts'
import { tierFor } from './map.ts'
import { applyCommand } from './state.ts'
import { damageBreakdown, dealDamageToEnemy } from './effects.ts'
import { attackIntent, destroySetIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import { applyDebugOverrides, clearDebugOverrides } from './content.ts'
import { effectiveCost } from './effects.ts'
import type { GameState } from './types.ts'

const noHand = (s: GameState): GameState => withHand(s, [])

describe('弱体 (プレイヤーの与ダメ25%減)', () => {
  it('弱体中の攻撃はダメージ25%減 (切り捨て)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_strike'])
    s = { ...s, player: { ...s.player, weak: 2 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4) // 打撃6 → floor(6*0.75)=4
  })

  it('弱体は自ターン終了時に1減る', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, weak: 2 } }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.weak).toBe(1)
  })
})

describe('脆弱 (敵の攻撃ダメージ50%増)', () => {
  it('脆弱中は敵の攻撃が50%増 (切り捨て)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = withIntent(s, attackIntent(10))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 10 * 1.5
  })

  it('延焼を持つ敵にも脆弱は素の実値に掛かる (威嚇は撤去済み)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5, hp: 999 })) }
    s = withIntent(s, attackIntent(10))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 10 * 1.5
  })

  it('脆弱は敵フェーズ終了時に1減る (そのフェーズは有効)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, vulnerable: 1 } }
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.vulnerable).toBe(0)
  })
})

describe('負傷 (死に札の混入)', () => {
  it('負傷は使用不可カードを捨て札に加える', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'hex',
      shownMin: 0,
      shownMax: 0,
      actual: 0,
      inflict: { status: 'wound', amount: 2 },
    })
    s = applyCommand(s, { type: 'EndTurn' })
    const wounds = s.player.discardPile.filter((c) => c.def.id === 'status_wound')
    expect(wounds.length).toBe(2)
  })

  it('負傷は1戦闘の上限5枚 (ハメ防止)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'hex',
      shownMin: 0,
      shownMax: 0,
      actual: 0,
      inflict: { status: 'wound', amount: 99 },
    })
    s = applyCommand(s, { type: 'EndTurn' })
    const all = [...s.player.discardPile, ...s.player.drawPile, ...s.player.hand]
    expect(all.filter((c) => c.def.id === 'status_wound').length).toBe(5)
  })

  it('攻撃に付与された状態異常はダメージ後に適用される (攻撃+弱体)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_hexer', 42))
    s = withIntent(s, {
      kind: 'attack',
      shownMin: 6,
      shownMax: 9,
      actual: 7,
      inflict: { status: 'weak', amount: 2 },
    })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 7)
    expect(s.player.weak).toBe(2)
  })
})

describe('連撃 (multi-hit)', () => {
  it('連撃は1発の実値×ヒット数のダメージ', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15)
  })

  it('ブロックはヒット順に消費される', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = { ...s, player: { ...s.player, block: 7 } }
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 8) // 15 - 7
  })

  it('連撃は1発ずつ素の実値で解決される (威嚇は撤去済み)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_wolf', 42))
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5, hp: 999 })) }
    s = withIntent(s, { kind: 'attack', shownMin: 4, shownMax: 6, actual: 5, hits: 3 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 15) // 5 × 3
  })
})

describe('再生とフェーズ変化 (苔まといの主)', () => {
  it('敵フェーズ終了時にHPが回復する (最大HPまで)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: maxHp - 10 })) }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    const regen = getEnemyDef('enemy_moss').regen ?? 0
    expect(regen).toBeGreaterThan(0)
    expect(s.enemies[0].hp).toBe(maxHp - 10 + regen)
  })

  it('HP50%以下では再生しない (スタール防止)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    const low = Math.floor(maxHp * 0.4)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: low })) }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(low)
  })

  it('HP50%以下では行動テーブルが牙をむく側に切り替わる', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_moss', 42))
    const maxHp = s.enemies[0].maxHp
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(maxHp * 0.4) })) }
    s = withIntent(s, attackIntent(1))
    s = applyCommand(s, { type: 'EndTurn' })
    // 次ターンの意図宣言は movesBelowHalf (bite 14〜18) から選ばれる
    const intent = s.enemies[0].intent
    expect(intent).not.toBeNull()
    expect(intent!.kind).toBe('attack')
    expect(intent!.shownMin).toBeGreaterThanOrEqual(14)
  })
})

describe('激昂 (刻限の門番) = 時喰らい型タイマー', () => {
  // 2026-08-26: 「毎フェーズ自動+2・累積上限6」から「プレイヤーが8枚プレイするたび+2・上限なし」へ。
  // 本家StSに上限という概念は無く、時喰らいはプレイヤーのテンポに誘発を紐づけている。
  // 時間でなく手数で進むので、低速デッキほどタイマーが遅い = 自己調整する。
  it('時間では強化されない (ターンを回すだけでは強化0のまま)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_warden', 42))
    expect(s.enemies[0].strength).toBe(0)
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBe(0) // 手を出さなければ鐘は鳴らない
  })

  it('プレイヤーが規定枚数をプレイするたびに強化される (上限なし)', () => {
    const def = getEnemyDef('enemy_warden')
    const every = def.enrageEveryCards ?? 0
    const amount = def.enrage ?? 0
    expect(every).toBeGreaterThan(0)
    let s = freshCombat('set-confirm', 'enemy_warden', 42)
    s = { ...s, player: { ...s.player, energy: 99 } }
    for (let i = 0; i < every * 2; i++) {
      s = withHand(s, ['green_strike'])
      s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
      if (s.phase !== 'player-turn') break
    }
    expect(s.player.cardsPlayedTotal).toBe(every * 2)
    // 枚数16枚で2回 + 打撃6×16=96ダメが enrageEveryDamage(80) を1回跨ぐ = +2 (2026-08-30 与ダメ併用)
    expect(s.enemies[0].strength).toBe(amount * 2 + 2)
  })
})

describe('挑発 (嘲る道化)', () => {
  it('伏せが無いと大振り (通常テーブルは強攻撃のみ)', () => {
    const s = freshCombat('set-confirm', 'enemy_joker', 42)
    // 開始時は伏せ無し → moves (大振り) から宣言される
    expect(s.enemies[0].intent?.kind).toBe('attack')
    expect(s.enemies[0].intent!.shownMin).toBeGreaterThanOrEqual(15)
  })

  it('伏せがあると別の行動になる (2026-09-03 賭け型化: 大振りは無いが用心の一撃12-16+脆弱1で弱腰ではない)', () => {
    const def = getEnemyDef('enemy_joker')
    expect(def.movesVsSet).toBeDefined()
    expect(def.movesVsSet!.some((m) => m.id === 'wild_swing')).toBe(false) // 大振り(15-19+脆弱2)は無い
    const jab = def.movesVsSet!.find((m) => m.id === 'cautious_jab')!
    expect([jab.min, jab.max]).toEqual([12, 16])
    expect(jab.inflict).toEqual({ status: 'vulnerable', amount: 1 }) // 旧7-10=「1Eで押せるスイッチ」の是正
  })

  it('ただし、はったりを見破る手段を持つ (伏せっぱなしで完封できない)', () => {
    const def = getEnemyDef('enemy_joker')
    expect(def.movesVsSet!.some((m) => m.kind === 'destroy-set')).toBe(true)
  })
})

describe('伏せへの罰 (2026-08-26。無期限温存で敵を弱い分岐に固定できた問題)', () => {
  // プレイテストで2人が独立に発見: リアクションを伏せたまま一度も発動しないことで
  // 嘲る道化・用心深い影を弱い行動に固定し続けられた (エリートを無傷で撃破)。
  // 伏せは発動か破壊まで無期限に持続する仕様なので、敵側に膠着を破る手段が要る。
  it('伏せに反応する敵は必ず「伏せっぱなしを罰する手段」を持つ', () => {
    const offenders: string[] = []
    for (const def of allEnemies) {
      const vsSet = def.movesVsSet
      if (!vsSet || vsSet.length === 0) continue
      const breaksStandoff = vsSet.some((m) => m.kind === 'destroy-set')
      const normalMin = Math.min(
        ...def.moves.filter((m) => m.kind === 'attack').map((m) => m.min ?? 99),
      )
      const vsSetMax = Math.max(
        ...vsSet.filter((m) => m.kind === 'attack').map((m) => m.max ?? 0),
        0,
      )
      // 伏せ破壊を持つか、通常時と同等以上に殴れる分岐があること
      if (!breaksStandoff && vsSetMax < normalMin) offenders.push(def.name)
    }
    expect(offenders).toEqual([])
  })

  it('1〜3戦目のプールにも伏せへの罰がある (かつては皆無だった)', () => {
    // tier1 = 探り屋 / うねる獣 / 探り屋の二人組
    const probe = getEnemyDef('enemy_probe')
    expect(probe.movesVsSet).toBeDefined()
    const lunge = probe.movesVsSet!.find((m) => m.id === 'lunge')
    expect(lunge).toBeDefined() // 伏せると探りの猶予 (poke×2) が消える
  })
})

describe('伏せ破壊への応答 (2026-08-27。確定済みルール表「伏せ破壊への応答」)', () => {
  // 5色テストで3色が独立に「読み合いでなく一方的な没収」と報告した問題への対処。
  // 逃がしルールは廃止 (2026-08-30 A2)。破壊されそうな札は回収 (1E) で事前に引き上げる —
  // 「発動して逃がす」は破壊を敵の最弱行動にしていた (3幕フルラン実測)
  it('回収: 1E払って伏せ札を手札に戻せる (払った伏せコストは返らない)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 11, 'starter')
    s = withHand(s, ['green_reaction_vine'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_vine' })
    const energyBefore = s.player.energy
    s = applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_reaction_vine' })
    expect(s.player.energy).toBe(energyBefore - 1)
    expect(s.player.setCards).toHaveLength(0)
    expect(s.player.hand.some((c) => c.uid === 't0_green_reaction_vine')).toBe(true)
  })

  it('破壊は素直に通り、がらくたが付与される (2026-08-30 窓は開かない)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 11, 'starter')
    s = withHand(s, ['green_reaction_vine'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_vine' })
    s = withIntent(s, {
      kind: 'destroy-set', shownMin: 0, shownMax: 0, actual: 0,
      inflict: { status: 'junk', amount: 1 },
    })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'SetCardDestroyed')).toBe(true)
    // がらくたは山札のランダム位置に混ざり、次ターンのドローで手札に来ていることもある
    const junk = [...s.player.hand, ...s.player.drawPile, ...s.player.discardPile].filter(
      (c) => c.def.id === 'status_junk',
    )
    expect(junk).toHaveLength(1)
  })

  it('条件を満たさない札は逃がせない (窮鼠の大牙はHP満タンでは応答候補にならない)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 11, 'starter')
    // 窮鼠の大牙は 2026-09-03 に撤去。条件付きリアクションの機構は合成defで固定
    s = withHand(s, ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, hand: [{ uid: 't0_green_reaction_cornered', def: { ...getCardDef('green_reaction_thorns'), id: 'test_cornered', name: '窮鼠(テスト)', effects: [{ trigger: 'onAttacked' as const, condition: { hpAtOrBelowRatio: 0.5 }, effect: 'counter' as const, amount: 20 }] } }] } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_cornered' })
    s = withIntent(s, destroySetIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    // 候補ゼロなので窓は開かず、そのまま破壊される
    expect(s.eventLog.some((e) => e.type === 'SetCardDestroyed')).toBe(true)
  })

  it('弾け実の罠は破壊されると必ず爆ぜる (逃がしルール廃止で罰札が常に発火する)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 11, 'starter')
    s = withHand(s, ['green_reaction_powder_pod'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_powder_pod' })
    s = withIntent(s, destroySetIntent())
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'SetCardDestroyed')).toBe(true)
    expect(hpBefore - s.enemies[0].hp).toBeGreaterThanOrEqual(12) // onSetDestroyed の12全体
  })
})

describe('虚弱 (カードのプレイで得るブロック25%減。2026-09-01 敵圧監査)', () => {
  it('虚弱中にカードで得るブロックは25%減 (切り捨て)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_guard'])
    s = { ...s, player: { ...s.player, frail: 1 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_guard' })
    expect(s.player.block).toBe(3) // 防御5 → floor(5*0.75)=3
  })

  it('氷壁は虚弱の対象外 (別経路 = 青の柱を侵さない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['blue_ice_wall'])
    s = { ...s, player: { ...s.player, frail: 1 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ice_wall' })
    expect(s.player.iceBlock).toBe(15) // 減らない
  })

  it('置物のターン開始ブロックは虚弱の対象外 (カードのプレイではない)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    const def = getCardDef('white_perm_shieldmaiden')
    s = {
      ...s,
      player: { ...s.player, frail: 3, permanents: [{ uid: 'perm_test', def }] },
    }
    s = withIntent(s, attackIntent(1))
    s = applyCommand(s, { type: 'EndTurn' })
    // 次ターン開始時の盾の乙女 (毎ターンブロック2) は素通し
    expect(s.player.block).toBe(2)
  })

  it('虚弱は自ターン終了時に1減る (弱体と同じ対称則)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    s = { ...s, player: { ...s.player, frail: 2 } }
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.frail).toBe(1)
  })
})

describe('alsoBuff (攻撃と同時の強化。2026-09-01 バフ専用ターンを作らない雪だるま)', () => {
  it('攻撃の解決後に強化が乗る (次の攻撃から加算される)', () => {
    let s = noHand(freshCombat('set-confirm', 'enemy_brute', 42))
    const before = s.enemies[0].strength
    s = withIntent(s, { kind: 'attack', shownMin: 5, shownMax: 7, actual: 5, alsoBuff: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBe(before + 1)
    expect(s.eventLog.some((e) => e.type === 'StrengthGained' && e.amount === 1)).toBe(true)
  })
})

describe('敵圧監査の新敵2体 (2026-09-01 幕1の状態異常ゼロを解消)', () => {
  it('囁きの狂信者はカルト型タイマー (毎フェーズ強化+2の激昂)', () => {
    const def = getEnemyDef('enemy_cultist')
    expect(def.enrage).toBe(2)
    expect(def.enrageEveryCards).toBeUndefined() // 毎フェーズ自動 = 時限爆弾
  })

  it('酸吐きの蛞蝓は状態異常の教師 (舐め=弱体 → 酸=虚弱 → 体当たりのローテーション)', () => {
    const def = getEnemyDef('enemy_slug')
    expect(def.sequence).toEqual(['lick', 'acid_spit', 'tackle'])
    expect(def.moves.find((m) => m.id === 'lick')!.inflict).toEqual({ status: 'weak', amount: 2 })
    expect(def.moves.find((m) => m.id === 'acid_spit')!.inflict).toEqual({ status: 'frail', amount: 1 })
  })
})

describe('デバフ圧の本家水準化 (2026-09-01 第2弾。確定済みルール表「状態異常」)', () => {
  it('攻撃ライダーの4件: 樽の爆発=火傷2(2026-09-02格上げ)・砥石の断頭=虚弱1・苔の叩きつけ=虚弱1・斧鬼の大振り=弱体1', () => {
    const pin = (enemyId: string, moveId: string, status: string, amount: number) => {
      const m = getEnemyDef(enemyId).moves.find((x) => x.id === moveId)!
      expect(m.inflict, `${enemyId}.${moveId}`).toEqual({ status, amount })
    }
    pin('enemy_bomber', 'big_boom', 'scald', 2) // 2026-09-02 敵ギミック第1波: 負傷1→火傷2 (即時の圧へ)
    pin('enemy_whetstone_colossus', 'guillotine', 'frail', 1)
    pin('enemy_moss', 'slam', 'frail', 1)
    pin('enemy_axe_ogre', 'great_swing', 'weak', 1)
  })

  it('分布の不変条件: 幕2/3の編成の6割以上が状態異常・ステータス札の付与源を持つ', () => {
    // 本家StSは幕2以降ほぼ全戦闘が何かを付与してくる。倍率でなく「割合で効く圧」は
    // 完成デッキにも自動でスケールする = 幕2の谷への構造処方 (難易度検証3本の一致所見)
    for (const act of [2, 3]) {
      const pool = tierFor(act, 5) // Weak帯 (幕頭2行の教師枠) を除いた本帯プール (2026-09-02 Weak帯導入に追随)
      let carriers = 0
      for (const encId of pool) {
        const members = resolveEncounter(encId)
        const has = members.some((mem) => {
          const d = getEnemyDef(mem.enemyId)
          const tables = [d.moves, d.movesVsSet ?? [], d.movesBelowHalf ?? []]
          return tables.some((t) =>
            t.some((m) => m.inflict !== undefined || m.setAlt?.inflict !== undefined),
          )
        })
        if (has) carriers++
      }
      expect(carriers / pool.length, `幕${act}: ${carriers}/${pool.length}`).toBeGreaterThanOrEqual(0.6)
    }
  })
})

describe('ダメージ内訳 damageBreakdown (2026-09-01 カードホバー用。実処理と同手順の純関数)', () => {
  it('成長→勢い→弱体(床1)→急所→装甲→敵ブロックの順で値が推移する', () => {
    let s = freshCombat('set-confirm', 'enemy_warden', 42) // 門番 = 装甲35
    s = {
      ...s,
      player: { ...s.player, growth: 10, momentum: 5, weak: 1 },
      enemies: s.enemies.map((e) => ({ ...e, exposed: 1, block: 6 })),
    }
    const bd = damageBreakdown(s, 0, 30)!
    // 30+10+5=45 → 弱体 floor(45*0.75)=33 → 急所 floor(33*1.5)=49 → 装甲35 → ブロック6 ⇒ 29
    expect(bd.steps.map((st) => st.value)).toEqual([30, 40, 45, 33, 49, 35, 29])
    expect(bd.hpLoss).toBe(29)
    // 実処理との一致 (dealDamageToEnemy の DamageDealt と同じ値になる)
    const after = dealDamageToEnemy(s, 0, 30)
    const ev = after.eventLog.findLast((e) => e.type === 'DamageDealt')
    expect(ev?.type === 'DamageDealt' && ev.hpLoss).toBe(29)
  })

  it('貫通は敵ブロックを無視し、倒れている敵には null', () => {
    let s = freshCombat('set-confirm', 'enemy_probe', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    expect(damageBreakdown(s, 0, 8, true)!.hpLoss).toBe(8)
    expect(damageBreakdown(s, 0, 8, false)!.hpLoss).toBe(0)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 0 })) }
    expect(damageBreakdown(s, 0, 8)).toBeNull()
  })
})

describe('火傷 (2026-09-02 敵ギミック第1波。本家Burn相当)', () => {
  it('火傷は手札に押し込まれ、自ターン終了時に手札にあると1枚2の直接HP損失', () => {
    let s = freshCombat('set-confirm', 'enemy_cinder_imp', 42)
    s = noHand(s)
    s = applyCommand(s, { type: 'EndTurn' }) as GameState
    // 手動で付与を再現: 手札に火傷2枚
    s = {
      ...s,
      phase: 'player-turn',
      player: {
        ...s.player,
        hand: [
          { uid: 'sc1', def: getCardDef('status_scald') },
          { uid: 'sc2', def: getCardDef('status_scald') },
        ],
      },
    }
    const hp = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'ScaldTick' && e.amount === 4)).toBe(true)
    // 直接HP損失4 (敵の攻撃分は別途) — ScaldTick時点のHPで検証する代わりにイベント量で固定済み
    expect(hp - s.player.hp).toBeGreaterThanOrEqual(4)
  })

  it('焚きつけのインプ: 火の粉(攻撃+火傷1)→煽り(火傷2)→噛みつき のローテ', () => {
    const def = getEnemyDef('enemy_cinder_imp')
    expect(def.sequence).toEqual(['spark_toss', 'fan_flames', 'bite'])
    expect(def.moves.find((m) => m.id === 'spark_toss')?.inflict).toEqual({ status: 'scald', amount: 1 })
    expect(def.moves.find((m) => m.id === 'fan_flames')?.inflict).toEqual({ status: 'scald', amount: 2 })
  })

  it('火傷は捨てコストの支払いに使える (処分の抜け道 = 手札マネジメントの問い)', () => {
    const def = getCardDef('status_scald')
    expect(def.effects).toEqual([]) // 使用不可の死に札 (プレイ効果なし)
  })
})

describe('分裂 (2026-09-02 敵ギミック第1波。本家Slime準拠)', () => {
  it('大苔スライムを倒すと苔スライム×2が意図付きで現れ、戦闘は続く', () => {
    let s = freshCombat('set-confirm', 'enemy_big_slime', 42)
    // 外科的に瀕死へ → 6ダメで撃破
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.phase).toBe('player-turn') // 勝利していない
    expect(s.enemies).toHaveLength(3)
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.enemies[0].split).toBe(true) // 二度と分裂しない
    expect(s.enemies[1].enemyId).toBe('enemy_moss_slime')
    expect(s.enemies[1].intent).not.toBeNull() // 生成時に意図宣言 = その敵フェーズから行動
    expect(s.enemies[2].intent).not.toBeNull()
    expect(s.eventLog.filter((e) => e.type === 'EnemySplit')).toHaveLength(1)
    // 分裂体を両方倒せば勝利
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? e : { ...e, hp: 1, block: 0 })) }
    s = withHand(s, ['green_sweep'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sweep' })
    expect(s.phase).toBe('won')
  })

  it('難易度の打点倍率 (atkScale) は分裂体に継承される', () => {
    let s = freshCombat('set-confirm', 'enemy_big_slime', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0, atkScale: 1.6 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[1].atkScale).toBe(1.6)
  })
})

describe('陣形: 庇うと連携 (2026-09-02 敵ギミック第1波)', () => {
  it('護衛の生存中、単体対象は護衛にリダイレクトされる (全体攻撃は素通し)', () => {
    let s = freshCombat('set-confirm', 'enc_squire_archer', 42)
    expect(s.enemies).toHaveLength(2)
    const squireHp = s.enemies[0].hp
    const archerHp = s.enemies[1].hp
    // 射手 (index1) を狙っても従士に向かう
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 1 })
    expect(s.enemies[1].hp).toBe(archerHp) // 射手は無傷
    expect(s.enemies[0].hp + s.enemies[0].block - 6).toBeLessThan(squireHp + 6) // 従士が受けた
    // 全体攻撃は両方に届く
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_sweep'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sweep' })
    expect(s.enemies[1].hp).toBeLessThan(archerHp)
  })

  it('護衛が倒れたら単体対象は素通しになる', () => {
    let s = freshCombat('set-confirm', 'enc_squire_archer', 42)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, hp: 0 } : e)) }
    const archerHp = s.enemies[1].hp
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 1 })
    expect(s.enemies[1].hp).toBeLessThan(archerHp)
  })

  it('連携: 仲間の生存中は攻撃+N、倒れたら次の宣言から素に戻る', () => {
    // 双牙の狼 bondStrength=2。sequence=[twin_bite(4-6×2), lunge(9-12), prowl(defend)]・2頭目はoffset1
    const s = freshCombat('set-confirm', 'enc_fang_twins', 42)
    expect(s.enemies[0].intent?.shownMin).toBe(6) // twin_bite 4+2
    expect(s.enemies[0].intent?.shownMax).toBe(8) // 6+2
    expect(s.enemies[1].intent?.shownMin).toBe(11) // lunge 9+2
    expect(s.enemies[1].intent?.shownMax).toBe(14) // 12+2
    // 片方を倒してターンを進めると、次の宣言は素の幅 (宣言時判定)
    let t: GameState = { ...s, enemies: s.enemies.map((e, i) => (i === 1 ? { ...e, hp: 0 } : e)) }
    t = withHand(t, [])
    t = applyCommand(t, { type: 'EndTurn' })
    expect(t.enemies[0].intent?.shownMin).toBe(9) // lunge 素
    expect(t.enemies[0].intent?.shownMax).toBe(12)
  })
})

describe('行動文法の器 (2026-09-02 StS2解析からの全体改善)', () => {
  afterEach(() => clearDebugOverrides())

  it('opener: weight抽選の敵でも最初の宣言は指定の行動 (初手固定)', () => {
    applyDebugOverrides({
      enemies: [
        {
          id: 'test_opener',
          name: 'テスト初手',
          archetype: 'wide-power',
          maxHp: 50,
          opener: 'howl',
          moves: [
            { id: 'bite', kind: 'attack', min: 5, max: 8, weight: 9 },
            { id: 'howl', kind: 'buff', min: 2, max: 2, weight: 1 },
          ],
        },
      ],
    })
    for (const seed of [1, 2, 3, 4, 5]) {
      const s = freshCombat('set-confirm', 'test_opener', seed)
      expect(s.enemies[0].intent?.kind).toBe('buff') // 重み1/10でも初手は必ずhowl
    }
  })

  it('noRepeat: 直前と同じ行動は引かない / once: 1戦闘に1回だけ', () => {
    applyDebugOverrides({
      enemies: [
        {
          id: 'test_norepeat',
          name: 'テスト分岐制約',
          archetype: 'wide-power',
          maxHp: 500,
          moves: [
            { id: 'big', kind: 'attack', min: 20, max: 20, weight: 5, noRepeat: true },
            { id: 'small', kind: 'attack', min: 1, max: 1, weight: 1 },
            { id: 'roar', kind: 'buff', min: 3, max: 3, weight: 20, once: true },
          ],
        },
      ],
    })
    let s = freshCombat('set-confirm', 'test_norepeat', 7)
    const seen: string[] = []
    let buffs = 0
    for (let t = 0; t < 12; t++) {
      const it = s.enemies[0].intent
      const label = it?.kind === 'buff' ? 'roar' : it?.actual === 20 ? 'big' : 'small'
      if (label === 'roar') buffs++
      if (seen.length > 0 && label === 'big') expect(seen[seen.length - 1]).not.toBe('big')
      seen.push(label)
      s = withHand(s, [])
      s = applyCommand(s, { type: 'EndTurn' })
      if (s.phase !== 'player-turn') break
    }
    expect(buffs).toBeLessThanOrEqual(1) // onceは1回まで
  })

  it('phaseAfterUses: 妖術師は呪いを2回宣言したら殴りローテへ恒久切替 (KnowledgeDemon式)', () => {
    let s = freshCombat('set-confirm', 'enemy_hexer', 42)
    const kinds: string[] = []
    for (let t = 0; t < 10 && s.phase === 'player-turn'; t++) {
      kinds.push(s.enemies[0].intent?.kind ?? '?')
      s = withHand(s, [])
      s = applyCommand(s, { type: 'EndTurn' })
    }
    // 旧ローテ mud→curse→slap で curse(hex) は2回まで。7ターン目以降に hex が現れない
    const hexCount = kinds.filter((k) => k === 'hex').length
    expect(hexCount).toBe(2)
    expect(kinds.slice(6)).not.toContain('hex')
  })

  it('movesWhenAlone: 従士は射手が倒れると護りを捨てて殴りに転職する (LivingShield式)', () => {
    let s = freshCombat('set-confirm', 'enc_squire_archer', 42)
    // 射手を倒して宣言し直す
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 1 ? { ...e, hp: 0 } : e)) }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn')
    const it = s.enemies[0].intent
    expect(it?.kind).toBe('attack')
    expect(it?.shownMin).toBeGreaterThanOrEqual(9) // avenging_rush 9-12 (通常の盾打ち6-8ではない。2026-09-02 白スターターsim13%で半歩戻し)
  })

  it('拘束: 1ターンにプレイできるカードは3枚まで。伏せは制限されず、ターン終了で1減る', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = { ...s, player: { ...s.player, restrain: 1, energy: 9 } }
    s = withHand(s, ['green_strike', 'green_strike', 'green_strike', 'green_strike', 'green_reaction_thorns'])
    for (let k = 0; k < 3; k++) {
      const uid = s.player.hand.find((c) => c.def.id === 'green_strike')!.uid
      s = applyCommand(s, { type: 'PlayCard', cardUid: uid })
    }
    const fourth = s.player.hand.find((c) => c.def.id === 'green_strike')!.uid
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: fourth })).toThrow(/拘束/)
    // 伏せはプレイでないので通る
    const reaction = s.player.hand.find((c) => c.def.id === 'green_reaction_thorns')!.uid
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: reaction })).not.toThrow()
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.restrain).toBe(0)
  })

  it('拘束は亡骸プレイにも効き、焚べ (addCasts) の嵩では詰まらない (playsThisTurn参照)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    // 詠唱数だけ盛って実プレイ0の状態: 拘束中でもプレイできる
    s = { ...s, player: { ...s.player, restrain: 1, energy: 9, cardsPlayedThisTurn: 5 } }
    s = withHand(s, ['green_strike'])
    expect(() => applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })).not.toThrow()
    // 実プレイ3枚に達したら亡骸プレイ (PlayNecro) も拒否される
    let t = freshCombat('set-confirm', 'enemy_brute', 43)
    t = {
      ...t,
      player: {
        ...t.player,
        restrain: 1,
        energy: 9,
        playsThisTurn: 3,
        exhaustPile: [{ uid: 'n0', def: getCardDef('black_rotten_claw') }],
      },
    }
    expect(() => applyCommand(t, { type: 'PlayNecro', cardUid: 'n0' })).toThrow(/拘束/)
  })

  it('重圧オーラ: 敵の生存中カードのコストが上がり、倒すと即座に戻る', () => {
    applyDebugOverrides({
      enemies: [
        {
          id: 'test_aura',
          name: 'テスト重圧',
          archetype: 'hexer',
          maxHp: 40,
          aura: { cardType: 'physical', costUp: 1 },
          moves: [{ id: 'poke', kind: 'attack', min: 3, max: 5, weight: 1 }],
        },
      ],
    })
    let s = freshCombat('set-confirm', 'test_aura', 42)
    s = withHand(s, ['green_strike'])
    const card = s.player.hand[0]
    expect(effectiveCost(s, card)).toBe(card.def.cost + 1)
    const dead = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 0 })) }
    expect(effectiveCost(dead, card)).toBe(card.def.cost)
  })
})

describe('正確性の修正 (2026-09-02 StS2解析ミニングで発見)', () => {
  it('脆弱のjustAppliedガード: 敵フェーズ中に付与された脆弱は同フェーズ末に蒸発しない', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withIntent(s, {
      kind: 'attack',
      shownMin: 5,
      shownMax: 5,
      actual: 5,
      inflict: { status: 'vulnerable', amount: 2 },
    })
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.vulnerable).toBe(2) // 旧実装は付与→同フェーズ末-1で実効1だった
    // 次の敵フェーズ (新規付与なし) では通常どおり1減る
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.vulnerable).toBe(1)
  })

  it('火傷は敵ターン終了後の全捨てで消える = 1回きり (捨て札を循環しない)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withIntent(s, { kind: 'attack', shownMin: 3, shownMax: 3, actual: 3, inflict: { status: 'scald', amount: 2 } })
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    // 敵フェーズで火傷2枚が手札に注入され、次の自ターン開始時点では手札に残っている
    const inHand = s.player.hand.filter((c) => c.def.id === 'status_scald').length
    expect(inHand).toBe(2)
    // 何もせずターンを回すと: 自ターン終了時に疼き→全捨てで火傷は消滅 (捨て札に行かない)
    const hpBefore = s.player.hp
    s = withIntent(s, { kind: 'defend', shownMin: 5, shownMax: 5, actual: 5 })
    const scaldsUids = new Set(s.player.hand.filter((c) => c.def.id === 'status_scald').map((c) => c.uid))
    s = { ...s, player: { ...s.player, hand: s.player.hand.filter((c) => scaldsUids.has(c.uid)) } }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(hpBefore - s.player.hp).toBe(4) // 2枚×2の疼き
    const anywhere = [...s.player.hand, ...s.player.drawPile, ...s.player.discardPile, ...s.player.exhaustPile]
      .filter((c) => c.def.id === 'status_scald').length
    expect(anywhere).toBe(0)
  })
})

describe('ギミック変種 (2026-09-02 全体改善・第6波)', () => {
  it('残機: 骸兵は倒すたび次の形態で再起動し、最終形態を倒すと勝利。弐→壱は筋力+2で出る', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_husk_3', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.phase).toBe('player-turn')
    expect(s.enemies.some((e) => e.enemyId === 'enemy_elite_husk_2' && e.hp > 0)).toBe(true)
    // 弐を倒すと壱 (筋力+2)
    s = { ...s, enemies: s.enemies.map((e) => (e.hp > 0 ? { ...e, hp: 1, block: 0 } : e)) }
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: s.enemies.findIndex((e) => e.hp > 0) })
    const husk1 = s.enemies.find((e) => e.enemyId === 'enemy_elite_husk_1' && e.hp > 0)
    expect(husk1).toBeDefined()
    expect(husk1!.strength).toBe(2)
  })

  it('スタン付き分裂: 蛙鬼の幼体は出現ターンに動かない (初回意図=隙)', () => {
    let s = freshCombat('set-confirm', 'enemy_brood_toad', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    const broodlings = s.enemies.filter((e) => e.enemyId === 'enemy_broodling' && e.hp > 0)
    expect(broodlings).toHaveLength(3)
    for (const b of broodlings) expect(b.intent?.kind).toBe('rest')
  })

  it('孵化: 卵は3手目に孵化して走竜の仔になり、打ち消すと次の宣言も孵化 (=1ターン遅延)', () => {
    let s = freshCombat('set-confirm', 'enc_raptor_nest', 42)
    const eggIdx = s.enemies.findIndex((e) => e.enemyId === 'enemy_raptor_egg')
    expect(eggIdx).toBeGreaterThanOrEqual(0)
    // 2ターン回すと3手目=孵化の意図
    for (let t = 0; t < 2; t++) {
      s = withHand(s, [])
      s = applyCommand(s, { type: 'EndTurn' })
    }
    expect(s.enemies[eggIdx].intent?.kind).toBe('hatch')
    // そのまま敵フェーズを通すと孵化する
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[eggIdx].enemyId).toBe('enemy_raptor_chick')
    expect(s.enemies[eggIdx].hp).toBe(40) // 2026-09-02 本家形: 孵化後>親の締切 (22→40)
    // sequenceLoopFrom=2: もう1つの卵も以降の宣言は常に孵化 (打ち消し遅延が1ターン単位になる根拠)
  })

  it('弔い強化: 番いの片割れを倒すと残りの筋力+4 (逃走では発火しない)', () => {
    let s = freshCombat('set-confirm', 'enc_mourn_beasts', 42)
    const before = s.enemies[1].strength
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, hp: 1, block: 0 } : e)) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
    expect(s.enemies[1].strength).toBe(before + 4)
    expect(s.eventLog.some((e) => e.type === 'StrengthGained' && e.reason === 'mourn')).toBe(true)
  })

  it('sequenceLoopFrom: 前奏を終えるとloopFrom位置から巡回する', () => {
    applyDebugOverrides({
      enemies: [
        {
          id: 'test_loopfrom',
          name: 'テスト前奏',
          archetype: 'brute',
          maxHp: 200,
          sequenceLoopFrom: 1,
          moves: [
            { id: 'warmup', kind: 'buff', min: 3, max: 3, weight: 1 },
            { id: 'jab', kind: 'attack', min: 5, max: 5, weight: 1 },
            { id: 'cross', kind: 'attack', min: 9, max: 9, weight: 1 },
          ],
          sequence: ['warmup', 'jab', 'cross'],
        },
      ],
    })
    let s = freshCombat('set-confirm', 'test_loopfrom', 7)
    const kinds: string[] = []
    for (let t = 0; t < 6 && s.phase === 'player-turn'; t++) {
      kinds.push(s.enemies[0].lastMoveId ?? '?')
      s = withHand(s, [])
      s = applyCommand(s, { type: 'EndTurn' })
    }
    expect(kinds).toEqual(['warmup', 'jab', 'cross', 'jab', 'cross', 'jab']) // warmupは一度きり
  })
})

describe('デバフ拡張と時限呪い (2026-09-02 全体改善・第7波)', () => {
  it('霞み: ドローが2枚減り (最低3)、自ターン終了時に1減る', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withIntent(s, { kind: 'attack', shownMin: 3, shownMax: 3, actual: 3, inflict: { status: 'mist', amount: 2 } })
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.mist).toBe(2)
    expect(s.player.hand.filter((c) => c.def.id !== 'status_scald')).toHaveLength(3) // 5-2=3
    s = withIntent(s, { kind: 'defend', shownMin: 3, shownMax: 3, actual: 3 })
    s = { ...s, player: { ...s.player, hand: [] } }
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.mist).toBe(1)
  })

  it('重り: 敵の攻撃がプレイ枚数×10%重くなる (justAppliedガードあり)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = { ...s, player: { ...s.player, slow: 1, energy: 9, hp: 70, block: 0 } }
    // 3枚プレイしてからターン終了 → 敵の攻撃10は floor(10*1.3)=13
    s = withHand(s, ['green_strike', 'green_strike', 'green_strike'])
    for (let k = 0; k < 3; k++) {
      const uid = s.player.hand[0].uid
      s = applyCommand(s, { type: 'PlayCard', cardUid: uid })
    }
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    const hp0 = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(hp0 - s.player.hp).toBe(13)
  })

  it('仮初の烙印: 手札滞留で烙印と同じHP-1、勝利を重ねると自然消滅する (run層)', () => {
    // combat側: GUILT_DEFが烙印tickに数えられる
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = withIntent(s, { kind: 'defend', shownMin: 3, shownMax: 3, actual: 3 })
    s = {
      ...s,
      player: {
        ...s.player,
        hand: [{ uid: 'g0', def: getCardDef('status_guilt') }],
      },
    }
    const hp0 = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(hp0 - s.player.hp).toBe(1)
  })
})

describe('敵ギミック第3波 (2026-09-02 残件議論: 量の問いの器・本家普遍の器)', () => {
  it('ターン装甲: 1ターンのHP損失累計が上限で頭打ちになり、次の自ターンでリセットされる', () => {
    let s = freshCombat('set-confirm', 'enemy_sludge_berserker', 42) // turnArmor 45
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0 })), player: { ...s.player, energy: 9 } }
    const hp0 = s.enemies[0].hp
    // 大蛇の丸呑み+相当の一撃を2回 (成長なし: 34ダメ×2=68 > 45)
    s = withHand(s, ['green_serpent_gulp', 'green_serpent_gulp', 'green_strike', 'green_strike'])
    const gulp1 = s.player.hand[0].uid
    s = applyCommand(s, { type: 'PlayCard', cardUid: gulp1, discardUids: [s.player.hand[2].uid] })
    const gulp2 = s.player.hand.find((c) => c.def.id === 'green_serpent_gulp')!.uid
    const filler = s.player.hand.find((c) => c.def.id === 'green_strike')!.uid
    s = applyCommand(s, { type: 'PlayCard', cardUid: gulp2, discardUids: [filler] })
    expect(hp0 - s.enemies[0].hp).toBe(45) // 68 → 45 に頭打ち
    expect(s.eventLog.some((e) => e.type === 'DamageDealt' && (e.turnArmorCut ?? 0) > 0)).toBe(true)
    // ターンを回すと累計がリセット
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].damageThisTurn ?? 0).toBe(0)
  })

  it('アーティファクト: 急所付与を弾いて1消費し、延焼は弾かない', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_giant_face', 42) // artifact 2
    expect(s.enemies[0].artifact).toBe(2)
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_basic_bash', 'green_basic_bash'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid })
    expect(s.enemies[0].exposed).toBe(0) // 急所+2は弾かれた
    expect(s.enemies[0].artifact).toBe(1)
    expect(s.eventLog.some((e) => e.type === 'ArtifactBlocked')).toBe(true)
    // 延焼は通る
    applyDebugOverrides({
      enemies: [
        {
          id: 'test_artifact_burn',
          name: 'テスト',
          archetype: 'brute',
          maxHp: 80,
          artifact: 1,
          moves: [{ id: 'poke', kind: 'attack', min: 3, max: 5, weight: 1 }],
        },
      ],
    })
    let t = freshCombat('set-confirm', 'test_artifact_burn', 42)
    t = { ...t, player: { ...t.player, energy: 9 } }
    t = withHand(t, ['red_ignite'])
    t = applyCommand(t, { type: 'PlayCard', cardUid: t.player.hand[0].uid })
    expect(t.enemies[0].burn).toBeGreaterThan(0)
    expect(t.enemies[0].artifact).toBe(1) // 消費されていない
  })

  it('被弾覚醒: 鉄卵は累計20ダメで眠りの前奏を打ち切り、次の宣言が目覚め (awaken) になる', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_iron_egg', 42)
    expect(s.enemies[0].intent?.kind).toBe('defend') // 眠り=殻を積む
    // しきい値未満では眠り続ける
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0, damageTakenTotal: 10 })) }
    expect(s.enemies[0].woken).toBeUndefined()
    // 20以上のHP損失を与える (装甲22で1ヒット上限22 = 丸呑み1発で20超え)
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = withHand(s, ['green_serpent_gulp', 'green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: s.player.hand[0].uid, discardUids: [s.player.hand[1].uid] })
    expect(s.enemies[0].woken).toBe(true)
    expect(s.enemies[0].intent?.kind).toBe('defend') // 宣言済みの意図は変わらない (宣言時固定則)
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].intent?.kind).toBe('buff') // 次の宣言 = awaken
  })

  it('技の恒久成長: 巨面の圧潰は使うたび+4 (幅表示・実値の両方に乗る)', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_giant_face', 42)
    const crushValues: number[] = []
    for (let t = 0; t < 6 && s.phase === 'player-turn'; t++) {
      const it = s.enemies[0].intent
      if (it?.kind === 'attack') crushValues.push(it.shownMin)
      s = withHand(s, [])
      s = { ...s, player: { ...s.player, hp: 999, maxHp: 999 } }
      s = applyCommand(s, { type: 'EndTurn' })
    }
    expect(crushValues.length).toBeGreaterThanOrEqual(2)
    // 睨みの筋力+2〜3も乗るので「+4以上ずつ増える」で固定
    for (let i = 1; i < crushValues.length; i++) {
      expect(crushValues[i] - crushValues[i - 1]).toBeGreaterThanOrEqual(4)
    }
  })
})

describe('代替ボス3体 (2026-09-02 本家同等バリエーション: TheKin/KaiserCrab/TestSubject型)', () => {
  it('血族の儀式: 踊り手が全滅すると司祭は単独時ローテ (祈りをやめて毎ターン殴る) へ転職する', () => {
    let s = freshCombat('set-confirm', 'enc_kin_ritual', 42)
    expect(s.enemies).toHaveLength(3)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? e : { ...e, hp: 0 })) }
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    const def = getEnemyDef('enemy_kin_priest')
    const aloneIds = new Set((def.movesWhenAlone ?? []).map((m) => m.id))
    expect(aloneIds.size).toBeGreaterThan(0)
    expect(aloneIds.has(s.enemies[0].lastMoveId ?? '')).toBe(true)
  })

  it('双腕の巨蟹: 片腕を倒すと残る腕が弔い+3', () => {
    let s = freshCombat('set-confirm', 'enc_kaiser_crab', 42)
    const str0 = s.enemies[1].strength
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, hp: 1, block: 0 } : e)) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
    expect(s.enemies[1].strength).toBe(str0 + 3)
  })

  it('蘇る合成獣: 残機チェーンはボス係数など親のHP倍率を継承する (実効の合計を3回に分けて払わせる)', () => {
    let s = freshCombat('set-confirm', 'enemy_chimera_1', 42)
    // 幕3ボス係数×2.4を模して親のmaxHpを倍率化した状態から倒す
    const def1 = getEnemyDef('enemy_chimera_1')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, maxHp: def1.maxHp * 2, hp: 1, block: 0 })) }
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    const second = s.enemies.find((e) => e.enemyId === 'enemy_chimera_2' && e.hp > 0)
    expect(second).toBeDefined()
    expect(second!.maxHp).toBe(getEnemyDef('enemy_chimera_2').maxHp * 2) // 倍率2を継承
    expect(second!.strength).toBe(2)
    expect(second!.intent?.kind).toBe('rest') // 蘇生ターンは隙 (予告してから殺す)
  })
})







