// engine/run.ts — ドラフト連戦モード (純ロジック。DOM/React依存禁止)
// マップラン (確定済みルール表「マップ」2026-08-28): StS式DAGを1ノードずつ進む。
// 戦闘勝利で4枚提示から1枚ピック (スキップ可) → マップで次のノードを選ぶ。
// 敵は行の帯で深度スケーリング (強化+HP倍率) され、だんだん強くなる (StS参考)。
// HPは持ち越し、強制焚き火行 (5/10/14) で回復。
// ラン専用RNGをシードから回すため、同じシード+同じコマンド列=同じラン (リプレイ可能)。

import { startCombatWithOptions } from './combat.ts'
import { ACT_COUNT, bossRowFor, generateMap, tierFor } from './map.ts'
import { allEvents, getEventDef, WOUND_DEF } from './content.ts'
import type { MapNode, RunMap } from './map.ts'
import { fuseBlockReason, fuseCards } from './fusion.ts'
import {
  BRAND_DEF, GUILT_DEF,
  allCards,
  allRelics,
  buildDeck,
  buildRelicPermanent,
  getCardDef,
  getLeaderDef,
  getRelicDef,
} from './content.ts'
import { createRng, nextInt, shuffle } from './rng.ts'
import { applyCommand } from './state.ts'
import type { CardColor, CardDef, CardInstance, Command, EventChoiceDef, GameState, ReactionMode, RngState } from './types.ts'

/** 報酬プールから除外する基本札 (スターターに入っている素のカード) */
export const REWARD_EXCLUDED = new Set([
  'black_shiv_token', // 骨のナイフ: 生成トークン (この戦闘限り) = 報酬・ショップに出さない
  'green_strike',
  'green_guard',
  'green_basic_bash', // 打ち据え (2026-08-29 テンポ再校正②: スターターのBash枠)
  // スターターのリアクション2枚 (2026-08-30。中立スターター化の追随漏れ = 既に持っている札が
  // ピックに出ていた。伏せ枠は1つなので2枚目の価値も低い)。凍結色のスターターリアクションは解凍時に追随
  'green_reaction_thorns',
  'blue_strike',
  'blue_guard',
  'blue_counterspell', // 青スターターのリアクション2枚 (2026-08-30 中立化追随)
  'blue_frost_veil',
  // --- 2026-08-31 スターター個性注入の追随 (赤Opusランで火花が報酬に4回出た漏れ。
  // 確定済みルール「報酬プールはスターター札除外」に全色を同期) ---
  'green_entangle',
  'green_vine_wedge',
  'blue_ice_lance',
  'blue_ponder',
  'blue_tide_drop',
  'red_spark',
  'red_ignite',
  'red_perm_flarecoat',
  'red_perm_thorn_flame',
  'white_shield_strike',
  'white_perm_squire',
  'white_service',
  'white_bodyslam',
  'black_dark_pact',
  'black_drain',
  'black_bursting_corpse',
  'black_shadow_blade',
  'white_reaction_ward', // 白スターターのリアクション2枚 (解凍時の追随漏れを同時に是正)
  'white_reaction_retribution',
  'black_reaction_curse', // 黒スターターのリアクション2枚
  'black_reaction_grudge',
  'red_strike',
  'red_guard',
  'white_strike',
  'white_guard',
  'black_strike',
  'black_guard',
])
// 0.3→0.25 (2026-08-31 ユーザー裁定「25%で様子見」。再検証ラン2本とも「HPが半分を切らない」=
// 焚き火散布でルート選択の代償を作ったのに回復が毎回リセットしていた、への最小の絞り)
const CAMPFIRE_HEAL_RATIO = 0.25
// 2026-08-26 再設計: 回復は焚き火に到達すれば自動で入る。
// 「回復か強化か」の二択にすると、実測で焚き火到達時HPが常に20〜46%のため全員が回復しか選べず、
// 強化・除去が一度も使われなかった (供給側の機能が「既に余裕のある者」にしか届かない状態だった)。
/** 勝利ごとの自動回復は廃止 (2026-08-25 StS踏襲。回復は焚き火のみ=マラソン構造) */
const VICTORY_HEAL = 0
/** エリート補正: 強化+2・HP×1.35 (エリートはマップの選択ノード。2026-08-28 opt-inオファー廃止) */
// エリート補正は廃止 (2026-08-31 エリート専用敵化): 専用敵は素の値で完成しているため
const ELITE_STRENGTH = 0
// レリック上限は撤廃 (2026-08-29)。上限5は1幕時代の校正で、3幕化により幕2で満杯
// →以後のボスレリック・ショップレリックが全部死んでいた。実効上限は在庫数 (9個)
/** ゴールド (確定済みルール表「ゴールド」「ショップ」。相場はStS比例で入れて校正) */
const STARTING_GOLD = 50
const GOLD_PER_BATTLE_MIN = 12
const GOLD_PER_BATTLE_MAX = 18
const GOLD_ELITE_BONUS_MIN = 30
const GOLD_ELITE_BONUS_MAX = 40
/** 盗人を逃がす前に倒した時の懸賞金 (確定済みルール表「盗みと逃走」) */
const THIEF_BOUNTY = 10
/**
 * ?マスの累積確率の基礎値 (本家 monster10%/shop3%/treasure2%。2026-08-29)。
 * 浮動小数を持たない = 整数パーセントポイント (Unity移植の RNG 等価性を守る)
 */
const UNKNOWN_PITY_BASE = { monster: 10, shop: 3, treasure: 2 } as const
/** イベント抽選で祠プールを引く確率 (本家 SHRINE_CHANCE = 0.25) */
const SHRINE_CHANCE_PERCENT = 25
const SHOP_CARD_COUNT = 5
const SHOP_RELIC_PRICE = 150
/** 除去サービス: 回数無制限・使うたびラン通算で+25G (本家Purge式。2026-08-29) */
const SHOP_REMOVAL_BASE = 75
// 逓増を強化 (2026-08-31 ゴールドシンク: 502Gで強化2+除去2が同時に買えて選択になっていない実測)
const SHOP_REMOVAL_STEP = 50
/** 強化サービス: 回数無制限・使うたびラン通算で+30G (2026-08-29 ユーザー指示) */
const SHOP_UPGRADE_BASE = 100
const SHOP_UPGRADE_STEP = 50

/** 現在の除去サービス価格 (ラン通算の逓増)。?? 0 はフィールド導入前のセーブ読み込み対策 (NaN汚染防止) */
export function shopRemovalPrice(run: RunState): number {
  return SHOP_REMOVAL_BASE + SHOP_REMOVAL_STEP * (run.removalCount ?? 0)
}
/** 現在の強化サービス価格 (ラン通算の逓増)。?? 0 はフィールド導入前のセーブ読み込み対策 (NaN汚染防止) */
export function shopUpgradePrice(run: RunState): number {
  return SHOP_UPGRADE_BASE + SHOP_UPGRADE_STEP * (run.upgradeCount ?? 0)
}

/**
 * 深度スケーリング: 敵の初期強化。
 * 敵データは15枚スターター基準の強さなので、ラン序盤は「若い個体」(マイナス強化) で登場し、
 * ボスでフルスペック近くになる (StSの「敵はだんだん強く」の再現)。
 */
export function depthStrength(row: number, act = 1): number {
  // 若い個体補正は撤廃 (2026-08-25 人間基準化)。幕ボスのみ+1
  return row >= bossRowFor(act) ? 1 : 0
}

/**
 * 難易度10段階 (確定済みルール表「難易度」2026-09-01 ユーザー指示「敵が弱い」)。
 * 段3＝現状維持 (×1.0/×1.0)。打点優先で伸ばす＝「同じ危険を短く濃く」の既定路線。
 * 全敵一律 (ボス・エリート含む)。既存の幕スケール・打点+15%の上に乗算する。
 */
// 打点は段10で×3.0 (2026-09-01 ユーザー裁定「10で三倍の打点くらいに」。段3→10は約×1.17/段の幾何級数)
export const DIFFICULTY_TABLE: readonly { readonly hp: number; readonly atk: number }[] = [
  { hp: 0.85, atk: 0.85 }, // 1
  { hp: 0.95, atk: 0.95 }, // 2
  { hp: 1.0, atk: 1.0 }, // 3 = 既定 (現状維持)
  { hp: 1.05, atk: 1.15 }, // 4
  { hp: 1.1, atk: 1.35 }, // 5
  { hp: 1.15, atk: 1.6 }, // 6
  { hp: 1.2, atk: 1.9 }, // 7
  { hp: 1.25, atk: 2.2 }, // 8
  { hp: 1.3, atk: 2.6 }, // 9
  { hp: 1.35, atk: 3.0 }, // 10
]
export const DEFAULT_DIFFICULTY = 3
/** 難易度→倍率。範囲外と旧セーブの欠落 (undefined) は既定3へ丸める */
export function difficultyScale(level: number | undefined): { readonly hp: number; readonly atk: number } {
  const n = Number.isFinite(level) ? Math.round(level as number) : DEFAULT_DIFFICULTY
  return DIFFICULTY_TABLE[Math.min(DIFFICULTY_TABLE.length, Math.max(1, n)) - 1]
}

