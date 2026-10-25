#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo 'Usage: run-specifications.sh PROJECT [Debug|Release] [native runner options...]' >&2
    exit 2
fi

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
cd "$repo_root"
project=$1
shift
configuration=Debug
case "${1:-}" in
    Debug|Release) configuration=$1; shift ;;
esac

project_directory=$(cd "$(dirname "$project")" && pwd)
project_name=$(basename "$project" .csproj)
assembly_name="Cratis.$project_name"
leading_args=()
case "$project_name" in
    Tool)
        if [ "$configuration" = Release ]; then
            echo 'Tool specifications require Debug; the Release tool has no specification runner.' >&2
            exit 2
        fi
        assembly_name=Cratis.Screenplay.Tool
        leading_args=(--specifications)
        ;;
esac
assembly="$project_directory/bin/$configuration/net10.0/$assembly_name.dll"
if [ ! -f "$assembly" ]; then
    echo "Build the $configuration net10.0 specification executable first: $assembly" >&2
    exit 2
fi

results_root=${SPEC_RESULTS_ROOT:-"$repo_root/.ai-work/specifications"}
mkdir -p "$results_root"
results_directory=$(mktemp -d "$results_root/run.XXXXXXXX")
trx="$results_directory/results.trx"
started=$(date +%s)
# Native exit codes remain authoritative; a fresh positive TRX is required in addition.
dotnet "$assembly" ${leading_args[@]+"${leading_args[@]}"} -result-trx "$trx" "$@"
dotnet run --file "$repo_root/.github/scripts/verify-specifications-trx.cs" -- "$trx" "$started"
