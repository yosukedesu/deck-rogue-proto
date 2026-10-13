# Hero source loss-band repair

Off-tree candidate. No live edits. Four130 native currently obscures HP48/80 with bright, dense forecast hatching; this delta restores the proven source geometry/ink before a separately gated underlay experiment.

## Exact source

Hash-verified Stage41 BattleScreen.cs 1581–1598 and 1610–1668: the HP bar has a 2-unit ink edge, a 1-unit track, and a 3-unit inset inner fill. Forecast loss occupies [LossTo/Max, Now/Max] within that same inner track. RoseLoss is #e1ae9e. Its left tick is 2 units wide and extends 3 units beyond the inner top/bottom; lethal loss hides that tick.

Shared Theme.cs 774–795, ThemeFx.Hatch: 8×8 Point/Repeat pattern, (x+y)%8<2, Ink #2f2e35 alpha .35. One texture pixel equals one source canvas unit. Shared PaperFx.cs 839–843 and 928–963: RoseLoss is not remapped by NightFillPairs; the thin Ink tick becomes NightEdge. Outer broad Ink remains Ink; Paper track becomes NightTrack #31344f.

The old hero draws loss over the entire outer bar and overlays bright paper lines every 6 logical pixels. It neither uses source ink nor scales its hatch period by source unit. This is a verified source-backed omission, not a subjective contrast preference.

## Scope

Four guarded hunks across SourceHealthBar and BuildUxHero. Enemy branch is unchanged. Hero records BattleUnit, uses source inset geometry, source loss tint, exact repeated pixel-mask spans, and left tick. The existing rose fill color is retained; its source gradient remains an explicitly unresolved rendering difference. Existing HP, forecast, receipt and prediction timing/owners are unchanged. No rules or commands change.

Pixel spans reproduce the tiled pattern without texture resources, readbacks, filter inheritance, or new native callbacks. No resources are allocated per draw. The bar still redraws through the existing owner. The source uses fixed LossTo during its numeric slide; this patch does not redesign existing Godot prediction ownership and does not claim that separate timing gap closed.

## Verification and native gate

218,208 pure pixel/geometry/installer assertions pass (including separate-underlay installer checks). These validate the implemented mask algorithm and bounds, not native pixels. Combined current source + hatch + underlay compiles with 0 errors/15 baseline warnings. No native acceptance yet.

Apply by runtime owner only:
python ui-parity-restoration-20261013/hero-loss-hatch-delta/apply.py PROJECT --apply

Run rich-four and rich-solo at100/130 with idle.actions.json. Preserve first hatch-only frame, then compare the separately applied hero-hp-underlay-delta. Check full currentHP/max, source 8-unit ink hatch rhythm, unclipped tick, lethal loss tick suppression, no-loss normal bar, block reducing predicted loss, and hidden intents. Preserve global text scale and decision numbers. If readability still fails, report it; do not declare parity from a compile.
