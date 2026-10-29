# Canopy300 color candidate (not adopted)

Source implementation commit5eead9aa091e8e34cccba9253d610f3fd18c98c0; captures from final unity-canopy-coloronly,32frames/case. PNGs are unedited frame10 originals.

- baseline-10.png: original decoded-linear sample, normal TiltShift.
- candidate-10.png: canopyencoded=1, RGB-only explicit encode before gain; alpha masks unchanged. Normal TiltShift remains.
- candidate-notilt-10.png: same color candidate with global TiltShift disabled for diagnosis only. This global switch is not a product proposal.

Color-domain mismatch is supported by calibrated paired GPU measurements. Godot RGBA8 is not a sampled-float oracle. Encoded-vs-linear background blend, dark-channel residual and normal blur remain unresolved. Normal craft and whole-image parity HOLD; default canopyencoded=0. Do not use this candidate as a correction or PASS for other materials. Details: ../../canopy-color-candidate-2026-10-29.md.
