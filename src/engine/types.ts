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
  /**
   * このターン開始時点のエナジー上限 (上限参照札はこちらを読む。2026-08-30)。
   * 確定済みルール「ランプ即時利用の廃止 = 上限増加は次の自ターンから」が、実装では
   * エナジー補充にしか効いておらず、上限参照札 (幹撃等) が同ターンのランプを即座に
   * 数えていた (計測ランで発覚した仕様違反)。「今ランプするか今殴るか」の悩みを実装する
   */
  readonly energyMaxAtTurnStart: number
  /** 毎ターンのドロー枚数 (リーダーの個性で変わる) */
  readonly drawPerTurn: number
  readonly hand: readonly CardInstance[]
  readonly drawPile: readonly CardInstance[]
  readonly discardPile: readonly CardInstance[]
  /** 伏せているカード (基本は同時1枚。setSlots で拡張) */
  readonly setCards: readonly CardInstance[]
  /** 回収 (2026-08-30) したターン中、この uid の札は伏せ直しコスト不要 (自ターン終了でクリア) */
  readonly freeResetUid?: string
  /** 伏せ枠の数。既定1。かすみ (ディミア) のリーダー個性で2 (確定済みルール表「伏せ枚数」) */
  readonly setSlots: number
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
  /** この戦闘でプレイしたカードの累計 (ターンを跨いでリセットされない)。時喰らい型タイマーの参照値 */
  readonly cardsPlayedTotal: number
  /** 霊気 (青): 妨害・リアクションの成功で溜まるエネルギー (戦闘内持続)。霊気放出で全消費する */
  readonly aether: number
  /**
   * マナ軽減トークン: 次にプレイする1枚のコストを軽減して消費される。
   * 素のコスト0のカードは消費しない。伏せるコストは対象外。未使用分は持ち越し
   */
  readonly nextCardDiscount: number
  /** 衝動 (赤): 「このターン限り」の手札の uid。自ターン終了時に未使用なら消滅する */
  readonly impulseUids: readonly string[]
  /** 弱体: 残りNターンの間、与ダメージ25%減 (切り捨て)。自ターン終了時に1減る */
  readonly weak: number
  /** 脆弱: 残りNフェーズの間、敵の攻撃ダメージ50%増 (切り捨て・威嚇適用後)。敵フェーズ終了時に1減る */
  readonly vulnerable: number
  /** この戦闘でカード効果 (loseHp) により失ったHPの累計 (黒: 背徳の収穫の参照値。敵からの被弾は含まない) */
  readonly selfHpLost: number
  /** この戦闘でプレイしたランダム火力の枚数 (カオスの刈り取りの参照値。2026-08-30) */
  readonly randomPlayedThisCombat: number
  /** 直前の敵フェーズで受けた攻撃ダメージの合計 (赤: 逆上の参照値。敵フェーズ開始時にリセット) */
  readonly damageTakenLastEnemyPhase: number
  /** 反復トークン (青: 呪文コピー)。次に唱える呪文の効果を2回解決する。自ターン終了時にリセット (勢いと同じ持続則 = 敵フェーズに得た分は次の自ターンまで持つ) */
  readonly spellEchoes: number
}

export interface EnemyState extends CombatantState {
  readonly enemyId: string
  readonly intent: EnemyIntent | null
  /** 強化 (StSの筋力)。攻撃の実値と幅表示の両方に加算される */
  readonly strength: number
  /** 延焼 (赤のバーン)。毎敵フェーズ開始時にこの値のダメージ (ブロック無視) を受けて1減る */
  readonly burn: number
  /** 混乱 (青の精神攻撃)。攻撃が他の生存敵 (いなければ自分) に向かい、攻撃1回ごとに1減る */
  readonly confusion: number
  /** 急所 (敵版脆弱)。次に受けるプレイヤーダメージN回が+50%。1ヒットごとに1減る */
  readonly exposed: number
  /** 行動ローテーション (sequence) の現在位置。sequence を持たない敵では未使用 */
  readonly patternIndex: number
  /** 編成で反応テーブルを無効化された個体 (確定済みルール表「編成の反応テーブル」) */
  readonly noReactTable?: boolean
  /** 装甲: 1ヒットの被ダメ上限 (def からコピー。テスト・編成補正で上書き可) */
  readonly armor?: number
  /** この戦闘で受けた累計ダメージ (enrageEveryDamage の判定用。2026-08-30) */
  readonly damageTakenTotal?: number
  /** 前回の再生判定以降に受けた累計HP損失 (regenBreak の判定用。再生判定のたびにリセット) */
  readonly hpLostSinceRegen?: number
  /** とげ: プレイヤーの攻撃ヒットごとにNダメ反射 (defからコピー。確定済みルール表「とげ（敵の報復）」) */
  readonly thorns?: number
  /** 盗みで抱えているゴールド。精算は勝利時にrun層 (確定済みルール表「盗みと逃走」) */
  readonly stolenGold?: number
  /** 逃走済み (hp:0とセットで立つ = 既存の死亡判定がそのまま勝利判定に使える) */
  readonly fled?: boolean
}

/** 敵の意図。プレイヤーへは幅あり表示 (例: 攻撃6〜12)。実値は宣言時にロール済みで非公開 */
export interface EnemyIntent {
  readonly kind: EnemyActionKind
  readonly shownMin: number
  readonly shownMax: number
  /** 実際の値。UI には見せない。連撃 (hits>1) では1ヒット分の値 */
  readonly actual: number
  /** 連撃: ヒット数 (省略時1)。幅表示は「per-hit×N」 */
  readonly hits?: number
  /** 手数の鏡: 実行時にヒット数=このターンのプレイ枚数 (最低1) になる。表示は「×手数」 */
  readonly mirrorHits?: boolean
  /** 状態異常の付与予告 (意図表示に出す = フェアネス。確定済みルール表「状態異常」) */
  readonly inflict?: StatusInflict
  /** 攻防一体: 攻撃と同時に得る固定ブロック (意図表示「⚔️N+🛡️M」。確定済みルール表「攻防一体・隙」) */
  readonly alsoDefend?: number
  /**
   * 条件付き意図 (2026-08-25): 反応テーブルを持つ敵は「条件を満たすなら alt / 満たさないなら本体」の
   * 両方を宣言時に確定し、実行時の盤面で分岐する (確定済みルール表「条件付き意図」)。
   * 'set' = 伏せ札がある / 'tokens' = 従者・トークンが場にいる
   */
  readonly conditionalOn?: 'set' | 'tokens'
  /** conditionalOn を満たす時に実行される分岐 */
  readonly alt?: EnemyIntentBranch
}

