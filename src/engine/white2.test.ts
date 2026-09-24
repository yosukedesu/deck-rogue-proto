// 白の拡張 (2026-08-25 +19枚) のテスト。
// 確定済みルール表「召喚」「置物登場の誘発」「威圧の換金」「隊列の盾」を固定する。
import type { CardDef } from './types.ts'
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, setAndArm, withHand, withIntent } from './test-helpers.ts'

describe('召喚 (トークン再現)', () => {
  it('一斉召集: 従者の少年トークンを2体場に出し、集結の弾になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_muster',
      'white_rally',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_muster' })
    expect(s.player.permanents).toHaveLength(3)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_rally' })
    expect(s.enemies[0].hp).toBe(hpBefore - 18) // 置物3×6 (2026-09-24 CSV)
  })

  it('召喚された従者は毎ターン開始時に自動攻撃する (本体と同じ挙動)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_muster',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_muster' })
    s = withIntent(s, attackIntent(3))
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12) // 従者3体×(3+育ち1)
  })
})

describe('置物登場の誘発 (白の接着剤)', () => {
  it('軍楽隊: 置物が場に出るたび1ドロー (自身の登場にも誘発)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_band',
      'white_perm_squire',
    ])
    s = { ...s, player: { ...s.player, energy: 9, light: 2 } } // 鐘の人形は 1E・灯2 (2026-09-20 灯と人形の結び)
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_band' })
    // 軍楽隊自身の登場で1ドロー (手札: -軍楽隊+1ドロー = handBefore)
    expect(s.player.hand.length).toBe(handBefore)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_squire' })
    // 従者の登場でさらに1ドロー
    expect(s.player.hand.length).toBe(handBefore - 1 + 1)
  })

  it('白銀の軍旗 + 一斉召集: トークン2体の登場で敵全体に2ダメ×2', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_white'), [
      'white_perm_banner',
      'white_muster',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_banner' })
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_muster' })
    // 軍旗 2ダメ×2回 (2026-08-26 軍旗 1→2) + 点灯の定義 (2026-09-20 白共通ルール): 出た剣の人形2体がすぐに1回動く (2ダメ×2)。
    // 人形の打点はランダムな生存敵へ (同日裁定) = 合計12で、各敵は軍旗の4以上
    const lost0 = hp0 - s.enemies[0].hp
    const lost1 = hp1 - s.enemies[1].hp
    expect(lost0 + lost1).toBe(21)
    expect(lost0).toBeGreaterThanOrEqual(4)
    expect(lost1).toBeGreaterThanOrEqual(4)
    expect(s.player.light ?? 0).toBe(0) // 人形の登場では灯は増えない (2026-09-20 夜 廃止)
  })
})

describe('威圧の換金 (断罪の槌)', () => {
  it('対象の威圧スタック×3の追加ダメージ。威圧なしなら追加なし (2026-09-03 Weak化: dealDamagePerWeak。眩みの槌は 2026-09-20 夜に撤去=機構は仮の札で固定)', () => {
    const hammer: CardDef = { id: 'test_hammer', name: '試しの槌', cost: 2, type: 'physical', color: 'white', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 8 }, { trigger: 'onPlay', effect: 'dealDamagePerWeak', amount: 3 }] }
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [])
    s = { ...s, player: { ...s.player, energy: 9, hand: [{ uid: 'h1', def: hammer }, { uid: 'h2', def: hammer }] }, enemies: s.enemies.map((e) => ({ ...e, weak: 0 })) }
    const hpBefore = s.enemies[0].hp
    // 威圧なし: 8のみ
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'h1', targetIndex: 0 })
    expect(s.enemies[0].hp).toBe(hpBefore - 8)
    // 威圧2 → 8 + 2×3
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, weak: 2 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'h2', targetIndex: 0 })
    expect(s.enemies[0].hp).toBe(hpBefore - 8 - 8 - 6)
  })
})

