---
title: "Screenplay contributions: add items to any outlet"
description: "Use contribute to in Screenplay when a module or feature adds items to an outlet a layout or template declares. Each feature declares its own entry near its screens."
---

# Contributions

Use Screenplay contributions when several modules or features need to add entries to a shared list without maintaining one central list. The layout or template declares an [outlet](outlets.md), and each module or feature declares what it adds with `contribute to <outlet>`.

A lower level's `outlet <name> extends` changes the list for the screens beneath it. A contribution adds to the outlet wherever it appears, so the entry stays next to the screens it belongs to.

## Syntax

The layout or template declares the outlet:

```screenplay
layout <Name>
  <slot-name>
  outlet <OutletName> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot-name>]
```

A module or feature names the outlet, not the layout or template that declares it:

```screenplay
contribute to <OutletName>
  navigate to <Screen> [by <param>]
  label "<text>"
  order <number>
  <item lines of the outlet's kind>
```

- `contribute to <OutletName>` - declared directly on a `module` or on a `feature` at any nesting depth. It works for any outlet, from any module or feature.
- `navigate`, `label` and `order` describe one navigation entry; all are optional. `label` accepts an unquoted `$strings.<key>` token in place of a literal - see [Internationalization](internationalization.md).
- Item lines - `button`, `group`, `filter`, `tab`, `stat` or `item` - add items of the outlet's kind, after the items it already has. An item of another kind is `PLAY0900`.

## Example

The InvoiceManagement feature owns the Invoices link, and the Payments feature adds a tab to the invoice page without editing the template:

```screenplay
layout AppShell
  category Shell
  navbar panel
  main page routed primary
  outlet Navigation of navigation in navbar

module Invoicing
  screen template InvoicePage
    category Detail
    fits slot main
    header
    detail pane
    outlet detailTabs of tabs in detail

  feature InvoiceManagement
    slice StateView InvoiceList
      screen InvoiceList

    contribute to Navigation
      navigate to InvoiceList
      label "Invoices"
      order 10

  feature Payments
    slice StateView InvoicePayments
      screen InvoicePayments

    contribute to detailTabs
      tab payments title "Payments" shows InvoicePayments
```

For the application's topbar and sidebar as a whole - hierarchy, targets, rendering slots and module level inherit, extend and override - use [navigation outlets](navigation.md).

## Navigation item metadata

A navigation entry can carry the rest of Scene's `NavigationItem`: a stable id, an icon, a presentation hint, a group and where it opens.

```screenplay
contribute to Navigation
  navigate to InvoiceList
  label "Invoices"
  order 10
  id "nav:invoices"
  icon receipt
  presentation primary
  group "Billing"
  destination slot main
```

- `id`, `icon`, `presentation` and `group` take an identifier or a quoted string, kept exactly as written.
- `destination slot <name>` opens the item in a named slot, `destination dialog <DialogTemplate>` opens a dialog, and `destination external "<route>"` leaves the application. These map to Scene's `DestinationKind` `Slot`, `Dialog` and `External`.
- A slot or dialog template nothing declares is reported as `PLAY0654`.
- Screenplay 5 wrote `destination outlet <name>`; that form is now an error (`PLAY0891`) whose message names `screenplay migrate --from 5 --to 6`.
- A `navigate to` inside a contribution accepts the same `route`, `slot` and `parameter` lines as a screen's navigation.

## How a contribution resolves

The compiler first checks the contribution's own module for a template that declares the outlet. A local match keeps the contribution within that module, including contributions declared in nested features. Multiple matching templates in the module produce an ambiguity warning.

If there is no local match, the compiler checks outlets outside the module, including application layouts. Other modules and layouts are checked together, not as separate precedence levels. Competing matches across modules, or between a module and a layout, produce an ambiguity warning.

Declare an application-wide outlet such as `Navigation` on the layout when all modules should contribute to the shared shell. Declare it on a module's template when that module should collect its own entries. Features do not declare templates; a feature that needs a separate structure uses a template declared on its module. See [Layouts and templates](templates.md).

A contribution to an outlet nothing declares is an error (`PLAY0903`). A document that uses a blueprint is exempt, since the blueprint's templates may declare the outlet.

## Route arguments

Use `navigate to <Screen> by <param>` to pass a typed value to the destination, not to interpolate a route string. It uses the same navigation binding as a screen's `action` and `on row-click`, with compiler name checks. The widget consuming the outlet decides how that binding becomes a URL, query string or native deep link.

## From Screenplay 5

Screenplay 5 marked the receiving slot with `<slot> contributes <Point>`. That form is an error since Screenplay 6 (`PLAY0895`); write `outlet <Point> of navigation in <slot>`, or run `screenplay migrate --from 5 --to 6`.

For a topbar or sidebar with nested destinations and module inheritance, continue with [navigation outlets](navigation.md). For every outlet kind, see [Outlets](outlets.md). For a screen that fills one slot directly, use [layouts and templates](templates.md).
