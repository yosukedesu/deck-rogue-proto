// 敵のギミック特性の言語化 (2026-09-02 表示網羅の純モジュール)。
// CLI (sim/play.ts) はこの関数でチップ相当のタグを出す。UI (App.tsx) は JSX チップだが、
// 「どのギミックにどの用語 (KEYWORD_HELP) を使うか」は GIMMICK_KEYWORDS で共有し、
// display-coverage.test が「新しい EnemyDef キーに用語解説とタグの両方があること」を機械固定する。
import { getEnemyDef } from './content.ts'
import { moveLabel, sleepingInterrupt } from './enemyGraph.ts'
import { interruptPreviews, splitChildHp, turnsUntilHatch } from './summary.ts'
import type { EnemyDef, EnemyState, GameState } from './types.ts'

/** EnemyDef のギミック系キー (enemy-conventions.test のホワイトリストと共有) */
export const ENEMY_GIMMICK_KEYS = [
  'enrage', 'enrageEveryCards', 'enrageEveryDamage', 'regen', 'regenBreak', 'burnResist',
  'thorns', 'armor', 'startingBlock', 'angerOnBlock', 'guardian', 'bondStrength',
  'interrupts', 'splitInto', 'hatchInto', 'mournStrength', 'aura',
  'turnArmor', 'artifact', 'burrow', 'nemesis', 'imbalanced', 'slippery',
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
  interrupts: '眠り', // 割り込み (HP半分の豹変・被弾覚醒・単独時の転職)。被弾覚醒だけ用語解説があり、他は意図の予告で見える
  splitInto: '分裂',
  hatchInto: '孵化',
  mournStrength: '弔い',
  aura: '重圧',
  turnArmor: 'ターン装甲',
  artifact: 'アーティファクト',
  burrow: '潜伏',
  nemesis: '因縁',
  imbalanced: 'バランス崩し',
  slippery: '朧',
}

/**
 * 鎮めの錘 (ギア) で割り込みを止めた敵の一文 (CLI の行動欄・Web のチップが共用)。まだ起きていない割り込みが無ければ null
 * (2026-09-24 Opus ひなた E5: 止めた後も「HPが半分以下になると…」の予告が最後まで残っていた。予告は interruptPreviews が空を返す)
 */
export function interruptBlockedNote(def: EnemyDef, e: EnemyState): string | null {
  if (e.interruptBlocked !== true) return null
  const fired = e.firedInterrupts ?? []
  const kinds = new Set((def.interrupts ?? []).filter((_, k) => !fired.includes(k)).map((it) => it.on))
  if (kinds.size === 0) return null
  const when = [
    kinds.has('damageTaken') ? 'ダメージを受けても' : '',
    kinds.has('hpBelowHalf') ? 'HPが半分を切っても' : '',
    kinds.has('allyDied') || kinds.has('alone') ? '仲間が倒れても' : '',
  ].filter(Boolean)
  return `鎮めの錘: この戦闘中は${when.join('・')}行動が変わらない`
}

