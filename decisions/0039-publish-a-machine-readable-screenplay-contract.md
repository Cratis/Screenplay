---
id: 0039
title: Publish a machine-readable Screenplay contract
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting (delegated to the implementing agent's recommendation)
applies-to:
  - Source/DotNET/Screenplay.Contracts/**
  - Source/DotNET/Tool/**
  - Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs
  - .github/workflows/publish.yml
  - Documentation/screenplay/tool.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[Screenplay #500](https://github.com/Cratis/Screenplay/issues/500) needs a release-specific contract that the AI corpus can compare with its own facts. Repeating keyword, diagnostic, tool and version lists in another document would create another drift source.

## Decision

Publish schema-version 1 JSON through `screenplay contract` and an MCP-independent `ScreenplayContract.Write` library entry point. Generation requires neither a model folder nor an MCP connection. The library reads build-embedded compiler sources for parser dispatch, diagnostic catalog summaries and diagnostic emission severities; MCP descriptions and argument schemas come from the live catalog. CLI metadata comes from the tool's usage and argument dispatch. Supported ESM versions come from the schema support types.

Each construct has `parseStatus: accepted` and an admission entry per ESM schema version: `esmVersion`, `status` (`admitted`, `refused`, `conditional`), `diagnostic`, `issue`, and `condition` only for conditional admission. Minimum versions and refusal diagnostics are obtained by compiling and binding representative source probes. Where the binder cannot expose a complete construct-level rule mechanically, one explicit probe table supplies the condition and source examples. Specs hold the table to actual binding behavior, and require a probe for every dispatched construct. Metadata that binds but is not carried into the backend ESM is refused with its report-only/deferred diagnostic, not advertised as executable.

Diagnostic titles are the C# `DiagnosticCodes` XML summaries, not documentation or Monaco copy. Severity is derived from C# emission sites, including the named helpers used for indirect emissions; reserved catalog entries explicitly retain their original severity. The primary severity is the highest emitted severity and `severities` preserves all possible severities. Historically retired codes are recorded explicitly in code, retaining their original title and severity. An unused but still declared code is not retired. Missing titles or severities fail generation/specs rather than silently defaulting to an error.

A checked-in golden captures the entire generated document. Regeneration is an explicit CLI command; normal specs only compare. Release builds publish the generated document as an asset and package content. Cross-repository dispatch uses an appropriately scoped publishing credential and fails if notification fails.

## Options considered

- **Generated JSON with embedded owning sources and binder probes (chosen).** Works from an installed package without a checkout and remains connected to the existing sources of truth.
- **A manually maintained contract table.** Rejected for keywords, diagnostics, MCP schemas and versions; it would reproduce the original drift problem. A bounded, tested admission table is necessary for conditional rules the binder does not expose as metadata.
- **MCP-only introspection.** Rejected: consumers should not need a model or protocol session to read release facts.
- **Changing parser and diagnostic APIs to use a new registry.** Rejected for this change: it would broaden the refactoring and risk changing compiler behavior.

## Verification

Done when the golden comparison covers every dispatched construct, PLAY code, MCP tool and parameter, CLI command/option and supported ESM version; admission probes agree with the binder; the command works without a model; documentation shows regeneration; release packaging and asset publication use the same generated JSON.

## Consequences

The contract schema is a public interface: incompatible JSON changes require a schema-version increment. Compiler-source extraction patterns are deliberately bounded and fail on unclassified diagnostics or constructs. New admission rules need representative probes and explicit conditional wording, not claims that parsing implies execution. Cratis/AI's drift check remains a separate work item (AI #529).
