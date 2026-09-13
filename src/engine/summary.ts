// engine/summary.ts — 撃破サマリー (2026-08-29 面白さ5への処方③: ピーク体験)。
// eventLog の純関数集計なので engine に置く (UI/CLI が共用。DOM依存なし)。
// 「俺の戦いだった」を1行で見せる: 最大ターン火力・読み勝ち・完全に凌いだ回数。

import type { GameEvent, EnemyDef, RelicDef } from './types.ts'

export interface BattleSummary {
  /** かかったターン数 */
  readonly turns: number
  /** プレイヤーの総与ダメージ */
  readonly totalDealt: number
  /** 1ターンの最大与ダメージ (多段・複数枚の合算 = ぶん回りの記録) */
  readonly bestTurnDealt: number
  /** 失ったHPの合計 (敵の攻撃によるもの) */
  readonly hpLost: number
  /** リアクション発動回数 (読み勝ちの回数) */
  readonly reactionsFired: number
  /** 敵の攻撃を完全に防いだ回数 (被弾予定があったのにHP損失0) */
  readonly perfectBlocks: number
  /** 打ち消した敵行動の数 */
  readonly negates: number
}

export function battleSummary(log: readonly GameEvent[]): BattleSummary {
  let turns = 0
  let totalDealt = 0
  let bestTurnDealt = 0
  let currentTurnDealt = 0
  let hpLost = 0
  let reactionsFired = 0
  let perfectBlocks = 0
  let negates = 0
  for (const e of log) {
    switch (e.type) {
      case 'TurnStarted':
        turns = Math.max(turns, e.turn)
        bestTurnDealt = Math.max(bestTurnDealt, currentTurnDealt)
        currentTurnDealt = 0
        break
      case 'DamageDealt':
        if (e.source === 'player') {
          totalDealt += e.amount
          currentTurnDealt += e.amount
        } else {
          hpLost += e.hpLoss
          if (e.amount > 0 && e.hpLoss === 0) perfectBlocks++
        }
        break
      case 'ThornsReflected':
        // とげ反射も「受けたダメージ」に数える (2026-08-30 計測ランで発覚: 針毛の栗鼠戦で
        // 実際は9減っているのに「被ダメ1」と表示されていた = サマリーが嘘をついていた)
        hpLost += e.hpLoss
        break
      case 'BurnTick':
        // 延焼ティックもプレイヤーの与ダメージ (2026-08-31 赤バーン縛りランで発覚:
        // うねる獣59HPを倒して「総与ダメ27」= バーン型では表示が実ダメの半分以下だった。
        // ThornsReflected と同じ「サマリーが嘘をつく」穴)。敵フェーズ中のティックは
        // 直前の自ターンの投資なので、最大ターン火力 (currentTurnDealt) にも算入する
        totalDealt += e.amount
        currentTurnDealt += e.amount
        break
      case 'ReactionTriggered':
        reactionsFired++
        break
      case 'ActionNegated':
        negates++
        break
      default:
        break
    }
  }
  bestTurnDealt = Math.max(bestTurnDealt, currentTurnDealt)
  return { turns, totalDealt, bestTurnDealt, hpLost, reactionsFired, perfectBlocks, negates }
}

/** サマリーの1行表示 (UI/CLI共用の文言) */
export function summaryLine(s: BattleSummary): string {
  const parts = [
    `${s.turns}ターン`,
    `総与ダメ${s.totalDealt}${s.bestTurnDealt > 0 ? `（最大ターン${s.bestTurnDealt}）` : ''}`,
    `被ダメ${s.hpLost}`,
    s.reactionsFired > 0 ? `読み勝ち${s.reactionsFired}回` : '',
    s.perfectBlocks > 0 ? `敵の攻撃${s.perfectBlocks}回を完全に凌いだ` : '',
    s.negates > 0 ? `打ち消し${s.negates}回` : '',
  ]
  return parts.filter(Boolean).join(' / ')
}

// ---- カード表示のラベル (UI と CLI で1つの真実を共有する純関数) ----

/**
 * カードのコスト表記。**Xコスト札は `cost` フィールド (=1) でなく "X" と出す**
 * (2026-08-29 バグ修正: UI側に xCost の分岐が1つも無く、ピック画面・ショップ・手札・デッキ一覧の
 * すべてで X札が「1マナ」と表示されていた。CLIだけが独自に対応していたので共有関数に一本化した)。
 * discounted は「次のカード-N」適用後の実効コスト (素と違う時だけ渡す)。
 */
export function cardCostLabel(def: { cost: number; xCost?: boolean }, discounted?: number): string {
  if (def.xCost === true) return 'X' // 割引はXコストに効かない (確定済みルール表「Xコスト」)
  return String(discounted ?? def.cost)
}

/**
 * Xコスト札のヒット表記。xHits の効果は支払ったXの回数だけ繰り返される。
 * 表示に出さないと「1マナで7ダメージ」に見えてカードの正体が伝わらない。
 * 成長・勢いの注記はダメージ効果だけに付ける (2026-09-01 検証ラン指摘: 樹皮の重鎧=Xブロックに
 * 旧文言がそのまま出て「成長がブロックに乗る」と読め、防御計算を誤らせていた)
 */
