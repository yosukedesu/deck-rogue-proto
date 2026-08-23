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
 * エンジン本体 (combat.ts / state.ts) に方式の if 分岐が漏れたら設計ミス (CLAUDE.md)。
 */
export interface ReactionSystem {
  readonly mode: ReactionMode
  /** この方式で受け付けるコマンドか (UI の操作可否判定にも使う) */
  canHandle(state: GameState, command: Command): boolean
  /** 方式固有コマンド (SetCard / ReactManual / ConfirmReaction) の処理 */
  handleCommand(state: GameState, command: Command): GameState
  /**
   * イベントフック。エンジンは敵の行動実行直前に EnemyActionExecuting を、
   * 敵フェーズ終端に EnemyPhaseEnded を流す。
   * - 割り込みが必要なら phase を 'awaiting-reaction' にして返す (エンジンが中断する)
   * - 即時発動 (set-auto) はここで解決して返す
   * - 空振り (ReactionWhiffed) の計上も方式固有のためここで行う
   */
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
  /** 伏せているカード (同時1枚が現ルール。レリック拡張余地) */
  readonly setCards: readonly CardInstance[]
  /** 置物: プレイすると場に残り戦闘中ずっと効果を発揮 (破壊不可・伏せ破壊の対象外) */
  readonly permanents: readonly CardInstance[]
  /** 消滅したカード (この戦闘から除外。再シャッフルされない) */
  readonly exhaustPile: readonly CardInstance[]
  /** 緑の柱④: 成長カウンター (戦闘内のみ)。プレイヤーの与ダメージ全てに加算 */
  readonly growth: number
  /** トランプルの核「勢い」: 同一ターン中の以降の攻撃ダメージに加算。自ターン終了時にリセット */
  readonly momentum: number
  /** 氷壁 (青): ターン開始で消えないブロック。通常ブロックを使い切った後に消費される */
  readonly iceBlock: number
  /** 詠唱数 (青のストーム): このターンにプレイしたカード数。ターン開始でリセット */
  readonly cardsPlayedThisTurn: number
  /** 霊気 (青): 妨害・リアクションの成功で溜まるエネルギー (戦闘内持続)。霊気放出で全消費する */
  readonly aether: number
  /**
   * マナ軽減トークン: 次にプレイする1枚のコストを軽減して消費される。
   * 素のコスト0のカードは消費しない。伏せるコストは対象外。未使用分は持ち越し
   */
  readonly nextCardDiscount: number
}

export interface EnemyState extends CombatantState {
  readonly enemyId: string
  readonly intent: EnemyIntent | null
  /** 強化 (StSの筋力)。攻撃の実値と幅表示の両方に加算される */
  readonly strength: number
  /** 行動ローテーション (sequence) の現在位置。sequence を持たない敵では未使用 */
  readonly patternIndex: number
}

/** 敵の意図。プレイヤーへは幅あり表示 (例: 攻撃6〜12)。実値は宣言時にロール済みで非公開 */
export interface EnemyIntent {
  readonly kind: EnemyActionKind
  readonly shownMin: number
  readonly shownMax: number
  /** 実際の値。UI には見せない */
  readonly actual: number
}

/**
 * フェーズ。敵ターンは EndTurn コマンド内で同期的に解決される。
 * リアクション方式が割り込みを要求した場合のみ 'awaiting-reaction' で中断し、
 * ReactManual / ConfirmReaction コマンドで再開する。
 */
export type CombatPhase = 'player-turn' | 'awaiting-reaction' | 'won' | 'lost'

/** リアクション誘発の追加条件 */
export interface EffectCondition {
  /** 自分のHPが maxHp×この比率 以下なら発動可 (例: 0.5 = 半分以下) */
  readonly hpAtOrBelowRatio?: number
  /** 直前に受けたダメージ (HP減) がこの値以上なら発動可 */
  readonly minDamageTaken?: number
  /** 敵の行動の実値がこの値以下なら発動可 (pre窓専用。マナ漏出など条件付き打ち消し) */
  readonly maxActionValue?: number
}

