// 敵拡充+6体と新機構 (2026-08-29 ユーザー指示「敵の面白さのクオリティを上げたい・種類も増やしたい」) のテスト。
// 確定済みルール表「とげ（敵の報復）」「盗みと逃走」「回復役（敵）」「攻防一体・隙」「ランの敵並び」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { getEnemyDef, resolveEncounter } from './content.ts'
import { applyRunCommand, createRun } from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { EnemyIntent, GameState } from './types.ts'

function intent(partial: Partial<EnemyIntent> & { kind: EnemyIntent['kind'] }): EnemyIntent {
  return { shownMin: 0, shownMax: 0, actual: 0, ...partial }
}

describe('とげ (敵の報復。針毛の栗鼠)', () => {
  it('攻撃ヒットごとに反射する = 多段ヒットはヒット数ぶん痛い', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thorn_squirrel', 42), ['green_sig_vine_dance'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sig_vine_dance' })
    expect(s.player.hp).toBe(hpBefore - 2 * 5) // 2×5ヒット、各ヒットにとげ2
  })

  it('そのヒットで敵が倒れたら反射しない = 一撃で抜けば無傷', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thorn_squirrel', 42), ['green_strike'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 3 })) }
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.player.hp).toBe(hpBefore) // 反射なし
  })

  it('反射はプレイヤーのブロックで防げる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thorn_squirrel', 42), ['green_strike'])
    s = { ...s, player: { ...s.player, block: 5 } }
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    expect(s.player.hp).toBe(hpBefore)
    expect(s.player.block).toBe(3)
  })
})

describe('盗みと逃走 (こそ泥ゴブリン)', () => {
  it('盗み: ロール額を敵が抱え込む (ゴールドはまだ減らない = combat層は金を知らない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thief', 42), [])
    s = withIntent(s, intent({ kind: 'steal-gold', shownMin: 15, shownMax: 25, actual: 20 }))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].stolenGold).toBe(20)
  })

  it('逃走: hp0+fledで離脱し、最後の1体なら戦闘は勝利で終わる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thief', 42), [])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, stolenGold: 20 })) }
    s = withIntent(s, intent({ kind: 'flee' }))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].fled).toBe(true)
    expect(s.enemies[0].hp).toBe(0)
    expect(s.phase).toBe('won')
  })

  it('打ち消しで逃走を阻止できる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_thief', 42), [])
    s = { ...s, negateNextAction: true }
    s = withIntent(s, intent({ kind: 'flee' }))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].fled).not.toBe(true)
    expect(s.enemies[0].hp).toBeGreaterThan(0)
  })

  it('ローテーションは4拍 (隠れ身→盗み→小突き→逃走) で逃走が最後', () => {
    const def = getEnemyDef('enemy_thief')
    expect(def.sequence).toEqual(['sneak', 'snatch', 'mug', 'run_off'])
    expect(def.moves.find((m) => m.id === 'run_off')!.kind).toBe('flee')
  })
})

describe('盗みの精算 (run層。確定済みルール表「盗みと逃走」)', () => {
  /** 次の戦闘に入り、combat を書き換えてから全滅させて勝利する */
  function winWith(seed: number, mutate: (c: GameState) => GameState): RunState {
    let run = createRun(seed, 'set-confirm')
    let guard = 0
    while (run.phase !== 'combat' && guard++ < 40) {
      if (run.phase === 'map') run = chooseToward(run, 'battle')
      else break
    }
    let c = mutate(run.combat!)
    c = withIntent(withHand(c, ['green_sweep']), intent({ kind: 'defend', actual: 0 }))
    c = { ...c, player: { ...c.player, energy: 9 } }
    return applyRunCommand(
      { ...run, combat: c },
      { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
    )
  }

  it('逃げる前に倒した盗人: ゴールドは失われず懸賞金+10G', () => {
    const base = winWith(31, (c) => ({ ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }))
    const bounty = winWith(31, (c) => ({
      ...c,
      enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0, stolenGold: 20 })),
    }))
    expect(bounty.gold).toBe(base.gold + 10 * base.combat!.enemies.length)
  })

  it('逃走された盗人: 抱えた額を失う (最低0)', () => {
    const base = winWith(31, (c) => ({ ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }))
    const robbed = winWith(31, (c) => ({
      ...c,
      enemies: c.enemies.map((e, i) =>
        i === 0
          ? { ...e, hp: 1, block: 0 }
          : { ...e, hp: 0, fled: true, stolenGold: 25 },
      ),
    }))
    // 編成が1体のみのシードなら fled 敵がいない = 差0。2体以上なら25失う
    const fledCount = robbed.combat!.enemies.filter((e) => e.fled === true).length
    expect(robbed.gold).toBe(Math.max(0, base.gold - 25 * fledCount))
  })
})

