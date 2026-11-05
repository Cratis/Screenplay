---
title: "Screenplay navigation regions: define shell destinations"
description: "Declare topbar and sidebar destinations on a Screenplay layout. Modules can inherit, extend or replace each region to guide users through their features."
---

# Navigation regions

Screenplay navigation regions define the topbar and sidebar destinations in the application shell. Declare shared items on a `layout`, then let each module inherit them, add its own destinations or replace a region. This keeps application-wide navigation separate from the screens that users reach through it.

Use regions to guide users to modules and features. For a link to a particular screen or record, use [screen navigation](screens.md#navigation-metadata) or an [interaction](interactions.md).

## At application level

Start with regions inside a [`layout`](templates.md), next to its slots. The layout supplies the base navigation, so these region blocks have no inheritance mode. In this example, the topbar links to Sales and Support, while the sidebar gives Sales a feature hierarchy:

```screenplay
layout AppShell
  category application
  topbar
  navigation contributes Navigation
  content
  side

  navigation topbar
    logo image "/assets/acme.svg" title "Acme"
    item sales title "Sales" icon receipt target Sales
    item support title "Support" target Support.Tickets slot content
  navigation sidebar
    item sales title "Sales" target Sales
      item orders title "Orders" target Sales.Orders
      item returns title "Returns" target Sales.Orders.Returns slot side
    item help title $strings.nav.help target Support

module Sales
  feature Orders
    feature Returns

module Support
  feature Tickets
```

- `navigation topbar` and `navigation sidebar` each appear at most once per layout. `navigation contributes Navigation` is still a slot declaration; `navigation` followed by `topbar` or `sidebar` is a region.
- `logo image "<path>" title "<text>"` is the application's logo. It is declared once, in a layout - never in a module (`PLAY0744`).
- `item <id> title "<text>" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]` is one item. The id is unique within the region (`PLAY0747`). The title is a quoted string or a `$strings.<key>` token, see [Internationalization](internationalization.md). `icon` takes an identifier or a quoted string.
- `target` names a module, or a module and a feature path (`Sales.Orders.Returns` for a nested feature). A module target goes to the module's first feature. An item with no target must have sub-items (`PLAY0748`).
- `slot` names the layout slot the target renders into. Without it the target renders into the layout's primary content slot. A slot no layout declares is `PLAY0749`.
- In the sidebar, an item may nest further `item` lines: top-level items target modules and sub-items target features. Topbar items do not nest (`PLAY0746`).
- `button <id> title "<text>" [slice <Module>.<Feature>.<Slice>] [emphasis primary|secondary|quiet] [icon <name>]` is an application-level toolbar button in `navigation topbar`. It has the grammar and the checks of a [screen toolbar button](screens.md): the slice is a State Change slice (`PLAY0761` to `PLAY0765`), and the button opens that slice's task dialog over whichever page the user is on. An indented `authorize <Policy>` line under the button limits who sees it; it takes the same policy expression as `authorize` on a command. Buttons are not allowed in `navigation sidebar` (`PLAY0741`). In a module, `navigation topbar extends` adds buttons after the application's, `overrides` shows only the module's own, and `inherits` keeps the application's, exactly as for items.

## At module level

Declare a region on a module only when its screens need different navigation. If you omit the region, it inherits the application's items. Here, Sales adds Customers to the shared sidebar but replaces the topbar with its Orders destination:

```screenplay
layout AppShell
  category application
  content

  navigation sidebar
    item sales title "Sales" target Sales
    item support title "Support" target Support

module Sales
  navigation sidebar extends
    item customers title "Customers" icon people target Sales.Customers
  navigation topbar overrides
    item orders title "Orders" target Sales.Orders

  feature Orders
  feature Customers

module Support
  feature Tickets
```

| Written | Effective items in the module |
| --- | --- |
| no block | The application's items (inherits). |
| `navigation <region> inherits` | The application's items. The explicit form of the default; it lists no items (`PLAY0745`). |
| `navigation <region> extends` | The application's items, followed by the module's own. |
| `navigation <region> overrides` | The module's own items only. An override with no items leaves the region empty and warns (`PLAY0752`). |

- Each region has its own mode; `topbar` and `sidebar` are independent.
- A module's own items may only target that module (`PLAY0750`). To link elsewhere, put the item in the layout.
- An extending module's item ids must not repeat the application's (`PLAY0747`).
- Targeting a module with no feature warns (`PLAY0751`).

A layout region that states a mode is rejected with `PLAY0745`.

## `navigation` and `contribute to Navigation`

Use a region block when you need a destination tree with hierarchy, target slots and module inheritance. Use [`contribute to Navigation`](contributions.md) when a feature should add a flat item to a slot marked `contributes Navigation`, without maintaining a central list.

The two declarations are independent: neither generates the other, and a layout can use both. Start a new application's navigation with regions, then add contributions where features need to supply their own entries.

## Checks and the TypeScript compiler

Use the C# compiler to validate navigation against the document's modules, features and layout slots. It reports [`PLAY0740`-`PLAY0752`](diagnostics.md#navigation-regions), matching Scene's `navTargetMissing`, `navSlotUnknown`, `navDuplicateId`, `navTargetOutOfScope` and `navModuleWithoutFeatures` findings.

The TypeScript compiler skips module `navigation` blocks, as it skips layouts; it does not run these checks. Navigation blocks are authoring syntax, not part of the executable model. Stage plans them from the syntax tree.

Parser, printer and parser again keep every item, id, icon, slot, target and mode. The syntax tree carries `NavigationRegionSyntax` (on `LayoutSyntax` and `ModuleSyntax`), `NavigationItemSyntax`, `NavigationTargetSyntax` and `NavigationLogoSyntax`; see [AST compatibility](ast-compatibility.md).

## Rename and move

When renaming or moving a module or feature, update the navigation items that target it in the same coordinated typed edits. An isolated rename or move is refused with an explicit message if an item's `target` would be left behind. Declarations that no item targets can be renamed or moved on their own.

## See also

- [Layouts and templates](templates.md) for the shell and its slots.
- [UI profile](ui-profile.md) for the `root template` that selects the shell the navigation regions live in.
- [Contributions](contributions.md) for the many-to-one `contribute to`.
- [Screens capability matrix](screens-capability-matrix.md).
