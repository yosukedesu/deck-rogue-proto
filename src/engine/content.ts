// engine/content.ts — data/*.json の読み込みと参照
// JSON は本実装へそのまま持っていく共通資産。スキーマは types.ts が一次資料。

import cardsGreenJson from '../data/cards.green.json' with { type: 'json' }
import cardsBlueJson from '../data/cards.blue.json' with { type: 'json' }
import cardsRedJson from '../data/cards.red.json' with { type: 'json' }
import cardsWhiteJson from '../data/cards.white.json' with { type: 'json' }
import cardsBlackJson from '../data/cards.black.json' with { type: 'json' }
import decksJson from '../data/decks.json' with { type: 'json' }
import encountersJson from '../data/encounters.json' with { type: 'json' }
import enemiesJson from '../data/enemies.json' with { type: 'json' }
import leadersJson from '../data/leaders.json' with { type: 'json' }
import relicsJson from '../data/relics.json' with { type: 'json' }
import eventsJson from '../data/events.json' with { type: 'json' }
import type {
  CardColor,
  CardDef,
  CardInstance,
  DeckDef,
  EncounterDef,
  EncounterMember,
  EnemyDef,
  LeaderDef,
  RelicDef,
  EventDef,
} from './types.ts'

// 色は JSON に書かず、ファイル単位でここで付与する (JSONを本実装へ持ち込む際の共通規約)
const withColor = (cards: readonly unknown[], color: CardColor): readonly CardDef[] =>
  (cards as readonly Omit<CardDef, 'color'>[]).map((c) => ({ ...c, color }))

export const allCards: readonly CardDef[] = [
  ...withColor(cardsGreenJson, 'green'),
  ...withColor(cardsBlueJson, 'blue'),
  ...withColor(cardsRedJson, 'red'),
  ...withColor(cardsWhiteJson, 'white'),
  ...withColor(cardsBlackJson, 'black'),
]
export const allEnemies = enemiesJson as readonly EnemyDef[]
export const allEncounters = encountersJson as readonly EncounterDef[]

/**
 * 敵ID or 編成ID を編成メンバー列に解決する (確定済みルール表「戦闘形式」)。
 * 編成IDが優先。どちらでもなければエラー。敵ID直指定はソロ編成 (後方互換)
 */
export function resolveEncounter(id: string): readonly EncounterMember[] {
  const enc = allEncounters.find((e) => e.id === id)
  if (enc) return enc.members
  if (allEnemies.some((e) => e.id === id)) return [{ enemyId: id }]
  throw new Error(`未定義の敵/編成: ${id}`)
}

/** 表示名 (編成名 or 敵名) */
export function encounterName(id: string): string {
  const enc = allEncounters.find((e) => e.id === id)
  if (enc) return enc.name
  return getEnemyDef(id).name
}
export const allDecks = decksJson as readonly DeckDef[]
export const allLeaders = leadersJson as readonly LeaderDef[]
export const allRelics = relicsJson as readonly RelicDef[]

export const allEvents = eventsJson as readonly EventDef[]

export function getEventDef(id: string): EventDef {
  const def = allEvents.find((e) => e.id === id)
  if (!def) throw new Error(`未定義イベント: ${id}`)
  return def
}

export function getRelicDef(id: string): RelicDef {
  const def = allRelics.find((r) => r.id === id)
  if (!def) throw new Error(`未定義レリック: ${id}`)
  return def
}

/** A型レリックを「戦闘開始時から場にある不可視の置物」として実体化する (リーダーパッシブと同型) */
export function buildRelicPermanent(relic: RelicDef): CardInstance {
  return {
    uid: `relic_${relic.id}`,
    innate: true, // 戦闘開始時から場にある = 置物数参照で数えない (2026-08-26)
    def: {
      id: `${relic.id}_passive`,
      name: relic.name,
      cost: 0,
      type: 'permanent',
      color: 'green',
      effects: relic.effects ?? [],
    },
  }
}

export function getLeaderDef(id: string): LeaderDef {
  const def = allLeaders.find((l) => l.id === id)
  if (!def) throw new Error(`未定義リーダー: ${id}`)
  return def
}

/** リーダーの色アイデンティティで使えるデッキか (統率者方式) */
export function deckAllowedForLeader(leader: LeaderDef, deck: DeckDef): boolean {
  return leader.colors.includes(deck.color)
}

/** リーダーのパッシブを「戦闘開始時から場にある置物」として実体化する */
export function buildLeaderPassive(leader: LeaderDef): CardInstance {
  return {
    uid: `leader_${leader.id}`,
    innate: true, // 戦闘開始時から場にある = 置物数参照で数えない (2026-08-26)
    def: {
      id: `${leader.id}_passive`,
      name: `${leader.name}の能力`,
      cost: 0,
      type: 'permanent',
      color: leader.colors[0],
      effects: leader.passive,
    },
  }
}

/**
 * 負傷 (状態異常カード): 敵が捨て札に混入させる使用不可の死に札。
 * onPlay 効果を持たないため isPlayableFromHand が自然に false になる。
 * 報酬プール (allCards) には含めない。色は便宜上 red (無色概念は未導入)
 */
export const WOUND_DEF: CardDef = {
  id: 'status_wound',
  name: '負傷',
  cost: 0,
  type: 'spell',
  color: 'red',
  effects: [],
}

/**
 * 火傷 (状態異常カード 2026-09-02 敵ギミック第1波)。本家StSのBurn相当:
 * 使用不可の死に札で、**自ターン終了時に手札にあると自傷2**。戦闘終了で消える (デッキに残らない)。
 * 捨てコスト・消滅コストの支払いには使える = 手札マネジメントの問い。負傷 (痛みが遅い) との差別化
 */
