#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the screens-release acceptance harnesses against the installed public CLI and the Stage image that CLI defaults
# to, so the next released vector is rerun with one command:
#
#   Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/run-public-acceptance.sh
#
# Optional environment:
#   SCREENPLAY_EXPECTED_CLI_VERSION  fail the run unless `cratis --version` matches (for example 3.40.5)
#   SCREENPLAY_EXPECTED_STAGE_TAG    fail the run unless the started Stage image has this tag (for example 4.49.9)
#   SCREENPLAY_STAGE_TAG             pin a Stage image instead of the CLI default
#   SCREENPLAY_PLAYWRIGHT_NODE_PATH  NODE_PATH that resolves `playwright` (defaults to NODE_PATH)
#   SCREENPLAY_AI_VERIFICATION       Cratis/AI Source/Verification folder for the MCP stdio transcript
#   SCREENPLAY_STUDIO_URL            deployed Studio URL for the production save/export/import/Play harness
#   SCREENPLAY_ACCEPTANCE_PORT       browser port (default 19180); the workbench uses this plus 16000
#
# Exit codes: 0 every harness passed, 1 a harness ran and found defects or blockers, 2 the run could not start.

set -euo pipefail

root="$(git rev-parse --show-toplevel)"
cd "$root"
harness="Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness"

if ! command -v cratis >/dev/null 2>&1; then
    echo "run-public-acceptance: cratis is not on PATH" >&2
    exit 2
fi

cli_version="$(cratis --version)"
if [[ -n "${SCREENPLAY_EXPECTED_CLI_VERSION:-}" && "$cli_version" != "$SCREENPLAY_EXPECTED_CLI_VERSION" ]]; then
    echo "run-public-acceptance: expected CLI ${SCREENPLAY_EXPECTED_CLI_VERSION}, found ${cli_version}" >&2
    exit 2
fi

node_path="${SCREENPLAY_PLAYWRIGHT_NODE_PATH:-${NODE_PATH:-}}"
if ! NODE_PATH="$node_path" node -e "require('playwright')" >/dev/null 2>&1; then
    echo "run-public-acceptance: playwright is not resolvable; set SCREENPLAY_PLAYWRIGHT_NODE_PATH" >&2
    exit 2
fi

if ! node "${harness}/screen-composition-browser-native-controls.cjs" --self-test; then
    echo "run-public-acceptance: the browser harness self-test failed; its results cannot be trusted" >&2
    exit 2
fi

port="${SCREENPLAY_ACCEPTANCE_PORT:-19180}"
workbench_port=$((port + 16000))
results=".ai-work/acceptance/cli-${cli_version}-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$results"
echo "run-public-acceptance: CLI ${cli_version}; results in ${results}"

failures=0

run_step() {
    local name="$1"
    shift
    local status=0
    "$@" >"${results}/${name}.out" 2>&1 || status=$?
    echo "  ${name}: exit ${status}"
    if [[ "$status" -ne 0 ]]; then
        failures=$((failures + 1))
    fi
}

run_step browser env \
    NODE_PATH="$node_path" \
    SCREENPLAY_BROWSER_PORT="$port" \
    SCREENPLAY_BROWSER_WORKBENCH_PORT="$workbench_port" \
    SCREENPLAY_BROWSER_RESULT="${results}/browser.json" \
    node "${harness}/screen-composition-browser-native-controls.cjs"

if [[ -n "${SCREENPLAY_AI_VERIFICATION:-}" ]]; then
    run_step mcp bash -c "cd \"\$1\" && yarn tsx screenplay-mcp-transcript.ts --corpus \"\$2\" --scratch \"\$3\"" _ \
        "$SCREENPLAY_AI_VERIFICATION" \
        "${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder" \
        "${root}/${results}/mcp-scratch"
else
    echo "  mcp: not run (set SCREENPLAY_AI_VERIFICATION to the Cratis/AI Source/Verification folder)"
    failures=$((failures + 1))
fi

run_step studio env \
    NODE_PATH="$node_path" \
    SCREENPLAY_STUDIO_RESULT="${results}/studio.json" \
    node "${harness}/screen-composition-studio-production-play.cjs"

if [[ -f "${results}/browser.json" ]]; then
    node -e '
        const r = require(process.argv[1]);
        console.log(`  browser status: ${r.status}; stage image: ${r.vector?.stageImage ?? "unknown"}; blockers: ${r.remainingBlockers.length}`);
        for (const blocker of r.remainingBlockers) console.log(`    - ${blocker}`);
    ' "${root}/${results}/browser.json"
fi

if [[ "$failures" -gt 0 ]]; then
    echo "run-public-acceptance: ${failures} harness(es) did not pass"
    exit 1
fi

echo "run-public-acceptance: every harness passed"
