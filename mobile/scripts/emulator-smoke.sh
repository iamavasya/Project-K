#!/usr/bin/env bash
# Smoke run of the mobile shell on an Android emulator (used by .github/workflows/mobile.yml).
# Installs the bundled debug APK, screenshots the main screens in light and dark mode,
# then installs a live-reload build, edits a source file and checks the emulator picks it up.
# Usage: emulator-smoke.sh <bundled.apk> <live-reload.apk> <out dir>
set -uo pipefail

BUNDLED_APK=$1
LIVE_APK=$2
OUT=$3
APP_ID=dev.rostyslavmukha.lileyka
MOBILE_DIR=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$OUT"
REPORT="$OUT/report.md"
: > "$REPORT"
FAILED=0

log() { echo "$*"; echo "$*" >> "$REPORT"; }
pass() { log "- ✅ $*"; }
fail() { log "- ❌ $*"; FAILED=1; }

shot() {
  adb exec-out screencap -p > "$OUT/$1.png"
  echo "  screenshot $1.png"
}

dump_ui() {
  rm -f "$OUT/ui.xml"
  adb shell uiautomator dump /sdcard/ui.xml > /dev/null 2>&1
  adb pull /sdcard/ui.xml "$OUT/ui.xml" > /dev/null 2>&1
}

# Waits until a node whose text or content-desc contains $1 is on screen.
wait_for_text() {
  local text=$1 tries=${2:-30}
  for _ in $(seq "$tries"); do
    dump_ui
    grep -q -- "$text" "$OUT/ui.xml" 2>/dev/null && return 0
    sleep 2
  done
  return 1
}

# Taps the centre of the first node whose text or content-desc equals $1, ignoring case
# (Material buttons render uppercase).
tap_text() {
  dump_ui
  local xy
  xy=$(python3 - "$OUT/ui.xml" "$1" <<'PY'
import re, sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
for node in root.iter('node'):
    wanted = sys.argv[2].casefold()
    if wanted in (node.get('text', '').strip().casefold(), node.get('content-desc', '').strip().casefold()):
        x1, y1, x2, y2 = map(int, re.findall(r'\d+', node.get('bounds')))
        print((x1 + x2) // 2, (y1 + y2) // 2)
        break
PY
)
  [ -n "$xy" ] || return 1
  adb shell input tap $xy
}

launch() {
  adb shell am force-stop "$APP_ID"
  adb shell monkey -p "$APP_ID" -c android.intent.category.LAUNCHER 1 > /dev/null 2>&1
}

log "## Bundled build"
adb install -r "$BUNDLED_APK" > /dev/null && pass "APK installed" || fail "APK install"
adb shell cmd uimode night no
launch
if wait_for_text "Привіт від S1" 60; then pass "App starts and renders Головна"; else fail "Головна did not render"; fi
grep -q 'text="md"' "$OUT/ui.xml" && pass "Ionic mode is md on Android" || fail "Ionic mode md not found"
grep -q 'text="android' "$OUT/ui.xml" && pass "Capacitor platform is android" || fail "Capacitor platform android not found"
shot 01-home-light

if tap_text "Натиснути" && sleep 1 && tap_text "Натиснути" && wait_for_text "Натиснуто: 2" 5; then
  pass "Signals update the screen without zone.js (Натиснуто: 2 · подвоєно: 4)"
else
  fail "Tap counter did not update"
fi
shot 02-home-tapped

if tap_text "Ще" && wait_for_text "Про Лілейку" 10; then pass "Tab Ще opens"; else fail "Tab Ще did not open"; fi
shot 03-more-light

adb shell cmd uimode night yes
launch
if wait_for_text "Привіт від S1" 60; then pass "Dark mode renders"; else fail "Dark mode did not render"; fi
shot 04-home-dark
adb shell cmd uimode night no

log ""
log "## Live reload (server.url = http://10.0.2.2:4200)"
adb install -r "$LIVE_APK" > /dev/null && pass "Live-reload APK installed" || fail "Live-reload APK install"
launch
if wait_for_text "Привіт від S1" 60; then pass "App loads from the dev server via 10.0.2.2 (cleartext allowed)"; else fail "App did not load from the dev server"; fi
sleep 2
dump_ui
grep -q '10.0.2.2:5205' "$OUT/ui.xml" && pass "Dev build points the API at http://10.0.2.2:5205/api" || fail "Dev API URL not shown"
shot 05-live-before

sed -i 's/Привіт від S1/Live reload працює/' "$MOBILE_DIR/src/app/pages/home.ts"
if wait_for_text "Live reload працює" 30; then pass "Editing src/ updates the emulator without reinstalling"; else fail "Edit did not reach the emulator"; fi
shot 06-live-after
git -C "$MOBILE_DIR" checkout -- src/app/pages/home.ts

rm -f "$OUT/ui.xml"
exit $FAILED
