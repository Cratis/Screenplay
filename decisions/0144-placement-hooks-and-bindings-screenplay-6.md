---
id: 0144
title: A slice's screen says where it fits, and a hook opens or runs it (Screenplay 6)
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

Decisions 0142 and 0143 gave templates slots with roles and outlets with items (Cratis/Screenplay#1178). What was still
missing is the link between them and a slice: which places a slice's screen suits, how a button that names a slice opens
it, how a State Change whose inputs all come from the selected row runs without a form, which form fields are pickers,
and how a slot shows the screen for what another element has selected. Without these the board and a renderer guess,
and every State Change slice needed a screen even when nothing on it asked for input.

## Decision

1. **`fits <role>, …` on a slice's screen**, in order of preference: `dialog`, `drawer`, `pane`, `panel`, `page`. Without
   it a screen fits anywhere. A malformed line is `PLAY0906`.
2. **A button that names a slice is a hook.** On the button line: `for item|for screen` and
   `opens dialog|drawer|in slot <name>`; without `opens` it opens the screen in its first fit the page has free. Under it:
   `runs` (issue the command directly, without a form), `enabled when <condition>` in the guarded-action condition grammar,
   and `confirm`. A toolbar `item` takes `enabled when` too.
3. **A State Change slice may have no screen when a hook can run it.** A slice with no screen fits nothing: a hook that
   names no place runs it, and one that cannot - an input needs a form - or that names a place to open it is `PLAY0908`.
   `runs` on a slice whose inputs do not all resolve without a form is `PLAY0907`; the identifier resolves from the item
   or screen the hook acts on, generated values from the command.
4. **`field <property>` takes `choose from query <Query> show <field>` under it**, and a picker is inferred when the
   property's concept is the key of a read model exactly one list query returns. `PLAY0913` checks the line.
5. **`show <Screen> by <param> from <element>.selection` in a template slot.** `PLAY0914` checks the form, the slot, the
   names and the key.
6. **More checks**: `PLAY0909`-`PLAY0911` for `opens in slot` (undeclared, already filled, role not fitted), `PLAY0912`
   (warning) for a component bound to nothing, `PLAY0915` for `enabled when` reading `item` on a hook that acts `for screen`.
7. **`PLAY0533` is on by default.** Every model check reports a State Change command nothing issues, and a hook counts as
   issuing its slice's commands when it runs or opens a slice with a screen. It is no longer selected by
   `--check input-surfaces`.
8. **No executable-model version**: these are screen authoring syntax and deferred from the ESM, as in 0142 and 0143.

## Consequences

- `ScreenSyntax.Fits`, `ToolbarButtonSyntax.Subject|Opens|OpensSlot|EnabledWhen`, `ToolbarItemSyntax.EnabledWhen`,
  `FormFieldSyntax.Choices` and `ScreenShowSyntax` join the syntax tree; the canonical JSON omits them while empty.
- The TypeScript compiler reads `fits`, `show` and the hook parts and reports `PLAY0906`, the malformed `show` of
  `PLAY0914` and `PLAY0760`; forms stay opaque to it, and the reference checks are C#-only.
- Models that relied on an unissued State Change now see `PLAY0533` without asking for it; samples, the canonical corpus and
  fixtures give such slices a screen or a hook.