describe('回復役 (苔の癒し手) と 攻防一体・隙', () => {
  it('heal: 最もHP割合の低い生存味方を回復する (最大HPまで)', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_bomber_healer', 42), [])
    // 0=火薬樽 (大きく負傷) / 1=癒し手。癒し手の行動を heal にする
    s = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, hp: 10 }
          : { ...e, intent: intent({ kind: 'heal', shownMin: 8, shownMax: 12, actual: 10 }) },
      ),
    }
    s = withIntent(s, intent({ kind: 'defend', actual: 0 }))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(20) // 火薬樽が+10
  })

  it('alsoDefend: 攻撃と同時に固定ブロックを得る (門番の改修と石殻の番人)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_shell_guard', 42), [])
    s = withIntent(s, intent({ kind: 'attack', shownMin: 12, shownMax: 16, actual: 14, alsoDefend: 14 }))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 14)
    expect(s.enemies[0].block).toBeGreaterThanOrEqual(14) // 攻撃と同時に14 (次の意図で上乗せの可能性)
  })

  it('rest (隙): 何も起きずに手番が終わる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_axe_ogre', 42), [])
    s = withIntent(s, intent({ kind: 'rest' }))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore)
  })

  it('門番の防御ターンは「守りつつ鐘を打つ」(攻撃6〜8+ブロック12) = 無料の殴りターンではない', () => {
    const gate = getEnemyDef('enemy_warden').moves.find((m) => m.id === 'gate_guard')!
    expect(gate.kind).toBe('attack')
    expect(gate.alsoDefend).toBe(12)
  })
})

describe('防御割合の監査 (2026-08-29 ユーザー指摘「既存敵の防御選択割合が低くない？」)', () => {
  it('うねる獣: 防御重み1→2 (40%)', () => {
    const coil = getEnemyDef('enemy_wide_power').moves.find((m) => m.id === 'coil')!
    expect(coil.weight).toBe(2)
  })

  it('罠壊し・道化: 通常テーブルに防御行動を持つ', () => {
    expect(getEnemyDef('enemy_set_breaker').moves.some((m) => m.kind === 'defend')).toBe(true)
    expect(getEnemyDef('enemy_joker').moves.some((m) => m.kind === 'defend')).toBe(true)
  })
})

describe('幕プールの拡充 (敵18体・編成10種)', () => {
  it('新敵6体と新編成4種が定義済みで解決できる', () => {
    for (const id of [
      'enemy_thorn_squirrel',
      'enemy_thief',
      'enemy_bomber',
      'enemy_moss_healer',
      'enemy_axe_ogre',
      'enemy_shell_guard',
    ]) {
      expect(getEnemyDef(id).name.length).toBeGreaterThan(0)
    }
    for (const id of ['enc_thief_pair', 'enc_bomber_healer', 'enc_axe_drummer', 'enc_shell_hexer']) {
      expect(resolveEncounter(id).length).toBeGreaterThanOrEqual(2)
    }
  })

  it('苔の癒し手はソロプールに入らない (編成専用 = ソロ自己回復のスタール防止)', async () => {
    const { tierFor } = await import('./map.ts')
    for (const act of [1, 2, 3]) {
      expect(tierFor(act, 3)).not.toContain('enemy_moss_healer')
    }
  })
})

