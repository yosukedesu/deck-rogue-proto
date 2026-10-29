# Selected326 Light Burst: scoped Unity port

Inputs and verified hashes: unity-checkpoint326-intake-2026-10-29.md. Raw PNG is unchanged (SHA2563430f200eeaf99ebbdb553efa1bd52f35f2dc0d5e135b481297bec5ede41b827). No new actor art or shared shader conversion.

LightBurstBinding captures the registered PlayCard and actual numeric receipt before presentation. It requires the white hero, previous-hand spell UID, Light1 to0, three living matching enemy identities with HP>4, one LightDischarged Spent1, ordered three damage4 and three Weak1 events. PresentationSource UID is distinct from the absent damage SourceUid. Source/family eligibility is evaluated after the existing presentation route populates the ticket; receipt commit, generation, current receiver and white hero are checked at actual drawing. No fabricated event or damage receipt.

Only this context replaces local_light main/core with private64/96 and4/6 geometry. The original48-vertex particles, source timing, world-light envelope and Weak cues remain. Dedicated cache slot restores indices after Mesh.Clear; retire clears binding, alpha, renderer visibility and mesh vertices. Diagnostic burst326=0 selects the old path. Other target counts and Light amounts retain the old path; this does not certify their art.

The private shader uses Unity hardware sRGB texel decode before filtering, matching the source shader's explicit per-tap Compatibility decode. This differs from the previously diagnosed Canopy contract. RGBA PNG import stays sRGB, bilinear clamp, no mips, uncompressed; UV uses Unity bottom-left sampling of the symmetric source ROI. HDR gain and alpha stay separate. Background-dependent blend and full scene parity have not been proved.

Two integration failures were found by real capture and corrected: initial reservation incorrectly demanded a fully classified ticket; the next candidate used a new renderer ID that was outside the existing light ownership allowlist and could add generic hit effects. The final code retains continuous-local-material-r1 as the ownership ID, with owned-light-burst326 exposed in detailed review data. Initial and v3 capture sets are diagnostic failures, not acceptance evidence.

Final build: unity-burst326-v4-build.log, Succeeded/errors0/warnings43. Existing JobTempAlloc shutdown notices remain; no Android or runtime performance PASS. Final actual capture and audit are documented in reviews/unity-burst326-20261029.

Visual qualification: Godot contact frames8/12/15 and Unity contacts are not assumed to align by absolute frame. Unity's existing UI discharge rays, actor hurt, world light, postprocessing and receiver scale differ. Neither raw equivalence nor successful geometry upload is a craft/world parity PASS. Canopy/floor experimental defaults remain unchanged and HOLD.

Final teardown verification: v5 build Succeeded/errors0/warnings43;2cases/140frames plus normal player shutdown with no exception. This fixes the v4 OnDisable retirement exception described in the review.
