// scripts/fusion-table.ts — 工房の机上検証用テーブル (2026-09-05)。
// 使い方: npx tsx scripts/fusion-table.ts --color green [--same] [--sample N --seed S] [--deck deckId] [--pairs a,b;c,d]
// 合成結果 (名前/コスト/タイプ/効果) を md 表で出す。Opus に「ランで工房を踏む」代わりに表を読ませる = 1ランの1/10のコストで数百ペアを検分できる
import { allCards, allDecks, getCardDef } from '../src/engine/content.ts'
import { fuseBlockReason, fuseCards } from '../src/engine/fusion.ts'
import type { CardDef, DeclarativeEffect } from '../src/engine/types.ts'

const args = process.argv.slice(2)
const opt = (k: string): string | undefined => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined }
const flag = (k: string) => args.includes(k)
const color = opt('--color') ?? 'green'
const pool = allCards.filter((c) => c.color === color && !c.id.endsWith('_token'))

function rng(seed: number) { let s = seed >>> 0; return () => { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296 } }
const JA: Record<string, string> = {
  dealDamage: 'ダメ', gainBlock: 'ブロック', counter: '返し', addGrowth: '成長+', addMomentum: '勢い+', drawCards: 'ドロー', gainEnergy: '一時マナ+', gainEnergyMax: '上限+',
  exposeEnemy: '急所', weakenEnemy: '威圧', negate: '打ち消し', doubleGrowth: '成長2倍', doubleMomentum: '勢い2倍', dischargeGrowth: '成長×Nダメ', dischargeGrowthBlock: '成長×Nブロック',
  dischargeMomentumDamage: '勢い×Nダメ', dischargeMomentumBlock: '勢い×Nブロック', dischargeMomentumGrowth: '勢い→成長', dischargeMomentumVolley: '勢い×N×3回', momentumCarryHalf: '勢い半分持ち越し(常在)', dealDamagePerEnergyMax: '上限×Nダメ', gainBlockPerEnergyMax: '上限×Nブロック',
  dealDamagePerAttackPlayed: '攻撃数×Nダメ', shatterBlock: '粉砕', shatterBlockConvert: '粉砕換金', impulseDraw: '衝動', searchDeck: 'サーチ', retrieveFromDiscard: '回収', upgradeInHand: '手札を鍛える', growSelf: 'プレイごと+', addCopyToDiscard: 'コピー', gainSetSlot: '伏せ枠+',
}
const TR: Record<string, string> = { onPlay: '', onTurnStart: '毎T:', onAttackPlayed: '攻撃ごと:', onAttackIncoming: '被攻撃前:', onAttacked: '被攻撃後:', onEnemyAction: '敵行動時:', onEnemyBuffed: '敵強化時:', onEnemyDefended: '敵防御時:', onGrowthGained: '成長ごと:', onMomentumGained: '勢いごと:', onCardSet: '伏せるたび:', onSetDestroyed: '伏せ破壊時:' }
const fx = (e: DeclarativeEffect) => `${TR[e.trigger] ?? e.trigger + ':'}${JA[e.effect] ?? e.effect}${e.amount !== undefined ? e.amount : ''}${e.target === 'all' ? '@全' : ''}${e.pierce ? '貫' : ''}${e.growthMultiplier ? `(成長×${e.growthMultiplier})` : ''}${e.momentumMultiplier ? `(勢い×${e.momentumMultiplier})` : ''}${e.xHits ? '×X' : ''}${e.condition ? '[条件]' : ''}`
const desc = (d: CardDef) => {
  const parts = d.effects.map(fx)
  if (d.modes?.length) parts.push(`モード{${d.modes.map((m) => m.effects.map(fx).join('+')).join(' / ')}}`)
  const tags = [d.exhaust ? '消滅' : '', d.retain ? '保持' : '', d.xCost ? 'X' : '', d.discardCost ? `捨て${d.discardCost}` : '', d.exhaustCost ? `消滅コスト${d.exhaustCost}` : '', d.freeIfMomentumAtLeast ? `勢い${d.freeIfMomentumAtLeast}以上で0E` : '', d.freeIfHandAllPhysical ? '手札物理のみ0E' : ''].filter(Boolean)
  return `${parts.join('、')}${tags.length ? `【${tags.join('・')}】` : ''}`
}
const T = (d: CardDef) => ({ physical: '物', spell: '呪', reaction: '反', permanent: '置' }[d.type] ?? d.type)
const row = (a: CardDef, b: CardDef) => {
  const reason = fuseBlockReason({ uid: 'a', def: a }, { uid: 'b', def: b })
  if (reason) return `| ${a.name} × ${b.name} | 不可 | ${reason} |`
  const r = fuseCards({ uid: 'a', def: a }, { uid: 'b', def: b })
  return `| ${a.name}(${a.xCost ? 'X' : a.cost}E${T(a)}) × ${b.name}(${b.xCost ? 'X' : b.cost}E${T(b)}) | ${r.name} ${r.xCost ? 'X' : r.cost}E${T(r)} | ${desc(r)} |`
}
const out: string[] = []
const header = () => { out.push('| 素材A × 素材B | 結果 | 効果 |'); out.push('|---|---|---|') }
if (flag('--same')) {
  out.push(`## 同名合成（真・化）${color} ${pool.length}種`); header()
  for (const c of pool) out.push(row(c, c))
}
const deckId = opt('--deck')
if (deckId) {
  const deck = allDecks.find((d) => d.id === deckId); if (!deck) throw new Error(`deck ${deckId}`)
  const ids = [...new Set(deck.cards.map((c) => c.cardId))]
  out.push(`\n## デッキ内ペア ${deck.name} (${ids.length}種)`); header()
  for (let i = 0; i < ids.length; i++) for (let j = i + 1; j < ids.length; j++) out.push(row(getCardDef(ids[i]), getCardDef(ids[j])))
}
const sample = Number(opt('--sample') ?? 0)
if (sample > 0) {
  const r = rng(Number(opt('--seed') ?? 1))
  out.push(`\n## ランダム${sample}ペア（${color}・seed ${opt('--seed') ?? 1}）`); header()
  const seen = new Set<string>()
  while (seen.size < sample) {
    const a = pool[Math.floor(r() * pool.length)], b = pool[Math.floor(r() * pool.length)]
    if (a.id === b.id) continue
    const key = [a.id, b.id].sort().join('|'); if (seen.has(key)) continue; seen.add(key)
    out.push(row(a, b))
  }
}
const pairs = opt('--pairs')
if (pairs) { out.push('\n## 指定ペア'); header(); for (const p of pairs.split(';')) { const [x, y] = p.split(','); out.push(row(getCardDef(x.trim()), getCardDef(y.trim()))) } }
console.log(out.join('\n'))
