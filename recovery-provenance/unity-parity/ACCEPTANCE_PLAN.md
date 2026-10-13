# Unity parity review, 2026-10-13

Base: reviewed recovery `bd66ae1655023fa3bc57f89ed217ce404fcdf54b`.
Branch: `codex/unity-parity-20261013`.
The delivered recovery and frozen snapshot remain immutable.

## Reference identities and limits

The local original `DeckRogue-unity-ux-ref-a7f86b5.zip` contains Windows Unity 6000.6.0f1 reference evidence. Its `ux-target3-video.mp4` has 332 actual encoded frames at 30/1 fps, 1920×886, duration 11.066667 s. README, STATE, layout and sequence JSON identify seed4242, 60 Hz deterministic capture, every second source frame, UI phone1.6. It documents actual EventSystem pointer paths; those are Unity automated QA inputs, not physical device testing.

Local Unity source in `deckrogue_cloud_migration/worktree/unity` has Git HEAD4bdf5d4 plus uncommitted edits. a7f86b5 is not present as a Git object. Exact files used must be individually hashed and described as an available source ancestor/current dirty source. No Unity Editor was found in the cloud. Do not claim a new Unity run or exact a7 source verification.

## First fixed-state capture

Leader green, act1, starter, difficulty3, seed4242, gold150, HP48/80, block3, energy3, mudling trio, hand strike/guard/growth ring/reaction thorns/double lash. UIDs follow the reference Autopilot pattern. Full fixture JSON and SHA are retained. The first native sequence starts at the reference equivalent4.6 s, plays strike on middle enemy at frame30 (reference306/5.1 s), and EndTurn at frame123 (reference399/6.65 s). Original command dispatch isolates presentation; this first sequence does not assert pointer/drag parity.

Godot runs normal scene `_Process` and native render loop with official `--fixed-fps 60`; every `FramePostDraw` writes a real1170×540 PNG and per-frame JSON. Process counters must be consecutive and host deltas checked. No duplicate/interpolated frames are used to disguise missing capture. Encode unchanged frames at60fps; compare every other frame to the Unity30fps source. This is deterministic temporal visual proof, not real-time/device performance.

Metadata: loaded module MVID/hash, fixture hash, process/render/presentation counters, host delta, wall duration, sprite texture/pose/frame/frame time, motion/recoil offsets and elapsed, local pause, source feet, camera and contact-event onset. Expand shader/light probes separately as required without modifying production behavior.

## Acceptance gates

1. Baseline diagnosis: review continuous video and ordered frame strips, track source sprite hold durations, first motion, contact, recoil, finish, and next-turn release. Never infer smoothness from rule tests or selected stills.
2. Integrate sprite-clock correction alone; same state and capture cadence, exact sprite-change timing matrix at30/60/90/120Hz plus stalled frames. Preserve source-authored40ms cap and one-frame advancement unless source evidence disproves it.
3. Integrate bounded VFX/lighting differences alone; verify source/target/floor light contributions, actor silhouette and contact timing; compare green strike, double lash, water trail and terminal death. No arbitrary power boost or bloom workaround.
4. UI source inventory and paired fixed-state overlays at100% and130%: intents, riders, targeting, H5/H10, choice/detail/menu and full modal return paths. Actual CUA hover/cancel/play/EndTurn stays a separate gate.
5. Live cadence diagnostics: no fixed-fps, log every natural render timestamp. Cloud llvmpipe results are environmental and cannot prove Android GPU smoothness. Capture overhead must be reported; metadata-only live capture isolates image-write cost.
6. Aggregate invariance and production exclusion: all original engine/RNG hashes unchanged; rule fixtures, one-command-per-play, cancel/reset and terminal fences pass. Final production assembly excludes QA types, and source evidence remains reproducible.

A remaining omitted feature is a documented gap, not a parity pass. Improvements beyond Unity are separate, justified by a demonstrated source defect and reviewed after source behavior is established.