describe('幕1ボスの第2形態 (2026-08-30 Opusテスターの指摘「サンドバッグ化」への処方)', () => {
  it('オーガのHP半分以下は固定シーケンス = 連続バフの事故が構造的に起きない', () => {
    const def = getEnemyDef('enemy_brute')
    // 重み抽選だと「追い詰めた瞬間に雄叫びを連続で引く」事故が起きる (実プレイで4ターン中3バフを観測)。
    // 「起こしてしまったら、もう眠らない」= 乱打→乱打→雄叫び の見境なしを固定する
    expect(def.sequenceBelowHalf).toEqual(['rage_flurry', 'rage_flurry', 'war_roar'])
  })
})

describe('発火保証パッケージ (2026-08-30。3幕フルラン実測「設計が発火する前に敵が死ぬ」への処方)', () => {
  it('盗みは宣言と同時に成立する (宣言ターンに倒しても盗みのレースは発生している)', () => {
    let s = freshCombat('set-confirm', 'enemy_thief', 42)
    // こそ泥の意図が steal-gold になるまでターンを送る (初手が盗みでないシードもある)
    for (let i = 0; i < 6 && s.enemies[0].intent?.kind !== 'steal-gold'; i++) {
      s = withIntent(s, { kind: 'defend', shownMin: 1, shownMax: 1, actual: 1 })
      s = applyCommand(s, { type: 'EndTurn' })
    }
    if (s.enemies[0].intent?.kind === 'steal-gold') {
      // 宣言の時点で既に抱えている = 実行を待たない
      expect(s.enemies[0].stolenGold ?? 0).toBeGreaterThan(0)
      expect(s.eventLog.some((e) => e.type === 'GoldStolen')).toBe(true)
    }
  })

  it('開幕ブロック: 大亀・門番・火薬樽・石殻は戦闘開始時からブロックを持つ (T1から見える問い)', () => {
    for (const [id, block] of [
      ['enemy_turtle', 10],
      ['enemy_warden', 15],
      ['enemy_bomber', 12],
      ['enemy_shell_guard', 10],
    ] as const) {
      const s = freshCombat('set-confirm', id, 42)
      expect(s.enemies[0].block, id).toBe(block)
    }
  })

  it('激昂の与ダメ併用: 門番は累計80ダメージを跨ぐたびに強化+2 (高火力1枚デッキも鳴らす)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_warden', 42), ['green_fang'])
    s = { ...s, player: { ...s.player, growth: 70, energy: 9 } } // 14+70=84 = 80を1回跨ぐ
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0, armor: undefined })) } // 開幕ブロック・装甲を外して素の判定に
    const strBefore = s.enemies[0].strength
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    expect(s.enemies[0].strength).toBe(strBefore + 2)
  })
})

describe('装甲 (2026-08-30 n²スケーリングへのワクチン)', () => {
  it('1ヒットの被ダメは装甲値で頭打ちになる (急所・成長込みの最終値に適用)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_warden', 42), ['green_fang'])
    s = { ...s, player: { ...s.player, growth: 100, energy: 9 } } // 14+100=114の一撃
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    expect(hpBefore - s.enemies[0].hp).toBe(35) // 門番の装甲35
  })

  it('延焼は装甲を無視する (バーンが装甲の解答になる)', () => {
    let s = freshCombat('set-confirm', 'enemy_warden', 42)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 50, block: 0 })) }
    const hpBefore = s.enemies[0].hp
    s = withIntent(s, { kind: 'defend', shownMin: 1, shownMax: 1, actual: 1 })
    s = applyCommand(s, { type: 'EndTurn' })
    // 延焼50は装甲35を超えて丸ごと通る (敵フェーズ開始時のDoT)
    expect(hpBefore - s.enemies[0].hp).toBeGreaterThanOrEqual(50)
  })
})
