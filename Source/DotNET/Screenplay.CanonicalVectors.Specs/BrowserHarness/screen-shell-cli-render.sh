#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# CLI leg of the application shell vector: renders the `screen-shell-navigation-runnable` typed case of the screens corpus with
# the installed public `cratis` (3.67.0 or later bundles the Stage that loads navigation regions, toolbar slice buttons and
# sample data) and requires the module navigation targets and the toolbar slice in the generated scene.json and no sample
# data anywhere in the generated application (spec SD6). The same case is a Stage plan in the corpus, so the CLI and Stage
# plans read one document.
#
#   screen-shell-cli-render.sh <work-folder> [corpus-package-version]
#
# Without a corpus package version the case is read from this repository's corpus resource.
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
harness="${root}/Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness"
work="$(mkdir -p "${1:?usage: screen-shell-cli-render.sh <work-folder> [corpus-package-version]}" && cd "$1" && pwd)"
corpus_version="${2:-}"

command -v cratis >/dev/null 2>&1 || { echo "screen-shell-cli-render: cratis is not on PATH" >&2; exit 2; }
rm -rf "$work/model" "$work/app"
mkdir -p "$work/model"
if [[ -n "$corpus_version" ]]; then
    line="$(dotnet run --project "${harness}/PublishedCorpus" -p:CorpusPackageVersion="$corpus_version" -- --case screen-shell-navigation-runnable "$work/model" | tail -1)"
    [[ "$line" == *"CanonicalCorpus ${corpus_version}"* ]] || { echo "screen-shell-cli-render: expected corpus ${corpus_version}, got: ${line}" >&2; exit 2; }
    echo "corpus from the published package: ${line}"
else
    cp "${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/positive/ScreenShellRunnable.play.txt" "$work/model/application.play"
fi

cratis render "$work/model" --name Shell --destination "$work/app" -o json > "$work/render.json" 2> "$work/render.err"
node -e '
const fs = require("fs");
const app = process.argv[1];
const text = fs.readFileSync(app + "/scene.json", "utf8");
const scene = JSON.parse(text);
const targets = new Set();
const walk = value => {
    if (Array.isArray(value)) { value.forEach(walk); return; }
    if (value && typeof value === "object") {
        if (value.target && value.target.module) targets.add(value.target.module + (value.target.feature ? "." + value.target.feature : ""));
        Object.values(value).forEach(walk);
    }
};
walk(scene.navigation);
const fail = message => { console.error(message); process.exit(1); };
for (const target of ["Sales.Orders", "Sales.Customers", "Support.Tickets", "Support.KnowledgeBase"]) if (!targets.has(target)) fail("navigation target " + target + " is missing; found " + [...targets].join(","));
if (!text.includes("Sales.Orders.PlaceOrder")) fail("the toolbar button does not carry its slice");
if (text.includes("Sample customer that must never ship")) fail("sample data reached scene.json");
console.log("navigation targets: " + [...targets].join(", "));
' "$work/app"
if grep -rq "Sample customer that must never ship" "$work/app" --include='*.json' --include='*.cs' --include='*.ts' --include='*.tsx'; then
    echo "screen-shell-cli-render: sample data reached the generated application" >&2
    exit 1
fi
echo "screen-shell-cli-render: passed with cratis $(cratis --version)"
