# Probe poke260 — final qualified Unity port handoff

## Integration authority
Published base258: `8a3d31afe0ec503247076435ea162e27cca2fd2d`. Selected260 is frozen248 logical paths in the recovered authoring workspace. Godot full files are comparison material; do not replace the user's Unity renderer/math/shader/Stage/Actor wholesale. Preserve Linear color/palette `c.linear`/HDRgain UV/triangle510/zero-safe math and the user's existing port. Port only the exact factory and profile/bridge dispatch intent into existing Unity APIs. No Unity code was changed. No new raw/shader/audio/import assets.

Original historical workspace and original QA remain inaccessible/unrestored, not proven permanently lost. This series uses fresh native QA against reconstructed258, not a claim to restore the old full manifest.

## Scope and game rules
`enemy:enemy_probe/poke`, actor `enemy_probe` (探り屋), definition/move `poke`, source kind `enemy_actual_execution`, family `pierce_contact`, physical, `FixedSmall=false`, selected renderer `Hd2dEnemyContactFx` only. `ProbePokeRoute` first requires the existing `SelectedRoute` full tuple, then exact key/definition/family. The existing actual EnemyAction execution/index, damage/contribution, valid player receipt, source actor continuity and HpLoss>0 selection remain unchanged.

The old `CreateEnemyPoke()` stays byte-identical. Other six selected actor/move pairs continue through it: probe/lunge, RockBeetle/horn_jab, BondWolf/lunge, GoldRaven/peck, HazeStag/antler_jab and antler_charge. No new spear, huge body/hand, projectile or beam bridging the stage gap. This is only a receiving-side contact cue. Existing enemy weapon/pose/approach remains, and the effect does **not** certify that the actual weapon tip reaches the hero.

## Actual comparison baseline
Fresh258: actual initialized single enemy_probe, leader_green, initial energy3, real EndTurn with no card use or move/intent replacement. Real damage5/HpLoss5, hero80→75. Shipped enemy HP45 and behavior retained. First visible native frame28 at age−.0533; first nonnegative frame30 at+.0133, recorded independently. Eight natural keys28/29/30/31/32/34/37/41. Frame filename or capture-label time is not effect age.

Original thin white-blue shaft beside hero read like a small light blade.259 removed the long bright shaft but hid most pressure under hero hurt/axe.260 moves only the transparent pressure's width/outer extent back outside the hero silhouette. Core remains short, not a new long blade. This is a modest regular-enemy contact improvement, not a new spectacular attack. Existing hero white-hurt/scene brightness remains dominant and is not claimed as improved.

## Source changes / numbers
New `Hd2dAttackPrototype.CreateProbePokeContact()` plus one `EnemyContactVfxProfiles.ProbePokeRoute` selector and one NativeGeometryBridge dispatch line before generic enemy dispatch. Removing those additions reconstructs selected258 source exactly (before259/260 historical docs). Shared shader/renderer/palettes and all other factories are unchanged.

**Stage/Actor integration numerical changes: zero.** Camera, gameplay, real hit time, enemy approach and hero hurt unchanged.

- Original local contact center `(0,.025,.035)`, direction=normalized `(-source.X,-source.Y,0)` with existing Vector3.Left fallback. Existing ConfigureSource callback preserved. No hardcoded screen coordinates.
- Same sample window[-.07,+.28] seconds. Outside it all batches reset empty. Same target/floor peak `.09/.09`; Light=`exp(-age/.085)` and Floor=`exp(-age/.14)` for nonnegative age; StageFocus0. Existing glow Sprite `.20×.13`, alpha`.24*exp(-age/.055)` unchanged.
- Pressure is8 short ribbon cells with **continuous global UV0..1**, not8 individual UV fades. Kind21 old soft cross-section/whole-length endpoint shader unchanged.
- Ribbon centerline lerps outer→inner with normal bend×sin(PI*u). Half-width=`.010 + width*sin(PI*u)*(.90-.40*u)` in260.259 had `(.45+.55*u)` and thus placed width nearer the obscured contact.
- Negative phase: advance=Ease((age+.065)/.065); tip=center−direction×(.30×(1−advance)+.025); outer=tip−direction×.18; width`.028`, opacity`.48*advance`, bend`.008`. No negative core/glow.
- Positive: release=Ease(age/.16), alpha=1−Ease((age−.025)/.13). Outer distance`.68-.16*release` (259 `.48-.16*release`, original shared `.76-.18*release`); inner`.025+.10*release`; width`.11*(1-.60*release)`; bend`.022*(1-release)`. Pressure gain1.05, original palette primary.
- Contact core from center−direction×.13 to center−direction×.012: length`.118`, half-width`.017`; alpha=alpha×(1−Ease(age/.075)), old gain1.15. One subordinate very short oblique stress half-width`.006`, alpha×.65. Bright core does not span the pressure length.
- Two unequal residue pieces start+.025/+.037, lengths`.035/.022`, widths`.005/.0035`, alpha coefficients`.45/.30`, old gain.85; fade `1-Ease((dt-.055)/.10)`. No ring or second impact.
- Batches kind14/21/21/2, max written6/48/12/12; each allocated Count512. Count is capacity, not active count.

