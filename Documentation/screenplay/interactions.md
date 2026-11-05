---
title: "Screenplay interactions: respond to user actions"
description: "Screenplay interactions connect gestures and signals to action sequences, such as opening a dialog, submitting a command and refreshing a query."
---

Screenplay interactions describe what happens when a user acts or a signal arrives. Use an `on` block to open a dialog, execute a command or refresh displayed data. Keep it beside the element it affects, or name a reusable behavior when several elements need the same response.

This screen refreshes its invoices on entry and opens the activated invoice's details on a double click:

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

An interaction connects a trigger to one or more actions. A behavior groups those connections:

| Word | What it is |
| --- | --- |
| **Interaction trigger** | What starts it — the `on <thing>` clause. |
| **Action** | What happens — `execute`, `navigate to`, `open dialog`, `refresh`, and the rest of a closed set. |
| **Behavior** | A bundle of trigger-to-action bindings, attached to something. |

For one labeled button that selects a command from the current item's state, use a [guarded screen action](screens.md#one-action-several-commands). For a gesture that chooses a whole action list, use block-form alternatives below.

## Write it inline, or name it

For a response used in one place, write the trigger and action directly on the element:

```screenplay
button
  on click
    execute RegisterInvoice
```

The inline `on` block is an anonymous behavior attached to the button. When several controls need confirmation before executing a command, declare that behavior once and pass each control's command and message through `uses`:

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

In `ConfirmThenExecute`, confirmation must succeed before `execute command` runs. The `uses` site supplies the command and localized message, so the behavior can be reused without hard-coding either one. Named and inline behaviors use the same resolution, printing and diagnostic rules.

## Interaction triggers

An interaction trigger is never declared. It exists only as the `on` clause of a binding.

| Trigger | Occurs when | Carries |
| --- | --- | --- |
| `on click` | the element is activated — clicked, tapped, or confirmed from the keyboard | the element's data item, if any |
| `on double click` | it is activated twice | the element's data item |
| `on select` | selection changes in an items control | the selected item |
| `on submit` | a form is submitted and passes the modeled validation | the form's values |
| `on change` | a bound value changes, or the user changes an item of a component that presents items, such as moving a card on a board | the old and new value; for an item, the changed item and the new value |
| `on load` / `on unload` | the element or screen becomes live, or is torn down | route parameters |
| `on enter` / `on leave` | the screen is navigated to, or away from | route parameters |
| `on event <Event>` | a modeled domain event is observed | the event's payload |
| `on interval <n> <unit>` | a timer elapses | — |
| `on <ApplicationTrigger>` | a declared application trigger fires | that trigger's declared payload |

Use `on event` to respond to a modeled domain event, or `on <ApplicationTrigger>` for a signal declared with `trigger <Name>`. That application trigger has a payload shape and can also be consumed by a `reaction ... when`; see [Triggers](triggers.md). You do not declare the interaction's `on` clause separately.

Built-in names such as `click`, `submit` and `enter` are reserved only inside an `on` clause. They remain ordinary identifiers elsewhere, but an application trigger cannot use one of those names: `on <Name>` would resolve to the built-in interaction instead of the declared trigger.

## Choose an action list by item state

`on click`, `on double click`, `on select` and `on change` can choose one whole action list:

```screenplay
table Invoices
  column status
  on double click
    when item.status == "draft"
      open dialog InvoiceDetails
        with invoiceId from item.invoiceId
    otherwise
      notify info "Only draft invoices can be edited"
```

