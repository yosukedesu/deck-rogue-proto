import { allCards, allEnemies, allRelics, encounterName, getEnemyDef, getLeaderDef } from '../engine/content.ts'

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

/** 戦闘直後の5段階評価 (2026-09-01 ユーザー要望「敵の強さや面白さを5段階で入力→ログに出せばいいデータ」) */
export interface BattleRating {
  readonly strength: number // 敵の強さ 1〜5
  readonly fun: number // 面白さ 1〜5
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
function dataFingerprint(): string {
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
    L.push('| # | 敵 | 結果 | ターン | HP | 強さ | 面白さ |')
    L.push('|---|---|---|---|---|---|---|')
    for (const h of history) {
      L.push(
        `| ${h.battleNo} | ${safeEncounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} | ${h.result === 'won' ? '勝利' : '敗北'} | ${h.turns} | ${h.hpBefore}→${h.hpAfter} | ${h.rating?.strength ?? ''} | ${h.rating?.fun ?? ''} |`,
      )
    }
    L.push('')
    for (const h of history) {
      L.push(
        `### ${h.battleNo}戦目 ${safeEncounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} — ${h.result === 'won' ? '勝利' : '敗北'} / ${h.turns}ターン / HP ${h.hpBefore}→${h.hpAfter} / デッキ${h.deckSize}枚${h.rating ? ` / 評価: 強さ${h.rating.strength} 面白さ${h.rating.fun}` : ''}`,
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
    const url = URL.createObjectURL(new Blob([text], { type: 'text/markdown' }))
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

/** 敵の行動テーブルのプレフィックス → EnemyDef のフィールド名 */
const ENEMY_TABLE: Record<string, 'moves' | 'movesVsSet' | 'movesVsTokens' | 'movesBelowHalf'> = {
  m: 'moves',
  vs: 'movesVsSet',
  tk: 'movesVsTokens',
  bh: 'movesBelowHalf',
}
const ENEMY_TABLE_LABEL: Record<string, string> = { m: '行動', vs: '伏せ反応', tk: '従者反応', bh: '半分以下' }
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

/** 敵 fields キー → 現行値。例: "maxHp"・"m0.min"・"vs1.inflict.amount"・"m2.alt.max" */
function enemyFieldValue(def: ReturnType<typeof getEnemyDef>, key: string): number | undefined {
  const top = (def as unknown as Record<string, unknown>)[key]
  if (typeof top === 'number') return top
  const mm = /^(m|vs|tk|bh)(\d+)\.(?:(alt)\.)?(?:(inflict)\.)?(\w+)$/.exec(key)
  if (!mm) return undefined
  const table = def[ENEMY_TABLE[mm[1]]]
  const mv = table?.[Number(mm[2])] as unknown as Record<string, unknown> | undefined
  if (!mv) return undefined
  const base = mm[3] === 'alt' ? (mv.setAlt as Record<string, unknown> | undefined) : mv
  const holder = mm[4] === 'inflict' ? (base?.inflict as Record<string, unknown> | undefined) : base
  const v = holder?.[mm[5]]
  return typeof v === 'number' ? v : undefined
}

function enemyFieldLabel(def: ReturnType<typeof getEnemyDef>, key: string): string {
  const top = ENEMY_TOP_FIELDS.find(([k]) => k === key)
  if (top) return top[1]
  const mm = /^(m|vs|tk|bh)(\d+)\.(?:(alt)\.)?(?:(inflict)\.)?(\w+)$/.exec(key)
  if (!mm) return key
  const table = def[ENEMY_TABLE[mm[1]]]
  const mv = table?.[Number(mm[2])]
  const name = mv ? `${ENEMY_TABLE_LABEL[mm[1]]}「${mv.id}」` : key
  const FIELD_JA: Record<string, string> = { min: '最小', max: '最大', weight: '重み', hits: 'ヒット数', alsoDefend: '攻防一体ブロック', alsoBuff: '同時強化', amount: '付与量' }
  return `${name}${mm[3] === 'alt' ? '(伏せ時分岐)' : ''}の${mm[4] === 'inflict' ? '状態異常' : ''}${FIELD_JA[mm[5]] ?? mm[5]}`
}

/** レリック fields キー → 現行値。例: "e0.amount"・"bonus.maxHp"・"rule.setDamageReduction" */
function relicFieldValue(def: (typeof allRelics)[number], key: string): number | undefined {
  const eff = /^e(\d+)\.amount$/.exec(key)
  if (eff) return def.effects?.[Number(eff[1])]?.amount
  const bonus = /^bonus\.(\w+)$/.exec(key)
  if (bonus) return (def.bonus as unknown as Record<string, unknown> | undefined)?.[bonus[1]] as number | undefined
  if (key === 'rule.setDamageReduction') return def.combatRule?.setDamageReduction
  return undefined
}

const RELIC_BONUS_LABEL: Record<string, string> = {
  maxHp: '最大HP', victoryHeal: '勝利時回復', rewardChoices: 'ピック候補+',
  campfireRatio: '焚き火回復率', goldPerVictory: '勝利ゴールド+', campfireForge: '鍛える追加回数',
}
function relicFieldLabel(def: (typeof allRelics)[number], key: string): string {
  const eff = /^e(\d+)\.amount$/.exec(key)
  if (eff) {
    const e = def.effects?.[Number(eff[1])]
    return e ? `効果〔${e.trigger}/${e.effect}〕の量` : key
  }
  const bonus = /^bonus\.(\w+)$/.exec(key)
  if (bonus) return RELIC_BONUS_LABEL[bonus[1]] ?? bonus[1]
  if (key === 'rule.setDamageReduction') return '伏せ中の敵攻撃-N'
  return key
}

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

/** fields キー → 現行値 (defから解決)。不明キーは undefined */
function currentFieldValue(def: (typeof allCards)[number], key: string): number | undefined {
  const eff = /^e(\d+)\.(amount|amountMax)$/.exec(key)
  if (eff) {
    const e = def.effects[Number(eff[1])] as unknown as Record<string, unknown> | undefined
    const v = e?.[eff[2]]
    return typeof v === 'number' ? v : undefined
  }
  const cond = /^e(\d+)\.cond\.(\w+)$/.exec(key)
  if (cond) {
    const e = def.effects[Number(cond[1])]
    const v = (e?.condition as unknown as Record<string, unknown> | undefined)?.[cond[2]]
    return typeof v === 'number' ? v : undefined
  }
  if (key === 'exhaustCost' || key === 'discardCost' || key === 'necroCost') return def[key]
  return undefined
}

/** fields キー → 人間向けラベル */
function fieldLabel(def: (typeof allCards)[number], key: string): string {
  const CARD_COST_LABEL: Record<string, string> = {
    exhaustCost: '消滅コスト',
    discardCost: '捨てコスト',
    necroCost: '亡骸コスト',
  }
  if (CARD_COST_LABEL[key] !== undefined) return CARD_COST_LABEL[key]
  const eff = /^e(\d+)\.(amount|amountMax)$/.exec(key)
  if (eff) {
    const e = def.effects[Number(eff[1])]
    const base = e ? `効果${Number(eff[1]) + 1}〔${e.trigger}/${e.effect}${e.target === 'all' ? '(全体)' : ''}〕` : key
    return `${base}の${eff[2] === 'amountMax' ? 'ロール上限' : '量'}`
  }
  const cond = /^e(\d+)\.cond\.(\w+)$/.exec(key)
  if (cond) return `効果${Number(cond[1]) + 1}の条件 ${cond[2]}`
  return key
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
}

/** 汎用マークの節 (変更案+削除案)。敵・レリックで共用 */
function simpleMarkSections(
  domain: string,
  marks: Readonly<Record<string, SimpleMark>> | undefined,
  head: (id: string) => string,
  cur: (id: string, key: string) => number | undefined,
  label: (id: string, key: string) => string,
): string[] {
  const entries = Object.entries(marks ?? {}).filter(([, m]) => !isEmptySimpleMark(m))
  const changes = entries.filter(([, m]) => m.remove !== true)
  const removes = entries.filter(([, m]) => m.remove === true)
  const body = (id: string, m: SimpleMark, withFields: boolean): string[] => {
    const out: string[] = [`- ${head(id)}`]
    if (withFields) {
      for (const [key, val] of Object.entries(m.fields ?? {})) {
        out.push(`  - ${label(id, key)}: ${cur(id, key) ?? '?'} → ${val}`)
      }
    }
    if ((m.change ?? '').trim() !== '') out.push(`  - 補足: ${m.change!.trim()}`)
    return out
  }
  const L: string[] = []
  L.push(`## ${domain}の変更案（${changes.length}件）`)
  for (const [id, m] of changes) L.push(...body(id, m, true))
  L.push('')
  L.push(`## ${domain}の削除案（${removes.length}件）`)
  for (const [id, m] of removes) L.push(...body(id, m, false))
  L.push('')
  return L
}

/** 新規ドラフトの節 (JSONブロック)。敵・レリックで共用 */
function newDefSections(domain: string, defs: readonly { name: string }[] | undefined, toJson: (d: never) => Record<string, unknown>): string[] {
  const list = defs ?? []
  const L: string[] = [`## 新しい${domain}案（${list.length}件）`]
  for (const d of list) {
    L.push(`### ${d.name.trim() !== '' ? d.name : '（無名の下書き）'}`)
    L.push('```json')
    L.push(JSON.stringify(toJson(d as never), null, 2))
    L.push('```')
  }
  L.push('')
  return L
}

/** 調整案の提案書を生成する (純関数)。旧シグネチャの互換ラッパ */
export function buildCardProposals(
  marks: Readonly<Record<string, CardProposalMark>>,
  newCards: string,
  newCardDefs: readonly CardDraft[] = [],
): string {
  return buildProposals({ cardMarks: marks, newCards, newCardDefs })
}

/** 調整案の提案書 (カード・敵・レリック一式) を生成する (純関数)。マークの無いエントリと空文字は無視する */
export function buildProposals(bundle: ProposalBundle): string {
  const marks = bundle.cardMarks
  const newCards = bundle.newCards
  const newCardDefs = bundle.newCardDefs
  const defOf = (id: string) => allCards.find((c) => c.id === id)
  const head = (id: string): string => {
    const d = defOf(id)
    if (!d) return `**${id}**（現行データに存在しない — 統合/リネーム済みの可能性）`
    const COLOR: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
    const RARITY: Record<string, string> = { common: 'コモン', uncommon: 'アンコモン', rare: 'レア' }
    const cost = d.xCost === true ? 'X' : `${d.cost}E`
    return `**${d.name}**（\`${id}\` ${COLOR[d.color] ?? d.color}/${RARITY[d.rarity ?? 'common']}/${cost}/${d.type}） 現行: \`${JSON.stringify(d.effects)}\``
  }
  /** 構造化マーク → 「現行→提案」の差分行 (実データと同じ語彙で並ぶ = そのまま実装に落とせる) */
  const diffLines = (id: string, m: CardProposalMark): string[] => {
    const d = defOf(id)
    const out: string[] = []
    if (m.cost !== undefined) {
      out.push(`  - コスト: ${d ? (d.xCost === true ? 'X' : d.cost) : '?'} → ${m.cost}`)
    }
    if (m.rarity !== undefined) out.push(`  - レアリティ: ${d?.rarity ?? 'common'} → ${m.rarity}`)
    if (m.exhaust !== undefined) {
      out.push(`  - 消滅: ${d?.exhaust === true ? 'あり' : 'なし'} → ${m.exhaust ? 'あり' : 'なし'}`)
    }
    for (const [key, val] of Object.entries(m.fields ?? {})) {
      const cur = d ? currentFieldValue(d, key) : undefined
      out.push(`  - ${d ? fieldLabel(d, key) : key}: ${cur ?? '?'} → ${val}`)
    }
    if ((m.change ?? '').trim() !== '') out.push(`  - 補足: ${m.change!.trim()}`)
    if (m.redef !== undefined) {
      out.push('  - 定義ごと差し替え（下のJSONが提案の完全形）:')
      out.push('```json')
      out.push(JSON.stringify(cardDraftToDefJson(m.redef), null, 2))
      out.push('```')
    }
    return out
  }
  const entries = Object.entries(marks).filter(([, m]) => !isEmptyMark(m))
  const changes = entries.filter(([, m]) => m.remove !== true)
  const removes = entries.filter(([, m]) => m.remove === true)
  const L: string[] = []
  L.push('# カード調整案')
  L.push(`書き出し: ${new Date().toISOString()} / データ指紋: ${dataFingerprint()}`)
  L.push('')
  L.push(`## 変更案（${changes.length}件）`)
  for (const [id, m] of changes) {
    L.push(`- ${head(id)}`)
    L.push(...diffLines(id, m))
  }
  L.push('')
  L.push(`## 削除案（${removes.length}件）`)
  for (const [id, m] of removes) {
    L.push(`- ${head(id)}`)
    L.push(...diffLines(id, { ...m, remove: false, cost: undefined, rarity: undefined, exhaust: undefined, fields: {} }))
  }
  L.push('')
  L.push(`## 新カード案（${newCardDefs.length}件）`)
  for (const d of newCardDefs) {
    L.push(`### ${d.name.trim() !== '' ? d.name : '（無名の下書き）'}`)
    L.push('```json')
    L.push(JSON.stringify(cardDraftToDefJson(d), null, 2))
    L.push('```')
  }
  L.push('')
  // ---- 敵・レリック節 (2026-09-01) ----
  const enemyHead = (id: string): string => {
    const d = allEnemies.find((e) => e.id === id)
    if (!d) return `**${id}**（現行データに存在しない）`
    return `**${d.name}**（\`${id}\` HP${d.maxHp}/${d.archetype}） 現行: \`${JSON.stringify(d)}\``
  }
  L.push(
    ...simpleMarkSections(
      '敵',
      bundle.enemyMarks,
      enemyHead,
      (id, key) => {
        const d = allEnemies.find((e) => e.id === id)
        return d ? enemyFieldValue(d, key) : undefined
      },
      (id, key) => {
        const d = allEnemies.find((e) => e.id === id)
        return d ? enemyFieldLabel(d, key) : key
      },
    ),
  )
  L.push(...newDefSections('敵', bundle.newEnemyDefs, enemyDraftToDefJson as (d: never) => Record<string, unknown>))
  const relicHead = (id: string): string => {
    const d = allRelics.find((r) => r.id === id)
    if (!d) return `**${id}**（現行データに存在しない）`
    return `**${d.name}**（\`${id}\`）「${d.description}」 現行: \`${JSON.stringify({ ...d, name: undefined, description: undefined, sprite: undefined })}\``
  }
  L.push(
    ...simpleMarkSections(
      'レリック',
      bundle.relicMarks,
      relicHead,
      (id, key) => {
        const d = allRelics.find((r) => r.id === id)
        return d ? relicFieldValue(d, key) : undefined
      },
      (id, key) => {
        const d = allRelics.find((r) => r.id === id)
        return d ? relicFieldLabel(d, key) : key
      },
    ),
  )
  L.push(...newDefSections('レリック', bundle.newRelicDefs, relicDraftToDefJson as (d: never) => Record<string, unknown>))
  if (newCards.trim() !== '') {
    L.push('### メモ（自由記述）')
    L.push(newCards.trim())
    L.push('')
  }
  L.push('※ この提案書はレビュー用。実装時は card-power.md の査定 (定価115〜135%帯・色レート・追加コスト算入) と敵の数値基準・cardrules/enemiesの機械テストを通すこと')
  return L.join('\n')
}

/** 調整案一式の書き出し (ダウンロード + クリップボード) */
export function saveProposals(bundle: ProposalBundle): void {
  deliverText(`tuning-proposals-${stampNow()}.md`, buildProposals(bundle))
}

/** 調整案の書き出し (ダウンロード + クリップボード) */
export function saveCardProposals(
  marks: Readonly<Record<string, CardProposalMark>>,
  newCards: string,
  newCardDefs: readonly CardDraft[] = [],
): void {
  deliverText(`card-proposals-${stampNow()}.md`, buildCardProposals(marks, newCards, newCardDefs))
}
