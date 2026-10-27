# Next explicit physical batch — read-only design audit
2026-10-26 17:50 UTC. All 72 physical attack definitions compared against current source/data/cards.*.json: unchanged. Current PhysicalVfxProfiles.ForCard contains 14 selected physical IDs. No implementation/tests performed.

## Recommend FIVE, not an artificial eight
All payloads below are authored base amounts, NOT final damage. Actual event/receipt remains authoritative. Current engine adds Growth and, during card play, Momentum; Weak, Exposed, armor, block and other modifiers remain intact. No new literal animal/body/weapon geometry.

| ID / name | Exact ordered onPlay payload | Target/contact contract | Existing family / required gate |
|---|---|---|---|
| green_trample_charge 突進の助走 | Momentum +3, damage 2, damage 2; cost 1 | Selected enemy; up to two resolved hits. Momentum added BEFORE both hits, including onMomentumGained hooks. If first hit kills, no second hit event. | BluntImpact, standard. Pure profile mapping candidate; test resource + multihit ordering. |
| green_sig_stampede 巨象の突進 | damage 32, pierce:true; cost 4, retain | Selected enemy, one resolved hit, NOT AOE or literal elephant. Pierce does not bypass active burrow shell. | BluntImpact, Heavy weight independent of shape. Parameter/weight review, not new family. |
| green_sig_trample 踏み荒らし | Momentum +4, damage 8 pierce:true, damage 8 pierce:true; cost 3 | Selected enemy; up to two resolved hits, not ground AOE. Resource precedes damage/hooks; second absent after kill. | GroundPressure, tune lighter/repeated contact versus 50-damage finisher. Existing shape, timing/weight review, not blindly copying finisher duration twice. |
| green_sweeping_horn 薙ぎ角 | damage 6 target:all pierce:true, then Momentum +2; cost 2 | One pass over living enemies in index order; Momentum gained AFTER pass, not before. Only actual receipts; no horn object. | AxialThrust + ResolvedSweep targeting. Existing primitives but combined path requires AOE/ownership QA, not scalar mapping alone. |
| green_horn_storm 嵐の角 | damage 3 target:all pierce:true, then damage 3 target:all pierce:true; cost 2 | Two complete ordered passes; targets dead in first pass absent in second. NOT two impacts per initial target interleaved. | AxialThrust + ResolvedSweep. Existing family + two-pass event contract; do not infer rounds from total event count. |

Suggested order: trample_charge → stampede → trample → sweeping_horn → horn_storm. Preview one heavy and one multi-target representative before opt-in. This is recommendation, not permission to expand current implementation before root's preview gate.

## Remaining explicit slash/blunt/pierce/stomp: do NOT flatten material identity
Twenty unselected IDs total in these explicit families (including wood_blunt_contact). Five above + fifteen below. Fang/vine excluded and remain separate.