/** 条件付き意図の分岐 (alt を再帰させないための素の形) */
export interface EnemyIntentBranch {
  readonly kind: EnemyActionKind
  readonly shownMin: number
  readonly shownMax: number
  readonly actual: number
  readonly hits?: number
  readonly inflict?: StatusInflict
  readonly alsoDefend?: number
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
  /**
   * 敵の行動の実値がこの値**以上**なら発動可 (pre窓専用。2026-08-26)。
   * maxActionValue の裏返しで「大技しか止められない打ち消し」を作れる。
   * 敵が育つほど条件が成立するので「脅威は指数的・防御は線形」への直接の答えになる
   */
  readonly minActionValue?: number
  /**
   * 猛り火 (2026-08-30。赤のカラーパイ再編)。**生存する敵の延焼の合計が BLAZE_THRESHOLD(8) 以上**
   * なら発動可。しきい値は全札で単一 (ユーザー判断)。延焼を溜めるほど札が化ける＝
   * 「勝ち筋が時間を要求し、弱点が時間を許さない」という赤の自己矛盾を、
   * 時間依存でなく**しきい値依存**に置き換える機構
   */
  readonly blaze?: boolean
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
  /** 敵の1行動につきリアクション1回まで、の消費フラグ。各行動の実行開始時にリセット (確定済みルール表「リアクション回数」) */
  readonly reactionUsedThisAction: boolean
  /** 直前に解決された敵の行動 (行動解決後リアクションの条件判定用。行動開始時にリセット) */
  readonly lastAction: {
    readonly enemyIndex: number
    readonly kind: EnemyActionKind
    readonly hpLoss: number
    /** その行動の実値 (2026-08-31: post窓の minActionValue 判定用) */
    readonly actual: number
  } | null
  /** 発生済みイベントログ (リプレイ・シミュレーション統計の材料) */
  readonly eventLog: readonly GameEvent[]
  /** C型レリック (静かな鈴): 伏せ札がある間、敵の攻撃実値-N。旧セーブに無いので optional */
  readonly setDamageReduction?: number
  /** C型レリック (蜃気楼の面): 意図の実値を常時公開。旧セーブに無いので optional */
  readonly revealIntents?: boolean
}

// ============================================================
// コマンド (UI / bot はこれを投げるだけ)
// ============================================================

