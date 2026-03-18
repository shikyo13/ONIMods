#!/bin/bash
# Mod Maintenance Cron Script (unified sweep for all mods)
export PATH="$HOME/.local/bin:$HOME/.npm-global/bin:/usr/local/bin:$PATH"

REPO_ROOT="D:/Dev/Projects/GameModding"
LOG_DIR="$REPO_ROOT/maintenance-logs"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/maintenance-$TIMESTAMP.log"
mkdir -p "$LOG_DIR"

# Log rotation - delete logs older than 7 days
find "$LOG_DIR" -name "maintenance-*.log" -mtime +7 -delete

# Pre-flight checks
for cmd in node claude python3; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "[$TIMESTAMP] FATAL: $cmd not found in PATH" | tee -a "$LOG_FILE"
    source "$(dirname "$0")/alert-failure.sh" "Pre-flight failed: $cmd not found"
    exit 1
  fi
done

cd "$REPO_ROOT"

echo "[$TIMESTAMP] Starting unified mod maintenance sweep..." | tee "$LOG_FILE"
echo "claude: $(which claude)" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

claude -p "/mod-maintenance" \
  --dangerously-skip-permissions \
  --max-turns 50 \
  2>&1 | tee -a "$LOG_FILE"
SWEEP_EXIT=${PIPESTATUS[0]}

echo "" | tee -a "$LOG_FILE"
echo "Sweep exit code: $SWEEP_EXIT" | tee -a "$LOG_FILE"

if [ $SWEEP_EXIT -ne 0 ]; then
  echo "[$TIMESTAMP] Sweep FAILED with exit code $SWEEP_EXIT" | tee -a "$LOG_FILE"
  source "$(dirname "$0")/alert-failure.sh" "Sweep failed (exit $SWEEP_EXIT). Check log: $LOG_FILE"
fi

# Post-sweep validation: deterministic issue tracking
echo "" | tee -a "$LOG_FILE"
echo "--- Post-sweep validation ---" | tee -a "$LOG_FILE"
python3 "$REPO_ROOT/.claude/scripts/post-sweep-validate.py" \
  --report "$REPO_ROOT/maintenance-report.txt" \
  --state "$REPO_ROOT/maintenance-state.json" \
  2>&1 | tee -a "$LOG_FILE"
VALIDATE_EXIT=${PIPESTATUS[0]}

if [ $VALIDATE_EXIT -ne 0 ]; then
  echo "[$TIMESTAMP] Validation FAILED with exit code $VALIDATE_EXIT" | tee -a "$LOG_FILE"
  source "$(dirname "$0")/alert-failure.sh" "Post-sweep validation failed (exit $VALIDATE_EXIT)"
fi

# Only commit if content actually changed (not just timestamps)
cd "$REPO_ROOT"
if ! git diff --quiet maintenance-report.txt maintenance-state.json 2>/dev/null; then
  git add maintenance-report.txt maintenance-state.json maintenance-report.json
  git diff --cached --quiet || \
    git commit -m "chore: post-sweep validation - issue tracking" 2>&1 | tee -a "$LOG_FILE"
else
  echo "No content changes detected - skipping commit" | tee -a "$LOG_FILE"
fi

echo "" | tee -a "$LOG_FILE"
echo "Done. Window closing in 30s..."
sleep 30
