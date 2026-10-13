#!/usr/bin/env bash
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
BASE=/workspace/scratch/948944000d55/unity-parity-20261013/authored-hand-native
FIX=/workspace/scratch/948944000d55/ui-parity-restoration-20261013/authored-hand-native-fixtures
for style in slash water stomp; do
 case "$style" in slash) card=green_strike;; water) card=blue_strike;; stomp) card=green_finisher_stomp;; esac
 for scale in 1.0 1.3; do
  for role in baseline candidate; do
   OUT="$BASE/$role-$style-$scale"
   test ! -e "$OUT/frames.jsonl" || exit 3
   export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache"
   mkdir -p "$HOME"
   "$GODOT" --path "$BASE/$role-project" --audio-driver Dummy --fixed-fps 60 --disable-vsync -- --unity-parity-sequence --parity-out "$OUT" --parity-frames 140 --parity-warmup 90 --parity-quality high --parity-fixture "$FIX/$style.save.json" --parity-actions "$FIX/$style.actions.json" --parity-card "$card" --ui-phone -uiscale 1.6 --ui-text-scale="$scale" --hand-native-trace --hand-trace-frames 1800 > "$OUT.log" 2>&1
   code=$?; echo "$code" > "$OUT/exit.txt"; test "$code" = 0 || exit "$code"
  done
 done
done
