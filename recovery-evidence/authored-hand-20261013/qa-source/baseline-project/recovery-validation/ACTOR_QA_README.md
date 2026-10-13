# Native color QA handoff

`RecoveryActorColorQa.cs` compiles against the off-tree candidate with zero errors/warnings. It is test-only and has not been run natively by its author.

Runtime owner integration:
1. Include this file only in a `RECOVERY_QA` build (or wrap compilation with that symbol).
2. At the end of the existing normal Main._Ready, call `DeckRogue.GodotPort.RecoveryQA.RecoveryActorColorQa.Start(this);` under `#if RECOVERY_QA`.
3. Launch actual Compatibility GUI with `-- --recovery-actor-qa --recovery-qa-out <new-absolute-output-directory> --ui-phone -uiscale 1.6 --stage-atmosphere-baseline`.
4. The runner waits3s, applies Root.Size1170x540 plus1600x900 CanvasItems/Expand, then checks actual captured PNG dimensions. World/actor inputs and SceneTree are frozen for pairs; the real normal-Main Stage and UI remain in use.

The runner creates an isolated GameSession under the output directory and rebinds the normal UI/stage. Normal save files are not written. Fixtures use original public CreateDebugCheckpointRun/DebugLaunchCombat and original card definitions. Explicit fixture-only hand/energy/enemyHP inputs ensure a positive nonlethal green_strike; full input saves are written for review. Every command's outcome is independently compared with original Run.ApplyRunCommand before presentation.

Captures: all3 acts baseline/candidate/restored idle and actual first contact, then settled; blue/white leader idle pairs. Native protected signatures include run/save hashes, camera, lights, actor texture/transform and all unchanged source uniforms. Restored full RGB must be exact. Actor-only idle isolation uses alpha0.4 source texel projections with2native-pixel boundary allowance. Contact glow outside-alpha changes are recorded honestly; if present, a glow-off causal control runs and must eliminate outside changes.

Live guards: one actor2→3→1 with actual GPU LUT hashes/EV; unsupported post exposure/environment; per-act/global baseline precedence; owned texture exit/disposal; reset/reentry. Final output results.json records all assertions, actual per-capture uniforms/LUTs, PNG hashes, and failures. Any failure exits1; success exits0.

No source timing/UX parity claims. The baseline is still early APK scheduling. These fixtures are explicitly constructed and are not claimed to be the lost H5/target3 native references. Pixel comparison covers this restored run, not a missing original screenshot.

Do not broaden masks or weaken gates to hide a failure. Preserve the first-attempt output and report whether the defect is harness, inherited baseline, or reconstructed adapter. If baseline drift comes from an unpaused shader/time source, diagnose before rerunning.
