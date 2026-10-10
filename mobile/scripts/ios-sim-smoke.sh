#!/usr/bin/env bash
# Smoke run of the mobile shell on an iOS Simulator (used by .github/workflows/mobile.yml).
# Installs the simulator build, screenshots it in light and dark mode, then opens the dev
# server in the simulator's Safari (spike S2: same WebKit as an iPhone, no native build).
# Usage: ios-sim-smoke.sh <App.app> <dev server url> <out dir>
set -uo pipefail

APP_PATH=$1
DEV_URL=$2
OUT=$3
APP_ID=dev.rostyslavmukha.lileyka
mkdir -p "$OUT"
REPORT="$OUT/report.md"
: > "$REPORT"
FAILED=0

log() { echo "$*"; echo "$*" >> "$REPORT"; return 0; }
pass() { log "- ✅ $*"; return 0; }
fail() { log "- ❌ $*"; FAILED=1; return 0; }

shot() {
  local name=$1
  xcrun simctl io booted screenshot "$OUT/$name.png" > /dev/null 2>&1
  echo "  screenshot $name.png"
  return 0
}

launch() {
  xcrun simctl terminate booted "$APP_ID" > /dev/null 2>&1
  xcrun simctl launch booted "$APP_ID" > /dev/null
  local status=$?
  sleep 8
  return $status
}

# Safari's first cold start can outlast openurl's own timeout on a busy runner; try a few times.
open_url() {
  local url="$1"
  local attempt
  for attempt in 1 2 3; do
    xcrun simctl openurl booted "$url" && return 0
    echo "  openurl attempt $attempt timed out, retrying"
    sleep 10
  done
  return 1
}

DEVICE=$(xcrun simctl list devices available -j | python3 -c '
import json, sys
devices = json.load(sys.stdin)["devices"]
for runtime in sorted(devices, reverse=True):
    if "iOS" not in runtime:
        continue
    for device in devices[runtime]:
        if device["name"].startswith("iPhone") and "Pro" not in device["name"]:
            print(device["udid"], device["name"], runtime.rsplit(".", 1)[-1], sep="|")
            sys.exit(0)
')
if [[ -z "$DEVICE" ]]; then
  fail "No iPhone simulator available"
  exit 1
fi
IFS='|' read -r UDID NAME RUNTIME <<< "$DEVICE"
log "## $NAME ($RUNTIME)"

xcrun simctl boot "$UDID" && xcrun simctl bootstatus "$UDID" -b > /dev/null && pass "Simulator booted" || fail "Simulator boot"
xcrun simctl ui booted appearance light

log ""
log "## Native app (Capacitor, bundled)"
xcrun simctl install booted "$APP_PATH" && pass "App installed" || fail "App install"
launch && pass "App launched" || fail "App launch"
# The first cold start can screenshot a blank WebView; relaunch once warm.
launch || fail "App relaunch in light mode"
shot 01-ios-home-light
xcrun simctl ui booted appearance dark
launch || fail "App relaunch in dark mode"
shot 02-ios-home-dark
xcrun simctl ui booted appearance light

log ""
log "## Safari on the simulator (S2: $DEV_URL)"
open_url "$DEV_URL/tabs/home" && pass "Safari opened the dev server" || fail "Safari openurl"
sleep 10
shot 03-safari-home
open_url "$DEV_URL/tabs/more" || fail "Safari openurl tabs/more"
sleep 6
shot 04-safari-more

log ""
log "Screenshots are checked by eye: the native app should show Режим Ionic = ios, Платформа Capacitor = ios."
exit $FAILED
