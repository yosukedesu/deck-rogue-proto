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