describe('灯り溜め (白の再設計 2026-09-20: 守りが準備)', () => {
  it('ブロック8＋灯+2 (2026-09-24 CSV でブロック5→8)。人形2体の登場では灯は増えない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
      'white_perm_shieldmaiden',
      'white_light_hoard',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_shieldmaiden' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_light_hoard' })
    expect(s.player.block).toBe(3 + 8) // 盾の人形の点灯3 + 灯り溜め8
    expect(s.player.light).toBe(2) // 灯り溜めの+2だけ (人形の登場では灯は増えない 2026-09-20 夜)
  })
})

describe('回復軸の接着剤 (光の器)', () => {
  it('回復のたびブロック2 (満タンの過剰回復でも誘発。2026-08-31)。回復するたび灯+1 (2026-09-20)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_chalice',
      'white_mass_heal',
      'white_mass_heal',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_chalice' })
    // 満タン: 実回復0でも器は鳴る (満タン沈黙3割への処方)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_mass_heal' }) // 癒しの光は 2026-09-24 に撤去 = 大いなる癒し (回復15+灯3)
    expect(s.player.block).toBe(2)
    expect(s.player.light).toBe(4) // 回復+1・明示の灯+3
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_mass_heal' })
    expect(s.player.hp).toBe(65)
    expect(s.player.block).toBe(4)
    expect(s.player.light).toBe(8)
  })
})

describe('白の新リアクション', () => {
  it('敵が攻撃以外の行動をするたび (onEnemyActed + actionKindsNot): 威圧1+ブロック3。攻撃には鳴らない (眩みの障壁は 2026-09-20 夜に撤去=機構は仮の札で固定)', () => {
    const barrier: CardDef = { id: 'test_barrier', name: '試しの障壁', cost: 2, type: 'permanent', color: 'white', effects: [{ trigger: 'onEnemyActed', effect: 'weakenEnemy', amount: 1, condition: { actionKindsNot: ['attack'] } }, { trigger: 'onEnemyActed', effect: 'gainBlock', amount: 3, condition: { actionKindsNot: ['attack'] } }] }
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [])
    s = { ...s, player: { ...s.player, energy: 9, hand: [{ uid: 'b0', def: barrier }] } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 'b0' })
    expect(s.player.permanents.some((p) => p.def.id === 'test_barrier')).toBe(true)
    let d = applyCommand(withIntent(s, { kind: 'buff', actual: 3 }), { type: 'EndTurn' })
    expect(d.enemies[0].weak).toBe(1)
    expect(d.eventLog.some((e) => e.type === 'BlockGained' && e.target === 'player' && e.amount === 3)).toBe(true)
    const a = applyCommand(withIntent(s, { kind: 'attack', actual: 3 }), { type: 'EndTurn' })
    expect(a.enemies[0].weak ?? 0).toBe(0)
    expect(a.eventLog.some((e) => e.type === 'BlockGained' && e.target === 'player' && e.amount === 3)).toBe(false)
  })

  it('呪文プレイで起爆する仕込み札 (onSpellPlayed。光盾の点灯は 2026-09-24 に撤去=仮の札で固定): ブロック12', () => {
    const chant: CardDef = { id: 'test_chant', name: '試しの詠唱', cost: 1, type: 'reaction', color: 'white', effects: [{ trigger: 'onSpellPlayed', effect: 'gainBlock', amount: 12 }] }
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [])
    s = { ...s, player: { ...s.player, hand: [{ uid: 'c0', def: chant }] } }
    s = setAndArm(s, 'c0') // 罠モデル: 伏せたターンは自己誘発も鳴らない
    s = withHand(s, ['white_calling']) // 呪文 (癒しの光は 2026-09-24 に撤去)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_calling' })
    expect(s.player.block).toBe(12) // 2026-08-27 7→9・2026-09-23 罠の強化 9→12
    expect(s.player.setCards).toHaveLength(0)
  })
})
