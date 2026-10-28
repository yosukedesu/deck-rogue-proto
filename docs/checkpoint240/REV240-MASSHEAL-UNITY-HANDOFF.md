# MassHeal 240 — handoff (technical and bounded visual gates closed; parent selection authority)

## Scope and authority
Godot is visual authoring only; Unity remains the product. Do not overwrite the user's Unity renderer, math shim, URP shader, linear palette, HDR gain stream, triangle-count or actor ownership adaptations with Godot files. Port the intent into existing Unity APIs. Standard shape/material and continuous-frame gates passed narrowly; focused bounds and nested ownership tests are terminal. Final checkpoint selection/publication remains parent authority.

Only the exact positive `support:white_mass_heal/heal` support route invokes `CreateMassHeal`. Existing SupportVfxProfiles owner-action validation, first eligible heal slot, zero-heal slot consumption and renderer dispatch are unchanged from235. This is owner-action attribution, not a general assertion that nested effects are direct card-heal effects. Prayer and all other support families remain unchanged. No gameplay values, actual events, heal amount, cost, draw, exhaust, token or target rules changed. Stage/Actor/camera unchanged.

## Source and material
Use `revision240-source` / its224-entry SHA manifest, not live source. There is no source239:239 was a single-texture mip diagnostic on source238.
- `CreateMassHeal` is receiver-local, negative-age empty, end `.66s`.
- Left cubic points (-1.05,-.34,.025),(-.93,-.04,.05),(-.22,.15,.05),(.08,.12,.03); right (.47,-.26,.025),(.55,.04,.045),(.25,.23,.04),(.04,.13,.03).
- Right material lags `.075s`; retain the existing one numeric heal, not a second heal at convergence.
- Visible curve end `.40+.60*Ease(local/.16)`; start `.66*Ease((local-.23)/.17)`; thus long gathering surface remains before the root catches up.
- Halfwidth `(.012+(.17 left/.105 right)*sin(pi*u))*(.66+.34*open)*(1-.74*closing)*(1-.45*lift)`; open=Ease(age/.10),closing=Ease((local-.23)/.17),lift=Ease((local-.28)/.28).
- Separate low-frequency belly from raw detail on the same curve. Belly alpha `envelope*(.20+.08*sin(pi*across))*ends`, raw alpha `envelope*.28*ends`; `ends=Ease(u/.10)*Ease((1-u)/.10)`. Raw z offset +.001 for ordering, not a second physical shape.
- Raw UV `(.08+.84*u-.06*Ease(local/.28),across)` moves density toward chest. Local gain1; short highlights alpha .08/.05. No global brightness increase.
- Existing Light `.35*Ease(age/.16)*(1-Ease((age-.26)/.28))`, FloorLight `.12*Ease(age/.16)*(1-Ease((age-.28)/.25))`, TargetPeak/FloorPeak .045 and StageFocus0 retained.
- Layers: belly432/raw432/folds24/focus6/motes24 maximum active vertices; each below512. Do not confuse total vertices across layers with per-layer capacity. Unity's existing triangle index truncation remains necessary.

Raw `assets/Art/fx/hd2d-mass-heal-density-v1.png`: original1536×1024 RGBA,1,804,114 bytes, SHA256 `38290b7e1db5916c1fa9739ecc8d0cc231866e6a5f791f9db7276e0b7fffffb0`. Unmodified original retained; no resize. Alpha zero52.54%, max253; left/right raw edges contain alpha, so geometric endpoint taper is required. Existing authored-RGBA Kind3 path and shared shader are unchanged; no new shader/renderer selector in240.

## Sampling qualification
The selected test runtime used mip generation on this texture only, yielding10 mip levels, with existing `filter_linear_mipmap` sampler. Source239 does not exist and Godot `.import` is not a Unity deliverable. Reproduce the needed mipchain/trilinear sampling in Unity's existing texture pipeline; do not change all textures. The239 diagnostic softened edges but **did not** cure the missing belly;240's low-frequency/detail separation is the material change. Preserve Unity sRGB/linear handling; no double conversion.

## Evidence and limits
235 stationary white A-frame held;236 fog balls held;237 long-surface geometry causality improved but uniform cloth held;238 raw-only veins held;239 mip-only hypothesis rejected as primary cause. All evidence retained.
240 standard261 PASS; same rules/events as239 and relative age difference0. Directmovie363 PASS,70 continuous FramePostDraw frames at fixed simulation30fps,2.333s,silent495,034 bytes; wall22.244s, NOT a device-performance benchmark.
Independent reviewer saw standard full frames and MP4 consecutive decoded indices0–20: tail moves chestward after.233, shortens at.30/.333/.367, no double pulse, clear from.667. Upper release is subtle; dominant reading is chestward absorption then fade. Direct normal-speed subjective pleasantness was not certified. Unity motion/lighting and device performance remain unverified.

## Terminal focused bounds (fresh240)
Total1627 checks /6 clean exit0 hosts: standard261, directmovie363, heal0/1+lifecycle431 across2hosts, nested572 across2hosts. These counts contain repeated assertions and are not an exhaustive family matrix.
- Full HP actualheal0: no dedicated MassHeal body; first slot consumed, original rules retained. Partial actualheal1: one dedicated body. Cancel/death/completion lifecycle checks clean.
- Real initial energy3 → ManaConvert cost1/gain2 →4 → Censer cost1 →3 → MassHeal cost3 →0. Fresh direct15+nested1 and direct0+nested0 cases pass; installed nested SourceUid `qa-prep-1` remains separate. Dedicated body maximum1/0 respectively, no nested duplicate recovery membrane. No fabricated energy or synthetic heal event.
- All224 frozen source hashes remain authoritative; no source changes after standard. Source239 never existed: mip-only runtime experiment is separately evidenced.
- Native stopped after these tests. No phone-specific new capture or actual Unity motion/performance certification is implied by these contracts.