export type Command =
  | {
      readonly type: 'StartCombat'
      readonly seed: number
      /** 敵ID (ソロ編成) または encounters.json の編成ID。編成IDが優先 */
      readonly enemyId: string
      /** 使用デッキ (data/decks.json の id)。省略時は 'starter' */
      readonly deckId?: string
      /** リーダー (data/leaders.json の id)。省略時はリーダーなしの素のルール */
      readonly leaderId?: string
    }
  | {
      readonly type: 'PlayCard'
      readonly cardUid: string
      /** 選択式カード (modes) 用: 選んだモードの添字。modes を持つカードでは必須 */
      readonly modeIndex?: number
      /** 手札捨てコスト (discardCost) 用: 追加コストとして捨てる手札の uid。discardCost 枚数ぶん必須 */
      readonly discardUids?: readonly string[]
      /** 単体対象カード用: 対象の敵 index。生存敵が2体以上いる場合は必須 (StS式ターゲティング) */
      readonly targetIndex?: number
      /** 消滅コスト (exhaustCost) 用: 追加コストとして消滅させる手札の uid。exhaustCost 枚数ぶん必須 */
      readonly exhaustUids?: readonly string[]
      /** retrieveFromExhaust / playFromExhaust 用: 消滅置き場から選ぶカードの uid */
      readonly retrieveUid?: string
    }
  | { readonly type: 'SetCard'; readonly cardUid: string } // set-auto / set-confirm 用
  | { readonly type: 'RetrieveSetCard'; readonly cardUid: string } // 回収 (2026-08-30): 1E払って伏せ札を手札に戻す
  | { readonly type: 'PlayNecro'; readonly cardUid: string; readonly targetIndex?: number } // 亡骸プレイ (黒 2026-08-31): 消滅置き場の necroCost 持ち札を一度だけプレイ (プレイ後はゲームから完全に取り除く)
  | { readonly type: 'ReactManual'; readonly cardUid: string } // hold-manual 用 (敵行動への割り込み)
  | {
      readonly type: 'ConfirmReaction'
      readonly fire: boolean // set-confirm: 発動/温存。hold-manual: fire=false でパス
      /** 伏せ2枚 (かすみ) 用: 発動する伏せ札の uid。窓に合致する伏せが複数ある時に指定。省略時は先頭の合致札 */
      readonly cardUid?: string
    }
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
  | { readonly type: 'SetCardRetrieved'; readonly cardId: string }
  | { readonly type: 'EnemyIntentDeclared'; readonly enemyIndex: number; readonly intent: EnemyIntent }
  /** 敵行動の実行直前フック点 (pre窓)。ReactionSystem はこれを見て割り込む */
  | { readonly type: 'EnemyActionExecuting'; readonly enemyIndex: number; readonly kind: EnemyActionKind }
  /** 敵行動の解決後フック点 (post窓)。返し系リアクション・置物の茨はここで発動する */
  | {
      readonly type: 'EnemyActionResolved'
      readonly enemyIndex: number
      readonly kind: EnemyActionKind
      readonly hpLoss: number
      readonly actual: number
    }
  | { readonly type: 'ActionNegated'; readonly enemyIndex: number }
  | { readonly type: 'EnemyConfused'; readonly enemyIndex: number; readonly amount: number } // 混乱付与
  | {
      // 混乱による仲間割れ: enemyIndex の攻撃が targetIndex の敵 (自分自身もありうる) に命中
      readonly type: 'ConfusedAttack'
      readonly enemyIndex: number
      readonly targetIndex: number
      readonly amount: number
    }
  | {
      readonly type: 'DamageDealt'
      readonly source: 'player' | 'enemy'
      readonly amount: number // ブロック適用前の値
      readonly hpLoss: number // 実際に減った HP
      /** 装甲で切り捨てられた量 (2026-08-31 収穫ラン指摘「切られた量が見えないと積むのをやめる判断を学習できない」) */
      readonly armorCut?: number
    }
  | { readonly type: 'BlockGained'; readonly target: 'player' | 'enemy'; readonly amount: number }
  | { readonly type: 'IceBlockGained'; readonly amount: number } // 氷壁 (持ち越しブロック)
  | { readonly type: 'AetherGained'; readonly amount: number } // 霊気 (妨害の蓄積)
  | { readonly type: 'SpellEchoed'; readonly cardId: string } // 反復 (青): 呪文の効果が2回解決された
  | { readonly type: 'NecroFired'; readonly cardId: string } // 亡骸効果 (黒): 消滅した札の亡骸効果が発火した
  | { readonly type: 'NecroPlayed'; readonly cardId: string } // 亡骸プレイ (黒): 消滅置き場からプレイされ、ゲームから取り除かれた
  | { readonly type: 'AetherDischarged'; readonly spent: number } // 霊気放出
  | { readonly type: 'DiscountGained'; readonly amount: number } // マナ軽減トークン
  | { readonly type: 'BurnApplied'; readonly enemyIndex: number; readonly amount: number } // 延焼付与
  | { readonly type: 'BurnTick'; readonly enemyIndex: number; readonly amount: number } // 延焼ダメージ
  | { readonly type: 'StatusInflicted'; readonly status: PlayerStatus; readonly amount: number } // 状態異常付与
  | { readonly type: 'RegenTicked'; readonly enemyIndex: number; readonly amount: number }
  | { readonly type: 'RegenBroken'; readonly enemyIndex: number } // 再生回復
  | { readonly type: 'BlockShattered'; readonly enemyIndex: number; readonly amount: number } // 粉砕
  | { readonly type: 'ImpulseDrawn'; readonly count: number } // 衝動 (このターン限りの手札)
  | { readonly type: 'HpLost'; readonly amount: number } // 自傷
  | { readonly type: 'StrengthGained'; readonly enemyIndex: number; readonly amount: number }
  | { readonly type: 'EnergyGained'; readonly amount: number } // 一時マナ
  | { readonly type: 'MomentumAdded'; readonly amount: number }
  | { readonly type: 'PermanentPlayed'; readonly cardId: string }
  | { readonly type: 'CardExhausted'; readonly cardId: string } // 消滅
  | { readonly type: 'BurnDischarged'; readonly enemyIndex: number; readonly amount: number } // 爆熱: 延焼の換金
  | { readonly type: 'TokenDestroyed'; readonly cardId: string } // トークン破壊 (敵メカニクス)
  | { readonly type: 'ThornsReflected'; readonly enemyIndex: number; readonly amount: number; readonly hpLoss: number } // とげ反射 (確定済みルール表「とげ（敵の報復）」)
  | { readonly type: 'GoldStolen'; readonly enemyIndex: number; readonly amount: number } // 盗み (精算は勝利時)
  | { readonly type: 'EnemyFled'; readonly enemyIndex: number } // 逃走 (戦闘離脱)
  | { readonly type: 'EnemyHealed'; readonly enemyIndex: number; readonly targetIndex: number; readonly amount: number } // 回復役
  | { readonly type: 'CardRetrieved'; readonly cardId: string } // 屍集め: 消滅置き場から手札へ
  | { readonly type: 'CardPlayedFromExhaust'; readonly cardId: string } // 死者再生: 消滅置き場から直接プレイ
  | { readonly type: 'CardsDiscarded'; readonly cardIds: readonly string[] } // 手札捨てコスト
  | { readonly type: 'EnergyMaxGained'; readonly amount: number }
  | { readonly type: 'GrowthAdded'; readonly amount: number }
  | { readonly type: 'GrowthDischarged'; readonly spent: number } // 成長放出 (開花の蔦)
  | { readonly type: 'HpHealed'; readonly amount: number } // 回復 (白)
  | { readonly type: 'CardsMilled'; readonly count: number; readonly cardIds?: readonly string[] } // 忘却=山札からの消滅 (黒)。cardIds=何が墓地へ行ったか (2026-08-31 可視化)
  | { readonly type: 'EnemyWeakened'; readonly enemyIndex: number; readonly amount: number } // 威圧 (白)
  | { readonly type: 'ExposedApplied'; readonly enemyIndex: number; readonly amount: number } // 急所付与
  | { readonly type: 'ReactionTriggered'; readonly cardId: string; readonly mode: ReactionMode }
  /**
   * set-confirm で「温存」を選んだ記録 (2026-08-26)。
   * 発動/温存の判断こそがこの方式の主題なのに、温存だけが一切ログに残っていなかった。
   * ReactionWhiffed は敵フェーズ終端の残存枚数なので、意図的な温存とは別物
   */
  | {
      readonly type: 'ReactionHeld'
      readonly enemyIndex: number
      readonly stage: 'pre' | 'post'
      readonly kind: EnemyActionKind
      /** pre窓は敵行動の実値、post窓は被ったHP減 */
      readonly value: number
      /** その窓で発動できた候補 (cardId) */
      readonly candidateIds: readonly string[]
    }
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
export type CardColor = 'green' | 'blue' | 'red' | 'white' | 'black'

/**
 * カードタイプ (MTGのカードタイプ相当。機械的な挙動で切る):
 * - physical: 物理。通常プレイの使い切り (肉体・武器・自然の力)
 * - spell: 呪文。通常プレイの使い切り (魔法・術式)。物理との分割は
 *   「物理耐性の敵」「呪文数参照」などの将来の設計余地のため
 * - reaction: リアクション。伏せて敵の行動に誘発 (MTGのインスタント相当)
 * - permanent: 置物。場に残り戦闘中持続 (エンチャント相当)
 * 表示名 (世界観ラベル) は UI 側のラベルマップで差し替える
 */
