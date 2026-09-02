// scripts/dump-card-audit-csv.ts — docs/cards-<color>.csv を機械査定 (scripts/card-audit.ts の assess) から再生成する。
// 使い方: npx tsx scripts/dump-card-audit-csv.ts green > docs/cards-green.csv
import { allCards } from '../src/engine/content.ts'
import { assess } from './card-audit.ts'

const color = process.argv[2] ?? 'green'
const RAR: Record<string, string> = { common: 'コモン', uncommon: 'アンコモン', rare: 'レア' }
const TYPE: Record<string, string> = { physical: '物理', spell: '呪文', reaction: 'リアクション', permanent: '置物' }
const fx = (c: (typeof allCards)[number]) => {
  const one = (e: { effect: string; amount?: number; trigger: string; target?: string; pierce?: boolean }) =>
    `${e.trigger !== 'onPlay' ? `[${e.trigger}]` : ''}${e.effect}${e.amount ?? ''}${e.target === 'all' ? '@all' : ''}${e.pierce ? '(貫通)' : ''}`
  return c.modes && c.modes.length > 0
    ? c.modes.map((m) => `『${m.name}』${m.effects.map(one).join('+')}`).join(' / ')
    : c.effects.map(one).join(' ')
}
const ALLOW = (cost: number) => 6 * cost + 2
console.log('﻿id,名前,レアリティ,コスト,タイプ,消滅,効果,特記,機械VP,許容VP,定価%,査定メモ')
for (const c of allCards.filter((x) => x.color === color)) {
  const a = assess(c)
  const cost = c.xCost === true ? 'X' : String(c.cost)
  const notes = [c.xCost ? 'Xコスト' : '', c.discardCost ? `捨て${c.discardCost}` : '', c.exhaustCost ? `消滅コスト${c.exhaustCost}` : '', c.type === 'permanent' ? '置物=生涯×3レンズで読む' : '', c.type === 'reaction' ? 'リアクション=条件係数の手動査定' : '']
    .filter(Boolean)
    .join('・')
  const row = [c.id, c.name, RAR[c.rarity ?? 'common'], cost, TYPE[c.type] ?? c.type, c.exhaust ? '消滅' : '', fx(c), notes, a.computable ? a.vp.toFixed(1) : '', String(ALLOW(c.xCost ? 3 : c.cost)), a.computable ? String(Math.round(a.pct)) : '', '']
  console.log(row.map((v) => (/[,"\n]/.test(v) ? `"${v.replace(/"/g, '""')}"` : v)).join(','))
}
