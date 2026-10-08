---
id: 0029
title: Select guarded screen commands in authored order without authorization fall-through
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-07
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Parsing/**
  - Source/Screenplay/**
  - Documentation/screenplay/screens.md
  - Samples/**
---

## Context

One labeled screen action can offer different commands as an item's state changes. Renderers need one portable selection rule; choosing a different command after an authorization denial would silently change the user's intent. Screens are not admitted to the executable semantic model (ESM).

## Decision

> **2026-10-07 — missing values and command availability clarification.** A condition over a missing or null `item.` field does not match: comparisons are false, except `== null`, which matches only null. Missing is not an authored null value. When the selected alternative's command is unavailable to the caller, the action is presented as unavailable, as [#335](https://github.com/Cratis/Screenplay/issues/335) defines once admitted; it is never replaced by a later alternative. Studio's importer currently drops screens ([StudioIssues#522](https://github.com/Cratis/StudioIssues/issues/522)); [StudioIssues#534](https://github.com/Cratis/StudioIssues/issues/534) tracks preserving guarded actions on import.

A label-headed guarded action selects the first matching `when … execute` alternative in authored order, re-evaluated whenever its data changes. Its subject is the nearest enclosing container's single data item or selected collection row. No subject always hides the action. No match hides it unless `otherwise execute` supplies a fallback; `otherwise hidden` and an omitted fallback are equivalent. Authorization never falls through to another alternative, and the chosen command still enforces its own authorization, validation and constraints. Inputs resolve in this order: explicit `with` bindings, same-name subject fields, the command's declared form, renderer input. Navigation follows successful execution. A click executes the choice shown to the user; if a click-time check changes that choice, the renderer refreshes rather than executing the new command.

## Options considered

- First match, with optional hidden fallback: chosen because it preserves authored priority and permits intentional gaps.
- Fall through on authorization: rejected because it can execute a command the user did not choose.
- Require exhaustive conditions or an explicit fallback: rejected because open state vocabularies cannot be exhaustively checked and hidden is a useful default.
- Require every input mapping: rejected in favor of the existing same-name item convention.

## Default if unanswered

Only separate unguarded buttons remain available. Authors duplicate labels and renderers invent incompatible visibility and selection rules.

## Timeline and scope

Applies from acceptance until superseded. In scope: guarded screen-action syntax and the contract for Stage and other renderers. Out of scope: renderer implementation, guarded `on` bindings, persona diagnostics, and ESM admission or version allocation (decisions 0004 and 0025 remain unchanged).

## Verification

**Done when:** the compiler preserves ordered alternatives, fallback and input bindings; consumers draw one labeled action and renderers implement the selection and authorization contract without silently ignoring it.

**Verify by:** run parser, printer, validator, syntax conformance and event-model board specifications. In each renderer, exercise data changes, absent selection, fallback, click-time drift and denial without fall-through. A binder specification compares ESM bytes and revision with and without a guarded action.

## Consequences

Authors describe one user decision without duplicating buttons. Older renderers must reject the new directive kind rather than render it as an empty plain action. Compile-time checks do not prove a downstream renderer implements this contract.

## Status notes

**2026-10-07 — accepted.** Accepted by Sindre Alstad Wilting on 2026-10-07 after reviewing the amended text. Renderer verification remains downstream work; the record therefore stays at `stage: none`.
