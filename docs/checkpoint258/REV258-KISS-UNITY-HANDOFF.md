# CorpseKiss 258 — final qualified Unity port handoff

## Authority / do not replace Unity files wholesale
Selected published base is 253 (`428fbaeb1bb1ae9686834c1667613f86e3ee1c2a`). Selected258 is frozen246 logical paths in the reconstructed workspace. Godot full C# and shaders are comparison sources, not drop-in replacements for Unity renderer/math/shader/Stage/Actor. Preserve Unity-specific Linear color, `c.linear`, separate HDR gain UV, triangle-index truncation, zero-safe math, and existing APIs. Port only the changed factory, exact route/material dispatch, original texture, and material intent. No Unity code was changed here.

The original pre-recovery workspace and its historical QA remain inaccessible/unrestored; permanent loss is not established. Current evidence is fresh native QA in the formally reconstructed workspace, not a claim that the old full manifest was recovered.

## Exact scope and game meaning
- `black_corpse_kiss` / `SourceKind=necro` / `card:black_corpse_kiss` / `dark_drain` / `dark` / `Hd2dNecroContactFx` / `FixedSmall=true` only.
- Necro means the actual card instance was moved from draw pile to exhaust by installed Censer, with ordered actual `CardsMilled → NecroFired → DamageDealt` events and a valid enemy presentation receipt. It does not mean ordinary hand-play Kiss.
- Ambient `SourceUid` is installed Censer (`qa-prep-0` in this fixture), while `PresentationSource.Uid` is the exhausted Kiss (`qa-nested-drain`). They are intentionally **different**. Do not copy installed-Thorn UID-equality logic.
- The game remains unchanged: real Censer install + Prayer exhausts Kiss, necro drain3, independent heal1, then recursive non-necro mill as the real Engine dictates. Prayer and other Light healing remain separate. No summoned body, Censer projectile, or healing return beam was invented.
- Old shared Dark, Skull necro, direct Kiss drain5, installed Dark, selected Curse, other support and system routes remain unchanged.

## Files / ownership
`CreateCorpseKissContact()` is new. `NecroContactVfxProfiles.CorpseKissRoute()` restricts existing selected ownership. `NativeGeometryBridge` dispatches only this exact route to the new factory. `AuthoredGeometryFx.UsesCorpseKissDensityMaterial()` chooses the new shader only for the full route plus effect `Hd2dPrototype`/`hd2d-dark`, body `Kind16`, `Space0`, exact texture `hd2d-necro-kiss-density-v1`. Dark contact and motes stay on the old shader. Shared `authored_Hd2dPrototype.gdshader` is byte unchanged.

## Original material
`assets/Art/fx/hd2d-necro-kiss-density-v1.png`: original rawB, 1254×1254 RGBA, 1,370,215 bytes, SHA256 `a98b61f48f1f6621bdc32e97608003a34017d29e89d21b442a80d681902b507b`. Requested generation size was1024 but actual1254 is retained without resizing. Alpha>16 bbox `(50,99)-(1162,1143)`, border max1. Do not apply offline edits to the original. RawA closed cloudbag is retained as rejected evidence, not the runtime material.

Use a clamped RGBA color texture. Preserve sRGB texture sampling into linear color and original alpha. This filename deliberately does not match old `dark-contraction` erosion/phase prefixes; `_TextureMotion=0`. No settings of other assets change. Current native import settings are evidence, not universal Unity product-quality certification.

## Root, shape and local time
No Stage/Actor integration change: all such numerical deltas are **zero**. No camera, hit time, actor pose, gameplay resource or floor lighting change.
- Existing target-local chest root `(0.26,0.01,0.025)` is scaled by the existing target height/size and pose-independent effect anchor convention. No fixed screen coordinate.
- Measured raw dark-tip centroid ≈`(.894616,.830288)`, authored UV pivot `(.895,.830)`.
- Carrier `x=(u-.895)*.697`, `y=(.830-v)*.503`. `d=Clamp(sqrt(x*x+y*y)/.65)`; small bend `y += .075*sin((u-.895)*PI)*d*(1-consume)`. Then root+(x,y,0).
- **258 removes** earlier radial `draw=1-.64*consume*d`. The branches are no longer all folded into one fan.
- `dt=visualAge*1.25`, `consume=Ease(dt/.20)`, `onset=Ease(dt/.018)`.
- `envelope=1-Ease((dt-.25)/.10)`; original body opacity coefficient `.58`, gain1. Negative age or age>.30 resets all geometry.
- Upper broad stream mask `main=1-Ease((v-(.26+.81*u))/.10)`. Lower subordinate stream factor `subordinate=1-Ease((dt-.005)/.070)`; hierarchy `main+(1-main)*subordinate`.
- Outer-end erasure frontier `.85*Ease((dt-.015)/.22)+.035*sin(v*8)*Ease(dt/.06)`; remaining `Ease((u-front+.10)/.18)`. Coverage=hierarchy×remaining. This is local material consumption, not a new damage event or a uniform whole-body fade.
- Body uses12×6 cells=432 written vertices; dark contact6 and motes18. Allocated `Count=512` per batch, not total written count. Kinds4/16/2.
- Small dark touch and3 inward quiet remnants keep their original timing. `TargetPeak=.028`, `FloorPeak=0`, `StageFocus=0`, `Light=1-Ease((age-.025)/.12)` for nonnegative age; floor light0.

