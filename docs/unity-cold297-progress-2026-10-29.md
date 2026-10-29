# Unity Cold Sweep 297: technical integration, visual review pending

Dedicated branch `codex/unity-checkpoint305-20261029`, based on original main `05ecb302` and Unity checkpoint192. Previous integration commit `fc571039` contains LightBolt305. This is a partial checkpoint305 migration, not acceptance of all effects. Original main independently advanced to `adf7786d` and remained clean when inspected; it has not been reset, merged, or pushed by this work.

## Implemented and observed

Exact selected297 sampler, original1774x887 frost PNG, private URP shader, actual command/UID/target ownership, current-generation committed receipt gate, and pool reset are integrated into the existing BoundedContact route. The existing .18-second contact lifetime, actor timing, gameplay, and shared materials stay in use. Raw sRGB decodes once; linear role vectors and HDR gain remain separate from alpha. The 160-vertex buffer uploads all240 triangle indices. Godot image-top-left crop rectangles are converted into Unity bottom-left texture coordinates; the same missing conversion was also corrected in the previous LightBolt shader. The earlier LightBolt Library packet is historical, before this UV correction.

Cold297 editor check passed60 geometry samples, actual4 damage and separate IceBlockGained4 then passive IceBlockGained1, actual receipt commit/generation reset, and original texture dimensions/no mipmaps. The first test incorrectly looked for StatusInflicted; its failure is retained, and the corrected assertion checks the actual IceBlockGained event type. No gameplay implementation was changed to make it pass.

Windows build succeeded with0 errors and39 warnings. All13 Unity engine golden replays passed after integration. The first build stopped with exit-1 before completion; retry succeeded. Editor shutdown additionally reported JobTempAlloc warnings. The precise cause of the first termination is unconfirmed. The first regression invocation selected the internal VerifyCore entry without an exit wrapper; that owned process was stopped and the proper VerifyGoldens entry completed with exit0.

Actual visible D3D11 game capture: normal Cold private draw is present on consecutive frames4..9, age0..166.7ms, and absent from10 onward. Final engine HP44->40. Same-process six cases passed: normal, cancel-before(frame2), cancel-after(frame5), reuse, other IceHail(real6 damage), and implicit singleton target. Cancel-before has no private draw; cancel-after has only frame4; reuse repeats4..9. Every case ends with zero bounded active/visible renderers. Hail and implicit-target never select the private Cold shader. This intentionally preserves the supplied explicit-target ownership scope; ordinary singleton UI may still use the old renderer.

First Cold geometry projects approximately69.91x31.89 pixels at1280x720, scale1.7627 and gain.72. The Godot handoff reports95.09x42.01 with a different stage/camera/actor footprint. These are geometry bounds, not a proof of fragment coverage. Actual PNG comparison still finds the Unity frost face hard to read. **Visual/commercial-quality acceptance remains HOLD.** Do not silently enlarge all effects or alter global lighting to conceal this difference.

UV-corrected LightBolt was recaptured for70 consecutive30fps frames: actual contact commits12,19,25;3x4 damage, HP43->31; terminal active/visible0. Godot's source movie contacts12,19,26, so the third-hit timing discrepancy remains documented. Existing Unity actor/queue timing was not changed.

## Inputs and remaining scope

305 reconstruction still has302 exact logical matches,0 mismatches, and3 unavailable original Lightning241-243 historical contracts; recovered243 code matches its provided SHA. Full Lightning live route and the other selected effects through305 remain incomplete. Wraith312 is fetched/hash-verified but not applied. Added Crossbow318 kit and movie are fetched and exact-size/SHA256 verified (kit19 ZIP members); not yet applied or visually reviewed. Apply312 then318 only after their prerequisites.

318 kit `libfile_c994c6bbb7108191a807aae7891fb305` v0,300986B,SHA256 `c71080fa4915caf45210373a2b9ffb7e65597a65f6293fc6584659367e7b3bc3`; movie `libfile_9cd18915a5948191baa3ee51d16491a9` v0,776934B,SHA256 `f3be816c137d4b2b9854f83db78c88c9fa296cea27c1998b0958d397cbf3440c`.

Visible game execution is authorized and works; there is no pending approval request. Earlier hidden/batch captures were black, with no proven graphics root cause. Library's earlier large reconstructed-input upload remains blocked by unavailable prepared-upload capability after auth recovery; this is separate from local implementation and the successfully saved earlier small LightBolt packet. No Android device performance, other-body validation, complete305/312/318 acceptance, or all-Ice quality claim is made.