export type CardType = 'physical' | 'spell' | 'reaction' | 'permanent'

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
    | 'onCombatStart' // 戦闘開始時に1回 (レリック用。第1ターンのドロー・意図宣言の後に発火)
    | 'onAttackPlayed' // 攻撃カードをプレイした時 (置物、および伏せ札の自己誘発)
    | 'onSpellPlayed' // 呪文カードをプレイした時 (伏せ札の自己誘発。物理/呪文分割の機構的活用)
    | 'onCardPlayed' // カードを1枚プレイするたび (種類を問わない。赤の「手数」= ひばなのパッシブ)
    | 'onSetDestroyed' // この伏せ札が敵に破壊された時 (罠仕掛けの火薬)
    | 'onHealed' // 実回復 (>0) が発生した時 (置物。黒: 血の月。ドレイン・リーダーパッシブでも誘発)
    | 'onBlockGained' // プレイヤーがブロックを得るたび (置物。白の要塞: 城壁の弩。StS Juggernaut型。氷壁は別経路なので誘発しない)
    | 'onActionNegated' // 敵の行動を打ち消した時 (置物。青のパーミッション: 還流の水鏡。negate / negateConvertIce 共通)
    | 'onHpLost' // カード効果で自分のHPを失った時 (置物。黒: 苦痛の芯。敵からの被弾では誘発しない=StSルプチャー式)
    | 'onCardExhausted' // カードが消滅するたび (置物。黒: 亡者の合唱。忘却・消滅コスト・消滅札・衝動失効すべて)
    | 'onCostExhausted' // 消滅コスト (exhaustCost) を支払った時のみ (置物。黒: 闇市の帳簿)
    | 'onPermanentEntered' // 置物が場に出るたび (白の接着剤。プレイ・召喚・直接プレイすべて。自身の登場にも誘発。戦闘開始時から場にあるもの=リーダー/レリックは「登場」しない)
    | 'onImpulsePlayed' // 衝動カードをプレイした時 (赤の接着剤: 刹那の焔)
    | 'onRandomPlayed' // ランダム火力の札をプレイした時 (赤カオスの接着剤: 賭博師の焔。2026-08-30)
    | 'onAetherGained' // 霊気を得るたび (青の接着剤: 静電の帳。妨害の成功が自動火力になる)
    | 'onCardSet' // カードを伏せるたび (レリック: 符師の懐。set-confirmシナジー)
    | 'onReactionFired' // リアクションが発動するたび (置物。緑: 狩人の眼光=読み勝ちの換金。自己誘発・全方式共通)
    | 'onSelfExhausted' // 亡骸効果 (黒 2026-08-31): この札が「プレイ以外の経路」(ミル・消滅コスト・衝動失効) で消滅した時。プレイして消滅した場合は発火しない (onPlayが仕事を終えているため)
  /** 誘発の追加条件 (きつい条件ほど効果は派手に、が設計方針) */
  readonly condition?: EffectCondition
  readonly effect:
    | 'dealDamage'
    | 'gainBlock'
    | 'gainIceBlock' // 氷壁: ターン開始で消えず持ち越されるブロック (青)
    | 'dealDamagePerCardPlayed' // ストーム攻撃: 詠唱数 × amount のダメージ (青)
    | 'dealDamagePerCardPlayedTotal' // 大津波 (青 2026-08-31): この戦闘の累計プレイ数 × amount。長く戦うほど肥えるフィニッシャー
    | 'dealDamagePerMomentum' // トランプル換金: 勢い × amount のダメージ (緑。勢いは消費しない)
    | 'doubleMomentum' // トランプルの倍加: 現在の勢いを2倍にする (緑。角笛)
    | 'gainIceBlockPerCardPlayed' // ストーム防御: 詠唱数 × amount の氷壁 (青)
    | 'drawCardsPerCardPlayed' // ストームドロー: 詠唱数 × amount 枚ドロー (青)
    | 'addAether' // 霊気+X: 妨害・リアクション成功の蓄積 (青)
    | 'dischargeAether' // 霊気放出: 霊気×amount のダメージを与え、霊気を全消費 (青)
    | 'discountNext' // マナ軽減: 次にプレイするカードのコスト-X
    | 'applyBurn' // 延焼+X: 敵への継続ダメージ (赤)
    | 'shatterBlock' // 粉砕: 敵のブロックを全て破壊する (赤)
    | 'dealDamageRandom' // ランダム火力: amount〜amountMax のロールでダメージ (赤)
    | 'impulseDraw' // 衝動: 山札の上からX枚を「このターン限り」の手札に加える (赤)
    | 'loseHp' // 自傷: 自分のHPを失う (赤のコスト)
    | 'gainEnergy' // 一時マナ: ターン終了までエナジー+X (energyMax は増えない)
    | 'gainEnergyMax'
    | 'addGrowth'
    | 'doubleGrowth' // 成長スタックのシグネチャー: 成長カウンターを2倍にする
    | 'addMomentum' // トランプルの核: 勢い+X (同一ターン中の以降の攻撃に加算)
    | 'dealDamagePerEnergyMax' // ビッグマナのシグネチャー: エナジー上限 × amount のダメージ
    | 'counter'
    | 'negate'
    | 'confuse' // 混乱+X: 敵の攻撃が他の生存敵 (いなければ自分) に向かう (青の精神攻撃)
    | 'exposeEnemy' // 急所+X: その敵が次に受けるプレイヤーダメージX回が+50% (敵版脆弱)
    | 'gainHp' // 回復 (白の専売): 最大HPまで回復
    | 'weakenEnemy' // 威圧 (白): 敵の強化を-X (攻撃は最低1クランプの既存則)
    | 'dealDamagePerBlock' // 要塞型ペイオフ: 現在のブロック×Xダメージ (ボディスラム型)
    | 'dealDamagePerPermanent' // 集結 (白): 置物の数×Xダメージ (従者の横並び参照)
    | 'dealDamageDrain' // ドレイン (黒の専売): Xダメージを与え、floor(X/2)回復
    | 'exhaustFromDeck' // 忘却 (黒): 山札の上X枚を消滅させる (捨て札はリシャッフルで空になるため消滅を墓地とする)
    | 'dealDamagePerExhaust' // 墓地参照 (黒): 消滅した枚数×Xダメージ (単調増加。衝動失効・消滅札とも共鳴)
    | 'dealDamageDrainPerExhaust' // 墓地参照ドレイン (黒): 消滅枚数×Xダメージ + 半分回復 (死霊の饗宴)
    | 'dealDamagePerSelfHpLost' // 自傷の換金 (黒): この戦闘でカード効果により失ったHP×Xダメージ (背徳の収穫)
    | 'retrieveFromExhaust' // コスト再利用 (黒): 消滅置き場から1枚選んで手札に戻す (屍集め。combat.ts が retrieveUid で解決)
    | 'playFromExhaust' // コスト再利用 (黒): 消滅置き場のリアクション以外1枚をコストを支払わず直接プレイ (死者再生)
    | 'summonPermanent' // 召喚 (白): summonId の置物トークンを amount 体場に出す (従者の横並び=トークン再現)
    | 'dischargeBurn' // 爆熱 (赤): 対象の延焼×amount のダメージを与え、延焼を全て失わせる (DoT+焼き切りを手放す緊張)
    | 'shatterBlockConvert' // 破城槌 (赤): 敵のブロックを全て破壊し、破壊した値と同じダメージを与える
    | 'dealDamageExecute' // 処刑 (赤): amount ダメージ。対象のHPが最大の25%以下なら amountMax ダメージ
    | 'dealDamagePerDamageTaken' // 逆上 (赤): 直前の敵フェーズで受けたダメージ×amount (憤怒=被弾の換金)
    | 'dealDamagePerRandomPlayed' // 一擲乾坤 (赤カオス): この戦闘で撃ったランダム火力の枚数×amount
    | 'dealDamagePerHandCard' // 抱え込み (青): 手札の枚数×amount のダメージ (解決時の手札 = 自身・追加コストは数えない)
    | 'gainIceBlockPerHandCard' // 抱え込み (青): 手札の枚数×amount の氷壁
    | 'addSpellEcho' // 反復 (青): 次に唱える呪文の効果を2回解決するトークン+X (自ターン終了時に消える)
    | 'blessRetainers' // アンセム (白 2026-08-31): この置物がある間、従者 (retainer) の量つき効果+X (常在。栄光の頌歌型)
    | 'applyBurnPerDamageTaken' // 業腹 (赤): 直前の敵フェーズで受けたダメージ×amount の延焼 (憤怒→猛り火の橋)
    | 'dealDamagePerIceBlock' // 氷の槍 (青): 現在の氷壁×amount のダメージ (蓄積の換金)
    | 'negateConvertIce' // 魔力盗み (青): 打ち消し + その行動の実値ぶん氷壁を得る
    | 'dischargeAetherDraw' // 霊気の奔流 (青): 霊気×amount 枚ドローして霊気を全消費 (放出の第二の出口)
    | 'dealDamagePerNegStrength' // 威圧の換金 (白): 対象の強化がマイナスなら その絶対値×X の追加ダメージ (断罪の槌)
    | 'gainBlockPerPermanent' // 隊列の盾 (白): 置物の数×X ブロック
    | 'gainBlockPerEnergyMax' // 巨木の盾 (緑): エナジー上限×X ブロック (ランプ中の無防備を受けるスケーリング防御)
    | 'gainBlockPerExhaust' // 亡者の壁 (黒): 消滅した枚数×X ブロック (墓地型のタイマー耐性)
    | 'dischargeGrowthBlock' // 守りの刈り (緑 2026-08-31): 成長×Nブロックを得て成長を全て失う (収穫の性格付け)
    | 'dischargeGrowth' // 成長放出: 成長×Xダメージを与え、成長を全て失う (緑)
    | 'dischargeMomentumBurn' // 火移し (赤): 勢い×amount の延焼を与え、勢いを全て失う (手数→猛り火の橋)
    | 'dischargeMomentumBlock' // 余勢の構え (赤): 勢い×amount のブロックを得て、勢いを全て失う (攻めの勢いが守りになる)
    | 'dealDamageCleave' // キル連鎖: Xダメージ。対象が倒れたら別の生存敵に同値
    | 'drawCards'
    | 'script'
  readonly amount?: number
  /** dealDamageRandom 用: ロールの上限 (下限は amount) */
  readonly amountMax?: number
  /** 貫通 (トランプル): このダメージは敵ブロックを無視する。dealDamage 系のみ有効 */
  readonly pierce?: boolean
  /** Xコスト札専用: この効果を支払ったX回ぶん繰り返す (大角の暴走=6ダメ×X貫通) */
  readonly xHits?: boolean
  /** dealDamagePerBlock 用: 解決後にブロックを全て失う (壁を売り払う)。VPの二重計上を消す歯止め */
  readonly spendBlock?: boolean
  /** 全体攻撃: 'all' で生存する敵全体に解決する (dealDamage/applyBurn/shatterBlock 等)。省略時は単体 */
  readonly target?: 'all'
  /** summonPermanent 用: 場に出す置物カードの id (例: white_perm_squire) */
  readonly summonId?: string
  /**
   * 忘却の刻 (黒のしきい値。確定済みルール表「忘却の刻」): 消滅置き場がこの枚数以上なら
   * amount の代わりに amountMax を使う。dealDamageRandom / dealDamageExecute とは併用しない
   */
  readonly exhaustThreshold?: number
  readonly scriptId?: string
}

