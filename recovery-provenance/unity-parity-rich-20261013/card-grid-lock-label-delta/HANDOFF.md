# Explicit card lock-reason label only

Off-tree one-hunk follow-up to native relic-cap finding (attempt6-hand130 manual093). No live change.

Cause: SourceCardGrid printed `locked ?? note` for every disabled card. At the relic selection cap, note is the action caption 選ぶ, so unselected dimmed cards acquired a misleading red external label.

Source: shared Unity RunUi.cs427–441 shows a reason surface only for the explicit nonempty lockReason, separately from btnEnabled; RewardScreen.cs150–163 passes transform-lock reasons independently of max-count selection enablement. Max-count disable does not become an error-caption surface.

Correction: show the existing reason label only when lockReason is nonempty; use that reason verbatim. Dimming, inspection, selected corners, actual transform lock reasons, desktop action buttons, enable predicates, commands and state are unchanged. Existing external placement of real lock labels remains a renderer/layout difference from Unity's over-card strip; this small correction does not claim to fix that separate gap.

Checks: full composed compile0 errors/15 baseline warnings;54 label-policy/guard cases; exact dry-run matches3df4d5a. Native follow-up: at cage2/2 and astrolabe3/3, disabled unselected cards no longer show red 選ぶ; explicit status/brand transform restriction still shows its meaningful reason; deselect/reselect and confirm stay exact once. Nonblocking and separate from current battle batch.

    python ui-parity-restoration-20261013/card-grid-lock-label-delta/apply.py runtime-recovery-20261012/project

Dry-run by default. Only runtime owner applies in its chosen window. One existing file only: GameUI.CardModalControls.cs.
