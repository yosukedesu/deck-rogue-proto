# Physical VFX: representative-first rollout

Authorized scope: common attack presentation architecture, representative
families, then remaining physical and magical/elemental attacks. Recovery,
defence and support are separately inventoried nonattack presentations, not
inflicted-hit VFX. This document tracks the physical-first implementation stage.
No game-rule changes, APK, push or
main-branch restructuring. Revision67 is the accepted visual baseline.

## Verified inventory

Actual data contains466 cards,98 physical:72 damaging/routed attacks and26
support/defence cards. Support cards receive no attack VFX simply because their
type is physical. The72 divide into38 explicit geometry identities,20 ambiguous
semantic attacks, and14 elemental hybrids. Enemy routes are inventoried only;
they are not silently included in the card rollout.

## First representatives

| Family | Actual ID | Rule payload | Design distinction |
|---|---|---|---|
| Directional cut | green_strike | damage6 | Actual weapon trace, broad curved light, torn cooling exit; accepted67 |
| Blunt impact | green_basic_bash | damage4 + expose2 | Compressed short contact, asymmetric pressure shards, rapid radial release; no summoned fist/hammer |
| Thrust | green_weak_point | damage8 + expose2 | Narrow accelerating axial light, focused penetration point, retracting/fragmented trail; no solid giant spear |
| Heavy ground pressure | green_finisher_stomp | damage50 | Looming ground shadow + hollow fleeting aura, pressure fan then stone/dust; accepted67 |

## Architecture and gates

1. `PhysicalVfxProfiles` is an immutable, explicit card-ID registry. It declares
   family, source/target anchoring, anticipation/contact/release timing and
   per-resolved-event presentation policy. It owns no combat events or damage.
   First migration includes ONLY accepted67 cut and heavy ground pressure.
2. Prove exact67 timing/geometry/scene equivalence in native renders and lifecycle
   tests before activating additional entries. Fire stays outside this physical
   registry, with its selected67 presentation untouched.
3. Implement blunt and thrust as distinct geometry/choreography branches. Reuse
   optical materials, phase lifecycle, lighting and cleanup—not the same slash
   silhouette. Review each actual full-gameplay-scale cycle against references.
4. Test real dispatch, upgrades, rules payload and once-per-resolved-event contact;
   supported actor anchors, obstruction/remote handling, small/large targets,
   stages and interruption/reset. No representative is accepted by compilation
   or a registry entry alone.
5. Only then map remaining explicit-family cards in bounded batches. Preserve
   each card's conditional hits, multihit rhythm, target/cascade/random targeting,
   modes and damage/status payload. One event is not assumed to mean one card hit.
6. Ambiguous and hybrid identities require a deliberate per-card mapping. Preserve
   vine/plant/fire/meteor/spark identity and existing magic; never infer physical
   status from a slash renderer or replace all effects with gold.

The no-body rule is in PHYSICAL-VFX-DIRECTION.md. Actual actors/equipment stay;
force comes from light, shadow, incomplete aura, compression and debris/dust.

## Subsequent elemental extension

Share the event-relative phase/lifecycle contract, not physical shape rules.
Preserve fire67 as an equivalence baseline, then choose actual elemental
representatives from the complete data inventory. Family and weight are
independent axes: high damage does not imply a stomp or larger copy of every
shape. Elemental material, travel, emission and release remain distinct.

## Resolved chain / sweep contracts

Triple-horn uses actual volley count, not three fabricated events. Its intact
source frames stay owned through the actual contact span and return on cancel,
death or drain. Early engine death can reduce the sequence to one event.

Sweep candidate78 uses each authoritative target in original order, with 45ms
per-target display staggering inside each volley. This changes no damage, game
state, target selection or mechanical timing. Only the primary target uses the
reachable weapon-contact lock; other resolved targets receive depth-aware
optical travel and local response, without extra per-target source swings.

