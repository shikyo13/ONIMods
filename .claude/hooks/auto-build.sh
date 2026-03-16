#!/bin/bash
# PostToolUse hook: auto-build the affected mod after .cs file edits
INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null)

if [[ -z "$FILE_PATH" ]] || [[ "$FILE_PATH" != *.cs ]]; then
  exit 0
fi

REPO_ROOT="D:/Dev/Projects/GameModding/ONIMods"

for mod in ReplaceStuff BuildThrough DuplicantStatusBar OniProfiler GCBudget; do
  if [[ "$FILE_PATH" == *"$mod"* ]]; then
    BUILD_OUTPUT=$(cd "$REPO_ROOT" && dotnet build "$mod/$mod.csproj" 2>&1 | tail -5)
    if echo "$BUILD_OUTPUT" | grep -q "Build succeeded"; then
      echo "$BUILD_OUTPUT"
    else
      echo "$BUILD_OUTPUT" >&2
      exit 2
    fi
    exit 0
  fi
done