/** 選択式カードのモード (プレイ時に1つを選ぶ) */
export interface CardMode {
  readonly name: string
  readonly effects: readonly DeclarativeEffect[]
}

/** ?マス (イベント) の結果効果 (ギャンブルの当たり/外れ用) */
export interface EventOutcome {
  readonly gold?: number
  readonly hp?: number
  readonly wounds?: number
}

/** ?マス (イベント) の選択肢。効果は宣言的 (確定済みルール表「?マス（イベント）」) */
export interface EventChoiceDef {
  readonly label: string
  /** ゴールド増減 (負値は支払い) */
  readonly gold?: number
  /** HP増減 (最大HPまで。負値は自傷。0未満にはならず、0になったらラン敗北) */
  readonly hp?: number
  /** 最大HP増加 (現在HPも同量増える) */
  readonly maxHp?: number
  /** 負傷カードをデッキに混入する枚数 */
  readonly wounds?: number
  /** 色プールからランダムなカードをN枚獲得 */
  readonly addRandomCards?: number
  /** レリック候補列の次の1個を獲得 (上限なら何も起きない) */
  readonly relic?: boolean
  /** デッキから1枚を除去 (EventChoice.cardIndex で対象指定) */
  readonly removeCard?: boolean
  /** デッキの1枚を鍛える (EventChoice.cardIndex で対象指定) */
  readonly upgradeCard?: boolean
  /** この選択肢に必要な所持ゴールド (不足なら選べない) */
  readonly requireGold?: number
  /** ギャンブル: chance の確率で win、外れたら lose (ロールはラン RNG = 決定的) */
  readonly gamble?: {
    readonly chance: number
    readonly win: EventOutcome
    readonly lose: EventOutcome
  }
  // ---- 2026-08-29 本家踏襲の拡充で追加 ----
  /**
   * 現在HPを「最大HPの比率」で増減 (正=回復・負=ダメージ。切り捨て)。
   * 本家イベントの過半が最大HP比。固定値だとリーダー間 (80/75/65/60) で意味が壊れる
   */
  readonly hpRatio?: number
  /** cardIndex の1枚を除去し、同じレアリティの別のカードに置き換える (本家 Transmogrifier) */
  readonly transformCard?: boolean
  /** cardIndex の1枚を複製する (本家 Duplicator) */
  readonly duplicateCard?: boolean
  /** 強化可能な札からランダムにN枚を鍛える (対象選択は不要。本家 Shining Light) */
  readonly upgradeRandomCards?: number
  /** デッキの負傷カードを全て取り除く (本家 The Divine Fountain)。0枚なら何も起きない */
  readonly removeAllWounds?: boolean
}

