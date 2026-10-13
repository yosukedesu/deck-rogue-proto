#!/usr/bin/env bash
set -u
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
BASE=/workspace/scratch/948944000d55/unity-parity-20261013/ui-rich-batch
FIX=/workspace/scratch/948944000d55/ui-parity-restoration-20261013/shared-rich-native-fixtures
for SPEC in 'solo100 rich-solo idle 1.0 3 60' 'solo130 rich-solo idle 1.3 3 60' 'four130 rich-four idle 1.3 3 60' 'ordinary100 ordinary-permanent ordinary-permanent 1.0 140 60' 'turn130 owned-set-turn owned-set-turn 1.3 390 60'; do
 read NAME FIXTURE ACTION SCALE FRAMES WARMUP <<< "$SPEC"
 OUT="$BASE/$NAME";export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache";mkdir -p "$HOME"
 "$GODOT" --path "$BASE/project" --audio-driver Dummy --fixed-fps 60 --disable-vsync -- --unity-parity-sequence --parity-out "$OUT" --parity-frames "$FRAMES" --parity-warmup "$WARMUP" --parity-quality high --parity-fixture "$FIX/$FIXTURE.save.json" --parity-actions "$FIX/$ACTION.actions.json" --ui-phone -uiscale 1.6 --ui-text-scale="$SCALE" > "$OUT.log" 2>&1
 STATUS=$?;printf '%s %s\n' "$NAME" "$STATUS" >> "$BASE/batch-exits.txt";[ "$STATUS" = 0 ] || exit "$STATUS"
done
