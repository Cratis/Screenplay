---
id: 0142
title: Every region content is placed in is a slot, with an optional role (Screenplay 6)
status: proposed
stage: designed
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Screenplay 5 used the word `outlet` for a region content is placed in: a component's `outlet <name>`, a layout or template's
`outlet <name>`, a navigation's `outlet <name>` and a contribution's `destination outlet <name>`. Plain lines of a layout or
template already declared slots, and Scene already calls the destination kind `Slot`. Cratis/Screenplay#1178 (epic
Cratis/StudioIssues#1327) gives `outlet` a new meaning - a list of items a template offers - so the region needs one name, and
designers and renderers need to know what kind of content a region holds.

## Decision

1. **A region content is placed in is a slot.** `slot <name>` replaces `outlet <name>` in a component, a layout, a screen
   template, a dialog template and under `navigate to`; `destination slot <name>` replaces `destination outlet <name>`. The
   syntax tree follows: `ComponentSlotSyntax`, `TemplateSlotSyntax` (`recursiveSlots`), `ScreenNavigateSyntax.slot` and
   `ContributionDestinationKind.Slot`.
2. **No deprecation period.** The Screenplay 5 forms are errors with their own codes - `PLAY0890` (component), `PLAY0891`
   (destination), `PLAY0892` (navigation) and `PLAY0893` (layout or template) - whose message names the replacement and
   `screenplay migrate --from 5 --to 6`. The parser still reads the old form as the slot it means, so one rename does not
   cascade into reference errors.
3. **A slot line may carry a role and modifiers**, after the name and in this order, each optional:
   `<name> [page|pane|panel] [routed [by <param>]] [primary] [optional] [contributes <Point>]`. They are syntax and
   printer facts for renderers and designers; this decision adds no check on them.
4. **No executable-model version.** Layouts, templates and screens are deferred from the ESM (`PLAY0269` information), so
   slots never reach its bytes; a new language/semantic version would select nothing. The release is the product major,
   Screenplay 6.

## Consequences

- The C# and TypeScript compilers, the AST schema and goldens, Monaco, the VS Code grammar, the samples and the documentation
  use `slot`. The TypeScript compiler reports `PLAY0890` and `PLAY0892`; layouts, templates and contributions are opaque to
  it, so `PLAY0891` and `PLAY0893` are C#-only.
- A slot line whose first word is a metadata keyword now reads as a slot when its next word is a role: `content page` is the
  slot `content` with the role `page`, not "the content slot is `page`".
- Later phases of #1178 reuse `outlet` for item lists (`outlet <name> of <kind>`) and convert `navigation topbar|sidebar`,
  `contributes` and feature `toolbar`.
