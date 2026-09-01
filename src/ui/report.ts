import { allCards, allEnemies, allLeaders, allRelics, encounterName, getEnemyDef, getLeaderDef } from '../engine/content.ts'

/**
 * 名前解決の安全版 (2026-08-30)。レポートはプレイテストのデータ回収の道具なので、
 * 未知のID (旧バージョンが残した 'unknown'、将来のID変更など) で絶対に例外死させない。
 * localStorage のバックアップには古いデータが残り続けるため、恒久的に必要な防御
 */
function safeEncounterName(id: string): string {
  try {
    return encounterName(id)
  } catch {
    return id
  }
}
function safeEnemyName(id: string): string {
  try {
    return getEnemyDef(id).name
  } catch {
    return id
  }
}
import { effectiveIntent, effectiveCost, isPlayableFromHand } from '../engine/effects.ts'
import type { RunState } from '../engine/run.ts'
import type { CardInstance, GameEvent, GameState } from '../engine/types.ts'
import { cardName, intentText, logLine } from './log.ts'
export { STATUS_LABEL, inflictSuffix, intentText, cardName, logLine } from './log.ts'
export type { LogLine } from './log.ts'

/**
 * 戦闘直後の5段階評価 (2026-09-01 ユーザー要望「敵の強さや面白さを5段階で入力→ログに出せばいいデータ」)。
 * note は同日追補 (ユーザー指摘「フィードバックメモなくない？」) — 点数の理由がその場で残る
 */
export interface BattleRating {
  readonly strength?: number // 敵の強さ 1〜5
  readonly fun?: number // 面白さ 1〜5
  readonly note?: string // ひとことメモ (自由記述)
}

export interface BattleArchive {
  readonly battleNo: number
  readonly enemyId: string
  readonly elite: boolean
  readonly result: 'won' | 'lost'
  readonly turns: number
  readonly hpBefore: number
  readonly hpAfter: number
  readonly deckSize: number
  readonly lines: readonly string[]
  /** 戦闘直後にプレイヤーが入力した評価 (任意。未入力なら undefined) */
  readonly rating?: BattleRating
}

/**
 * プレイ中メモ (2026-09-01 ユーザー要望「気がついたことが揮発せずにいい」)。
 * UI層の観察記録であってゲーム状態ではない — engine には持たせない。
 * レポート書き出しに「## プレイメモ」として同梱される
 */
export interface PlayNote {
  readonly at: string // ISO時刻
  readonly context: string // 記録時の文脈 (幕/行/フェーズ/ターン)
  readonly text: string
}

/** 1戦闘あたりの保管ログ行数の上限 (10戦ぶんでもファイルが読める範囲に収める) */
const ARCHIVE_LINES_CAP = 300

/** 決着した combat を保管形式に変換する */
export function archiveBattle(
  combat: GameState,
  battleNo: number,
  enemyId: string,
  elite: boolean,
  hpBefore: number,
  deckSize: number,
): BattleArchive {
  const all = combat.eventLog.map(reportLine).filter((x): x is string => x !== null)
  return {
    battleNo,
    enemyId,
    elite,
    result: combat.phase === 'won' ? 'won' : 'lost',
    turns: combat.turn,
    hpBefore,
    hpAfter: combat.player.hp,
    deckSize,
    lines: all.length > ARCHIVE_LINES_CAP ? all.slice(-ARCHIVE_LINES_CAP) : all,
  }
}

/** カードデータの指紋。エクスポートを読む側が「同じビルドか」を判定する */
export function dataFingerprint(): string {
  let h = 5381
  for (const c of allCards) {
    const s = `${c.id}:${c.cost}:${c.effects.length}`
    for (let i = 0; i < s.length; i++) h = ((h * 33) ^ s.charCodeAt(i)) >>> 0
  }
  return `cards${allCards.length}/enemies${allEnemies.length}/${h.toString(36)}`
}

const LOG_CAP = 600

const names = (cs: readonly CardInstance[]) => (cs.length ? cs.map((c) => c.def.name).join('、') : '（なし）')


/**
 * ログ行。敵行動フック (Executing/Resolved) は全行動で必ず出る定型ノイズなので描かない。
 * 「窓が開いたか」は ReactionTriggered (発動) と ReactionHeld (温存) の和で厳密に分かる
 */
function reportLine(e: GameEvent): string | null {
  if (e.type === 'EnemyPhaseEnded') return '敵フェーズ終了'
  return logLine(e)?.text ?? null
}

