#!/usr/bin/env sh
set -eu

# Export (not just assign) so the defaults reach envsubst, which only substitutes
# variables present in the environment. Compose-provided values are already exported
# and win; otherwise these defaults apply. This is the self-host release image, so the
# environment badge defaults to Self-Host (dev-container composes always override it).
export PROJECTK_API_URL="${PROJECTK_API_URL:-http://localhost:5205/api}"
export PROJECTK_ENVIRONMENT_NAME="${PROJECTK_ENVIRONMENT_NAME:-Self-Host}"
# Product name shown in the browser tab. Override once the system leaves beta
# under its release name; no frontend rebuild required.
export PROJECTK_APP_NAME="${PROJECTK_APP_NAME:-Лілейка}"

envsubst '${PROJECTK_API_URL} ${PROJECTK_ENVIRONMENT_NAME} ${PROJECTK_APP_NAME}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