/**
 * 'awaiting-reaction' 中断中の再開情報。
 * stage 'pre' = 行動の実行前 (打ち消し・軽減の窓)、'post' = 行動の解決後 (返し系の窓)
 */
export interface PendingWindow {
  readonly enemyIndex: number
  readonly stage: 'pre' | 'post'
}

export interface GameState {
  readonly rng: RngState
  readonly reactionMode: ReactionMode
  readonly phase: CombatPhase
  readonly turn: number
  readonly player: PlayerState
  readonly enemies: readonly EnemyState[]
  /** 割り込み中断中の再開情報。通常は null */
  readonly pendingWindow: PendingWindow | null
  /** 次の敵行動を無効化 (打ち消し効果が立てる。方式非依存の汎用メカニクス) */
  readonly negateNextAction: boolean
  /** 直前に解決された敵の行動 (行動解決後リアクションの条件判定用。行動開始時にリセット) */
  readonly lastAction: {
    readonly enemyIndex: number
    readonly kind: EnemyActionKind
    readonly hpLoss: number
  } | null
  /** 発生済みイベントログ (リプレイ・シミュレーション統計の材料) */
  readonly eventLog: readonly GameEvent[]
}

// ============================================================
// コマンド (UI / bot はこれを投げるだけ)
// ============================================================

export type Command =
  | {
      readonly type: 'StartCombat'
      readonly seed: number
      readonly enemyId: string
      /** 使用デッキ (data/decks.json の id)。省略時は 'starter' */
      readonly deckId?: string
    }
  | {
      readonly type: 'PlayCard'
      readonly cardUid: string
      /** 選択式カード (modes) 用: 選んだモードの添字。modes を持つカードでは必須 */
      readonly modeIndex?: number
      /** 手札捨てコスト (discardCost) 用: 追加コストとして捨てる手札の uid。discardCost 枚数ぶん必須 */
      readonly discardUids?: readonly string[]
    }
  | { readonly type: 'SetCard'; readonly cardUid: string } // set-auto / set-confirm 用
  | { readonly type: 'ReactManual'; readonly cardUid: string } // hold-manual 用 (敵行動への割り込み)
  | { readonly type: 'ConfirmReaction'; readonly fire: boolean } // set-confirm: 発動/温存。hold-manual: fire=false でパス
  | { readonly type: 'EndTurn' }

// ============================================================
// イベント (戦闘内の出来事はすべてイベント。効果はフックとして実装)
// ============================================================

export type GameEvent =
  | { readonly type: 'CombatStarted'; readonly enemyId: string }
  | { readonly type: 'TurnStarted'; readonly turn: number }
  | { readonly type: 'TurnEnded'; readonly turn: number }
  | { readonly type: 'CardsDrawn'; readonly count: number }
  | { readonly type: 'CardPlayed'; readonly cardId: string }
  | { readonly type: 'CardSet'; readonly cardId: string }
  | { readonly type: 'EnemyIntentDeclared'; readonly enemyIndex: number; readonly intent: EnemyIntent }
  /** 敵行動の実行直前フック点 (pre窓)。ReactionSystem はこれを見て割り込む */
  | { readonly type: 'EnemyActionExecuting'; readonly enemyIndex: number; readonly kind: EnemyActionKind }
  /** 敵行動の解決後フック点 (post窓)。返し系リアクション・置物の茨はここで発動する */
  | {
      readonly type: 'EnemyActionResolved'
      readonly enemyIndex: number
      readonly kind: EnemyActionKind
      readonly hpLoss: number
    }
  | { readonly type: 'ActionNegated'; readonly enemyIndex: number }
  | {
      readonly type: 'DamageDealt'
      readonly source: 'player' | 'enemy'
      readonly amount: number // ブロック適用前の値
      readonly hpLoss: number // 実際に減った HP
    }
  | { readonly type: 'BlockGained'; readonly target: 'player' | 'enemy'; readonly amount: number }
  | { readonly type: 'IceBlockGained'; readonly amount: number } // 氷壁 (持ち越しブロック)
  | { readonly type: 'AetherGained'; readonly amount: number } // 霊気 (妨害の蓄積)
  | { readonly type: 'AetherDischarged'; readonly spent: number } // 霊気放出
  | { readonly type: 'DiscountGained'; readonly amount: number } // マナ軽減トークン
  | { readonly type: 'StrengthGained'; readonly enemyIndex: number; readonly amount: number }
  | { readonly type: 'EnergyGained'; readonly amount: number } // 一時マナ
  | { readonly type: 'MomentumAdded'; readonly amount: number }
  | { readonly type: 'PermanentPlayed'; readonly cardId: string }
  | { readonly type: 'CardExhausted'; readonly cardId: string } // 消滅
  | { readonly type: 'CardsDiscarded'; readonly cardIds: readonly string[] } // 手札捨てコスト
  | { readonly type: 'EnergyMaxGained'; readonly amount: number }
  | { readonly type: 'GrowthAdded'; readonly amount: number }
  | { readonly type: 'ReactionTriggered'; readonly cardId: string; readonly mode: ReactionMode }
  | { readonly type: 'ReactionWhiffed'; readonly cardId: string } // 空振り (伏せは無期限持続が現ルール)
  | { readonly type: 'SetCardDestroyed'; readonly cardId: string } // 伏せ破壊型の仕事
  | { readonly type: 'EnemyPhaseEnded'; readonly turn: number } // 空振り計上などのフック点
  | { readonly type: 'CombatEnded'; readonly result: 'won' | 'lost' }