PresentationTimeline routes EVERY HpLoss<=0 damage event to the existing
system:outcome / SystemOutcomeFx path. Guard/nullified feedback and numerical
contact accountability therefore remain unchanged. The zero conditional sweep
volley does not extend source ownership (VisualVolleys); positive or absorbed
attempts do. No new physical outcome attenuation is exercised or retained.
The unused 75/76 outcome enum/alpha plumbing was removed in78 after native route
auditing exposed it. Actual damage/source routes and selected75 visuals remain.

## Explicit geometry batch 79 (candidate)

- Cut: black_shiv_token, black_shiv_strike, green_wind_cutter.
- Blunt: green_twig_strike.
- Thrust: green_horn_thrust, green_charging_horn; resolved chain: green_horn_volley.

These exact IDs have existing physical geometry routes. Reuse the selected
family's phase/material contract, not arbitrary shape inferred from names.
Card-specific token creation, momentum scaling, pierce and actual emitted
contacts remain authoritative engine behavior. Source motion now reads the
explicit registry directly, avoiding a second independent list of motion aliases.
Each new route requires an actual dispatch/event receipt and gameplay-size review;
this candidate table is not a claim of completed coverage. No support, hybrid,
exhaustion-trigger, claw/fang or enemy route is included in this batch.

## Optical rake representative 80 (candidate)

black_rotten_claw retains its single authoritative 8-damage contact. Three unequal
near-parallel optical tearing paths share one crest and movement, then lose their
white core before amber fragments separate. Their visual count is NOT a hit count.
The actual source actor/equipment is unchanged; no claw, hand or other body model
is summoned. Native gameplay-scale identity and cadence are pending review.
After the bounded clear-physical batch and this representative, prioritize water
and ice material/motion representatives, rather than waiting for all physical IDs.

## Elemental water representative 81 (candidate)

blue_strike uses the shared event-relative phase/ownership contract through a
separate ElementalVfxProfiles registry. Fire phase/motion values are retained
exactly; physical family and reachable weapon-contact rules do not apply to spells.
Water tracks the existing actual cast focus through gather, freezes launch, then
connects a wet ribbon to a compressed foamy contact and opening liquid sheet.
Three registered RGBA phases share (.40,.56); import limit512+mips, raw originals
retained separately. Cool scene response and gravitational droplets distinguish
water from fire/physical light. Original generic contact audio remains: no fake
water-labelled fire/strike sound. No card effects, draw count or targets change.
Native material/anchor/cadence and fire-equivalence checks are pending.
Rake80 remains separately unselected pending receiving-surface registration fix.

## Rake registration correction 82 (candidate)

80's coherent crest was too source-side: paths crossed above the hero and ended
near the HP plate.82 keeps a short seed at actual locked contact, trims the
backspan and shifts/flattens the shared rake onto the receiving face above the
HP plate. It does not add paths, extra hits or scene-light gain. Water81 remains
a separately reviewed candidate; inclusion in a snapshot is not acceptance.

## Water motion correction 83 (candidate)

81 established liquid material and cool reflected mass, but its travel remained
thread-thin and late perforated sheet too static.83 retains the accepted contact
material/scale, widens connected flow, adds a compact moving liquid head, and
lets registered upper/lower breakup masks sag at different rates under gravity.
Masks overlap along an irregular neck on full-canvas quads, avoiding crop seams;
white folds disperse/cool before the final droplets. Duration/hit count unchanged.
82 rake standard cycle is accepted; size/remote gates remain separate.

## Ice representative 84 (candidate)

blue_ice_lance uses actual IceBlock-derived damage; no decorative extra hits or
resource modification. A real blue_guard prerequisite is used for native QA
(the current blue leader fixture yields6 IceBlock, not assumed5). Crystal focus
gathering and a short accelerating impulse precede fast radial facet growth.
The existing ice-crystal-surface-r2 intact/broken atlas is reused unchanged;
broken groups rotate and fall independently, unlike water's sagging sheets.
No metal weapon/hilt or physical anatomy is added. Phase and damage lifecycle
are shared, but material geometry/motion and cold reflected crest remain distinct.
83 water standard native art is accepted; applicability gates remain separate.

