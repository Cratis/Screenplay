#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Renders every domain blueprint sample (Samples/Crm, Ticketing, Approvals, Inventory, Hr, Projects, Administration, Orders, Catalog, Vendors, Billing, CustomerPortal, Contracts, WorkOrders, Bookings) exactly as
# authored with the installed `cratis` and the approved package set of the blueprint's published npm package, and fails when one does not
# render - a Stage admission refusal, an unknown package, or a `template blueprint ...` reference to a template the package does not ship
# (Cratis/Stage#457). The samples are the examples of the language; a render that needs them simplified is a defect of the sample or of the
# renderer, found here rather than by a user.
#
#   render-blueprint-samples.sh <work-folder>
#
# The samples are written in the language of this branch, and the published CLI carries the language of the last release. Across a
# major the two cannot meet: the samples move to the new major in the pull request that introduces it, and no published `cratis` can
# read them until a CLI carrying that major is released. The major the samples are written in is the highest major a migration step
# in Source/DotNET/Screenplay/Migrations leads to (samples never carry a form the migration rewrites). When the installed CLI carries
# an older major, the run says so and renders nothing; it renders again by itself once a CLI with that major is installed.
#
# Optional environment:
#   SCREENPLAY_BLUEPRINT_VERSION  the @cratis/scene.blueprint.* version to approve (default: latest)
#
# Exit codes: 0 every sample rendered (or the installed CLI carries an older major, said in a warning), 1 a sample did not render,
# 2 the run could not start.
set -uo pipefail

root="$(git rev-parse --show-toplevel)"
harness="${root}/Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness"
work="$(mkdir -p "${1:?usage: render-blueprint-samples.sh <work-folder>}" && cd "$1" && pwd)"
version="${SCREENPLAY_BLUEPRINT_VERSION:-latest}"

command -v cratis >/dev/null 2>&1 || { echo "render-blueprint-samples: cratis is not on PATH" >&2; exit 2; }
cli_version="$(cratis --version)"
echo "render-blueprint-samples: CLI ${cli_version}, blueprint packages @${version}"

cli_major="$(sed -nE 's/.*\(Screenplay ([0-9]+)\..*/\1/p' <<<"$cli_version")"
[[ -n "$cli_major" ]] || { echo "render-blueprint-samples: cannot tell which Screenplay '${cli_version}' carries" >&2; exit 2; }
samples_major="$(grep -rhoE --include='*.cs' --exclude-dir='for_*' 'ScreenplayVersion To => [0-9]+' "${root}/Source/DotNET/Screenplay/Migrations" \
    | sed -E 's/.*=> //' | sort -n | tail -1)"
[[ -n "$samples_major" ]] || { echo "render-blueprint-samples: no migration step says which Screenplay major the samples are written in" >&2; exit 2; }
if (( cli_major < samples_major )); then
    message="the samples are written in Screenplay ${samples_major} and the installed CLI carries Screenplay ${cli_major}; nothing renders them until a CLI with Screenplay ${samples_major} is published"
    [[ -n "${GITHUB_ACTIONS:-}" ]] && echo "::warning title=Blueprint samples not rendered::${message}"
    echo "render-blueprint-samples: ${message}"
    exit 0
fi

mkdir -p "$work/packages"
(cd "$work/packages" && npm init -y >/dev/null && npm install --no-audit --no-fund --silent \
    "@cratis/scene.blueprint.crm@${version}" "@cratis/scene.blueprint.ticketing@${version}" "@cratis/scene.blueprint.approvals@${version}" \
    "@cratis/scene.blueprint.inventory@${version}" "@cratis/scene.blueprint.hr@${version}" "@cratis/scene.blueprint.project-tracker@${version}" \
    "@cratis/scene.blueprint.administration@${version}" "@cratis/scene.blueprint.orders@${version}" \
    "@cratis/scene.blueprint.catalog@${version}" "@cratis/scene.blueprint.vendors@${version}" "@cratis/scene.blueprint.invoicing@${version}" \
    "@cratis/scene.blueprint.customer-portal@${version}" "@cratis/scene.blueprint.contracts@${version}" "@cratis/scene.blueprint.work-orders@${version}" \
    "@cratis/scene.blueprint.bookings@${version}") \
    || { echo "render-blueprint-samples: the blueprint packages could not be installed" >&2; exit 2; }

failures=0
# sample folder | npm package suffix | export | application name (not a folder name of the sample: the project would collide with it)
for entry in "Crm|crm|crmBlueprint|CrmApp" "Ticketing|ticketing|ticketingBlueprint|TicketingApp" "Approvals|approvals|approvalsBlueprint|ApprovalsApp" \
    "Inventory|inventory|inventoryBlueprint|InventoryApp" "Hr|hr|hrBlueprint|HrApp" "Projects|project-tracker|projectTrackerBlueprint|ProjectsApp" \
    "Administration|administration|administrationBlueprint|AdministrationApp" "Orders|orders|ordersBlueprint|OrdersApp" \
    "Catalog|catalog|catalogBlueprint|CatalogApp" "Vendors|vendors|vendorsBlueprint|VendorsApp" "Billing|invoicing|invoicingBlueprint|BillingApp" \
    "CustomerPortal|customer-portal|customerPortalBlueprint|PortalApp" "Contracts|contracts|contractsBlueprint|ContractsApp" "WorkOrders|work-orders|workOrdersBlueprint|WorkOrdersApp" \
    "Bookings|bookings|bookingsBlueprint|BookingsApp"; do
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
