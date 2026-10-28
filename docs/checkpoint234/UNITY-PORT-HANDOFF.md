# Published selected234 qualification
This document retains chronological233 HOLD then234 selected correction. Latest234 terminal section supersedes earlier pending wording. FixedUnity main audit is historical, not current or localUnity verification. Do not replace Unity-specific renderer/math/HDR-gain/index510/URP handling.

# Light Torrent 233: Unity port handoff

Status: HOLD after independent Astra continuous-frame review: three-beat cadence/clear passes, but the caster returns to idle before the third connector, leaving its source below the current prop. Do NOT treat233 as final accepted motion. Read-only234 pose-lifetime investigation is pending parent selection. This sidecar does not change frozen source216 or certify Unity motion. Historical 230–233 contract `status` fields describe their preparation time; terminal QA receipts supersede those statuses, not the source contracts.

## Transfer together, not as isolated art

- `RecoveredSource/DeckRogue.GodotPort.Presentation/Hd2dAttackPrototype.cs`: exact Torrent factory/guard and connection.
- `scripts/presentation/AuthoredGeometryFx.cs`: exact local material selector and vertex HDR payload semantics.
- `shaders/authored_Hd2dOpticalFlow.gdshader`: new local additive material. Existing `authored_Hd2dPrototype.gdshader` remains byte unchanged.
- `assets/Art/fx/hd2d-light-torrent-density-v1.png`: unmodified 1536×1024 RGBA original, 1,552,252 bytes, SHA256 `38c80640f6064c222b6af340aa543c7856f9ebd6fc1193fe9c72e7819c96308d`.
- Existing shared FX/profile/presentation/audio dependencies and contracts 230–233 belong to the same coherent checkpoint. Godot import cache/settings are not the art deliverable. No new audio or profile timing change was made for this trial.

## 1. Route and ownership first

Apply this ONLY when all route fields match: key `card:white_light_torrent`, definition `white_light_torrent`, source kind `card`, family `light`, renderer `LightRayFx`, element `light`, FixedSmall=false. Material additionally requires effect shader `Hd2dPrototype`, palette `hd2d-light`, batch Kind=33, Space=0, texture `hd2d-light-torrent-density-v1`. Any mismatch/null retains the previous material. This is not a global Light/Kind33 substitution; shared LightBolt and old shared shader are unchanged.

Use actual per-hit source/receiver anchors and actual event identity. Keep the three executed hits and 0.22-second planned interval; do not invent a hit when lethal/omission removes an event. Native reference fixture is actual damage 9×3, not a hardcoded damage requirement. No new aggregate sequence owner.

## 2. Material/color pipeline

Dedicated material is unlit additive (`blend_add`), two-sided, no depth writes, fog disabled. Preserve the existing occlusion/depth-test policy; do not turn this into an always-on-top UI sprite. Port its linear-light radiance and alpha contribution to the project's Unity render pipeline, rather than copying Godot OUTPUT_IS_SRGB literally.

Decode texture RGB from sRGB exactly once; alpha remains linear. In a Unity linear project, an sRGB texture sampler usually already supplies linear RGB: do not manually decode it again. Let the pipeline perform final display encoding, not a second shader encoding.

With sampled linear RGB c, L=dot(c,(.2126,.7152,.0722)):

- veil = .16 + .24*smoothstep(.10,.60,L)
- filament = .80*smoothstep(.60,.92,L)
- radiance = c*(veil+filament)*max(vertexHDR.rgb)
- alpha = rawAlpha*vertexAlpha*smoothstep(0,.08,min(u,1-u,v,1-v))

The density coefficient spans .16–1.20. Preserve the broad interior plateau; do NOT add another center/cross-strip feather, diffuse shadow, or lavender multiply that collapses it to a line/cloth. Canvas-edge fade is the only extra texture-boundary fade. VertexHDR RGB = linear palette role RGB × per-vertex gain × scenario emission; alpha = clamped sampled mesh alpha. Body/connector halo use neutral role 3 and gain 1.05, core gain 1.35. Actual renderer emission was measured as 1.0 in the reference fixture. Do not apply it twice. The new shader has NO `_Emission` uniform: the value arrives via CUSTOM0; null uniform is intentional, not missing setup.

Use equivalent local additive composition, including alpha weighting, in Unity. Verify against the footage in the actual Unity HDR/tonemapping pipeline; no Unity rendering equivalence has yet been measured. Do not compensate by globally increasing floor light, exposure, bloom, or camera gain.

## 3. Geometry and timing

Port `CreateLightTorrent` as a whole. Main flow uses full-canvas 12×3 mesh (216 vertices), analytic receiving UV (.965,.515), curved/folding interior, and the existing camera-local source direction. Do not replace it with a rigid translated sprite. The analytic anchor is not an exact grid vertex; validate apparent tip registration in Unity rather than asserting pixel equality.

233 connection is per-hit, window [-.11,+.10] seconds relative to that actual beat. Onset Ease((age+.11)/.035); release Ease((age-.055)/.045). It holds through +.055, then source endpoint moves toward end and fades by +.10. Next beat begins its window at +.11, leaving a .01-second gap: this is NOT a continuously held inter-hit beam.

