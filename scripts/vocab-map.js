#!/usr/bin/env node
// vocab-map.js — カード名の漢字から「どの語がどの色のものか」を機械的に出す (2026-09-10)。
//
// なぜ: システム全体の名前 (機構名・UI用語) を付ける時に、特定の色のカードが使っている語を選ぶと
// 「その流派の道具」に見える。2026-09-10 に「伏せ」の言い換え第1案「狩人の仕掛け」が
// 「緑のこのはだけができるものじゃないから」で却下されたのが実例。
//
// 使い方: node scripts/vocab-map.js          → 標準出力に Markdown
//         node scripts/vocab-map.js --write  → docs/vocab-map.md を上書き
//
// 判定: ある漢字が「その色のカード名に N 回以上出て、かつ全色合計の R 以上を占める」なら、その色の所有語。
import { readFileSync, writeFileSync } from 'node:fs'

const COLORS = ['green', 'blue', 'red', 'white', 'black']
const JA = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
const OWN_MIN = 3 // その色に何回出れば候補か
const OWN_RATIO = 0.7 // その色が全体の何割を占めれば「所有」か
const SHARED_MIN_COLORS = 3 // 何色にまたがれば中立か
const SHARED_MIN_TOTAL = 5

/** カード名から漢字だけを数える (かな・カナ・英数・記号は語の個性を持たないので落とす) */
function countKanji(names) {
  const out = {}
  for (const name of names) {
    for (const ch of name) {
      if (/[ぁ-んァ-ヶー・0-9a-zA-Z（）()+\s]/.test(ch)) continue
      out[ch] = (out[ch] ?? 0) + 1
    }
  }
  return out
}

const byColor = {}
const all = {}
for (const c of COLORS) {
  const cards = JSON.parse(readFileSync(new URL(`../src/data/cards.${c}.json`, import.meta.url), 'utf8'))
  byColor[c] = countKanji(cards.map((x) => x.name))
  for (const [ch, n] of Object.entries(byColor[c])) all[ch] = (all[ch] ?? 0) + n
}

const owned = {}
for (const c of COLORS) {
  owned[c] = Object.entries(byColor[c])
    .filter(([ch, n]) => n >= OWN_MIN && n / all[ch] >= OWN_RATIO)
    .sort((a, b) => b[1] - a[1])
}
const shared = Object.entries(all)
  .filter(([ch, n]) => COLORS.filter((c) => byColor[c][ch]).length >= SHARED_MIN_COLORS && n >= SHARED_MIN_TOTAL)
  .sort((a, b) => b[1] - a[1])
  .slice(0, 24)

/** 候補語がデータのどこかに出るか (カード名・敵名・敵の行動名・レリック名) */
function findAll(term) {
  const hits = []
  for (const c of COLORS)
    for (const x of JSON.parse(readFileSync(new URL(`../src/data/cards.${c}.json`, import.meta.url), 'utf8')))
      if (x.name.includes(term)) hits.push(`${x.name}(${JA[c]})`)
  for (const e of JSON.parse(readFileSync(new URL('../src/data/enemies.json', import.meta.url), 'utf8'))) {
    if (e.name?.includes(term)) hits.push(`${e.name}(敵)`)
    for (const m of [...(e.moves ?? []), ...(e.movesVsSet ?? []), ...(e.movesBelowHalf ?? [])])
      if (m.name?.includes(term)) hits.push(`${m.name}(敵の行動)`)
  }
  for (const r of JSON.parse(readFileSync(new URL('../src/data/relics.json', import.meta.url), 'utf8')))
    if (r.name.includes(term)) hits.push(`${r.name}(レリック)`)
  return [...new Set(hits)]
}

// 「システム名の候補になりうるか」を実際に引いて確かめる語
const CANDIDATES = ['夜', '月', '宵', '影', '闇', '息', '隠', '秘', '手', '印', '符', '罠', '構え', '読み', '気配']

let md = `# 語彙の所有マップ（どの語がどの色のものか）

生成: \`node scripts/vocab-map.js --write\`（実データのカード名から機械生成した**観察**であり、規約ではない）

## なぜ要るか

2026-09-10、「伏せ」を世界観の言葉に置き換える第1案「狩人の仕掛け（獣道に罠を張る）」が
**「緑のこのはだけができるものじゃないから」** で却下された。伏せは15リーダー・4色に共通のシステム機構なのに、
語彙が緑（狩人）の職能に見えた、という失敗。**システム全体の名前を付ける時は、どの色にも属さない語から選ぶ。**

## 色が所有する語（その色のカード名に${OWN_MIN}回以上出て、かつ全色合計の${OWN_RATIO * 100}%以上）

`
for (const c of COLORS) md += `- **${JA[c]}**: ${owned[c].map(([ch, n]) => `${ch}(${n})`).join('　')}\n`

md += `
## 複数の色にまたがる語（＝中立。${SHARED_MIN_COLORS}色以上に出る）

${shared.map(([ch, n]) => `${ch}(${n})`).join('　')}

**手** と **一** が中立なのは、「隠し手」「一手」のような抽象語がどの色でも読める根拠になる。

## 候補語を実際に引いた結果

| 語 | 既存の使用 |
|---|---|
`
for (const t of CANDIDATES) {
  const hits = findAll(t)
  md += `| ${t} | ${hits.length ? hits.slice(0, 6).join('、') + (hits.length > 6 ? ` …他${hits.length - 6}` : '') : '**未使用（フリー）**'} |\n`
}

md += `
**夜・月・宵・息・隠・秘** が空いているのは偶然ではない。これらは色の道具ではなく **世界そのもの**（永い夜・月の塔）か
**行為の抽象**だから、どの流派も所有していない。システム全体の名前はここから引くのが最も安全。

## 既存のキーワード（別の意味で使用済み。再利用しない）

- **潜伏** = 敵の殻（尽きるまでHPに通らない。岩皮の甲虫・潜行する大地虫）
- **構え** = 通常札の名前（荒角の構え・大幹の構え・谺の構え・余勢の構え）
- **気** = 青寄り（霊気の系列）。「気配」を使う時は霊気と読み違えられないか確認する
`

if (process.argv.includes('--write')) {
  writeFileSync(new URL('../docs/vocab-map.md', import.meta.url), md)
  console.log('wrote docs/vocab-map.md')
} else {
  console.log(md)
}
