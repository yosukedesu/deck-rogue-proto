# Imp289 / Light305 actor receive candidates

Opt-in independent actor-only candidates: `impreceive=.16/.08` and `boltreceive=.16/.08`, default1 reset per state. Bind only from the existing owned Imp or owned LightBolt real Contact path, on the current impact component's StageUnitLit material. Existing generation/impact validity remains required. No world Light intensity/color/position/range, local FX, actor hurt, ScreenFlash, gameplay receipt or clock is deliberately changed.

The attack array/predicate is separate from Monk's heal array. Selected contract groups must have only compatible same-family/material/factor contributions with live ownership. Ambiguous/mixed groups, colliding light positions and more than2 entries decline attenuation. Only the matching two owned light positions are scaled in that actor material's attackLocal term. This does not dim other units or the floor. Arrays' active count clears on cancel/retirement; cached vector storage may remain and is not claimed zeroed.

Final v2 native suite:12 cases/825 frames. Imp and Light each have baseline/.16/.08/cancel/reset; two Monk self/Squire cases test non-interference with attack candidates enabled. All states completed with zero reported exceptions. Build v2 Succeeded/errors0/warnings43. Earlier light cases aborted because new review logging called Array.ConvertAll on an unset array; logging now handles empty arrays. Earlier failed runs are not counted as completed evidence.

Observed invariants:

- Actual event JSON equals baseline for each normal candidate and reset.
- Imp receive active frames36..39; cancel38 removes from38 onward. Light receives11..16,18..23,25..30; cancel12 removes from12 onward. Actual Light contacts remain11/18/25, age0, hit indices0/1/2.
- Other actor attackCount0 throughout. All final counts0. Following default-factor states have no attack receive entries.
- Actor `_Flash` series difference0 across each candidate/reset vs baseline. Imp ScreenFlash remains red(.9,.1,.1,.205),duration.25,unsuppressed.
- Actual world intensity max observed difference is .00022426 for Imp and .000194 for Light including reset, from separate-state float timing; max age difference2.8641e-6. No full float parity claim.
- Monk self/nonself attackCount0, existing healCount2 with factors.08, then clear0. This is scoped regression evidence, not a new all-context craft approval.

Native images: .16 reduces Imp white leg/body wash while keeping orange contact and the large bright floor; .08 is also nonzero but no final value is selected. Light .16 reduces white torso/leg dominance while preserving the contact and unchanged bright floor. Captured baseline/candidates retain normal ScreenFlash; red HUD overlay still dominates Imp overall. Craft/world HOLD remains for both.

Floor implementation is still pending. Source material GUID933532a4fcc9baf4fa0491de14d08ed7 resolves to the installed URP Lit shader, while actors use StageUnitLit. A floor-only private receive path must be designed and its actual renderer/material binding measured; scaling shared world lights would also affect unrelated scene receivers and is not selected. Simultaneous mixed-owner/collision fail-closed branches are guarded in code but not independently exercised by this suite. No Android performance PASS.
