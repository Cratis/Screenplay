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
#   SCREENPLAY_CORPUS_PACKAGE_VERSION  read the corpus from this published Cratis.Screenplay.CanonicalCorpus version
#   SCREENPLAY_PLAYWRIGHT_NODE_PATH  NODE_PATH that resolves `playwright` (defaults to NODE_PATH)
#   SCREENPLAY_AI_VERIFICATION       Cratis/AI Source/Verification folder for the MCP stdio transcript
#   SCREENPLAY_STUDIO_URL            deployed Studio URL for the production save/export/import/Play vector. Defaults to
#                                    http://127.0.0.1:5156 when that answers (the production principal-header proxy of the
#                                    debugging rule: `kubectl port-forward -n studio deploy/core 5155:8080` and
#                                    `tools/studio-prod-proxy.mjs <sub> <name> <tenant> 5156 5155`).
#   SCREENPLAY_STUDIO_CHECKOUT       a Cratis/Studio checkout (installed with `yarn install`) whose
#                                    scripts/screen-editor/check-studio-production-scratch.mjs runs the vector against a scratch
#                                    project it deletes again. Without it the harness records blocked-missing-studio-checkout.
#
# Harnesses: browser (native controls), render-audit (determinism, portable artifacts, profile rejection,
# render-then-recover), shell-cli (the application shell vector rendered by the CLI), mcp (stdio transcript) and studio (production Play).
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

for self_tested in screen-composition-browser-native-controls.cjs screen-composition-render-audit.cjs; do
    if ! node "${harness}/${self_tested}" --self-test; then
        echo "run-public-acceptance: the ${self_tested} self-test failed; its results cannot be trusted" >&2
        exit 2
    fi
done

# Read the corpus from the published package rather than this repository (Screenplay#173.10).
corpus_version="${SCREENPLAY_CORPUS_PACKAGE_VERSION:-}"
if [[ -n "$corpus_version" ]]; then
    corpus_folder="$(mktemp -d)/ScreenComposition"
    if ! corpus_line="$(dotnet run --project "${harness}/PublishedCorpus" -p:CorpusPackageVersion="$corpus_version" -- "$corpus_folder" | tail -1)"; then
        echo "run-public-acceptance: could not materialize Cratis.Screenplay.CanonicalCorpus ${corpus_version}" >&2
        exit 2
    fi
    if [[ "$corpus_line" != *"CanonicalCorpus ${corpus_version}"* ]]; then
        echo "run-public-acceptance: expected corpus ${corpus_version}, got: ${corpus_line}" >&2
        exit 2
    fi
    export SCREENPLAY_CORPUS_SOURCE="$corpus_folder"
    echo "run-public-acceptance: corpus from the published package: ${corpus_line}"
else
    echo "run-public-acceptance: corpus from this repository (set SCREENPLAY_CORPUS_PACKAGE_VERSION to read the published package)"
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

run_step render-audit env \
    SCREENPLAY_RENDER_AUDIT_RESULT="${results}/render-audit.json" \
    node "${harness}/screen-composition-render-audit.cjs"

# The application shell vector through the CLI: renders the screen-shell-navigation-runnable case and checks the navigation,
# the toolbar slice and that no sample data ships. Needs the CLI that bundles the shell Stage (3.67.0 or later).
run_step shell-cli "${harness}/screen-shell-cli-render.sh" "${results}/shell-cli" ${corpus_version:+"$corpus_version"}

if [[ -n "${SCREENPLAY_AI_VERIFICATION:-}" ]]; then
    run_step mcp bash -c "cd \"\$1\" && yarn tsx screenplay-mcp-transcript.ts --corpus \"\$2\" --scratch \"\$3\"" _ \
        "$SCREENPLAY_AI_VERIFICATION" \
        "${SCREENPLAY_CORPUS_SOURCE:-${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder}" \
        "${root}/${results}/mcp-scratch"
else
    echo "  mcp: not run (set SCREENPLAY_AI_VERIFICATION to the Cratis/AI Source/Verification folder)"
    failures=$((failures + 1))
fi

# Studio production vector: the scratch proof of the Studio repository (import the published corpus, edit and save a screen,
# export, Play) against a deployed Studio. The generic-selector harness (screen-composition-studio-production-play.cjs) only
# records the blocked state when no URL is reachable.
studio_url="${SCREENPLAY_STUDIO_URL:-}"
if [[ -z "$studio_url" ]] && curl -fsS -o /dev/null --max-time 5 "http://127.0.0.1:5156/" 2>/dev/null; then
    studio_url="http://127.0.0.1:5156"
fi
studio_checkout="${SCREENPLAY_STUDIO_CHECKOUT:-}"
if [[ -n "$studio_url" && -n "$studio_checkout" ]]; then
    studio_script="${studio_checkout}/scripts/screen-editor/check-studio-production-scratch.mjs"
    studio_corpus="${SCREENPLAY_CORPUS_SOURCE:-${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder}"
    studio_evidence="${root}/${results}/studio-evidence"
    studio_zip="${root}/${results}/studio-corpus.zip"
    if [[ ! -f "$studio_script" ]]; then
        echo "run-public-acceptance: ${studio_script} not found" >&2
        exit 2
    fi
    (cd "$studio_corpus" && rm -f "$studio_zip" && zip -qr "$studio_zip" .)
    run_step studio bash -c 'cd "$1" && STUDIO_URL="$2" STUDIO_IMPORT_ZIP="$3" STUDIO_EVIDENCE="$4" node scripts/screen-editor/check-studio-production-scratch.mjs' _ \
        "$studio_checkout" "$studio_url" "$studio_zip" "$studio_evidence"
    node -e '
        const fs = require("fs");
        const [evidence, out, url] = process.argv.slice(1);
        let scratch;
        try { scratch = JSON.parse(fs.readFileSync(`${evidence}/result.json`, "utf8")); } catch { scratch = undefined; }
        const steps = scratch?.steps ?? [];
        const failed = steps.filter(step => !step.passed).map(step => step.name);
        const play = scratch?.play;
        const status = scratch?.status === "running" || !scratch?.status ? "failed-no-result" : scratch.status;
        fs.writeFileSync(out, JSON.stringify({ name: "studio-production-scratch", studioUrl: url, status, failedSteps: failed, play, steps, evidence }, null, 2) + "\n");
    ' "$studio_evidence" "${root}/${results}/studio.json" "$studio_url"
else
    reason="blocked-missing-studio-url"
    if [[ -n "$studio_url" ]]; then reason="blocked-missing-studio-checkout"; fi
    echo "  studio: not run (${reason}); set SCREENPLAY_STUDIO_URL (or start the production proxy on 127.0.0.1:5156) and SCREENPLAY_STUDIO_CHECKOUT"
    run_step studio env \
        NODE_PATH="$node_path" \
        SCREENPLAY_STUDIO_URL="" \
        SCREENPLAY_STUDIO_RESULT="${results}/studio.json" \
        node "${harness}/screen-composition-studio-production-play.cjs"
fi

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
