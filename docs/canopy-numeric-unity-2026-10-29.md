# Canopy300 Unity constant-UV GPU measurements

Executed the agreed9 UV points (.25/.5/.75)^2 at vertex alpha1/.5, age.0333333 and gain.72. Each is a64x64 constant-UV quad; read top-origin(32,32), mapped explicitly to Unity lower-origin(32,31). Samples use the actual active Canopy main draw's bound texture. Raw original SHA256 remains8984752ffbba540693e371684a1ed06947a4a20feec639408ac16b7776c1d6e3.

Windows native D3D11 player, RTX2070. Active project color space Linear; target and readback R32G32B32A32_SFloat, rt.sRGB=false, no depth/MSAA/post. Bound texture R8G8B8A8_SRGB, Bilinear, Clamp, mipmapCount1. No inspector or PNG color interpretation is used.

130 GPU readbacks:72 intermediate draws (sample RGBA, gainRGB/finalAlpha, outputRGB/finalAlpha, edge/consume/remaining/finalAlpha),54 final production-shader composites over opaque black/linear.18 gray/transparent black, and4 opaque calibration draws0/.18/.5/1. The intermediate QA shader clones the original formula with Blend Off; final composites use a new material of the actual production shader, original fragment and Blend One OneMinusSrcAlpha, with no live material changes. Unity has no output_rgb branch here; gain and output passes intentionally measure the same expression. All outputs include JSON center values, full64x64 native little-endian RGBA float32 buffers (rows lower-origin), and float EXRs.

Calibration, gain*.72, masks/finalAlpha, half-alpha, unchanged sample values between alpha cases, premultiplied transparent output and opaque background blend identities pass with maximum error1.0000000005838672e-7. This is numerical self-consistency only, not a claim about Godot or craft. Source-side values are pending.

At UV(.5,.5), alpha1:

- sampled RGBA=(.068359375,.11328125,.00631713867,.9902037)
- gain/output RGB=(.04921875,.0815625042,.00454834), final alpha=.9902036
- transparent composite=(.0487365872,.08076349,.004503783,.9902036)
- opaque gray=(.0504999347,.08252684,.00626713131,1)

Actual Canopy fixture completed32 frames and actual events were retained. The screenshot was inspected; existing dark fold remains HOLD. This QA hook executes only with canopypatches=1 at continuous frame8. No production gain, color conversion, timing, lights or gameplay code was altered.

Build v3 reports Succeeded/errors0/warnings43. Initial build had a missing namespace brace in the new QA class; corrected before capture. An overlapping v2 launch exited before building; all earlier logs are retained as failed attempts, not success evidence. v3 shutdown JobTempAlloc leak notices are retained separately; they do not establish a runtime leak in this probe, nor are they suppressed. Native player completed one state with zero reported exceptions.

Imp/Light receiver candidates remain unimplemented in this commit. Source inspection confirms floor materials derive from the Diorama material path, while actor uses StageUnitLit; target-only actor attenuation cannot be counted as a floor correction. Separate receiver identity and light-position matching must precede those candidates. Existing Monk320/321 behavior remains untouched.
