# Independent return2 face QA

Verdict: **accept candidate4** for face fidelity, apparent scale, and visual continuity.

Reviewed the actual 192×128 candidate4, its high-resolution source, candidate3, canonical idle, and unchanged return1. Compared native-size alpha composites on dark gray and white, plus nearest-neighbor enlarged detail.

- Candidate4 preserves the canonical idle's narrow eye footprint, fringe overlap, visible cheek area, and overall head/face proportions. The oversized, rounder-face problem in candidate1 is absent.
- Contrast is modestly stronger than candidate3 and idle: the eyes, hair accents, and bodice are darker, and highlights are brighter. This is visible in enlarged comparison but does not read as a different identity or a head-size jump at native size.
- Both hands remain plausibly placed on the continuous diagonal shaft. The small scythe angle/position adjustment provides a credible final settling step into idle; no visibly broken shaft, detached hand, or new anatomy defect was found.
- No visible glow, background rectangle, or stray effect appeared in the actual alpha composites on dark gray or white. The glow seen in the raw-image viewer is not visible in those composites.
- Numeric validation: candidate4 is RGBA 192×128, alpha bounding box (9, 3, 128, 116), with the last nonzero-alpha row at 115. Thus it has no nonzero alpha below row115. Its broad alpha bounds still indicate faint pixels outside the visible sprite, so this is not a claim of perfectly empty surrounding alpha.

No production image was edited. Review-only composite images were created alongside this note.
