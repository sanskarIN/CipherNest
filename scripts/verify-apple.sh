#!/usr/bin/env sh
set -eu

if [ "$(uname -s)" != "Darwin" ]; then
  echo "scripts/verify-apple.sh must run on macOS." >&2
  exit 1
fi

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
cd "$REPO_ROOT"

PROJECT="src/CipherNest.App/CipherNest.App.csproj"

dotnet workload restore "$PROJECT"

for framework in net10.0-ios net10.0-maccatalyst; do
  dotnet restore "$PROJECT" -p:TargetFramework="$framework"
  dotnet build "$PROJECT" -c Release -f "$framework" --no-restore
done
