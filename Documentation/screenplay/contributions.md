# Contributions

A NavBar needs its entries from wherever they are declared - a dozen features scattered across a dozen modules, each adding the one link it owns. A slot on its own is the wrong tool for this: a slot is one parent placing one block of content into one region, not many children contributing into a shared collection. `contribute to` is the many-to-one counterpart.

## Syntax

Slots live on the application's [`layout`](templates.md) and on every `screen template` and `dialog template`, and any of them can declare it accepts contributions under a name:

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

Anything anywhere in the module/feature tree contributes to it, without knowing or caring who is listening:

```screenplay
contribute to <ContributionPoint>
  navigate to <Screen> [by <param>]
  label "<text>"
  order <number>
```

- `<slot-name> contributes <ContributionPoint>` - marks a slot as the target for contributions under that name. A slot without `contributes` behaves exactly as it always has.
- `contribute to <ContributionPoint>` - one contributed item. Declared directly on a `module` (alongside `screen template`, `dialog template`, `form` and `feature`) or on a `feature` at any nesting depth. `navigate`, `label` and `order` are all optional. `label` accepts an unquoted `$strings.<key>` token in place of a literal - see [Internationalization](internationalization.md).

## Example

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

## How a contribution resolves

A contribution attaches to the **nearest enclosing structure that declares a matching contribution point**, walking outward the same way a bare name already resolves elsewhere in the document - just through the module/feature containment tree rather than by declaration scope. Concretely: a contribution first looks for a `contributes <ContributionPoint>` slot among its **own module's** templates. A module with its own matching slot stops contributions inside it from bubbling any further - that module owns the point. Only when the module has no matching slot does the search continue outward, across every other module in the document, and finally to the application's own `layout`s, which belong to no module and are the outermost shell of all.

This gives three real tiers of nesting: **application-wide** (the `layout` declares the point, so every module in the document contributes into the one shell - the natural home of `Navigation`), **cross-module** (a contribution resolves to some other module's template because its own module has none that matches) and **module-level** (a module's own template claims every contribution inside it, however deeply nested in that module's features). A fourth tier - a feature declaring its own sub-shell that only its own slices contribute to - would need a template scoped to a feature rather than a module, which does not exist yet.

Unresolved and ambiguous contribution points are warnings, the same as every other reference in the document: a name may still resolve to something outside the document, and the point is that the gap stays visible.

```screenplay
module Payments
  contribute to Navigation
    navigate to InvoiceList
```

If `Payments` has no template of its own, and two *other* modules each declare a `contributes Navigation` slot, the contribution is ambiguous - both are equally near, and nothing in the document says which one it means.

## Route arguments

`navigate to <Screen> by <param>` is not string interpolation. It reuses the exact typed `navigate` binding a screen's `action` and `on row-click` already use, checked by the compiler the same way any other bare name is. Turning that into a URL, a query string, or a native deep link is a rendering concern the widget consuming the contribution point owns - not something the language decides.

## Scope

`Navigation` is the mechanism's first user, not the whole of it - a widget declares whatever contribution point name and shape it needs, and `contribute to <Name>` opens a property bag shaped by that name. This version of the mechanism ships with `navigate`, `label` and `order`; ordering by a `group` beyond a flat list, and an explicit override for the rare case where nearest-enclosing is not the contribution point a contributor means, are intentionally left for a later iteration.