export function xHitsSuffix(e: { xHits?: boolean; effect?: string }): string {
  if (e.xHits !== true) return ''
  return e.effect !== undefined && e.effect.startsWith('dealDamage')
    ? '×Xヒット(各ヒットに成長・勢いが乗る)'
    : '×Xヒット'
}


// ---- 被ダメ予測 (2026-09-02 レビュー是正: UIフッター・💀致死級バッジ・CLIで式が3通りに割れていたのを1本化。
// 2026-09-14 実値公開: 幅の上限でなく宣言した実値に補正 (威圧→鈴→脆弱→重り=実処理 combat.ts と同順) を掛けた
// 「今フェーズに実際に受ける量」になった。意図の数字 (displayedIntentValue) と同じ式) ----
import { effectiveIntent, applyEnemyWeak } from './effects.ts'
import { firstMoveOf, interruptTriggerText, peekMoves } from './enemyGraph.ts'
import { getEnemyDef as getEnemyDefForSummary } from './content.ts'
import type { EnemyInterrupt, EnemyIntent, EnemyIntentBranch, EnemyMove, EnemyState, GameState } from './types.ts'

/** 攻撃1ヒットに今の補正 (威圧・静かな鈴・脆弱・重り) を掛けた値 = 意図に出す数字 (本家形のライブ表示) */
export function modifiedHit(s: GameState, enemyIndex: number, actual: number): number {
  const e = s.enemies[enemyIndex]
  let v = actual
  // 威圧 (2026-09-03 Weak化): -25% (切り捨て・最低1)
  v = applyEnemyWeak(v, e?.weak)
  // 静かな鈴 (C型): 伏せ札がある間、各ヒット-N (最低1)
  if ((s.setDamageReduction ?? 0) > 0 && s.player.setCards.length > 0) {
    v = Math.max(1, v - (s.setDamageReduction ?? 0))
  }
  // 脆弱: +50% (切り捨て)
  if (s.player.vulnerable > 0) v = Math.floor(v * 1.5)
  // 重り: +10%×このターンの実プレイ枚数 (切り捨て)
  if ((s.player.slow ?? 0) > 0 && (s.player.playsThisTurn ?? 0) > 0) {
    v = Math.floor(v * (1 + 0.1 * (s.player.playsThisTurn ?? 0)))
  }
  return v
}

/** 意図 (または分岐) の表示値: 攻撃は補正込みの1ヒット・それ以外は実値 */
export function displayedIntentValue(s: GameState, enemyIndex: number, it: EnemyIntent | EnemyIntentBranch): number {
  return it.kind === 'attack' ? modifiedHit(s, enemyIndex, it.actual) : it.actual
}

/** 補正が実値を変えている時の注記 (「威圧-25%」「脆弱+50%」「重り+N%」「鈴-N」)。無ければ空配列 */
export function intentModifierNotes(s: GameState, enemyIndex: number, it: EnemyIntent | EnemyIntentBranch): string[] {
  if (it.kind !== 'attack') return []
  const e = s.enemies[enemyIndex]
  const notes: string[] = []
  if ((e?.weak ?? 0) > 0) notes.push('威圧-25%')
  if ((s.setDamageReduction ?? 0) > 0 && s.player.setCards.length > 0) notes.push(`鈴-${s.setDamageReduction}`)
  if (s.player.vulnerable > 0) notes.push('脆弱+50%')
  if ((s.player.slow ?? 0) > 0 && (s.player.playsThisTurn ?? 0) > 0) notes.push(`重り+${10 * (s.player.playsThisTurn ?? 0)}%`)
  return notes
}

/** 実行時のヒット数 (手数の鏡は今のプレイ枚数+伏せ) */
export function intentHits(s: GameState, it: EnemyIntent | EnemyIntentBranch): number {
  return (it as EnemyIntent).mirrorHits === true ? Math.max(1, s.player.cardsPlayedThisTurn + (s.player.setsThisTurn ?? 0)) : (it.hits ?? 1)
}

/** 敵1体の「今フェーズに受ける合計ダメージ」。攻撃以外・死亡・混乱 (仲間に向かう) は0 */
export function incomingFrom(s: GameState, enemyIndex: number): number {
  const e = s.enemies[enemyIndex]
  if (!e || e.hp <= 0 || e.confusion > 0) return 0
  const it = effectiveIntent(s, enemyIndex)
  if (it?.kind !== 'attack') return 0
  return modifiedHit(s, enemyIndex, it.actual) * intentHits(s, it)
}

/** 全敵の合計 (被ダメ予測の分子) */
export function incomingTotal(s: GameState): number {
  return s.enemies.reduce((sum, _e, i) => sum + incomingFrom(s, i), 0)
}

const MOVE_KIND_MARK: Record<string, string> = {
  attack: '⚔️', defend: '🛡️', buff: '💪', rally: '📣', hex: '🧿', heal: '💚', 'steal-gold': '💰', flee: '🏃', rest: '😮‍💨', hatch: '🐣', mill: '📖', 'destroy-set': '💥', 'destroy-token': '🪓',
}