// ============================================================
// データスキーマ (data/*.json)
// JSON は本実装 (Unity 等) へそのまま持っていく共通資産。
// エンジン都合の値を混ぜない (CLAUDE.md「データ駆動」)。
// スキーマ変更時はこのファイルを同時に更新する (C# クラス定義変換の一次資料)。
// ============================================================

/** カードの色 (MTGカラーパイ準拠)。data/*.json のファイル単位で決まり、読込時に付与される */
export type CardColor = 'green' | 'blue'

export type CardCategory =
  | 'ramp'
  | 'draw' // 青のドロー・ルーティング
  | 'attack'
  | 'defend'
  | 'finisher'
  | 'reaction'
  | 'growth'
  | 'permanent'

/**
 * 宣言的効果。表現できないものだけ scriptId で名前付きスクリプト効果に逃がす。
 * trigger:
 *   - onPlay: 自ターンにプレイした時
 *   - onAttackIncoming: 敵の攻撃でダメージを受ける直前 (軽減系リアクション)
 *   - onAttacked: 敵の攻撃でダメージを受けた後 (返し系リアクション・置物)
 *   - onEnemyAction: 敵の任意の行動の確定時・実行前 (打ち消し系リアクション)
 *   - onEnemyBuffed: 敵の強化の解決後 (リアクション)
 *   - onEnemyDefended: 敵が防御 (ブロック獲得) した後 (リアクション)
 *   - onTurnStart: 自ターン開始時 (置物)
 *   - onAttackPlayed: 攻撃カテゴリのカードをプレイした後 (置物。そのカード自身の解決後に発火)
 */