/** ?マス (イベント) の定義。data/events.json が一次資料 */
export interface EventDef {
  readonly id: string
  /**
   * 層 (本家の3層構造。2026-08-29)。省略=幕専用 (引いたら二度と出ない) /
   * shrine=祠 (幕をまたぐと復活する) / oneTime=1ランで1回
   */
  readonly kind?: 'act' | 'shrine' | 'oneTime'
  /** 出現する幕。省略=全幕 (祠・ワンタイムの既定) */
  readonly act?: number
  readonly name: string
  readonly sprite?: string
  readonly flavor: string
  readonly choices: readonly EventChoiceDef[]
}

export interface CardDef {
  /**
   * レアリティ (確定済みルール表「レアリティ」2026-08-29)。報酬抽選はスロットごとに
   * コモン60%/アンコモン37%/レア3%の本家比率。未指定はコモン扱い (凍結色は解凍時に割当)
   */
  readonly rarity?: 'common' | 'uncommon' | 'rare'
  /**
   * Xコスト (確定済みルール表「Xコスト」2026-08-29): プレイ時に現在のエナジーを全て支払い、
   * 支払った量Xを xHits 効果が参照する。プレイ条件はエナジー1以上。割引の対象外。
   * cost フィールドは名目値 (カーブ集計用に1を置く)
   */
  readonly xCost?: boolean
  /** 猛り火 (延焼合計8以上) の間、このカードのコストがこの値だけ下がる (2026-08-30) */
  readonly blazeDiscount?: number
  /**
   * アーキタイプの軸 (報酬抽選の重み付け用。確定済みルール表「軸の重み付け」)。
   * 効果名から自動導出できない札 (多段ヒットの成長ペイオフ・貫通のトランプル札など) だけ明示する。
   */
  readonly axis?: readonly string[]
  readonly id: string
  readonly name: string
  readonly cost: number
  readonly type: CardType
  /** 色。JSONには書かず、content.ts が読込時にファイル単位で付与する */
  readonly color: CardColor
  /** 通常効果。modes を持つカードでは空配列にする */
  readonly effects: readonly DeclarativeEffect[]
  /** 選択式: プレイ時に modes から1つを選んで解決する */
  readonly modes?: readonly CardMode[]
  /** 消滅: 使用後この戦闘から除外される */
  readonly exhaust?: boolean
  /** 亡骸プレイ (黒 2026-08-31): 消滅置き場からNエナジーで一度だけプレイできる。プレイ後はゲームから完全に取り除かれる (刻の燃料も減る)。割引 (discountNext) の対象外 */
  readonly necroCost?: number
  /** 追加コスト: 手札を N 枚捨てる */
  readonly discardCost?: number
  /** 追加コスト: 手札を N 枚消滅させる (黒。捨てより重いが墓地燃料になる) */
  readonly exhaustCost?: number
  /** 従者 (生き物の置物): 敵の「従者狩り」で破壊されうる。道具・オーラ系置物は対象外 (確定済みルール表「トークン破壊」) */
  readonly retainer?: boolean
}

/** デッキ/手札上のカード実体 (同名カード複数を区別する uid 付き) */
export interface CardInstance {
  readonly uid: string
  readonly def: CardDef
  /** 召喚トークン: 敵の「トークン破壊」の対象になる (手張り置物・リーダー・レリックは対象外) */
  readonly token?: boolean
  /**
   * 伏せの鮮度 (2026-08-30 見切り)。このターンに伏せられた札だけ true。
   * 敵の伏せ反応 (setAlt/movesVsSet) は**新しい札にだけ**反応する — 置きっぱなしの札は
   * 「織り込み済み」で敵の行動を変えない (蓋の対処)。ただし破壊 (destroy-set) の判定は
   * 鮮度を問わない = 晒し続けた札は壊されには行かれる。自ターン開始時に false へ
   */
  readonly setFresh?: boolean
  /**
   * 生得: 戦闘開始時から場にあるもの (リーダーパッシブ・レリック)。
   * 「登場」しないので onPermanentEntered が誘発せず、置物数参照 (集結など) でも数えない
   * (2026-08-26。確定済みルール表「置物数参照」)。パッシブが召喚したトークンは生得ではない。
   */
  readonly innate?: boolean
}

