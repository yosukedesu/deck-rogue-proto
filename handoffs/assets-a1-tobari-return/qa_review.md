# Tobari A1-r revised return frames: independent QA

## Readiness

**Qualified pass: ready for animation review and integration testing.** The revised set meets the objectively verifiable file, ground-line, connectivity, and no-visible-effects requirements. The earlier native-idle size gap is substantially resolved. This is a file/render review, not an in-game verification or a guarantee of a perfectly seamless transition.

Only the revised files in `final/` were judged for this report. Images were not modified.

## Required checks

- All three frames are **192 × 128, RGBA**.
- The true final nontransparent row is **115 in every frame**, including alpha=1 pixels. All pixels from row 116 downward are transparent.
- Feet stay on the same ground line. Canvas placement is compatible with the stated **(99,116)** ground anchor. PNG files alone do not establish an engine pivot; runtime registration remains to be confirmed.
- Right-facing/three-quarter character presentation is retained. No left-facing reversal was found.
- The three frames advance the scythe from a low diagonal after attack15 to the raised diagonal idle hold. The largest rotation occurs from return0 to return1; return2 settles toward idle.
- Both visible hand grips plausibly connect to the shaft. The shaft-to-blade joint is continuous. At alpha >16, each frame is one connected silhouette; visual inspection also found no detached hand or weapon part.
- No visible glow, smoke, trail, or added effect was found in rendered alpha composites.
- Body proportions are broadly consistent. The torso rises between return0 and return1 as the character straightens; no gross scale discontinuity was found in the revised sequence.

**No strict required-criterion failure was found in this file/render review.**

## Native-idle endpoint and continuity

The comparison now uses the actual native idle at its original 128 × 84 pixel scale, placed at (+39,+39) on the 192 × 128 canvas. This placement was independently checked to preserve the original pixels exactly.

- Return2 alpha bounding box: **(60,43)–(127,116)**, exclusive maximum coordinates; height **73 px**.
- Native idle placed at (+39,+39): **(64,41)–(130,116)**; height **75 px**.
- The material return2 silhouette at alpha >16 begins at y=44, so its visible upper edge is about 3 px below native idle's.

The old substantial enlargement at the native-idle handoff is no longer evident. Small weapon-tip, shaft-tail, face, hair, and hand-position shifts remain, but the final pose is a credible intermediate into the untouched idle. Return2 is not a pixel-identical near-copy of idle, so a small settling change is still visible.

## Quality caveats, not visible blockers

- Return0 has isolated alpha=1 pixels at **(30,42)** and **(0,72)**. Return1 has a three-pixel alpha=1 component at **(84,57), (85,57), (84,58)**. Return2 has no isolated nonzero component. These are not apparent at normal display size and do not violate the bottom-row requirement. No thresholding or cleanup was performed.
- The generated frames have partial alpha, predominantly near opaque, with maximum alpha 254. The source sprites use binary alpha. This produces slightly softer edge treatment rather than a visible glow.
- Face/eye details and fine clothing shading change somewhat between the generated poses and source art. These are minor identity/style consistency caveats, not detached geometry or registration failures.

## Preview verification

`attack_return_preview.apng` contains five 192 × 128 frames in the correct order. Each decoded frame exactly matches its corresponding reference/output PNG: attack15, return0, return1, return2, native idle placed at (+39,+39). Durations are **350 / 100 / 100 / 100 / 500 ms**, looping. This verifies the preview contents, not game timing or renderer behavior.

## Reviewed SHA-256 hashes

- `leader_black_attack_return_0.png`: `3a148a631d194e7fa9a667bd74ebdf3efab23001bf8ff88df484a50e93c64c97`
- `leader_black_attack_return_1.png`: `750a7796fb39e79730529023e36e71f3fa0bdc1ca862226011fdccdbafe70648`
- `leader_black_attack_return_2.png`: `c2ec2c516e1fcedd3ff77e9db655f32350ab18b91906877ec2dc774fff04b9c0`
