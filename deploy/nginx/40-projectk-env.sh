#!/usr/bin/env sh
set -eu

: "${PROJECTK_API_URL:=http://localhost:5205/api}"

envsubst '${PROJECTK_API_URL}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
