#!/bin/bash
# Mod Maintenance Cron Script (unified sweep for all mods)
export PATH="$HOME/.local/bin:$HOME/.npm-global/bin:/usr/local/bin:$PATH"

REPO_ROOT="D:/Dev/Projects/GameModding"
LOG_DIR="$REPO_ROOT/maintenance-logs"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/maintenance-$TIMESTAMP.log"
mkdir -p "$LOG_DIR"

cd "$REPO_ROOT"

echo "[$TIMESTAMP] Starting unified mod maintenance sweep..." | tee "$LOG_FILE"
echo "claude: $(which claude)" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

claude -p "/mod-maintenance" \
  --dangerously-skip-permissions \
  --max-turns 50 \
  2>&1 | tee -a "$LOG_FILE"

echo "" | tee -a "$LOG_FILE"
echo "Exit code: $?" | tee -a "$LOG_FILE"
echo "Done. Window closing in 30s..."
sleep 30
