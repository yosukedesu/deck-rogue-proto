// scripts/pixellab-sheet.ts — PixelLab (ドット絵生成) へ渡す素材一覧 (発注書) をデータから生成する (2026-09-07)。
// 使い方: npx tsx scripts/pixellab-sheet.ts > docs/pixellab-assets.md
// 命名規約: Assets/Resources/Art/<種別>/<id>.png (Point フィルタ・非圧縮・PPU は Editor の ArtImporter が自動設定)。
// プレースホルダー (コード生成) と同じ名前・同じ寸法で置けば差し替わる。
import { allCards, allEnemies, allRelics, allLeaders, allEncounters } from '../src/engine/content.ts'
import { ACT_BOSS_POOLS, ELITE_POOLS } from '../src/engine/map.ts'

const lines: string[] = []
const p = (s = '') => lines.push(s)

p('# PixelLab 素材一覧（発注書）')
p()
p('`npx tsx scripts/pixellab-sheet.ts` で `src/data` から生成。**置き場と名前は `Assets/Resources/Art/<種別>/<id>.png`**。')
p('寸法はドット絵の実寸（画面には整数倍で置く。基準解像度 1920×1080 では 3〜4 倍）。背景は透過 PNG。パレットは種別ごとに揃える。')
p('プレースホルダー（コード生成）が同じ名前で出ているので、1枚ずつ差し替えて見比べられる。')
p()
p('## 寸法と枚数の要約')
p()
p('| 種別 | フォルダ | 実寸 (px) | 枚数 | 備考 |')
p('|---|---|---|---|---|')
const enemies = allEnemies
const membersOf = (encId: string): string[] => {
  const enc = allEncounters.find((e) => e.id === encId)
  return enc ? enc.members.map((m) => m.enemyId) : [encId]
}
const bossIds = new Set(ACT_BOSS_POOLS.flat().flatMap(membersOf))
const eliteIds = new Set(ELITE_POOLS.flat().flatMap(membersOf))
const normal = enemies.filter((e) => !bossIds.has(e.id) && !eliteIds.has(e.id))
p(`| 敵（通常） | enemies | 96×96 | ${normal.length} | 待機2コマ（\`<id>.png\` と \`<id>_2.png\`）。被弾は白フラッシュで代用 |`)
p(`| 敵（エリート） | enemies | 128×128 | ${eliteIds.size} | 同上 |`)
p(`| 敵（幕ボス） | enemies | 160×160 | ${bossIds.size} | 同上。第2形態は色替えで代用 |`)
p(`| リーダー（戦闘内ちび） | leaders | 96×96 | ${allLeaders.length} | 頭身低め（確定済みルール表「絵柄の頭身」）。立ち絵は人間の絵師へ外注 |`)
p(`| リーダー（アイコン） | leaders | 32×32 | ${allLeaders.length} | \`<id>_icon.png\`。セットアップ・ラン画面用 |`)
p(`| レリック | relics | 32×32 | ${allRelics.length} | |`)
p('| カード枠 | ui | 64×96 | 4タイプ×5色＋共通 | `card_<type>_<color>.png`。9スライス（角12px） |')
p('| レア度の宝石 | ui | 12×12 | 3 | `gem_common/uncommon/rare.png` |')
p('| コスト玉 | ui | 20×20 | 1 | `cost_orb.png`（数字は文字で乗せる） |')
p('| 状態アイコン（16px） | icons | 16×16 | 下表 | インライン用（カード文面・意図・バフ欄） |')
p('| 意図アイコン | icons | 24×24 | 13 | `intent_<kind>.png` |')
p('| マップのノード | map | 32×32 | 8 | `node_<type>.png`（battle/elite/boss/shop/campfire/workshop/unknown/treasure） |')
p('| 背景（幕） | bg | 480×270 | 3 | `act1/act2/act3.png`。4倍で 1920×1080 |')
p('| 焚き火・工房・ショップの情景 | scenes | 240×135 | 4 | `campfire/workshop/shop/event.png` |')
p('| UI 部品 | ui | 各種 | 約10 | パネル9スライス（`panel.png` 24×24 角6）・ボタン3態（`btn_normal/hover/pressed.png` 24×24 角6）・HPバー枠 |')
p()
p('## 状態アイコン（icons/16px）')
p()
p('| 名前 | 用途 |')
p('|---|---|')
const icons: [string, string][] = [
  ['sword', 'ダメージ／攻撃'], ['shield', 'ブロック'], ['heart', 'HP・回復'], ['energy', 'エナジー'], ['draw', 'ドロー'],
  ['growth', '成長'], ['momentum', '勢い'], ['pierce', '貫通'], ['burn', '延焼'], ['ice', '氷壁'], ['aether', '霊気'],
  ['exposed', '急所'], ['weak', '弱体'], ['frail', '虚弱'], ['vulnerable', '脆弱'], ['restrain', '拘束'], ['haze', '霞み'], ['weight', '重り'],
  ['strength', '筋力'], ['armor', '装甲'], ['thorns', 'とげ'], ['regen', '再生'], ['artifact', 'アーティファクト'],
  ['exhaust', '消滅'], ['retain', '保持'], ['set', '伏せ'], ['gold', 'ゴールド'], ['cast', '詠唱数'], ['permanent', '置物'], ['reaction', 'リアクション'],
]
for (const [n, u] of icons) p(`| ${n} | ${u} |`)
p()
p('## 意図アイコン（icons/24px・`intent_<kind>.png`）')
p()
p('attack / defend / buff / rally / heal / hex / destroy-set / destroy-token / steal-gold / flee / mill / rest / hatch')
p()
p('## 敵（enemies/）')
p()
p('| id | 名前 | 区分 | 寸法 |')
p('|---|---|---|---|')
for (const e of enemies) {
  const kind = bossIds.has(e.id) ? '幕ボス' : eliteIds.has(e.id) ? 'エリート' : '通常'
  const size = bossIds.has(e.id) ? '160×160' : eliteIds.has(e.id) ? '128×128' : '96×96'
  p(`| ${e.id} | ${e.name} | ${kind} | ${size} |`)
}
p()
p('## リーダー（leaders/）')
p()
p('| id | 名前 | 色 |')
p('|---|---|---|')
for (const l of allLeaders) p(`| ${l.id} | ${l.name} | ${(l as { colors?: string[] }).colors?.join('+') ?? ''} |`)
p()
p('## レリック（relics/ 32×32）')
p()
p('| id | 名前 | 現在の絵文字 |')
p('|---|---|---|')
for (const r of allRelics) p(`| ${r.id} | ${r.name} | ${r.sprite ?? ''} |`)
p()
p('## カード（絵は後回し）')
p()
p(`カードは ${allCards.length} 種。1枚ずつのイラストは最後（ゲートを越えてから）。それまでは枠＋タイプアイコン＋名前で成立させる。`)
p('タイプ別の枠色: 物理=茶／呪文=紫／リアクション=青緑／置物=金。色（緑青赤白黒）は枠の縁取り。')
console.log(lines.join('\n'))
