# Owned material GPU causal review

Dedicated branch `codex/unity-checkpoint305-20261029`, following `75188368e089be7db9281f6036d1fb92a6d2654a`. This change adds opt-in QA observations/comparisons. It selects no final material correction, removes no production hurt effect, and leaves default clocks, gains, world lights, shaders' normal formulas and gameplay events unchanged. Cold297, Root319 and Monk320/321 limited approvals are preserved.

## Canopy300 first: actual geometry, depth and post

Eight32-frame native cases: normal, solid, nodepth, solid-nodepth, notilt, nopost, mainonly and flat-mainonly. An additional both-off case disables TiltShift and URP post together. All retain actual cost2 installation -> cost0 Sprout -> Growth1 -> three damage3 events with original UIDs.

At actual contact age .0333333 (frame8), main batch kind40 is144 positive vertices, all role1. Top-origin projected bbox is x717.53656–791.6478 / y291.387878–328.003571, about74.11x36.62px. Vertex alpha .133–1, gain .72. Actual bound texture is canopy-organic-density-v1,1870x841; private shader DeckRogue/Hd2dCanopySurface. Main viewZ -27.7998638 versus captured receiver anchor -27.8597641. Anchor is not asserted to be the receiver mesh plane.

The independent GPU main-only draw yields1246 pixels with positive quantized alpha versus1249 for the solid geometry, max linear ARGB32 RGBA89/102/26/251 versus13/255/38/255. Thus this sampled main surface is not missing geometry, an unbound texture, or an entirely alpha-discarded surface. The solo PNGs are ungraded premultiplied linear ARGB32 diagnostics on transparent black, not final sRGB screenshots; HDR is clamped and the D3D offscreen image is vertically reversed relative to screen coordinates. Do not compare their apparent darkness directly to the tone-mapped stage.

Solid geometry is visible in the scene. Disabling depth testing does not visually restore the organic fold. Disabling TiltShift restores a sharper outline, but neither that nor URP post/both-off restores the selected Godot bright fold. This is evidence against treating Cold's TiltShift diagnosis as a complete Canopy diagnosis. Godot source shader and its gain path were read; original bitmap was not altered. Remaining color/compositing discrepancy needs a same-UV/source-renderer GPU oracle or further matched-stage measurements before selecting a correction.

Full-frame pixel equality is NOT claimed: independent states have evolving world/card animation and float timing differences. The audit retains nonzero full-image differences rather than hiding them. Projection and actual event observations support the narrow conclusions above.

## Enemy overlays and material-specific depth

Imp7, Wraith9, and Crossbow prior2 were each captured normal / no ScreenFlash / no ScreenFlash+no TiltShift. The actual call is red(.9,.1,.1), duration .25s; alpha .205 for Imp7, .235 for Wraith9. Initial Crossbow seed2 gives actual16 and alpha .34, so it is supplemental evidence only. The exact selected Crossbow seed20 was then captured in six conditions, actual14/Vulnerable1 and alpha .31: normal, noflash, noflash-notilt, solid, nodepth, solid-nodepth. Gameplay events are structurally equal across each A/B family.

Suppressing only the QA ScreenFlash removes HUD-wide red without restoring Wraith/Crossbow's selected local material. It does not remove the local actor hurt/movement. Wraith and Crossbow solid surfaces are visible; depth-always also does not restore their raw material. Actual plane measurements are viewZ -26.4659786 for effects vs receiver mesh-plane -26.74598, about .28 toward camera.

Wraith main role modeY1 at age .0333333 has60 positive vertices, bbox x292.298462–392.706238 / top-y299.420349–359.9064 (~100.41x60.49), gain1. The separate modeY0 motes occupy a different bbox. This replaces the former all-positive111x73 comparison; no fragment equivalence with Godot99x65 is claimed.

Crossbow seed20 modeY1 main is ~49.33x16.77px at age .0333347. ModeY2 hard edge, modeY3 shoulder and modeY0 motes are separately logged. Actual textures are R8G8B8A8_SRGB with named private shaders; no common scale/gain change was selected.

Imp additionally has four55-frame noflash cases: normal, only new contact batches6/7 after landing, world contract lights off, and both. Native frame37 shows that removing old batches alone leaves the large bright body/floor light, while removing the owned world-light contribution exposes the smaller orange heat-density contact. The two new batches' actual per-vertex projection, UV, alpha, gain RGB, texture and shader are recorded. This supports world-light dominance, not a reason to regenerate the raw or disable the game's whole hurt system. Actual7/Scald1 events are preserved.

## Light305 independent world-light comparison

Two70-frame actual white_light_bolt captures retain three damage4 events, corrected shared clocks, actor hurt values and local geometry. QA worldoff skips only the owned LightBolt contract contribution. Normal target peak is2.76412487 and floor peak9.008999; worldoff both0. White torso/legs and the floor pool strongly diminish while the underlying local contact remains faint. This isolates a major world-light contribution; zero light is not selected as a product fix.

Both runs commit at sampled frames11/18/25, each effect age0, materialFlash .0231481474. Actual-minus-expected delay is approximately0/.0133333/.0266667s, respecting the due-time gate. These correspond to relative contact offsets0/7/14; the Godot movie's12/19/26 has the same relative spacing. A common image-origin offset is a candidate, not proof of equal absolute clocks; source receipt/render-origin alignment still needs validation. No timing or hurt rollback was made.

## Validation and remaining work

33 native cases /1608 captured frames across five suites, Windows D3D11 1280x720 fixed30fps simulation. All completed build attempts used for capture report errors0; initial overlay instrumentation had one partial-script compile failure, corrected before capture, with its log retained. Existing43 build warnings are separate. Source inputs were already byte-verified; no raw regeneration/import changes were committed.

The evidence is causal diagnosis, not craft PASS. Canopy/Wraith/Crossbow still need the exact color/compositing cause and a validated local correction. Imp and Light need scoped world/receiver-light calibration, not wholesale light removal. Overall parity, Android performance, other body sizes and fatal Engine cases remain untested. No access/approval blocker is pending. The next useful source-side evidence would be actual selected Godot main-material GPU output at specified UV/age/gain/alpha, with render backend/output color space, to separate sampling/compositing from scene tone mapping; lack of that oracle does not block independent Unity measurements.
