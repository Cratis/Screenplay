---
id: 0035
title: Keep model reasoning as report-only authoring metadata
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[Issue #392](https://github.com/Cratis/Screenplay/issues/392) asks for specifications to name the rule they witness and for modeling reasoning to remain with declarations rather than in disconnected notes. Events already carry report-only fenced Markdown documentation. This record documents the bounded implementation requested for that issue; acceptance remains a human verdict.

## Decision

Specifications accept one quoted or fenced-text `description`, using the existing description grammar. Modules, features, slices, commands, read models and reactions accept one nonempty fenced-Markdown `documentation` block using the event pattern. These fields are report-only (`PLAY0270`): they add no executable-model bytes and allocate no ESM version. Event malformed-documentation diagnostics remain `PLAY0477`; the new owners share `PLAY0558`. A bare documentation directive never declares a property.

Folder merge keeps the first module or feature documentation, accepts identical copies and warns on conflicting copies (`PLAY0559`). Expanded files do not restate documentation in wrapper headers. MCP declaration summaries expose `description` and `documentation` for every kind supporting those fields.

## Options considered

- **Reuse report-only metadata:** chosen; preserves executable compatibility while making reasoning available through the typed AST and tools.
- **Serialize prose into the ESM:** rejected; prose is not behavior and must not change executable revisions or require a version migration.
- **Expand metadata to every declaration kind:** deferred to a separate issue. The issue comment's concepts, policies, constraints, projections, screens and forms are not part of this bounded change.

## Default if unanswered

The existing language remains unchanged: specifications cannot describe their case and non-event reasoning remains outside typed metadata. This costs continuity between modeling sessions.

## Timeline and scope

This contract holds from the metadata release until explicitly superseded. The in-repository scope includes both compilers, AST transport, printer and round trips, folder merge/layout, diagnostics, MCP, editors, conformance, documentation and Invoicing. Rendering, code extraction and downstream consumers need their own adaptations; metadata does not promise generated code comments or runtime enforcement.

## Verification

**Done when:** every supported owner parses and prints its metadata, MCP summaries expose it, conflicting folder fragments warn, and adding metadata preserves canonical executable bytes.

**Verify by:** compiler metadata and round-trip specs, semantic byte-equivalence specs, folder merge specs, MCP summary specs, shared conformance vectors and editor grammar/completion specs, followed by the repository CI gates.

## Consequences

Reasoning travels with the model without affecting execution. Consumers must preserve the new optional AST members even when they do not display them. New owners use Markdown for documentation and text for descriptions; event description syntax retains its existing behavior.
