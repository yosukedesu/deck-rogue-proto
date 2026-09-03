// 敵のギミック特性の言語化 (2026-09-02 表示網羅の純モジュール)。
// CLI (sim/play.ts) はこの関数でチップ相当のタグを出す。UI (App.tsx) は JSX チップだが、
// 「どのギミックにどの用語 (KEYWORD_HELP) を使うか」は GIMMICK_KEYWORDS で共有し、
// display-coverage.test が「新しい EnemyDef キーに用語解説とタグの両方があること」を機械固定する。
import { getEnemyDef } from './content.ts'
import { turnsUntilHatch } from './summary.ts'
import type { EnemyDef, GameState } from './types.ts'

/** EnemyDef のギミック系キー (enemy-conventions.test のホワイトリストと共有) */
export const ENEMY_GIMMICK_KEYS = [
  'enrage', 'enrageEveryCards', 'enrageEveryDamage', 'regen', 'regenBreak', 'burnResist',
  'thorns', 'armor', 'startingBlock', 'angerOnBlock', 'guardian', 'bondStrength',
  'opener', 'phaseAfterUses', 'splitInto', 'hatchInto', 'mournStrength', 'aura',
  'turnArmor', 'artifact', 'wakeOnDamage',
] as const
export type EnemyGimmickKey = (typeof ENEMY_GIMMICK_KEYS)[number]

/**
 * ギミックキー → KEYWORD_HELP の用語。null = 常時表示の対象外 (行動ローテの器で、意図表示側で見える)。
 * ここに載せた用語は KEYWORD_HELP に必ず存在しなければならない (display-coverage.test)
 */
export const GIMMICK_KEYWORDS: Record<EnemyGimmickKey, string | null> = {
  enrage: '激昂',
  enrageEveryCards: '激昂',
  enrageEveryDamage: '激昂',
  regen: '再生',
  regenBreak: '再生',
  burnResist: '延焼耐性',
  thorns: 'とげ',
  armor: '装甲',
  startingBlock: '開幕ブロック',
  angerOnBlock: 'ブロック反応',
  guardian: '庇う',
  bondStrength: '連携',
  opener: null,
  phaseAfterUses: null,
  splitInto: '分裂',
  hatchInto: '孵化',
  mournStrength: '弔い',
  aura: '重圧',
  turnArmor: 'ターン装甲',
  artifact: 'アーティファクト',
  wakeOnDamage: '眠り',
}

