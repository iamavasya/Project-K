#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${TELEGRAM_BOT_TOKEN:-}" || -z "${TELEGRAM_CHAT_ID:-}" || -z "${TELEGRAM_TEXT:-}" ]]; then
  echo "Telegram message skipped: TELEGRAM_BOT_TOKEN, TELEGRAM_CHAT_ID, or TELEGRAM_TEXT is empty."
  exit 0
fi

BASE_URL="${TELEGRAM_BASE_URL:-https://api.telegram.org}"

curl --fail --silent --show-error \
  --request POST \
  --header "Content-Type: application/json" \
  --data "$(jq -n \
    --arg chat_id "$TELEGRAM_CHAT_ID" \
    --arg text "$TELEGRAM_TEXT" \
    '{chat_id: $chat_id, text: $text, disable_web_page_preview: true}')" \
  "${BASE_URL%/}/bot${TELEGRAM_BOT_TOKEN}/sendMessage"
