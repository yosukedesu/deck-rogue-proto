# Imp289 / Light305 receiver candidates

Floor implementation and original baseline/combined images: commit7a00f118b7d500826a18113b6d74b4409900a9ff. All files here use final unity-floor-module, D3D11,1280x720, actual frame index in filename.

baseline: actor1/floor1. actor-only: actor.16/floor1. combined: actor.16/floor.25. Floor experiment is enabled only by command-line flag; default product has no floor override. Actual light intensities, hurt and receipt timing are preserved subject to recorded float differences. Grass/soil only; stones/props remain excluded. All normal craft/world parity HOLD; values are comparative candidates, not selected defaults.

16cases/1075frames, receipt equality/reset/cancel/Monk exclusion confirmed. Read validation-summary.json and ../../unity-floor-receive-candidate-2026-10-29.md. The new images add the actor-only comparison; no ZIP or full capture logs are included.

## Continuous evidence

The four MP4s are byte-identical copies of the existing floor-module capture movies, with no trimming, overlays or retiming. Imp: 55 frames (0–54); Light: 70 frames (0–69), 1280x720, 30 fps simulation. Matching *-clock.json contains the original per-frame records, compacted without changing values. Movie frame n corresponds to record index n. Imp contact is frames36–39; Light contacts begin11/18/25. Baseline actor1/floor1; combined actor.16/floor.25 with floor experiment enabled. The simulation times are in the records; this is not a wall-clock performance measurement.

All craft/world qualifications above remain HOLD. See movie-manifest.json for hashes.
