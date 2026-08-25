// ギルドリーダー第1波 (2026-08-25 8人) のテスト。
// 確定済みルール表「最終ロースター」を固定する。かすみ (ディミア・伏せ2枚) は第2波。
import { describe, expect, it } from 'vitest'
import { allLeaders, getLeaderDef } from './content.ts'
import { createRun } from './run.ts'
import { applyCommand } from './state.ts'
import { withHand } from './test-helpers.ts'

describe('ギルドリーダーのロースター', () => {
  it('単色6人+ギルド9人=15人ロースター完成。HPは5刻みの方針値', () => {
    expect(allLeaders.length).toBe(15)
    const hp = {
      leader_azorius: 70, leader_rakdos: 80, leader_gruul: 80, leader_selesnya: 75,
      leader_orzhov: 75, leader_golgari: 75, leader_boros: 75, leader_simic: 65,
    }
    for (const [id, expected] of Object.entries(hp)) {
      expect(getLeaderDef(id).maxHp).toBe(expected)
      expect(getLeaderDef(id).colors.length).toBe(2)
      expect(expected % 5).toBe(0)
    }
  })

  it('ギルドランは2色で始まり、初期デッキは両色混成の10枚', () => {
    const run = createRun(7, 'set-confirm', 'leader_rakdos')
    expect(run.colors).toEqual(['black', 'red'])
    expect(run.deck.length).toBe(10)
    const colors = new Set(run.deck.map((c) => c.def.color))
    expect(colors.has('black') && colors.has('red')).toBe(true)
  })
})

describe('ギルドパッシブ', () => {
  it('あかね (ラクドス): カード効果でHPを失うたび対象に1ダメージ', () => {
    const run = createRun(3, 'set-confirm', 'leader_rakdos')
    let s = withHand(run.combat!, ['black_shadow_blade'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_shadow_blade', targetIndex: 0 })
    // 影の刃: HP-2 (あかね1ダメ) → 8ダメ
    expect(s.enemies[0].hp).toBe(enemyHp - 1 - 8)
  })

  it('わかば (セレズニア): 置物が場に出るたび成長+1', () => {
    const run = createRun(3, 'set-confirm', 'leader_selesnya')
    let s = withHand(run.combat!, ['white_perm_squire'])
    expect(s.player.growth).toBe(0) // リーダーパッシブ自身は「登場」しない
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    expect(s.player.growth).toBe(1)
  })

  it('くろは (ゴルガリ): カードが消滅するたび成長+1', () => {
    const run = createRun(3, 'set-confirm', 'leader_golgari')
    let s = withHand(run.combat!, ['black_mill'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_mill' })
    expect(s.player.growth).toBe(4) // 忘却の霧=4枚消滅
  })

  it('あさひ (ボロス): 戦闘開始時に従者の少年トークン1体を召喚', () => {
    const run = createRun(3, 'set-confirm', 'leader_boros')
    const s = run.combat!
    const tokens = s.player.permanents.filter((p) => p.token === true)
    expect(tokens).toHaveLength(1)
    expect(tokens[0].def.id).toBe('white_perm_squire')
  })

  it('しずく (シミック): エナジー上限4で開始し、開幕1ドロー', () => {
    const run = createRun(3, 'set-confirm', 'leader_simic')
    const s = run.combat!
    expect(s.player.energyMax).toBe(4)
    expect(s.player.energy).toBe(4)
    expect(s.player.hand.length).toBe(6) // 通常5枚 + 開幕1ドロー
  })

  it('いぶき (グルール): 攻撃カードをプレイするたび勢い+1', () => {
    const run = createRun(3, 'set-confirm', 'leader_gruul')
    let s = withHand(run.combat!, ['red_strike', 'red_strike'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_strike', targetIndex: 0 })
    expect(s.player.momentum).toBe(1)
    // 2発目には勢い1が乗る
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_strike', targetIndex: 0 })
    expect(s.enemies[0].hp).toBe(enemyHp - 6 - 7)
  })
})
