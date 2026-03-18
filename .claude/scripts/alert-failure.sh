#!/bin/bash
# alert-failure.sh - Send failure alert to Discord #moderator-only channel
# Usage: bash alert-failure.sh "Error message here"
# Or: bash alert-failure.sh "Error message" "optional log file path"

ALERT_MSG="${1:-Mod maintenance failure (no details)}"
ALERT_LOG="${2:-}"

# Build message
DISCORD_MSG="**Mod Maintenance Alert**\nStatus: FAILED\nTime: $(date '+%Y-%m-%d %H:%M:%S')\nError: ${ALERT_MSG}"
if [ -n "$ALERT_LOG" ]; then
  DISCORD_MSG+="\nLog: \`${ALERT_LOG}\`"
fi

# Try Discord webhook (most reliable from cron)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEBHOOK_URL_FILE="$SCRIPT_DIR/.discord-webhook-url"
if [ -f "$WEBHOOK_URL_FILE" ]; then
  WEBHOOK_URL=$(cat "$WEBHOOK_URL_FILE")
  # Use jq for safe JSON escaping if available, otherwise basic escaping
  if command -v jq &>/dev/null; then
    PAYLOAD=$(jq -n --arg msg "$DISCORD_MSG" '{"content": $msg}')
  else
    # Basic escaping: replace backslashes, double quotes, newlines
    ESCAPED_MSG=$(printf '%s' "$DISCORD_MSG" | sed 's/\\/\\\\/g; s/"/\\"/g')
    PAYLOAD="{\"content\": \"${ESCAPED_MSG}\"}"
  fi
  curl -s --max-time 10 -H "Content-Type: application/json" \
    -d "$PAYLOAD" \
    "$WEBHOOK_URL" || echo "WARNING: Discord webhook failed"
else
  echo "WARNING: No Discord webhook configured at $WEBHOOK_URL_FILE"
  echo "Alert message: $ALERT_MSG"
fi
