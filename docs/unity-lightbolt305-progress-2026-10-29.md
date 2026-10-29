# Unity LightBolt 305 integration — verification in progress

This supersedes the execution blockers in the earlier intake note. It is a partial migration report, not acceptance of all checkpoints through 305 or 312.

## Baseline and inputs

Dedicated branch: `codex/unity-checkpoint305-20261029`, starting main `05ecb302`. Unity's original HD-2D implementation was checkpoint192 (`5668c3d0`), not270. The original checkout is protected; no merge or push is performed.

Published270 plus the corrected273/277 kits and the selected cumulative kits through305 were reconstructed in isolation. The305 logical manifest has302 exact matches, zero mismatches, and three missing original historical Lightning241–243 contracts. The recovery243 code matches its supplied SHA; the missing original contracts are not recreated or claimed verified. The selected312 kit and movie were downloaded and their supplied SHA256 values verified.312 is not applied yet.

## Implemented scope

- Exact recovered Lightning243 geometry method and geometry tests; full Lightning live routing is still pending.
- Exact selected305 `CreateOwnedLightBoltContact`, retaining the existing CreateLight anticipation.
- Dedicated original LightBolt density PNG, no resizing/mips/compression/alpha dilation, with dedicated URP material for only kinds41/42. Linear texture decoding and UV1 HDR gain are each applied once.
- Actual run-command ownership retained with the existing numeric ledger. The candidate requires an explicit play target, matching card UID, source, stable target identity, LightSpent1, and the actual three-hit event batch. It observes actual receipt commit and generation; it never creates damage or moves numeric timing.
- Separate effect cache key, callback refreshed before each sample, and cleared on retirement. Live pool/cancellation qualification is still pending.
- A QA-only `explicitcommand=1` option dispatches an ordinary engine command with a required explicit target. Ordinary singleton UI targeting stays unchanged. `playcontinuous=1` adds consecutive framebuffer capture and clock evidence.

## Verified results

- Unity6000.6.0f1 compile and Windows builds succeed. The first updated Windows build had0 errors/38 warnings.
- Existing13 golden run files match in Unity after command-owner propagation. The golden harness now uses the engine's actual replay-origin initializer, including checkpoint origins, without changing expected hashes.
- Lightning243:45 geometry samples,138240 finite vertex checks, indices/budgets/tail pass.
- LightBolt305:65 geometry samples pass, including pending/negative/nonfinite/end-age suppression, callback reset, legacy contact suppression and submitted triangle bounds.
- Actual engine explicit-command test: three real hits accepted; commit observed only after numeric ledger commit; implicit-target candidates rejected; all retained bindings invalidated after ledger generation reset.
- Actual Windows explicit-target capture selects `ownedLightBolt=True`, eight batches, for all three actual4-damage hits. Earlier ordinary singleton-UI capture selects the original five batches because that UI sends a null target, intentionally outside the selected305 owner contract.

The initial71-image capture uses the old snapshot helper, which advances multiple frames per image. A subsequent70-frame consecutive capture was verified by consecutive Unity frame numbers. Actual commits occur at indices12/19/25; Godot reference indices are12/19/26. Each private shoulder is submitted for ages0/.0333/.0667/.100/.1333/.1667. Pending/private contact draws are disabled, and final active FX is0. The actual texture object is1774x887. Mesh submission is not shaded-fragment coverage proof.

Same-process lifecycle sequence: normal, cancel-before(frame4), cancel-after(frame13), normal reuse, other light_glint and lethal1HP. Both cancellation cases have0 active FX/0 visible draws after cancellation. Normal and reuse each submit18 private shoulder samples; light_glint submits none. Lethal emits only one actual damage event and correctly rejects the three-hit private route. Its initial50-frame terminal assertion failed: old effect age.2745 was below the existing.42 retirement under finishing slow motion. Extending only that case to100frames confirms0 active/visible at the end, with last active frame54. No product clock was changed to satisfy the test.

The final material review changes `_RoleCore` to a vector upload because palette values are already linear; the final70-frame capture includes that correction and again records commits12/19/25 and terminal active/visible0. Unity uses its existing enemy_probe43HP fixture and existing actors; the Godot reference uses a200HP fixture and different framing. The new body is subtle against the existing white hurt flash. Overall commercial-quality and pixel parity acceptance remain HOLD; no equal-pixel or all-bodytype equivalence is claimed.

## Remaining work and external save status

Remaining: visual acceptance and broader ownership/bodytype/other-effect regressions beyond the specific lifecycle cases above; remaining post192 effects through305 (including Imp289, Cold297, Canopy300 and earlier owned gear/permanent/reaction routes); then exact Wraith312. Android performance is not measured. The source main checkout independently advanced to`fb4ff799` during this work and remained clean when checked; this branch keeps its original base and does not reset or merge the concurrent work.

No automatic approval rejection occurred. A sandbox-started Editor failed licensing IPC; only this task's Editor was stopped and the authorized ordinary host execution succeeded. Visible game execution is authorized and works.

The large reconstructed-input archive remains unsaved to Library. The official upload helper first failed401; with its supported auth-path configuration it progressed to `Library prepare_uploads is not available`, before creation. No alternate upload of that started helper request was attempted. Its local path is not a parent-delivered artifact. The old Library intake `libfile_41a83f639740819180985cae344bf6fd` is historical and does not contain this implementation.
