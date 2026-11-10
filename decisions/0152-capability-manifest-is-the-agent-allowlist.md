---
id: 0152
title: Emit a capability manifest only from the explicit agents allowlist
status: accepted
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Agents/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Documentation/screenplay/**
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[Decision 0151](0151-explicit-agent-exposure-and-fail-closed-comparison.md) makes agent exposure an
explicit default-deny allowlist. Consumers such as Arc need a small portable declaration, not an ESM
parser or inferred exposure. This is an export/transport contract, not a language change.

Number 0152 is a local placeholder pending coordination with the parent release train.

## Decision

The user accepted the #1199 plan and its binding review additions, and specified nested-feature
identity and the description bound on November 10, 2026. Emit `screenplay-capability-manifest/1` only
through `AgentCapabilities.From` on a successful complete compilation. No agents block and any
compilation error refuse emission; never emit a valid subset. An explicit empty block emits an empty
array. Unlisted operations never appear. The contract is independent of ESM version numbering.

Publish [the strict schema](../Documentation/screenplay/capability-manifest-v1.schema.json) and paired
[model](../Documentation/screenplay/capability-manifest-v1.example.play)/
[golden](../Documentation/screenplay/capability-manifest-v1.example.json) as shared consumer cases.

`features` is an ordered array, outermost first. The current
[grammar](../Documentation/screenplay/grammar.md) admits operations only inside slices inside features,
so the array is non-empty and `slice` is always present. `Ident = Letter, { Letter | Digit | "_" }`
excludes `.` in segment names. Canonical `operation` joins module, all features, slice when present,
then name with dots. No segments may be omitted or fuzzy-matched. Consumers map **exact canonical
identity plus kind** to registered operations; zero matches, multiple matches or kind mismatches
expose nothing. CLR and query-FQN mapping belongs to the consumer.

Descriptions are untrusted authored text. The manifest-only limit is **1024 Unicode code points**,
not UTF-16 units. Exactly 1024 is accepted; 1025 refuses the entire manifest with `DescriptionTooLong`
naming the operation. Never truncate or modify source-language admission. ASCII JSON escaping is
encoding, not prompt-injection protection.

Only personal tokens are supported. `approvalRequired` is always false until #1166 admits approval;
a future contract may admit true. False is **not authorization**. The agents allowlist and token kind
constrain exposure; token scope and effective runtime authorization must still be checked. Unknown
contracts, invalid shapes, unknown properties, duplicate operation keys and absent manifests fail closed.

CLI `export --format capability-manifest` rejects `--scope` and `--screens`. Refusals exit 1 and write
only diagnostics, not a manifest. Failed exports with a parsed file target delete an existing target
and report its removal, rather than leave an old allowlist active. Successful writes atomically replace
the target; identical bytes do not rewrite it. MCP `read-workspace` exposes the same bytes via
`capability-manifest`, preserving revision-pinned paging and unavailable/stale refusal conventions.

Serialization uses ordinal identity/kind order, camelCase, stable property order, ASCII-escaped UTF-8
without BOM, LF and exactly one trailing newline. There are no host paths, revisions, ESM versions,
timestamps, or platform-dependent fields inside the manifest.

## Consequences

Consumers receive exactly the author's allowlist, not every public operation. Strict unknown-field
handling requires explicit consumer upgrades for future contracts. Removing stale targets is deliberately
fail-closed and differs from ESM/UI export. No endpoint hosting, tokens, approval workflow, runtime
registration or execution implementation is added. TypeScript compiler, Monaco and VS Code are unchanged:
this feature adds no `.play` syntax, diagnostics or meaning.
