#!/bin/bash
# ==============================================================================
# Manual Recompile & Sync for All Indicators
# Usage:
#   ./recompile.sh              (sync all indicators)
#   ./recompile.sh volatility   (sync specifically volatility stop)
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PYTHON_EXEC="/home/mk/quant_env/bin/python3"

if [ ! -f "$PYTHON_EXEC" ]; then
    PYTHON_EXEC="python3"
fi

"$PYTHON_EXEC" "$SCRIPT_DIR/scripts/sync_indicators.py" "$@"
