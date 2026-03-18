#!/bin/bash
# Mod Maintenance Cron Script (unified sweep for all mods)
export PATH="$HOME/.local/bin:$HOME/.npm-global/bin:/usr/local/bin:$PATH"

# Resolve script directory reliably (works under Task Scheduler)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REPO_ROOT="D:/Dev/Projects/GameModding"
LOG_DIR="$REPO_ROOT/maintenance-logs"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/maintenance-$TIMESTAMP.log"
mkdir -p "$LOG_DIR"

# Log rotation - delete logs older than 7 days
find "$LOG_DIR" -name "maintenance-*.log" -mtime +7 -delete 2>/dev/null

# Pre-flight checks (python3 or python, whichever exists)
PYTHON_CMD=""
for py in python3 python py; do
  if command -v "$py" &>/dev/null; then
    PYTHON_CMD="$py"
    break
  fi
done
if [ -z "$PYTHON_CMD" ]; then
  echo "[$TIMESTAMP] FATAL: No python found in PATH" | tee -a "$LOG_FILE"
  bash "$SCRIPT_DIR/alert-failure.sh" "Pre-flight failed: no python found"
  exit 1
fi

for cmd in node claude; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "[$TIMESTAMP] FATAL: $cmd not found in PATH" | tee -a "$LOG_FILE"
    bash "$SCRIPT_DIR/alert-failure.sh" "Pre-flight failed: $cmd not found"
    exit 1
  fi
done

cd "$REPO_ROOT"

echo "[$TIMESTAMP] Starting unified mod maintenance sweep..." | tee "$LOG_FILE"
echo "claude: $(which claude)" | tee -a "$LOG_FILE"
echo "python: $PYTHON_CMD ($(which $PYTHON_CMD))" | tee -a "$LOG_FILE"
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
  bash "$SCRIPT_DIR/alert-failure.sh" "Sweep failed (exit $SWEEP_EXIT). Check log: $LOG_FILE"
  # Skip validation - report may be corrupt or missing
  echo "Skipping validation due to sweep failure." | tee -a "$LOG_FILE"
  echo "" | tee -a "$LOG_FILE"
  echo "Done. Window closing in 30s..."
  sleep 30
  exit $SWEEP_EXIT
fi

# Verify JSON report was generated (critical v1.1 requirement)
echo "" | tee -a "$LOG_FILE"
if [ ! -f "$REPO_ROOT/maintenance-report.json" ]; then
  echo "[$TIMESTAMP] WARNING: maintenance-report.json not generated - attempting recovery" | tee -a "$LOG_FILE"
  # Recovery: focused single-step invocation to generate JSON from txt + state
  claude -p "Read D:/Dev/Projects/GameModding/maintenance-report.txt and D:/Dev/Projects/GameModding/maintenance-state.json. Write D:/Dev/Projects/GameModding/maintenance-report.json following the JSON schema documented in D:/Dev/Projects/GameModding/.claude/commands/mod-maintenance.md Step 3. Only do this one thing." \
    --dangerously-skip-permissions \
    --max-turns 10 \
    2>&1 | tee -a "$LOG_FILE"
  if [ ! -f "$REPO_ROOT/maintenance-report.json" ]; then
    echo "[$TIMESTAMP] ERROR: JSON recovery also failed" | tee -a "$LOG_FILE"
    bash "$SCRIPT_DIR/alert-failure.sh" "JSON report missing after sweep AND recovery. Manual intervention needed."
  else
    echo "[$TIMESTAMP] JSON recovery succeeded" | tee -a "$LOG_FILE"
  fi
fi

# Post-sweep validation: deterministic issue tracking
echo "--- Post-sweep validation ---" | tee -a "$LOG_FILE"
$PYTHON_CMD "$REPO_ROOT/.claude/scripts/post-sweep-validate.py" \
  --report "$REPO_ROOT/maintenance-report.txt" \
  --state "$REPO_ROOT/maintenance-state.json" \
  2>&1 | tee -a "$LOG_FILE"
VALIDATE_EXIT=${PIPESTATUS[0]}

if [ $VALIDATE_EXIT -ne 0 ]; then
  echo "[$TIMESTAMP] Validation FAILED with exit code $VALIDATE_EXIT" | tee -a "$LOG_FILE"
  bash "$SCRIPT_DIR/alert-failure.sh" "Post-sweep validation failed (exit $VALIDATE_EXIT)"
fi

# Append stats snapshot to history (time-series for trend analysis)
SWEEP_END=$(date +%s)
if [ -f "$REPO_ROOT/maintenance-state.json" ]; then
  $PYTHON_CMD -c "
import json, sys
state = json.load(open('$REPO_ROOT/maintenance-state.json'))
entry = {
    'timestamp': state.get('lastSweep', ''),
    'sweepDurationSeconds': $SWEEP_END - ${TIMESTAMP:0:8}00 if '$TIMESTAMP' else 0,
    'sweepExitCode': $SWEEP_EXIT,
    'validateExitCode': $VALIDATE_EXIT,
    'stats': state.get('previousStats', {}),
    'openItemCount': len(state.get('openItems', []))
}
with open('$REPO_ROOT/stats-history.jsonl', 'a') as f:
    f.write(json.dumps(entry) + '\n')
print('[stats] Appended to stats-history.jsonl')
" 2>&1 | tee -a "$LOG_FILE"
fi

# Only commit if content actually changed (not just timestamps)
cd "$REPO_ROOT"
if ! git diff --quiet maintenance-report.txt maintenance-state.json 2>/dev/null; then
  git add maintenance-report.txt maintenance-state.json maintenance-report.json stats-history.jsonl
  git diff --cached --quiet || \
    git commit -m "chore: post-sweep validation - issue tracking" 2>&1 | tee -a "$LOG_FILE"
else
  echo "No content changes detected - skipping commit" | tee -a "$LOG_FILE"
fi

echo "" | tee -a "$LOG_FILE"
echo "Done. Window closing in 30s..."
sleep 30
exit $((SWEEP_EXIT | VALIDATE_EXIT))
