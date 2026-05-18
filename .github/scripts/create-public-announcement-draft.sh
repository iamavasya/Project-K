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

curl --fail --silent --show-error \
  --request POST \
  --header "X-ProjectK-Service-Token: ${PROJECTK_SERVICE_TOKEN}" \
  --header "Content-Type: application/json" \
  --data "$(jq -n \
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
    }')" \
  "${PROJECTK_API_BASE_URL%/}/api/admin/public-announcements"
