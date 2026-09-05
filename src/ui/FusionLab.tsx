// ui/FusionLab.tsx — 合成ラボ (単独ページ)。工房の「効果の合体」の結果を一覧で検分する開発者ツール (2026-09-05 ユーザー要望
// 「合成の結果を一覧したいので開発者ツールの合成ラボを単独ページに切り出して」)。engine には触らない読み取り専用の層。
import { useMemo, useState } from 'react'
import type React from 'react'
import { allCards, allDecks, getCardDef } from '../engine/content.ts'
import { fuseBlockReason, fuseCards, fusionNotes } from '../engine/fusion.ts'
import { cardCostLabel } from '../engine/summary.ts'
import { upgradeCard } from '../engine/upgrade.ts'
import type { CardDef, CardInstance } from '../engine/types.ts'

type Mode = 'one' | 'same' | 'deck' | 'all'
const COLORS = ['green', 'blue', 'red', 'white', 'black'] as const
const COLOR_JA: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
const TYPE_JA: Record<string, string> = { physical: '物理', spell: '呪文', reaction: 'リアクション', permanent: '置物' }

interface Row {
  readonly key: string
  readonly a: CardInstance
  readonly b: CardInstance
  readonly result: CardDef | null
  readonly reason: string | null
  readonly notes: readonly string[]
}

function inst(def: CardDef, uid: string): CardInstance {
  return { uid, def }
}

