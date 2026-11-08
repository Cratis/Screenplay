---
id: 0147
title: The HTTP API and the screens are stack-neutral contracts derived beside the executable model
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Api/**
  - Source/DotNET/Screenplay/Ui/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Documentation/screenplay/**
---

## Context

[#1186](https://github.com/Cratis/Screenplay/issues/1186) asks for a stack-neutral description of a model's HTTP API, and
[#1187](https://github.com/Cratis/Screenplay/issues/1187) for a stack-neutral contract of its screens. Routes, methods and
payloads were implied by Arc's conventions as configured by the Cratis renderer, and screens exist only as syntax built
on Scene vocabulary: binding reports them as deferred (`PLAY0269`).

The executable semantic model (ESM) is the portable authority for executable meaning. Admitting a construct to it needs
canonical bytes, a golden vector, reference execution or a typed unsupported result, and conformance vectors
([0004](0004-admission-and-governance-of-portable-executable-semantics.md)). Realization choices such as passive read
models ([0135](0135-passive-read-models-are-realization-metadata.md)) and sinks
([0146](0146-sinks-are-realization-metadata.md)) stay outside it.

## Decision

1. **The HTTP API is derived from the ESM, not stored in it.** `ApiDescription.For(model)` computes every command and
   query operation from the executable model and `ApiConventions`; `OpenApiDocument.Write` writes it as OpenAPI 3.1 with
   `x-screenplay` extensions for kind, identity, slice, authorization, policies and observable delivery. The default
   conventions are the Cratis renderer's: prefix `api`, kebab-cased module, feature and slice names, the query name
   always, and the command name only when a slice declares more than one command.
2. **Screens are described by a versioned UI contract beside the ESM.** `UiContractBuilder` reads the merged sources
   and, when there is one, the executable model, and writes `screenplay-ui/1` JSON: data, actions, navigation and layout
   per screen, with semantic identities, routes and the model revision taken from the executable model. Screens stay
   deferred from the ESM (`PLAY0269`), because a screen has no reference execution and the contract must be readable for
   a model whose executable model is refused.
3. **Scene is an optional layer.** Everything Scene-specific (templates with blueprints, component properties and
   presentation, toolbars, outlets) is under `scene`; the neutral members never require it.
4. **The library, CLI and MCP give the same bytes.** `screenplay export --format openapi|ui` and the `read-workspace`
   views `api-description` and `ui-contract` write exactly what the library writes.

## In scope

The two derived documents, their .NET API, the CLI formats, the MCP views and their documentation. Admitting screens as
ESM members is out of scope.

## Alternatives and consequences

- **Admit screens as ESM members in a new ESM version.** Rejected for now: it would need reference execution or a typed
  unsupported result, canonical vectors and every consumer pinning the new version, while the contract would then be
  unavailable for any model that does not bind. If a target ever needs screens inside the ESM, `screenplay-ui/1`
  is the shape to admit.
- **Store routes in the ESM.** Rejected: routes are a target's convention. Keeping them derived lets another target pass
  its own `ApiConventions` without a new ESM version.
- **Consequence.** A change to the Cratis renderer's route settings must be mirrored in `ApiConventions.Cratis`.
  Behavior wiring beyond navigation and command execution is not in `screenplay-ui/1`.

## Downgrade path

Both documents are derived; removing them removes only the CLI formats, MCP views and .NET types, with no change to
sources or ESM bytes.
