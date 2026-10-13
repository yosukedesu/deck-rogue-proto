# Measured hero numeral backing, bounded follow-on

Off-tree candidate following the rejected hero-hp-underlay-delta native A/B. It requires that delta. Frozen predecessor packages and live source are unchanged.

Original reviewed pixels: ui-rich-r2/four130/frame-0000.png versus halo-v3-combined/underlay-four130/frame-0000.png. Their native control receipts confirm identical face/underlay rectangles and correct sibling order. At nearest4x inspection the dark layer still competes with soft glyph antialiasing and the patterned band. Its presence is not a readability pass. See verification/hero-hp-underlay-review/hatch-only-top_underlay-bottom_nearest4x.png.

Source UiKit.NumHaloNight backs bright numerals with Ground using a glyph-shaped SDF underlay. This follow-on deliberately uses a small opaque Ground rectangle behind the measured numeral ink bounds as a Godot readability adaptation. Unity does not use a rectangular plate; exact shader parity is not claimed. No font, font size, face weight, spacing, line height or global scale changes.

Three hunks in SourceHudLabel only. The failed glyph-shaped underlay is hidden for all states, restoring the original bright core without that dark antialiasing interaction. The existing hero-only opt-in creates one Ignore-input ColorRect before the bright core. Bounds are the actual existing glyph-ink bounds plus max(1,BattleUnit) logical padding. It is visible only while the displayed hero has positive HP and predicted loss. No-loss baseline has neither plate nor failed glyph underlay, so its original face is retained. It never reads engine state, modifies the source hatch/track/tick geometry, or changes input/forecast ownership. No per-frame nodes or resources are created.

Runtime must preserve the previous failed A/B and test this as a separate candidate. Required native100/130: HP48/80 readable at normal frame scale, intact bright core, no clipping or hatch intrusion inside the measured backing; source loss/hatch/tick remains visible outside it; no-loss state has no plate; block changes shrink loss without shifting the text; existing forecast inspect hit remains unchanged. If these fail, leave the gate open. Do not expand to a font system redesign.

python ui-parity-restoration-20261013/hero-hp-backing-delta/apply.py PROJECT --apply
