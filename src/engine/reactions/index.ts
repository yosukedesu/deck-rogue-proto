// engine/reactions/index.ts — 方式レジストリ
// 3方式は ReactionSystem の実装として完全に差し替え可能 (CLAUDE.md「比較する3方式」)。

import type { ReactionMode, ReactionSystem } from '../types.ts'
import { holdManualSystem } from './hold-manual.ts'
import { setAutoSystem } from './set-auto.ts'
import { setConfirmSystem } from './set-confirm.ts'

const systems: Record<ReactionMode, ReactionSystem> = {
  'set-auto': setAutoSystem,
  'hold-manual': holdManualSystem,
  'set-confirm': setConfirmSystem,
}

export function getReactionSystem(mode: ReactionMode): ReactionSystem {
  return systems[mode]
}
