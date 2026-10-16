#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Renders every domain blueprint sample (Samples/Crm, Ticketing, Approvals, Inventory, Hr, Projects, Administration, Orders) exactly as
# authored with the installed `cratis` and the approved package set of the blueprint's published npm package, and fails when one does not
# render - a Stage admission refusal, an unknown package, or a `template blueprint ...` reference to a template the package does not ship
# (Cratis/Stage#457). The samples are the examples of the language; a render that needs them simplified is a defect of the sample or of the
# renderer, found here rather than by a user.
#
#   render-blueprint-samples.sh <work-folder>
#
# Optional environment:
#   SCREENPLAY_BLUEPRINT_VERSION  the @cratis/scene.blueprint.* version to approve (default: latest)
#
# Exit codes: 0 every sample rendered, 1 a sample did not render, 2 the run could not start.
set -uo pipefail

root="$(git rev-parse --show-toplevel)"
harness="${root}/Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness"
work="$(mkdir -p "${1:?usage: render-blueprint-samples.sh <work-folder>}" && cd "$1" && pwd)"
version="${SCREENPLAY_BLUEPRINT_VERSION:-latest}"

command -v cratis >/dev/null 2>&1 || { echo "render-blueprint-samples: cratis is not on PATH" >&2; exit 2; }
echo "render-blueprint-samples: CLI $(cratis --version), blueprint packages @${version}"

mkdir -p "$work/packages"
(cd "$work/packages" && npm init -y >/dev/null && npm install --no-audit --no-fund --silent \
    "@cratis/scene.blueprint.crm@${version}" "@cratis/scene.blueprint.ticketing@${version}" "@cratis/scene.blueprint.approvals@${version}" \
    "@cratis/scene.blueprint.inventory@${version}" "@cratis/scene.blueprint.hr@${version}" "@cratis/scene.blueprint.project-tracker@${version}" \
    "@cratis/scene.blueprint.administration@${version}" "@cratis/scene.blueprint.orders@${version}") \
    || { echo "render-blueprint-samples: the blueprint packages could not be installed" >&2; exit 2; }

failures=0
# sample folder | npm package suffix | export | application name (not a folder name of the sample: the project would collide with it)
for entry in "Crm|crm|crmBlueprint|CrmApp" "Ticketing|ticketing|ticketingBlueprint|TicketingApp" "Approvals|approvals|approvalsBlueprint|ApprovalsApp" \
    "Inventory|inventory|inventoryBlueprint|InventoryApp" "Hr|hr|hrBlueprint|HrApp" "Projects|project-tracker|projectTrackerBlueprint|ProjectsApp" \
    "Administration|administration|administrationBlueprint|AdministrationApp" "Orders|orders|ordersBlueprint|OrdersApp"; do
    IFS='|' read -r sample suffix exported application <<<"$entry"
    node "${harness}/render-blueprint-samples.cjs" "$work/packages/node_modules" "$suffix" "$exported" "$work/${sample}.packages.json" \
        || { echo "render-blueprint-samples: ${sample}: the approved package set could not be made" >&2; exit 2; }
    rm -rf "$work/${sample}.app"
    if cratis render "${root}/Samples/${sample}" --target cratis --name "$application" --package-set "$work/${sample}.packages.json" \
        --destination "$work/${sample}.app" -o plain >"$work/${sample}.out" 2>&1; then
        echo "  ${sample}: rendered"
    else
        echo "  ${sample}: DID NOT RENDER (exit $?)"
        grep -E "error " "$work/${sample}.out" | head -10 | sed 's/^/      /'
        failures=$((failures + 1))
    fi
done

[[ "$failures" -eq 0 ]] || { echo "render-blueprint-samples: ${failures} sample(s) did not render" >&2; exit 1; }
echo "render-blueprint-samples: every sample rendered as authored"
