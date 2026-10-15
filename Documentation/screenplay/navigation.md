# Navigation regions

The application shell has two navigation regions: a **topbar** and a **sidebar**. A `layout` declares what the application puts in them. A `module` states how it relates to them - it inherits the application's items, extends them with its own, or overrides them. The blocks are the Screenplay form of Scene's `NavigationModel`, so a navigation authored here is the one the designer shows, Stage renders and Studio round-trips.

## At application level

Regions are blocks inside a [`layout`](templates.md), next to its slots. The application's regions have no mode: the application is the base.

```screenplay
layout AppShell
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

## At module level

A module declares a block only for a region it changes. A region it does not mention **inherits** the application's items.

```screenplay
layout AppShell
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

[`contribute to Navigation`](contributions.md) stays valid. It is how a feature adds one flat item to a slot that `contributes Navigation`. A region block describes the region's own item tree with hierarchy, targets, slots and inheritance. The two are independent: neither generates the other, and a layout can use both. A new application models its navigation with regions.

## Checks and the TypeScript compiler

The C# compiler checks the regions against the modules, features and layout slots the document declares and reports [`PLAY0740`-`PLAY0752`](diagnostics.md#navigation-regions); the findings mirror Scene's `navTargetMissing`, `navSlotUnknown`, `navDuplicateId`, `navTargetOutOfScope` and `navModuleWithoutFeatures`. The TypeScript compiler recognizes `navigation` in a module and skips it whole, as it skips layouts, so the checks are C#-only. Navigation blocks are authoring syntax: the executable model does not carry them, and Stage plans them from the syntax tree.

Parser, printer and parser again keep every item, id, icon, slot, target and mode. The syntax tree carries `NavigationRegionSyntax` (on `LayoutSyntax` and `ModuleSyntax`), `NavigationItemSyntax`, `NavigationTargetSyntax` and `NavigationLogoSyntax`; see [AST compatibility](ast-compatibility.md).

## Rename and move

An item's `target` names a module or feature. Renaming or moving a module or feature that a navigation item targets is refused with an explicit message - rename or move it together with the `target` in coordinated typed edits. Declarations no item targets rename and move as usual.

## See also

- [Layouts and templates](templates.md) for the shell and its slots.
- [Contributions](contributions.md) for the many-to-one `contribute to`.
- [Screens capability matrix](screens-capability-matrix.md).