/** 定義だけで決まる特性タグ (状態非依存) */
export function enemyTraitTagsOfDef(def: EnemyDef): string[] {
  const tags: string[] = []
  if (def.burnResist) tags.push(`延焼耐性${def.burnResist}`)
  if (def.thorns) tags.push(`とげ${def.thorns}(カードの攻撃ヒットごとに反射。人形の攻撃には反射しない。倒せば無傷)`)
  if (def.armor) tags.push(`装甲${def.armor}(1ヒットの被ダメは${def.armor}以下。成長・勢い・急所を乗せた後で頭打ち=上限を超える分の急所・成長は切り捨て。延焼は無視)`)
  if (def.startingBlock) tags.push(`開幕ブロック${def.startingBlock}`)
  if (def.burrow) tags.push(`潜伏(殻${def.burrow.block}が尽きるまでHPにダメージが通らない。超過は捨てる・貫通も殻に吸われる・粉砕は殻を割る。割れると次の行動が噛みつきに変わる=割ったターンのうちに倒せば来ない)`)
  if (def.nemesis) tags.push('因縁(奇数ターンは無形=1ヒットのHP損失が1固定。偶数ターンに実体化。延焼は通る)')
  if (def.imbalanced) tags.push('バランス崩し(攻撃を完全に防ぐ=HP損失0にすると体勢を崩し、次の行動が隙になる。軽減リアクション・ブロックの報酬)')
  if (def.slippery) tags.push(`朧${def.slippery}(霧で姿がぼやけ、HPに届く当たりの最初の${def.slippery}回は1ダメージになる。人形・罠の当たりも数える。ブロックで止まった当たりと延焼は数えない)`)
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
  if (def.aura) tags.push(`重圧(生存中、${def.aura.attacksOnly === true ? 'ダメージを与える' : (def.aura.cardType ?? '全')}カードのコスト+${def.aura.costUp})`)
  if (def.mournStrength) tags.push(`弔い+${def.mournStrength}(仲間が倒れるたび筋力+)`)
  if (def.angerOnBlock) tags.push(`ブロック反応${def.angerOnBlock}(あなたがカードでブロック・氷壁を得るたび筋力+${def.angerOnBlock}。パッシブ・レリックの自動分は除く)`)
  if (def.enrage) tags.push(def.enrageEveryCards ? `激昂+${def.enrage}/${def.enrageEveryCards}枚プレイ${def.enrageEveryDamage !== undefined ? `・+${def.enrage}/被ダメ${def.enrageEveryDamage}` : ''}` : `激昂+${def.enrage}/T`)
  const growing = def.moves.filter((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined)
  if (growing.length > 0) tags.push(`育つ技(${growing.map((m) => `${moveLabel(def, m.id)}: 使うたび${m.growPerUse ? `+${m.growPerUse}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse}` : ''}`).join('／')})`)
  return tags
}

/** 戦闘状態込みの特性タグ (残量・カウントダウン・庇われ中など) */
export function enemyTraitTags(s: GameState, i: number): string[] {
  const e = s.enemies[i]
  const def = getEnemyDef(e.enemyId)
  const tags: string[] = []
  if (def.burnResist) tags.push(`延焼耐性${def.burnResist}`)
  if (def.thorns) tags.push(`とげ${def.thorns}(カードの攻撃ヒットごとに反射。人形の攻撃には反射しない。倒せば無傷)`)
  if (def.armor) tags.push(`装甲${def.armor}(1ヒットの被ダメは${def.armor}以下。成長・勢い・急所を乗せた後で頭打ち=上限を超える分の急所・成長は切り捨て。延焼は無視)`)
  if (def.startingBlock) tags.push(`開幕ブロック${def.startingBlock}`)
  if (def.burrow) {
    tags.push(
      e.burrowActive === true
        ? `潜伏中(殻${e.block}。尽きるまでHPにダメージが通らない・超過は捨てる。貫通も殻に吸われる・粉砕で割れる。割れると次の行動が噛みつきに変わる=割ったターンのうちに倒せば来ない)`
        : '潜伏(殻は割れた=以後は普通に通る)',
    )
  }
  if (def.nemesis) {
    tags.push(
      `因縁(${s.turn % 2 === 1 ? '今ターンは無形=1ヒットのHP損失が1固定' : '今ターンは実体=普通に通る'}。奇数ターン無形・偶数ターン実体。延焼は通る)`,
    )
  }
  if ((e.sealed?.length ?? 0) > 0) tags.push(`封じている札: ${e.sealed!.map((c) => c.def.name).join('・')}(倒せば手札に戻る)`)
  if (def.slippery) {
    tags.push((e.slippery ?? 0) > 0 ? `朧 残り${e.slippery}回(HPに届く当たりは1ダメージになる。人形・罠の当たりも数える。延焼は素通し)` : '朧(霧が晴れた=以後は普通に通る)')
  }
  if (def.imbalanced) {
    tags.push(e.staggeredNext === true ? 'バランス崩し(体勢を崩した! 次の行動は隙)' : 'バランス崩し(攻撃を完全に防ぐ=HP損失0で次の行動が隙になる)')
  }
  if (def.splitInto) {
    const child = getEnemyDef(def.splitInto.enemyId)
    // 予告HPは親のHP倍率 (幕・ボス係数・難易度) を継承した実値で出す (2026-09-03 Opusラン K:
    // 「二の相HP55」の予告に対し実際は132で出ていた)
    const childHp = splitChildHp(e, def, child)
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
  if (def.aura) tags.push(`重圧(生存中、${def.aura.attacksOnly === true ? 'ダメージを与える' : (def.aura.cardType ?? '全')}カードのコスト+${def.aura.costUp})`)
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
  // 鎮めの錘 (ギア) で割り込みを止めた敵は目覚めない = 眠りのタグも出さない (2026-09-24 Opus ひなた E5。HP半分の予告は interruptPreviews が空を返す)
  const sleeping = e.interruptBlocked === true ? undefined : sleepingInterrupt(def, e)
  if (sleeping !== undefined) {
    tags.push(`眠り(累計${sleeping.amount ?? 0}ダメージで目覚める。いま${e.damageTakenTotal ?? 0})`)
  }
  // 割り込みの予告 (2026-09-14 即時差し替え): 自分のターン中に起きればその場で行動が変わる
  for (const p of interruptPreviews(def, e, s, i)) {
    if (p.trigger === 'damageTaken') continue
    if (p.trigger === 'alone' && !s.enemies.some((o, j) => j !== i && o.hp > 0)) continue
    tags.push(`${p.text}(自分のターン中に起きればその場で行動が変わる)`)
  }
  const growing = def.moves.filter((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined)
  if (growing.length > 0) {
    // 宣言した時点で使用回数が1つ進む = 宣言中の技は1つ前の回数ぶんだけ育っている (2026-09-24 Opus ひなた E7: 宣言直後から1回先を数えていた。engine の buildIntent と同じ数え方)
    const usesNow = (id: string): number => Math.max(0, (e.moveUses?.[id] ?? 0) - (e.intent !== null && e.intentMoveId === id ? 1 : 0))
    tags.push(`育つ技(${growing.map((m) => `${moveLabel(def, m.id)}: 使うたび${m.growPerUse ? `+${m.growPerUse}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse}` : ''}。いまは${m.growPerUse ? `+${m.growPerUse * usesNow(m.id)}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse * usesNow(m.id)}` : ''}`).join('／')})`)
  }
  if (def.angerOnBlock) tags.push(`ブロック反応${def.angerOnBlock}(あなたがカードでブロック・氷壁を得るたび筋力+${def.angerOnBlock}。パッシブ・レリックの自動分は除く)`)
  if (def.regen && e.hp > e.maxHp * 0.5) tags.push(`再生${def.regen}${def.regenBreak ? `(このターン${def.regenBreak}以上削ると停止)` : ''}`)
  if (def.enrage) tags.push(def.enrageEveryCards ? `激昂+${def.enrage}/${def.enrageEveryCards}枚プレイ${def.enrageEveryDamage !== undefined ? `・+${def.enrage}/被ダメ${def.enrageEveryDamage}` : ''}` : `激昂+${def.enrage}/T`)
  return tags
}
