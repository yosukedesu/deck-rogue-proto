#!/usr/bin/env bash
set -u
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
ROOT=/workspace/scratch/948944000d55
BASE="$ROOT/unity-parity-20261013/ownership-native"
for SPEC in 'green_double_lash 2' 'green_horn_storm 6' 'blue_strike 1' 'green_sweep 7' 'green_sig_vine_dance 20'; do
 read NAME EXPECT <<< "$SPEC";OUT="$BASE/$NAME";export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache";mkdir -p "$HOME"
 "$GODOT" --path "$BASE/project" --audio-driver Dummy --fixed-fps 60 --disable-vsync -- --unity-parity-sequence --parity-out "$OUT" --parity-frames 110 --parity-warmup 60 --parity-quality high --parity-fixture "$ROOT/vfx-rule-fixtures-20261013/fixtures/$NAME.json" --parity-actions "$ROOT/vfx-rule-fixtures-20261013/fixtures/$NAME-actions.json" --parity-until-settled --parity-expect-damage "$EXPECT" --parity-key-frames --ui-phone -uiscale 1.6 > "$OUT.log" 2>&1
 STATUS=$?;printf '%s %s\n' "$NAME" "$STATUS" >> "$BASE/batch-exits.txt";[ "$STATUS" = 0 ] || exit "$STATUS"
done
BASE="$ROOT/unity-parity-20261013/ui-rich-r2"
for TEXT in 1.0 1.3;do
 OUT="$BASE/float-desktop-correct-$TEXT";export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache";mkdir -p "$HOME"
 "$GODOT" --path "$BASE/project" --audio-driver Dummy --fixed-fps 60 --disable-vsync -- --unity-parity-sequence --parity-out "$OUT" --parity-frames 140 --parity-warmup 60 --parity-quality high --parity-fixture "$ROOT/ui-parity-restoration-20261013/float-size-native-fixtures/desktop-128.save.json" --parity-actions "$ROOT/ui-parity-restoration-20261013/float-size-native-fixtures/desktop-128.actions.json" -uiscale 1 --ui-text-scale="$TEXT" > "$OUT.log" 2>&1
 STATUS=$?;printf '%s %s\n' "correct-desktop$TEXT" "$STATUS" >> "$BASE/batch-exits.txt";[ "$STATUS" = 0 ] || exit "$STATUS"
done
