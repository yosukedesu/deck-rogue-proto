DeckRogue Nine Element VFX — source atlas delivery

All PNG files are original built-in image_gen output copied unchanged under canonical names. All atlas images are 1774 x 887 RGBA, aspect 2:1, with eight equal normalized cells in a 4 x 2 grid. Unity receiving workflow may resize the entire sheet to 1024 x 512; do not crop individual artwork.

See manifest.json for timings, blend, pivot, order, import notes and per-asset QA. See prompts/ for the exact generation/edit instructions used. See qa/ for measured alpha/frame statistics. Exact regeneration is not deterministic.

The PNGs include their finished colors. White tint and normal straight-alpha blending are intended. The dark atlas contains both a dark body and purple lit edges; additive-only blending would lose the dark body. World receiving lights, cast/impact position, emitter size and card-route coverage are implementation work and are not painted into these textures.

Alpha QA threshold is 16/255 for visible-content bounds. Very low alpha residuals can exist outside visible bounds. No alpha cleanup or boundary trimming has been performed. The narrow light ray in frame 2 has 10.38 normalized pixels of visible clearance, less than the nominal 16px target but without clipping.

Final textures: fire, water, ice, lightning, dark, light, heal, vine, earth. Only these nine accepted/final source atlases are included; rejected alternatives are excluded. Vine and earth use a recommended .36-second lifetime pending receiving implementation.
