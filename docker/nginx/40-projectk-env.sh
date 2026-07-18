#!/usr/bin/env sh
set -eu

: "${PROJECTK_API_URL:=http://localhost:5205/api}"
: "${PROJECTK_ENVIRONMENT_NAME:=Production}"

envsubst '${PROJECTK_API_URL} ${PROJECTK_ENVIRONMENT_NAME}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
