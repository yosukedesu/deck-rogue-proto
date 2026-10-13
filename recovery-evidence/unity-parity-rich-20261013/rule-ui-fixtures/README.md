# Native UI rule fixtures (2026-10-13)

## Validated scope

12 isolated loadable `kind: run` saves; 20 successful real `SessionController.Dispatch` commands, plus 4 atomic rejection checks. Generated using an off-tree byte-identical copy of the authoritative working-tree engine, Core session code, data, FlowRules and PlaySelection. No project, engine/rule/RNG source, live user save or GUI was changed. Source files and input provenance are in `source-provenance.json`; fixture hashes in `manifest.json`. This is engine/selection-helper evidence, **not a native-pointer pass**.

Run `./validate.sh` to regenerate and assert results. Actual expected post-command saves live in `expected/`; complete command JSON, revision deltas, hashes, and readable states are in `validated-command-outcomes.json`. Rejections are in `validated-rejections.json`. The original source has compiler nullability warnings; execution passes.

## Loading contract (runtime worker owns all actual UI work)

Use existing RECOVERY_QA `UnityUiManualQa` with `AutoSave=false`, fresh isolated output directory, and `--parity-fixture <path>`. For the currently running QA session, runtime may copy one fixture to `unity-parity-20261013/ui-manual/current-fixture.json` then press F8. F8 is an explicit reset and increments revision: record the new baseline R afterward; never compare absolute revisions across F8. The loader selects enemy index 1. F9 writes controls and save state; capture before and after each cancellation/confirmation. Do not point the test at a user save directory.

For committed outcomes, run `./verify-capture.py <F9-save.json> expected/<case>.json`. This compares the whole authoritative `run`, allowing session-only wall-clock metadata to differ. For a cancellation use `./verify-capture.py <after-F9-save.json> <before-F9-save.json> --cancel`, plus verify unchanged revision R and no state-change command. Whole-save hashes reported by the native tool are ideal for within-session invariance, but do not blindly require cross-process metadata hashes to match.

## Pointer checklist and exact expected commands

All selection-only steps below must leave export hash, authoritative run/RNG and revision R unchanged. Only a final committed action should move to R+1 once. Double-click or stale controls must not produce two intended commits. Read labels and UIDs rather than assuming screen coordinates.

### Optional kindle

Fixtures: `kindle-choice` (two impulse cards), `kindle-exact-fuel` (one), `kindle-no-fuel` (zero). Source card UID `qa-kindle` is red 火にくべる, cost 1, optional impulseCost 1. Starting energy 3; all three mudlings unburned (HP 9,9,8), preferred target 1. Ordinary card `qa-normal` is never fuel.

1. Open/play `qa-kindle`; expect `くべるか選ぶ`, buttons `くべずにプレイ`, `衝動の札1枚をくべる`, `使用を取り消す`.
2. Cancel at this prompt: no command, R unchanged, energy/hand/impulse/enemies/RNG unchanged. Reopen and skip.
3. Skip command: `Combat{PlayCard{cardUid:"qa-kindle",targetIndex:1}}`; no exhaustUids field. Result energy 2, enemies HP 5/5/4, burn 0; fuel remains in hand.
4. Pay when one/two fuel: opens impulse picker, even for exact count one. Select `qa-fuel-cheap` (灰の息). Only impulse fuel appears. `qa-fuel-costly` is 巨獣の踏みつけ and appears only in choice fixture. Cancel picker before selecting: same invariants. Reopen and pay.
5. Pay command: `Combat{PlayCard{cardUid:"qa-kindle",targetIndex:1,exhaustUids:["qa-fuel-cheap"]}}`. Result energy 2, HP 5/5/4, target enemy 1 burn 3, cheap fuel exhausted and removed from impulse list. Exactly one revision increment.
6. With no fuel, pay button disabled but skip remains playable. Clicking disabled pay must not mutate anything. Real engine rejects pretending ordinary `qa-normal` is fuel atomically.

Expected saves are `kindle-{choice|exact-fuel}-{skip|pay}.json` and `kindle-no-fuel-skip.json`.

### Keep impulse → normal retain → hearth

Fixture `keep-retain-hearth`: two impulse UIDs; cheap cost 1 and costly cost 5, ordinary guard UID `qa-normal`; 燠の守り, 潮溜まり (retain max 1), 灯の火床; light 6, hearth max 2. End turn must open these steps in order:

