---
id: 0131
title: Selection-bound panes use native named selection sources
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-30
decider: woksin (ruling and refinement on #684)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[#684](https://github.com/Cratis/Screenplay/issues/684) asks for selection-bound panes and selection-required toolbar items on the same screen. woksin's analysis on the issue (2026-10-29) records that action toolbar items already accept `for item`, that [0110](0110-explicit-action-subjects-and-selection.md) separates selection availability from hiding, and that package components can already expose a selection into a detail outlet. What remains is the native master-detail increment: a native table has no explicit selection output, and there is no package-independent pane that consumes one. The analysis names its dependencies as decision 0110, a pane-semantics decision, and coordinated Stage/Scene support or a typed refusal, with a generator follow-up in Cratis/Arc#3049.

## Decision

This record is the pane-semantics decision that the analysis names. It writes down the owner's ruling and its refinement and adds no design beyond them.

The ruling, quoted verbatim from [woksin on #684, 2026-10-30](https://github.com/Cratis/Screenplay/issues/684#issuecomment-6528147231):

> **Ruling (woksin, 30 Oct 2026):** selection-bound panes use native named selection sources.
>
> - A pane names the table that supplies it and binds one selected row to a typed embedded target.
> - The pane clears when the selection clears, so it never shows stale details.
> - Navigation and dialog toolbar items require a selection only when they ask for one. Without a selection they are disabled, not hidden.
> - The action rules of decision 0110 stay unchanged. There is no new ESM version.
>
> Next step: a decision record, then the implementation.

The refinement, quoted verbatim from [woksin on #684, 2026-10-30](https://github.com/Cratis/Screenplay/issues/684#issuecomment-6528266880):

> **Ruling refined after verification:** decision 0110's rules for availability without a selection still apply (0110 lines 75–77). Ordinary item actions are disabled, and guarded actions stay hidden. Renderers must support selection sources or refuse them with a typed reason.

So:

1. A pane names the table that supplies it, and binds one selected row to a typed embedded target.
2. When the selection clears, the pane clears.
3. Navigation and dialog toolbar items require a selection only when they ask for one; without a selection they are disabled, not hidden.
4. 0110 is unchanged, including its rule for availability without a selection (item 3 of its decision): ordinary item actions are disabled, and guarded actions stay hidden.
5. There is no new ESM version.
6. Renderers support selection sources or refuse them with a typed reason.

## Timeline and scope

In scope, per the analysis on the issue: screen syntax and parsing in both compilers, composition and reference validation, typed bindings, printers, syntax serialization, MCP, editors, documentation and samples. Out of scope: renderer-specific component conventions, which belong in each target (Stage, Scene), and the generator follow-up Cratis/Arc#3049.

## Verification

Done when both compilers parse and round-trip a pane bound to a named table selection, reject ambiguous or missing sources and invalid bindings, keep existing package outlets working, and renderers either cover selection changes, cleared selections, disabled item actions and an unchanged active screen, or refuse selection sources with a typed reason.

## Consequences

Master-detail screens can be modelled natively, without a package component. Toolbar availability follows 0110 for actions and the same disabled-not-hidden rule for navigation and dialog items that ask for a selection. Renderers that cannot support selection sources must say so rather than render a stale or unbound pane.
