# Bounded hand gate review

Independent read-only review, 2026-10-13 04:06 UTC. Nine native receipt assertions pass. Reviewed actual native PNGs for airborne hand and the changed fan; no new source-level blocker found.

- 100% real-desktop incoming cancel, module 3fcc4737-936c-41a0-9f03-ff1af6a2ccf2: attempt5 process 1091 pressed Guard with four other deals active; manual 004–007 are byte-identical, including RNG and revision 4.
- 130% final lifecycle module f11c468c-2a94-441e-a1d3-0e7d7bdce48f: low cancel manual 005→006 preserves the full run.
- 130% stationary-pointer modal open/close 007→008: overlay changes true→false; hover UID null, drag UID null, zero hover clocks, unchanged full save. Normal test time 1.
- 130% other-card play during Strike return 009→010→011: Guard is played once; Strike immediately adopts its new canonical seat (387.04556,585.6666), then remains exactly there after returning to normal time. No stale return overwrites it.
- 130% airborne dwell 013→014: Strike hovered with four deals active at frame 2126; 14.463572 seconds later at QA time scale .01, pointer-down 2265 still belongs to Strike, with one other deal active and zero hover clocks. Exactly one Strike command occurs 2279 targeting enemy 1, energy 3→2 and one 6 HP loss. Neighbor hover does not reassign the pressed UID.
- Earlier bound Linux app.drag cancel route is explicitly excluded because global pointer position did not follow its supplied path. Real desktop input supplies the valid cancel receipts.

Machine report: final-hand-gates.json. The reviewed pointer-log prefix is frozen through frame 2280 as attempt6-pointer-events-through2280.jsonl; later runtime events cannot silently change this evidence.

This accepts the named input/lifecycle regressions. It does not claim complete Unity hand parity, Android timing, or that slow-time interception proves normal animation cadence. Continuous normal-time deal/float evidence is separately recorded in REVIEW.md. Runtime owns any remaining final normal-time/route closure and integrated checkpoint acceptance. Outgoing discard flight, dedicated source drag-return 0.15 s, full simultaneous Unity tween equivalence and the measured float UI-proxy anchor gap remain documented separately.
