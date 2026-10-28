# 243 → 245: BurnTick target-local thermal material

Status: parent selected245. Static and contiguous-frame visual gates passed; focused bounds completed. This sidecar does not modify frozen source. One movie shutdown warning remains unresolved; see final qualification below.

## Unity integration boundary

Port the new `CreateBurnTickContact` factory and the single StatusContact dispatch change. Godot comparison source is NOT a replacement for the user's Unity renderer, shader, math, Stage, or actor code. Preserve Unity-specific Linear/sRGB handling, HDR gain channels, triangle index correction, and user changes.

- Existing validated `system:burn` only. `Select` requires system/key/family/fire/FixedSmall, positive actual BurnTick, owned event, matching enemy receipt/index. `SelectedRoute` itself checks only system/key/family/selected renderer; do not describe it as an independent full receipt proof.
- `NativeGeometryBridge`: existing StatusContact selected branch now calls `CreateBurnTickContact`.
- Keep old `CreateBurnStatus` and `CreateInstalledFireContact()=>CreateBurnStatus()` unchanged. Installed Fire is deliberately NOT switched to the new material. Removing the new factory restores the entire known 243 Hd2d source byte-for-byte.
- No new owner, additional damage, projectile, source cast, persistent between-tick flame, or spread event. No Stage/Actor/camera/timing/Engine changes.

## Raw / materials

No new raw or shader. Keep original `hd2d-fire-volume-02.png` and `hd2d-fire-volume-04.png` unchanged, along with existing `hd2d-fire` palette and shared shader behavior. No global import changes.

Texture-02 UV regions, expressed as `(originX, originY, width, height)`:
- main upper flame: `(0.53, 0.035, 0.46, 0.60)`
- short subordinate flame: `(0.04, 0.29, 0.43, 0.44)`
- texture-04 single upper remnant: `(0.55, 0, 0.45, 0.73)`

These are UV selections on untouched originals, not edited images. They avoid presenting the whole texture's central radial white explosion as a recurring status tick. Do not tile the cropped rectangles visibly.

## Geometry and seconds

All local geometry stays in Space0. Each tongue uses 8×4 cells (192 vertices); two body tongues total384, cooling192, five ember tapers30, warmth6. Each batch remains below512; total vertices and per-batch capacity are different quantities.

Let `v=0` be the upper edge and `u` the width coordinate. Across-width coefficient is `0.20 + 0.80*Ease(v/0.82)`; upper end narrows without increasing height. Curvature is proportional to `(1-v)^2`. Endpoint alpha uses one explicit boundary taper `Ease(u/.14)*Ease((1-u)/.14)*Ease(v/.14)*Ease((1-v)/.14)`, and the lower edge is consumed through `1-consumed*Ease((v-.45)/.55)`.

- `rise=Ease(age/.13)`; `consumed=Ease((age-.035)/.12)`.
- Main envelope stays1 through.035 then fades to0 at.155. Main bend `.12+.09*rise`, main halfwidth `.255*(1-.22*consumed)`, halfheight `.335*(1-.12*consumed)`.
- Main center is original local center `(.035,-.055,.035)` plus `(.075+.025*rise,.09+.15*rise,0)`.
- Subordinate center offset `(-.285,.01+.11*rise,.004)`, halfwidth `.115*(1-.35*consumed)`, halfheight.215, bend `-.035-.04*rise`; fade .020→.105. This asymmetry leaves receiver-visible negative space rather than a symmetric pair or merged fire egg.
- Remnant onset0→.035, decay .075→.200; upward motion and lower-edge consumption prevent a second ignition. Shape is a single selected texture04 lobe, not three equal fragments.
- Negative age has zero geometry; sampler returns after.212. Five original ember births remain at `i*.011` and their original motion/fade is unchanged.
- Existing light retained exactly: TargetPeak.035, FloorPeak.015, StageFocus0, Light=`age<0?0:exp(-age/.085)`, FloorLight0. Do not interpret .212 geometry cutoff as a newly zeroed analytical Light function.

## Fresh evidence and limits

This work uses a formally reconstructed 243 source tree. Whole 243 Hd2d bytes matched the prior known SHA; the original227-file QA/source manifest is not currently recovered because access to its original environment has not returned; permanent loss is not established. All current native results are NEW evidence, not replacement claims for lost originals.

Fresh243 baseline: real Ignite damage3, HP200→197, Burn4, Energy3→2; actual EndTurn BurnTick4, HP197→193, Burn4→3. 422 assertions/exit0. Fresh244 and245 standard use the same whole-rule/event/actor/relative-age fixture. Absolute startup stage clock may differ and is not a visual-age equivalence claim. 244 held (merged fire egg);245 static gate passed (thin asymmetric main flame, short subordinate flame, receiver gap).

Temporal review uses decoded consecutive MP4 frames, not direct normal-speed subjective viewing. Observed first native Burn frame19 at age0; frame21+.067 lower-edge consumption;22+.100 low upper remnant;23+.133 short sparks; system:burn absent by27. A separate enemy:poke occurs at34–41, so do NOT claim all effects clear from27. No extra Burn flash/reignition was seen. Unity output, physical-device performance, and subjective full-speed feel remain unverified.

## Burn HP semantics — do not change game rules

Engine Burn subtraction is not clamped: `Hp=e.Hp-amount`. Enemy receipt copies the raw before/after HP. Actor `DisplayedHp` also stores raw HP; only the HP label text clamps with `Math.Max(0,hp)`. Thus a lethal HP1/Burn2 receipt can end at-1. Compare raw delta to Amount, dead state with `<=0`, and visible label separately. The initial helper proposal incorrectly assumed clamped raw HP; it was corrected before execution and preserved as an unrun preparation error. Do not implement a gameplay clamp to satisfy the mistaken assertion.

## Final focused QA qualification

- 2165 assertions across5 successful exit0 runs: standard422, normal-drain movie508, repeat519, zero307, lethal409. This is4 runs without shutdown-resource notices (known driver VSync warnings remain) plus1 movie shutdown-warning run, NOT5 clean runs and NOT a whole-game matrix.
- The initial movie attempt had496 checks with1 drain assertion failure; it is retained separately and excluded from successful totals. The corrected capture kept70 native frames then allowed1 additional natural frame for EndTurn to finish; no VFX fast-forward or source/cleanup patch.
- Repeat: actual HP197→193→190, Burn4→3→2 and tick amounts4→3, full events/receipts/oracle and terminal clearing passed.
- Zero: initial Burn0 and actual EndTurn produce no dedicated Burn sampler.
- Lethal/multiple: real leaping flame then EndTurn; enemy0 raw HP1→-1, DisplayedHp-1, label0/200, receipt index11 with7 positive samples; enemy1 raw200→198, label198/200, receipt index13 with6 positive samples. Independent owners, both positive geometry, all native effects inactive/lights0 at completion.
- Normal-drain movie still reports unresolved act1.ogg AudioStreamOggVorbis/OggPacketSequence/Playback resource shutdown warnings. This is not presented as leak-free; no BGM fix is mixed into this VFX checkpoint.
- Movie:70 contiguous fixed-simulation30fps frames, silent584672B, SHA256 `10f85f7b8347eb8bf2f4e304a0d8ec9aa31887948c564c4ce16e0c1cb21edd28`. Library `libfile_433fdb4c46f48191aab0e8f5eac09272` v0. It is not device-performance evidence.

Final fresh technical receipt: `qa/REV245-TERMINAL-QUALIFIED-SUMMARY.json`. Frozen245 source remains unchanged. Historical244 HOLD remains retained; this final selected qualification supersedes pending labels in trial contract documents without rewriting their bytes.
