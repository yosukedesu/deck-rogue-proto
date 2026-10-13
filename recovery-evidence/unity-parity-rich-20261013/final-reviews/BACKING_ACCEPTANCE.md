# Bounded hero numeral readability accepted

Five original native first frames in module943e2464 pass: rich100, rich130, normal100, no-loss100, block10-at100. Bright48/80 strokes are legible without the rejected glyph layer. No-loss has no backing. Block10 retains the same bar/label bounds and correct48→27 forecast. All backing controls ignore input.

The source hatch/tick/bar code is byte-identical to R2. Rich130 pixel comparison has880 changed bar pixels, all inside the measured backing rectangle; zero bar pixels outside change. The background is a deliberate Godot adaptation to source NumHaloNight’s legibility intent, not a Unity rectangle. It can cover a narrow stripe/tick beneath the numeral region; forecast numbers remain visible.

This closes the named static legibility/visibility gate. It does not claim all rich-control pointer/lifecycle cases, dynamic digit/resize cases, or exact source shader/font parity. Further typography polish is deferred.
