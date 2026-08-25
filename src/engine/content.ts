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

export function getRelicDef(id: string): RelicDef {
  const def = allRelics.find((r) => r.id === id)
  if (!def) throw new Error(`未定義レリック: ${id}`)
  return def
}

/** A型レリックを「戦闘開始時から場にある不可視の置物」として実体化する (リーダーパッシブと同型) */
export function buildRelicPermanent(relic: RelicDef): CardInstance {
  return {
    uid: `relic_${relic.id}`,
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