1. `燠の守り · 残す衝動の札を選ぶ`: manual choose cheap or `自動で選ぶ（最も高コストの札）`.
2. `残す手札を選ぶ`: choose `qa-normal`; deselect/reselect to exercise toggle and disabled alternatives at max. `これでターンを終える` opens hearth without committing.
3. `灯の火床 · 火種の枚数を選ぶ`: spinner 0..2, default 0. Choose 2, then `この枚数でターン終了`.
4. Manual command `Combat{EndTurn{hearthSparks:2,retainUids:["qa-normal"],keepUid:"qa-fuel-cheap"}}`. Auto command omits keepUid. Each submits once, R+1. Next player turn: HP 76, energy 4, light 0; normal card and chosen impulse survive. Auto keeper chooses cost-5 card. Two sparks are created; one is drawn by the next turn in this seed. Full draw/hand order is in expected saves.
5. IMPORTANT: `KeptImpulse` is transient and cleared at next-turn discard cleanup. Expected final kept card has no flag; check surviving UID and `ImpulseKept` event instead of requiring `keptImpulse:true` in final save.
6. At each of all three prompts test close/back before final confirmation. Overlay Close can return to the previous prompt because they are stacked; continue closing until dismissed. No partial EndTurn command is allowed. Reopen end-turn and verify fresh selection state.
7. Default zero variant: auto, choose no normal retain, hearth 0; command only `hearthSparks:0`. Light remains 6. Compare `keep-auto-hearth-zero.json`.

Fixture `normal-retain-hearth` removes keeper/impulse flags but keeps retain + hearth. End turn goes directly to retain. Select normal, hearth 1: light 3; normal and intrinsic-retain cost-5 card survive. Compare `normal-retain-hearth.json`.

### Reward rounds (not checkbox multi-select)

`reward-two-rounds`: original red options plus one extra reward round and a gear option. Inspect/cancel any card: no command/R change. `この札を取る` index 0 sends PickReward{index:0}, commits immediately R+1, adds one card and generates a second set of choices by the genuine seeded engine. The second selection sends another PickReward and adds another card. Gear is independent, so phase remains reward with gear pending after card choices. `reward-second-round` is the real first-pick output if you want to start there. `札を見送る` is SkipReward and advances a round, **not cancel**. Expected first/second/skip outcomes supplied.

### Workshop two-card selection

`workshop-selection`: 18 cards, 700G; indices 0/1 red kindle/ash breath, index 2 ember keeper. Click 0 then 1: labels 素材 A / 素材 B and fusion preview; all state unchanged. Toggle selected 0 off/on, exercise third pick replacing older selection, inspect/dismiss preview; no command until final action. Confirm with exact indices 0/1: WorkshopFuse{indexA:0,indexB:1}, 100G cost, 600G remains, deck 17, map phase; compare `workshop-fuse`. `見送る` is WorkshopSkip, commits R+1 and goes map with deck/gold unchanged (`workshop-skip`). It is not a hash-preserving modal cancel.

### Relic multi-select, deselect, and zero confirmation

`relic_empty_cage-reward` and `relic_astrolabe-reward`: taking index 0 dispatches PickRelic and generates real pending relic choice. Corresponding `*-choose` fixtures are the actual generated states. Cage cap 2, Astrolabe cap 3. Select indices 0/1, deselect/reselect, inspect/dismiss; no engine command during toggling. At max, unselected cards should be unavailable but selected ones remain deselectable. Confirm sends sorted RelicChooseCards{indices:[0,1]}, once R+1; cage removes two (15→13), astrolabe transforms/upgrades two (15 unchanged), phase map/pending cleared. Expected saves contain exact transformed cards and RNG.

The current full-page relic selection has no dedicated cancel button. Deselecting all is state-preserving; pressing 決定 (0/N 枚) is a real RelicChooseCards{indices:[]} commit that exits to map and clears pending, while deck remains identical. Do not label that operation a cancellation invariant. Test zero confirmation separately using `*-zero-confirm` outcomes.

## Limits

Synthetic preconditions are deliberately obvious QA-only UIDs and cross-color permanent composition, with original content definitions and original RNG states. All saves load through the production session and all submitted commands execute the real source engine. This does not prove source UI pointer reachability, modal layout, timing, stale-control suppression or cancellation implementation; the runtime worker must record those on the actual Godot surface.
