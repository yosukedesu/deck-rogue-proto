# Forest causeway and world combat, 2026-10-05

## Evidence and interpretation

Official Unreal Fest Europe 2019 slides: https://epicgames.ent.box.com/s/i7p4at5sc36d0hd2tzekqe0qz8f6xhv0 . Downloaded the 98-page, 10,133,281-byte PDF and visually inspected pages 17, 23, 26, 29, 32, 35, 43, 47, 52, 57, 63. Page 47 is black/embedded video and was NOT treated as watched video. Local reference only; source artwork is not included in the project.

The applied lessons are placement density and scale; actual depth in structures and banks; material variation at more than one scale; authored texture shading plus real lighting; foreground/middle/background separation; and controlled world post-processing. Fixed camera is a user accessibility preference, so the source game's zoom/focus shifts were deliberately not reproduced. Timing values below are original tuning, not measured source-game timings.

## Implementation

- Original mossy causeway, irregular raised paving, broken low parapet, drainage bed and bank, thick incomplete stone gate and warm practical lamp. The battle seats remain at their established height. Existing tile and foliage assets, shared materials, merged geometry; no copied source-game assets.
- `StageCombat` owns world offsets on the unit visual. UI rectangles remain the layout authority and do not inherit attack/recoil transforms. Approach stops short of target using actor/target scale; anticipation 120 ms, arrival at 320 ms, local hold, return. Multi-hit melee retains its stand-off position through the sequence. Existing attack/hurt/block animation poses remain in use.
- World VFX uses the original generated atlas on pooled, depth-tested transparent mesh quads, ordinary URP forward rendering, fog and world post-processing. UI is composed later. Pool caps 24, low quality 12. No added custom renderer.
- Representative hit shape and contact burst are separate. Spells and enemy ranged attacks use a world projectile. This is a foundation: not every weapon/element has bespoke motion/art yet.
- Existing damage events trigger VFX, reaction, audio, light and displayed HP together through the existing sequenced presentation path. Engine calculations and save schema are unchanged. Played cards leave toward the discard pile before contact.
- Disabled global time-scale hitstop and camera shake/zoom/dolly in the world-combat path. Pose holds are local. Reduced sprite white flash and removed its duplicated UI halo. Existing hit light pool now has restrained radius, duration and intensity. Mobile hit shadows remain disabled pending device measurement.

## Validation and boundaries

Windows native player, explicit `-shots` isolated save path, seed 4242. Scenarios: forest, axe, heavy, guard, hurt, multi-hit light spell, four enemies. Capture script checks camera matrices, final world offsets and layout errors. Normal run.json, run.json.bak, reports/autosave.json and autosave.md hashes must remain unchanged.

Source-level verification confirms depth test LEqual, no depth write for transparent VFX, and alpha-cutout shadow/depth passes on units. Actual Frame Debugger inspection and Android GPU/thermal/frame-time measurement are not completed; do not claim those gates passed. Deterministic captures use 60 Hz simulation, not proof of achieved 60 FPS. 30 FPS pacing has not been measured.

The first distributed APK remains untouched. No remote push or image upload is part of this implementation pass.


## Cloud masonry integration — 2026-10-05

Source: Library libfile_1215dfb2dcd08191a2e6568d5fb16f1a, DeckRogue_Cloud_ArtKit_v1.zip.
The original Blender FBX is retained with its mesh names. Editor CloudKitSetup combines 38 selected meshes into gate, pillar, stairs and rocks, relocates each module into the forest, and binds the two supplied 128px PNGs to two shared DeckRogue/StageModule materials (URP forward, shadow, depth and project fog). The five presentation paving slabs are not duplicated into the existing causeway.

FBX changes source X handedness. Module origins are normalized from imported bounds rather than assuming GLB/source transform signs. Gate is scaled to about 4.2m tall. Modules have four renderers; no new shadow-casting light or full-screen render pass was added. The source FBX also stays in Resources for reproducibility; generated meshes/prefab are committed and builds do not require Blender.

Rebuild derived assets with CHECKS=DeckRogue.EditorTools.CloudKitSetup.Run scripts/unity-win.sh checks, then copy Assets/Resources/CloudKit from the disposable Windows project back into the WSL source before the next sync. Existing GUIDs are retained on regeneration.

