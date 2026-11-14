#!/usr/bin/env bash
# Screenplay frozen candidate train-8: origin/main (train 7, v5.3.1) + #1104 propose-source attachment reuse.
# Mirrors the PR jobs of dotnet-build, javascript-build, native-editor-repairs (osx-arm64 leg),
# mcp-desktop (osx-arm64 leg, minus mcpb packaging), verify-ai-corpus-impact (tests) and docker-build.
# FROM=<step> resumes at that step (earlier steps are skipped; install is never repeated on resume).
set -u
cd /Volumes/sourcecode/repos/cratis/.worktrees/screenplay-train-11 || exit 2
[ "$(git rev-parse HEAD)" = "$EXPECT" ] || { echo "HEAD moved"; exit 2; }
[ -z "$(git status --porcelain)" ] || { echo "dirty"; exit 2; }
L=.ai-work/train/t11; P=~/.pi/agent/bin/pi-phase; t=Source/DotNET
FROM=${FROM:-}; started=1; [ -z "$FROM" ] || started=0
h() { local n=$1 k=$2 to=$3; shift 3
  if [ "$started" != 1 ]; then
    if [ "$n" = "$FROM" ]; then started=1; else echo "STEP $n skipped (resume from $FROM)"; return 0; fi
  fi
  local local_time; local_time=$(date +%H%M)
  if [[ "$local_time" > "1129" && "$local_time" < "1230" ]]; then echo "QUIET SLOT: no new phase before $n"; exit 75; fi
  local free_kb wait_start; wait_start=$SECONDS
  while :; do
    free_kb=$(df -k . | awk 'NR==2 {print $4}')
    [ "$free_kb" -ge 27262976 ] && break
    [ "$((SECONDS-wait_start))" -lt 3600 ] || { echo "DISK HOLD: below 26 GiB for 60 minutes before $n"; exit 75; }
    echo "DISK WAIT: below 26 GiB before $n; elapsed=$((SECONDS-wait_start))s"
    sleep 60
  done
  echo "STEP $n start $(date -u +%FT%TZ) HEAD=$(git rev-parse HEAD)"
  if [ "$to" -gt 600 ]; then
    "$P" run --kind "$k" --label "sp-train11 $n" --timeout "$to" --approved-long-timeout --priority low --queue-timeout 2400 --log "$L/$n.log" --tail 12 -- "$@"
  else
    "$P" run --kind "$k" --label "sp-train11 $n" --timeout "$to" --priority low --queue-timeout 2400 --log "$L/$n.log" --tail 12 -- "$@"
  fi
  local rc=$?; echo "STEP $n exit=$rc $(date -u +%FT%TZ)"; [ "$rc" -eq 0 ] || exit "$rc"
  [ "$(git rev-parse HEAD)" = "$EXPECT" ] && [ -z "$(git status --porcelain)" ] || { echo "candidate changed"; exit 2; }
  if [ "${THROUGH:-}" = "$n" ]; then echo "BOUNDED STOP after $n"; exit 0; fi
}
# CI runs plain yarn without a committed lockfile. Preserve the unique ignored source bytes first.
if [ -z "$FROM" ]; then
  if [ -f yarn.lock ]; then
    [ ! -e "$L/before-yarn.lock" ] || { echo "lock archive already exists; refusing overwrite"; exit 2; }
    mv yarn.lock "$L/before-yarn.lock" || exit 2
  fi
  h yarn-install build 600 yarn
fi
# --- dotnet-build.yml
h board-build build 600 yarn workspaces foreach -Rt --from @cratis/screenplay-mcp-app run build
h launcher-test test 300 node --test .github/scripts/run-specifications.test.mjs
h shard-coverage test 300 node "$L/shard-coverage.mjs"
h debug-build build 900 dotnet build --configuration Debug
for p in Screenplay.Specs/Screenplay.Specs Screenplay.Mcp/Screenplay.Mcp Screenplay.Matching/Screenplay.Matching Screenplay.Matching.Specs/Screenplay.Matching.Specs Screenplay.Contexts/Screenplay.Contexts Screenplay.Conformance/Screenplay.Conformance Screenplay.ContractCatalog.Specs/Screenplay.ContractCatalog.Specs Screenplay.Contracts/Screenplay.Contracts Tool/Tool Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs; do
  # MCP: train3 measured 1010s; compiler specs retain the former inline-spec load.
  to=600
  [ "$p" != Screenplay.Mcp/Screenplay.Mcp ] || to=1800
  [ "$p" != Screenplay.Specs/Screenplay.Specs ] || to=900
  h "spec-$(basename "$p")" test "$to" bash .github/scripts/run-specifications.sh "$t/$p.csproj" Debug
done
h release-build build 600 dotnet build --configuration Release -p:Version=9999.0.0
h vectors-release test 600 bash .github/scripts/run-specifications.sh "$t/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj" Release
h pack build 600 dotnet pack --no-build --configuration Release -o ./Artifacts/NuGet -p:Version=9999.0.0
h verify-package build 120 python3 "$t/Screenplay.Contexts/verify-package.py" ./Artifacts/NuGet 9999.0.0
# --- javascript-build.yml
h js-build build 600 yarn build
h js-compile build 600 yarn compile
h js-lint build 600 yarn lint:ci
h js-test test 600 yarn test
# --- verify-ai-corpus-impact.yml (the verify step itself needs a PR number + token)
h corpus-tests test 300 bash -c 'node --test .github/scripts/tests/*.test.mjs'
# --- native-editor-repairs.yml, osx-arm64 leg
export SCREENPLAY_REPAIR_SERVER="$PWD/$L/native-server/Cratis.Screenplay.Tool"
export SCREENPLAY_REPAIR_VSIX="$PWD/$L/native-editor.vsix"
h native-board build 600 yarn workspaces foreach -Rt --from screenplay --from @cratis/screenplay-mcp-app run build
h native-publish build 600 dotnet publish "$t/Tool/Tool.csproj" -c Release -r osx-arm64 --self-contained true -p:PackAsTool=false -o "$L/native-server"
h native-repair-process test 600 yarn workspace screenplay test:repair-process
h native-package build 300 yarn workspace screenplay package --out "$SCREENPLAY_REPAIR_VSIX"
h native-inspect-vsix test 120 python3 Source/Screenplay/VSCodeExtension/tests/inspectVsix.py "$SCREENPLAY_REPAIR_VSIX"
h native-build-test-host build 300 yarn workspace screenplay build:test-host
# CI provisions pinned VS Code 1.105.1 (prepareCiHost.mjs is CI-only); locally the installed VS Code is used.
export VSCODE_EXECUTABLE_PATH="/Applications/Visual Studio Code.app/Contents/MacOS/Code"
echo "STEP native-test-host SKIPPED: CI-only (secret-safety)"
# --- mcp-desktop.yml, osx-arm64 leg (build.py/smoke.py need mcpb, which is not installed locally)
h desktop-packaging-tests test 300 python3 -m unittest discover -s "$t/Screenplay.Mcp/Packaging" -p 'test_*.py'
h desktop-board-present test 60 test -s Source/Screenplay/McpApp/dist/event-model-board.html
h desktop-publish build 600 dotnet publish "$t/Tool/Tool.csproj" -c Release -r osx-arm64 --self-contained true -p:PackAsTool=false -p:Version=0.0.0-desktop-ci -o Artifacts/Desktop/publish
# --- docker-build.yml (Tier 2)
h docker-build build 1800 docker buildx build --platform linux/amd64,linux/arm64 .
echo "SCREENPLAY CANDIDATE TIER1+2 PASSED HEAD=$EXPECT"