export interface DeclarativeEffect {
  readonly trigger:
    | 'onPlay'
    | 'onAttackIncoming'
    | 'onAttacked'
    | 'onEnemyAction'
    | 'onEnemyBuffed'
    | 'onEnemyDefended'
    | 'onTurnStart'
    | 'onAttackPlayed'
  /** 誘発の追加条件 (きつい条件ほど効果は派手に、が設計方針) */
  readonly condition?: EffectCondition
  readonly effect:
    | 'dealDamage'
    | 'gainBlock'
    | 'gainIceBlock' // 氷壁: ターン開始で消えず持ち越されるブロック (青)
    | 'dealDamagePerCardPlayed' // ストーム攻撃: 詠唱数 × amount のダメージ (青)
    | 'gainIceBlockPerCardPlayed' // ストーム防御: 詠唱数 × amount の氷壁 (青)
    | 'drawCardsPerCardPlayed' // ストームドロー: 詠唱数 × amount 枚ドロー (青)
    | 'addAether' // 霊気+X: 妨害・リアクション成功の蓄積 (青)
    | 'dischargeAether' // 霊気放出: 霊気×amount のダメージを与え、霊気を全消費 (青)
    | 'discountNext' // マナ軽減: 次にプレイするカードのコスト-X
    | 'gainEnergy' // 一時マナ: ターン終了までエナジー+X (energyMax は増えない)
    | 'gainEnergyMax'
    | 'addGrowth'
    | 'doubleGrowth' // 成長スタックのシグネチャー: 成長カウンターを2倍にする
    | 'addMomentum' // トランプルの核: 勢い+X (同一ターン中の以降の攻撃に加算)
    | 'dealDamagePerEnergyMax' // ビッグマナのシグネチャー: エナジー上限 × amount のダメージ
    | 'counter'
    | 'negate'
    | 'drawCards'
    | 'script'
  readonly amount?: number
  /** 貫通 (トランプル): このダメージは敵ブロックを無視する。dealDamage 系のみ有効 */
  readonly pierce?: boolean
  readonly scriptId?: string
}

/** 選択式カードのモード (プレイ時に1つを選ぶ) */
export interface CardMode {
  readonly name: string
  readonly effects: readonly DeclarativeEffect[]
}

export interface CardDef {
  readonly id: string
  readonly name: string
  readonly cost: number
  readonly category: CardCategory
  /** 色。JSONには書かず、content.ts が読込時にファイル単位で付与する */
  readonly color: CardColor
  /** 通常効果。modes を持つカードでは空配列にする */
  readonly effects: readonly DeclarativeEffect[]
  /** 選択式: プレイ時に modes から1つを選んで解決する */
  readonly modes?: readonly CardMode[]
  /** 消滅: 使用後この戦闘から除外される */
  readonly exhaust?: boolean
  /** 追加コスト: 手札を N 枚捨てる */
  readonly discardCost?: number
}

/** デッキ/手札上のカード実体 (同名カード複数を区別する uid 付き) */
export interface CardInstance {
  readonly uid: string
  readonly def: CardDef
}

export type EnemyArchetype = 'wide-power' | 'probe' | 'set-wary' | 'set-breaker' | 'brute' | 'charger'

/** buff = 強化 (StSの筋力上昇)。以降の攻撃の実値・幅表示に加算される */
export type EnemyActionKind = 'attack' | 'defend' | 'destroy-set' | 'buff'

/** 敵の1行動。attack/defend/buff は [min, max] を宣言時にロール。destroy-set は数値なし */
export interface EnemyMove {
  readonly id: string
  readonly kind: EnemyActionKind
  readonly min?: number
  readonly max?: number
  /** 重み抽選 (同テーブル内の相対値)。sequence を持つ敵では使われない */
  readonly weight: number
}

export interface EnemyDef {
  readonly id: string
  readonly name: string
  readonly archetype: EnemyArchetype
  readonly maxHp: number
  /** 行動定義。sequence がある場合は id 参照用の辞書を兼ねる */
  readonly moves: readonly EnemyMove[]
  /**
   * 行動ローテーション (StSのSentry等参考)。moves の id をこの順で繰り返す。
   * 指定時は重み抽選しない。movesVsSet の割り込みではローテーションは進まない
   */
  readonly sequence?: readonly string[]
  /** プレイヤーに伏せカードがある時に優先する行動テーブル (伏せ警戒型・伏せ破壊型)。省略時は通常行動 */
  readonly movesVsSet?: readonly EnemyMove[]
}

// ---- デッキ (アーキタイプ理想形の検証用プリセット) ----

export interface DeckCardEntry {
  readonly cardId: string
  readonly count: number
}

export interface DeckDef {
  readonly id: string
  readonly name: string
  readonly description: string
  /** デッキの色 (UI表示・ランの色対応に使う) */
  readonly color: CardColor
  readonly cards: readonly DeckCardEntry[]
}