/** 技の短い表記「⚔️7〜9×2」「🛡️12〜17」「💪+2」 (予告チップ・図鑑向け。実値でなく技の幅) */
export function moveShort(m: EnemyMove): string {
  const mark = MOVE_KIND_MARK[m.kind] ?? m.kind
  const range = m.min !== undefined ? (m.min === m.max ? `${m.min}` : `${m.min}〜${m.max}`) : ''
  const sign = m.kind === 'buff' || m.kind === 'rally' ? '+' : ''
  const hits = m.mirrorHits === true ? '×手数' : (m.hits ?? 1) > 1 ? `×${m.hits}` : ''
  const inflict = m.inflict ? `+${m.inflict.status}${m.inflict.amount}` : ''
  return `${mark}${sign}${range}${hits}${inflict}`
}

/**
 * 割り込みの予告 (2026-09-14 即時差し替えの両分岐予告): 「HP半分で→⚔️7〜9×2」のように引き金と最初の技を並べる。
 * 発火済み・条件が今は立たないもの (仲間がいない alone 等) は呼び出し側で絞る
 */
export function interruptPreviews(def: EnemyDef, e?: EnemyState): { readonly index: number; readonly trigger: EnemyInterrupt['on']; readonly text: string }[] {
  return (def.interrupts ?? []).flatMap((it, index) => {
    if (e !== undefined && (e.firedInterrupts ?? []).includes(index)) return []
    const first = firstMoveOf(def, it.goto)
    return [{ index, trigger: it.on, text: `${interruptTriggerText(it)}→${first ? moveShort(first) : '…'}` }]
  })
}

/**
 * 孵化までの残り手数 (2026-09-02 検証ラン「カウントダウンが無い」への処方)。
 * 0=宣言済みの意図が孵化 (このフェーズで孵化する)・N=あとN回の宣言で孵化・null=孵化を持たない。
 * patternOffset で卵ごとに非対称になる = この敵の一番面白い部分を常時可視化する
 */
export function turnsUntilHatch(s: GameState, enemyIndex: number): number | null {
  const e = s.enemies[enemyIndex]
  if (!e || e.hp <= 0) return null
  const def = getEnemyDefForSummary(e.enemyId)
  if (def.hatchInto === undefined) return null
  if (e.intent?.kind === 'hatch') return 0
  // 行動グラフ (2026-09-14): カーソルから決定的に辿れる範囲で孵化の技を探す (乱択に当たったら分からない=null)
  const ahead = peekMoves(def, s, enemyIndex, e.node, Object.keys(def.nodes).length + 2)
  for (let d = 0; d < ahead.length; d++) {
    const moveId = ahead[d]
    if (moveId === null) return null
    if (def.moves.find((m) => m.id === moveId)?.kind === 'hatch') return d + 1
  }
  return null
}

/**
 * 伏せ分岐の「型」の注記 (2026-09-03 Opusラン F 指摘: 探り屋の分岐がターンごとに向きが反転して見え、
 * 「伏せると殴られる敵」と1回で誤学習する)。反応テーブル (movesVsSet) が重み抽選で、素の行動が固定ローテの敵は
 * 「伏せを見ると順番を崩す＝どちらが出るかは毎ターン変わる」を予告に添える。
 * setAlt (行動単位の分岐) の敵は向きが固定なので注記しない。表示専用の純関数 (CLI/UI共用)
 */
export function setBranchNote(def: EnemyDef): string | null {
  // 素の行動が固定 (乱択の節を持たない) で、伏せ分岐が2本以上ある敵だけ
  if (Object.values(def.nodes).some((n) => n.random !== undefined)) return null
  if (!def.movesVsSet || def.movesVsSet.length < 2) return null
  return '順番を崩す=向きは毎ターン変わる'
}

/** レリックの層の表示タグ (2026-09-03 本家式の層。CLI/UI共用。common は無印) */
export function relicRarityTag(def: RelicDef): string {
  switch (def.rarity ?? 'common') {
    case 'uncommon': return '◆アンコモン'
    case 'rare': return '★レア'
    case 'boss': return '👑ボス'
    case 'shop': return '🛒店売り'
    case 'event': return '❓イベント'
    default: return ''
  }
}

/**
 * 残機・分裂の予告HP: 分裂体は素の値×親のHP倍率 (幕・ボス係数・難易度) で出る (確定済みルール表「敵ギミック第1波」)。
 * 表示は必ずこの実値を出す (2026-09-03 Opusラン K「二の相HP55」の予告に対し実際は132 → CLI は是正済みだったが
 * ブラウザUIは素の値のままだった = 2026-09-06 人間ラン#8「復活するときの体力の説明が違う」)
 */
export function splitChildHp(parent: { readonly maxHp: number }, parentDef: EnemyDef, childDef: EnemyDef): number {
  const ratio = parentDef.maxHp > 0 ? parent.maxHp / parentDef.maxHp : 1
  return Math.max(1, Math.round(childDef.maxHp * ratio))
}

