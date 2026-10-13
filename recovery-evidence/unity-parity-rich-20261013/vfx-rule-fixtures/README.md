# Real-engine VFX fixtures

Five production-loadable synthetic QA setups, each dispatched through the original SessionController and original content definitions. No live project or engine changes. Engine/source provenance is the byte-identical source snapshot in ../rule-ui-fixtures-20261013/source-provenance.json; this harness reuses that snapshot through a relative symlink. Native rendering is untested here.

fixtures/<id>.json is the starting save, <id>-actions.json is the native schedule (play at frame30,target1). expected/<id>.json is complete committed engine output; validated-events.json contains actual original-engine ordered events, commands and fixture SHA256. Load with AutoSave=false in an isolated QA window. Frame budgets must cover the complete sequence; vine dance has20 damage receipts, not just5.

- green_double_lash: two card-owned hits on target1; expected authored indices0,1.
- green_horn_storm: two three-target volleys; expected indices0,0,0,1,1,1. Check actual route supports intended geometry; no effect mapping overridden here.
- blue_strike: one water card hit. This verifies player-origin rendering only; source-right/left geometric mirror requires separate stage probe, not a fabricated enemy spell.
- green_sweep with white_perm_hound: six card hits then one tagged permanent hit. This is mixed ownership but NOT interleaved.
- green_sig_vine_dance with green_perm_thorn_canopy: five iterations of three permanent-owned hits on enemies0/1/2 followed by one card-owned hit on target1. All20 damage events come from actual engine triggers. Expected card-authored indices0,1,2,3,4; permanent hits must not consume them. This is the genuine interleaved ownership fixture. The canopy is not falsely described as a retainer.

QA assumptions: hand is deliberately replaced by the one tested card; enemies'HP/MaxHP=100 to prevent premature death; energy=10; growth/momentum=0; selectedpermanent is explicitly injected as a valid original-definition instance. These are synthetic preconditions, not a organically played run. Original card rules and RNG are used for all commands. No contradictory stale source UID is fabricated: test those via the separately labeled generated-event selector suite.

Reproduce:
source ../runtime-recovery-20261012/env.sh
dotnet run --project runner/Validate.csproj -- "$PWD"

Assertions: successful load, exactlyone revisionpercommand, expected own-card counts2/6/1/6/5 and tagged permanent contributions in mixed fixtures. Additional native frame/vertex/source-owner evidence remains the runtime/VFX owner's responsibility.
