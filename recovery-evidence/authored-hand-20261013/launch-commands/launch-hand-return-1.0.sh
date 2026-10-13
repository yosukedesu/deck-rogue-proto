#!/usr/bin/env bash
source /workspace/scratch/948944000d55/runtime-recovery-20261012/env.sh
OUT=/workspace/scratch/948944000d55/unity-parity-20261013/authored-hand-native/return-desktop-1.0
export HOME="$OUT/cloud-home" XDG_DATA_HOME="$OUT/cloud-home/data" XDG_CONFIG_HOME="$OUT/cloud-home/config" XDG_CACHE_HOME="$OUT/cloud-home/cache"
mkdir -p "$HOME"
"$GODOT" --path /workspace/scratch/948944000d55/unity-parity-20261013/authored-hand-native/candidate-project --audio-driver Dummy -- --unity-ui-manual --parity-out "$OUT" --parity-fixture /workspace/scratch/948944000d55/ui-parity-restoration-20261013/authored-hand-native-fixtures/slash.save.json --ui-text-scale=1.0 -uiscale 1 --hand-native-trace --hand-native-passive --hand-trace-frames 600 > "$OUT.log" 2>&1
