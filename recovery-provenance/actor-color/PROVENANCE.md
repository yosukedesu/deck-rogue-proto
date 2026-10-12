# Reconstructed actor color correction

## Status

This is an off-tree reconstruction of the previously accepted Act1/2/3 provisional actor display correction. It is not the lost b05a0da source. The original baseline recovery, APK extraction, and pristine decompilation remain untouched.

The candidate uses the buildable recovered baseline in `source-recovery-20261012/recovered-project`. Three pre-existing C# files have bounded call-site edits. New partials provide the display binding and stage baseline toggle. No engine, gameplay event, clock, RNG, UI, stage geometry, atmosphere, or light-array equation is changed.

## Recovered exact inputs

- `shaders/source_actor_display_functions.gdshaderinc` is the exact recovered native-evaluation helper, SHA256 c12492a79a70c75d624dc2bc1fd149095a702fa599433c98a950cf8c67ead602.
- Shader baseline is the recovered early-APK `authored_StageUnitLit.gdshader`, SHA25665dfa792d59517613f2485d5c3f5000cbc5b6a477dd3048987a76d2ffde6efbf. The new finish call site is reconstructed.
- SourceActorDisplayMath.cs preserves every equation of the recovered standalone evaluator. Only namespace/provenance adaptation differs. It does not match the lost production file hash; see math/README.md and fresh receipts.
- The eight Act2/3 source look JSONs were recovered with exact pre-loss accepted hashes. Act1 effective parameters are recovered from the early APK look layers, which reproduce the accepted source profile. Fixed functional fingerprints are embedded in the wrapper; they are not derived from runtime input and thus cannot validate themselves.

The complete original production license-notice file was not recovered. Historical source/license attribution is preserved in the recovered helper PROVENANCE.md; this bundle is for local recovery/validation, not a claim that release-notice recovery is complete.

## Reconstructed behavior

All3 explicit source profiles are enabled by default only under actual gl_compatibility rendering, Filmic exposure1.12/white1, no adjustments or camera attributes, and exact source post fingerprint. Unknown/missing/changed functional values reject the transfer. Underscore documentation fields and numerically equivalent integer/float JSON representations are accepted.

Raw source ambient and source display finish switch atomically. Key light remains explicitly source-linear. No source texture, normal/emission map, flash, dissolve, local/attack light array, or modulation changes. The finish returns inverse-GL ALBEDO directly; the baseline keeps its original authored_output_rgb wrapper.

Source grade order is vignette→exposure→full-color ACES/clamp→LDR32 RGB8 LUT→output, compensated against the guarded GodotGL display path. This remains an actor-only provisional SDR transfer. It does not restore shared source HDR/bloom/DOF. Scene glow can spread changed actor radiance outside alpha; this is not grounds to dim source hit lighting arbitrarily.

Each actor owns and retires its GPU LUT. Act1/3 share identical managed LUT bytes; Act2 is separate. A live actor changing2→3→1 must update family, exposure/vignette and raw-ambient state. A built-in TreeExiting signal closes resources without relying on unavailable source-generator output for a new virtual callback.

## API and test seams

- BattleStage.SetActorSourceDisplay(bool), ActorSourceDisplayRequested.
- BattleActor.SetSourceDisplayTransferEnabled(bool), SetOtherActSourceDisplayEnabled(bool), RefreshSourceDisplayTransfer().
- Actor diagnostics: SourceDisplayTransferActive, SourceDisplayTransferStatus, SourceDisplayTransferRequested, OtherActSourceDisplayRequested.
- `--actor-color-baseline` overrides all source requests. `--actor-color-other-acts-baseline` disables only2/3.
- SourceActorDisplayTransfer.MatchesProfile takes the **post object**. CopyExpectedPost returns a fresh clone for independent mutation checks. CreateSourcePost/GetGeneratedLutSha256/CopyGeneratedLutBytes expose deterministic CPU values. EvaluateEnvironmentContract is the pure version of the actual live guard.
- Status IDs: act1-source-ldr32-gl-v1, act2-source-ldr32-gl-v1, act3-source-ldr32-gl-v1, otherwise explicit legacy reasons.

## Fresh verification

- Off-tree recovered-project C# build succeeds:0errors,13 original-engine nullable warnings.
- Pure actual-wrapper contracts:72/72, covering all3 profiles, missing/extra/NaN/numeric keys, LUT hashes, and environment refusal paths.
- Independent math driver regenerates both exact LUT hashes and all3 retained dark color triples; see math/evidence.
- No native shader/material/lifecycle or new pixel parity claim yet. Runtime worker must validate the bundle on a restored actual runtime before adoption.

## Required native gate

1. Same frozen pose/camera/light baseline→candidate→baseline in Acts1/2/3 idle and actual damage contact.
2. green/blue/white leaders and enemies; source textures/normal/emission/light inputs unchanged except atomic source ambient API semantics.
3. Same live actor2→3→1 and unsupported post/environment, global/per-act baseline toggles, teardown and reentry; confirm LUT bytes/EV/status and no source-material exceptions.
4. Restored baseline image identity. If glow changes contact pixels outside actor masks, retain the strict failure and use a glow-off causal control rather than silently broadening masks.
5. Exact run/save/RNG invariance and normal-Main idle/contact/settled views. No exact Unity Act2/3 optical parity claim without matched source captures.
