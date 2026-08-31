// 敵の作り込み (2026-08-24) のテスト。
// 状態異常 (弱体/脆弱/負傷)・連撃・再生・フェーズ変化・激昂・挑発。
// 確定済みルール表「敵の設計原則」「状態異常」「連撃」「再生」「敵フェーズ変化」「激昂」を固定する。
import { describe, expect, it } from 'vitest'
import { allEnemies, getCardDef, getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, destroySetIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
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

  it('伏せがあると用心する (movesVsSet に大振りは無い)', () => {
    const def = getEnemyDef('enemy_joker')
    expect(def.movesVsSet).toBeDefined()
    const attacks = def.movesVsSet!.filter((m) => m.kind === 'attack')
    const maxAttack = Math.max(...attacks.map((m) => m.max ?? 0))
    const wildMin = Math.min(...def.moves.filter((m) => m.kind === 'attack').map((m) => m.min ?? 99))
    expect(maxAttack).toBeLessThan(wildMin) // 用心時の最大値 < 大振りの最小値
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
    s = withHand(s, ['green_reaction_cornered']) // HP半分以下でのみ発動可
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
