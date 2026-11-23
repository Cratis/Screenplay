---
title: "Screenplay navigation: navigation outlets in the shell"
description: "Declare topbar and sidebar destinations as navigation outlets on a Screenplay layout, which modules, features and screens can inherit, extend or override."
---

# Navigation

Screenplay navigation is an [outlet](outlets.md) of kind `navigation`: a named list of destinations the layout offers. Declare the application-wide items once on the `layout`, then let each module, feature or screen inherit them, add its own destinations or replace the list. This keeps application-wide navigation separate from the screens users reach through it.

Use navigation outlets to guide users to modules, features or a selected screen within a feature. For a link carrying record parameters, use [screen navigation](screens.md#navigation-metadata) or an [interaction](interactions.md).

## At application level

A layout declares each navigation outlet with `outlet <name> of navigation`, optionally `in <slot>` to say which slot it fills, and lists its items with `default`. The topbar carries the application's one `logo`:

```screenplay
layout AppShell
  category Shell
  topbar
  side panel
  content page routed primary

  outlet topbar of navigation in topbar
    logo image "/assets/acme.svg" title "Acme"
    default sales title "Sales" icon receipt target Sales
    default support title "Support" target Support slot content
  outlet sidebar of navigation in side
    default sales title "Sales" icon receipt target Sales
      item orders title "Orders" target Sales.Orders
      item returns title "Returns" target Sales.Orders.Returns slot side
    default support title "Support" target Support

module Sales
  feature Orders
    feature Returns

module Support
```

The `logo image` is a URL the browser loads, and the compiler does not check it. Two forms work:

- A path under `/assets/`, as above, with the file in an `assets` folder beside the `.play` files (`assets/acme.svg`). [Stage](https://github.com/Cratis/Stage) serves that folder read-only in the sandbox from 5.2.0. A host that does not serve it shows a broken image, and the compiler cannot tell you.
- An inline `data:image/svg+xml;base64,...` URI, which needs no file and works in every host. [The Acme walkthrough](acme-commerce-walkthrough.md#navigation-and-the-logo) uses it.

A navigation item is `<id> title "…" [icon <name>] [target <Module>[.<Feature>] [screen <Screen>]] [slot <name>]` - after `default` on a layout, after `item` below it:

| Part | Meaning |
| --- | --- |
| `<id>` | Unique within the outlet. Quote it when it is not an identifier: `default "nav:help" title "Help"`. |
| `title` | A quoted string or a `$strings.<key>` token. |
| `icon` | Optional icon name. |
| `target` | A module, or a module and a feature path - `Sales.Orders.Returns` for a nested feature. An item without a target must have sub-items. |
| `screen` | Optional page-capable executable screen in the exact target feature. Requires `target Module.Feature`; nested feature paths work. |
| `slot` | The layout slot the target renders into. Without it, the target renders into the layout's primary slot. |

Any item nests sub-items as `item` lines under it.

## Choose a feature's landing screen

Without `screen`, the consumer keeps its existing fallback: the first page-capable screen at or beneath the resolved feature (or module). Declaration order matters; this is not a form/list heuristic. Reordering command forms to influence a sidebar link changes the fallback for other links too.

Select a destination on the item instead:

```screenplay
layout AppShell
  category Shell
  content page routed primary
  outlet sidebar of navigation
module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects screen Projects
    item create title "Create project" target Shop.Projects screen CreateProject
  feature Projects
    slice StateView Views
      screen CreateProject
        fits page
      screen Projects
        fits page
```

The first item selects `Projects` even though `CreateProject` occurs first. Two items may select different screens of the same feature. `screen Projects target Shop.Projects` is equivalent; the printer writes `target` before `screen`. Layout defaults, nested items and navigation contributions use the same item form.

The name must identify one executable screen **in that exact feature**, not a descendant. A free screen is eligible only where its first adopting slice owns it; adopting it again does not move its identity. A screen with no `fits` is page-capable, as is one explicitly including `page`; dialog-only, drawer-only and pane-only screens are not navigation destinations. Missing, competing and non-page screens are errors, never reasons to choose a different screen. A module-only target or grouping item without a target cannot carry `screen`.

This is item-local selection, not a feature-wide `default screen` declaration. It does not define the application's initial `/` route or logo destination. Scene conversion and Stage rendering must be updated to carry and honor `screen`; this producer change alone does not add that consumer support. Until then, a renderer may retain its old fallback. Stage's existing exclusion of dialog-only screens remains separate from selecting a list instead of an earlier page-capable command form.

## At module level

A module changes an outlet with `outlet <name> inherits|extends|overrides`. An outlet a module does not mention keeps the items it inherits:

```screenplay
layout AppShell
  category Shell
  content page routed primary
  outlet topbar of navigation
    default sales title "Sales" target Sales
  outlet sidebar of navigation
    default sales title "Sales" target Sales

module Sales
  outlet sidebar extends
    item customers title "Customers" icon people target Sales.Customers
  outlet topbar inherits
  feature Customers

module Support
  outlet sidebar overrides
    item tickets title "Tickets" target Support.Tickets
  feature Tickets
```

| Mode | Shows |
| --- | --- |
| `inherits` | The inherited items, unchanged. The explicit form of writing nothing; it lists no items (`PLAY0897`). |
| `extends` | The inherited items, then the module's own. |
| `overrides` | The module's own items only. Overriding with none warns (`PLAY0752`). |

A feature and a slice screen change an outlet the same way, for the screens beneath them. `hide <id>` drops an inherited item and `before <id>` or `after <id>` under an item places it among the inherited ones - see [Outlets](outlets.md#change-an-outlet-below-the-template).

A module's own items may only target that module (`PLAY0750`). The logo belongs to the application: a logo below the layout is `PLAY0744`.

## Navigation outlets and `contribute to`

Use a navigation outlet when you need a destination tree with hierarchy, target slots and inheritance. Use [`contribute to <outlet>`](contributions.md) when a feature should add an entry to an outlet without the module listing it. A contribution may target any outlet, from any module or feature.

## Checks and the TypeScript compiler

Use the C# compiler to validate navigation against the document's modules, features and layout slots. It reports [`PLAY0741`, `PLAY0743`, `PLAY0744` and `PLAY0748`-`PLAY0752`](diagnostics.md#navigation-outlets) and the [outlet checks](diagnostics.md#outlets), matching Scene's `navTargetMissing`, `navSlotUnknown`, `navDuplicateId`, `navTargetOutOfScope` and `navModuleWithoutFeatures` findings.

Layouts stay opaque to the TypeScript compiler: it does not validate explicit screen destinations in layout or template defaults. It reads a module's, feature's or screen's outlets (including free screens), reports their form, and validates explicit screen destinations after full-document or application assembly. Like the C# compiler, it skips screen resolution when an item's target is outside its declaring module; C# reports `PLAY0750` for that out-of-scope target. The remaining native layout, slot and reference checks are not all ported. Outlets are authoring syntax, not part of the executable model. Stage plans them from the syntax tree.

Parser, printer and parser again keep every item, id, icon, slot, target, mode, hidden item and position. The syntax tree carries `OutletSyntax` (on `LayoutSyntax`, `ScreenTemplateSyntax`, `DialogTemplateSyntax`, `ModuleSyntax`, `FeatureSyntax` and `ScreenSyntax`), `NavigationItemSyntax`, `NavigationTargetSyntax` and `NavigationLogoSyntax`; see [AST compatibility](ast-compatibility.md).

## From Screenplay 5

Screenplay 5 wrote these lists as `navigation topbar|sidebar` blocks and a slot's `contributes <Point>`. Both are errors since Screenplay 6 (`PLAY0894`, `PLAY0895`); run `screenplay migrate --from 5 --to 6` to rewrite a model. A Screenplay 5 region could also hold buttons; a navigation outlet lists destinations only, so the migration moves a region's buttons to an actions outlet, `topbarActions` or `sidebarActions`. See [Migrating from 5](migration.md#migrating-from-5).

## Rename and move

When renaming or moving a module or feature, update the navigation items that target it in the same coordinated typed edits. An isolated rename or move is refused with an explicit message if an item's `target` would be left behind. Declarations that no item targets can be renamed or moved on their own.

## See also

- [Outlets](outlets.md) for every outlet kind and how lower levels change them.
- [Layouts and templates](templates.md) for the shell and its slots.
- [UI profile](ui-profile.md) for the `root template` that selects the shell the navigation outlets live in.
- [Contributions](contributions.md) for the many-to-one `contribute to`.
- [Screens capability matrix](screens-capability-matrix.md).
