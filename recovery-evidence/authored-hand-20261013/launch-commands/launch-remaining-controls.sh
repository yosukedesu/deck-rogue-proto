#!/usr/bin/env bash
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
BASE=/workspace/scratch/948944000d55/unity-parity-20261013/remaining-controls-ccdce85
OUT="$BASE/manual100"; export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache";mkdir -p "$HOME"
exec "$GODOT" --path /workspace/scratch/948944000d55/unity-parity-20261013/accepted-ccdce85-preview/project --audio-driver Dummy -- --unity-ui-manual --parity-out "$OUT" --parity-fixture "$BASE/current-fixture.json" --ui-phone -uiscale 1.6 --ui-text-scale=1.0 > "$OUT.log" 2>&1
