#!/usr/bin/env bash
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
OUT=/workspace/scratch/948944000d55/unity-parity-20261013/remaining-controls-ccdce85/abandon-nullsafe
export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache"
mkdir -p "$HOME"
"$GODOT" --path /workspace/scratch/948944000d55/unity-parity-20261013/authored-hand-native/baseline-project --audio-driver Dummy -- --unity-ui-manual --parity-out "$OUT" --parity-fixture /workspace/scratch/948944000d55/rule-ui-fixtures-20261013/fixtures/reward-two-rounds.json --ui-phone -uiscale 1.6 --ui-text-scale=1.0 > "$OUT.log" 2>&1
