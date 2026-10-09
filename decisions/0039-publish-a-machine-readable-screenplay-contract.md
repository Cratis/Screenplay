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

Publish schema-version 1 JSON through `screenplay contract` and an MCP-independent `ScreenplayContract.Write` library entry point. Generation requires neither a model folder nor an MCP connection. The library reads a compiler-generated catalog for parser dispatch, diagnostic catalog summaries and diagnostic emission severities; MCP descriptions and argument schemas come from the live catalog. CLI metadata comes from the same typed command and option definitions the tool uses for dispatch and argument comparisons. No compiler or CLI source is embedded or scanned at runtime. Supported ESM versions come from the schema support types.

Each construct has `parseStatus: accepted` and an admission entry per ESM schema version: `esmVersion`, `status` (`admitted`, `refused`, `conditional`), `diagnostic`, `issue`, and `condition` only for conditional admission. Each construct also publishes its `probes`: source, observed minimum semantic version, actual primary disposition diagnostic, every raised diagnostic code and severity, and admission per schema. Minimum versions and refusal diagnostics are obtained by compiling and binding each source, including advanced forms. A conditional entry's `condition` contains zero-based indexes into `probes` for forms refused by that schema. Version-only refusal has a null diagnostic; no generic PLAY code is substituted. Specs independently bind every published source and compare its version, status and diagnostic, then verify the aggregate entry. A probe is required for every dispatched construct. Metadata that binds but is not carried into the backend ESM is refused with its report-only/deferred diagnostic, not advertised as executable.

Diagnostic titles are the C# `DiagnosticCodes` XML summaries, not documentation or Monaco copy. A Roslyn generator resolves dispatch (including comparisons, patterns and multiword phrases), reserved-word sets and regex-grammar terminals, XML catalog summaries and bound symbol flow from diagnostic constants through helper parameters and record properties to emission sites. Severity is derived from those emission sites; unused catalog constants carry a reservation attribute retaining their historical severity and retirement status. The compiler fails if a reserved code acquires any emission site, or an unreserved code has no classified emission. The primary severity is the highest emitted severity and `severities` preserves all possible severities. Historically retired codes are recorded explicitly in code, retaining their original title and severity. An unused but still declared code is not retired. Missing titles or severities fail generation/specs rather than silently defaulting to an error.

A checked-in golden captures the entire generated document. Regeneration is an explicit CLI command; normal specs only compare. Release builds publish the generated document as an asset and package content. Cross-repository dispatch uses an appropriately scoped publishing credential and fails if notification fails.

## Options considered

- **Generated compiler catalogs, shared CLI definitions and binder probes (chosen; derivation corrected after review on 2026-10-08).** Works from an installed package without a checkout. The catalog is generated in the owning compiler compilation, and the CLI consumes its published definitions rather than independently spelling command and option tokens.
- **A manually maintained contract table.** Rejected for keywords, diagnostics, MCP schemas and versions; it would reproduce the original drift problem. A bounded, tested admission table is necessary for conditional rules the binder does not expose as metadata.
- **MCP-only introspection.** Rejected: consumers should not need a model or protocol session to read release facts.
- **Changing parser dispatch to use a new registry.** Rejected: it would broaden the refactoring and risk changing compiler behavior. Compile-time catalog generation reads the bound grammar instead. Diagnostic reservation attributes and shared CLI definitions classify facts without changing accepted syntax.

## Verification

Done when the golden comparison covers every dispatched construct, PLAY code, MCP tool and parameter, CLI command/option and supported ESM version; admission probes agree with the binder; the command works without a model; documentation shows regeneration; release packaging and asset publication use the same generated JSON.

## Consequences

The contract schema is a public interface: incompatible JSON changes require a schema-version increment. Compiler catalog generation fails on unclassified diagnostics or constructs. New admission rules need representative binding probes, not unchecked condition prose or claims that parsing implies execution. Cratis/AI's drift check remains a separate work item (AI #529).