## Ice fracture correction 85 (candidate)

84 growth and cold scene peak were useful, but release still read as a planted
crystal fading.85 preserves those peaks and rotates each existing broken group
about its own approximate shard centroid, with unequal angular velocities and
outward/downward ballistic paths. The central attachment shrinks away after
fracture onset; no extra tiny-particle count, soft liquid bending or extra hits.

## Nonattack defence representative86

SupportVfxProfiles selects validated owner-action gains separately from attack
registries. Only blue_guard first positive IceBlockGained is opted in; later
passive events remain untouched. See NONATTACK-VFX-CONTRACTS.md. No new attack
allowlist entry or inflicted-hit route is created. Native support art pending.

## Candidate92 material axis

PhysicalVfxMaterial is independent of shape, weight and targeting. Only Mineral
(existing default) and Ember (red_finishing_flame trial) are implemented. This
is presentation material, not an engine element/damage-type override. The ember
thrust retains actual weapon delivery and focused axis, with a small eroding
thermal residue and falling/cooling fragments. It does not cast a fireball, apply
burn, or grant pierce. Actual execute5/15 uses the engine quarter-HP predicate;
no visual rule computes damage. Wood/leaf/shadow/light materials remain deferred
until actual material/motion evidence, not enum-only coverage.


## Revision109 narrow mapping candidates
- white_severance: physical/slash metadata, damage13 followed by separate spark-token draw/discard/hand events. The earlier spark-coated-material hold is withdrawn: it was assistant inference from gameplay axis, with no authored route/art requirement. Reuse Cut/Mineral without changing token semantics.
- fusion_practice_swing: first explicit fusion presentation candidate, Block5 then damage7; conditional free base cost and any additive aura costs remain engine-owned. No implication that all fusion recipes or effects have been migrated.
Native shape/command-boundary validation is pending for both IDs.


## Revision115 evidenced neutral-route proposal
red_final_blow moves from legacy minimal neutral to an explicitly chosen Cut presentation. Its actual illustration shows a broad axe/blade in close combat; this is moderate-confidence art evidence, not a claim that neutral metadata implied slash. Native quality remains a gate.
The execute10/28 threshold remains fully engine-owned. No fire, gore, extra axe/body or extra hit is inferred. red_reckless and red_flurry are not automatically admitted by name, type or top-level slash fallback.


## Revision118 abstract neutral representative
red_flurry (灼熱の連打) is an explicitly selected abstract local-pressure presentation, not an assertion that its ambiguous illustration depicts a particular weapon/fist or literal flame. It reuses compact impulse material with renewed source load per actual hit and .22 presentation interval.
The engine's three7-damage payloads and early-invalid/death omissions remain authoritative. A warm pressure crest does not apply burn, summon a body part or add visual hits. This approach may resolve neutral ambiguity without inventing unsupported literal anatomy, subject to native review.

## Revision122 isolated alias trial (not selected)
Based on selected118, excludes unselected LightScatter120. Four former neutral card routes use compact abstract pressure as an explicit presentation choice: green_sprint, green_tailwind, white_shining_strike, red_fang_breaker. Names do not introduce wind, light beams, jaws or fists. green_spreading_vine uses selected Vegetal cut/fibre/leaf material for its one real6-damage event; post-command copy-to-discard is independently observed, not a second effect hit. Paid/free Momentum5 boundary, pierce versus block, Weak-before-damage, Light-after-damage and copied token UID/zone require real-command QA.

