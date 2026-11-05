---
title: "Screenplay contributions: add feature-owned navigation"
description: "Use contribute to in Screenplay when features need to add entries to a shared slot. Each feature declares its own link, label and order near its screens."
---

# Contributions

Use Screenplay contributions when several features need to add entries to a shared navigation area without maintaining one central list. The layout or template declares a named contribution point, and each feature declares the link it owns with `contribute to`.

A normal slot takes one block of content from its parent. A contribution point lets that slot collect entries from several modules or features, keeping each entry near the screens it links to.

## Syntax

First mark the receiving slot with `contributes <ContributionPoint>`. Slots on an application [`layout`](templates.md), `screen template` or `dialog template` can accept contributions:

```screenplay
layout <Name>
  <slot-name> contributes <ContributionPoint>
  <slot-name>
```

```screenplay
module <Name>
  screen template <TemplateName>
    <slot-name> contributes <ContributionPoint>
    <slot-name>
```

Then add an entry on a module or feature. The entry names the contribution point, not the layout or template that receives it:

```screenplay
contribute to <ContributionPoint>
  navigate to <Screen> [by <param>]
  label "<text>"
  order <number>
```

- `<slot-name> contributes <ContributionPoint>` - marks a slot as the target for contributions under that name. A slot without `contributes` behaves exactly as it always has.
- `contribute to <ContributionPoint>` - one contributed item. Declared directly on a `module` (alongside `screen template`, `dialog template`, `form` and `feature`) or on a `feature` at any nesting depth. `navigate`, `label` and `order` are all optional. `label` accepts an unquoted `$strings.<key>` token in place of a literal - see [Internationalization](internationalization.md).

## Example

The InvoiceManagement feature owns the Invoices link. Its module's `AppShell` template receives that entry in `navbar`, so adding another feature's link does not require editing the template:

```screenplay
module Invoicing
  screen template AppShell
    navbar contributes Navigation
    main

  feature InvoiceManagement
    slice StateView InvoiceList
      screen InvoiceList

    contribute to Navigation
      navigate to InvoiceList
      label "Invoices"
      order 10
```

`contribute to Navigation` adds one flat item to a slot. For the application's topbar and sidebar as a whole - hierarchy, targets, rendering slots and module level inherit, extend and override - use the [`navigation` regions](navigation.md).

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
  destination outlet main
```

- `id`, `icon`, `presentation` and `group` take an identifier or a quoted string, kept exactly as written.
- `destination outlet <name>` opens the item in a named outlet, `destination dialog <DialogTemplate>` opens a dialog, and `destination external "<route>"` leaves the application. These map to Scene's `DestinationKind` `Outlet`, `Dialog` and `External`.
- An outlet or dialog template nothing declares is reported as `PLAY0654`.
- A `navigate to` inside a contribution accepts the same `route`, `outlet` and `parameter` lines as a screen's navigation.

## How a contribution resolves

The compiler first checks the contribution's own module for a template slot with the matching `contributes` name. A local match keeps the contribution within that module, including contributions declared in nested features. Multiple matching slots in the module produce an ambiguity warning.

If there is no local match, the compiler checks matching points outside the module, including application layouts. Other modules and layouts are checked together, not as separate precedence levels. Competing matches across modules, or between a module and a layout, produce an ambiguity warning.

Put an application-wide point such as `Navigation` on the layout when all modules should contribute to the shared shell. Put it on a module's template when that module should collect its own entries. Features do not declare templates; a feature that needs a separate structure uses a template declared on its module. See [Layouts and templates](templates.md).

An unresolved point also produces a warning because it may resolve outside the document. Review unresolved and ambiguous points before relying on the resulting navigation.

```screenplay
module Payments
  contribute to Navigation
    navigate to InvoiceList
```

If `Payments` has no matching point of its own and two other modules each declare a `contributes Navigation` slot, the compiler reports ambiguity. The contribution does not select one of those modules.

## Route arguments

Use `navigate to <Screen> by <param>` to pass a typed value to the destination, not to interpolate a route string. It uses the same navigation binding as a screen's `action` and `on row-click`, with compiler name checks. The widget consuming the contribution point decides how that binding becomes a URL, query string or native deep link.

## Scope

`Navigation` is a contribution point name, not a reserved destination. You can name another point, but entries still use the supported directives described above; the name does not define a new property schema. Grouped ordering beyond a flat list and an explicit override for contribution-point resolution are not supported.

For a topbar or sidebar with nested destinations and module inheritance, continue with [navigation regions](navigation.md). For a screen that fills one slot directly, use [layouts and templates](templates.md).
