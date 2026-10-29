# Unity checkpoint 305 intake — 2026-10-29

## Result
Blocked on exact pre-288 inputs and Unity batch startup. No effect implementation was ported and no Unity parity or Android performance PASS is claimed. Existing main and other worktrees were preserved; no merge or push.

Repository: `/home/yosuke/projects/deck-rogue-proto` (WSL Ubuntu).
Isolated branch: `codex/unity-checkpoint305-20261029`.
Starting HEAD: `05ecb302`; Unity effect implementation commit `5668c3d0` explicitly ports checkpoint **192**, based on `8834bd18e9d09278cc0b49c1dc9515f4ad06670f`.
Worktree: `C:\Users\yosuke\Documents\Codex\2026-10-29\task\deck-rogue-unity-port`.
Unity Editor/project version: `6000.6.0f1`.
Read engine-choice-2026-10-26.md and dot-godot-port-2026-10-27.md. No repository AGENTS.md or .agents/skills found; Windows user AGENTS.md read. Initial repository status was clean.

## Inputs
All nine supplied Library inputs were materialized by the current official Library transfer helper into `/tmp/deckrogue-unity-inputs-20261029` in Ubuntu, including Library identity/version xattrs. Windows Python lacks os.setxattr, so the unmodified helper was executed in Linux. No storage URL was guessed and no publication was attempted.

| Input | Library ID | Opened/checked |
|---|---|---|
| 300→305 kit | libfile_97109956b77c8191a247b80339ef85bf | ZIP, handoff, manifest; 19 checksums pass |
| 297→300 kit | libfile_e0a841c188c88191a103c01690ee3bb9 | ZIP, handoff; 16 checksums pass |
| 289→297 kit | libfile_663ab92bbd24819198b0c7a22d067309 | ZIP, handoff; 20 checksums pass |
| 288→289 kit | libfile_9feee33a03f48191b9f5088b36e4304c | ZIP, handoff; 8 checksums pass |
| 305 movie | libfile_a9914df4f3d481919764b95598486384 | Decoded; actual frames 12/19/26/38 inspected |
| 300 movie | libfile_56799705849881919f8feb56ff1e54ab | Decoded; frame 36 inspected |
| 297 movie | libfile_4ddb4f79d0f48191a1a01691bca7bc6a | Decoded; frame 36 inspected |
| 289 movie | libfile_bb8b908d4d44819196e8672f07b167c7 | Decoded; frame 36 inspected |
| 305 QA packet | libfile_2b676f0262888191a0b02f67f2628507 | ZIP directory readable, 201 members, SHA verified; full QA not rerun |

305 movie SHA256: f49ce76fb78d5b830ae02578dd2f5b5871ae7321e82d1030c9a284e0e77d199d.
305 packet SHA256: 46d751c814aa1cde477dde16dcaa8cde9f9dbb98a3c227b96f3a12e1ac3647aa.
The inspected frames are Godot reference evidence, not Unity output or a continuous-playback quality certification.

## Exact prerequisites
Fetched only published checkpoint 270 commit `ff4f11edefdf0695f3b02b5554cbfb06b6042686`. Remote branch was not assumed current. Overlaying the four supplied deltas conceptually on this commit and comparing against MANIFEST-305-LOGICAL.json yields **274 matching, 27 missing, 4 mismatching** paths. No resulting tree was substituted for Unity.

Required: selected **271–288 cumulative source/material delta** (or a complete verified selected288 logical input), including checkpoint273 `assets/Art/fx/hd2d-fire-volume-powder-density-v1.png` and checkpoint284 `assets/Art/fx/maelstrom-water-density-v1.png`.
Also missing from the available270 tree: LIGHTNING-TRUNK-HIERARCHY-241, LIGHTNING-OPTICAL-SHOULDER-242, LIGHTNING-CONTINUOUS-SHOULDER-243 contract JSONs. Their absence is an audit gap, not evidence the corresponding code is absent.
Four mismatches: DeckRogue.csproj; NecroContactVfxProfiles.cs; ReactionContactVfxProfiles.cs; RecoveredSource/DeckRogue.Game/LocalMaterialProfileGeometry.cs.
Missing profile sources: GearContactVfxProfiles.cs, GearKegContactVfxProfiles.cs, NamelessPowderContactVfxProfiles.cs, NamelessKegContactVfxProfiles.cs.
Full path/hash inventory is in evidence/checkpoint305-prerequisites.json in the task workspace.

## Verification
- PASS: all 63 bundled checksum entries across four kits.
- PASS: existing C# engine golden verification, all 13 run JSONs match (isolated worktree, dotnet run -- verify ../../goldens/runs/*.json --data ../../src/data).
- NOT CONFIRMED: Unity baseline compilation. First batch log stops at AssetDatabase Initial Refresh Start. Second batch log explicitly says application return code1, following licensing-client validation and unavailable-access-token messages despite resolved entitlements. Root cause not established. No compiler success or rendered capture obtained.
- NOT RUN: ported normal/interrupted/repeated/UID/multi-hit/pool-reset regressions, Unity/Godot visual parity, Android performance.

## Continuation plan
1. Supply exact missing logical input; rerun full305 manifest reconstruction audit without modifying Godot publication.
2. Resolve baseline Unity batch startup on this dedicated worktree.
3. Incrementally port post192 route/receipt infrastructure, preserving existing StageCombat actor timing and indices divisible by3, UV1 HDR gain and linear RGB semantics.
4. Apply each selected effect with exact owner/receipt predicates; preserve installed/ambient/trigger UID distinctions and Scald semantics. Exclude checkpoint311.
5. Capture actual Unity conditions and verify lifecycle/other-route regressions before claiming parity.

No operation was denied by automatic approval review. Source input availability is pending; Unity execution was attempted but unsuccessful/unconfirmed as described.