## Revision124 Fire-family trial (not selected)
Based on pending123 consolidation; Fire aliases red_spreading_fire and red_double_tap require native selection. Six former red_strike checks now use the explicit Fire profile with card-source guard where route metadata is present: factory, authored audio, warm crest, rising target light, owned caster timing and fallback texture. Audio resolves to existing red_strike onset/contact; engine Burn/draw and actual hit counts are untouched. Exact red_strike phase/shape/light/source equivalence is required; double_tap must reload between two actual events and omit the second after first death. SpreadingFire draw is checked after actual Burn application, not from a guessed pre-play blaze state. No onLightDischarged data fixture exists: nine shipped definition JSONs/964 trigger fields contain zero such configured triggers; the engine hook alone is not test coverage.

## Revision128 ordered Growth contracts (not selected)
Based on selected124 Fire-inclusive checkpoint, excludes Earth125/126. VineDance reuses reviewed Vegetal on each actual emitted contact in five Growth1→damage2 groups; do not flatten final damage to2 or suppress later Growth after target death. VerdantHour uses neutral pressure: Growth1→5→conditional6 at currentGrowth>=3; Wrath uses compact heavy neutral pressure36→Growth1 (retain), never a literal tree. Growth receipts are not extra hits. Boundaries require actual prerequisites where feasible, living/earlydeath/blocked and source-owner interruption. No green_entangle slash alias: its plant_bind_contact needs a separate shape gate.

## Revision131 ordinary Block trial (not selected)
Based on safe129, excludes candidate Bind130/132. Green/white_guard and green_entangle mode0 receive neutral optical bracing, not an ice recolor/solid shield. Context derives exact primary CardPlayed event reference + owning RunCommand_Combat/Command_PlayCard + CardUid in previous hand; no name-only/missing-damage mode inference. Positive source-null valid BlockGained with exact Block delta and unchanged HP/MaxHP/IceBlock claims once; mode1 passive Block and mixed white_shield_strike stay legacy. Existing numeric UI, watcher and independent source-bearing events preserved. Existing Ice/Prayer behavior is unchanged by optional context. No fabricated zero-Block or unconfigured onBlockGained hook fixture.

## Revision 135 ordinary guard trial
Independent from 131; no Bind opt-in. A broader feathered front bracing shoulder replaces equal concentric fine lines, with one localized edge and two unequal converging companions. No scene crest, ownership, numeric or rules changes. Native selection pending.

## Revision 136 conditional consolidation
Includes accepted134 fibrous Bind (green_entangle Mode1) and135 ordinary Guard (green_guard, white_guard, green_entangle Mode0). Attack and support ownership remain separate; no defence inferred from absence of damage.135 bounded gates and fresh136 alternating-mode/passive-block/old-support controls must pass before selected checkpoint. No source rules/target/count changes.

## Revision 137 conditional ordinary Block aliases
Exact primary-command ordinary Guard allowlist adds green_catalyst_light (Block5), red_catalyst_light (Block5), white_light_hoard (Block8 then Light2). Abstract brace is an explicit presentation choice, not material inferred from names. Fusion catalyst metadata does not imply an on-play discount. Frail and all subsequent resource/passive events remain authoritative. Requires135/136 acceptance and per-ID native rules/visual gates.

## Revision 138 conditional neutral-pressure batch
Adds red_reckless (damage14 then own HP loss3), black_blood_price (own HP loss6 then damage24), white_rally (6 per non-innate permanent), green_leaf_strike (pierce6 then actual intent-dependent draw/Growth). These are explicit abstract-pressure choices for neutral source contexts, not literal fists/teeth/plant or new elemental material. green_leaf_blade remains separately deferred. Engine events own selfdeath/hooks, actual zero/blocked outcome, current intent and number/order of contacts. Requires predecessors and native per-ID/boundary gates.

## Revision 143 conditional Fang consolidation
Preserves all selected138 aliases and exact141 opposed-pressure factory, with green_fang profile only. Earlier91/93/96/139 unselected iterations are not acceptance evidence.141 standard native selected; bounded contracts and143 fresh controls required before checkpoint. Actual96/139 fixtures both probes; spider is added applicability. Excludes unselected140/142 WaterWide.

