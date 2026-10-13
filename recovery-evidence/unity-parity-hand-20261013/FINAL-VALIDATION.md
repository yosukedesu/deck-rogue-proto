# R5 final validation addendum

2026-10-13 03:37 UTC. The frozen source archive was not modified.

## Result

The bounded actor/sprite source-parity work is accepted. The frozen source package is `actor-source-parity-r5-20261013T0248Z.zip`, SHA256 `2b5e7a8d64c775638f6d3308c4c0b39ad25f508c5f963c66f011cd0eb1ec4e41`.

- Pure sprite/motion tests compare the extracted literal source methods at 30/60/90/120 Hz, variable deltas, 250 ms stalls and multiple absolute clock origins. The R5 inner-delay matrix has 60 passing source comparisons.
- Integrated headless regression passes 121 checks, module `1e9b72fb-cf7d-42df-980f-972603e80e1c`.
- Native actor-segment review covers showcase strike, ordinary melee, double-lash, rapid cards, enemy multihit and ranged attacks. Historical R3 rapid/ranged failures remain explicitly rejected.
- Final composed fixed-60 capture has 390 consecutive native frames and completes the whole turn. Actor poses, starts, elapsed clocks, offsets and pause values match the earlier accepted strike exactly for frames 30–109. Final `IsPresenting=false`, `ActiveMotionCount=0`. The run exits cleanly.
- The targeted native-engine gate uses the actual stage/command queue with a paused automatic clock and manual frame deltas. Both runs pass 46 checks: the literal float boundary fires on step 7, the 250 ms stall on step 2, and each contact occurs once. Four frozen native snapshots are supporting evidence, not continuous-motion proof. Both runs load module `d4987add-4a46-4fcf-922e-cddcc3af03ef`.

## Teardown qualification

The first targeted native-engine run reports an ObjectDB leak warning and an error stating six resources remain in use at exit. The unchanged verbose rerun has neither message and ends with tracked-instance disposal completed. No cleanup patch was applied; exact retained resource types are therefore not established, and the disappearance cannot be credited to explicit disposal. The original log is retained. Similar exit diagnostics occur in earlier baseline/R4 logs. This does not establish a production leak or an R5 timing regression. The separate 390-frame continuous run is clean.

## Evidence files

- `source-parity-r5-freeze.json`: frozen source and patch identities
- `timing-tests-r5.json`: pure source method comparisons
- `movement-native-review.json`: individual native acceptance and rejection scopes
- `r5-integration-addendum.json`: terminal integrated headless result
- `r5-native-integration-review.json`: final continuous-turn module, actor comparison and settlement
- `native-r5-composed-actor-review.jpg`: reviewed native stages through final settlement
- `r5-variable-native-review.json`: both actual orchestration runs and teardown qualification

Raw final runs are under `unity-parity-20261013/hand-float-native/corrected-turn100` and `unity-parity-20261013/motion-r5/native-variable-delay{,-verbose}`.

## Deliberately separate

Android live device cadence is not certified by these desktop proofs. The Unity-authored 40 ms sprite cap, one-frame advance policy, missing green hurt clip and shared-rest-anchor lunge restart remain intact. An optional continuous-handoff experiment is prepared under `quality-experiment`, defaults off, and is not part of this accepted source package. Its proposed native A/B has not been run.