/** 深度スケーリング: 敵HP倍率。確定済みルール表「敵の数値基準」の帯に対応する */
export function depthHpScale(row: number, act = 1): number {
  // 幕×幕内前後半の2段スケール (確定済みルール表「ランの敵強化」2026-08-29 3幕化)。
  // 各幕のプールは既にその幕の帯に校正済みなので、幕内の2段が「幕内でもだんだん強く」を再現する
  if (row >= bossRowFor(act)) return 1.0 // 幕ボスは素のHP
  // 幕内前後半の境界 = ボス行の半分 (幕1=行7・幕2=行7・幕3=行6。15/14/13行化 2026-09-02)
  const late = row >= Math.floor(bossRowFor(act) / 2)
  // 2026-08-31 幕2/3を+0.10。「幕2の消化試合」の最後の根 = ボスだけ幕スケールで太り
  // 通常敵が置き去り、の実測への処方。打点帯は据え置き = 危険を濃くせず長さだけ半歩戻す
  // 2026-09-01 幕2/3を+0.15 (段3新ベース検証: 完成デッキのターン火力40に対し敵HP60〜80が
  // 2ターンで溶け、デバフ・タイマーが鳴る前に敵が死んでいた。「幕2の谷」の敵側の受け)
  const table: readonly (readonly [number, number])[] = [
    [0.62, 0.72], // 1幕 (2026-09-02 +0.07。ユーザー裁定「幕1はもうちょっと強くていい」×StS2対照「幕1増強は打点でなくHP上端」。実効上端58.5→64.8)
    [1.05, 1.15], // 2幕
    [1.2, 1.3], // 3幕
  ]
  const [early, lateScale] = table[act - 1]
  return late ? lateScale : early
}

export type RunPhase = 'map' | 'combat' | 'relic-reward' | 'campfire' | 'workshop' | 'shop' | 'event' | 'reward' | 'won' | 'lost'

/** ショップの在庫 (ノード進入時にシードから決定) */
export interface ShopState {
  /** sold: 購入済みスロット (2026-08-31: 詰めると後続indexがずれて別商品を掴む事故があった) */
  readonly cards: readonly { readonly id: string; readonly price: number; readonly sold?: boolean }[]
  readonly relicId: string | null
  readonly relicPrice: number
}

export interface RunState {
  readonly seed: number
  readonly mode: ReactionMode
  /** リーダー (色アイデンティティ・初期デッキ・報酬プール・ピック候補数を決める) */
  readonly leaderId: string
  /** リーダーの色アイデンティティ (leaderId から導出してキャッシュ) */
  readonly colors: readonly CardColor[]
  /** 難易度 (1〜10・既定3=現状維持。確定済みルール表「難易度」。旧セーブに無いので読み取りは difficultyScale 経由) */
  readonly difficulty: number
  /** デバッグ: 意図を常時実値表示 (2026-09-02 退屈診断④の判定実験。仕様は変えず計測だけ。ジャーナルに記録=リプレイ再現) */
  readonly debugRevealIntents?: boolean
  /** 実験 (2026-09-02): 全カード伏せ可 (engine/setany.ts) */
  readonly setAnyCards?: boolean
  /** ラン専用RNG (敵並び・報酬・戦闘シードの決定に使う) */
  readonly rng: RngState
  /** 現在のデッキ (ピックで増える) */
  readonly deck: readonly CardInstance[]
  /** 戦闘間で持ち越すHP */
  readonly hp: number
  readonly maxHp: number
  /** 現在の幕 (1〜3。確定済みルール表「マップ」3幕構成) */
  readonly act: number
  /** 現在の幕のマップ (幕開始時にシードから確定。全体可視) */
  readonly map: RunMap
  /** 現在いる行 (-1 = 開始前。行0のノードを選ぶ) */
  readonly row: number
  /** 現在いる列 */
  readonly col: number
  /** クリアした戦闘数 (統計・結果画面用) */
  readonly battlesWon: number
  /** 所持ゴールド (確定済みルール表「ゴールド」) */
  readonly gold: number
  /** ショップ除去サービスの通算使用回数 (逓増価格の基準) */
  readonly removalCount: number
  /** ショップ強化サービスの通算使用回数 (逓増価格の基準) */
  readonly upgradeCount: number
  /** ショップの在庫 (shop フェーズ中のみ非null) */
  readonly shop: ShopState | null
  readonly phase: RunPhase
  readonly combat: GameState | null
  /** 報酬フェーズの提示カード (cardId) */
  readonly rewardOptions: readonly string[] | null
  /** ピック履歴 (cardId。統計・結果画面用) */
  readonly picks: readonly string[]
  /** 所持レリック (relicId。上限なし=在庫数まで) */
  readonly relics: readonly string[]
  /** レリック候補列 (ラン開始時にシードから確定。取得済みを除いた先頭3つが提示される) */
  readonly relicQueue: readonly string[]
  /** relic-reward フェーズの提示レリック */
  readonly relicOptions: readonly string[] | null
  /** 現在の戦闘がエリート戦か (勝利時のレリック報酬判定) */
  readonly currentElite: boolean
  /** B型レリックの恒久ボーナス */
  readonly victoryHealBonus: number
  readonly rewardChoicesBonus: number
  readonly campfireRatio: number
  /** 戦闘勝利のゴールド加算 (商人の秤)。旧セーブに無いので使用側は ?? 0 ガード */
  readonly goldPerVictoryBonus: number
  /** 焚き火の「鍛える」の追加回数 (鍛冶の砥石)。旧セーブに無いので使用側は ?? 0 ガード */
  readonly campfireForgeBonus: number
  /** この焚き火で「鍛える」を使った回数 (焚き火進入時にリセット) */
  readonly campfireUpgradesUsed: number
  /** 幕1で焚き火の「鍛える」を使った通算回数 (供給を後ろへ 2026-08-31: 幕1は1回まで) */
  // ---- ?マスの本家式解決 (2026-08-29)。すべて旧セーブに無いので使用側は ?? ガード ----
  /** ?マスの累積確率 (整数パーセントポイント。幕頭で基礎値へリセット) */
  readonly unknownPity: { readonly monster: number; readonly shop: number; readonly treasure: number }
  /** 直前に入った部屋がショップだったか (本家: ?→ショップの2連続禁止) */
  readonly lastRoomWasShop: boolean
  /** event フェーズで解決したイベントID (?は入った瞬間に中身が決まる。MapNode は持たない) */
  readonly eventId: string | null
  /** ラン通算で引いたイベント (幕専用・ワンタイムの再出現防止) */
  readonly seenEventIds: readonly string[]
  /** この幕で引いた祠 (幕をまたぐと復活する = 本家 Shrine) */
  readonly seenShrineIds: readonly string[]
}

export type RunCommand =
  | { readonly type: 'StartRun'; readonly seed: number }
  | { readonly type: 'Combat'; readonly command: Command } // 戦闘中コマンドの委譲
  | { readonly type: 'PickReward'; readonly index: number }
  | { readonly type: 'SkipReward' }
  | { readonly type: 'ChooseNode'; readonly col: number } // マップで次のノードを選ぶ
  | { readonly type: 'PickRelic'; readonly index: number }
  | { readonly type: 'SkipRelic' }
  // 焚き火 (確定済みルール表「焚き火」): 休んで回復するか、デッキから1枚を永久に取り除くか
  | { readonly type: 'CampfireRest' }
  | { readonly type: 'CampfireRemove'; readonly index: number }
  | { readonly type: 'CampfireUpgrade'; readonly index: number }
  // 工房 (確定済みルール表「カード合成（工房）」): 異なる2枚を選んで合成するか、見送る
  | { readonly type: 'WorkshopFuse'; readonly indexA: number; readonly indexB: number }
  | { readonly type: 'WorkshopSkip' }
  // ショップ (確定済みルール表「ショップ」)
  | { readonly type: 'ShopBuyCard'; readonly index: number }
  | { readonly type: 'ShopBuyRelic' }
  | { readonly type: 'ShopRemove'; readonly index: number }
  | { readonly type: 'ShopUpgrade'; readonly index: number }
  | { readonly type: 'ShopLeave' }
  // ?マス (確定済みルール表「?マス（イベント）」)。removeCard/upgradeCard の選択肢は cardIndex で対象指定
  | { readonly type: 'EventChoice'; readonly index: number; readonly cardIndex?: number }