Validation: Windows player build Succeeded, 0 errors, 3 warnings, 203MB. Editor shader inspection now uses ShaderUtil.GetShaderData passes rather than Material.FindPass, which is not reliable under Null graphics. Verified Forward, ShadowCaster, DepthOnly, DepthNormals and no ShaderHasError. Canopy/atlas checks pass (5630 changed texels).

Default-camera and diagnostic-camera forest/single-enemy plus four-enemy captures: no layout errors, missing resources or diorama gate failures. Diorama: 95711 triangles, 46 renderers, 12 materials, 13 dynamic entries. Source/reference before new modules was 94215 triangles, 42 renderers, 10 materials. These are scene counts, not measured Android frame times. Camera and combat behavior are unchanged by this integration.

Local captures only; screenshot upload was not retried. Ordinary save data was hash-checked against the existing post-incident snapshot; four files unchanged. No original pre-incident save has been recovered or restored.

Art source decision: gold/teal AI atlas libfile_4148c58b5e888191b0a02d207b61d8e8 was materialized but rejected by the user and is NOT in the repository. Silver physical atlas libfile_afd83f305aa48191830f6125a2aff238 is integrated as a 1024x512 4x2 atlas. Fixed core pivots and transparent padding are applied per cell, alpha is raised to 1.4 to reduce broad haze, and RGB is preserved. Arcane and guard SVG atlases are converted to neutral luminance plus original alpha, then tinted with one technique family color at runtime. This avoids the rejected simultaneous gold/teal color mixture.

Reference video observation was performed by parent, not this local agent. Local Computer Use skill was read, but its required node_repl is not exposed, nor is a browser automation tool. No claim of local video playback. Parent Steam observations distinguish preparation, contact illumination and recovery; long-distance melee travel is not required for every attack. Current CardHitStyle families: slash, fang, horn, vine, stomp, spark, spell, light. Current spell=Mana, light=LampGlow, spark=Ember; other melee currently uses paper/silver or warm white. These need deliberate family-specific revision rather than a universal crescent.

Unverified: real Android GPU/30fps/frame debugger/shadow cost, actual APK from this commit, full attack art/timing redesign. Do not infer 60fps from deterministic capture time.


### Layered VFX integration

Physical slashes use eight frames over 0.34 seconds and a small 0.13-second contact accent. Existing pool remains 24 slots / 12 at low quality. Textures add 6 MiB RGBA GPU storage; no mip/CPU copies. All layers remain depth-tested, fogged and world-rendered before UI. Per-slot texture/UV/color uses MaterialPropertyBlock without making a material per strike.

Magic now has caster/target ground preparation rings, a projectile, a vertical target burst and ground contact ring; it no longer uses the physical crescent. Stomp/vine use a ground impact and only 0.18 body-height approach instead of crossing the entire stage. Normal slashes retain the existing approach. This is a scoped reinterpretation, not a claim to reproduce every original attack or attribute. More elaborate flame/lightning imagery remains unimplemented.

Video evidence supplied by parent: Steam original second trailer Sweep at roughly 56.6–57.9s and Fireball at 71.6–73.1s, plus About Thunderbird 2.88s/25fps clip. Observed separation of preparation, contact illumination, residue and recovery informed these changes. No local playback claim. Camera remains fixed, UI is not paused, and hit highlights recover quickly.

Phone-aspect Windows check (1920x886, uiscale1.6, tier phone) passed stage gates: 79037 triangles, 40 renderers, 12 materials. This does not substitute for testing Android hardware.


## Final priority override: rapid card input

The user's later instruction makes Slay the Spire-like card cadence higher priority than finishing an attack animation. The earlier 0.32s contact and 0.86s sequential presentation policy above is superseded. HD2D commands now rebuild the authoritative board and hand immediately and present only nonblocking visuals. HD2D enemy phases and final combat transitions also skip the old input-blocking sequence; entrance bands no longer gate HD2D input. Legal targeting/energy rules are still enforced by the unchanged engine.

A new presentation generation invalidates scheduled visuals from older commands; existing world VFX slots and movement offsets are reclaimed/reset. Card-local scheduled delays are capped, preventing unlimited visual backlog. Impacts begin at 0.12s, normal approaches are capped at 0.4 body height (stomp/vine 0.18). Dead targets become unclickable immediately, HP is rebuilt from resolved state, and stale death callbacks cannot kill a living replacement. HD2D hitstop remains local only: no timeScale manipulation. Ordinary gameplay's run-in-background setting is unchanged; explicit autopilot checks enable background execution to avoid focus stalls.

