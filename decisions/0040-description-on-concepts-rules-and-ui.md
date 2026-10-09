---
id: 0040
title: Accept body-line descriptions on concepts, policies, constraints, projections, screens and forms
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[#535](https://github.com/Cratis/Screenplay/issues/535) extends [0035](0035-keep-model-reasoning-as-report-only-metadata.md). Concepts, policies, constraints, projections, screens and forms currently reject descriptions, leaving their intent in comments that tools cannot surface. A concept may be one line, and enumeration bodies also contain bare value names.

## Decision

Each of these six kinds accepts one indented `description`, using the existing quoted or fenced-text form. A one-line concept gains a body. For an enum, quoted descriptions and a bare `description` followed by an indented fence are directives; bare `description` without that fence remains a value and prints as `@description`.

The description is the first canonical body line, before `file` and every other directive. It does not satisfy a policy implementation, constraint rule or projection directive requirement. It is not a declarative option on a file constraint, not a screen title or constraint message, and not localizable UI text. Projection variants, nested blocks, screen sections and form fields do not gain descriptions.

Descriptions are report-only authoring metadata. Binding bound kinds reports `PLAY0270` Information and adds no executable bytes or version. Screens and forms remain outside executable binding. MCP declaration summaries expose descriptions. Markdown `documentation` is not added to these kinds.

## Options considered

- **Body line (chosen):** one spelling across declarations, reusing description parsing and printing.
- **Trailing concept header string:** rejected as a concept-only spelling that competes with future header syntax.
- **Captured comments:** rejected because comments remain trivia, not typed authoring metadata.
- **Also accept markdown documentation:** deferred; it widens scope without a demonstrated need.

## Default if unanswered

The existing rejection diagnostics remain. Authors continue hiding intent in comments that the AST and MCP cannot expose.

## Timeline and scope

This ruling holds until superseded. Scope includes both compilers, AST transport, printer/trivia, metadata binding, folder layout, MCP, editors, conformance, docs and Invoicing. Board presentation and Stage rendering remain separate adaptations; this record does not promise generated comments or runtime behavior.

## Verification

**Done when:** every kind parses and prints descriptions consistently, enum `description` remains a value without a fence, descriptions do not satisfy implementation/rule requirements, executable revisions are unchanged, and MCP summaries return the text.

**Verify by:** parser/printer and negative specs, a shared conformance vector, semantic byte-equivalence specs, MCP summaries, editor completion/grammar specs, documentation and sample compilation, regenerated transport/contract and focused repository gates.

## Consequences

Intent travels as optional AST metadata without changing executable behavior. Consumers preserve six new optional members. An enum value named `description` prints escaped.
