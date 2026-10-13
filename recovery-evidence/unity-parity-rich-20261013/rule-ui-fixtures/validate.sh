#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
source "$HERE/../runtime-recovery-20261012/env.sh"
dotnet run --project "$HERE/runner/Validate.csproj" -- "$HERE" | tee "$HERE/validation-rerun.log"
