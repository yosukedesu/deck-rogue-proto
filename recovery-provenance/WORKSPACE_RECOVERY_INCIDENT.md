# Workspace recovery incident, 2026-10-12 UTC

## Confirmed observations

- The previously used `deckrogue-godot-port` checkout, local Git objects, later APKs and installed tool directories were absent from the accessible workspace when work resumed around 16:04 UTC.
- The latest recorded local source commit was `b05a0da04a2e71e6a5f090286348d4f6e4514c81`. It had not been pushed. A GitHub lookup did not find that commit in the connected repository.
- At 17:04:57 UTC, `/proc/stat` reported `btime 1791819513`, corresponding to 15:38:33 UTC.
- `stat` showed scratch-directory birth at 15:44:14.704868742 UTC and shared-directory birth at 15:44:33.780732048 UTC.
- The shared directory retained an older modification timestamp. Read-only investigation also found surviving older files with October 4–7 modification dates but October 12 15:44 birth/change timestamps.
- The authorized earlier cleanup removed only three downloaded tool archives, totaling 1,494,659,951 bytes. This alone does not account for the much larger observed filesystem difference or the creation timestamps of surviving files.

## Interpretation and limits

These observations support a newly materialized or restored filesystem view containing older files. They do not establish the mechanism, trigger, permanent loss, or whether a newer workspace still exists elsewhere. The command executor and cloud desktop have not been proven to share a boot or mount identity. No supported cloud restore action was identified in the available status tools.

## Recovery boundary

The published APK, accepted engine source archive, selected source packages and visual reference images survived in Library/Drive. The APK preserved compiled application code and exported resources, but its 106 authored C# entries were placeholders. Portable PDB metadata retained source paths and checksums, not authored source text. Twenty-five original engine files match those checksums. The remaining Godot source is being reconstructed from the compiled application and separately recovered original fragments.

The later UI, actor-display and stage-atmosphere improvements are separate recovery work. A successful build or engine-golden run does not prove those later improvements, runtime behavior or visual parity were restored.

## Preservation correction

Each recovered milestone is archived outside the execution workspace in the user's Library, then independently downloaded and hash-checked. Original bytes, historical records and reconstructed source remain separately labeled. A local Git commit alone is not treated as a durable backup.
