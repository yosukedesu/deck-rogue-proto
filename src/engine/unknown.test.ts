// ?マスの本家式解決 (2026-08-29) のテスト。
// 確定済みルール表「?マス（イベント）」を機械的に固定する:
// - 入った瞬間に中身が決まる (戦闘10%/ショップ3%/宝箱2%/イベント85%) の累積確率 (pity)
// - 確率は整数パーセントポイント (浮動小数を持ち込まない = Unity移植のRNG等価性)
// - 幕をまたぐと累積確率と祠がリセットされる
// - 直前がショップなら ?→ショップ が起きない (本家のショップ2連続禁止)
import { describe, expect, it } from 'vitest'
import { allEvents } from './content.ts'
import { applyRunCommand, createRun } from './run.ts'
import type { RunState } from './run.ts'

/** ?ノードにいる状態を直接作る (マップ生成の運に左右されない) */
function atUnknown(seed: number, patch: Partial<RunState> = {}): RunState {
  const run = createRun(seed, 'set-confirm')
  return {
    ...run,
    map: [[{ type: 'event', encounterId: null, next: [0] }], [{ type: 'battle', encounterId: 'enemy_probe', next: [] }]],
    row: -1,
    col: 0,
    ...patch,
  }
}

/** ?ノードへ進入する (ChooseNode が enterNode → resolveUnknown を通す) */
function enterUnknown(run: RunState): RunState {
  return applyRunCommand(run, { type: 'ChooseNode', col: 0 })
}

describe('?マスの解決 (本家 pity)', () => {
  it('決定論: 同じシード・同じ状態なら解決結果が一致する', () => {
    const a = enterUnknown(atUnknown(31))
    const b = enterUnknown(atUnknown(31))
    expect(a.phase).toBe(b.phase)
    expect(a.eventId).toBe(b.eventId)
    expect(JSON.stringify(a.unknownPity)).toBe(JSON.stringify(b.unknownPity))
  })

  it('累積確率は整数パーセントポイントで進む (外れた種別だけ基礎値ぶん加算)', () => {
    // 基礎値からスタートして1回解決させる。どれに解決しても「当たりは基礎値へリセット・
    // 外れは基礎値ぶん加算」なので、値は必ず整数のまま
    const before = atUnknown(7)
    expect(before.unknownPity).toEqual({ monster: 10, shop: 3, treasure: 2 })
    const after = enterUnknown(before)
    const p = after.unknownPity
    expect(Number.isInteger(p.monster) && Number.isInteger(p.shop) && Number.isInteger(p.treasure)).toBe(true)
    // イベントに解決した場合は3種すべてが基礎値ぶん増える
    if (after.phase === 'event') expect(p).toEqual({ monster: 20, shop: 6, treasure: 4 })
  })

  it('外れ続けると戦闘率が上がる (10→20→30...)。100%を超えれば必ず戦闘になる', () => {
    const run = enterUnknown(atUnknown(3, { unknownPity: { monster: 100, shop: 3, treasure: 2 } }))
    expect(run.phase).toBe('combat')
    // 当たった種別だけ基礎値へリセット、外れた2種は加算される
    expect(run.unknownPity).toEqual({ monster: 10, shop: 6, treasure: 4 })
  })

  it('?→戦闘は通常戦闘扱い (エリートではない・敵はその幕の帯から出る)', () => {
    const run = enterUnknown(atUnknown(3, { unknownPity: { monster: 100, shop: 3, treasure: 2 } }))
    expect(run.phase).toBe('combat')
    expect(run.currentElite).toBe(false)
    expect(run.combat).not.toBeNull()
  })

  it('?→宝箱はレリック3択のみで、選んでもカード報酬は出ずマップへ戻る', () => {
    let run = enterUnknown(atUnknown(5, { unknownPity: { monster: 0, shop: 0, treasure: 100 } }))
    expect(run.phase).toBe('relic-reward')
    expect(run.relicOptions).toHaveLength(3)
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.relics).toHaveLength(1)
    expect(run.phase).toBe('map') // カード報酬は付かない
  })

  it('直前がショップなら ?→ショップ が起きない (本家のショップ2連続禁止)', () => {
    // shop枠を100にしても、直前がショップなら潰れて次の枠 (宝箱) に流れる
    const run = enterUnknown(
      atUnknown(9, { unknownPity: { monster: 0, shop: 100, treasure: 100 }, lastRoomWasShop: true }),
    )
    expect(run.phase).not.toBe('shop')
  })

  it('ショップに入ると lastRoomWasShop が立ち、他の部屋では下りる', () => {
    const shop = enterUnknown(atUnknown(9, { unknownPity: { monster: 0, shop: 100, treasure: 0 } }))
    expect(shop.phase).toBe('shop')
    expect(shop.lastRoomWasShop).toBe(true)
    const battle = enterUnknown(atUnknown(9, { unknownPity: { monster: 100, shop: 0, treasure: 0 } }))
    expect(battle.lastRoomWasShop).toBe(false)
  })

  it('旧セーブ互換: 新フィールドが無くても ? が解決でき、確率が NaN にならない', () => {
    const base = atUnknown(11)
    const { unknownPity: _p, lastRoomWasShop: _s, seenEventIds: _e, seenShrineIds: _sh, ...rest } = base
    const legacy = rest as RunState
    const run = enterUnknown(legacy)
    expect(['event', 'combat', 'shop', 'relic-reward', 'map']).toContain(run.phase)
    const p = run.unknownPity
    expect(Number.isFinite(p.monster) && Number.isFinite(p.shop) && Number.isFinite(p.treasure)).toBe(true)
  })
})

describe('イベントプールの3層構造 (幕専用・祠・ワンタイム)', () => {
  it('引いたイベントは seenEventIds に記録される', () => {
    const run = enterUnknown(atUnknown(13, { unknownPity: { monster: 0, shop: 0, treasure: 0 } }))
    expect(run.phase).toBe('event')
    expect(run.eventId).not.toBeNull()
    expect(run.seenEventIds).toContain(run.eventId!)
  })

  it('既出の幕専用イベントは抽選から除かれる (残り1個なら必ずそれが出る)', () => {
    const actIds = allEvents
      .filter((e) => (e.kind ?? 'act') === 'act' && (e.act === undefined || e.act === 1))
      .map((e) => e.id)
    const seen = actIds.slice(0, -1) // 最後の1個だけ未見にする
    const remaining = actIds[actIds.length - 1]
    const run = enterUnknown(
      atUnknown(17, { unknownPity: { monster: 0, shop: 0, treasure: 0 }, seenEventIds: seen }),
    )
    expect(run.phase).toBe('event')
    // 祠・ワンタイムがまだ無い段階では、残った幕専用の1個しか引けない
    expect(run.eventId).toBe(remaining)
  })

  it('プールが尽きたら既出から引き直す (throw しない = 壊れ検知の床)', () => {
    const allIds = allEvents.map((e) => e.id)
    const run = enterUnknown(
      atUnknown(19, { unknownPity: { monster: 0, shop: 0, treasure: 0 }, seenEventIds: allIds }),
    )
    expect(run.phase).toBe('event')
    expect(run.eventId).not.toBeNull()
  })
})