export const SCALD_DEF: CardDef = {
  id: 'status_scald',
  name: '火傷',
  cost: 0,
  type: 'spell',
  color: 'red',
  effects: [],
}

/**
 * 呪いの烙印 (状態異常カード 2026-09-02 呪いイベント用)。火傷の恒久版・弱化形:
 * 使用不可で、自ターン終了時に手札にあると自傷1。**ランのデッキに残る** (焚き火・ショップで除去可能)。
 * ?マスの「大報酬と引き換えの恒久汚染」取引に使う
 */
export const BRAND_DEF: CardDef = {
  id: 'status_brand',
  name: '呪いの烙印',
  cost: 0,
  type: 'spell',
  color: 'black',
  effects: [],
}

/**
 * 仮初の烙印 (2026-09-02 StS2 Guilty式の時限呪い): 烙印と同じ滞留HP-1だが、
 * 5戦すると自然に消える (CardInstance.expiresAfterBattles)。イベントの中間対価の器
 */
export const GUILT_DEF: CardDef = {
  id: 'status_guilt',
  name: '仮初の烙印',
  cost: 0,
  type: 'spell',
  color: 'black',
  effects: [],
}

/**
 * がらくた (状態異常カード): 罠壊しが山札に混ぜ込む使用不可の死に札。
 * 負傷 (捨て札に混入) と違い山札へ直接混ざるため、すぐ引かされる = 手札事故を即座に作る。
 */
export const JUNK_DEF: CardDef = {
  id: 'status_junk',
  name: 'がらくた',
  cost: 0,
  type: 'physical',
  color: 'red',
  effects: [],
}

export function getCardDef(id: string): CardDef {
  if (id === WOUND_DEF.id) return WOUND_DEF
  if (id === SCALD_DEF.id) return SCALD_DEF
  if (id === BRAND_DEF.id) return BRAND_DEF
  if (id === GUILT_DEF.id) return GUILT_DEF
  if (id === JUNK_DEF.id) return JUNK_DEF
  const def = allCards.find((c) => c.id === id)
  if (!def) throw new Error(`未定義カード: ${id}`)
  return def
}

export function getEnemyDef(id: string): EnemyDef {
  const def = allEnemies.find((e) => e.id === id)
  if (!def) throw new Error(`未定義の敵: ${id}`)
  return def
}

export function getDeckDef(id: string): DeckDef {
  const def = allDecks.find((d) => d.id === id)
  if (!def) throw new Error(`未定義デッキ: ${id}`)
  return def
}

/** デッキ定義から実カードリストを構築 (同一カード複数枚は uid で区別) */
export function buildDeck(deckId: string): readonly CardInstance[] {
  const deck = getDeckDef(deckId)
  const cards: CardInstance[] = []
  for (const entry of deck.cards) {
    const def = getCardDef(entry.cardId)
    for (let i = 0; i < entry.count; i++) {
      cards.push({ uid: `${entry.cardId}#${i}`, def })
    }
  }
  return cards
}

/** デッキの総枚数 (UI 表示用) */
export function deckSize(deck: DeckDef): number {
  return deck.cards.reduce((sum, e) => sum + e.count, 0)
}

// ---- デバッグ・オーバーレイ (2026-09-01 ユーザー要望「調整案のライブ適用」) ----
// 図鑑の調整モードで編集した定義を、データ読込層で差し替える。engineは適用済みの定義しか見ない
// (純ロジックの原則は不変)。リロードで元に戻る。適用中は debugOverridesActive() が true =
// レポート・セーブのデータ指紋が自然に変わるのに加えて明示のマーカーにも使う。

let overlayPristine: {
  cards: readonly CardDef[]
  enemies: readonly EnemyDef[]
  relics: readonly RelicDef[]
  leaders: readonly LeaderDef[]
} | null = null

export function debugOverridesActive(): boolean {
  return overlayPristine !== null
}

/** id一致で置換・なければ追記。配列はモジュール内の実体を直接書き換える (全lookupがfindなので即反映) */
function patchArray<T extends { readonly id: string }>(arr: readonly T[], items: readonly T[]): [number, number] {
  const a = arr as T[]
  let replaced = 0
  let added = 0
  for (const it of items) {
    const i = a.findIndex((x) => x.id === it.id)
    if (i >= 0) {
      a[i] = it
      replaced++
    } else {
      a.push(it)
      added++
    }
  }
  return [replaced, added]
}

export function applyDebugOverrides(o: {
  readonly cards?: readonly CardDef[]
  readonly enemies?: readonly EnemyDef[]
  readonly relics?: readonly RelicDef[]
  readonly leaders?: readonly LeaderDef[]
}): { replaced: number; added: number } {
  if (overlayPristine === null) {
    overlayPristine = { cards: [...allCards], enemies: [...allEnemies], relics: [...allRelics], leaders: [...allLeaders] }
  }
  let replaced = 0
  let added = 0
  for (const [arr, items] of [
    [allCards, o.cards],
    [allEnemies, o.enemies],
    [allRelics, o.relics],
    [allLeaders, o.leaders],
  ] as const) {
    if (items === undefined) continue
    const [r, a] = patchArray(arr as readonly { readonly id: string }[], items as readonly { readonly id: string }[])
    replaced += r
    added += a
  }
  return { replaced, added }
}

/** オーバーレイを解除して元データへ戻す */
export function clearDebugOverrides(): void {
  if (overlayPristine === null) return
  const restore = <T,>(arr: readonly T[], orig: readonly T[]) => {
    const a = arr as T[]
    a.length = 0
    a.push(...orig)
  }
  restore(allCards, overlayPristine.cards)
  restore(allEnemies, overlayPristine.enemies)
  restore(allRelics, overlayPristine.relics)
  restore(allLeaders, overlayPristine.leaders)
  overlayPristine = null
}