## Revision 144 WaterWide ownership/staging redesign
From selected143. Rejects140 sparse seam and142 repeated per-target canopy topology. Optional readonly path captures ONLY actual direct-card damage-event recipients and scheduled relative contacts, including blocked attempts; first positive effect owns one continuously traveling crest. Other positive effects add only small local rupture/spray; all-blocked stays existing system outcomes. Captured arrays are copied/reset at every Begin, never synthesize events/targets. Presentation spacing .10 does not alter engine mechanics. Full original registered water phases used once with shared head pivot(.70,.62); no cropped dense patch/TextureMotion29. Actual path/ownership/cache/cancel and native art gates required.

## Revision 146 original WideWater material trial
Based on144 single captured path, not145 enemy trial. Earlier140 seam/142 repeated canopies/144 translating jet remain unselected. Three original1536x1024 RGBA full canvases preserve aspect1.5 and actual leading basal anchor(.88,.73), not bbox fit. One216-vertex deforming sheet per crossfaded phase: low rounded body, forward lip, then low collapsed foam. Captured ground points have only.025 clearance, no former.30 torso lift; first source approach uses the receiving ground height. Small event-local ruptures remain independent. Shader30 only wet advection, no recolor/worldlight increase. Native registration, visual evolution, actual path ownership and interruption gates still required. Originals SHA/provenance retained in authored-material workspace.

## Revision 145 isolated enemy-source representative
From selected143, excludes144 Water trial. Exact enemy_actual_execution:enemy_probe/poke only, selected AFTER original timeline/scenario scheduling using real matched execution log, proven actor move, one aggregate contribution and accepted player receipt. Card SupportsRoute unchanged; new native cache includes exact source identity. Existing enemy pose/lunge/contact schedule/audio/guard and other moves remain legacy. Only short incoming-axis contact/residue at hero changes, with no new spear/body or hero attack ownership. No-card EndTurn baseline143 is authoritative; native eligibility/mitigation/turn/lifecycle/art gates pending.

## Revision 147 enemy-facing registration trial
Exact145 eligibility/timing/source ownership/lights/poses remain unchanged.145 actual geometry was emitted before contact but mostly overlapped bright hero waist (68x16 projected), so art was not selected. Short incoming shoulder now occupies enemy-facing exterior negative space, ends at unchanged contact rather than penetrating across the hero, and short cooling remnants remain outside. No new beam, actor response attenuation or damage timing change. Actual baseline/control/native-art gates required. Excludes146 Water.

## Revision148 conditional consolidation
Combines146 original single-path WaterWide and147 exact enemy poke contact/release. Each bounded contract and fresh merge controls required before selected checkpoint. Enemy improvement is modest receiver-local contact/retract; existing lunge owns anticipation, not a premium whole-enemy-family claim.

## Revision149 installed permanent contact trial
Exact black_blood_candle/permanent/installed UID and positive enemy receipt only; serialized card: key never grants card SupportsRoute. Target-local neutral compression/remnants, no candle/fire/blood/projectile or new hero motion. Existing prayer/leader heals may emit multiple real onHealed hits, including heal0; no one-trigger assumption or causal return flow. Authored-green148 baseline proves legacy ordinary hero attack frames0→2 during the permanent hit. Separate exact installed-candle ownership guard now skips only that hero fallback PlayPose call, including blocked attempts; genuine prayer/support, other cards and retainers remain unchanged. Timing/audio/outcomes unchanged. Negative sourcekind/UID, blocked, real removed owner and replay/cancel require native proof.148 baselines and untouched source remain preserved.

## Revision151 displayed-age permanent pressure correction
Based149 exact source guard, no150 status mixed.149 geometry emitted but first displayed positive age already mostly closed, hiding shoulders inside receiver. Shift only contraction/release progression into actual displayed positive ages; tiny contact seed and all numeric/audio events remain at0. Same geometry sizes/palette/world gain, no second flare. Exact candle hero-fallback suppression retained independently. Native appearance and controls still required.

