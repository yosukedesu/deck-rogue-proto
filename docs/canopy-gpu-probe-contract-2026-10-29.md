# Canopy300 matched GPU probe and scoped light candidates

## Minimal source-side request

Start with Canopy main role1 at actual age **0.0333333**, gain **0.72**. One age is sufficient to investigate the initial missing fold; do not expand to Wraith/Crossbow until this comparison identifies a mechanism. Use three constant samples from the captured main mesh, in separate 64x64 patches:

| Patch | UV | Interpolated vertex alpha | Source triangle vertices |
|---|---|---|---|
| belly | (0.472222219, 0.16666666666666666) | 0.5838612233333333 | 60,61,62 |
| lower belly | (0.472222219, 0.6666666666666666) | 0.9059505826666667 | 66,67,68 |
| root | (0.8055555333333334, 0.16666666666666666) | 0.41305178800000003 | 108,109,110 |

These are triangle-centroid interpolants of actual frame8 vertex data, not claims that a particular original screen pixel had these exact interpolants. All vertices of each diagnostic quad carry the same UV/alpha, avoiding rasterizer interpolation differences. Read numeric RGBA at top-origin pixel (32,32). The accompanying JSON preserves the full original144-vertex record and precise texture UVs.

Source shader: `authored_Hd2dCanopySurface.gdshader`, SHA256 `f194b57f8e45546b06d6766de8b4da36dcda13726cfae47037a6cd6ff1299cf9`.
Raw: `canopy-organic-density-v1.png`, 1870x841, 2045221 bytes, SHA256 `8984752ffbba540693e371684a1ed06947a4a20feec639408ac16b7776c1d6e3`.
Unity shader: `DeckRogue/Hd2dCanopySurface` at `fe228bd28d25f4fefe9725141a23ed0ba2e2b7c1`.

Set numeric `_RolePrimary=(.15,.40,.08,1)` and CUSTOM0 RGB=(.108,.288,.0576), alpha per table, so the source dot-ratio gain is .72. Record actual uniforms and CUSTOM0 rather than relying on inspector color display or an implicit gamma conversion. Geometry role1 is a semantic label; the Godot shader consumes CUSTOM0 and the primary uniform, not a separate role input.

Texture: original bytes, linear filtering, clamp, no mipmaps, sRGB/source_color interpretation enabled. Godot crop is (.18+.64*u, .22+.56*v); Unity reverses only the resulting texture v. Record actual texture format and sampling result.

First retain the selected Godot renderer/backend and its real `OUTPUT_IS_SRGB` value. Do not switch renderer to satisfy a target-format request. For the diagnostic numeric target prefer **RGBA32F, linear, no MSAA/depth/post/exposure/fog**, with clear backgrounds (0,0,0,0) and linear (.18,.18,.18,1). If this backend cannot provide that format, report the actual supported format, output transfer function and numeric readback; keep that result explicitly unmatched rather than silently interpreting UNORM or sRGB bytes as float-linear values. A separate float diagnostic must record whether changing the target changes OUTPUT_IS_SRGB. Two backgrounds are required because straight-alpha vs premultiplied blending is part of the question.

For each patch return:

1. GPU-sampled organic RGBA, before gain/masks; actual import/backend/output flags.
2. `organic.rgb * .72`, and actual ALBEDO after the original `output_rgb` branch.
3. edge/consume/organic_consume masks and final ALPHA.
4. Final target RGBA on both backgrounds; label pre/post transfer and alpha channel blend semantics. Include native images and numeric readbacks, with capture orientation.

Intermediate values may use a read-only diagnostic shader clone that exposes each value into a float attachment, with blending disabled for that intermediate pass. Preserve the source sampling, expression order, precision and branch; preserve the original shader/blending for final composition. Report any unsupported output instead of replacing GPU observations with CPU calculations. Native source screenshots are useful context but do not substitute for numeric probes.

The existing Unity solo PNG is linear **ARGB32 UNORM**, premultiplied, transparent clear, full geometry with overlap, and D3D vertically reversed. It is not the constant-patch float oracle. Unity's matched patch capture remains to be executed; this contract does not claim new GPU results. If the initial samples match, the next diagnostic is full-mesh overlap plus native stage tone mapping, not a speculative gain/gamma patch. Age .100 is a second-stage erosion probe only if initial sampling differs or remaining-mask behavior needs isolation.

## Independent Imp / Light305 candidates

The preceding33-case capture proved owned world-light dominance, but it did not prove that all scene receivers should be dimmed. `StageFx.UpdateContractEndpoint` controls an actual Unity Light; scaling that endpoint affects forest and unrelated units as well. Therefore neither worldoff nor a global endpoint scalar is selected as the product fix.

Prepare opt-in candidates with **nonzero receiver factors**: baseline actor/floor=(1,1), A=(.16,.25), B=(.08,.12). These numbers are a diagnostic bracket, not a quality decision or an inference from Monk's selected .08. Run each family independently, including a baseline with the normal ScreenFlash intact. Keep world-light position/color/intensity/range, local mesh/raw/gain, contact receipts, effect age, actor hurt and ScreenFlash unchanged.

Scope actor attenuation to the actual current impact receiver material: Imp289 player only after its owned real contact; Light305 its exact bound enemy receiver, independently for each of the three committed hits. Scope floor attenuation to the stage floor material and only the same owned light positions. Other materials keep1. Match a live light only while the original contribution generation and receiver identity remain valid. Position collision, mixed group, excess entries or ambiguous ownership must decline attenuation (factor1), not affect an unrelated light. Keep Monk320/321 admission and existing receiver arrays separate.

A new attack-receiver path is needed: existing `RefreshOwnedReceivers` deliberately accepts only `heal-uplift-continuous-r1`, and StageUnitLit alone consumes the heal array. Reusing that predicate or changing its meaning would risk the accepted Monk cases. Floor's actual material/shader binding must be measured before adding its receive path; the floor path is not yet implemented or tested.

Acceptance observations: faces/clothes remain readable, a smaller warm/white local core remains, the floor pool stays subordinate and nonzero; forest/other units and whole HUD retain baseline behavior. Compare native continuous windows, light/receiver uniforms, actor flash, actual event sequence and contact ages. Then test three-hit reuse, cancellation and a following unrelated attack restoring factor1. No new candidate is selected, no product code changed, and all Imp/Light craft/world HOLDs remain in effect.
