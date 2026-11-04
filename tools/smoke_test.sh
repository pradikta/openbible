#!/usr/bin/env sh
# Smoke test for a running OpenBible container.
# Usage: tools/smoke_test.sh <base-url>
set -eu

BASE_URL="${1:?usage: smoke_test.sh <base-url>}"
BASE_URL="${BASE_URL%/}"

# The ARM64 image runs under QEMU in CI, so give it time to start.
echo "waiting for ${BASE_URL}"
ready=0
code=000
for attempt in $(seq 1 60); do
  code=$(curl --silent --output /dev/null --max-time 10 --write-out '%{http_code}' "${BASE_URL}/" || echo 000)
  if [ "${code}" = "200" ]; then
    ready=1
    break
  fi
  sleep 2
done
if [ "${ready}" != "1" ]; then
  echo "server did not become ready (last HTTP ${code})" >&2
  exit 1
fi

failed=0
check() {
  path="$1"
  expected_type="$2"
  headers=$(curl --silent --output /dev/null --max-time 30 --dump-header - --write-out 'status:%{http_code}\n' "${BASE_URL}${path}" || true)
  status=$(printf '%s\n' "${headers}" | sed -n 's/^status://p')
  type=$(printf '%s\n' "${headers}" | tr -d '\r' | sed -n 's/^[Cc]ontent-[Tt]ype: *//p' | head -n 1)
  if [ "${status}" = "200" ] && printf '%s' "${type}" | grep -qi "${expected_type}"; then
    echo "ok   ${path} (${type})"
  else
    echo "FAIL ${path}: HTTP ${status}, content-type '${type}', expected '${expected_type}'" >&2
    failed=1
  fi
}

check "/" "text/html"
check "/read/kjv/0/1" "text/html"
check "/_framework/blazor.webassembly.js" "javascript"
check "/css/tailwind.css" "text/css"
check "/manifest.webmanifest" "json"
check "/version.json" "json"
# The bundled KJV is what makes the reader work offline on first run.
check "/data/en-kjv.yes.gz" "."

# Only catalog presets are proxied; anything else must be rejected, not forwarded.
code=$(curl --silent --output /dev/null --max-time 30 --write-out '%{http_code}' "${BASE_URL}/api/catalog/download/not-a-preset" || echo 000)
if [ "${code}" = "404" ]; then
  echo "ok   unknown preset rejected"
else
  echo "FAIL unknown preset returned HTTP ${code}, expected 404" >&2
  failed=1
fi

exit "${failed}"
