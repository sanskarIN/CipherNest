#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/src/CipherNest.Web/CipherNest.Web.csproj"
PORT="${CIPHERNEST_PORT:-5187}"
DATA_HOME="${XDG_DATA_HOME:-${HOME:-$ROOT_DIR}/.local/share}"
export CIPHERNEST_DATA_DIR="${CIPHERNEST_DATA_DIR:-$DATA_HOME/CipherNest}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "CipherNest Linux requires the .NET 10 SDK/runtime. 'dotnet' was not found." >&2
  exit 1
fi

if [[ ! "$PORT" =~ ^[0-9]+$ ]] || (( PORT < 1024 || PORT > 65535 )); then
  echo "CIPHERNEST_PORT must be an integer between 1024 and 65535." >&2
  exit 1
fi

mkdir -p "$CIPHERNEST_DATA_DIR"
URL="http://127.0.0.1:$PORT"

echo "Starting CipherNest local web UI on $URL"
echo "Vault data directory: $CIPHERNEST_DATA_DIR"

dotnet run --project "$PROJECT" -c Release --no-launch-profile -- --CipherNest:Port="$PORT" &
SERVER_PID=$!

cleanup() {
  if kill -0 "$SERVER_PID" >/dev/null 2>&1; then
    kill "$SERVER_PID" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT INT TERM

for _ in {1..60}; do
  if command -v curl >/dev/null 2>&1 && curl --fail --silent --show-error "$URL/healthz" >/dev/null 2>&1; then
    break
  fi
  if ! kill -0 "$SERVER_PID" >/dev/null 2>&1; then
    wait "$SERVER_PID"
    exit $?
  fi
  sleep 0.25
done

if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$URL" >/dev/null 2>&1 || true
else
  echo "Open $URL in a browser on this Linux machine."
fi

wait "$SERVER_PID"