Validation: AutopilotTempo exercises actual EventSystem card click handlers, with an independently applied engine command as the expected result. Eight cases passed twice: five rapid cards, five attacks on one enemy, death during a chain with retargeting, AoE then single target, insufficient-energy repeat, end turn immediately after a chain, drag cancellation then play, target-selection cancellation then play. Final run: 25 clicks, all effects/consumption resolved before returning from the input handler in the same frame. Handler-to-effect duration 2.058–77.127ms, mean25.441ms on this Windows PC while Android compilation was running. This is a handler measurement, not physical touch latency or Android FPS. Independent visual draining was 29 frames for normal cases and55 for the enemy turn, about0.50–1.00s wall time; it did not block any following card. No inputblock objects or timeScale changes were permitted by the assertions.

Evidence: task workspace tempo-final-check/tempo-check.json and tempo-final-build.log (Windows build 0errors/16warnings). All tests use isolated -shots/player-data. Final APK must be built from this newer tempo revision, not d1d049b. This remains the incremental forest improvement build, not the separately planned full Blender stage.

## 2026-10-05 representative strike review (Windows / phone profile)

The previously delivered 9e4d084 APK is not replaced in this checkpoint. The new result is a six-second, silent, actual game capture for visual review: DeckRogue_strike_review_v2.mp4, Library libfile_41b507b756d8819182b183224340b852. It contains 180 consecutive rendered frames at fixed 30 Hz, 1920x886. Single green_strike at frame 30; three further UI clicks at frames 90/91/92. This is not Android real-time performance evidence or an interpolated screenshot montage.

Causes addressed: starting another card invalidated pending visual callbacks; world combat bypassed the old strong StageMotion light while hitLight was reduced to intensity 1.25/duration .16/hold fraction .018 (2.88 ms plateau); the alpha implementation used Unity SmoothStep like an edge-based shader smoothstep. During this revision a proposed .68 hold expression would also have reduced starting alpha to .32, so it was corrected before this review capture. The delivered HEAD used .15, not .68; do not attribute the .32 starting alpha specifically to that APK.

Changes: a combat-lifetime impact queue survives new cards, but resets when combat is reset; damage/energy/hand still resolve immediately, independent of visual impact at 120 ms. The attack animation starts immediately, steps only .4 body height, sends a thin directional amber trail, then a silver slash and five asymmetric sparks at the target with a point-light floor response. Slash peak fades after .28 of its .30-second lifetime. Light intensity 6, duration .28, plateau fraction .32. Latest effects recycle oldest pooled residues instead of disappearing when the bounded 24/12-slot pool fills. Final lethal contact is shown for 480 ms before leaving the resolved battle. No camera motion or global hitstop added.

Verification: Windows build succeeded, 0 errors / 16 warnings. HD2D checks passed (10 shaders / 348 conversions / 0 failures / 6 warnings; stage layout OK). Final phone-profile tempo suite: 9/9 cases, 26 UI click attempts, 25 accepted, all authoritative state changes in the calling frame; final lethal, rapid5, same target, mid-chain death, AoE then single, insufficient energy, end turn, drag cancellation and selection cancellation. Handler mean 24.52 ms, maximum 92.48 ms; these are synchronous UI-handler measurements, not input-to-visible-hit latency (legacy JSON key inputToEffectMs). There is no animation gate, but CPU/UI handler optimization remains a performance concern. Visuals drained in 26-41 deterministic 60 Hz frames. Actual recording log confirms energy 4/3/2/1 and target HP 37/31/25/19 on the four scheduled clicks.

Current validation used isolated player-data. Four normal save/report files match the world-save-before baseline. This does not retroactively undo the earlier save incident already reported.

Limitations: no Android rebuild/device profiling, audio capture, subjective quality approval, or full attack-library retuning in this checkpoint. The newly received Blender stage v0 and front-facing Mizore pack v1 were materialized, read and visually inspected, but are not integrated. Further roots/contact and character-proportion corrections are pending upstream. No remote push, deployment, new APK delivery or video Drive upload.
