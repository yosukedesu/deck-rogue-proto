// 読み合いの全敵展開 (2026-08-28) のテスト。確定済みルール表「読み合いの全敵展開」「再生」を固定する。
// setAlt = 行動単位の条件分岐。既存の条件付き意図の配管 (両分岐予告・行動開始時確定) に乗る。
import { describe, expect, it } from 'vitest'
import { allEnemies, getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

/** 次の自ターンまで進める (確認ウィンドウは全て温存) */
function toNextTurn(state: GameState): GameState {
  let s = applyCommand(state, { type: 'EndTurn' })
  let guard = 0
  while (s.phase === 'awaiting-reaction' && guard++ < 20) {
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
  }
  return s
}

/** 伏せ札を1枚置く */
function withSet(state: GameState): GameState {
  let s = withHand(state, ['green_reaction_thorns'])
  return applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
}

describe('setAlt = 行動単位の条件分岐', () => {
  it('大亀: 大薙ぎ (3歩目) は伏せありで浅い薙ぎ (18-24+脆弱1。2026-09-03 賭け型化) の分岐を予告する', () => {
    let s = freshCombat('set-confirm', 'enemy_turtle', 42)
    // 1〜2歩目 (防御) を消化して3歩目 (大薙ぎ) の宣言まで進める
    s = toNextTurn(s)
    s = toNextTurn(s)
    const intent = s.enemies[0].intent!
    expect(intent.kind).toBe('attack') // 素 = 大薙ぎ24-32
    expect(intent.shownMin).toBeGreaterThanOrEqual(24)
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt!.kind).toBe('attack') // 伏せあり = 浅い薙ぎ (数字は下がるが脆弱1が付く=賭け)
    expect(intent.alt!.shownMin).toBeGreaterThanOrEqual(18)
    expect(intent.alt!.shownMax).toBeLessThanOrEqual(24)
    expect(intent.alt!.inflict).toEqual({ status: 'vulnerable', amount: 1 })
  })

  it('大亀: 伏せを維持したまま大薙ぎターンを迎えると浅い薙ぎで解決される', () => {
    let s = freshCombat('set-confirm', 'enemy_turtle', 42)
    s = toNextTurn(s)
    s = withSet(toNextTurn(s)) // 3歩目の宣言後に伏せる
    const hpBefore = s.player.hp
    s = toNextTurn(s)
    // 浅い薙ぎ16-22の範囲で被弾 (素の24-32ではない)。返し札は温存で伏せ枠は残る
    const dmg = hpBefore - s.player.hp
    expect(dmg).toBeGreaterThanOrEqual(16)
    expect(dmg).toBeLessThanOrEqual(22)
  })

  it('門番: 鐘の1発目は伏せありで「崩し打ち」(攻撃+弱体1) = 罰型の対価に変わる', () => {
    // 2026-08-30 罰型化: 旧「門を閉ざす」(防御化) は、0Eの死に札を伏せるだけで最終ボスの
    // 初手がタダで消える「敵の1ターンを無料で買うボタン」だった (3幕フルラン実測)。
    // 伏せを見たら「やや弱いが弱体1を置く」= 伏せる側にも代償が出る妖術師型の賭けへ
    let s = freshCombat('set-confirm', 'enemy_warden', 42)
    const intent = s.enemies[0].intent!
    expect(intent.kind).toBe('attack')
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt!.kind).toBe('attack')
    expect(intent.alt!.inflict?.status).toBe('weak')
    // 2発目の鐘は無条件 (膠着破り): 分岐なしの攻撃
    s = toNextTurn(s)
    expect(s.enemies[0].intent!.kind).toBe('attack')
    expect(s.enemies[0].intent!.conditionalOn).toBeUndefined()
  })

  it('狼: 連撃は伏せありで2連に減る (連撃数の読み)', () => {
    const s = freshCombat('set-confirm', 'enemy_wolf', 42)
    const intent = s.enemies[0].intent! // 1歩目 = flurry 3連
    expect(intent.hits).toBe(3)
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt!.hits).toBe(2)
  })

  it('コボルト: 太鼓は伏せありで鳴らない (怯えの小突き)', () => {
    const s = freshCombat('set-confirm', 'enc_wolf_drummer', 42)
    const drummer = s.enemies[1]
    expect(drummer.intent!.kind).toBe('rally')
    expect(drummer.intent!.conditionalOn).toBe('set')
    expect(drummer.intent!.alt!.kind).toBe('attack')
  })

  it('妖術師: 泥投げは伏せありで泥呪い (同じ6-9で弱体3の代わりに虚弱2。2026-09-03 賭け型化) に変わる', () => {
    const s = freshCombat('set-confirm', 'enemy_hexer', 42)
    const intent = s.enemies[0].intent! // 1歩目 = mud
    expect(intent.kind).toBe('attack')
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt!.kind).toBe('attack')
    expect(intent.alt!.inflict).toEqual({ status: 'frail', amount: 2 }) // 旧: hex 弱体3 (攻撃せず) = 弱腰
  })

  it('オーガ: 3歩目の棍棒は伏せありで激怒 (強化+2) に変わる', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42)
    s = toNextTurn(s) // 雄叫び消化
    s = toNextTurn(s) // 棍棒(無条件)消化
    const intent = s.enemies[0].intent! // 3歩目 = club_wild
    expect(intent.kind).toBe('attack')
    expect(intent.conditionalOn).toBe('set')
    // 2026-08-30 罰型化: 伏せを見ると激怒の雑な強打 (基礎9-13 → 12-16)。
    // 「伏せた札で受けきれるか」の賭けになる (旧: 強化+2 = 無料ターン化していた)
    expect(intent.alt!.kind).toBe('attack')
    expect(intent.alt!.shownMax).toBeGreaterThan(intent.shownMax)
  })

  it('うねる獣は意図的に読みなし (Act1の休符)', () => {
    const def = getEnemyDef('enemy_wide_power')
    expect(def.movesVsSet).toBeUndefined()
    expect(def.moves.every((m) => m.setAlt === undefined)).toBe(true)
  })
})

