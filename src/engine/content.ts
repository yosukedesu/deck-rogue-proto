// engine/content.ts — data/*.json の読み込みと参照
// JSON は本実装へそのまま持っていく共通資産。スキーマは types.ts が一次資料。

import cardsGreenJson from '../data/cards.green.json' with { type: 'json' }
import cardsBlueJson from '../data/cards.blue.json' with { type: 'json' }
import cardsRedJson from '../data/cards.red.json' with { type: 'json' }
import decksJson from '../data/decks.json' with { type: 'json' }
import enemiesJson from '../data/enemies.json' with { type: 'json' }
import leadersJson from '../data/leaders.json' with { type: 'json' }
import type { CardColor, CardDef, CardInstance, DeckDef, EnemyDef, LeaderDef } from './types.ts'

// 色は JSON に書かず、ファイル単位でここで付与する (JSONを本実装へ持ち込む際の共通規約)
const withColor = (cards: readonly unknown[], color: CardColor): readonly CardDef[] =>
  (cards as readonly Omit<CardDef, 'color'>[]).map((c) => ({ ...c, color }))

export const allCards: readonly CardDef[] = [
  ...withColor(cardsGreenJson, 'green'),
  ...withColor(cardsBlueJson, 'blue'),
  ...withColor(cardsRedJson, 'red'),
]
export const allEnemies = enemiesJson as readonly EnemyDef[]
export const allDecks = decksJson as readonly DeckDef[]
export const allLeaders = leadersJson as readonly LeaderDef[]

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
      category: 'permanent',
      color: leader.colors[0],
      effects: leader.passive,
    },
  }
}

export function getCardDef(id: string): CardDef {
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