/** 現在いるノード (row=-1 の開始前は null) */
export function currentNode(run: RunState): MapNode | null {
  return run.row >= 0 ? run.map[run.row][run.col] : null
}

/** マップで次に進めるノードの列リスト (開始前は行0の全ノード) */
export function nextChoices(run: RunState): readonly number[] {
  if (run.row < 0) return run.map[0].map((_, c) => c)
  // 実マップの行数から判定する (2026-09-02 レビュー是正: 幕定数 bossRowFor だと
  // 15/14/13行化以前の旧セーブ〔全幕16行〕が幕2/3のボス前でソフトロックする)
  if (run.row >= run.map.length - 1) return []
  return currentNode(run)?.next ?? []
}

/** 現在ノードの戦闘を開始する (戦闘シードはラン RNG から決定的に生成)。elite でエリート補正 */
function launchCombat(run: RunState, elite: boolean, encounterOverride?: string): RunState {
  const node = currentNode(run)
  // encounterOverride は ?マスが戦闘に解決した時の敵 (ノードは encounterId を持たない)
  const encounterId = encounterOverride ?? node?.encounterId ?? null
  if (node === null || encounterId === null) throw new Error('戦闘ノードではない')
  const [combatSeed, rng] = nextInt(run.rng, 0, 2 ** 31 - 1)
  // 難易度倍率 (確定済みルール表「難易度」): 全敵一律で既存スケールの上に乗算
  const diff = difficultyScale(run.difficulty)
  const combat = startCombatWithOptions(combatSeed, run.mode, encounterId, {
    deck: run.deck,
    leaderId: run.leaderId,
    playerHp: run.hp,
    playerMaxHp: run.maxHp,
    // ボスの幕スケール (確定済みルール表「マップ」2026-08-29): HP×1.0/1.6/2.4・強化+1/+1/+2。
    // 幕2以降のボスが1幕時代の校正のままで消化試合化していた実測への対処
    // エリート専用敵は素の値で完成 = 幕内深度スケールも掛けない (2026-08-31 緑Opusランで発見:
    // depthHpScale が残っていて鬼軍曹82→45・鉄卵112→90 と設計値の55〜80%で出ていた)
    enemyHpScale:
      (elite
        ? 1
        : depthHpScale(run.row, run.act) *
          // 幕1ボス×1.25 (2026-08-29 ユーザー体感「ボスが弱い」。幕2/3は3幕走破ランで校正済みのため据え置き)
          (node.type === 'boss' ? [1.35, 2.3, 2.4][run.act - 1] : 1)) * diff.hp, // 幕1 1.25→1.35・幕2 1.6→2.3 (2026-09-02 本家対照: 大亀実効224は本家幕2ボス帯321〜408に▲100の大穴だった。322=本家下端へ。門番372は本家幅内で据え置き)
    enemyStrength:
      (node.type === 'boss' ? [1, 1, 2][run.act - 1] : 0) + (elite ? ELITE_STRENGTH : 0),
    // 幕2/3の通常敵は打点+15% (2026-09-01 ユーザー裁定。HP経済ラン2本連続「幕2で被ダメ0の
    // 戦闘が過半=育ったデッキに打点が届かない」への処方。HP帯は据え置き=危険だけ濃くする。
    // ボス・エリートは各自の校正 (幕スケール/素の値) があるため対象外。難易度倍率は全敵一律に乗る)
    enemyAtkScale: (elite || node.type === 'boss' ? 1 : [1, 1.15, 1.15][run.act - 1]) * diff.atk,
    relicPermanents: run.relics
      .map(getRelicDef)
      .filter((r) => (r.effects?.length ?? 0) > 0)
      .map(buildRelicPermanent),
    // C型レリック: 所持レリックの combatRule を集計して戦闘ルールに渡す
    setDamageReduction: run.relics
      .map(getRelicDef)
      .reduce((sum, r) => sum + (r.combatRule?.setDamageReduction ?? 0), 0),
    revealIntents: run.debugRevealIntents === true || run.relics.some((id) => getRelicDef(id).combatRule?.revealIntents === true),
    revealOnSet: run.relics.some((id) => getRelicDef(id).combatRule?.revealOnSet === true),
    ...(run.setAnyCards === true ? { setAnyCards: true } : {}),
  })
  return { ...run, rng, combat, phase: 'combat', rewardOptions: null, currentElite: elite }
}

/** 選んだノードに入る: 戦闘ノードなら戦闘開始、焚き火なら回復、工房ならそのままフェーズへ */
function enterNode(run: RunState): RunState {
  const next = enterNodeInner(run)
  // 本家: 直前の部屋がショップなら ?→ショップ を抑止する (phase==='shop' ⟺ ショップに入った)
  return { ...next, lastRoomWasShop: next.phase === 'shop' }
}

function enterNodeInner(run: RunState): RunState {
  const node = currentNode(run)
  if (node === null) throw new Error('ノードにいない')
  switch (node.type) {
    case 'battle':
    case 'boss':
      return launchCombat(run, false)
    case 'elite':
      return launchCombat(run, true)
    case 'campfire': {
      // 本家式の排他三択に復帰 (2026-08-29 ユーザー体感「強制回復がぬるい」):
      // 休む(30%回復) / 鍛える / 取り除く から1つ。回復の自動化 (2026-08-26) は
      // 当時「焚き火到達時HPが常に20〜46%で全員休む一択」だったための処方だが、
      // テンポ再校正で被ダメが減った今は選択が成立する。マラソンのHP緊張も戻る
      return { ...run, phase: 'campfire', combat: null, rewardOptions: null, campfireUpgradesUsed: 0 }
    }
    case 'workshop':
      return { ...run, phase: 'workshop', combat: null, rewardOptions: null }
    case 'shop':
      return openShop(run)
    case 'event':
      return resolveUnknown(run)
    case 'treasure':
      // 宝箱行 (2026-08-31): 本家の「9階は全ノード宝箱」。レリック3択のみ・カード報酬なし
      return openTreasure(run)
  }
}

/**
 * レリック3択の抽選: 残候補からシードRNGで3枚引く (2026-08-31)。
 * 旧・候補列の先頭3枚固定は、断ったレリックが以後の全提示に再登場し続け、
 * 幕1の5提示中「砥石5回・鉄の心臓4回」の反復を生んでいた (Opusマップ検証の指摘)
 */
function drawRelicOptions(run: RunState): readonly [readonly string[], RngState] {
  const pool = run.relicQueue.filter((id) => !run.relics.includes(id))
  let rng = run.rng
  const picked: string[] = []
  while (picked.length < 3 && pool.length > 0) {
    const [i, next] = nextInt(rng, 0, pool.length - 1)
    rng = next
    picked.push(pool.splice(i, 1)[0])
  }
  return [picked, rng]
}

/** 宝箱: レリック3択 (スキップ可)。候補列が尽きていれば素通りで map へ戻る */
function openTreasure(run: RunState): RunState {
  const [options, rng] = drawRelicOptions(run)
  const base = { ...run, rng, combat: null, rewardOptions: null }
  if (options.length === 0) return { ...base, phase: 'map' as const }
  return { ...base, phase: 'relic-reward' as const, relicOptions: options }
}

/**
 * ?マスの解決 (本家 getEventRoomOutcomeHelper の整数版。2026-08-29)。
 * 戦闘/ショップ/宝箱/イベントへ分岐し、累積確率を更新する。
 * 出た種別だけ基礎値へリセット、出なかった種別は基礎値ぶん加算 (上限なし)。幕頭で全リセット。
 */
