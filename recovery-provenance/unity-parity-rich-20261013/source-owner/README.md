# Explicit damage owner routing

One guarded predicate correction. No timing constant, clock, event, rule, RNG, actor motion method or grouping algorithm changes.

## Source

Manifest-verified Stage41 cumulative source over PC `2f31050`, not exact `a7f86b5` source:

- `Assets/Game/CardVfxProfile.cs:109–113`, `IsCardDamage`: when `PresentationSource` exists, matching kind and definition determine ownership; `SourceUid` is considered only when presentation metadata is absent.
- `Assets/Game/Presenter.cs:1615–1643`, `PlanCardHits`: direct-card runs are grouped into volleys by repeated target; all targets inside a volley receive the same `HitPlan.Index`.

Current Godot `OwnsDirectHit` incorrectly requires a null legacy UID even when explicit card metadata matches. The correction preserves the source's legacy fallback, and continues excluding enemy, mismatched card, reaction, gear, permanent, necro, status and Burn metadata.

The fixture named Burn uses `kind=status, definitionId=burn`; it tests that an unrelated tagged status contribution cannot consume the card's parity. Legacy untagged player damage with a null UID is inherently ambiguous and remains accepted, exactly as source fallback does. This patch does not invent missing provenance.

## Apply

Runtime owner only, after frozen capture work:

`python3 unity-impact-parity-20261013/owner-routing/apply_guarded.py runtime-recovery-20261012/project`

Add `--apply` after reviewing the dry run. It replaces exactly one matched predicate and refuses an unexpected seam. Never copy the full candidate file into the working tree; that file exists solely for source extraction and compile checks.

## Evidence

`tests/extract_methods.py` extracts the corrected production predicate, the unchanged production volley-assignment loop, and the source predicate verbatim. `tests/Ownership.Tests.csproj` compiles these with the actual generated `GameEvent_DamageDealt` and `DamagePresentationSource` records, not imitation event types.

1,373 assertions pass, including 675 source/metadata/UID combinations, generated-event JSON roundtrips, multi-target indices `[0,0,1,1]`, repeated targets, interleaved status/permanent/gear/mismatch/enemy/reaction events and explicit-card ownership with a stale legacy UID. See `../evidence/source-owner-routing-tests.json`; extraction hashes and exact methods are in `tests/extraction.json`.

These are managed seam tests. Native mesh parity, ticket/volley mapping across actual callbacks, and source-side water geometry still require the runtime owner's native trace. Compile success is not native visual acceptance.

## Source-actor selection companion hunk

`source-actor-routing.patch` / `apply_actor_guarded.py` changes only the BeginBeat retainer-source lookup. Stage41 CardVfxProfile.cs107–108 and Presenter.cs721,2073 give explicit metadata priority over a legacy UID. Matching permanent metadata uses its own UID; card/reaction/gear/status and other explicit kinds do not revive inherited UID; null metadata preserves legacy lookup. Enemy actor selection and missing-retainer hero fallback are unchanged.

`actor-tests/ActorOwnership.Tests.csproj`:4,618 assertions pass with real generated events, exact production selector expression, and named actor stand-ins instead of native objects. It covers contradictory explicit/legacy owners, permanent matching/missing actor and UID, nonpermanent kinds, enemy source, and JSON roundtrips. Source extraction/hashes are in actor-tests/extraction.json. Both ownership hunks compile together with0errors,15existing warnings. Native actor/source-side traces still remain pending.

This companion hunk was prepared after the04:22 private supplement; that immutable Library version does not include it.
