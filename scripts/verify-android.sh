#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
cd "$REPO_ROOT"

PROJECT="src/CipherNest.App/CipherNest.App.csproj"
FRAMEWORK="net10.0-android"

dotnet workload restore "$PROJECT"
dotnet restore "$PROJECT" -p:TargetFramework="$FRAMEWORK"
dotnet build "$PROJECT" -c Release -f "$FRAMEWORK" --no-restore