function resolveUnknown(run: RunState): RunState {
  const pity = run.unknownPity ?? UNKNOWN_PITY_BASE
  const shopPct = (run.lastRoomWasShop ?? false) ? 0 : pity.shop
  const [roll, rng] = nextInt(run.rng, 0, 99)
  const bump = (hit: 'monster' | 'shop' | 'treasure' | 'event') => ({
    monster: hit === 'monster' ? UNKNOWN_PITY_BASE.monster : pity.monster + UNKNOWN_PITY_BASE.monster,
    shop: hit === 'shop' ? UNKNOWN_PITY_BASE.shop : pity.shop + UNKNOWN_PITY_BASE.shop,
    treasure: hit === 'treasure' ? UNKNOWN_PITY_BASE.treasure : pity.treasure + UNKNOWN_PITY_BASE.treasure,
  })
  if (roll < pity.monster) {
    // ?→戦闘: 敵はその行の帯から解決時に抽選する (生成時の直前2行回避は効かない)
    const pool = tierFor(run.act, run.row)
    const [i, r2] = nextInt(rng, 0, pool.length - 1)
    return launchCombat({ ...run, rng: r2, unknownPity: bump('monster'), eventId: null }, false, pool[i])
  }
  if (roll < pity.monster + shopPct) {
    return openShop({ ...run, rng, unknownPity: bump('shop'), eventId: null })
  }
  if (roll < pity.monster + shopPct + pity.treasure) {
    // ?→宝箱: レリック3択のみ (宝箱行と同じ配管。カード報酬は付かない)
    return openTreasure({ ...run, rng, unknownPity: bump('treasure'), eventId: null })
  }
  const [eventId, r3] = pickEvent(run, rng)
  const def = getEventDef(eventId)
  return {
    ...run,
    rng: r3,
    unknownPity: bump('event'),
    eventId,
    seenEventIds: [...(run.seenEventIds ?? []), eventId],
    seenShrineIds:
      def.kind === 'shrine' ? [...(run.seenShrineIds ?? []), eventId] : (run.seenShrineIds ?? []),
    phase: 'event',
    combat: null,
    rewardOptions: null,
  }
}

/**
 * イベントの抽選 (本家 generateEvent): 25%で祠+ワンタイムのプール、75%で幕プール。
 * 幕専用とワンタイムは引いたら二度と出ない / 祠は幕をまたぐと復活する。
 */
function pickEvent(run: RunState, rng0: RngState): readonly [string, RngState] {
  const seen = run.seenEventIds ?? []
  const seenShrine = run.seenShrineIds ?? []
  const inAct = (e: { act?: number }): boolean => e.act === undefined || e.act === run.act
  const shrinePool = allEvents.filter(
    (e) =>
      inAct(e) &&
      ((e.kind === 'shrine' && !seenShrine.includes(e.id)) ||
        (e.kind === 'oneTime' && !seen.includes(e.id))),
  )
  const actPool = allEvents.filter(
    (e) => (e.kind ?? 'act') === 'act' && inAct(e) && !seen.includes(e.id),
  )
  const [roll, rng] = nextInt(rng0, 0, 99)
  const useShrine = roll < SHRINE_CHANCE_PERCENT
  const primary = useShrine ? shrinePool : actPool
  const fallback = useShrine ? actPool : shrinePool
  const pool = primary.length > 0 ? primary : fallback
  // 両方尽きた場合のみ既出から引き直す (実質到達しない)
  const final = pool.length > 0 ? pool : allEvents.filter(inAct)
  const [i, r2] = nextInt(rng, 0, final.length - 1)
  return [final[i].id, r2]
}

/** ショップの在庫をシードから決定して開店する */
function openShop(run: RunState): RunState {
  const leader = getLeaderDef(run.leaderId)
  const canRamp = run.colors.includes('green')
  const costCap = leader.energyMax + (canRamp ? 2 : 0)
  const pool = allCards.filter(
    (c) => run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
  )
  let rng = run.rng
  const cards: { id: string; price: number }[] = []
  const remaining = [...pool]
  while (cards.length < SHOP_CARD_COUNT && remaining.length > 0) {
    const [idx, r1] = nextInt(rng, 0, remaining.length - 1)
    rng = r1
    const def = remaining[idx]
    remaining.splice(idx, 1)
    // 価格 = 40 + コスト×10 + ロール0〜10 (確定済みルール表「ショップ」)。
    // Xコスト札は cost フィールドが1なので、そのままだと最安の1コスト札と同値になってしまう
    // (2026-08-29 バグ修正)。実際に払うのは全エナジーなので、典型のX=3として値付けする
    const pricedCost = def.xCost === true ? 3 : def.cost
    const [roll, r2] = nextInt(rng, 0, 10)
    rng = r2
    cards.push({ id: def.id, price: 40 + pricedCost * 10 + roll })
  }
  // レア枠 (2026-08-31 ゴールドシンク): 品揃えの6枠目はレア確定・高額 (120+コスト×10 ≈ 150G)。
  // 「金は貯まるが使い道が選択にならない」実測への処方 = 高額の一点物を置く
  const rarePool = remaining.filter((c) => c.rarity === 'rare')
  if (rarePool.length > 0) {
    const [ri, r3] = nextInt(rng, 0, rarePool.length - 1)
    rng = r3
    const def = rarePool[ri]
    const pricedCost = def.xCost === true ? 3 : def.cost
    cards.push({ id: def.id, price: 120 + pricedCost * 10 })
  }
  const relicId = run.relicQueue.find((id) => !run.relics.includes(id)) ?? null
  const shop: ShopState = {
    cards,
    relicId,
    relicPrice: SHOP_RELIC_PRICE,
  }
  return { ...run, rng, shop, phase: 'shop', combat: null, rewardOptions: null }
}

/**
 * ランの報酬プール (色アイデンティティ・基本札除外・リーダーのコスト上限)。
 * イベントのランダム獲得・変成と共用する
 */
function rewardPool(run: RunState): readonly CardDef[] {
  const leader = getLeaderDef(run.leaderId)
  const canRamp = run.colors.includes('green')
  const costCap = leader.energyMax + (canRamp ? 2 : 0)
  return allCards.filter(
    (c) => run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
  )
}

/** イベント効果の適用 (宣言的な EventChoiceDef を RunState に反映する) */
/**
 * この選択肢は cardIndex (デッキの対象カード) を要求するか。
 * **UI・CLI はこれを見て対象選択を出す**——判定を各所で書くと、効果を足したときに
 * 片方だけ追随して「選べないダイアログ」が出る (2026-08-30 変転の祠で実際に起きた)。
 */
export function eventChoiceNeedsCard(choice: EventChoiceDef): boolean {
  return (
    choice.removeCard === true ||
    choice.upgradeCard === true ||
    choice.transformCard === true ||
    choice.duplicateCard === true
  )
}

