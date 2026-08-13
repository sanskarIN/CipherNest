#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
cd "$REPO_ROOT"

PROJECT="src/CipherNest.App/CipherNest.App.csproj"
FRAMEWORK="net10.0-android"
RID="android-arm64"

dotnet workload install maui-android --skip-manifest-update
dotnet build "$PROJECT" -c Release -p:CipherNestTargetFrameworks="$FRAMEWORK" -f "$FRAMEWORK" -r "$RID"
