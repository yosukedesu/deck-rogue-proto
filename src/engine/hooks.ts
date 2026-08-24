// engine/hooks.ts — イベントフックのディスパッチ
// 戦闘内イベントに反応するフック (リスナー) をここで束ねる。
// 現在のフック: ①置物 (permanent) ②リアクション方式。
// レリック・敵パッシブなどは将来ここに追加する (CLAUDE.md「イベントフックの口だけは最初から開けておく」)。

import { runPermanentTriggers } from './effects.ts'
import { getReactionSystem } from './reactions/index.ts'
import type { GameEvent, GameState } from './types.ts'

// 置物トリガーの実行本体は effects.ts へ移設 (回復・HP損失・消滅の誘発を効果解決の内側から発火させるため)。
// combat.ts などの既存の参照は従来どおりここから import できる
export { runPermanentTriggers }

export function dispatchHooks(state: GameState, event: GameEvent): GameState {
  let s = state
  // 1. 置物フック (自動発火。リアクションの割り込みより先に解決する)
  if (event.type === 'EnemyActionExecuting' && event.kind === 'attack') {
    // 被攻撃前 (軽減系) の置物
    s = runPermanentTriggers(s, 'onAttackIncoming', event.enemyIndex)
  }
  if (event.type === 'EnemyActionResolved' && event.kind === 'attack') {
    // 被攻撃後 (返し系) の置物: 茨の茂みなど
    s = runPermanentTriggers(s, 'onAttacked', event.enemyIndex)
  }
  if (event.type === 'EnemyActionExecuting' || event.type === 'EnemyActionResolved') {
    // 置物の返しで敵が倒れた・自分が倒れた場合、リアクション確認はもう不要
    const enemy = s.enemies[event.enemyIndex]
    if ((enemy && enemy.hp <= 0) || s.player.hp <= 0) return s
  }
  // 2. リアクション方式フック (方式は state.reactionMode から解決)
  s = getReactionSystem(s.reactionMode).onEvent(s, event)
  // 3. (将来) レリックフック / 敵パッシブフック をここに追加
  return s
}
