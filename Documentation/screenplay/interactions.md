---
title: Interactions
description: What happens when a user does something — behaviors, the on clauses that start them, the closed set of actions they run, and the continuations that chain those actions together.
---

A document can say what a screen looks like. This page is about the other half: what happens when someone clicks something.

```screenplay
screen InvoiceList
  on enter
    refresh Invoices

  table Invoices
    column invoiceId
    on double click
      open dialog InvoiceDetails
        with invoiceId from item.invoiceId
```

Three words carry the whole idea:

| Word | What it is |
| --- | --- |
| **Interaction trigger** | What starts it — the `on <thing>` clause. |
| **Action** | What happens — `execute`, `navigate to`, `open dialog`, `refresh`, and the rest of a closed set. |
| **Behavior** | A bundle of trigger-to-action bindings, attached to something. |

## Write it inline, or name it

Most interaction is one trigger and one action, written where it belongs:

```screenplay
button
  on click
    execute RegisterInvoice
```

That inline `on` block **is** a behavior — an anonymous one, attached to the button. When the same wiring is wanted in several places, give it a name, declare parameters, and attach it with `uses`:

```screenplay
behavior ConfirmThenExecute
  parameter command
  parameter message

  on click
    confirm message
      on success
        execute command

module Invoicing
  feature InvoiceManagement
    slice StateChange CancelInvoice
      screen CancelInvoice
        uses ConfirmThenExecute
          command CancelInvoice
          message $strings.confirmCancel
```

There is one construct behind both forms, so a named behavior and an inline block resolve, print and diagnose identically. The only difference is the name.

## Interaction triggers

An interaction trigger is never declared. It exists only as the `on` clause of a binding.

| Trigger | Occurs when | Carries |
| --- | --- | --- |
| `on click` | the element is activated — clicked, tapped, or confirmed from the keyboard | the element's data item, if any |
| `on double click` | it is activated twice | the element's data item |
| `on select` | selection changes in an items control | the selected item |
| `on submit` | a form is submitted and passes the modeled validation | the form's values |
| `on change` | a bound value changes | the old and new value |
| `on load` / `on unload` | the element or screen becomes live, or is torn down | route parameters |
| `on enter` / `on leave` | the screen is navigated to, or away from | route parameters |
| `on event <Event>` | a modeled domain event is observed | the event's payload |
| `on interval <n> <unit>` | a timer elapses | — |
| `on <ApplicationTrigger>` | a declared application trigger fires | that trigger's declared payload |

The last two rows are the bridge to the backend vocabulary, and the reason this page distinguishes two kinds of trigger.

> **`trigger` still means one thing.** A declared `trigger <Name>` is an *application trigger* - a signal with a payload shape that a `reaction ... when` consumes. See [Triggers](triggers.md). What starts an interaction is the anonymous `on` clause above. The built-in kinds (`click`, `submit`, `enter`, and their siblings) are reserved **inside an `on` clause only**; an identifier of the same name anywhere else in a document is just an identifier. Declaring an application trigger named after one of them is an error, because `on <Name>` would then mean the interaction and never the trigger.

## Actions

Every action names something the document declares. That is what makes an action that points at nothing a diagnostic rather than a control that quietly does nothing.

| Action | Effect | Resolves against |
| --- | --- | --- |
| `execute <Command>` | submits a modeled command | the document's commands |
| `navigate to <Screen>` | changes the active screen | the document's screens |
| `navigate back` | pops navigation history | — |
| `open dialog <DialogTemplate>` | opens a dialog over the application | the module's dialog templates |
| `close dialog` | dismisses the innermost dialog | — |
| `refresh <Query>` | re-runs a query-backed element | the document's queries |
| `set <target> to <value>` | writes screen state | the screen's declared state |
| `notify <info\|warning\|error> "<text>"` | surfaces a message | `$strings.` accepted |
| `confirm "<text>"` | gates the actions that follow on the user agreeing | — |
| `raise <ApplicationTrigger>` | fires a declared application trigger | the document's triggers |

Arguments are passed with `with <name> from <binding>`:

```screenplay
on click
  open dialog InvoiceDetails
    with invoiceId from item.invoiceId
```

A message operand may be a literal, a `$strings.` key, or the name of a parameter the attachment supplies — which is how one named behavior serves several call sites.

## Continuations

An action that has an outcome can branch on it:

```screenplay
on click
  execute RegisterInvoice
    on success
      close dialog
      refresh InvoiceList
      notify info $strings.invoiceRegistered
    on failure
      notify error $strings.registerFailed
```

Actions run in the order they are written. `confirm` short-circuits everything after it when the user declines.

| Continuation | Valid on |
| --- | --- |
| `on success`, `on failure` | `execute`, `refresh`, `confirm`, `open dialog`, `raise` — the actions that can fail |
| `on result` | `open dialog` only |

Attaching `on success` to a `navigate`, a `notify`, a `set` or a `close dialog` is an error. Those have no outcome to branch on, so the continuation could never run — and an author who wrote one expected it to.

Writing actions *after* an unconditional `navigate` is reported too: the screen they were written for is already gone.

## Where a behavior attaches

A behavior attaches at every level of the containment tree, and means the same thing at each:

| Level | Reaches |
| --- | --- |
| `layout` | every screen in the application |
| `module` | every screen in the module |
| `feature` | every screen in the feature, including nested ones |
| `screen template`, `dialog template` | every screen that uses it |
| `form` | that form |
| `screen`, `section`, slot, `table` | that element and what is inside it |

```screenplay
module Invoicing
  uses ConfirmDestructive

  screen template Workspace
    fits slot main
    content

    on enter
      refresh Invoices
```

In a form body, `on submit navigate to <Screen>` keeps its existing one-line meaning. Any other `on` is a behavior attached to the form.

Attachments are **additive**. A behavior on a template and a behavior on an element both run — the more specific one does not replace the less specific one. They run outermost first, unless a behavior declares its own `order`:

```screenplay
behavior ConfirmDestructive
  order 10
  ...
```

This is the same outward-resolving rule [contributions](contributions.md) already use.

In a [folder of files](folders.md), a module's or a feature's attachments may be written in any file that names it, and they accumulate the same way; expansion writes them back into the owner's own file.

## What the compiler checks

- Every operand resolves, or is reported. Unknown commands, screens, queries, dialog templates, events, triggers and behaviors are warnings, the way every other reference in the document is — a name may still resolve to something outside it, and what matters is that the gap stays visible.
- A `uses` site must supply exactly the parameters the behavior declares. A missing or unknown argument is an error.
- Continuations must sit on actions that can fail, and `on result` only on `open dialog`.
- Nesting is capped. Continuations nest arbitrarily in principle; past sixteen levels the compiler asks for a named behavior instead of reporting a stack overflow.

The full list, with codes, is in [Diagnostics](diagnostics.md).

## What this is not

Interaction is a closed vocabulary, deliberately. It is not a scripting language: conditions use the existing expression grammar, and actions are a fixed, resolvable set. Validation stays modeled on the command — the UI surfaces it rather than restating it. Animation, visual states, drag-and-drop and keyboard maps are not part of it.

Interaction constructs are deferred from the backend ESM v1 profile, the same way `screen`, `layout` and `ui profile` are, and report `PLAY0269`. Deferred does not mean dropped: the syntax tree, the printer and the semantic model carry every construct in full, and a target that cannot realize one reports it.

## See also

- [Triggers](triggers.md) — application triggers, and how an interaction reaches them.
- [Screens](screens.md) — what a behavior attaches to.
- [Layouts and templates](templates.md) — the shapes a screen fills.
- [Glossary](glossary.md) — one line per term.
