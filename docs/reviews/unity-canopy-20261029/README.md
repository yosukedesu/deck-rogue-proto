# Canopy300 color candidate (not adopted)

Source implementation commit5eead9aa091e8e34cccba9253d610f3fd18c98c0; captures from final unity-canopy-coloronly,32frames/case. PNGs are unedited frame10 originals.

- baseline-10.png: original decoded-linear sample, normal TiltShift.
- candidate-10.png: canopyencoded=1, RGB-only explicit encode before gain; alpha masks unchanged. Normal TiltShift remains.
- candidate-notilt-10.png: same color candidate with global TiltShift disabled for diagnosis only. This global switch is not a product proposal.

Color-domain mismatch is supported by calibrated paired GPU measurements. Godot RGBA8 is not a sampled-float oracle. Encoded-vs-linear background blend, dark-channel residual and normal blur remain unresolved. Normal craft and whole-image parity HOLD; default canopyencoded=0. Do not use this candidate as a correction or PASS for other materials. Details: ../../canopy-color-candidate-2026-10-29.md.

## Existing role-specific projection evidence

role-projection-summary.json is a read-only extraction from the already captured baseline/candidate/candidate-notilt clock records, frames7/8/9/10/11/12/15. Main kind40/role1 starts at about74.11x36.62px for enemy0, top-origin y291.39..328.00. Alpha range.133..1 is vertex alpha only. The camera-Z difference is relative to the captured contact anchor, NOT a newly measured opaque actor plane. These are positive-alpha vertex envelopes, not fragment coverage or art equivalence. No raw/depth cause is inferred, and no new capture or pixel modification was performed. Static craft HOLD from independent normal-image review remains.