export type EnemyArchetype =
  | 'wide-power'
  | 'probe'
  | 'set-wary'
  | 'set-breaker'
  | 'brute'
  | 'charger'
  | 'hexer'
  | 'flurry'
  | 'regenerator'
  | 'taunter'
  | 'enrager'
  | 'support'
  | 'mimic' // 物真似 (手数の鏡 2026-08-31)
  | 'elite' // エリート専用敵 (本家型 2026-08-31)
  | 'thorned' // とげ型: 攻撃ヒットごとに反射 (針毛の栗鼠)
  | 'thief' // 盗人型: 盗み→逃走 (こそ泥ゴブリン)
  | 'bomber' // 爆弾型: 3拍子の大爆発 (火薬樽かつぎ)
  | 'healer' // 回復役型: 味方回復。編成専用 (苔の癒し手)
  | 'windup' // 息切れ型: 大技→隙 (大振りの斧鬼)
  | 'shell' // 甲殻型: 毎ターン積みながら殴る (石殻の番人)

/** buff = 強化 (自分のみ)。rally = 応援 (味方全体の強化)。hex = 状態異常の付与のみ (数値なし・inflict必須) */
export type EnemyActionKind =
  | 'attack'
  | 'defend'
  | 'destroy-set'
  | 'destroy-token' // 召喚トークン1体をランダムに破壊 (確定済みルール表「トークン破壊」)
  | 'buff'
  | 'rally'
  | 'hex'
  | 'heal' // 回復役: 最もHP割合の低い生存味方 (自分含む) を回復 (確定済みルール表「回復役（敵）」)
  | 'steal-gold' // 盗み: ロール額を敵が抱える。精算は勝利時にrun層 (確定済みルール表「盗みと逃走」)
  | 'flee' // 逃走: 戦闘から離脱 (hp:0+fled)。打ち消しで止められる
  | 'rest' // 隙: 何もしない (斧鬼の息切れ = 大技を凌げば反撃の窓)

/** プレイヤーへの状態異常 (確定済みルール表「状態異常」) */
export type PlayerStatus = 'weak' | 'vulnerable' | 'wound' | 'junk'

/** 状態異常の付与。weak/vulnerable はカウンター加算、wound は死に札を捨て札に混入 (1戦闘上限5枚) */
export interface StatusInflict {
  readonly status: PlayerStatus
  readonly amount: number
}

/** 敵の1行動。attack/defend/buff は [min, max] を宣言時にロール。destroy-set/hex は数値なし */
export interface EnemyMove {
  readonly id: string
  readonly kind: EnemyActionKind
  readonly min?: number
  readonly max?: number
  /** 重み抽選 (同テーブル内の相対値)。sequence を持つ敵では使われない */
  readonly weight: number
  /** 連撃: 攻撃をN回のヒットに分割 (確定済みルール表「連撃」) */
  readonly hits?: number
  /** 手数の鏡 (物真似 2026-08-31): 実行時のヒット数=プレイヤーがこのターンにプレイした枚数 (最低1)。hits は無視される */
  readonly mirrorHits?: boolean
  /** この行動が付与する状態異常 (attackの追撃・hexの本体) */
  readonly inflict?: StatusInflict
  /** 攻防一体: 攻撃と同時に得る固定ブロック (確定済みルール表「攻防一体・隙」) */
  readonly alsoDefend?: number
  /**
   * 行動単位の条件分岐 (確定済みルール表「読み合いの全敵展開」2026-08-28):
   * プレイヤーに伏せ札があると、この行動の代わりに setAlt の行動になる。
   * 既存の条件付き意図 (両分岐予告・行動開始時確定) の配管にそのまま乗る
   */
  readonly setAlt?: {
    readonly kind: EnemyActionKind
    readonly min?: number
    readonly max?: number
    readonly hits?: number
    readonly inflict?: StatusInflict
    readonly alsoDefend?: number
  }
}

export interface EnemyDef {
  readonly id: string
  readonly name: string
  readonly archetype: EnemyArchetype
  /** 1行フレーバー (顔付け)。行動の読み方のヒントを兼ねる。UI表示専用 */
  readonly flavor?: string
  readonly maxHp: number
  /** 行動定義。sequence がある場合は id 参照用の辞書を兼ねる */
  readonly moves: readonly EnemyMove[]
  /**
   * 行動ローテーション (StSのSentry等参考)。moves の id をこの順で繰り返す。
   * 指定時は重み抽選しない。movesVsSet の割り込みではローテーションは進まない
   */
  readonly sequence?: readonly string[]
  /** プレイヤーに伏せカードがある時に優先する行動テーブル (伏せ警戒型・伏せ破壊型・挑発型)。省略時は通常行動 */
  readonly movesVsSet?: readonly EnemyMove[]
  /** プレイヤーに召喚トークンがいる時の行動テーブル (優先度: HP半分以下 > 伏せ反応 > トークン反応 > 通常) */
  readonly movesVsTokens?: readonly EnemyMove[]
  /** 延焼耐性: 毎フェーズ延焼が追加でN減る (敵の弱点・耐性システム第1号。確定済みルール表「敵の耐性」) */
  readonly burnResist?: number
  /** とげ: プレイヤーの攻撃ヒットごとにNダメ反射。敵カードに常時表示 (確定済みルール表「とげ（敵の報復）」) */
  readonly thorns?: number
  /** 鬼軍曹 (エリート 2026-08-31): プレイヤーが通常ブロックを得るたび強化+N (氷壁は対象外)。敵カードに常時表示 */
  readonly angerOnBlock?: number
  /** HP50%以下で切り替わる行動テーブル (フェーズ変化)。優先度: 半分以下 > 伏せ反応 > 通常 */
  readonly movesBelowHalf?: readonly EnemyMove[]
  /** HP50%以下のローテーション (movesBelowHalf の id を参照) */
  readonly sequenceBelowHalf?: readonly string[]
  /** 再生: 敵フェーズ終了時にHP回復。HP50%以下では停止 (確定済みルール表「再生」) */
  readonly regen?: number
  /**
   * 再生の中断条件 (確定済みルール表「再生」2026-08-28): そのターン (前回の再生判定以降) に
   * 合計N以上のダメージを受けていると、次の敵フェーズの再生が発動しない。敵カードに常時表示
   */
  readonly regenBreak?: number
  /** 激昂: 敵フェーズ終了時に強化+N (確定済みルール表「激昂」) */
  readonly enrage?: number
  /**
   * 時喰らい型タイマー (2026-08-26): プレイヤーの累計詠唱数がこの枚数に達するたび強化+enrage。
   * 時間ではなくプレイヤーのテンポに紐づくので、低速デッキほど誘発が遅い = 自己調整する。
   * enrage と併用する場合、こちらが指定されていれば毎フェーズの自動強化は行わない。
   */
  readonly enrageEveryCards?: number
  /**
   * 激昂の与ダメ併用トリガー (2026-08-30)。この敵が受けた累計ダメージが N の倍数に達するたび
   * 強化+enrage。枚数トリガーは「1枚で100点出すデッキ」を素通しする盲点があった (実測:
   * 門番戦12枚プレイで1回しか鳴らず) — 高火力・少枚数のデッキにもタイマーを効かせる
   */
  readonly enrageEveryDamage?: number
  /**
   * 開幕ブロック (2026-08-30 静的性質の配布)。戦闘開始時からこの量のブロックを持つ
   * (甲羅・門・抱えた樽・積んだ殻)。敵の特性が「敵のターンが来て初めて情報になる」のに対し、
   * これはT1から問いを出せる — 貫通 (緑)・延焼 (赤)・粉砕が最初のターンから解答になる
   */
  readonly startingBlock?: number
  /**
   * 装甲 (2026-08-30 n²スケーリングへのワクチン)。**1ヒットで受けるダメージはN以下**に頭打ち。
   * 5色すべてが持つ「線形参照×枚数」の乗算 (勢い×多段・詠唱×0マナ・ブロック変換・自傷高効率・
   * 成長×X) に対し、カードをナーフせず敵側で受ける構造的な処方。多段デッキには「ヒット数で
   * 押し切れ」、一撃デッキには「上限まで」と別の問いを出す。とげ・延焼耐性と同じく常時表示 (フェアネス)。
   * 延焼 (DoT) はヒットではないので装甲を無視する = バーンが装甲の解答になる
   */
  readonly armor?: number
}