function renderBoard(s: GameState): string[] {
  const p = s.player
  const out: string[] = []
  const st = [
    p.growth ? `成長${p.growth}` : '', p.momentum ? `勢い${p.momentum}` : '',
    p.iceBlock ? `氷壁${p.iceBlock}` : '', p.aether ? `霊気${p.aether}` : '',
    p.nextCardDiscount ? `次コスト-${p.nextCardDiscount}` : '', p.weak ? `弱体${p.weak}` : '',
    p.vulnerable ? `脆弱${p.vulnerable}` : '',
  ].filter(Boolean).join(' ')
  out.push(`自分: HP ${p.hp}/${p.maxHp} ブロック${p.block} エナジー${p.energy}/${p.energyMax} ${st}`)
  out.push(`手札(${p.hand.length}): ${p.hand.map((c) => {
    const cost = effectiveCost(s, c)
    const ok = isPlayableFromHand(c) && cost <= p.energy
    return `${c.def.name}(${cost})${ok ? '' : '✕'}`
  }).join('、') || '（なし）'}`)
  out.push(`伏せ(${p.setCards.length}/${p.setSlots}): ${names(p.setCards)}`)
  out.push(`置物: ${names(p.permanents)}`)
  out.push(`山札${p.drawPile.length} / 捨札${p.discardPile.length} / 消滅${p.exhaustPile.length}`)
  s.enemies.forEach((e, i) => {
    if (e.hp <= 0) { out.push(`敵${i + 1} ${safeEnemyName(e.enemyId)}: 撃破済み`); return }
    const dbg = [e.strength ? `強化${e.strength > 0 ? '+' : ''}${e.strength}` : '', e.block ? `ブロック${e.block}` : '',
      e.burn ? `延焼${e.burn}` : '', e.confusion ? `混乱${e.confusion}` : '', e.exposed ? `急所${e.exposed}` : '']
      .filter(Boolean).join(' ')
    out.push(`敵${i + 1} ${safeEnemyName(e.enemyId)}: HP ${e.hp}/${e.maxHp} ${dbg} → ${intentText(effectiveIntent(s, i))}`)
  })
  if (s.pendingWindow) {
    const w = s.pendingWindow
    const en = s.enemies[w.enemyIndex]
    out.push(`★確認ウィンドウ待ち: 敵${w.enemyIndex + 1} ${safeEnemyName(en.enemyId)} / ${w.stage}窓 / 実値 ${en.intent?.actual}（宣言 ${en.intent?.shownMin}〜${en.intent?.shownMax}）`)
  }
  return out
}

/** スナップショットの eventLog 上限。engine は eventLog を読まないので切り詰めても再開挙動は不変 */
const SNAPSHOT_LOG_CAP = 400

function trimLog(s: GameState): GameState {
  if (s.eventLog.length <= SNAPSHOT_LOG_CAP) return s
  // 先頭の CombatStarted は編成IDの唯一の記録なので必ず残す
  return { ...s, eventLog: [s.eventLog[0], ...s.eventLog.slice(-(SNAPSHOT_LOG_CAP - 1))] }
}

export function buildReport(
  run: RunState | null,
  state: GameState | null,
  history: readonly BattleArchive[] = [],
  note = '',
  playNotes: readonly PlayNote[] = [],
): string {
  const s = run ? run.combat : state
  const L: string[] = []
  L.push(`# プレイ状況レポート`)
  L.push(`書き出し: ${new Date().toISOString()} / データ指紋: ${dataFingerprint()}`)
  if (note) L.push(`メモ: ${note}`)
  L.push('')
  L.push('## いまの状況')
  if (run) {
    const leader = getLeaderDef(run.leaderId)
    L.push(`ラン ${leader.name}（${run.leaderId}） / seed ${run.seed} / mode ${run.mode} / 難易度 ${run.difficulty ?? 3}`)
    L.push(`進行: ${run.phase} / 幕${run.act}/3 行${run.row + 1}/16・${run.battlesWon}勝${run.currentElite ? '（強個体）' : ''} / HP ${run.hp}/${run.maxHp} / 💰${run.gold}G / デッキ${run.deck.length}枚`)
    L.push(
      `マップ: ${run.map
        .map((row, r) => {
          const cells = row
            .map((n, c) => {
              const label = n.encounterId !== null ? safeEncounterName(n.encounterId) : n.type === 'campfire' ? '焚き火' : n.type === 'workshop' ? '工房' : n.type === 'shop' ? 'ショップ' : '?'
              return `${label}${r === run.row && c === run.col ? '●' : ''}`
            })
            .join('/')
          return `行${r}:${cells}`
        })
        .join(' ')}`,
    )
    L.push(`レリック: ${run.relics.length ? run.relics.join('、') : '（なし）'}`)
    L.push(`ピック履歴: ${run.picks.length ? run.picks.map(cardName).join('、') : '（なし）'}`)
    if (run.rewardOptions) L.push(`報酬候補（いま提示中）: ${run.rewardOptions.map(cardName).join(' / ')}`)
    if (run.relicOptions) L.push(`レリック候補（いま提示中）: ${run.relicOptions.join(' / ')}`)
    L.push(`デッキ全体: ${names(run.deck)}`)
  } else if (state) {
    L.push(`単発検証 / mode ${state.reactionMode} / seed ${state.rng.seed}`)
  } else {
    L.push('（戦闘・ランともに未開始）')
  }
  L.push('')
  if (playNotes.length > 0) {
    L.push(`## プレイメモ（${playNotes.length}件・プレイヤーがその場で残した気づき）`)
    for (const n of playNotes) L.push(`- [${n.at.slice(11, 16)} ${n.context}] ${n.text}`)
    L.push('')
  }
  if (history.length > 0) {
    L.push(`## これまでの戦闘（${history.length}戦）`)
    L.push('')
    L.push('| # | 敵 | 結果 | ターン | HP | 強さ | 面白さ | メモ |')
    L.push('|---|---|---|---|---|---|---|---|')
    for (const h of history) {
      L.push(
        `| ${h.battleNo} | ${safeEncounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} | ${h.result === 'won' ? '勝利' : '敗北'} | ${h.turns} | ${h.hpBefore}→${h.hpAfter} | ${h.rating?.strength ?? ''} | ${h.rating?.fun ?? ''} | ${(h.rating?.note ?? '').trim().replace(/\|/g, '｜')} |`,
      )
    }
    L.push('')
    for (const h of history) {
      L.push(
        `### ${h.battleNo}戦目 ${safeEncounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} — ${h.result === 'won' ? '勝利' : '敗北'} / ${h.turns}ターン / HP ${h.hpBefore}→${h.hpAfter} / デッキ${h.deckSize}枚${h.rating ? ` / 評価:${h.rating.strength !== undefined ? ` 強さ${h.rating.strength}` : ''}${h.rating.fun !== undefined ? ` 面白さ${h.rating.fun}` : ''}${(h.rating.note ?? '').trim() !== '' ? `「${h.rating.note!.trim()}」` : ''}` : ''}`,
      )
      L.push(...h.lines)
      L.push('')
    }
  }
  if (s) {
    L.push(`## 盤面（ターン ${s.turn} / ${s.phase}）`)
    L.push(...renderBoard(s))
    L.push('')
    const all = s.eventLog.map(reportLine).filter((x): x is string => x !== null)
    const lines = all.length > LOG_CAP ? all.slice(-LOG_CAP) : all
    L.push(`## この戦闘のログ（${lines.length}行${all.length > lines.length ? ` / 冒頭${all.length - lines.length}行は省略` : ''}）`)
    L.push(...lines)
  } else {
    L.push('## 盤面')
    L.push('（進行中の戦闘なし）')
  }
  L.push('')
  L.push('## 再開用スナップショット（sim/play.ts 互換。`npx tsx src/sim/play.ts show` で開ける）')
  L.push('```json')
  const snapshot = run
    ? { kind: 'run' as const, run: run.combat ? { ...run, combat: trimLog(run.combat) } : run,
        logIndex: run.combat ? trimLog(run.combat).eventLog.length : 0 }
    : { kind: 'battle' as const, battle: state ? trimLog(state) : null,
        logIndex: state ? trimLog(state).eventLog.length : 0 }
  L.push(JSON.stringify(snapshot))
  L.push('```')
  return L.join('\n')
}