| ID / name | Actual payload / lifecycle | Disposition |
|---|---|---|
| green_sapling_strike 若幹の一撃 | single damage6; second6 only EnergyMaxAtTurnStart >=5 | Existing blunt shape, NEW wood material intent; conditional second contact. Candidate sixth only after wood direction is approved. |
| green_leaf_blade 落ち葉の刃 | cost0 + discardCost1, selected damage6, draw1 | Existing cut shape, leaf material intent not amber fallback; valid discard-cost command fixture, preserve draw. Candidate seventh only after material direction. |
| red_finishing_flame 燼の一突き | selected dealDamageExecute5 /15 if enemy HP <= floor(MaxHp*.25); cost1; no pierce:true, no applyBurn | Existing thrust shape, ember identity review. Name does not authorize fire damage/burn or armor penetration. Candidate eighth only after material + threshold QA. |
| red_billiard 玉突き | selected damage10; only if that target is now dead, damage10 once to first other living enemy by index; cost2 | Existing blunt shape, NEW conditional cross-target trajectory contract. Not AOE, random bounce, recursion, or guaranteed second hit. Separate batch. |
| black_strike 影の一撃 | play6; onSelfExhausted2 | Shadow cut material + trigger ownership, separate secondary event, not guaranteed two-hit play. |
| black_scythe 死神の鎌 | dealDamageDrain22; cost4/exhaust | Dark cut material + independent drain/heal contract; never fabricate healing from displayed damage. |
| black_shadow_blade 影の刃 | loseHp2 BEFORE damage8; cost0/exhaust | Shadow cut material, selfloss precedes outgoing contact. |
| black_oblivion_blade 忘却の刃 | damage6 /12 if exhaust pile count >=5; onSelfExhausted2 | Oblivion/shadow material decision + threshold and trigger ownership. |
| black_shadow_cleaver 影の大鉈 | damage8 then exhaustFromDeck1 | Shadow heavy-cut material; deck exhaust can trigger other card effects, not extra cleaver hits. |
| black_oblivion_axe 忘却の斧 | damage6 /14 if exhaust pile count >=5 | Oblivion/shadow material decision; threshold alters payload not hit count. |
| black_blood_spray 血飛沫の刃 | loseHp2 BEFORE damage13; onSelfExhausted3 | Distinct blood/dark material intent, not amber; outgoing and selfloss independent. |
| white_blade_prayer 灯りの刃 | damage6 then gainHp6 only perfectBlockLastPhase:true | Light cut material + conditional independent hero healing (including caps/zero rules). |
| white_holy_maul 灯火の大槌 | damage18 then gainHp5 | Light blunt material + independent hero heal lifecycle. |
| white_light_knockout 灯の大砕き | damage30 then addLight5 | Light heavy-blunt material; resource event not hit. |
| white_severance 断ち切り | damage13 then white_spark_token1 to draw,1 discard,1 hand | Light/spark cut material review; three distinct card-zone operations, no three extra attacks. |

Material labels in this section are DESIGN PROPOSALS grounded in names/current family, not claims that these elemental shaders already exist or that damage type changes. Current physical registry has no Material field; therefore material-preserving opt-in requires explicit rendering/palette design, not just ForCard copies.

## Source contracts and gates
- `data/cards.{green,red,black,white}.json`: exact definitions above.
- `RecoveredSource/DeckRogue.GodotPort.Presentation/PhysicalVfxProfiles.cs:5–16,72–99`: families/weight/targetpattern, no material axis, exact 14 selected IDs.
- `RecoveredSource/DeckRogue.Engine/Effects.cs:1052–1070`: ResolveEffectTargeted iterates live targets in index order for target:all.
- `Effects.cs:1536–1545`: final attack modifiers include growth/momentum and weak.
- `Effects.cs:2280+`: dead/missing target produces no outgoing event; pierce/active burrow/block/armor protections.
- `Effects.cs:2477–2487`: exhaust-threshold AmountMax and plain damage resolution.
- `Effects.cs:2512–2530`: MomentumAdded then onMomentumGained hooks.
- `Effects.cs:3219–3227`: execute quarter-HP predicate.
- `Effects.cs:3643–3664`: cleave first other live enemy only when first target dead.
- `Effects.cs:4428`: minEnergyMax uses EnergyMaxAtTurnStart, not current energy.

QA for proposed five: real commands against same-save oracle; exact event list, receipt ownership/order/target indices, no extra hit after first-hit kill; all-target first-pass kill and sparse/removed receiver; momentum before vs after pass; block/burrow/pierce protection; repeated play/cancel/scene transition yields no stale geometry/light/fence ownership. Camera/static-screen contract unchanged. Selected material fidelity and heavy vs standard duration judged in actual representative preview, not only headless green tests.

## 2026-10-27 exact material correction / candidate171
The earlier shadow-material rationale above is withdrawn for black_strike,
black_oblivion_blade, black_oblivion_axe and black_shadow_blade only. Actual
direct source profiles are physical/slash and inspected modern artwork shows
metal weapons, not a shadow plume; the blanket hold was assistant-inferred,
not a user constraint. These direct-card Cut candidates are not yet selected.
Same-key necro rows remain legacy under the source-kind gate. WhiteBladePrayer
retains real warm glowing-blade art intent through the selected warm Cut accent,
not a Light beam or a heal-return link. Other black cards are not generalized.
See CUT-ALIASES-171-EVIDENCE.json for original image/source hashes and mechanics.
