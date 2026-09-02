// 無限ループの回帰テスト (2026-08-26)。
// 魔力変換(青)・自然の奔流(緑) が正味+1エナジー・消滅なしだったため、
// 集中(次のカード-1)と連鎖する思考(詠唱数ぶんドロー)を挟むとエナジーもドローも青天井になった
// (実測: deck_storm がターン3で詠唱数301・エナジー102、simの3000手セーフガードに到達)。
// カード追加でこの手のループが再発したら、ここで落ちる。
import { describe, expect, it } from 'vitest'
import { allDecks, allEnemies } from '../engine/content.ts'
import { startCombat } from '../engine/combat.ts'
import { applyCommand } from '../engine/state.ts'
import { chooseCommand } from './run.ts'

/** 1ターンの詠唱数の上限。健全なデッキの実測最大は11 (deck_chaos の衝動連打) */
const MAX_PLAYS_PER_TURN = 20
/**
 * 1戦闘のコマンド数上限。これは「無限ループの backstop」であって試合の長さの基準ではない。
 * 実測の最長は deck_fortress vs 苔まといの主 の 329ターン (1329コマンド) で、
 * これはループではなく膠着 (要塞デッキの火力が再生をわずかに上回るだけ)。
 * この膠着自体は別途バランスの課題として扱う。
 */
const MAX_ACTIONS = 2000

describe('無限ループ検知', () => {
  it('全デッキ × 敵 × 複数シードで、1ターンの詠唱数と1戦闘のコマンド数が上限を超えない', { timeout: 120000 }, () => {
    const offenders: string[] = []
    let worstPlays = 0
    // 2026-08-26: 5シード×1敵では取り逃していた (集中のループは deck_storm × 用心深い影 × seed7 でしか出ない)。
    // 全デッキ × 全敵 × 10シードへ拡張する。
    for (const deck of allDecks) {
      for (let seed = 1; seed <= 10; seed++) {
        for (const enemy of allEnemies) {
          let s = startCombat(seed, 'set-confirm', enemy.id, deck.id)
          let actions = 0
          while (s.phase !== 'won' && s.phase !== 'lost') {
            if (++actions > MAX_ACTIONS) {
              offenders.push(`${deck.id} vs ${enemy.id} seed${seed}: コマンド数${actions}超過`)
              break
            }
            s = applyCommand(s, chooseCommand(s))
            if (s.player.cardsPlayedThisTurn > worstPlays) worstPlays = s.player.cardsPlayedThisTurn
            if (s.player.cardsPlayedThisTurn > MAX_PLAYS_PER_TURN) {
              offenders.push(
                `${deck.id} vs ${enemy.id} seed${seed}: ターン${s.turn}で詠唱数${s.player.cardsPlayedThisTurn}`,
              )
              break
            }
          }
        }
      }
    }
    // ループ (1ターンの詠唱数の暴走) は一件も許さない
    expect(offenders.filter((o) => o.includes('詠唱数'))).toEqual([])
    // 膠着 (決着はするが異常に長い) は0件であること。
    // 2026-08-26: 既知だった deck_fortress vs 苔まといの主 (329ターン) は解消した。
    // 原因はカードではなく ①ボットが attack を defend より先に見るため城壁砕きが壁を積む前に空撃ち
    // ②城壁砕きの効果順が [ダメージ→ブロック] で自前のブロックが自分に乗らない、の2点だった。
    const stalemates = [...new Set(offenders.filter((o) => o.includes('コマンド数')).map((o) => o.split(' seed')[0]))]
    expect(stalemates).toEqual([])
    // 上限に余裕があることも確認 (健全な最大は10台のはず)
    expect(worstPlays).toBeLessThanOrEqual(MAX_PLAYS_PER_TURN)
    // 全デッキ×全敵×複数シードの総当たりなので、カード・敵が増えるたびに重くなる。
    // 単独では約4秒だが並列実行だと既定5秒を超えるため上限を上げる (2026-08-29)
  })
})
