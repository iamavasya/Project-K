#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${PROJECTK_API_BASE_URL:-}" || -z "${PROJECTK_SERVICE_TOKEN:-}" ]]; then
  echo "Public announcement draft skipped: PROJECTK_API_BASE_URL or PROJECTK_SERVICE_TOKEN is empty."
  exit 0
fi

TITLE="${ANNOUNCEMENT_TITLE:-ProjectK release}"
BODY="${ANNOUNCEMENT_BODY:-A new ProjectK version is available.}"
SOURCE_ID="${ANNOUNCEMENT_SOURCE_ID:-${GITHUB_REF_NAME:-}}"
SOURCE_URL="${ANNOUNCEMENT_SOURCE_URL:-${GITHUB_SERVER_URL:-https://github.com}/${GITHUB_REPOSITORY:-}/releases/tag/${GITHUB_REF_NAME:-}}"
ENVIRONMENT="${ANNOUNCEMENT_ENVIRONMENT:-production}"
VERSION="${ANNOUNCEMENT_VERSION:-${GITHUB_REF_NAME:-}}"
CODENAME="${ANNOUNCEMENT_CODENAME:-}"

# Safely construct the JSON payload using jq and save it to a temporary file
PAYLOAD_FILE=$(mktemp)
jq -n \
  --arg title "$TITLE" \
  --arg body "$BODY" \
  --arg sourceId "$SOURCE_ID" \
  --arg sourceUrl "$SOURCE_URL" \
  --arg environment "$ENVIRONMENT" \
  --arg version "$VERSION" \
  --arg codename "$CODENAME" \
  '{
    title: $title,
    body: $body,
    sourceType: "GitHubRelease",
    sourceId: $sourceId,
    sourceUrl: $sourceUrl,
    environment: $environment,
    version: $version,
    codename: $codename,
    parseMode: "Html",
    imagePlacement: "ImageFirst"
  }' > "$PAYLOAD_FILE"

curl --fail --silent --show-error \
  --request POST \
  --header "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 GitHubActions/1.0" \
  --header "X-ProjectK-Service-Token: ${PROJECTK_SERVICE_TOKEN}" \
  --header "Content-Type: application/json" \
  --data "@$PAYLOAD_FILE" \
  "${PROJECTK_API_BASE_URL%/}/api/admin/public-announcements"

# Clean up
rm "$PAYLOAD_FILE"