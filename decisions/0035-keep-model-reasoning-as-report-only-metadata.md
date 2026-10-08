---
id: 0035
title: Keep model reasoning as report-only authoring metadata
status: accepted
stage: implemented
decided: 2026-10-08
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/DocumentationParser.cs
  - Source/DotNET/Screenplay/Parsing/DescriptionParser.cs
  - Source/DotNET/Screenplay/Parsing/SpecificationParser.cs
  - Source/DotNET/Screenplay/Files/PlayFolderMerge*.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder*.cs
  - Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs
  - Source/Screenplay/Compiler/Parsing/DocumentationParser.ts
  - Source/Screenplay/Compiler/Parsing/DescriptionParser.ts
  - Source/Screenplay/Compiler/Parsing/SpecificationParser.ts
  - Source/Screenplay/Compiler/Files/PlayFolderMerge.ts
  - Documentation/screenplay/slices.md
  - Documentation/screenplay/folders.md
  - Documentation/screenplay/diagnostics.md
---

## Context

[Issue #392](https://github.com/Cratis/Screenplay/issues/392) asks for specifications to name the rule they witness and for modeling reasoning to remain with declarations rather than in disconnected notes. Events already carry report-only fenced Markdown documentation. This record documents the bounded implementation requested for that issue.

## Decision

Specifications accept one quoted or fenced-text `description`, using the existing description grammar. Modules, features, slices, commands, read models and reactions accept one nonempty fenced-Markdown `documentation` block using the event pattern. These fields are report-only (`PLAY0270`): they add no executable-model bytes and allocate no ESM version. Event malformed-documentation diagnostics remain `PLAY0477`; the new owners share `PLAY0558`. A bare documentation directive never declares a property.

Folder merge keeps the first module or feature documentation, accepts identical copies and warns on conflicting copies (`PLAY0559`). Expanded files do not restate documentation in wrapper headers. MCP declaration summaries expose `description` and `documentation` for every kind supporting those fields.

[#535](https://github.com/Cratis/Screenplay/issues/535) may extend the same report-only rule to more declaration kinds (concepts, policies, constraints, projections, screens, forms) without a new record, provided each kind adds no executable-model bytes and settles its own placement.

## Options considered

- **Reuse report-only metadata:** chosen; preserves executable compatibility while making reasoning available through the typed AST and tools.
- **Serialize prose into the ESM:** rejected; prose is not behavior and must not change executable revisions or require a version migration.
- **`documentation` on specifications:** not added. A specification already is the worked example of its rule, so one short `description` names the rule it witnesses; a Markdown block would duplicate the reasoning the owning command or slice carries.
- **Expand metadata to every declaration kind:** deferred to a separate issue. The issue comment's concepts, policies, constraints, projections, screens and forms are not part of this bounded change.

## Default if unanswered

The shipped behavior stands without a recorded contract: later changes could silently alter the AST members, the report-only status or the merge rules that tools already rely on, and each would be a fresh debate instead of a supersession.

## Timeline and scope

This contract holds from the metadata release until explicitly superseded. The in-repository scope includes both compilers, AST transport, printer and round trips, folder merge/layout, diagnostics, MCP, editors, conformance, documentation and Invoicing. Rendering, code extraction and downstream consumers need their own adaptations; metadata does not promise generated code comments or runtime enforcement.

## Verification

**Done when:** every supported owner parses and prints its metadata, MCP summaries expose it, conflicting folder fragments warn, and adding metadata preserves canonical executable bytes.

**Verify by:** compiler metadata and round-trip specs, semantic byte-equivalence specs, folder merge specs, MCP summary specs, shared conformance vectors and editor grammar/completion specs, followed by the repository CI gates.

## Consequences

Reasoning travels with the model without affecting execution. Consumers must preserve the new optional AST members even when they do not display them. New owners use Markdown for documentation and text for descriptions; event description syntax retains its existing behavior.