Negative end = existing moving head minus direction×existing span×.15, inside the same main flow; positive end = actual receiver-local anchor (0,.035,.025). Source is the existing cast emitter, never fixed screen coordinates. Both endpoints and the head clock stay as 232.

Connection center = lerp(start,end,u) + normal*.03*sin(pi*u)*(1-release).
Let entry=Ease(u<.70 ? u/.70 : (1-u)/.30).
- Halo: same optical body batch/material, 6 segments /36 vertices; UV u=.08→.965, v=.43→.60; halfwidth=.010+.002u+.034entry; alpha=(.12-.02u+.164entry)*connectionEnvelope; gain1.05.
- Core: 3 segments /18 vertices; halfwidth=.005-.002u+.005entry; alpha=(.30-.18u+.346entry)*envelope; gain1.35.
- Max body capacity468<512, positive252, core24. Keep shared endpoints/UV progression so seams do not create separate white ruler lines.

Main body/crest/release and all shader231 bytes are unchanged by233. No BattleStage/BattleActor timing, lunge, pixel anchor, camera, world light/floor constants, gameplay state, receipt order, audio, or profile interval changed in228–233. Local geometry/material changes above are the only integration delta.

## 4. Evidence and acceptance checks

- `REV233-STANDARD-TECHNICAL-POSTPROOF.json`: 1379 PASS, selector/actual shader checks and source protections.
- `REV233-DIRECT-MOVIE-TECHNICAL-POSTPROOF.json`: 1477 PASS, 100 consecutive actual FramePostDraw frames, fixed simulation30fps, duration3.333333s, silent H264.
- Movie `rev233-torrent-direct-movie-reused222/native-contiguous-30fps.mp4`, SHA256 `fa38b8d1a8a4e0dd5fa6264bb085601c6dfcfc639d45906dca4ba7873572f16c`, bytes1,241,735.
- Wall time34.6308634s includes instrumentation/readback: this is NOT a real-time performance recording. H264 is lossy; decoded pixels are not claimed byte-identical to captured RGBA.
- Actual first-positive movie frames (zero-based)13/19/26 correspond to receipts17/18/19; sampled ages .033332884/.013332784/.026665866. Planned contacts1.9458326/2.1658325/2.3858328 retain .22 spacing; frame quantization is not a new gameplay interval.

Unity final visual checks: (1) source-to-selected-target direction reads at normal UI scale; (2) platinum core plus broad translucent flow dominates while enemy/background remain readable, not a floor-wash substitute; (3) three real beats have visible dim troughs and final convergence/clear at normal playback. Check exact route negatives/LightBolt fallback, source and receiver movement, omitted-hit ownership, no duplicated connector, and no hard UV strip/cloth/ruler appearance. These are port acceptance checks, not new Godot feature work.

## Suggested commit-body integration paragraph

Light Torrent only: new local unlit additive optical material, linear luminance thresholds .10/.60 and .60/.92, veil .16+.24 and filament .80; neutral vertex density gain1.05, core1.35. Per-hit source connection window -.11→+.10s (0.22s interval unchanged), curved center offset .03, six-segment same-texture halo and three-segment core. Main flow and material remain exact231 outside connector integration. No BattleStage/BattleActor, world floor/camera, numerical damage, owner, receipt, or audio changes. Raw1536×1024 image retained unchanged. Godot authoring footage only; Unity motion/tonemapping acceptance remains user-side.


# Revision234 addendum — authoritative integration delta over233

Parent/Astra continuous native review now passes dynamic source attachment with small visible transform differences accepted; material and three-beat temporal gates remain intact. Final bounded lifecycle/phone compatibility is closed: movie1486 + lifecycle1426 + phone1376 = 4288 PASS /3 clean hosts. Parent selected234; phone is limited small-target/white-ground compatibility, not Unity visual certification. Revision233's material work remains unchanged, but its early sprite recovery is NOT the final reference. All preceding statements of “no Stage/Actor change” apply to228–233 only;234 explicitly changes the release drawing lifetime below. Frozen217 source is not modified by this sidecar.

## Actual-pose root cause and remedy

The old renderer freezes each cast launch origin at age >= -.15s (Light LaunchLead). The existing `PrototypeFireSourcePoint` is calculated from attack drawing3 and its focus UV, mapped through current Sprite transform—not from whichever idle/attack texture is currently displayed. White uses (86/112,65/84), blue (100/128,38/84), green (69.5/92,54/78). Consequently, simply thawing the generic emitter would not follow the raised idle prop correctly.

233 sampled attack3 on standard frames16/18/20 but idle0 on23/25/27; its third connector therefore emerged below the returned prop.234 retains the existing release drawing3 only for this exact Torrent action, instead of changing the material or moving its hit point.

### Stage: explicit owner-derived deadline

New `BattleStage.IsTorrentReleaseHoldRoute` checks Enabled plus the exact full Torrent route tuple above. Each presentation update, immediately after `ProcessOrdinaryContacts` and before visual sampling:

