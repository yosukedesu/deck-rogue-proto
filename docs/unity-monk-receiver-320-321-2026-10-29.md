# Selected Monk 320 / 321 receiver-only light port

Branch: `codex/unity-checkpoint305-20261029`. Previous completed port HEAD: `1a7844d8993c90a1840c636348ba1dff7af93cca`. Checkout began at `05ecb302`; `5668c3d0` is its ancestor effect checkpoint192, not a replacement checkout base. See the provenance note. No main merge or push.

## Inputs and implementation

Both selected kits and original movies were materialized locally and verified against the parent's SHA256 and size. 320 kit `libfile_bb06e8c584c8819182de6f02d8349c4b`; 321 kit `libfile_15534edad0588191957fc34cdde1901f`. 321 movie `libfile_c9d64b40acd4819182d8cfc5ac4b4ad6`. Their handoffs and actual reference frames were inspected. No source bitmap was regenerated.

Unity already uses real URP point lights for target/floor, unlike Godot's analytic receiver arrays. The new receiver projection leaves these world Light intensities, colors, positions and envelopes unchanged. Only the current hero's StageUnitLit attack-light addition receives the two matched endpoint factors. Other actors retain count0. Per-material arrays are cleared and recomputed from the live owned max-combine winner. Stale generations, absent actor mappings, endpoint position collisions and more than two owned endpoints fail closed. No global bloom, white cap, flash, geometry alpha or shared effect gain was changed.

320 admits actual Monk self installation and valid player Heal1 under its installed UID. 321 has a separate predicate: previous installed Monk and new Squire, distinct UIDs, actual command/receipt references and generation, ordered unique PermanentPlayed / Heal1 / Light1 / Squire damage3 with permanent provenance / RetainerRushed. It preserves Squire's attack. Admission is not a new committed-receipt gate. Heal0 and other entrants are excluded.

## Verification and visual qualification

Unity 6000.6 Windows D3D11 game captures use 1280x720, fixed 30fps simulation with 100 actual rendered frames per case. This is not device performance evidence. Editor checks exercise actual self and nonself commands, cost, separate Light1, Squire damage3/rush, fullHP Heal0, precommit admission and stale generation rejection.

320 baseline1 / selected .08 / candidate .16 / nonself / fullHP / cancel-before / cancel-after / reuse: eight native cases. Same actual Heal cache ID673494322 across cases. Selected receive frames4–10, enemy count0, final receiver count0, pool not busy, root inactive, draws inactive. Cancellation before contact emits no selected receive; after frame5 clears. Nonself remains ordinary at320. All actual events match across the three A/B candidates. World light and active local draw samples differ only within measured absolute maxima .0000705 for .08 and .000330 for .16 (float simulation clock accumulation); they are not byte-identical. Inactive cached vertex/alpha values can remain and are not claimed cleared.

Direct raw images show baseline whitewash obscuring face/clothing, .08 restoring the face and robe shading while keeping the chest-local recovery light. .16 retains more bright clothing. .08 is the selected local receiver correction after this Unity A/B, not assumed optimal solely because Godot selected it. Full environment parity, all body sizes and Squire strike craft remain unqualified. The local frame improvement is suitable for independent review; no global visual PASS is inferred.

321 captures additionally test self preservation, real Monk→Squire, its factor1 comparison, Monk→LightCore exclusion, fullHP, before-contact cancellation, after-contact cancellation and reuse. Native result details are in the audit JSON and actual-events/prepare-events. Squire heal contact occurs later than self; the initial cancellation-at5 trial is a before-contact trial, not evidence for after-contact cleanup. A separate cancellation-at10 capture supplies the after-contact observation.

## Build diagnostics and remaining scope

Three earlier shader attempts logged FXC compiler connection loss even though BuildTools reported Succeeded/errors1. Those binaries were rejected for validation. Combining the existing additional-light position read with the owned receive lookup resolved compilation: 320 v4 errors0, 321 build errors0. Logs are retained; exact compiler-internal failure mechanism is unknown. No compiler/security setting was bypassed. Existing obsolete render settings and shutdown allocation warnings remain separate.

Cold297 normal fixture local craft PASS and Root319 target/forecast limited PASS are unchanged. Imp289, Canopy300, Wraith312 and Crossbow318 craft/world HOLD remain; no new causal GPU A/B for those four is in this packet. Next diagnostics: Canopy role-specific projection/depth/solid/post comparison, and enemy ScreenFlash isolated from local materials. Wraith all-positive-vertex bbox must not be compared as a main-role bbox. Light305 overall lighting/age parity HOLD remains. Android, lethal engine coverage and other body sizes were not tested. These limitations do not imply input/access/approval blockers.

321 native result: eight 100-frame cases plus one 100-frame after-contact cancellation capture completed (9 cases). Selected Squire receivers appear frames8-14; self frames4-10, both .08. LightCore retains ordinary reception with count0 after prior selected self using the same cache ID2007889332. FullHP retains Heal0/Light1 and Squire3/Rushed without a Heal FX. All final owned counts0 and inactive pool/draws. Squire baseline1 versus .08 world-light/active-draw absolute maximum difference .00052547 across100 samples; actual events identical. Independent original PNGs8/10/14 show recovered face/clothes and the later separate enemy strike. Local receiver improvement is confirmed; full parity and strike craft remain HOLD/unassessed.
