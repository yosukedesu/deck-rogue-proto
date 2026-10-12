# Reconstructed three-act atmosphere, 2026-10-12

This is a new implementation from surviving acceptance documents and three actual accepted native frames. It is not an exact-source recovery of b05a0da. The baseline is the recovered working project supplied by the runtime recovery owner, including decompiled original APK StageWorld code. Its original shader strings remain intact behind the new baseline switch.

## Inputs inspected

- The surviving STAGE_ATMOSPHERE_CHECKPOINT.md, reconstructed README, accepted StageWorld hook patch, and baseline handoff receipts.
- All three Library reference PNGs were materialized through the current Library flow and visually inspected before implementing: forest libfile_0fdc89b2e5c881918c57262a66344299 (802,554 bytes), mine libfile_d793b47973048191b1b448fc36680d93 (738,237 bytes), ancient city libfile_10e3a2e4cf188191b719478384cf96bb (757,233 bytes). Local hashes are in provenance/reference-images.json.

## Bounded implementation

Forest: distant cool blue-violet grade, low mist behind the actors, faint moon shafts with low endpoints and sparse deterministic motes. Existing local amber lantern lighting is retained.

Mine: stage-owned smoked umber/charcoal surface tint, restrained warm rear-wall air and sparse amber dust. Existing lantern/fire lighting remains local. No additional crystal illumination.

City: violet-slate stage-only materials, a silver shaft whose endpoint survives the phone crop, faint tier mist and sparse silver motes. No layout or source texture edits.

No stage geometry is rebuilt. Added unlit transparent air quads remain behind the actor lane. They retain depth testing, disable depth writing and shadows, and use colored ALBEDO rather than the historically broken EMISSION-only unshaded path. No screenshot copy, blur, DOF or post-process was adopted. Geometry, source lights, Environment, actor code/colors/timing, rules and gameplay RNG are unchanged. Surface tint is a stage-specific art treatment, not Unity optics parity.

## Guard and lifecycle

--stage-atmosphere-baseline disables every new effect. --forest-atmosphere-baseline and --other-stage-atmosphere-baseline disable their respective acts. Unsupported renderer, changed relevant source profile hashes, changed Filmic1.12 environment/white1, environment adjustments or camera attributes cause fallback. The guard is evaluated on stage build. Build/reset clears all owned resources; time updates use stage elapsed only. Metadata stage_atmosphere_active and stage_atmosphere_version provides runtime evidence.

## Verification and limits

Current candidate: r3, still unaccepted visually. Native r1 ran without shader exceptions and passed45/45 fixture/session/assertions; all9 all-act idle/contact/settled images were inspected. The r1 palette was rejected as too pale/warm in forest, too gray/bright in mine and too bright in city tiers. r2 calibrates stage-only palette, distant-air density/color and city shaft opacity from those images.

Build: 0 errors, 13 inherited warnings. 27/27 pure profile policy assertions. 70/70 structural/source-scope/alpha arithmetic checks. These are not visual acceptance, native shader validation, an aggregate gameplay pass, Android performance, or physical-device evidence. The target reference frames also include later UI and actor-color changes handled by other owners. The integrated normal Main must receive native 1170x540 idle, actual play and settled review across all acts before calling the reconstruction visually restored.

## Integration

The seven-file overlay is additive except for StageWorld's partial declaration and three lifecycle hooks. apply_overlay.py checks source hashes before copying, and is idempotent for the exact output. Example from workspace: python3 stage-atmosphere-reconstruction-20261012/apply_overlay.py runtime-recovery-20261012/project --check. Omit --check only when the runtime owner is ready. The active runtime project was not edited by this worker.

## r3 native-informed calibration

r2 improved mine/city playing-floor colors substantially and added a visible city shaft. It retained three art differences: too-saturated forest blues, lost amber lantern surround, and bright lower foreground platforms. r3 reduces forest saturation, retains the existing lantern's local warm grade, and adds a world-space lower-platform fade to stage surfaces only. The recovered forest shader is unchanged except one lantern-position uniform and a bounded local weight in its existing night-grade function; the pure shader comparison verifies this. No new light or Environment change. The stage-surface lower fade uses Godot's documented FOG output to blend only those stage pixels below the playing surface; it does not add a screen overlay or affect actors/VFX. API reference: https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/spatial_shader.html

r3 builds with 0 errors/13 inherited warnings; 27/27 policy and75/75 source-scope/alpha/lower-fade checks pass. Native r3 remains pending. UI M2 introduced opaque disabled enemy rectangles in r2 contact frames; this was reported separately to its owners and is not stage-art acceptance.
