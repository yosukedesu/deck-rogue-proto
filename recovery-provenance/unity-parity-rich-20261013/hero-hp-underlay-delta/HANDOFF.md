# Hero HP underlay native A/B candidate

Separate from the exact hero-loss-hatch-delta. Off-tree; unaccepted until runtime pixels are reviewed. Can be omitted while inspecting hatch-only.

Source UiKit.cs 202–236 NumHaloNight uses a bright face, Ground-colored zero-offset SDF underlay, underlay dilation1.2/softness.15 and no outline. Godot's previous zero-offset small-label shadow hollowed the bright face in the preserved native probe, so simply re-enabling it is unsafe. SDF material values are not equivalent to Godot pixel widths.

This bounded renderer adaptation draws a separate filled Ground label plus Ground outline behind the existing unchanged bright hero HP label. Its local radius is round(2 * BattleUnit * textScale), the prior translation's intended underlay extent. It is explicitly an A/B candidate, not measured TMP shader parity. The core font, face embolden, size, text, alignment and ownership remain unchanged. The underlay shares the core's current position/size/scale, is Ignore input, is parent-owned, and cannot update game state. It is created once per hero label; no per-frame nodes, font resources or theme overrides. It is inserted immediately before the core sibling so the bright face paints last.

Three guarded hunks in SourceHudLabel and BuildUxHero. Enemy labels and other labels never enable this helper. No new native callback or generator is needed; existing SourceHudLabel process/alignment bridge is reused.

Combined source compiles0 errors/15 baseline warnings. Installer preflight/idempotence/conflict checks pass. Native glyph fill, edge softness and precise readability are NOT validated.

Apply only after retaining hatch-only screenshots:
python ui-parity-restoration-20261013/hero-hp-underlay-delta/apply.py PROJECT --apply

At100/130, compare exact same rich-four and no-loss states. Require all bright glyph interiors intact, HP48/80 immediately readable over the hatch, no halo clipping, no changed baselines/spacing, and no duplicate input/inspect hits. Keep the layer only if native comparison improves decision-number readability. Font raster tuning beyond this functional underlay remains deferred.