## Revision150 actual BurnTick status trial
Independent selected148 branch, excludes149 permanent. Real positive BurnTick + exact system:burn route + valid enemy receipt and owned event only. Existing status timeline, numeric/audio/stack decay/death rules unchanged; no DamageDealt spoof, originating-card link or hero source. Reuses compact fire-volume material for one local thermal bloom/rising cooling fragments, no projectile/full attack crest. Existing enemy actions after tick remain separately gated. Actual148 baseline shows only small orange fleck; native readability and status lifecycle/death/zero gates required.

## Revision152 conditional source consolidation
Combines151 installed-candle contact/source-pose correction and150 actualBurnTick localstatus. All bounded source/numeric/lifecycle gates and fresh merged controls required; no further geometry or sourcekind broadening.

### Evidence-backed inherited coverage correction
39 stale candidate labels from107–148 are now selected148-inherited-bounded, backed by exact-primary-ID native transitions and checkpoint scope,218 verified evidence hashes. Earlier34 selected rows are not freshly re-audited. Actual white_guard135 phone fixture exists; no claim of exactwhite_guard Frail/lifecycle reruns. Crossroad defence remains legacy no-leakage, not ordinaryGuard art.62attack+12support−oneEntangle overlap=73unique is accounting, not global quality proof. New permanent/status gates remain conditional until their separate QA closes.

## Revision153 exact censer/deck-exhaust necro trial
Fresh152 authored-green baseline proves legacy ordinary hero attack frames0–3 while actual source is exhausted corpse-kiss UID, not hero/censer actor. Exact owned-command previousDraw→currentExhaust card UID/definition/effect signature, absent priorExhaust/currentDraw, actual installed ambientcenser SourceUid and preceding CardsMilled/NecroFired corroboration gate only this case. SourceUid and PresentationSource.Uid are legitimately DIFFERENT. Other necro movement/ambient cases remain legacy, no generic NecroFired CardId ownership inference. Same serialized card key retains selected DIRECTcard gate unchanged. Exact eligible hit suppresses only falsehero fallback attack; positive receipt uses local porous Dark contraction with no source callback/packet/return-heal link, smaller footprint and decay within original legacy timing. Existing heal/Light/exhaust/target/death/audio remain independent. MatcheddirectDark control and source-kind/lifecycle native gates required.

## Revision 154 — isolated WoodBlunt material trial (pending native gates)

Based on selected 153. Exact `green_sapling_strike` has explicit `wood_blunt_contact` source context; this is not inferred from its name. Its actual weapon-led contact is retained. New dry muted wood fibre compression, longitudinal splitting and a few slender tumbling chips distinguish it from mineral pressure and green Vegetal leaves. No branch, club or body part is summoned.

One actual damage event owns one contact. The second 6-base event exists only when the engine accepts `EnergyMaxAtTurnStart >= 5`; same-turn EnergyMax gain is not equivalent. First-hit death, full absorption and cancellation remain authoritative. The .28 interval is presentation cadence only. Two original 1254-square RGBA canvases are copied byte-for-byte with measured common pivot (.34,.56), preserved full-canvas scale and 512/mipped import recipes. The common knot erodes early; native straw-bundle/held-object risk remains a real gate. No Wood acceptance is claimed by compilation or registry inclusion.

153 is separately selected: exact censer-mediated deck-exhaust `black_corpse_kiss` only, distinct exhausted-instance and ambient permanent UIDs, no borrowed hero swing or return-heal stream. Native 15 runs/10,228 checks passed; three shutdown-only notices and bounded resource-growth limits remain explicit.

## Revision 155 — isolated explicit Water/Light aliases (pending)
Based on selected153, not unselected154 Wood. `blue_catalyst_echo` uses selected Water for one actual damage4; crafting echo metadata is not a second hit. `white_light_dart` uses focused Light for single10 and actual Light2 cost. `white_light_volley` uses selected LightBolt cadence for three actual5 effects, with death truncation and blocked outcomes owned by engine. No rule, draw, resource, target or status changes. Exact-ID native/art gates required; no acceptance from registry inclusion.

