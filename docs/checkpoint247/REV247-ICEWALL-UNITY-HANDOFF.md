# IceWall 245 → 247 authoring handoff (final focused evidence; see final addendum)

## Port intent, not wholesale Unity replacement
Godot is the effect/material authoring reference. Keep the user's existing Unity Linear color, palette linearization, HDR-gain UV channel, triangle-count alignment and math/actor integration. Copy the intent of the exact factory and selector into Unity APIs; do not replace the Unity renderer, shader, math or gameplay classes with Godot files. Unity motion and real-device performance are not yet evaluated.

## Exact scope and unchanged gameplay
`blue_ice_wall` (氷壁) is cost 2, actual gainIceBlock 15. The blue leader separately gains 1. The fresh initial-energy-3 fixture has Energy 3→1 and Ice 0→15→16. The whole command transition and event oracle match selected245. This is one protecting wall, not two formations for the two gain receipts.

The new `SupportVfxProfiles.IceWallRoute` checks enabled/full tuple: support:blue_ice_wall/ice-gain, definition blue_ice_wall, sourceKind support, family ice_guard_gain, renderer Hd2dSupportFx, element ice, FixedSmall true. It is used before the unchanged generic IceGuard dispatch. Existing Describe/claimed-first-gain owner-action logic is unchanged. IceBlockGained has no SourceUid: the existing owner-action/first-eligible-slot evidence is NOT a stronger direct-effect-origin guarantee for nested events.

Other IceGuard cards, all non-IceWall factories, Stage/Actor, rules, camera, shader and renderer are unchanged. No added hit, block gain, pose or owner.

## Final candidate shape and timing
One main face projects outside the hero toward the enemy, with a short right/top side. Local bottom = -.48, top = .48 relative to the existing effect anchor. Main rim XY: (.12,-.48),(.66,-.48),(.73,.20),(.57,.48),(.27,.43),(.12,-.17). Bottom fixed; each height is bottom+(originalY-bottom)*Ease(age/.11). This is a local 2D/projected construction, not a new gameplay collider.

Visible rise Ease(age/.035); stable interval then fade 1-Ease((age-.28)/.23); sampler empty for age<0 or age>.52. No explosive shatter. The few existing release motes start .28 and are subordinate.

Side faces use rim edges 1/2/3 and offset (+.065,+.025*build,-.018); the main front z=.035. The dark side is visible on the enemy side rather than hidden behind the hero. Rim thickness remains .008 on top-facing edges, .004 elsewhere; rim alpha .42/.20 times existing opacity, not a white outlining frame.

Light functions remain byte-identical to246/old IceGuard: .45*Ease(age/.08)*(1-Ease((age-.16)/.18)); floor .18*Ease(age/.10)*(1-Ease((age-.18)/.18)); TargetPeak .055, FloorPeak .08, StageFocus 0. No global gain or floor-wash increase.

## Raw and material
`assets/Art/fx/hd2d-ice-wall-density-v1.png`: original generated RGBA 1254×1254, 2,813,683 bytes, SHA256 23811409e8e00ea0310533d8912228a773c9ae5bf366aa02bfe958c0106eef0a. Requested1024square was not the returned size; the returned original is retained without resizing or image editing.

One face uses existing authored-RGBA material path, Kind13, TextureMotion0. Main vertex alpha=.46*visible*fade; raw alpha supplies actual internal clouding/transparency. UV x=.06+(x-.12)/.61*.88, v=.94-(y-bottom)/max(.0001,build)/.96*.88. Thus upper transparent material stays upper and the texture grows with geometry. The old rectangular procedural frost strip is removed (empty batch), not alpha-stacked on the raw. Shader output/linearization follows the existing platform-native implementation; do not add a Godot sRGB branch blindly to Unity.

Current native import: lossless, mipmaps=false (observed test condition, not evidence that disabling mipmaps is best). Raw remains independent of engine import metadata. Evaluate Unity's actual projected texture filtering; no global import changes are part of this scope.

Batch order halo/side/face/frost(empty)/edge/motes; Kinds 6/13/13/13/1/2. Every NativeBatch allocates Count512. Maximum WRITTEN vertices 6/18/18/0/48/24, distinct from Count and from total active vertices. No new shader.

## Evidence qualifications
Fresh245 baseline:379 assertions, exit0, 9 natural keys, no shutdown-resource notice (known VSync driver warning).246 first extra QA Count assertion confused capacity with active vertices and failed before review; corrected503PASS/exit0 is kept separately.246 art held for glass-like uniform plane.

247 standard:504 assertions, exit0, 9 natural keys, full oracle unchanged. Act1.ogg/OggPacketSequence shutdown-resource notice recurred; this is NOT a clean shutdown. It is retained as a pre-existing unresolved audio lifetime issue, not silently fixed in VFX code.

Independent static-image review permits temporal validation: irregular basal white clouding, clear upper material and short thickness improve over246; no opaque snow board, face/numeric obstruction or rectangular raw crop. Upper straight edges still carry a slight aquarium-like reading. This is not highest-quality/Unity-complete certification. Continuous formation/stability/dissolve and minimum bounds remain pending here.

## Final evidence addendum — supersedes pending statements above
247 is a scoped selection candidate with **1923 assertions / 4 successful exit-0 runs**: standard504, directmovie577, shared-guard negative315, lifecycle527. These are repeated assertions across focused cases, not full game coverage. The standard and guard runs each emitted2 shutdown resources for act1.ogg; lifecycle emitted4 including card_draw.ogg/OggPacketSequence; only movie did not reproduce the shutdown-resource notice. All have the known VSync driver warning. Do not describe all four as clean. Audio/resource lifetime defects remain unresolved and no audio cleanup patch is included.

Shared blue_guard was played legally and retained its old4 native batches with no dedicated wall. Exact selector negative tests and four lifecycle cycles passed. The actor-dead component + cancellation case is a component lifecycle test, NOT an actual Engine lethal battle. No fabricated zero-gain IceWall case was added: blue_ice_wall has fixed15 gain and no legitimate zero path was established in this scope. Standard/movie already prove actual15+passive1 produces one owned formation.

Independent review decoded contiguous movie idx0–19: .033s basal sliver, .067/.100 fixed-base growth, .133–.267 stable wall, .300 onward dissolve, idx16 clear. Full70-frame JSON retains one receipt and one source anchor; effects remain empty16–69. No material reverse drift, floating base, second wall or re-flash. This is a **continuous-frame temporal PASS**, not direct equal-speed subjective viewing or Unity validation. The slight glass-like upper straight corner remains a documented aesthetic limitation, not highest-quality completion.

Direct movie:70 fixed simulation frames at30fps,2.333s,silent,474794bytes; SHA256 9dceebb85249b13e4190d8646f7860c2ad75dd9ab594b932247b24f4272b745a. Native readback/encoding wall time is not device-performance footage. Original 246QA capacity mistake and all held art/raw/log evidence remain preserved.