/** 定義だけで決まる特性タグ (状態非依存) */
export function enemyTraitTagsOfDef(def: EnemyDef): string[] {
  const tags: string[] = []
  if (def.burnResist) tags.push(`延焼耐性${def.burnResist}`)
  if (def.thorns) tags.push(`とげ${def.thorns}(攻撃ヒットごとに反射。倒せば無傷)`)
  if (def.armor) tags.push(`装甲${def.armor}(1ヒットの被ダメは${def.armor}以下。成長・勢い・急所を乗せた後で頭打ち=急所は装甲持ちに乗らない。延焼は無視)`)
  if (def.startingBlock) tags.push(`開幕ブロック${def.startingBlock}`)
  if (def.splitInto) {
    const child = getEnemyDef(def.splitInto.enemyId)
    tags.push(
      def.splitInto.count === 1
        ? `残機(倒すと${child.name}HP${child.maxHp}で再起動。素の値=実戦では親の倍率を継承)`
        : `分裂(倒すと${child.name}×${def.splitInto.count}に${def.splitInto.stunned ? '。出現ターンは動かない' : ''})`,
    )
  }
  if (def.guardian) tags.push('庇う(生存中は単体対象がこの敵に向かう。全体・延焼は素通し)')
  if (def.bondStrength) tags.push(`連携+${def.bondStrength}(仲間が生きている間、攻撃+${def.bondStrength})`)
  if (def.aura) tags.push(`重圧(生存中、${def.aura.cardType ?? '全'}カードのコスト+${def.aura.costUp})`)
  if (def.mournStrength) tags.push(`弔い+${def.mournStrength}(仲間が倒れるたび筋力+)`)
  if (def.angerOnBlock) tags.push(`ブロック反応${def.angerOnBlock}(あなたがカードでブロック・氷壁を得るたび筋力+${def.angerOnBlock}。パッシブ・レリックの自動分は除く)`)
  if (def.enrage) tags.push(def.enrageEveryCards ? `激昂+${def.enrage}/${def.enrageEveryCards}枚プレイ${def.enrageEveryDamage !== undefined ? `・+${def.enrage}/被ダメ${def.enrageEveryDamage}` : ''}` : `激昂+${def.enrage}/T`)
  const growing = def.moves.filter((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined)
  if (growing.length > 0) tags.push(`育つ技(${growing.map((m) => `${m.id}:使うたび${m.growPerUse ? `+${m.growPerUse}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse}` : ''}`).join('/')})`)
  return tags
}

/** 戦闘状態込みの特性タグ (残量・カウントダウン・庇われ中など) */
export function enemyTraitTags(s: GameState, i: number): string[] {
  const e = s.enemies[i]
  const def = getEnemyDef(e.enemyId)
  const tags: string[] = []
  if (def.burnResist) tags.push(`延焼耐性${def.burnResist}`)
  if (def.thorns) tags.push(`とげ${def.thorns}(攻撃ヒットごとに反射。倒せば無傷)`)
  if (def.armor) tags.push(`装甲${def.armor}(1ヒットの被ダメは${def.armor}以下。成長・勢い・急所を乗せた後で頭打ち=急所は装甲持ちに乗らない。延焼は無視)`)
  if (def.startingBlock) tags.push(`開幕ブロック${def.startingBlock}`)
  if (def.splitInto) {
    const child = getEnemyDef(def.splitInto.enemyId)
    // 予告HPは親のHP倍率 (幕・ボス係数・難易度) を継承した実値で出す (2026-09-03 Opusラン K:
    // 「二の相HP55」の予告に対し実際は132で出ていた)
    const ratio = def.maxHp > 0 ? e.maxHp / def.maxHp : 1
    const childHp = Math.max(1, Math.round(child.maxHp * ratio))
    tags.push(
      def.splitInto.count === 1
        ? `残機(倒すと${child.name}HP${childHp}で再起動)`
        : `分裂(倒すと${child.name}×${def.splitInto.count}に${def.splitInto.stunned ? '。出現ターンは動かない' : ''})`,
    )
  }
  if (def.guardian) tags.push('庇う(生存中は単体対象がこの敵に向かう。全体・延焼は素通し)')
  if (!def.guardian && s.enemies.some((g) => g.hp > 0 && getEnemyDef(g.enemyId).guardian === true)) {
    tags.push('⛔庇われ中(単体対象はこの敵を選べない)')
  }
  if (def.bondStrength) tags.push(`連携+${def.bondStrength}(仲間が生きている間、攻撃+${def.bondStrength})`)
  if (def.aura) tags.push(`重圧(生存中、${def.aura.cardType ?? '全'}カードのコスト+${def.aura.costUp})`)
  if (def.hatchInto) {
    const t = turnsUntilHatch(s, i)
    tags.push(`孵化(${t === 0 ? 'このフェーズで孵化!' : t !== null ? `あと${t}手` : ''}→${getEnemyDef(def.hatchInto.enemyId).name}。打ち消しで遅延可・行動値条件の打ち消しは反応しない)`)
  }
  if (def.mournStrength) tags.push(`弔い+${def.mournStrength}(仲間が倒れるたび筋力+)`)
  if (def.turnArmor) {
    const remaining = Math.max(0, def.turnArmor - (e.damageThisTurn ?? 0))
    // 「今ターン倒せない」の予告 (2026-09-02 Opusラン: HP52>残り45 を暗算させて死因に直結。フェアネス=予告してから殺す)
    const unkillable = e.hp > 0 && e.hp > remaining ? ` ⚠今ターン倒せない(HP${e.hp}>残り${remaining})` : ''
    tags.push(`ターン装甲${def.turnArmor}(1ターンのHP損失は${def.turnArmor}以下。残り${remaining}。延焼は無視)${unkillable}`)
  }
  if ((e.artifact ?? 0) > 0) tags.push(`アーティファクト${e.artifact}(デバフ付与を${e.artifact}回弾く。延焼は通る)`)
  if (def.wakeOnDamage && !e.woken && e.patternIndex < def.wakeOnDamage.resumeAt) {
    tags.push(`眠り(累計${def.wakeOnDamage.damage}ダメで目覚める。現在${e.damageTakenTotal ?? 0})`)
  }
  const growing = def.moves.filter((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined)
  if (growing.length > 0) {
    tags.push(`育つ技(${growing.map((m) => `${m.id}:使うたび${m.growPerUse ? `+${m.growPerUse}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse}` : ''}・現在${e.moveGrowth?.[m.id] ?? 0}回`).join('/')})`)
  }
  if (def.angerOnBlock) tags.push(`ブロック反応${def.angerOnBlock}(あなたがカードでブロック・氷壁を得るたび筋力+${def.angerOnBlock}。パッシブ・レリックの自動分は除く)`)
  if (def.regen && e.hp > e.maxHp * 0.5) tags.push(`再生${def.regen}${def.regenBreak ? `(このターン${def.regenBreak}以上削ると停止)` : ''}`)
  if (def.enrage) tags.push(def.enrageEveryCards ? `激昂+${def.enrage}/${def.enrageEveryCards}枚プレイ${def.enrageEveryDamage !== undefined ? `・+${def.enrage}/被ダメ${def.enrageEveryDamage}` : ''}` : `激昂+${def.enrage}/T`)
  return tags
}
