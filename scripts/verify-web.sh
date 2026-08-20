#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/src/CipherNest.Web/CipherNest.Web.csproj"
PORT="${CIPHERNEST_TEST_PORT:-5187}"
TEMP_DATA="$(mktemp -d)"
SERVER_PID=""

cleanup() {
  if [[ -n "$SERVER_PID" ]] && kill -0 "$SERVER_PID" >/dev/null 2>&1; then
    kill "$SERVER_PID" >/dev/null 2>&1 || true
  fi
  rm -rf "$TEMP_DATA"
}
trap cleanup EXIT INT TERM

cd "$ROOT_DIR"
dotnet restore "$PROJECT"
dotnet build "$PROJECT" -c Release --no-restore
dotnet format "$PROJECT" --verify-no-changes --no-restore

CIPHERNEST_DATA_DIR="$TEMP_DATA" dotnet run --project "$PROJECT" -c Release --no-build --no-launch-profile -- --CipherNest:Port="$PORT" >"$TEMP_DATA/server.log" 2>&1 &
SERVER_PID=$!

for _ in {1..80}; do
  if curl --fail --silent --show-error "http://127.0.0.1:$PORT/healthz" >/dev/null; then
    echo "CipherNest Web loopback health probe passed."
    exit 0
  fi
  if ! kill -0 "$SERVER_PID" >/dev/null 2>&1; then
    cat "$TEMP_DATA/server.log" >&2
    wait "$SERVER_PID"
    exit $?
  fi
  sleep 0.25
done

cat "$TEMP_DATA/server.log" >&2
echo "CipherNest Web did not become healthy on loopback in time." >&2
exit 1