export function FusionLabPage({
  onClose,
  renderCard,
  effectText,
}: {
  onClose: () => void
  renderCard: (def: CardDef) => React.ReactNode
  effectText: (def: CardDef) => string
}) {
  const [color, setColor] = useState<string>('green')
  const [mode, setMode] = useState<Mode>('one')
  const [baseId, setBaseId] = useState<string>('')
  const [baseUpgraded, setBaseUpgraded] = useState(false)
  const [deckId, setDeckId] = useState<string>('')
  const [typeFilter, setTypeFilter] = useState<string>('')
  const [minCost, setMinCost] = useState<string>('')
  const [maxCost, setMaxCost] = useState<string>('')
  const [exhaustOnly, setExhaustOnly] = useState(false)
  const [notesOnly, setNotesOnly] = useState(false)
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<'cost' | 'costDesc' | 'name' | 'effects'>('cost')
  const [limit, setLimit] = useState(300)
  const [selected, setSelected] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  const pool = useMemo(() => allCards.filter((c) => c.color === color && !c.id.endsWith('_token') && !c.id.startsWith('status_')), [color])
  const decks = useMemo(() => allDecks.filter((d) => d.cards.some((c) => getCardDefSafe(c.cardId)?.color === color)), [color])
  const base = pool.find((c) => c.id === baseId) ?? pool[0]

  const rows = useMemo<Row[]>(() => {
    const mk = (a: CardDef, b: CardDef, ua = false): Row => {
      let A = inst(a, `a_${a.id}`)
      if (ua) A = upgradeCard(A)
      const B = inst(b, `b_${b.id}`)
      const reason = fuseBlockReason(A, B)
      let result: CardDef | null = null
      let err: string | null = null
      if (reason === null) {
        try { result = fuseCards(A, B) } catch (e) { err = e instanceof Error ? e.message : String(e) }
      }
      return { key: `${A.def.name}__${b.id}`, a: A, b: B, result, reason: reason ?? err, notes: reason === null ? fusionNotes(A, B) : [] }
    }
    if (mode === 'one') {
      if (!base) return []
      return pool.map((b) => mk(base, b, baseUpgraded))
    }
    if (mode === 'same') return pool.map((c) => mk(c, c))
    if (mode === 'deck') {
      const d = decks.find((x) => x.id === deckId) ?? decks[0]
      if (!d) return []
      const ids = [...new Set(d.cards.map((c) => c.cardId))].filter((id) => getCardDefSafe(id)?.color === color)
      const out: Row[] = []
      for (let i = 0; i < ids.length; i++) for (let j = i + 1; j < ids.length; j++) out.push(mk(getCardDef(ids[i]), getCardDef(ids[j])))
      return out
    }
    const out: Row[] = []
    for (let i = 0; i < pool.length; i++) for (let j = i; j < pool.length; j++) out.push(mk(pool[i], pool[j]))
    return out
  }, [pool, mode, base, baseUpgraded, decks, deckId, color])

  const filtered = useMemo(() => {
    const q = search.trim()
    const lo = minCost === '' ? -1 : Number(minCost)
    const hi = maxCost === '' ? 99 : Number(maxCost)
    const list = rows.filter((r) => {
      if (!r.result) return q === '' || r.a.def.name.includes(q) || r.b.def.name.includes(q)
      const cost = r.result.xCost === true ? 99 : r.result.cost
      if (typeFilter && r.result.type !== typeFilter) return false
      if (!(r.result.xCost === true) && (cost < lo || cost > hi)) return false
      if (exhaustOnly && r.result.exhaust !== true) return false
      if (notesOnly && r.notes.length === 0) return false
      if (q !== '' && !(r.a.def.name.includes(q) || r.b.def.name.includes(q) || r.result.name.includes(q) || effectText(r.result).includes(q))) return false
      return true
    })
    const costOf = (r: Row) => (r.result ? (r.result.xCost === true ? 99 : r.result.cost) : 100)
    list.sort((x, y) =>
      sort === 'cost' ? costOf(x) - costOf(y) : sort === 'costDesc' ? costOf(y) - costOf(x) : sort === 'name' ? (x.result?.name ?? '').localeCompare(y.result?.name ?? '', 'ja') : effectText(x.result ?? x.a.def).length - effectText(y.result ?? y.a.def).length,
    )
    return list
  }, [rows, search, minCost, maxCost, typeFilter, exhaustOnly, notesOnly, sort, effectText])

  const stats = useMemo(() => {
    const byType: Record<string, number> = {}
    const byCost: Record<string, number> = {}
    let blocked = 0
    let exhaust = 0
    for (const r of filtered) {
      if (!r.result) { blocked++; continue }
      byType[r.result.type] = (byType[r.result.type] ?? 0) + 1
      const c = r.result.xCost === true ? 'X' : String(r.result.cost)
      byCost[c] = (byCost[c] ?? 0) + 1
      if (r.result.exhaust) exhaust++
    }
    return { byType, byCost, blocked, exhaust }
  }, [filtered])

  const sel = filtered.find((r) => r.key === selected) ?? null

  const copyMd = () => {
    const L = ['| 素材A × 素材B | 結果 | 効果 | 注記 |', '|---|---|---|---|']
    for (const r of filtered.slice(0, limit)) {
      const ab = `${r.a.def.name}(${cardCostLabel(r.a.def)}E) × ${r.b.def.name}(${cardCostLabel(r.b.def)}E)`
      if (!r.result) L.push(`| ${ab} | 不可 | ${r.reason ?? ''} | |`)
      else L.push(`| ${ab} | ${r.result.name} ${cardCostLabel(r.result)}E ${TYPE_JA[r.result.type] ?? r.result.type}${r.result.exhaust ? '・消滅' : ''} | ${effectText(r.result).replace(/\|/g, '｜')} | ${r.notes.join('／').replace(/\|/g, '｜')} |`)
    }
    void navigator.clipboard?.writeText(L.join('\n')).then(() => { setCopied(true); setTimeout(() => setCopied(false), 1200) })
  }

  return (
    <div className="viewer-overlay" style={{ zIndex: 80 }}>
      <div className="viewer-panel lab-page">
        <div className="viewer-head">
          <span className="viewer-title">🔬 合成ラボ（一覧）</span>
          <span className="hint">工房「効果の合体」の結果を検分する。engine の fuseCards をそのまま呼ぶ＝本番と同じ結果</span>
          <button className="btn" onClick={onClose}>閉じる</button>
        </div>
        <div className="lab-controls">
          <label>色 <select value={color} onChange={(e) => { setColor(e.target.value); setBaseId(''); setDeckId(''); setSelected(null) }}>{COLORS.map((c) => <option key={c} value={c}>{COLOR_JA[c]}</option>)}</select></label>
          <span className="lab-tabs">
            {([['one', '基準1枚×全カード'], ['same', '同名（真・化）一覧'], ['deck', 'デッキ内の全ペア'], ['all', '全ペア']] as const).map(([m, label]) => (
              <button key={m} className={`btn${mode === m ? ' btn-primary' : ''}`} onClick={() => { setMode(m); setSelected(null) }}>{label}</button>
            ))}
          </span>
          {mode === 'one' && (
            <>
              <label>基準 <select value={base?.id ?? ''} onChange={(e) => setBaseId(e.target.value)}>{pool.map((c) => <option key={c.id} value={c.id}>{c.name}（{cardCostLabel(c)}E・{TYPE_JA[c.type]}）</option>)}</select></label>
              <label title="基準札を鍛え済み (+) にして合成する = 鍛えの引き継ぎの確認"><input type="checkbox" checked={baseUpgraded} onChange={(e) => setBaseUpgraded(e.target.checked)} /> 基準を鍛え済みに</label>
            </>
          )}
          {mode === 'deck' && (
            <label>デッキ <select value={(decks.find((x) => x.id === deckId) ?? decks[0])?.id ?? ''} onChange={(e) => setDeckId(e.target.value)}>{decks.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}</select></label>
          )}
        </div>
        <div className="lab-controls">
          <label>結果の型 <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}><option value="">すべて</option>{Object.entries(TYPE_JA).map(([k, v]) => <option key={k} value={k}>{v}</option>)}</select></label>
          <label>コスト <input value={minCost} onChange={(e) => setMinCost(e.target.value)} size={2} placeholder="min" /> 〜 <input value={maxCost} onChange={(e) => setMaxCost(e.target.value)} size={2} placeholder="max" /></label>
          <label><input type="checkbox" checked={exhaustOnly} onChange={(e) => setExhaustOnly(e.target.checked)} /> 消滅つきだけ</label>
          <label><input type="checkbox" checked={notesOnly} onChange={(e) => setNotesOnly(e.target.checked)} /> 注記ありだけ</label>
          <label>検索 <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="素材名・結果名・効果" size={18} /></label>
          <label>並び <select value={sort} onChange={(e) => setSort(e.target.value as typeof sort)}><option value="cost">コスト昇順</option><option value="costDesc">コスト降順</option><option value="name">結果名</option><option value="effects">効果の長さ</option></select></label>
          <button className="btn" onClick={copyMd}>{copied ? '✅ コピーした' : '表をmdでコピー'}</button>
        </div>
        <div className="hint lab-stats">
          {filtered.length}件（表示{Math.min(limit, filtered.length)}）／合成不可 {stats.blocked}／消滅つき {stats.exhaust}／型: {Object.entries(stats.byType).map(([k, v]) => `${TYPE_JA[k] ?? k}${v}`).join('・') || '-'}／コスト: {Object.entries(stats.byCost).sort().map(([k, v]) => `${k}E:${v}`).join(' ') || '-'}
        </div>
        <div className="lab-body">
          <div className="lab-table-wrap">
            <table className="lab-table">
              <thead>
                <tr><th>素材A</th><th>素材B</th><th>結果</th><th>E</th><th>型</th><th>効果</th><th>注記</th></tr>
              </thead>
              <tbody>
                {filtered.slice(0, limit).map((r) => (
                  <tr key={r.key} className={`${selected === r.key ? 'lab-row-selected' : ''}${r.result ? '' : ' lab-row-blocked'}`} onClick={() => setSelected(r.key)}>
                    <td>{r.a.def.name}<span className="pile-info">（{cardCostLabel(r.a.def)}E {TYPE_JA[r.a.def.type]?.[0]}）</span></td>
                    <td>{r.b.def.name}<span className="pile-info">（{cardCostLabel(r.b.def)}E {TYPE_JA[r.b.def.type]?.[0]}）</span></td>
                    {r.result ? (
                      <>
                        <td>{r.result.id.startsWith('fusion_') ? '⭐' : ''}{r.result.name}{r.result.exhaust ? <span className="pile-info">・消滅</span> : null}{r.result.retain ? <span className="pile-info">・保持</span> : null}</td>
                        <td>{cardCostLabel(r.result)}</td>
                        <td>{TYPE_JA[r.result.type]}</td>
                        <td className="lab-effects">{effectText(r.result)}</td>
                        <td className="lab-notes">{r.notes.length > 0 ? r.notes.map((n) => n.split(':')[0]).join('・') : ''}</td>
                      </>
                    ) : (
                      <td colSpan={5} className="lab-effects">❌ {r.reason}</td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
            {filtered.length > limit && <button className="btn" style={{ marginTop: 8 }} onClick={() => setLimit((l) => l + 300)}>さらに300件</button>}
          </div>
          <div className="lab-detail">
            {sel && sel.result ? (
              <>
                <div className="setup-section-title">結果</div>
                <div className="hand-cards">{renderCard(sel.result)}</div>
                {sel.notes.length > 0 && <ul className="hint">{sel.notes.map((n, i) => <li key={i}>{n}</li>)}</ul>}
                <div className="setup-section-title">素材</div>
                <div className="hand-cards">{renderCard(sel.a.def)}{renderCard(sel.b.def)}</div>
                <div className="hint">素材コスト {cardCostLabel(sel.a.def)}E + {cardCostLabel(sel.b.def)}E → 結果 {cardCostLabel(sel.result)}E</div>
              </>
            ) : sel ? (
              <div className="hint">❌ {sel.reason}</div>
            ) : (
              <div className="hint">行をクリックすると結果と素材のカード枠と注記を表示します</div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

function getCardDefSafe(id: string): CardDef | null {
  try { return getCardDef(id) } catch { return null }
}