## 257 local linear highlight shoulder (held fixed in258)
New shader `authored_Hd2dKissDensity.gdshader` copies the old shader with one local sampled-linear-RGB operation. Let `Y=dot(sampledRGB,(.2126,.7152,.0722))`. If Y≤.28, preserve exactly. Otherwise `Y'=.28+.18*(1-exp(-(Y-.28)/.18))`, `sampledRGB *= Y'/Y`. Alpha and RGB ratios remain unchanged. Examples `.5→.40698`, `.9→.45425`.

`.46` is the sampled-texture luminance asymptote **before vertex scale**, not a final pixel/bloom/display luminance limit. Unity linear texture sampling already converts sRGB: do not port Godot `OUTPUT_IS_SRGB` compensation literally. Keep blend_mix-style transparent composition and existing material scaling; no local additive emission was added.

Reason: authoredRGBA uses rawRGB×max(vertexTintRGB), not the palette hue. Raw upper-white density therefore survives.257's compression changed actual pixels but did not solve the three-finger/fan silhouette. It remains fixed only to isolate258 consumption; do not describe257 alone as an artistic success.

## Final qualified selection / evidence
258 is selected for this exact receiving-only necro Kiss scope after the following focused evidence. Earlier contract `pending` fields and the draft sidecar are historical; this final section has priority. They do not override the final source formulas above.

**5939 assertions / 7 successful exit-0 runs**, with repeated assertions (not5939 distinct behaviors or a full regression matrix):
- Standard949; continuous native movie1041.
- Actual RockBeetle shell627; shared Skull754; normal hand-play Kiss422; Moss small-window1006; lifecycle corrected-B1140.
- Known driver VSync warning in every run. Two successful runs also emitted unresolved shutdown-resource notices: movie act1 Ogg2; shell act1 + card_draw Ogg4. Other5 runs did not reproduce shutdown-resource notices. This does not resolve the Audio issue; no Audio product code is in this checkpoint.
- Original lifecycle attempt stopped on a QA assertion that confused pooled draw capacity with active3 batches (1056 assertions / exit1); retained separately and excluded from successful totals. Corrected-B verifies pool4, active prefix3, unused1 invisible with zero surfaces. Product source was not altered to fix the QA test.
- Earlier selector-harness compile failure (unqualified BindingFlags) and all held254–257 source/QA remain separate. RawA closed bag was rejected before native; original retained, runtime usesB only.

Actual rules and ownership:
- Whole transition remains byte-equal to fresh253 baseline. Initial hero72→74 Prayer; enemy200→197 Kiss; hero75 from independent drain heal1; subsequent actual recursive non-necro mill and Light healing are not flattened into one event.
- Shell uses real shipped RockBeetle block (no injected block): necro amount3 / HpLoss0 → subsequent corresponding heal1 → next green_guard mill, hero72→75. Prayer heal2 is not falsely counted as the drain heal. Dedicated Kiss body stays absent for the blocked hit.
- Shared Skull amount4 / separate heal2, hero76; old shared Dark batch topology retained.
- Normal hand-play Kiss amount5 / heal2, hero74, Energy3→2; no necro Kiss body. This is an actual legal card command, not a fabricated source event.
- Moss actual amount3 / heal1, hero75. Original target/probe and Moss two body types reviewed, with960×540 reduced window. Not a physical-phone test or all-enemy registration proof.
- Lifecycle covers before contact cancel, natural completion, after contact cancel, and component `SetDead(true)+cancel`, plus reload/cleanup. Component death is not an Engine-lethal event test.
- Production material selector was directly invoked for real and near-miss route/null/effect-shader/palette/body-texture/space/kind cases; no QA reimplementation of the selector. Actual body shader is dedicated; contact/motes remain old.

Art limits:
Astra inspected native stills and decoded contiguous movie indices7–20: idx8/.017 contact presence → .050/.083 lower/outer material consumed first → .117 small chest contact → .150 almost gone, no renewed expansion. Brief multiple pale branches remain at the peak, but the time sequence no longer holds a fixed white hand/wing/fan. This is a bounded improvement, not a claim of perfect peak silhouettes or highest-quality completion. Existing hero idle, one Kiss receipt, separate Prayer/heal timing preserved.

The70-frame native movie is30fps fixed simulation, silent;489593bytes, SHA256 `93bb776f6120a99adc3f3b74a2eb73f2aa05c7f756db0a38969a5c536de2cac3`. Contiguous-frame review is not direct equal-speed subjective playback certification, wall-clock performance, device performance, or Unity motion verification. Actual `visualAge`, not filename/capture-label time, is comparison authority.

No new source/body edits are requested by the independent review. Final Unity integration appearance and performance remain for the user's Unity captures; production need not stop waiting for those results.
