// 戦闘評価テレメトリの横断集計 (2026-09-02 残件議論の採択 #115)。
// 使い方: npx tsx scripts/aggregate-ratings.ts <ファイル or ディレクトリ...>
//   .json = セーブ/バックアップ (history[].rating) / .md = プレイレポート (戦闘履歴テーブル)
// 出力: 敵ごとの 戦闘数・強さ平均・面白さ平均・理不尽票・メモ抜粋 (面白さ昇順 = 作り直し候補が上に来る)
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'

type Row = { enemy: string; strength?: number; fun?: number; lossFeel?: string; note?: string; result?: string }

function collect(path: string, out: Row[]): void {
  const st = statSync(path)
  if (st.isDirectory()) {
    for (const f of readdirSync(path)) collect(join(path, f), out)
    return
  }
  if (path.endsWith('.json')) {
    try {
      const doc = JSON.parse(readFileSync(path, 'utf8'))
      const history: unknown[] = Array.isArray(doc?.history) ? doc.history : []
      for (const h of history as Array<Record<string, unknown>>) {
        const r = (h.rating ?? {}) as Record<string, unknown>
        out.push({
          enemy: String(h.enemyId ?? '?'),
          strength: typeof r.strength === 'number' ? r.strength : undefined,
          fun: typeof r.fun === 'number' ? r.fun : undefined,
          lossFeel: typeof r.lossFeel === 'string' ? r.lossFeel : undefined,
          note: typeof r.note === 'string' ? r.note : undefined,
          result: typeof h.result === 'string' ? h.result : undefined,
        })
      }
    } catch {
      /* JSONでないファイルは無視 */
    }
  } else if (path.endsWith('.md')) {
    const text = readFileSync(path, 'utf8')
    // | # | 敵 | 結果 | ターン | HP | 強さ | 面白さ | (敗因 |) メモ |
    for (const line of text.split('\n')) {
      const m = line.match(/^\|\s*(\d+)\s*\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)\|(.*)\|\s*$/)
      if (!m) continue
      const rest = m[8].split('|').map((x) => x.trim())
      const hasFeel = rest.length >= 2
      out.push({
        enemy: m[2].trim().replace(/（強個体）/, ''),
        result: m[3].trim(),
        strength: m[6].trim() === '' ? undefined : Number(m[6]),
        fun: m[7].trim() === '' ? undefined : Number(m[7]),
        lossFeel: hasFeel ? (rest[0] === '理不尽' ? 'unfair' : rest[0] === '構築の失敗' ? 'build' : undefined) : undefined,
        note: (hasFeel ? rest[1] : rest[0]) || undefined,
      })
    }
  }
}

const rows: Row[] = []
for (const p of process.argv.slice(2)) collect(p, rows)
if (rows.length === 0) {
  console.error('usage: npx tsx scripts/aggregate-ratings.ts <save.json | report.md | dir>...')
  process.exit(1)
}
const byEnemy = new Map<string, Row[]>()
for (const r of rows) byEnemy.set(r.enemy, [...(byEnemy.get(r.enemy) ?? []), r])
const avg = (xs: number[]) => (xs.length ? (xs.reduce((a, b) => a + b, 0) / xs.length).toFixed(2) : '-')
const table = [...byEnemy.entries()]
  .map(([enemy, rs]) => {
    const st = rs.map((r) => r.strength).filter((x): x is number => x !== undefined)
    const fn = rs.map((r) => r.fun).filter((x): x is number => x !== undefined)
    const unfair = rs.filter((r) => r.lossFeel === 'unfair').length
    const losses = rs.filter((r) => r.result === 'lost' || r.result === '敗北').length
    const notes = rs.map((r) => r.note).filter(Boolean).slice(0, 3).join(' / ')
    return { enemy, n: rs.length, st: avg(st), fn: avg(fn), fnNum: fn.length ? fn.reduce((a, b) => a + b, 0) / fn.length : 99, unfair, losses, notes }
  })
  .sort((a, b) => a.fnNum - b.fnNum)
console.log('| 敵 | 戦闘数 | 敗北 | 強さ平均 | 面白さ平均 | 理不尽票 | メモ抜粋 |')
console.log('|---|---|---|---|---|---|---|')
for (const t of table) console.log(`| ${t.enemy} | ${t.n} | ${t.losses} | ${t.st} | ${t.fn} | ${t.unfair} | ${t.notes} |`)
console.error(`# ${rows.length}戦闘 / ${byEnemy.size}種。面白さ平均の低い順 = 作り直し候補。理不尽票が2以上の敵は balance-policy.md の作り直し基準の入力`)