function applyEventChoice(run: RunState, choiceIndex: number, cardIndex?: number): RunState {
  // ?は入った瞬間に中身が決まる (2026-08-29)。MapNode でなく RunState が持つ
  const eventId = run.eventId ?? null
  if (eventId === null) throw new Error('イベントノードではない')
  const def = getEventDef(eventId)
  const choice = def.choices[choiceIndex]
  if (choice === undefined) throw new Error(`不正な選択肢: ${choiceIndex}`)
  if (choice.requireGold !== undefined && run.gold < choice.requireGold) {
    throw new Error(`ゴールドが足りない (必要${choice.requireGold}G)`)
  }
  let next: RunState = { ...run }
  let rng = run.rng
  const applyOutcome = (o: { gold?: number; hp?: number; hpRatio?: number; wounds?: number; brands?: number; timedCurses?: number }): void => {
    if (o.gold) next = { ...next, gold: Math.max(0, next.gold + o.gold) }
    if (o.hp) next = { ...next, hp: Math.min(next.maxHp, next.hp + o.hp) }
    // 最大HP比の増減 (2026-08-29)。リーダー間で最大HPが違う (80/75/65/60) ので、
    // 本家イベントの過半と同じく比率で持つ。切り捨て
    if (o.hpRatio) {
      next = { ...next, hp: Math.min(next.maxHp, next.hp + Math.trunc(next.maxHp * o.hpRatio)) }
    }
    if (o.wounds) {
      const wounds: CardInstance[] = Array.from({ length: o.wounds }, (_, i) => ({
        uid: `wound_a${run.act}_r${run.row}_${i}`,
        def: WOUND_DEF,
      }))
      next = { ...next, deck: [...next.deck, ...wounds] }
    }
    if (o.brands) {
      // 呪いの烙印 (2026-09-02): 恒久のデッキ汚染と引き換えの大報酬。焚き火・ショップで除去可能
      const brands: CardInstance[] = Array.from({ length: o.brands }, (_, i) => ({
        uid: `brand_a${run.act}_r${run.row}_${i}`,
        def: BRAND_DEF,
      }))
      next = { ...next, deck: [...next.deck, ...brands] }
    }
    if (o.timedCurses) {
      // 仮初の烙印 (2026-09-02 StS2 Guilty式): 烙印と同じ滞留HP-1だが5戦で自然消滅する中間対価
      const guilts: CardInstance[] = Array.from({ length: o.timedCurses }, (_, i) => ({
        uid: `guilt_a${run.act}_r${run.row}_${i}`,
        def: GUILT_DEF,
        expiresAfterBattles: 5,
      }))
      next = { ...next, deck: [...next.deck, ...guilts] }
    }
  }
  applyOutcome(choice)
  if (choice.maxHp) {
    next = { ...next, maxHp: next.maxHp + choice.maxHp, hp: next.hp + choice.maxHp }
  }
  if (choice.addRandomCards) {
    const pool = rewardPool(run)
    for (let i = 0; i < choice.addRandomCards && pool.length > 0; i++) {
      const [idx, r1] = nextInt(rng, 0, pool.length - 1)
      rng = r1
      next = {
        ...next,
        deck: [...next.deck, { uid: `event_a${run.act}_r${run.row}_${i}_${pool[idx].id}`, def: pool[idx] }],
      }
    }
  }
  if (choice.relic) {
    const relicId = run.relicQueue.find((id) => !next.relics.includes(id))
    if (relicId !== undefined) {
      next = applyRelicBonus({ ...next, relics: [...next.relics, relicId] }, relicId)
    }
  }
  if (choice.removeCard) {
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    if (next.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
    next = { ...next, deck: next.deck.filter((_, i) => i !== cardIndex) }
  }
  if (choice.upgradeCard) {
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    if (isUpgraded(card)) throw new Error('すでに鍛えられている')
    if (upgradeTier(card.def) === 'none') throw new Error(`${card.def.name} は鍛えられない`)
    next = { ...next, deck: next.deck.map((c, i) => (i === cardIndex ? upgradeCard(c) : c)) }
  }
  if (choice.transformCard) {
    // 変成 (本家 Transmogrifier): 1枚を除去し、同じレアリティの別カードへ置き換える。
    // 同レアリティ固定なので、レア3%の希少性を迂回する経路にはならない
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    const rarity = card.def.rarity ?? 'common'
    const pool = rewardPool(run).filter((c) => (c.rarity ?? 'common') === rarity && c.id !== card.def.id)
    if (pool.length > 0) {
      const [idx, r1] = nextInt(rng, 0, pool.length - 1)
      rng = r1
      const replacement: CardInstance = {
        uid: `trans_a${run.act}_r${run.row}_${pool[idx].id}`,
        def: pool[idx],
      }
      next = { ...next, deck: next.deck.map((c, i) => (i === cardIndex ? replacement : c)) }
    }
  }
  if (choice.duplicateCard) {
    // 複製 (本家 Duplicator): 同じ def が1枚増える。uid は一意にする
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    const copy: CardInstance = { uid: `dup_a${run.act}_r${run.row}_${card.uid}`, def: card.def }
    next = { ...next, deck: [...next.deck, copy] }
  }
  if (choice.upgradeRandomCards) {
    // ランダム強化 (本家 Shining Light): 強化可能な札からN枚。足りなければそこで打ち切る
    for (let i = 0; i < choice.upgradeRandomCards; i++) {
      const idxs = next.deck
        .map((c, j) => (!isUpgraded(c) && upgradeTier(c.def) !== 'none' ? j : -1))
        .filter((j) => j >= 0)
      if (idxs.length === 0) break
      const [pick, r1] = nextInt(rng, 0, idxs.length - 1)
      rng = r1
      const target = idxs[pick]
      next = { ...next, deck: next.deck.map((c, j) => (j === target ? upgradeCard(c) : c)) }
    }
  }
  if (choice.removeAllWounds) {
    // 負傷の一掃 (本家 The Divine Fountain)。0枚でも何も起きないだけで throw しない
    next = { ...next, deck: next.deck.filter((c) => c.def.id !== WOUND_DEF.id) }
  }
  if (choice.gamble) {
    // ロールはラン RNG = 決定的 (リプレイ再現)
    const [roll, r1] = nextInt(rng, 0, 999)
    rng = r1
    applyOutcome(roll < choice.gamble.chance * 1000 ? choice.gamble.win : choice.gamble.lose)
  }
  if (next.hp <= 0) return { ...next, rng, hp: 0, phase: 'lost' }
  return { ...next, rng, phase: 'map' }
}

export function createRun(
  seed: number,
  mode: ReactionMode,
  leaderId = 'leader_green',
  deckId?: string,
  difficulty = DEFAULT_DIFFICULTY,
  opts?: { readonly revealIntents?: boolean; readonly setAnyCards?: boolean },
): RunState {
  const leader = getLeaderDef(leaderId)
  // 種の選択制 (確定済みルール表「ラン初期デッキ」): リーダーが許可する初期デッキのみ受け付ける
  const deckChoices = leader.runDeckChoices ?? [leader.runDeckId]
  const chosenDeck = deckId ?? leader.runDeckId
  if (!deckChoices.includes(chosenDeck)) {
    throw new Error(`このリーダーでは選べない初期デッキ: ${chosenDeck}`)
  }
  const rng0 = createRng(seed)
  // マップもレリック候補列もシードから確定 (リプレイ再現性)
  // 幕1の工房はちょうど1個 (2026-08-31 ユーザー指示。個数の制御は map.ts の quota 側)
  const [map, rngAfterMap] = generateMap(rng0, 1, true)
  // 伏せ参照レリックは、このランの報酬プールにリアクションが1枚も無い色 (赤単など) では
  // 永久の死に選択肢になるため候補列から除く (2026-08-30 Opusランで符師の懐が3択に3回連続出現)。
  // (蜃気楼の面は 2026-09-03 に撤去)
  const SET_RELICS = new Set(['relic_talisman_pouch', 'relic_quiet_bell'])
  const canSet = allCards.some(
    (c) => leader.colors.includes(c.color) && c.type === 'reaction',
  )
  const [relicQueue, rngAfterRelics] = shuffle(
    rngAfterMap,
    allRelics.map((r) => r.id).filter((id) => canSet || !SET_RELICS.has(id)),
  )
  return {
    seed,
    mode,
    leaderId,
    ...(opts?.revealIntents === true ? { debugRevealIntents: true } : {}),
    ...(opts?.setAnyCards === true ? { setAnyCards: true } : {}),
    colors: leader.colors,
    // 範囲外・非数は表の端/既定へ丸めて保存 (以降の読み取りも difficultyScale が守る)
    difficulty: Number.isFinite(difficulty)
      ? Math.min(DIFFICULTY_TABLE.length, Math.max(1, Math.round(difficulty)))
      : DEFAULT_DIFFICULTY,
    rng: rngAfterRelics,
    deck: buildDeck(chosenDeck),
    hp: leader.maxHp,
    maxHp: leader.maxHp,
    act: 1,
    map,
    row: -1,
    col: 0,
    battlesWon: 0,
    gold: STARTING_GOLD,
    removalCount: 0,
    upgradeCount: 0,
    shop: null,
    phase: 'map',
    combat: null,
    rewardOptions: null,
    picks: [],
    relics: [],
    relicQueue,
    relicOptions: null,
    currentElite: false,
    victoryHealBonus: 0,
    rewardChoicesBonus: 0,
    campfireRatio: CAMPFIRE_HEAL_RATIO,
    goldPerVictoryBonus: 0,
    campfireForgeBonus: 0,
    campfireUpgradesUsed: 0,
    unknownPity: { ...UNKNOWN_PITY_BASE },
    lastRoomWasShop: false,
    eventId: null,
    seenEventIds: [],
    seenShrineIds: [],
  }
}

/**
 * リプレイ・ジャーナル (2026-09-01)。エンジンの決定性 (同じシード+同じコマンド列=同じ結果) の
 * 具現化 — origin から初期状態を再現し commands を順に適用すれば任意の地点が正確に復元できる。
 * ブラウザUIとCLIハーネス (sim/play.ts) の両方が同じ形式で記録する
 */
export interface ReplayOrigin {
  readonly kind: 'run' | 'checkpoint'
  readonly seed: number
  readonly leaderId: string
  readonly deckId?: string
  readonly difficulty?: number
  /** デバッグの実値表示トグル (2026-09-02)。shownMin/Max の状態値が変わるので再現に必要 */
  readonly revealIntents?: boolean
  /** 実験: 全カード伏せ可 (2026-09-02)。伏せ可否とコストが変わるので再現に必要 */
  readonly setAnyCards?: boolean
  /** kind='checkpoint' の開始オプション (createDebugCheckpointRun の引数) */
  readonly checkpoint?: {
    readonly act: number
    readonly deckId: string
    readonly relicIds?: readonly string[]
    readonly hpRatio?: number
    readonly gold?: number
    readonly difficulty?: number
  }
}
export interface RunJournal {
  readonly origin: ReplayOrigin
  readonly commands: readonly RunCommand[]
}

/** origin からラン初期状態を再現する */
export function replayInitialRun(origin: ReplayOrigin): RunState {
  if (origin.kind === 'checkpoint' && origin.checkpoint !== undefined) {
    return createDebugCheckpointRun(origin.seed, 'set-confirm', origin.leaderId, origin.checkpoint)
  }
  return createRun(origin.seed, 'set-confirm', origin.leaderId, origin.deckId, origin.difficulty, {
    ...(origin.revealIntents ? { revealIntents: true } : {}),
    ...(origin.setAnyCards ? { setAnyCards: true } : {}),
  })
}

/**
 * ジャーナル全体を再実行し、各コマンド後の状態列を返す (states[0]=初期状態、states[i]=iコマンド後)。
 * データ定義が変わって再現が分岐した場合はそこで打ち切り、error に理由を入れる
 */
export function replayStates(journal: RunJournal): { states: RunState[]; error: string | null } {
  const states: RunState[] = [replayInitialRun(journal.origin)]
  for (let i = 0; i < journal.commands.length; i++) {
    try {
      states.push(applyRunCommand(states[states.length - 1], journal.commands[i]))
    } catch (e) {
      return { states, error: `コマンド${i + 1}/${journal.commands.length}で再現が分岐 (データ変更の可能性): ${e instanceof Error ? e.message : String(e)}` }
    }
  }
  return { states, error: null }
}

/**
 * チェックポイント開始 (2026-09-01 デバッグ機能「幕2/幕3から代表デッキで開始」)。
 * 幕2の谷・終盤の検証に毎回幕1を遊ぶコストを消す = LLMランの「幕サンプリング」を人間にも。
 * 通常の createRun を土台に、幕・マップ・デッキ・レリック (B型ボーナス込み)・HP・金だけ差し替える純関数
 */
export function createDebugCheckpointRun(
  seed: number,
  mode: ReactionMode,
  leaderId: string,
  opts: {
    readonly act: number // 2 | 3
    readonly deckId: string
    readonly relicIds?: readonly string[]
    readonly hpRatio?: number // 0.05〜1 (既定1)
    readonly gold?: number
    readonly difficulty?: number
  },
): RunState {
  const base = createRun(seed, mode, leaderId, undefined, opts.difficulty ?? DEFAULT_DIFFICULTY)
  const act = Math.min(ACT_COUNT, Math.max(1, Math.round(opts.act)))
  const [map, rng] = generateMap(base.rng, act, act === 1)
  let run: RunState = {
    ...base,
    act,
    map,
    rng,
    row: -1,
    col: 0,
    deck: buildDeck(opts.deckId), // デッキ選択制の検証は通さない (デバッグ = 理想形も可)
    battlesWon: act === 3 ? 19 : act === 2 ? 10 : 0,
    gold: opts.gold ?? 150,
  }
  for (const id of opts.relicIds ?? []) {
    getRelicDef(id) // 未定義なら throw
    run = applyRelicBonus({ ...run, relics: [...run.relics, id], relicQueue: run.relicQueue.filter((q) => q !== id) }, id)
  }
  const ratio = Math.min(1, Math.max(0.05, opts.hpRatio ?? 1))
  return { ...run, hp: Math.max(1, Math.round(run.maxHp * ratio)) }
}

/**
 * 効果名 → アーキタイプの軸。確定済みルール表「軸の重み付け」。
 * 効果に軸が現れない札 (多段ヒットの成長ペイオフ・貫通のトランプル札など) は
 * CardDef.axis で明示する (JSONで宣言。ここは自動導出ぶんだけ)。
 */
const EFFECT_AXIS: Record<string, string> = {
  addGrowth: 'growth', doubleGrowth: 'growth', dischargeGrowth: 'growth', dischargeGrowthBlock: 'growth',
  gainEnergyMax: 'ramp', dealDamagePerEnergyMax: 'ramp', gainBlockPerEnergyMax: 'ramp',
  addMomentum: 'trample', dealDamagePerMomentum: 'trample', doubleMomentum: 'trample',
  dischargeMomentumBurn: 'burn', dischargeMomentumBlock: 'trample',
  applyBurn: 'burn', dischargeBurn: 'burn',
  addAether: 'aether', dischargeAether: 'aether', dischargeAetherDraw: 'aether',
  gainIceBlock: 'ice', dealDamagePerIceBlock: 'ice', gainIceBlockPerCardPlayed: 'ice',
  negate: 'permission', negateConvertIce: 'permission',
  summonPermanent: 'retinue', dealDamagePerPermanent: 'retinue', gainBlockPerPermanent: 'retinue',
  exhaustFromDeck: 'graveyard', exhaustFromDeckChoose: 'graveyard', dealDamagePerExhaust: 'graveyard', retrieveFromExhaust: 'graveyard',
  playFromExhaust: 'graveyard', gainBlockPerExhaust: 'graveyard', dealDamageDrainPerExhaust: 'graveyard',
  loseHp: 'selfharm', dealDamagePerSelfHpLost: 'selfharm',
  dealDamagePerCardPlayed: 'storm', drawCardsPerCardPlayed: 'storm', addCasts: 'storm',
  impulseDraw: 'impulse',
  weakenEnemy: 'oppress', dealDamagePerNegStrength: 'oppress',
  gainHp: 'heal', dealDamageDrain: 'heal',
  dealDamagePerDamageTaken: 'wrath', applyBurnPerDamageTaken: 'wrath',
  dealDamagePerBlock: 'fortress',
  shatterBlock: 'shatter', shatterBlockConvert: 'shatter',
  dealDamageRandom: 'chaos', dealDamagePerRandomPlayed: 'chaos',
  dealDamageExecute: 'execute', exposeEnemy: 'execute',
  confuse: 'confuse',
  dealDamagePerHandCard: 'grimoire', gainIceBlockPerHandCard: 'grimoire', // 抱え込み (青 2026-08-31)
  addSpellEcho: 'echo', // 反復 (青の呪文コピー)
}

/** 誘発トリガー → 軸。置物の「接着剤」札はここでほぼ自動的に分類される */
const TRIGGER_AXIS: Record<string, string> = {
  onPermanentEntered: 'retinue',
  onCardExhausted: 'graveyard',
  onCostExhausted: 'graveyard',
  onHealed: 'heal',
  onHpLost: 'selfharm',
  onAetherGained: 'aether',
  onImpulsePlayed: 'impulse',
  onRandomPlayed: 'chaos',
  onSpellPlayed: 'storm',
  onBlockGained: 'fortress',
  onActionNegated: 'permission',
  onSelfExhausted: 'graveyard', // 亡骸効果 (黒 2026-08-31)
}

/** この札が属する軸 (効果名・トリガー・フィールドからの自動導出 + JSONの明示宣言) */
export function axesOf(def: CardDef): readonly string[] {
  const all = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
  const set = new Set<string>(def.axis ?? [])
  for (const e of all) {
    const byEffect = EFFECT_AXIS[e.effect]
    if (byEffect) set.add(byEffect)
    const byTrigger = TRIGGER_AXIS[e.trigger]
    if (byTrigger) set.add(byTrigger)
    if (e.exhaustThreshold !== undefined) set.add('graveyard') // 忘却の刻の参照札
    if (e.pierce === true) set.add('trample') // 貫通はトランプルの核
  }
  if (def.exhaustCost !== undefined) set.add('graveyard') // 消滅コストは墓地の燃料
  if (def.retainer === true) set.add('retinue')
  return [...set]
}

/** 報酬を抽選 (リーダーの色アイデンティティのカードのみ・基本札除外・重複なし)。候補数はリーダー個性+収集家の鞄 */
function rollRewards(run: RunState): RunState {
  const leader = getLeaderDef(run.leaderId)
  // 報酬プールはリーダーのエナジー上限を考慮する (2026-08-25 プレイテストで発見:
  // 白は上限3固定なのに4Eの大行進が提示され、ラン中ずっと死に札だった)。
  // 緑はランプで上限を伸ばせるので +2 まで許容する
  const canRamp = run.colors.includes('green')
  const costCap = leader.energyMax + (canRamp ? 2 : 0)
  const pool = allCards
    .filter(
      (c) =>
        run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
    )
  // レアリティ抽選 (確定済みルール表「レアリティ」2026-08-29): スロットごとに
  // コモン60% / アンコモン37% / レア3% の本家比率でレアリティを決め、その帯から一様に引く。
  // 軸の重み付けは全廃 — 報酬はデッキを一切見ない (本家準拠。ドラフトの発見性を守る)
  const remaining = [...pool]
  const picked: string[] = []
  let rng = run.rng
  const want = leader.rewardChoices + run.rewardChoicesBonus
  const rarityOf = (c: CardDef) => c.rarity ?? 'common'
  while (picked.length < want && remaining.length > 0) {
    const [roll, r1] = nextInt(rng, 0, 99)
    rng = r1
    // エリート報酬はレア1枚確定 (2026-08-31 ユーザー指示。本家のエリート=レア率上昇を確約に強化)。
    // 先頭スロットをレア帯で引く。以降のスロットは通常比率
    // 逃がしたエリート (金羽の大鴉など) はレア確定を失う (2026-08-31 緑・青ラン指摘
    // 「逃がしてもフル報酬=ノーリスク」への処方。レリック3択は維持=エリート報酬の約束は守る)
    const eliteEscaped = run.combat?.enemies.some((e) => e.fled === true) === true
    const wanted: ('rare' | 'uncommon' | 'common')[] =
      run.currentElite && picked.length === 0 && !eliteEscaped
        ? ['rare', 'uncommon', 'common']
        : roll < 3
          ? ['rare', 'uncommon', 'common']
          : roll < 40
            ? ['uncommon', 'common']
            : ['common']
    // 希望レアリティの札が尽きていたら下の帯へフォールバック。それも無ければプール全体
    let candidates: CardDef[] = []
    for (const r of wanted) {
      candidates = remaining.filter((c) => rarityOf(c) === r)
      if (candidates.length > 0) break
    }
    if (candidates.length === 0) candidates = remaining
    const [idx, r2] = nextInt(rng, 0, candidates.length - 1)
    rng = r2
    const chosen = candidates[idx]
    picked.push(chosen.id)
    remaining.splice(remaining.indexOf(chosen), 1)
  }
  return { ...run, rng, rewardOptions: picked, phase: 'reward' }
}

/** 戦闘勝利後の処理: HP持ち越し → (エリートならレリック報酬 →) カード報酬 or ラン勝利 */
function afterVictory(run: RunState, combat: GameState): RunState {
  const isBoss = currentNode(run)?.type === 'boss'
  // 3幕目のボス撃破 = ラン走破
  if (isBoss && run.act >= ACT_COUNT) {
    return { ...run, combat, battlesWon: run.battlesWon + 1, phase: 'won' }
  }
  // 自動回復は狩人の恵み (victoryHealBonus) のみ。幕ボス撃破は全回復 (確定済みルール表「マップ」)。
  // 2026-08-29 Meat on the Bone式: 救助に限定して満タン維持を防ぐ。
  // 2026-09-01 しきい値 50%→30% (検証ラン: 幕2の谷39〜44%を全部+8で埋め戻し、焚き火25%の
  // 絞りと正面衝突していた。「あと1発」の帯だけを救助し、30〜50%の緊張は残す)
  const rescueHeal =
    combat.player.hp <= run.maxHp * 0.3 ? run.victoryHealBonus : 0
  const hp = isBoss ? run.maxHp : Math.min(run.maxHp, combat.player.hp + VICTORY_HEAL + rescueHeal)
  // ゴールド獲得 (通常12〜18G・エリート+30〜40G・幕ボス+40〜50G。確定済みルール表「ゴールド」)
  let rng = run.rng
  const [base, r1] = nextInt(rng, GOLD_PER_BATTLE_MIN, GOLD_PER_BATTLE_MAX)
  rng = r1
  let gained = base
  if (run.currentElite) {
    const [bonus, r2] = nextInt(rng, GOLD_ELITE_BONUS_MIN, GOLD_ELITE_BONUS_MAX)
    rng = r2
    gained += bonus
  }
  if (isBoss) {
    const [bonus, r3] = nextInt(rng, 40, 50)
    rng = r3
    gained += bonus
  }
  // 商人の秤 (B型レリック): 戦闘勝利のゴールド加算
  gained += run.goldPerVictoryBonus ?? 0
  // 盗みの精算 (確定済みルール表「盗みと逃走」): 逃走した盗人が抱えた額を失い (合計は最低0)、
  // 逃げる前に倒した盗人は全額戻る (ゴールドは一度も減っていない) + 懸賞金
  const fledLoss = combat.enemies
    .filter((e) => e.fled === true)
    .reduce((sum, e) => sum + (e.stolenGold ?? 0), 0)
  const bounty =
    combat.enemies.filter((e) => e.fled !== true && (e.stolenGold ?? 0) > 0).length * THIEF_BOUNTY
  gained += bounty - fledLoss
  // 時限呪い (2026-09-02 StS2 Guilty式): 勝利ごとに残り戦数-1・0でデッキから自然消滅
  const deckAfterCurses = run.deck
    .map((c) =>
      c.expiresAfterBattles !== undefined
        ? { ...c, expiresAfterBattles: c.expiresAfterBattles - 1 }
        : c,
    )
    .filter((c) => c.expiresAfterBattles === undefined || c.expiresAfterBattles > 0)
  const next: RunState = {
    ...run,
    rng,
    combat,
    hp,
    deck: deckAfterCurses,
    battlesWon: run.battlesWon + 1,
    // 盗みの喪失で負になりうるので0でクランプ
    gold: Math.max(0, run.gold + gained),
  }
  // 幕ボス・エリート戦の勝利: レリック3択 (幕ボスは本家のボスレリック相当)
  if (run.currentElite || isBoss) {
    const [options, rng2] = drawRelicOptions(next)
    if (options.length > 0) {
      return { ...next, rng: rng2, phase: 'relic-reward', relicOptions: options }
    }
  }
  return rollRewards(next)
}

/** 幕ボスのカード報酬を受け取った後、次の幕へ進む (新しいマップを生成して行0の選択から) */
function advanceActIfBossCleared(run: RunState): RunState {
  if (currentNode(run)?.type !== 'boss' || run.act >= ACT_COUNT) {
    return { ...run, phase: 'map' }
  }
  const nextAct = run.act + 1
  const [map, rng] = generateMap(run.rng, nextAct, true)
  return {
    ...run,
    rng,
    act: nextAct,
    map,
    row: -1,
    col: 0,
    combat: null,
    phase: 'map',
    unknownPity: { ...UNKNOWN_PITY_BASE }, // ピティは幕をまたがない (本家 transitionToAct)
    seenShrineIds: [], // 祠は幕をまたぐと復活する
    eventId: null,
    lastRoomWasShop: false,
  }
}

import { canUpgradeCard, isUpgraded, upgradeCard, upgradeTier } from './upgrade.ts'
export { canUpgradeCard, isUpgraded, upgradeCard, upgradeTier }

/** B型レリックの取得時効果を適用する */
function applyRelicBonus(run: RunState, relicId: string): RunState {
  const def = getRelicDef(relicId)
  const b = def.bonus
  if (!b) return run
  return {
    ...run,
    maxHp: run.maxHp + (b.maxHp ?? 0),
    hp: Math.min(run.maxHp + (b.maxHp ?? 0), run.hp + (b.maxHp ?? 0)),
    victoryHealBonus: run.victoryHealBonus + (b.victoryHeal ?? 0),
    rewardChoicesBonus: run.rewardChoicesBonus + (b.rewardChoices ?? 0),
    campfireRatio: b.campfireRatio ?? run.campfireRatio,
    // ?? 0 二段: 旧セーブは RunState 側のフィールド自体が無い (NaN汚染防止。shop-event.test.ts の前例)
    goldPerVictoryBonus: (run.goldPerVictoryBonus ?? 0) + (b.goldPerVictory ?? 0),
    campfireForgeBonus: (run.campfireForgeBonus ?? 0) + (b.campfireForge ?? 0),
  }
}

export function applyRunCommand(run: RunState, command: RunCommand): RunState {
  switch (command.type) {
    case 'StartRun':
      return createRun(command.seed, run.mode, run.leaderId)
    case 'Combat': {
      if (run.phase !== 'combat' || run.combat === null) throw new Error('戦闘中ではない')
      if (command.command.type === 'StartCombat') throw new Error('ラン中の戦闘開始はランが管理する')
      const combat = applyCommand(run.combat, command.command)
      if (combat.phase === 'lost') return { ...run, combat, hp: 0, phase: 'lost' }
      if (combat.phase === 'won') return afterVictory(run, combat)
      return { ...run, combat }
    }
    case 'PickReward': {
      if (run.phase !== 'reward' || run.rewardOptions === null) throw new Error('報酬フェーズではない')
      const cardId = run.rewardOptions[command.index]
      if (cardId === undefined) throw new Error(`不正な報酬指定: ${command.index}`)
      // uid は行番号で一意化 (1行につき1ノードしか訪れないため衝突しない)
      const card: CardInstance = { uid: `pick_a${run.act}_r${run.row}_${cardId}`, def: getCardDef(cardId) }
      return advanceActIfBossCleared({
        ...run,
        deck: [...run.deck, card],
        picks: [...run.picks, cardId],
        rewardOptions: null,
      })
    }
    case 'SkipReward': {
      if (run.phase !== 'reward') throw new Error('報酬フェーズではない')
      return advanceActIfBossCleared({ ...run, rewardOptions: null })
    }
    case 'ChooseNode': {
      if (run.phase !== 'map') throw new Error('マップフェーズではない')
      const candidates = nextChoices(run)
      if (!candidates.includes(command.col)) throw new Error(`進めないノード: ${command.col}`)
      return enterNode({ ...run, row: run.row + 1, col: command.col })
    }
    case 'PickRelic': {
      if (run.phase !== 'relic-reward' || run.relicOptions === null) {
        throw new Error('レリック報酬フェーズではない')
      }
      const relicId = run.relicOptions[command.index]
      if (relicId === undefined) throw new Error(`不正なレリック指定: ${command.index}`)
      let next: RunState = { ...run, relics: [...run.relics, relicId], relicOptions: null }
      next = applyRelicBonus(next, relicId)
      // ?マスの宝箱はレリックのみでカード報酬は付かない (2026-08-29)。
      // combat===null が「戦闘勝利を経ていない=宝箱」の判別 (afterVictory は必ず combat を渡す)
      if (run.combat === null) return { ...next, relicOptions: null, phase: 'map' }
      return rollRewards(next)
    }
    case 'SkipRelic': {
      if (run.phase !== 'relic-reward') throw new Error('レリック報酬フェーズではない')
      if (run.combat === null) return { ...run, relicOptions: null, phase: 'map' }
      return rollRewards({ ...run, relicOptions: null })
    }
    case 'CampfireRest': {
      // 休む = 最大HPの30% (campfireRatio) を回復して次へ。鍛える/除去とは排他 (2026-08-29 復帰)。
      // 砥石で「鍛える」を使った後は回復なしの立ち去りになる (選べるのは1種類の原則)
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      const hp =
        (run.campfireUpgradesUsed ?? 0) > 0
          ? run.hp
          : Math.min(run.maxHp, run.hp + Math.floor(run.maxHp * run.campfireRatio))
      return { ...run, hp, phase: 'map' }
    }
    case 'CampfireUpgrade': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な強化指定: ${command.index}`)
      if (isUpgraded(card)) throw new Error('すでに鍛えられている')
      // 2026-08-28 修正: 強化不可札 (上限ランプ) を受理して「+」だけ付ける事故の再発防止。
      // 焚き火の選択権 (4回しかない希少資源) を無言で浪費させない
      if (upgradeTier(card.def) === 'none') {
        throw new Error(`${card.def.name} は鍛えられない (エナジー上限を上げる札は強化対象外)`)
      }
      // 幕1の「鍛える1回」制限は撤廃 (2026-09-01 ユーザー指示「幕に対しての制限不要」。
      // 旧・供給集中対策 2026-08-31 は工房の幕1×1個化と焚き火の希少化で役目を終えた)
      // 鍛冶の砥石 (B型レリック): 追加回数のぶん焚き火に留まり、もう1枚鍛えられる
      const used = (run.campfireUpgradesUsed ?? 0) + 1
      const allowed = 1 + (run.campfireForgeBonus ?? 0)
      return {
        ...run,
        deck: run.deck.map((c, i) => (i === command.index ? upgradeCard(c) : c)),
        campfireUpgradesUsed: used,
        phase: used < allowed ? 'campfire' : 'map',
      }
    }
    case 'WorkshopFuse': {
      if (run.phase !== 'workshop') throw new Error('工房フェーズではない')
      const a = run.deck[command.indexA]
      const b = run.deck[command.indexB]
      if (a === undefined || b === undefined) throw new Error('不正な合成指定')
      const reason = fuseBlockReason(a, b)
      if (reason !== null) throw new Error(`合成できない: ${reason}`)
      const fusedDef = fuseCards(a, b)
      const fused: CardInstance = { uid: `fused_a${run.act}_r${run.row}_${fusedDef.id}`, def: fusedDef }
      // 素材2枚はデッキから消え、合成札1枚が入る = 圧縮と強化が同時に起きる
      const deck = run.deck.filter((_, i) => i !== command.indexA && i !== command.indexB)
      return { ...run, deck: [...deck, fused], phase: 'map' }
    }
    case 'WorkshopSkip': {
      if (run.phase !== 'workshop') throw new Error('工房フェーズではない')
      return { ...run, phase: 'map' }
    }
    case 'CampfireRemove': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      // 砥石で焚き火に留まっていても「鍛える/取り除く/何もしない から1つ選ぶ」の原則は崩さない
      if ((run.campfireUpgradesUsed ?? 0) > 0) throw new Error('この焚き火ではすでに鍛えている (選べるのは1種類)')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な除去指定: ${command.index}`)
      // デッキが痩せすぎないよう最低5枚は残す
      if (run.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
      return { ...run, deck: run.deck.filter((_, i) => i !== command.index), phase: 'map' }
    }
    case 'ShopBuyCard': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      const item = run.shop.cards[command.index]
      if (item === undefined) throw new Error(`不正な商品指定: ${command.index}`)
      if (item.sold === true) throw new Error('その商品は売り切れ')
      if (run.gold < item.price) throw new Error(`ゴールドが足りない (${item.price}G)`)
      const card: CardInstance = { uid: `buy_a${run.act}_r${run.row}_${item.id}`, def: getCardDef(item.id) }
      return {
        ...run,
        gold: run.gold - item.price,
        deck: [...run.deck, card],
        picks: [...run.picks, item.id],
        // index を詰めない = 売切マーク (2026-08-31 黒ラン: 連続購入で別商品を掴んだ事故)
        shop: {
          ...run.shop,
          cards: run.shop.cards.map((c, i) => (i === command.index ? { ...c, sold: true } : c)),
        },
      }
    }
    case 'ShopBuyRelic': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      if (run.shop.relicId === null) throw new Error('レリックの在庫がない')
      if (run.gold < run.shop.relicPrice) throw new Error(`ゴールドが足りない (${run.shop.relicPrice}G)`)
      const relicId = run.shop.relicId
      let next: RunState = {
        ...run,
        gold: run.gold - run.shop.relicPrice,
        relics: [...run.relics, relicId],
        shop: { ...run.shop, relicId: null },
      }
      next = applyRelicBonus(next, relicId)
      return next
    }
    case 'ShopRemove': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      const price = shopRemovalPrice(run)
      if (run.gold < price) throw new Error(`ゴールドが足りない (${price}G)`)
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な除去指定: ${command.index}`)
      if (run.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
      return {
        ...run,
        gold: run.gold - price,
        deck: run.deck.filter((_, i) => i !== command.index),
        removalCount: (run.removalCount ?? 0) + 1,
      }
    }
    case 'ShopUpgrade': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      // 幕1の強化サービス封鎖は撤廃 (2026-09-01。焚き火の幕1制限撤廃と対で不要になった)
      const price = shopUpgradePrice(run)
      if (run.gold < price) throw new Error(`ゴールドが足りない (${price}G)`)
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な強化指定: ${command.index}`)
      if (isUpgraded(card)) throw new Error('すでに鍛えられている')
      if (upgradeTier(card.def) === 'none') {
        throw new Error(`${card.def.name} は鍛えられない (エナジー上限を上げる札は強化対象外)`)
      }
      return {
        ...run,
        gold: run.gold - price,
        deck: run.deck.map((c, i) => (i === command.index ? upgradeCard(c) : c)),
        upgradeCount: (run.upgradeCount ?? 0) + 1,
      }
    }
    case 'ShopLeave': {
      if (run.phase !== 'shop') throw new Error('ショップではない')
      return { ...run, shop: null, phase: 'map' }
    }
    case 'EventChoice': {
      if (run.phase !== 'event') throw new Error('イベントではない')
      return applyEventChoice(run, command.index, command.cardIndex)
    }
      default:
      // 未知のコマンドは throw (2026-08-30)。旧実装は switch を素通りして undefined を返し、
      // CLIハーネスがそれを保存してセーブを破壊した (実プレイでラン喪失。play.ts の
      // コマンド分類ミスが引き金だが、エンジンが undefined を返さなければ被害は出なかった)
      throw new Error(`未知のランコマンド: ${(command as { type: string }).type}`)
  }
}
