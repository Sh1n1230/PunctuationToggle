#!/usr/bin/env bash
set -euo pipefail

COUNT="${1:-50000000}"
OUT="${2:-test.txt}"

python3 -c "print('、。' * $COUNT)" > "$OUT"