describe('膠着破りの不変条件 (setAlt版)', () => {
  it('setAltを持つ敵は「伏せられても殴る」手段を必ず残す', () => {
    for (const def of allEnemies) {
      const hasSetAlt = def.moves.some((m) => m.setAlt !== undefined)
      if (!hasSetAlt) continue
      // 伏せあり時の実効行動列: setAlt があればその kind、なければ素の kind
      const table = def.sequence
        ? def.sequence.map((id) => def.moves.find((m) => m.id === id)!)
        : def.moves
      const attacksWhileSet = table.some((m) =>
        m.setAlt !== undefined ? m.setAlt.kind === 'attack' : m.kind === 'attack',
      )
      const breaksStall = attacksWhileSet || def.enrageEveryCards !== undefined
      expect(breaksStall, `${def.id} は伏せっぱなしで無力化できてしまう`).toBe(true)
    }
  })
})

describe('regenBreak (苔まといの主の再生をバーストで止める)', () => {
  it('そのターンに12以上削ると次の再生が発動しない', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    // 2026-08-30 割合化: 12は幕3の平均ターン火力66に対して自動成立していた (再生が1点も仕事せず)。
    // 幕3の想定ターン火力の半分 = 30 へ
    expect(getEnemyDef('enemy_moss').regenBreak).toBe(30)
    s = withHand(s, ['green_fang']) // 14ダメ + 成長16 = 30 (しきい値ちょうど)
    s = { ...s, player: { ...s.player, growth: 16 } }
    const target = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    const afterHit = s.enemies[0].hp
    expect(target - afterHit).toBeGreaterThanOrEqual(30)
    s = toNextTurn(s)
    // 再生していない (敵の攻撃後もHPは削った値のまま)
    expect(s.enemies[0].hp).toBe(afterHit)
    expect(s.eventLog.some((e) => e.type === 'RegenBroken')).toBe(true)
  })

  it('12未満の削りでは従来どおり再生する', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    s = withHand(s, ['green_fang']) // 14ダメージ (<30)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_fang' })
    const afterHit = s.enemies[0].hp
    s = toNextTurn(s)
    expect(s.enemies[0].hp).toBe(afterHit + 5) // regen 5
  })

  it('累積はターンごとにリセットされる (前ターンの削りは持ち越さない)', () => {
    let s = freshCombat('set-confirm', 'enemy_moss', 42)
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' }) // 6
    s = toNextTurn(s) // 再生する (6 < 12)。累積リセット
    s = withHand(s, ['green_strike'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' }) // また6 (合計12だが別ターン)
    const afterHit = s.enemies[0].hp
    s = toNextTurn(s)
    expect(s.enemies[0].hp).toBe(afterHit + 5) // 持ち越し無し = 再生する
  })
})
