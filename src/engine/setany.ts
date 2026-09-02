// engine/setany.ts — 実験「全カード伏せ可」(2026-09-02 ユーザー裁定「そうしてみる」)。
// 通常カード (物理/呪文) を 1E で伏せ、誘発時に印字コストを持ち越しエナジーから払って発動する。
// 専用リアクションは従来どおり (伏せ時に印字コスト・発動無料 = 「伏せた時点で撃てる保証」が特権)。
// 変換は**トリガーだけ**: 防御系を含む札は被攻撃前 (onAttackIncoming)、それ以外は被攻撃後 (onAttacked) に
// onPlay 効果をそのまま解決する (dealDamage は行動してきた敵へ)。
import type { CardDef, CardInstance, DeclarativeEffect } from './types.ts'

/** 通常カードを伏せる時に払う固定の手数料 */
export const SET_ANY_FEE = 1

/** 敵フェーズに解決しても意味が無い・悪用になる効果は伏せ不可 */
const EXCLUDED = new Set([
  'drawCards', 'impulseDraw', 'gainEnergy', 'gainEnergyMax', 'discountNext',
  'retrieveFromDiscard', 'searchDeck', 'upgradeInHand', 'addCopyToDiscard', 'growSelf',
  'exhaustFromDeck', 'exhaustFromDeckChoose', 'recycleExhaust', 'addCardToHand', 'summonPermanent',
  'gainSetSlot', 'addSpellEcho', 'addCasts', 'retrieveFromExhaust', 'playFromExhaust', 'gainHp',
  'negate', 'negateConvertIce', 'confuse', 'drawCardsPerCardPlayed', 'dischargeAetherDraw',
])
/** これを含む札は被攻撃前 (pre) 窓で解決する = 守りとして構える */
const PRE_WINDOW = new Set([
  'gainBlock', 'gainIceBlock', 'gainBlockPerEnergyMax', 'gainBlockPerPermanent', 'gainIceBlockPerCardPlayed',
  'gainIceBlockPerHandCard', 'dischargeGrowthBlock', 'dischargeMomentumBlock', 'shatterBlock', 'shatterBlockConvert', 'weakenEnemy',
])

/** 通常カードとして伏せられるか (実験フラグが立っている時のみ意味を持つ) */
export function canSetAsNormal(def: CardDef): boolean {
  if (def.type !== 'physical' && def.type !== 'spell') return false
  if (def.xCost === true || (def.modes?.length ?? 0) > 0) return false
  if (def.discardCost || def.exhaustCost || def.necroCost !== undefined || def.shivToken === true) return false
  if (def.effects.length === 0) return false
  if (def.effects.some((e) => e.trigger !== 'onPlay')) return false
  if (def.effects.some((e) => EXCLUDED.has(e.effect))) return false
  return def.effects.some((e) => e.effect.startsWith('dealDamage') || PRE_WINDOW.has(e.effect))
}

/** 伏せた通常カードが解決する窓 */
export function setWindowStage(def: CardDef): 'pre' | 'post' {
  return def.effects.some((e) => PRE_WINDOW.has(e.effect)) ? 'pre' : 'post'
}

/** 伏せ札として読む効果列。専用リアクションはそのまま、通常カードはトリガーだけを窓に差し替える */
export function setEffectsOf(card: CardInstance): readonly DeclarativeEffect[] {
  if (card.def.type === 'reaction') return card.def.effects
  const trigger = setWindowStage(card.def) === 'pre' ? 'onAttackIncoming' : 'onAttacked'
  return card.def.effects.map((e) => ({ ...e, trigger }))
}

/** 伏せから発動する時に払うコスト (専用リアクションは伏せ時に支払い済み=0) */
export function setFireCost(card: CardInstance): number {
  return card.def.type === 'reaction' ? 0 : card.def.cost
}
