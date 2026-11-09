---
id: 0149
title: Screens are admitted to the executable model on request as ESM v42
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-09
decider: Accepted Screenplay#1187 (delegated)
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Ui/**
  - Source/DotNET/Screenplay.Conformance/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Documentation/screenplay/**
---

## Context

[#1187](https://github.com/Cratis/Screenplay/issues/1187) asks for screens in the executable model with their data,
actions and navigation, readable without Scene. [Decision 0147](0147-stack-neutral-api-and-ui-contracts.md) delivered
the `screenplay-ui/1` contract beside the executable semantic model (ESM) and kept screens deferred (`PLAY0269`),
because every ESM consumer pins the versions it reads and a model that does not bind still needs its UI contract.

## Decision

1. **ESM v42 admits screens as slice members.** A slice writes optional final member `screens`. A screen holds its
   identity, name, title, optional description, ordered data (a query identity, an optional `by` and the fields it
   shows), ordered actions (a command identity, label, subject, every command input as a property identity with an
   optional `from`, an optional `when` and an optional `then` screen identity), ordered navigation (a target screen
   identity, a trigger, optional `by` and `opens`) and a layout of fits and nested regions. A screen identity is the
   slice-declaration address `Screen` (`SemanticKind.Screen`) resolved through the identity catalog.
2. **Admission is opt-in.** Binding keeps screens deferred, so a default compilation keeps its version, bytes and
   revision and no consumer has to read v42 it did not ask for. `ExecutableScreens.Admit` admits them, and so do
   `screenplay export --screens` and the MCP `read-workspace` view `executable-model` with `screens: true`.
3. **References are resolved, never dropped.** Data, actions and navigation resolve in the declaring slice's scope;
   a reference that is not a query, command or screen of the executable model refuses admission with a reason.
4. **The neutral contract is the ESM member; Scene stays outside.** Templates, component properties, toolbars and
   outlets remain in the `screenplay-ui/1` contract's `scene` layer. `UiContractBuilder.For(ExecutableSemanticModel)`
   reads the neutral contract from an ESM v42 model alone, and it equals the one read from the sources.
5. **Screens have no execution meaning.** The reference evaluator does not run them; the commands and queries they
   reference execute through their slices.

## Consequences

- Point 2 of decision 0147 and its rejected alternative "Admit screens as ESM members" are amended: screens stay
  deferred by default and are admitted on request.
- A consumer that reads v42 must accept `screens` on a slice; a consumer that does not opt in never sees v42 from
  screens alone.
- Behavior wiring beyond navigation and command execution (refresh, set state, notify, dialogs) is not in v42.

## Downgrade path

Admission is opt-in and additive. Removing it removes the v42 member, the CLI flag and the MCP argument; default
compilations, their bytes and revisions are unchanged.