/**
 * 書き出し実行。ダウンロードとクリップボードコピーを両方やる
 * (スマホの Safari は a[download] が不安定なため、貼り付けでも渡せるようにする)
 */
function stampNow(): string {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}`
}

/**
 * テキストの配達 (ダウンロード + クリップボード + __lastReport)。
 * 保険 (2026-08-30): ダウンロード/クリップボードが塞がれる環境でも、開発者ツールから
 * copy(window.__lastReport) で必ず取り出せるようにテキストを残す (「このデータは確実に取りたい」)
 */
function deliverText(filename: string, text: string): void {
  ;(window as unknown as { __lastReport?: string }).__lastReport = text
  console.info(
    `[deck-rogue] ${filename} 生成 (${text.length}文字)。ダウンロードに失敗した場合は ` +
      'DevTools コンソールで copy(__lastReport) を実行するとクリップボードに入ります',
  )
  try {
    const url = URL.createObjectURL(new Blob([text], { type: filename.endsWith('.json') ? 'application/json' : 'text/markdown' }))
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.click()
    URL.revokeObjectURL(url)
  } catch {
    // ダウンロード不可の環境 (iframe・一部モバイル)。__lastReport とクリップボードが受け皿
  }
  navigator.clipboard?.writeText(text).catch(() => {})
}

export function saveReport(
  run: RunState | null,
  state: GameState | null,
  history: readonly BattleArchive[] = [],
  playNotes: readonly PlayNote[] = [],
): void {
  deliverText(`play-${stampNow()}.md`, buildReport(run, state, history, '', playNotes))
}

// ---- カード調整サイクル (2026-09-01 ユーザー要望) ----
// 開発者がブラウザの図鑑上で「変更案・削除案・新カード案」をマークし、1枚のmdに書き出して
// AIレビュー→実装へ渡す。ブラウザはリポジトリに書けないので、成果物は提案書であってデータ変更ではない

/**
 * 1枚のカードへのマーク (2026-09-01 構造化。ユーザー指摘「実データと揃える形のほうが楽」)。
 * cost/rarity/exhaust/fields は**現行値と異なる時だけ**保存する = 保存されていれば提案。
 * fields のキーは実データのパス: "e0.amount"（効果0の量）・"e0.hits"（ヒット数）・
 * "e0.cond.minActionValue"（条件値）・"exhaustCost"/"discardCost"/"necroCost"（追加コスト）。
 * change は補足の自由記述 (構造化で表せない意図をここに書く)
 */
export interface CardProposalMark {
  readonly change?: string
  readonly remove?: boolean
  readonly cost?: string // '0'〜'5' | 'X'
  readonly rarity?: string // 'common' | 'uncommon' | 'rare'
  readonly exhaust?: boolean
  readonly fields?: Readonly<Record<string, number>>
  /** 定義ごと差し替え (2026-09-01 「カード1枚が実データとして作れるレベル」)。あればこれが提案の完全形 */
  readonly redef?: CardDraft
}

/** マークが空 (提案なし) か */
export function isEmptyMark(m: CardProposalMark): boolean {
  return (
    (m.change ?? '').trim() === '' &&
    m.remove !== true &&
    m.cost === undefined &&
    m.rarity === undefined &&
    m.exhaust === undefined &&
    m.redef === undefined &&
    Object.keys(m.fields ?? {}).length === 0
  )
}

/**
 * カードビルダーの効果1行 (2026-09-01)。実データ (DeclarativeEffect) と1:1対応。
 * trigger/effect の語彙は現行カードから抽出したもの (新語彙は engine 実装が要るため補足で提案する)
 */
export interface EffectDraft {
  readonly trigger: string
  readonly effect: string
  readonly amount?: number
  readonly amountMax?: number
  readonly target?: 'all'
  readonly pierce?: boolean
  readonly summonId?: string
  /** 条件キー (hpAtOrBelowRatio / minActionValue / blaze 等)。'' = 条件なし */
  readonly condKey?: string
  readonly condValue?: number
}

/** カードビルダーの1枚ぶんの下書き。cardDraftToDefJson で実データ形に落ちる */
export interface CardDraft {
  readonly id?: string
  readonly name: string
  readonly color: string
  readonly cost: number
  readonly xCost?: boolean
  readonly type: string
  readonly rarity: string
  readonly exhaust?: boolean
  readonly exhaustCost?: number
  readonly discardCost?: number
  readonly necroCost?: number
  readonly effects: readonly EffectDraft[]
}

/**
 * 下書き → 実データ形 (cards.*.json の1エントリ)。undefined/未使用フィールドは落とす。
 * color は実ファイルが色別なので配置時に取り除く前提で含める (どのファイルへ入れるかの指示)
 */
export function cardDraftToDefJson(d: CardDraft): Record<string, unknown> {
  const effects = d.effects.map((e) => {
    const out: Record<string, unknown> = { trigger: e.trigger, effect: e.effect }
    if (typeof e.amount === 'number') out.amount = e.amount
    if (typeof e.amountMax === 'number') out.amountMax = e.amountMax
    if (e.target === 'all') out.target = 'all'
    if (e.pierce === true) out.pierce = true
    if ((e.summonId ?? '') !== '') out.summonId = e.summonId
    if ((e.condKey ?? '') !== '') {
      out.condition = e.condKey === 'blaze' ? { blaze: true } : { [e.condKey!]: e.condValue ?? 0 }
    }
    return out
  })
  const j: Record<string, unknown> = {
    id: (d.id ?? '').trim() !== '' ? d.id!.trim() : `${d.color}_TODO_命名`,
    name: d.name.trim() !== '' ? d.name : '（無名の下書き）',
    cost: d.cost,
    type: d.type,
    rarity: d.rarity,
    effects,
  }
  if (d.xCost === true) j.xCost = true
  if (d.exhaust === true) j.exhaust = true
  for (const k of ['exhaustCost', 'discardCost', 'necroCost'] as const) {
    const v = d[k]
    if (typeof v === 'number' && v > 0) j[k] = v
  }
  j.color = d.color
  return j
}

// ---- 敵・レリックの調整サイクル (2026-09-01 ユーザー要望「敵やレリックもカード同様に」) ----

/** 汎用マーク (敵・レリック用)。fields のキーは各ドメインのパス。カードと同じ「現行値と違う値だけ保存」 */
export interface SimpleMark {
  readonly change?: string
  readonly remove?: boolean
  readonly fields?: Readonly<Record<string, number>>
}

export function isEmptySimpleMark(m: SimpleMark): boolean {
  return (m.change ?? '').trim() === '' && m.remove !== true && Object.keys(m.fields ?? {}).length === 0
}

/** 敵のトップレベル数値フィールド (存在する時だけ編集対象になる) */
export const ENEMY_TOP_FIELDS: readonly (readonly [string, string])[] = [
  ['maxHp', 'HP'],
  ['armor', '装甲'],
  ['thorns', 'とげ'],
  ['regen', '再生'],
  ['regenBreak', '再生中断しきい値'],
  ['burnResist', '延焼耐性'],
  ['startingBlock', '開幕ブロック'],
  ['enrage', '激昂量'],
  ['enrageEveryCards', '激昂:枚数ごと'],
  ['enrageEveryDamage', '激昂:被ダメごと'],
  ['angerOnBlock', 'ブロック反応の強化'],
]

/** 敵の新規作成ドラフト。高度な分岐 (setAlt・伏せ反応・フェーズ変化) は補足/メモで提案する */
export interface EnemyMoveDraft {
  readonly id: string
  readonly kind: string
  readonly min?: number
  readonly max?: number
  readonly weight?: number
  readonly hits?: number
  readonly inflictStatus?: string
  readonly inflictAmount?: number
  readonly alsoDefend?: number
  readonly alsoBuff?: number
}
export interface EnemyDraft {
  readonly id?: string
  readonly name: string
  readonly sprite?: string
  readonly archetype: string
  readonly flavor?: string
  readonly maxHp: number
  readonly moves: readonly EnemyMoveDraft[]
  /** ローテーション (move id をカンマ区切り。空=重み抽選) */
  readonly sequence?: string
  readonly armor?: number
  readonly thorns?: number
  readonly regen?: number
  readonly regenBreak?: number
  readonly burnResist?: number
  readonly startingBlock?: number
  readonly enrage?: number
  readonly enrageEveryCards?: number
  readonly enrageEveryDamage?: number
}

export function enemyDraftToDefJson(d: EnemyDraft): Record<string, unknown> {
  const moves = d.moves.map((m) => {
    const out: Record<string, unknown> = { id: m.id, kind: m.kind }
    if (typeof m.min === 'number') out.min = m.min
    if (typeof m.max === 'number') out.max = m.max
    out.weight = m.weight ?? 1
    if (typeof m.hits === 'number' && m.hits > 1) out.hits = m.hits
    if ((m.inflictStatus ?? '') !== '') out.inflict = { status: m.inflictStatus, amount: m.inflictAmount ?? 1 }
    if (typeof m.alsoDefend === 'number') out.alsoDefend = m.alsoDefend
    if (typeof m.alsoBuff === 'number') out.alsoBuff = m.alsoBuff
    return out
  })
  const j: Record<string, unknown> = {
    id: (d.id ?? '').trim() !== '' ? d.id!.trim() : 'enemy_TODO_命名',
    name: d.name.trim() !== '' ? d.name : '（無名の敵）',
    archetype: d.archetype,
    maxHp: d.maxHp,
    moves,
  }
  if ((d.sprite ?? '').trim() !== '') j.sprite = d.sprite
  if ((d.flavor ?? '').trim() !== '') j.flavor = d.flavor
  const seq = (d.sequence ?? '').split(',').map((x) => x.trim()).filter((x) => x !== '')
  if (seq.length > 0) j.sequence = seq
  for (const [k] of ENEMY_TOP_FIELDS) {
    if (k === 'maxHp') continue
    const v = (d as unknown as Record<string, unknown>)[k]
    if (typeof v === 'number' && v > 0) j[k] = v
  }
  return j
}

/** レリックの新規作成ドラフト。effects はカードと同じ EffectDraft を使い回す */
export interface RelicDraft {
  readonly id?: string
  readonly name: string
  readonly sprite?: string
  readonly description: string
  readonly effects: readonly EffectDraft[]
  readonly maxHp?: number
  readonly victoryHeal?: number
  readonly rewardChoices?: number
  readonly campfireRatio?: number
  readonly goldPerVictory?: number
  readonly campfireForge?: number
  readonly setDamageReduction?: number
  readonly revealIntents?: boolean
}

export function relicDraftToDefJson(d: RelicDraft): Record<string, unknown> {
  const j: Record<string, unknown> = {
    id: (d.id ?? '').trim() !== '' ? d.id!.trim() : 'relic_TODO_命名',
    name: d.name.trim() !== '' ? d.name : '（無名のレリック）',
    sprite: (d.sprite ?? '').trim() !== '' ? d.sprite : '🔮',
    description: d.description,
  }
  if (d.effects.length > 0) {
    j.effects = (cardDraftToDefJson({ name: '', color: 'green', cost: 0, type: 'spell', rarity: 'common', effects: d.effects }) as { effects: unknown }).effects
  }
  const bonus: Record<string, number> = {}
  for (const k of ['maxHp', 'victoryHeal', 'rewardChoices', 'campfireRatio', 'goldPerVictory', 'campfireForge'] as const) {
    const v = d[k]
    if (typeof v === 'number' && v > 0) bonus[k] = v
  }
  if (Object.keys(bonus).length > 0) j.bonus = bonus
  const rule: Record<string, unknown> = {}
  if (typeof d.setDamageReduction === 'number' && d.setDamageReduction > 0) rule.setDamageReduction = d.setDamageReduction
  if (d.revealIntents === true) rule.revealIntents = true
  if (Object.keys(rule).length > 0) j.combatRule = rule
  return j
}

/** リーダーの数値フィールド (2026-09-01 ユーザー要望「リーダーも図鑑編集」) */
export const LEADER_TOP_FIELDS: readonly (readonly [string, string])[] = [
  ['maxHp', '最大HP'],
  ['drawPerTurn', '毎ターンドロー'],
  ['energyMax', 'エナジー上限'],
  ['rewardChoices', 'ピック候補数'],
  ['setSlots', '伏せ枠'],
]

/** リーダーの新規作成ドラフト。パッシブはカードと同じ EffectDraft */
export interface LeaderDraft {
  readonly id?: string
  readonly name: string
  readonly sprite?: string
  readonly colors: readonly string[]
  readonly maxHp: number
  readonly drawPerTurn: number
  readonly energyMax: number
  readonly rewardChoices: number
  readonly setSlots?: number
  readonly runDeckId?: string
  readonly description: string
  readonly passive: readonly EffectDraft[]
}

export function leaderDraftToDefJson(d: LeaderDraft): Record<string, unknown> {
  const j: Record<string, unknown> = {
    id: (d.id ?? '').trim() !== '' ? d.id!.trim() : 'leader_TODO_命名',
    name: d.name.trim() !== '' ? d.name : '（無名のリーダー）',
    colors: d.colors,
    maxHp: d.maxHp,
    drawPerTurn: d.drawPerTurn,
    energyMax: d.energyMax,
    rewardChoices: d.rewardChoices,
    runDeckId: (d.runDeckId ?? '').trim() !== '' ? d.runDeckId : 'run_basic',
    sprite: (d.sprite ?? '').trim() !== '' ? d.sprite : '🎭',
    description: d.description,
    passive: (cardDraftToDefJson({ name: '', color: 'green', cost: 0, type: 'spell', rarity: 'common', effects: d.passive }) as { effects: unknown }).effects,
  }
  if (typeof d.setSlots === 'number' && d.setSlots > 1) j.setSlots = d.setSlots
  return j
}

/** 調整案の一式 (カード・敵・レリック)。図鑑の🛠調整モードの下書きがこの形で書き出される */
export interface ProposalBundle {
  readonly cardMarks: Readonly<Record<string, CardProposalMark>>
  readonly newCards: string
  readonly newCardDefs: readonly CardDraft[]
  readonly enemyMarks?: Readonly<Record<string, SimpleMark>>
  readonly newEnemyDefs?: readonly EnemyDraft[]
  readonly relicMarks?: Readonly<Record<string, SimpleMark>>
  readonly newRelicDefs?: readonly RelicDraft[]
  readonly leaderMarks?: Readonly<Record<string, SimpleMark>>
  readonly newLeaderDefs?: readonly LeaderDraft[]
}

/**
 * 調整案の書き出しは生JSON (2026-09-01 ユーザー裁定「mdじゃなくて生のjsonのほうが良い」)。
 * - fields のキーは実データのパスそのまま = AIレビュー側で逆引き不要・適用をスクリプト化できる
 * - current にマーク時点の現行定義を丸ごと同梱 = 書き出しとレビューの間のデータ変化を検出できる
 * - 自由記述 (補足・メモ) はJSON文字列で持つ (mdはプレイレポート側に残る = 物語はmd・データはjson)
 */
function flatMarks<M extends SimpleMark>(
  marks: Readonly<Record<string, M>> | undefined,
  isEmpty: (m: M) => boolean,
  currentOf: (id: string) => unknown,
  proposalOf: (m: M) => Record<string, unknown>,
): { changes: Record<string, unknown>[]; removals: Record<string, unknown>[] } {
  const entries = Object.entries(marks ?? {}).filter(([, m]) => !isEmpty(m))
  const note = (m: M) => ((m.change ?? '').trim() !== '' ? m.change!.trim() : undefined)
  return {
    changes: entries
      .filter(([, m]) => m.remove !== true)
      .map(([id, m]) => ({ id, current: currentOf(id) ?? '現行データに存在しない (統合/リネーム済みの可能性)', proposal: proposalOf(m), note: note(m) })),
    removals: entries
      .filter(([, m]) => m.remove === true)
      .map(([id, m]) => ({ id, current: currentOf(id) ?? '現行データに存在しない', note: note(m) })),
  }
}

/** 調整案一式 → 生JSON (純関数)。マークの無いエントリと空文字は無視する */
export function buildProposals(bundle: ProposalBundle): string {
  const cardProposal = (m: CardProposalMark): Record<string, unknown> => {
    const out: Record<string, unknown> = {}
    if (m.cost !== undefined) out.cost = m.cost
    if (m.rarity !== undefined) out.rarity = m.rarity
    if (m.exhaust !== undefined) out.exhaust = m.exhaust
    if (Object.keys(m.fields ?? {}).length > 0) out.fields = m.fields
    if (m.redef !== undefined) out.redef = cardDraftToDefJson(m.redef)
    return out
  }
  const simpleProposal = (m: SimpleMark): Record<string, unknown> =>
    Object.keys(m.fields ?? {}).length > 0 ? { fields: m.fields } : {}
  const doc = {
    kind: 'deck-rogue-tuning-proposals',
    version: 1,
    exportedAt: new Date().toISOString(),
    fingerprint: dataFingerprint(),
    howToRead:
      'fieldsのキーは実データのパス: e0.amount=effects[0].amount / e0.cond.X=effects[0].condition.X / ' +
      'm0=moves[0]・vs=movesVsSet・tk=movesVsTokens・bh=movesBelowHalf・.alt.=setAlt / p0=passive[0] / ' +
      'bonus.*=レリックB型 / rule.*=combatRule。current はマーク時点の現行定義。new は配置先ファイルへそのまま貼れる形 ' +
      '(カードの color は配置先ファイルの指示で、実ファイルでは取り除く)。実装時は card-power.md の査定と機械テストを通すこと',
    cards: {
      ...flatMarks(bundle.cardMarks, isEmptyMark, (id) => allCards.find((c) => c.id === id), cardProposal),
      new: bundle.newCardDefs.map(cardDraftToDefJson),
    },
    enemies: {
      ...flatMarks(bundle.enemyMarks, isEmptySimpleMark, (id) => allEnemies.find((e) => e.id === id), simpleProposal),
      new: (bundle.newEnemyDefs ?? []).map(enemyDraftToDefJson),
    },
    relics: {
      ...flatMarks(bundle.relicMarks, isEmptySimpleMark, (id) => allRelics.find((r) => r.id === id), simpleProposal),
      new: (bundle.newRelicDefs ?? []).map(relicDraftToDefJson),
    },
    leaders: {
      ...flatMarks(bundle.leaderMarks, isEmptySimpleMark, (id) => allLeaders.find((l) => l.id === id), simpleProposal),
      new: (bundle.newLeaderDefs ?? []).map(leaderDraftToDefJson),
    },
    memo: bundle.newCards.trim() !== '' ? bundle.newCards.trim() : undefined,
    // 読み戻し (ラウンドトリップ) 用の下書き原本。図鑑の「調整案を読み込む」がこれを読む
    sourceDraft: bundle,
  }
  return JSON.stringify(doc, null, 2)
}

// ---- 調整案のライブ適用 (2026-09-01)。マークを現行定義へ当てて差し替え用の定義列を作る ----

function deepClone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v)) as T
}

/** カードマーク → 適用済み定義。redef があればそれが完全形 (他のマークより優先) */
export function applyCardMark(def: (typeof allCards)[number], m: CardProposalMark): (typeof allCards)[number] {
  if (m.redef !== undefined) {
    const j = cardDraftToDefJson(m.redef) as unknown as (typeof allCards)[number] & { id: string }
    return { ...j, id: def.id } // 差し替えは同じidを維持する
  }
  const d = deepClone(def) as unknown as Record<string, unknown>
  if (m.cost !== undefined) {
    if (m.cost === 'X') {
      d.xCost = true
      d.cost = 1
    } else {
      d.cost = Number(m.cost)
      delete d.xCost
    }
  }
  if (m.rarity !== undefined) d.rarity = m.rarity
  if (m.exhaust !== undefined) {
    if (m.exhaust) d.exhaust = true
    else delete d.exhaust
  }
  const effects = d.effects as Record<string, unknown>[]
  for (const [key, val] of Object.entries(m.fields ?? {})) {
    const eff = /^e(\d+)\.(amount|amountMax)$/.exec(key)
    const cond = /^e(\d+)\.cond\.(\w+)$/.exec(key)
    if (eff && effects[Number(eff[1])]) effects[Number(eff[1])][eff[2]] = val
    else if (cond && effects[Number(cond[1])]) {
      const e = effects[Number(cond[1])]
      e.condition = { ...(e.condition as Record<string, unknown> | undefined), [cond[2]]: val }
    } else d[key] = val
  }
  return d as unknown as (typeof allCards)[number]
}

/** 敵マーク → 適用済み定義。fields のパス (m0.min / vs1.alt.max / bh0.inflict.amount / maxHp 等) を書き戻す */
export function applyEnemyMark(def: ReturnType<typeof getEnemyDef>, m: SimpleMark): ReturnType<typeof getEnemyDef> {
  const d = deepClone(def) as unknown as Record<string, unknown>
  const TABLE: Record<string, string> = { m: 'moves', vs: 'movesVsSet', tk: 'movesVsTokens', bh: 'movesBelowHalf' }
  for (const [key, val] of Object.entries(m.fields ?? {})) {
    const mm = /^(m|vs|tk|bh)(\d+)\.(?:(alt)\.)?(?:(inflict)\.)?(\w+)$/.exec(key)
    if (!mm) {
      d[key] = val
      continue
    }
    const table = d[TABLE[mm[1]]] as Record<string, unknown>[] | undefined
    const mv = table?.[Number(mm[2])]
    if (!mv) continue
    const base = mm[3] === 'alt' ? (mv.setAlt as Record<string, unknown> | undefined) : mv
    if (!base) continue
    const holder = mm[4] === 'inflict' ? (base.inflict as Record<string, unknown> | undefined) : base
    if (!holder) continue
    holder[mm[5]] = val
  }
  return d as unknown as ReturnType<typeof getEnemyDef>
}

/** レリックマーク → 適用済み定義 */
export function applyRelicMark(def: (typeof allRelics)[number], m: SimpleMark): (typeof allRelics)[number] {
  const d = deepClone(def) as unknown as Record<string, unknown>
  for (const [key, val] of Object.entries(m.fields ?? {})) {
    const eff = /^e(\d+)\.amount$/.exec(key)
    const bonus = /^bonus\.(\w+)$/.exec(key)
    if (eff) {
      const effects = d.effects as Record<string, unknown>[] | undefined
      if (effects?.[Number(eff[1])]) effects[Number(eff[1])].amount = val
    } else if (bonus) {
      d.bonus = { ...(d.bonus as Record<string, unknown> | undefined), [bonus[1]]: val }
    } else if (key === 'rule.setDamageReduction') {
      d.combatRule = { ...(d.combatRule as Record<string, unknown> | undefined), setDamageReduction: val }
    } else d[key] = val
  }
  return d as unknown as (typeof allRelics)[number]
}

/** リーダーマーク → 適用済み定義 */
export function applyLeaderMark(def: (typeof allLeaders)[number], m: SimpleMark): (typeof allLeaders)[number] {
  const d = deepClone(def) as unknown as Record<string, unknown>
  for (const [key, val] of Object.entries(m.fields ?? {})) {
    const pv = /^p(\d+)\.amount$/.exec(key)
    if (pv) {
      const passive = d.passive as Record<string, unknown>[]
      if (passive[Number(pv[1])]) passive[Number(pv[1])].amount = val
    } else d[key] = val
  }
  return d as unknown as (typeof allLeaders)[number]
}

/**
 * バンドル全体 → 差し替え/追記用の定義列 (content.applyDebugOverrides に渡す形)。
 * 削除案は適用しない (スターター・理想形が壊れるため = 提案としてのみ渡る)。
 * 新規defのidがプレースホルダのままなら debug_ 連番を振る
 */
export function buildOverrideDefs(bundle: ProposalBundle): {
  cards: (typeof allCards)[number][]
  enemies: ReturnType<typeof getEnemyDef>[]
  relics: (typeof allRelics)[number][]
  leaders: (typeof allLeaders)[number][]
} {
  let seq = 0
  const fixId = (j: Record<string, unknown>, prefix: string): Record<string, unknown> => {
    const id = String(j.id ?? '')
    if (id === '' || id.includes('TODO')) j.id = `debug_${prefix}_${++seq}`
    return j
  }
  const cards: (typeof allCards)[number][] = []
  for (const [id, m] of Object.entries(bundle.cardMarks)) {
    if (isEmptyMark(m) || m.remove === true) continue
    const def = allCards.find((c) => c.id === id)
    if (def) cards.push(applyCardMark(def, m))
  }
  for (const nd of bundle.newCardDefs) cards.push(fixId(cardDraftToDefJson(nd), 'card') as unknown as (typeof allCards)[number])
  const enemies: ReturnType<typeof getEnemyDef>[] = []
  for (const [id, m] of Object.entries(bundle.enemyMarks ?? {})) {
    if (isEmptySimpleMark(m) || m.remove === true) continue
    const def = allEnemies.find((e) => e.id === id)
    if (def) enemies.push(applyEnemyMark(def, m))
  }
  for (const nd of bundle.newEnemyDefs ?? []) enemies.push(fixId(enemyDraftToDefJson(nd), 'enemy') as unknown as ReturnType<typeof getEnemyDef>)
  const relics: (typeof allRelics)[number][] = []
  for (const [id, m] of Object.entries(bundle.relicMarks ?? {})) {
    if (isEmptySimpleMark(m) || m.remove === true) continue
    const def = allRelics.find((r) => r.id === id)
    if (def) relics.push(applyRelicMark(def, m))
  }
  for (const nd of bundle.newRelicDefs ?? []) relics.push(fixId(relicDraftToDefJson(nd), 'relic') as unknown as (typeof allRelics)[number])
  const leaders: (typeof allLeaders)[number][] = []
  for (const [id, m] of Object.entries(bundle.leaderMarks ?? {})) {
    if (isEmptySimpleMark(m) || m.remove === true) continue
    const def = allLeaders.find((l) => l.id === id)
    if (def) leaders.push(applyLeaderMark(def, m))
  }
  for (const nd of bundle.newLeaderDefs ?? []) leaders.push(fixId(leaderDraftToDefJson(nd), 'leader') as unknown as (typeof allLeaders)[number])
  return { cards, enemies, relics, leaders }
}

// ---- セーブ機能 (2026-09-01 ユーザー裁定で解禁範囲を拡張: 続きから+ファイル書き出し/読み込み) ----

/**
 * ランのセーブファイル (sim/play.ts の SaveFile 互換 = CLIでもそのまま開ける)。
 * history/playNotes/fingerprint はUI側の拡張フィールド (CLIは無視する)
 */
export interface RunSaveFile {
  readonly kind: 'run'
  readonly run: RunState
  readonly logIndex: number
  readonly fingerprint?: string
  readonly history?: readonly BattleArchive[]
  readonly playNotes?: readonly PlayNote[]
}

/** ランのセーブを直列化する (純関数)。戦闘ログはスナップショット上限で切り詰める (engineは読まない) */
export function buildRunSaveFile(
  run: RunState,
  history: readonly BattleArchive[] = [],
  playNotes: readonly PlayNote[] = [],
): string {
  const r = run.combat ? { ...run, combat: trimLog(run.combat) } : run
  const sf: RunSaveFile = {
    kind: 'run',
    run: r,
    logIndex: r.combat?.eventLog.length ?? 0,
    fingerprint: dataFingerprint(),
    history,
    playNotes,
  }
  return JSON.stringify(sf)
}

/** セーブの書き出し (ダウンロード + クリップボード)。ファイルは CLI (sim/play.ts) でも開ける */
export function saveRunFile(
  run: RunState,
  history: readonly BattleArchive[] = [],
  playNotes: readonly PlayNote[] = [],
): void {
  deliverText(`save-${stampNow()}.json`, buildRunSaveFile(run, history, playNotes))
}

/** 調整案一式の書き出し (ダウンロード + クリップボード) */
export function saveProposals(bundle: ProposalBundle): void {
  deliverText(`tuning-proposals-${stampNow()}.json`, buildProposals(bundle))
}
