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

dotnet workload install maui-ios maui-maccatalyst --skip-manifest-update

dotnet build "$PROJECT" -c Release -p:CipherNestTargetFrameworks=net10.0-ios -f net10.0-ios -r iossimulator-arm64
dotnet build "$PROJECT" -c Release -p:CipherNestTargetFrameworks=net10.0-maccatalyst -f net10.0-maccatalyst -r maccatalyst-arm64