Actual259 frame30 registration used to diagnose occlusion: hero Height1.7837344; effect target world(-4.9730654,.912877,1.2397707), internal1600 screen(364.9928,467.76132); hero axe point internal(416.06314,502.43866). These values are evidence, not runtime anchors or universal body coordinates.

## Final focused QA / art qualification
Final260 supersedes draft/pending fields in the historical259/260 contracts. New source was held fixed throughout these final tests.

**2985 assertions / 6 successful exit-0 runs** (repeated assertions, not2985 distinct behaviors or a complete matrix): standard462, movie420, real guard524, old shared lunge500, blue receiving/small-window534, lifecycle545. Known driver VSync warning in all runs. Movie and guard each reproduced unresolved act1 Ogg2 shutdown-resource notices; other4 did not. No Audio product fix was mixed in, no leak-free claim.

- Fresh258 baseline446PASS retained;259 staticHOLD retained. No held trial is relabeled selected.
- Whole actual standard transition remains exact258: damage5 / HpLoss5 / hero80→75, actual execution/receipt/actor source preserved.
- Real green_guard PlayCard thenEndTurn: incoming5, Block5→0 / HpLoss0, hero80 unchanged, dedicated Poke body absent. No injected initial block.
- Same initialized single probe,3 complete actual preparatory EndTurns, then fourth-turn shared lunge: incoming14, hero69→55. Old `CreateEnemyPoke` still used; no exactProbePoke dispatch. Earlier prior1 proposal was a read-only unrun mistake, corrected from actual data. `poke_2` is a behavior node mapping move`poke`, not an invented move id.
- Blue receiver real EndTurn with no spell card: poke5, hero65→60. Actual profile max HP differs fromgreen; no fixed80 assumption.4 natural keys at960×540, not an actual phone/device. Existing onSpellPlayed Ice passive is not invoked by this command.
- Lifecycle: before-contact cancel, natural completion, after actual Poke contact cancel, component `SetDead(true)+cancel`; reload/inactive/light cleanup checked. Component death is not an Engine-lethal event proof.

Astra standard and contiguous-frame review passed within this scope. Movie indices25–36 show existing enemy lunge/weapon pose → one contact at29 → weak pressure remaining30 → fading31–33 → clear34. No new spear/white shaft/second pulse. Negative-age pressure and two residue pieces are subtle, not certified as a rich flowing spectacle: anticipation is led by existing enemy motion. Direct equal-speed subjective playback was not inspected.

Blue/small-window placement also passed, but peak existing hurt strongly whitens blue face/torso and separates the tiny contact less clearly than green. This is a retained hurt-integration limitation, not solved by this checkpoint. Two leader body types only; no all-leader/all-enemy or actual-phone generalization. Unity motion/lighting/device performance remain unconfirmed.

The70-frame native movie is fixed30fps simulation /2.333s /silent, not wall-clock performance footage. Actual effect visualAge, not screenshot filename/capture-label time, is comparison authority. The full source artifacts do not authorize replacing any independent Unity work.

Movie bytes: 607466; SHA256 `8f8a64ed795dfec42c1ec6975cb261536b6fb38e8b8469aea182373e0a722e4f`.