Use exactly the [guarded action condition grammar](screens.md#one-action-several-commands): `item.<path>` compared with a literal, combined with `and`, `or` and parentheses. Each `when` and the optional final `otherwise` contains a non-empty indented list. At least one `when` is required. Plain actions and `where` cannot mix with alternatives. The one-line `when … execute …` spelling belongs to labeled actions, not interactions.

The first matching branch runs once against the rendered item when the gesture occurs: no refetch first, no re-evaluation while the list runs. No match runs the fallback if present, otherwise nothing. No subject runs nothing, **including fallback**. Failure, denial or unavailable commands never fall through; commands still enforce their own rules. This differs from a labeled action's click-time refresh check because the interaction displays no choice beforehand.

A table's click or double click uses the activated row; `select` uses the newly selected item; `change` uses the item that changed (see [Bind an item change to a command](#bind-an-item-change-to-a-command)). Clearing selection runs nothing. Other elements use the nearest enclosing container's single item or selected row, with the same sibling precedence as guarded actions. Named behaviors are checked at each `uses` site. A component's context binding or `selectedItem` does not supply a subject; unresolved or equally near data subjects produce PLAY0346.

`on submit` has no alternatives: `item` means populated data, not submitted form values. Event, interval and application triggers also have no alternatives. The one-line `on row-click navigate to … by …` stays unchanged.

Opaque `where` is deprecated **only** on click, double click and select (PLAY0564); a `change` keeps it. A strict item condition produces a warning and a C# typed repair to one `when` block with no fallback; repairs require individual review and refuse comment loss. Other guard text produces Information without a repair. Event/change/application `where` remains opaque and unchanged. TypeScript reports the same diagnostics but does not offer these repairs.

## Bind an item change to a command

Use `on change` on an editable items component when a user action should issue a command. It carries the changed item, just as `on select` carries the selected item. In this board, moving a card updates the item's `status` for branch selection, and `item.talkId` supplies the command's identity:

```screenplay
screen ReviewBoard
  data TalkSummary[] via query AllTalks

  component Cratis.Blueprint.Default.kanbanBoard talkBoard
    property columns
      item
        id    = "submitted"
        label = "Submitted"
      item
        id    = "accepted"
        label = "Accepted"
      item
        id    = "withdrawn"
        label = "Withdrawn"
    property cards from query AllTalks
    property cardId = "talkId"
    property cardColumn = "status"
    on change
      when item.status == "accepted"
        execute AcceptTalk
          with talkId from item.talkId
      when item.status == "withdrawn"
        execute WithdrawTalk
          with talkId from item.talkId
      otherwise
        notify warning "A talk cannot move back to Submitted"
```

What a change binds:

| Binding | Means |
| --- | --- |
| `item` | the item that changed: its row from the component's query, with the property the user changed (here `status`, named by `cardColumn`) already holding the new value. `item.talkId` and `item.status` read the row's own fields, so the command's inputs are bound to the data and not to the widget |
| `value` | the new value of the changed property, such as the column the card was dropped in |
| `previous` | the value it had before |

A command that takes the new value as an input binds it the same way, so one command can serve every column:

```screenplay
on change
  execute MoveTalk
    with talkId from item.talkId
    with status from value
```

The command still enforces its constraints and authorization. A component implementing this contract must not apply a refused move: after the command answers, it shows the data again and runs `on failure` as for any `execute`. Only a component that lets the user change items reports an item change; a read-only component raises none.

## Actions

Actions use a fixed vocabulary. Where an action names a command, screen, query or trigger, the compiler checks that reference against the document and reports an unresolved name.

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

Do not attach `on success` to `navigate`, `notify`, `set` or `close dialog`. Those actions have no outcome to branch on, so the compiler reports the continuation as an error.

Writing actions *after* an unconditional `navigate` is reported too: the screen they were written for is already gone.

## Returned values in continuations

After a command succeeds, you can pass its response to the next action by name. A scalar `returns invoiceId` exposes `invoiceId`; a `returns` block exposes its field names. Those names are available in the form's one-line or block `on submit`, and in `on success` beneath `execute <Command>`.

A bare value in `with … from …`, `set … to …`, or an unquoted `notify` / `confirm` message resolves to the nearest successful command response that supplies that name. A form field or behavior parameter with the same name keeps its existing meaning. Dotted paths, `$`-rooted bindings, quoted literals and other opaque text are unchanged. Action declaration names and state-write targets are not response operands.

A nested command's `on success` shadows matching outer response names only inside that subtree; siblings restore the outer scope. Its `on failure` cannot use its returned names (`PLAY0685`), but can still use an outer command's successful response. Continuations of `refresh`, `confirm`, `open dialog` and `raise` inherit their surrounding value scope rather than creating a response. A parameterized `execute command` inside a named behavior has no known response shape at declaration time.

Monaco and VS Code complete returned names after `from` and the form sugar's `by`, and hover shows the field type. This is an authoring contract outside the ESM: renderers must bind response values. Screen `action <Command>` navigation sugar does not gain a response scope.

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

Attachments accumulate. If both a template and an element attach a behavior, both run; the element's attachment does not replace the template's. They run outermost first unless a behavior declares its own `order`:

```screenplay
behavior ConfirmDestructive
  order 10
  ...
```

Use [contributions](contributions.md) to collect content in shared slots; use behavior attachments to describe responses to gestures and signals.

In a [folder of files](folders.md), a module's or a feature's attachments may be written in any file that names it, and they accumulate the same way. An attachment identical to one another file already makes is ignored with a warning rather than run twice; expansion writes them back into the owner's own file.

## What the compiler checks

- Every operand resolves, or is reported. Unknown commands, screens, queries, dialog templates, events, triggers and behaviors are warnings, the way every other reference in the document is — a name may still resolve to something outside it, and what matters is that the gap stays visible.
- A `uses` site must supply exactly the parameters the behavior declares. A missing or unknown argument is an error.
- Continuations must sit on actions that can fail, and `on result` only on `open dialog`.
- Nesting is capped. Continuations nest arbitrarily in principle; past sixteen levels the compiler asks for a named behavior instead of reporting a stack overflow.

The full list, with codes, is in [Diagnostics](diagnostics.md).

## What this is not

Use interactions to connect modeled actions, not to embed general-purpose scripts. Conditions use the existing expression grammar, and actions come from a fixed, resolvable set. Keep validation on the command so the UI can surface its rules without restating them. Animation, visual states, drag-and-drop and keyboard maps are not part of this vocabulary.

The event model board draws the containing table or screen as a static prototype; it does not execute gesture action lists or invent a button for each branch. Runtime support belongs to renderers, which must reject alternatives they cannot realize.

The backend executable semantic model (ESM) v1 profile does not execute interaction constructs, just as it does not execute `screen`, `layout` or `ui profile`; it reports `PLAY0269`. The syntax tree and printer preserve the constructs for downstream consumers. A rendering target must report a construct it cannot realize.

## See also

- [Triggers](triggers.md) — application triggers, and how an interaction reaches them.
- [Screens](screens.md) — what a behavior attaches to.
- [Layouts and templates](templates.md) — the shapes a screen fills.
- [Glossary](glossary.md) — one line per term.
