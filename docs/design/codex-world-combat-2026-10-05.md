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
