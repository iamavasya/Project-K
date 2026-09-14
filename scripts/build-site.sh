#!/usr/bin/env bash
# Builds the public site: the Astro pages plus the static demo of the app under /demo/.
# No API, no database: the demo answers from fixtures recorded with
# Frontend/projectk-frontend/scripts/record-demo-fixtures.mjs and committed under
# Frontend/projectk-frontend/public/assets/demo/.
#
# Cloudflare Pages: build command `bash scripts/build-site.sh`, output directory `site/dist`,
# NODE_VERSION=22. Locally: the same command from the repository root; result in site/dist.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
frontend="$root/Frontend/projectk-frontend"
site="$root/site"

echo "== badge pictures for the demo"
# The pictures are heavy (46 MB of Illustrator SVG) and public, so they are not in git: they come
# from the same PlastBadgesParser release the API extracts, once per checkout.
badges="$frontend/public/assets/demo/badges_images"
if [ ! -d "$badges" ] || [ -z "$(ls -A "$badges" 2>/dev/null)" ]; then
  release_url="$(curl -fsSL https://api.github.com/repos/iamavasya/PlastBadgesParser/releases/latest     | grep -o '"browser_download_url": *"[^"]*\.zip"' | head -1 | sed 's/.*"\(https[^"]*\)"//')"
  echo "   fetching $release_url"
  tmp="$(mktemp -d)"
  curl -fsSL -o "$tmp/badges.zip" "$release_url"
  mkdir -p "$frontend/public/assets/demo"
  unzip -q -o "$tmp/badges.zip" 'badges_images/*' -d "$frontend/public/assets/demo"
  rm -rf "$tmp"
fi
echo "   $(ls "$badges" | wc -l | tr -d ' ') pictures"

echo "== demo app (Angular, configuration demo)"
cd "$frontend"
if [ ! -d node_modules ]; then npm ci; fi
npx ng build --configuration demo

echo "== placing the demo under site/public/demo"
rm -rf "$site/public/demo"
mkdir -p "$site/public/demo"
cp -R "$frontend/dist/projectk-frontend/browser/." "$site/public/demo/"
# index.html asks for env.js; the demo has no runtime config, so an empty file keeps the console clean.
: > "$site/public/demo/env.js"
# The app's theme and pages reference /assets/... by absolute path (fonts, the hero photo), which
# on the site host is the root, not /demo/. A copy of the same folder at the root keeps them found.
rm -rf "$site/public/assets"
cp -R "$site/public/demo/assets" "$site/public/assets"

echo "== site (Astro)"
cd "$site"
if [ ! -d node_modules ]; then npm ci; fi
npm run build

echo "== done: $site/dist"
