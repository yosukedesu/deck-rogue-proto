# Curse reaction 247→249 Unity handoff (final focused evidence)

## Preserve the existing Unity implementation
Godot C# files are comparison sources, not replacement Unity classes. Preserve the user's Linear palette sampling, HDR-gain UV channel, aligned triangle counts, math shim, renderer and actor ownership/clock integration. Port only the new exact predicate, dispatch and target-local factory. No Unity code is edited here; Unity motion and device performance remain unverified.

## Route and rules
Actual card `black_reaction_curse` / 呪詛返し, cost1 reaction/onAttacked, drain6. `CurseRoute(route)` is the subset of existing `ReactionContactVfxProfiles.SelectedRoute` with DefinitionId black_reaction_curse; therefore sourceKind reaction, key card:black_reaction_curse, family dark_drain, renderer Hd2dReactionContactFx, element dark, FixedSmall remain required. The existing ConfirmReaction/armed-set-UID/usable-window/receipt ownership proof is untouched. Ambient SourceUid is null in this fixture; PresentationSource.Uid qa-card and actual ConfirmReaction.CardUid establish ownership. Do not conflate ambient and presentation UIDs or claim missing ownership from SourceUid null.

The existing `CreateReactionDark` wrapper and all of `CreateDark` are byte-unchanged. UnderworldHand/Grudge/LastStand, necro, installed and direct Dark do not use the new factory. One real damage receipt; independent drain healing is NOT represented by a return beam. Hero's legitimate counter attack pose remains unchanged, with no new pose or suppression.

## Baseline and correction
Fresh247 baseline was404 assertions/exit0 with8 natural keys. It already had a large readable purple-black effect, not an absent wash, but two detached smoke lobes with the larger one near the target's head.248 brought both roots together but registered the contraction near the weapon and was held.249 changes only two runtime expressions from248: root(.015,-.10,.025)→(.26,.01,.025), and main angle .40+.42*close→.12+.22*close. Material/alpha/width/phase/light are unchanged from248.

The root choice is grounded in actual248 frame20: camera projection reproduced logged effect target to<.01 internal pixel. Old root at native1280 (907.78,346.99) matched the hand/blade; new predicted root≈(945.01,330.28) falls inside the independent reviewer torso region x938–956/y318–344. These screen coordinates are evidence only; product uses existing `target + cameraBasis * localPosition * height * size`, not hardcoded screen coordinates. Real actor recoil shifts its body relative to the target anchor, so different-body/camera registration is a separate bounds gate.

## Factory geometry, material and seconds
3 existing raw phases `hd2d-dark-contraction-01/02/03`, unchanged originals.2 unequal portions, both rooted at(.26,.01,.025). Main radius(.66,.43), secondary(.35,.27); scale factors(1-.36*close)/(1-.48*close), secondary opacity .68. Main angle .12+.22*close, secondary -.52-.30*close. `dt=age*1.25-part*.012`, `close=Ease(dt/.20)`, onset Ease(dt/.018). Exterior bend subtracts close*reach²*(.16 main/.05 secondary) from local y before rotation; reach=Clamp(length(x,y)/.80). This folds the outer material toward the same fixed local root rather than translating two stamps.

Phase weights preserve the old local Dark windows: phase0 1-Ease((dt-.055)/.035); phase1 Ease((dt-.055)/.035)*(1-Ease((dt-.145)/.050)); phase2 Ease((dt-.145)/.050)*(1-Ease((dt-.25)/.10)). Negative age empty; age>.30 empty.2parts ×8columns×4rows×6=384 vertices max per phase batch; each allocates512.3 texture batches Kind16, one motes batch Kind2/max24. Continuous UV across each mesh, x adds2*part for the existing porous complementary split. No shader/renderer changes or new raw.

4 subordinate inward remnants end within.23s, radius .22*(1-pull), width.006, alpha .45*old pulse, gain.85. No gain increase. Old local light retained exactly: TargetPeak .028, FloorPeak0, StageFocus0, FloorLight0, Light age<0?0:1-Ease((age-.025)/.12).

## Fresh evidence and limits
249 standard460 assertions/exit0, same8 natural keys and exact247 whole rules/event oracle. First sampled positive frame19 is age .016666055, NOT age0. Movie536 assertions/exit0,70 simulation frames at30fps (2.333s), silent; it is not device-performance footage. Initial observed reaction is one contact, independent heal starts later. Source234 logical manifest is frozen; original historical QA remains inaccessible while this is fresh recovered-environment evidence.

Independent static and contiguous-frame temporal review pass: .05→.117 exterior closes into the torso while root remains, .15 main mass nearly gone, no re-flash/second hit. Actual blade remains separate; face/6/HP readable. Review decoded sequential frames, not direct equal-speed subjective viewing. Additional bounds and final shutdown classification will be appended; pending text in trial contracts does not supersede the final addendum.

## Final focused qualification — supersedes pending labels above
**2555 assertions /6 successful exit0 runs**: standard460, movie536, real decline256, RockBeetle fullblock396, shared Underworld510, Moss small-window397. Decline emitted unresolved act1.ogg2 shutdown resources; other5 did not reproduce it. All retain known VSync warning. This is not6 clean runs, leak-free certification or a whole-game matrix. No audio fix is mixed in.

- Actual ConfirmReaction Fire=false gives no dedicated Curse. Owner=false is separately a selector unit negative, not a fabricated engine event.
- Real initialized RockBeetle burrow Block12 survives the legal setup: reaction6 fully absorbed, Block12→6, HpLoss0, dedicated Curse absent. Engine still emits independent heal3; this behavior is preserved, not suppressed based on HpLoss.
- Real UnderworldHand retains the old7-batch local Dark factory, proving the new4-batch Curse shape does not leak through the shared reaction adapter.
- Real MossSlime start bump→ooze→bump, actual Set→3EndTurns→Confirm, positive damage6.960×540 small-window frames show root on the creature's upper front, no floating residue, readable outline/6/HP. Peak partially covers its face, accepted only for this scoped compatibility. This is **probe+Moss two-body registration**, not all enemies or a phone/device test.

Independent artist passed static, decoded contiguous temporal frames and Moss bound. No more source changes requested. The main body closes into the receiver and ends before the separate heal. Equal-speed subjective experience, final Unity presentation and real-device performance remain unverified. Existing held248 evidence is retained distinctly.

### Final lifecycle supplement (authoritative final total)
Parent-authorized existing lifecycle host added419PASS/exit0. **Final total2974 assertions /7 successful exit0 runs**, replacing2555/6 above. Two runs have shutdown-resource notices: decline act1.ogg2 and lifecycle act1+card_draw Ogg4; other5 did not reproduce. All have VSync warnings. Audio remains separate unresolved work.

Actual pending fixture reload exercised four cycles:0 cancel before contact;1 natural completion;2 cancel after contact;3 after-contact actor.SetDead(true)+cancel. This is actor/component lifecycle testing, NOT actual Engine lethal damage.234 logical source hashes,8758 expected source files,8751 reconstructed immutable files and62imports passed final protection checks. Authoritative receipt: qa/REV249-TERMINAL-QUALIFIED-SUMMARY.json. Source remains frozen249; no additional geometry/material change.
