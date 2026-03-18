#!/bin/bash
# alert-failure.sh - Send failure alert to Discord #moderator-only channel
# Usage: source alert-failure.sh "Error message here"
# Or: source alert-failure.sh "Error message" "optional log file path"

ALERT_MSG="${1:-Mod maintenance failure (no details)}"
ALERT_LOG="${2:-}"
ALERT_CHANNEL="1351014671267659840"  # #moderator-only

# Build message
DISCORD_MSG="**Mod Maintenance Alert**\n"
DISCORD_MSG+="Status: FAILED\n"
DISCORD_MSG+="Time: $(date '+%Y-%m-%d %H:%M:%S')\n"
DISCORD_MSG+="Error: ${ALERT_MSG}\n"
if [ -n "$ALERT_LOG" ]; then
  DISCORD_MSG+="Log: \`${ALERT_LOG}\`"
fi

# Try Discord webhook first (most reliable from cron)
WEBHOOK_URL_FILE="$(dirname "$0")/.discord-webhook-url"
if [ -f "$WEBHOOK_URL_FILE" ]; then
  WEBHOOK_URL=$(cat "$WEBHOOK_URL_FILE")
  curl -s --max-time 10 -H "Content-Type: application/json" \
    -d "{\"content\": \"${DISCORD_MSG}\"}" \
    "$WEBHOOK_URL" || echo "WARNING: Discord webhook failed"
else
  echo "WARNING: No Discord webhook configured at $WEBHOOK_URL_FILE"
  echo "Alert message: $ALERT_MSG"
fi