1. Clear all previous per-frame hold deadlines.
2. For each launched, unfinished action with an actor and a current existing `action.Capture` Motion token, consider only Direct, non-Rejected `GameEvent_DamageDealt` beats passing that full route predicate.
3. Set deadline to the maximum of `max(beat.ContactAt, beat.Contacted ? beat.LastContact : beat.ContactAt) + .10f` across those beats.
4. Pass deadline and the existing token to the actor. No new owner or hit is created. An absent/rejected beat does not extend it; future contact retiming and actual quantized contact are authoritative.

### Actor: only frame3 is extended

`SetTorrentReleaseHold` accepts only a token belonging to this actor's Motion, a current HD2D pose owner, live actor, exact `white_light_torrent` pose, and finite deadline. `TorrentReleaseHoldActive(clock)` additionally requires sprite pose `attack`, frame index3, and clock < deadline.

AdvancePose still advances original frames0/1/2 normally. Once frame3's normal duration has been consumed, it holds frame3 with that duration already consumed. At the first visual update after deadline/cancellation/token invalidation/death, ordinary advancement resumes; it does NOT restart frame3's .18-second tail. `BeginHd2dPose` clears the hold; per-frame Stage clearing and token invalidation prevent previous actions from retaining it. No shared LightBolt/caster-wide hold was introduced.

Numerical explanation, not magic constants in code: nominal first contact .40s, three hits at .22 interval => final .84s, connection end .94s relative to action launch. Old owned sprite durations .10+.12+.055+.18=.455s from sprite pose start. Sprite/action origins can differ and must not be conflated. Implementation derives actual beat deadlines, not a fixed .94 or hardcoded triple count. The .10 tail corresponds exactly to existing connector end; gameplay .22 spacing, hit amounts, receipt order and shader/geometry remain unchanged.

## 234 Unity implementation checklist

- Use the actual Unity release drawing/pose corresponding to the authored focus anchor. Hold its release section until the last existing eligible hit's connection end, using the same command/action ownership; preserve anticipation and recovery semantics.
- Do not globally pause the animation controller, hold unrelated Light spells, restart casting on every hit, or invent a final hit just to keep the beam alive.
- Abort on cancellation, death, owner replacement; do not leak the hold into a subsequent card. Preserve the real event list when hit count/interval differs or is truncated.
- Keep connector and main optical material geometry/shader exactly233 for this port step. Small transform movement is not claimed pixel-exact: compare each beat's source with the visible prop, especially the third release and final recovery.
- No generic source-unfreeze or dynamic-emitter callback was added. The fixed attack3 UV premise must be replaced with the matching Unity release-pose emitter, not copied as screen coordinates.

## Latest reference

`qa/rev234-torrent-direct-movie-reused222/native-contiguous-30fps.mp4` — 1,229,823 bytes, SHA256 `acefb563adab805299564b83de02b0fefb5832adfd4dae1bff6e85b8a9ac9c1e`; 100 actual consecutive fixed-simulation30fps frames, silent3.333333s. Actual wall time35.4437126s, NOT a real-time performance recording. Movie QA1486 PASS; actual3 receipts17/18/19 and planned .22 times unchanged233, all17 positive samples Committed/Exact. Parent/Astra dynamic①/②/③ visual PASS and final lifecycle/phone bounds are now closed; total4288 PASS /3 clean hosts. Source217 immutable; only BattleStage/BattleActor and new234 contract differ233.

## Additional commit-body paragraph REQUIRED for234

BattleStage/BattleActor integration: exact direct LightTorrent/current existing Motion owner only; retain attack release frame3 until max(existing scheduled/actual eligible contact)+0.10s, refreshed after contact processing. Original frames0/1/2 and all other routes unchanged. Frame3 normal duration remains consumed so recovery has no extra0.18s restart; token replacement/cancel/death/expiry releases it. Default3-hit explanation: contact0.40, interval0.22, last0.84, connector end0.94s (derived, not hardcoded); prior normal sprite frame durations0.10/0.12/0.055/0.18. No gameplay/receipt/camera/light change; optical shader/raw/mesh remain exact233. Unity must hold its matching release-pose emitter through the same existing event deadline.

## Delivery identity (selected234)

Native movie saved as new Library `libfile_a94c33bf72d08191b75dd822e2eb08c7`, version0; no previous movie replaced. Filename `光の奔流234_Godot演出試作_Unity未確認_無音30fpsシミュレーション.mp4`. Original MP4 byte SHA unchanged after Library identity xattrs. Receipt `qa/rev234-native-movie-library-receipt.json`. Parent sends the user this artifact with fixed-simulation/not-performance qualification. This delivery is not a Git publication receipt; verify the actual remote commit separately.

### Bounded QA qualifications retained

Phone case source-to-target world distance was 8.1663 versus standard6.3068: it is NOT a short-distance/oversize test. Small target, white/bright ground and UI-clear compatibility passed; no phone normal-speed movie was captured. QA reports source projection micro-difference2.32583 internal pixels, first5.6127; this is not pixel-perfect source locking. The LightBolt pose negative is an isolated production-function/pose test, not an actual engine LightBolt play. Do not expand any of these claims when describing Unity port readiness.
