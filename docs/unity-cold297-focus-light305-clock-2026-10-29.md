# Cold297 focus and Light305 clock/lighting investigation

Branch: `codex/unity-checkpoint305-20261029`; follows `780a90e8`.
Unity product baseline remains checkpoint192 (`5668c3d0`). This is a partial port, not completion of checkpoints305/312/318.

## Cold297: measured cause and local correction

The original Unity frost was a small blurred patch. Ordinary-depth solid role2 renders, and forcing depth Always does not restore the original frost. Actual role2 viewZ is -28.70278 versus receiver -28.98278: the intended .28 foreground bias exists. UV orientation and HDR gain .72 agree with Godot.

An isolated actual GPU draw of the unchanged private shader and imported sRGB texture gives RGBA [.203398466,.266781241,.305866718,.494117647]. A four-texel sRGB-decode-before-bilinear reference gives [.203483907,.267022501,.306271085,.494117647], maximum error .00040437. A gamma-space CPU-filter approximation is not the correct reference. Raw alpha and vertex alpha are present.

Disabling TiltShift only for a diagnostic capture restores the missing folded frost surface. Enlarging projection alone does not. The product correction draws only owned Cold297 after TiltShift (550), before URP postprocessing, using a dedicated RenderGraph pass (551), the original depth test, material formula and gain. The existing world blur, bloom, lighting, actor and other shaders remain unchanged. The ordinary forward pass is disabled while the dedicated pass is active, with a forward fallback when unavailable. Pooled render-pass lists are cleared before each recording.

Owned Cold297 alone uses projection factor1.36: original Unity bbox69.9x31.9 becomes95.08x43.37px, close to the Godot reference95.09x42.01. This is a local calibration for the measured fixture, not proof for other actor sizes. Bboxes are vertex bounds, not effective alpha coverage. All screen y coordinates require lower-origin conversion when compared with PNGs.

Actual final captures: normal, cancel-before, cancel-after, reuse, other-hail, implicit-target and forward-fallback, 70 frames each. Normal/reuse private frames4..9; cancel-after only4; cancel-before/other-hail/implicit none. All end with zero bounded visible draws. Each active focused frame executes exactly one draw and disables forward. Engine HP44→40 (hail44→38); damage4 and defense Ice4 plus separate passive Ice1 retain the earlier engine validation. These are local technical PASS. The focused candidate visibly restores the white-blue folded surface and asymmetric consumption; independent craft/parity review remains pending. Android, alternate bodies and unavailable/MSAA fallback failure paths are not certified.

## Light305: distinguish receipt, effect age, hurt and world lights

Historical Godot first receipt indices12,19,26; Unity legacy capture12,19,25. Setting the QA capture clock to30 before the actual command yields Unity11,18,25: equal seven-frame intervals, with capture-start offset. The prior transition from60fps dispatch to30fps capture introduces half-frame phase. Product contact scheduling was not shifted to force screenshot-index equality.

Unity resets effect age to0 at actual receipt; Godot recorded ages approximately0,.01333,.02667 against planned contact time. This semantic difference remains explicit and is not declared parity PASS.

Read-only actual capture shows identical material hurt flash .0231481474 and remaining duration .0416666679 at all three first contact frames. Light power .65 is also identical. Before correction, legacy target intensities .32370/.36232/2.76412 and floor1.05502/1.18089/9.009 expose a separate lighting inconsistency. The light envelope closes over a globally cached mutable effect: the next pending hit overwrites its sampled age, leaving earlier contacts with anticipation light while the third reads full contact light. Owned Light305 now retains a separate mutable effect per pool slot, reusing it only after retirement. This isolates geometry, committed state and light clock without changing shared world-light caps, raw texture, collision schedule or other routes.

After separation both capture modes give all three target intensities2.76412487 and floor9.008999, with unchanged hurt flash and original receipt indices. Six actual100-frame cases (normal, before/after cancel, reuse, other-glint, lethal) pass terminal zero-active/zero-visible and route exclusion checks. This fixes state ownership; the stronger first two contacts are not a claim that the global Godot lighting balance now matches. The live image still needs light calibration review. Latest Windows build succeeds; thirteen engine goldens match with exit0. Existing editor JobTempAlloc shutdown warnings remain unresolved.

## Remaining scope

Earlier273..288 full runtime migration, Imp289, Canopy300, Wraith312 and Crossbow318 remain incomplete; selected312/318 inputs were downloaded and verified, not applied. Original241..243 contract documentation remains partly unavailable although recovered code SHA matches. No main merge or pending-publication push was performed. The previous large reconstructed-input prepared upload remains blocked by unavailable preparation; new small evidence packets can be saved separately through Library.

Additional319 kit `libfile_f9ede344cd848191b258957ab29680b7` and movie `libfile_93529775018c81919e28819724002ac5` were materialized and verified against supplied sizes/SHA256; six ZIP members pass CRC. They are queued after318, not applied. RootWeave enemy negation-label scope must be checked against Unity's existing recipient behavior before changing it.
