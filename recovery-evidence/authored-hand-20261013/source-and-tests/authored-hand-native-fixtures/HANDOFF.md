# Authored played-hand fixture handoff

Status: FIXTURE/ORIGINAL-ENGINE VALIDATION PASSED. NATIVE ACCEPTANCE NOT RUN.

Only three Stage41 authored IDs are covered, one original PlayCard command per full input RunSave:

| Input / actions | Exact played UID | Original leader | Resource / target outcome | Style / destination |
|---|---|---|---|---|
| `slash.save.json` / `slash.actions.json` | `authored-slash-played-001` | `leader_green` | energy3→2, enemy1000→994 | `green_strike` / slash / discard |
| `water.save.json` / `water.actions.json` | `authored-water-played-001` | `leader_blue` | energy3→2, enemy1000→995, draw1, ice block0→1 | `blue_strike` / water / discard |
| `stomp.save.json` / `stomp.actions.json` | `authored-stomp-played-001` | `leader_green` | energy5→0, enemy1000→950 | `green_finisher_stomp` / stomp / discard |

Every input has a distinct same-definition sibling ending `-sibling-002`. Only `-played-001` leaves the hand. The sibling may re-layout normally; it must not acquire the played UID's authored retirement. Target0 is a surviving original `enemy_mudling`, block0, maximumHP1000. Original leader HP/passives and unmodified card definitions are retained. The fixed resource, draw-pile and enemy HP setup is QA state, not a change to content or rules. Water draws the original `blue_guard` UID `authored-water-draw-004` from a nonempty deterministic draw pile, avoiding a shuffle. Both run and combat RNG remain unchanged.

All schedules dispatch once at recorder frame30, with target0. `idle.actions.json` is available only for a separate pointer-operated run; do not combine scheduled and pointer dispatch. `*-expected-after.save.json` are reference outcomes, not input fixtures. `*.contacts.json` preserve every original event and receipt, including Water's player ice-block receipt.

## What was actually verified

- Six complete `Report.SerializeRunSaveFile` / `Report.ReadRunSaveFile` roundtrips with the matching original content fingerprint and event-log boundary.
- Each input is reopened from disk and passed to unchanged `SessionController.Load`, then unchanged `SessionController.Dispatch`.
- Fresh controller load revision1, admitted PlayCard revision2; distinct cloned outer and inner command objects with identical UID/target/command values.
- Full resulting RunState equality with `Run.ApplyRunCommand` from the loaded save; unique UID and expected hand/discard membership; target survival; one direct damage receipt; unchanged RNG; exact original costs; natural Water draw and leader passive.
- Three default SourceShowcase profiles have the expected non-null authored style. No native Stage registration, descriptor invocation or Tween execution was run.
- Original engine comparison: all25 frozen engine source files byte-equal to the live runtime at preparation. Build references and copied source-data hashes are recorded in `source-provenance.json`. The generator is pinned to copied immutable references/data under this new directory and does not reread or write live source.

## Native handoff and blockers

Follow `NATIVE_SCHEDULE.md` and `CAPTURE_GATES.json`. The currently inspected `UnityParitySequenceQa.cs` loads these saves/actions without changes. Its present frame rows omit the exact retirement Tween start/elapsed/completion trace, tile alpha/instance ID, UI epoch, frozen plan/destination and accepted/rejected receipt counters. Existing sequence `results.passed` alone cannot pass the authored hand native gate. Runtime owner must provide read-only observation of those fields, preserving the baseline/candidate native schedule. No QA loader change or GUI execution was made here.

Default discard coverage is ready for native execution. An original-engine exhaust case is still BLOCKED within these three unchanged IDs: all three base definitions and their actual original one-time upgrade results have `Exhaust=null`. `Combat.PlayCard` chooses exhaust from the effective definition; its brand-only override does not apply. Dynamic fusion gives `fused_...` IDs outside the exact profile lookup. No legitimate same-ID exhaust route was established in this bounded audit. Do not set `Def.Exhaust`, fabricate a post-command pile mutation, or relabel a fused card to count coverage. `exhaust-reachability.json` records the actual upgrade results. An independently proven existing engine route would be needed before an exhaust fixture can be added.

Not covered by these three single-command fixtures: cost-exhaust sibling removal, queued A→B ownership, playing during deal, reset/resize/modal cancellation, reshuffle same-UID replacement, missing-anchor fallback, generic/set/permanent and showcase-disabled controls. The original package's broader native procedure remains pending; this fixture handoff does not replace it.

## Reproduce fixture validation only

From workspace root:

    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/authored-hand-native-fixtures/BuildFixtures.csproj -- "$PWD/ui-parity-restoration-20261013/authored-hand-native-fixtures"

This overwrites only this package's generated fixture/reference JSON files. No GUI, native acceptance, integration, engine/rules change, live runtime edit or older frozen-package modification is included.