## Revision 156 — conditional consolidation
Exact154 Wood plus155 explicit Water/Light aliases, no new geometry. Native merge gates may run only after both inherited technical and visual scopes close. Until then this is an unselected conditional snapshot, not a safe master. Fresh Wood high-threshold and alias controls required.

## Revision 157 — exact Izzet Lightning adapter trial
Based on safely selected156. This deliberately does **not** add `blue_aether_burst` to key-only Elemental/Physical/SourceShowcase registries. The owning real `Command_PlayCard` UID must match `PresentationSource` and an actual previous-hand Burst instance; owning `Current.LeaderId` must be `leader_izzet`. The already-resolved route must be card/Burst/lightning_discharge/LightningDischargeFx/lightning/not FixedSmall and have a valid positive enemy receipt. AetherDischarged alone has no card UID and is not used as global causal evidence. Other leader, wrong owner/source, zero Aether/no hit, and absorbed outcomes retain legacy paths.

A separate marker/cache factory reuses the unchanged lightning-discharge-surface-r2 halves for contact and broken-channel decay. A short unequal branched channel uses the actual supplied source position, not a new weapon or invented caster actor. Original .10 lead/.20 tail, ordinary source pose, event/audio/resource timing and default .35/.12 peak strengths remain unchanged; no global StageFocus crest. The Izzet procedural source was actually idle in untouched156 and this trial does not claim upgraded actor animation or polished character art. Native full-cycle/other-leader equivalence and exact provenance gates are required.

Untouched156 baseline: four runs617PASS, actual Breath Aether2 then Burst Discharged2/damage14/Aether0; zero emits only CardPlayed, no discharge/hit; real blue leader same14 plus independent IceBlock1 uses neutral renderer. Material trial is not yet selected.

## Revision 158 — isolated Lightning channel correction (pending)
157 source/leader controls passed660 checks, but its uniform thin polygon wire/perpendicular stubs failed the visual gate.158 changes only the main channel geometry: uneven stations/offsets, width hierarchy and cool feathered shoulder, acute tapering secondary forks, unequal brief segment release. Exact owner admission, original contact/tail, actor motion, peak strengths, local material and other-leader path are unchanged. No additional strike, world gain, texture or held duration. Native visual gate pending.

## Revision 160 — nested Lightning channel geometry trial
158 timing/ownership remained sound but its long clean bars read as a neon polyline.160 retains the broad path and short timing while splitting each span into unequal fine kinks, continuously varying width, and adding two substantial acute tapered jagged forks. No world-gain increase, new resource/strike, actor pose change or bitmap. Native gate pending;159 aliases remain independent and excluded.

## Revision 159 — independent explicit Light/Water batch (pending)
Based on selected156; excludes unselected157/158 contextual Lightning. Exact Light Lance30/Light4, Light Torrent3x9/Light3, single Water StormLash3xcurrent prior-card count, WaterWide Tsunami1xprior combat-total cards/all/exhaust, and TideSurge2xremaining hand/all. Their original definitions/events remain unchanged. Existing shared WaterWide path is per actual same-card receipts, not N copied fans; blocked/zero/sparse/death/cached-owner reset require per-ID gates. Current card removal precedes Tide's hand count and normal own counter increment follows effects. Real setup commands/cross-turn state, not fabricated counters or resources, prove conditions. No new material art, literal spear or name-derived lightning.

## Revision 161 — conditional consolidation
Exact160 contextual Izzet Lightning plus159 five existing-material aliases; no new geometry or rules. Run only after both inherited bounded technical/art gates close. Fresh Lightning and dynamic WideWater plus Light controls required before safe checkpoint. Until then this is an unselected conditional snapshot.
