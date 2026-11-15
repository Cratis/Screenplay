---
id: 0163
title: A provide whose value comes from code or a file is an implementation attachment, admitted in ESM v54
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-14
decider: Einar Ingebrigtsen (direct request for Cratis/Screenplay#1307)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Decision 0161 introduced `provide`, which adds a named value to `$context`. A value can come from a projection or from code (an inline code block or a file). The code forms parsed but the binder refused the whole model with PLAY0268 (Cratis/Screenplay#1300). This record admits them (Cratis/Screenplay#1307).

## Decision

- **A code provide is an implementation attachment,** like an operation body. The model states the member name and its source; the model never describes what the code does.
- **ESM v54** admits it. The role is `ProvideValue`. The canonical form carries kind `code` and a `requirementId`. Readers and writers below v54 refuse it. A model without a code provide keeps its earlier version.
- **The reference evaluator never runs it, and a code provide can never produce allowed or denied.** The host decides availability and the evaluator treats every gate as available, so a slice gated by a model with a code provide runs its specification unchanged. Runtimes own execution of the code.
- **Files** named by a file provide are attachment files and are collected with the model's other attachments.

## Consequences

- The preview row for #1300 is removed. `Samples/Invoicing` demonstrates both forms.
- A runtime that does not implement the attachment reports it as unsupported rather than guessing a value.
- This amends decision 0161.
