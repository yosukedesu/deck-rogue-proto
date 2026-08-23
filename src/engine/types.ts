// engine/types.ts — ルールエンジンの型定義とデータスキーマ
// ⚠️ このディレクトリ (engine/) は DOM・React・描画に一切依存しないこと。
//    Node 単体で動く純ロジックのみを置く (CLAUDE.md「アーキテクチャ原則」)。

// ============================================================
// リアクション方式 (本プロジェクトの主役: 3方式を差し替え比較する)
// ============================================================

export type ReactionMode = 'set-auto' | 'hold-manual' | 'set-confirm'

/**
 * リアクション方式の差し替えインターフェース。
 * 方式固有のロジックは必ずこの実装の中に閉じ込める。
 * エンジン本体 (state.ts / イベントパイプライン) に方式の if 分岐が
 * 漏れたら設計ミス (CLAUDE.md)。
 */
export interface ReactionSystem {
  readonly mode: ReactionMode
  /** この方式で受け付けるコマンドか (UI の操作可否判定にも使う) */
  canHandle(state: GameState, command: Command): boolean
  /** イベント発生時のフック。方式固有の割り込み処理を返す */
  onEvent(state: GameState, event: GameEvent): GameState
}

// ============================================================
// 状態 (イミュータブル)
// ============================================================

/** シード付き RNG の状態。GameState に埋め込み、消費のたびに新しい値へ差し替える */
export interface RngState {
  readonly seed: number
  readonly counter: number
}

export interface CombatantState {
  readonly hp: number
  readonly maxHp: number
  readonly block: number
}

export interface PlayerState extends CombatantState {
  readonly energy: number
  readonly energyMax: number // 緑の柱①: ランプで戦闘中のみ増える (戦闘ごとにリセット)
  readonly hand: readonly CardInstance[]
  readonly drawPile: readonly CardInstance[]
  readonly discardPile: readonly CardInstance[]
  /** 伏せているカード (同時1枚が現ルール。レリックで拡張余地) */
  readonly setCards: readonly CardInstance[]
  /** 緑の柱④: 成長カウンター (戦闘内のみ) */
  readonly growth: number
}

export interface EnemyState extends CombatantState {
  readonly enemyId: string
  readonly intent: EnemyIntent | null
}

/** 敵の意図。プレイヤーへは幅あり表示 (例: 攻撃6〜12) するが、内部では実値も持つ */
export interface EnemyIntent {
  readonly kind: 'attack' | 'probe' | 'destroy-set' | 'buff' | 'defend'
  readonly shownMin: number
  readonly shownMax: number
  /** 実際の値。UI には見せない */
  readonly actual: number
}

export type CombatPhase = 'player-turn' | 'enemy-turn' | 'awaiting-reaction-confirm' | 'won' | 'lost'

export interface GameState {
  readonly rng: RngState
  readonly reactionMode: ReactionMode
  readonly phase: CombatPhase
  readonly turn: number
  readonly player: PlayerState
  readonly enemies: readonly EnemyState[]
  /** 発生済みイベントログ (リプレイ・シミュレーション統計の材料) */
  readonly eventLog: readonly GameEvent[]
}

// ============================================================
// コマンド (UI / bot はこれを投げるだけ)
// ============================================================

export type Command =
  | { readonly type: 'StartCombat'; readonly seed: number; readonly enemyIds: readonly string[] }
  | { readonly type: 'PlayCard'; readonly cardUid: string; readonly targetIndex?: number }
  | { readonly type: 'SetCard'; readonly cardUid: string } // set-auto / set-confirm 用
  | { readonly type: 'ReactManual'; readonly cardUid: string } // hold-manual 用
  | { readonly type: 'ConfirmReaction'; readonly fire: boolean } // set-confirm 用 (発動/温存)
  | { readonly type: 'EndTurn' }

// ============================================================
// イベント (戦闘内の出来事はすべてイベント。効果はフックとして実装)
// ============================================================

export type GameEvent =
  | { readonly type: 'CombatStarted' }
  | { readonly type: 'TurnStarted'; readonly turn: number }
  | { readonly type: 'TurnEnded'; readonly turn: number }
  | { readonly type: 'CardPlayed'; readonly cardId: string }
  | { readonly type: 'CardSet'; readonly cardId: string }
  | { readonly type: 'EnemyIntentDeclared'; readonly enemyIndex: number; readonly intent: EnemyIntent }
  | { readonly type: 'DamageDealt'; readonly source: 'player' | 'enemy'; readonly amount: number }
  | { readonly type: 'ReactionTriggered'; readonly cardId: string; readonly mode: ReactionMode }
  | { readonly type: 'ReactionWhiffed'; readonly cardId: string } // 空振り (次ターン持続が現ルール)
  | { readonly type: 'CombatEnded'; readonly result: 'won' | 'lost' }

// ============================================================
// データスキーマ (data/*.json)
// JSON は本実装 (Unity 等) へそのまま持っていく共通資産。
// エンジン都合の値を混ぜない (CLAUDE.md「データ駆動」)。
// ============================================================

export type CardCategory = 'ramp' | 'attack' | 'defend' | 'finisher' | 'reaction' | 'growth'

/** 宣言的効果。表現できないものだけ scriptId で名前付きスクリプト効果に逃がす */
export interface DeclarativeEffect {
  readonly trigger?: 'onPlay' | 'onAttacked' | 'onEnemyIntent' | 'onTurnEnd'
  readonly effect: 'dealDamage' | 'gainBlock' | 'gainEnergyMax' | 'addGrowth' | 'counter' | 'script'
  readonly amount?: number
  readonly scriptId?: string
}

export interface CardDef {
  readonly id: string
  readonly name: string
  readonly cost: number
  readonly category: CardCategory
  readonly effects: readonly DeclarativeEffect[]
}

/** デッキ/手札上のカード実体 (同名カード複数を区別する uid 付き) */
export interface CardInstance {
  readonly uid: string
  readonly def: CardDef
}

export type EnemyArchetype = 'wide-power' | 'probe' | 'set-wary' | 'set-breaker' | 'brute'

export interface EnemyDef {
  readonly id: string
  readonly name: string
  readonly archetype: EnemyArchetype
  readonly maxHp: number
}