// ---- エンカウンター (1〜3体の編成。data/encounters.json) ----

/** 編成メンバー。hpScale/strength は「群れ補正」(頭数=行動回数が増えるぶん個体を弱める) */
export interface EncounterMember {
  readonly enemyId: string
  /** 個体HP倍率 (省略時1)。ランの深度スケーリングとは乗算で重なる */
  readonly hpScale?: number
  /** 個体の初期強化補正 (省略時0)。ランの深度補正とは加算で重なる */
  readonly strength?: number
  /** ローテーション開始位置のズラし。同型2体の大技同期 (同時lunge等) を防ぐ */
  readonly patternOffset?: number
  /**
   * 伏せ/従者への反応テーブル (movesVsSet / movesVsTokens) をこの個体では使わない。
   * 群れで全員が同時に反応すると、伏せ1枚のリスクが頭数に比例して跳ね上がるため、
   * 先頭の1体だけが反応するようにする (2026-08-26。確定済みルール表「編成の反応テーブル」)
   */
  readonly noReactTable?: boolean
}

export interface EncounterDef {
  readonly id: string
  readonly name: string
  readonly members: readonly EncounterMember[]
}

// ---- レリック (エリート挑戦の報酬。docs/relics-design.md) ----

/**
 * レリック定義。A型=フック効果 (effects。リーダーパッシブと同じ置物注入機構) /
 * B型=ラン定数 (bonus。取得時に RunState を書き換える)
 */
export interface RelicDef {
  readonly id: string
  readonly name: string
  readonly sprite: string
  readonly description: string
  /** A型: 戦闘開始時に不可視の置物として注入される宣言的効果 */
  readonly effects?: readonly DeclarativeEffect[]
  /** B型: ラン定数の恒久変更 */
  readonly bonus?: {
    readonly maxHp?: number
    readonly victoryHeal?: number
    readonly rewardChoices?: number
    readonly campfireRatio?: number
    /** 戦闘勝利のゴールド獲得に加算 (商人の秤) */
    readonly goldPerVictory?: number
    /** 焚き火の「鍛える」の追加回数 (鍛冶の砥石=+1で計2枚) */
    readonly campfireForge?: number
  }
  /**
   * C型: 戦闘ルールの改変 (少数精鋭)。launchCombat が所持レリックから集計して
   * CombatOptions 経由で GameState に渡す
   */
  readonly combatRule?: {
    /** 伏せ札がある間、敵の攻撃実値-N (最低1クランプ。静かな鈴) */
    readonly setDamageReduction?: number
    /** 敵の意図の実値を常時公開 (宣言時に shownMin=shownMax=actual へ畳む。蜃気楼の面) */
    readonly revealIntents?: boolean
  }
}

// ---- リーダー (カラーパイの個性。色アイデンティティ=使える色) ----

export interface LeaderDef {
  readonly id: string
  readonly name: string
  /** 色アイデンティティ。デッキ・ランの報酬で使える色 (統率者方式)。ギルドは2色 */
  readonly colors: readonly CardColor[]
  readonly maxHp: number
  /** 毎ターンのドロー枚数 (リソース個性) */
  readonly drawPerTurn: number
  readonly energyMax: number
  /** ランの報酬ピックの候補数 (リソース個性) */
  readonly rewardChoices: number
  /** ランの初期デッキ */
  readonly runDeckId: string
  /**
   * 種の選択制 (確定済みルール表「ラン初期デッキ」2026-08-29): ラン開始時に選べる初期デッキの一覧。
   * 複数持つリーダーだけ選択UIが出る。省略時は runDeckId のみ
   */
  readonly runDeckChoices?: readonly string[]
  readonly sprite: string
  readonly description: string
  /** パッシブ能力。戦闘開始時から場にあるリーダー置物として解決される */
  readonly passive: readonly DeclarativeEffect[]
  /** 伏せ枠 (リソース個性)。省略時1。かすみ (ディミア) =2 (確定済みルール表「伏せ枚数」) */
  readonly setSlots?: number
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
