---
title: "Screenplay outlets: item lists a template offers"
description: "Declare actions, filters, tabs, stats and navigation as Screenplay outlets on a layout or template, then let modules, features and screens inherit, extend, override, hide and reorder their items."
---

# Outlets

An outlet is a named list of items a layout or template offers: the buttons of a toolbar, the filters above a list, the tabs of a detail pane, the figures of a dashboard, the destinations of a sidebar. The template says what kind of items the list holds and which it shows by default. A module, feature or screen changes the list for the screens beneath it - without copying the template.

An outlet is not a [slot](templates.md#slot-roles). A slot is a region content is placed in; an outlet is a list of items, which `in <slot>` can tie to the slot that shows it.

## Declare an outlet on a layout or template

```screenplay
outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]
  default <id> title "<text>" …
```

| Part | Meaning |
| --- | --- |
| `of <kind>` | The kind of item the outlet holds - see [Item kinds](#item-kinds). |
| `for item` / `for screen` | What the items act on: one item of the screen's data, or the screen as a whole. Leave it out when it does not matter. |
| `in <slot>` | The slot that shows the outlet. |
| `default` | An item the outlet shows unless a lower level changes it, written in the outlet's item form without the item's keyword. |

```screenplay
module Catalog
  screen template ListPage
    category List
    fits slot content
    header
    main
    detail pane routed by sku optional
    outlet toolbar of actions in header
      default refresh title "Refresh" runs refresh
      default export title "Export"
    outlet rowActions of actions for item
    outlet filters of filters in header
    outlet stats of stats in header
    outlet detailTabs of tabs in detail
```

A layout, screen template and dialog template declare outlets; a component does not. Declaring an outlet anywhere else is `PLAY0897`, and declaring the same outlet twice is `PLAY0904`. A `default` button with no `slice` is a placeholder: it does not warn (`PLAY0763`), because a lower level gives it its hook.

## Item kinds

| Kind | Items | Form |
| --- | --- | --- |
| `actions` | `button`, `group` | `button <id> title "…" [slice <Module>.<Feature>.<Slice>] [emphasis primary\|secondary\|quiet] [icon <name>] [runs close\|submit\|refresh]`, with optional `confirm "…"` and `authorize` lines under it. `group <id> title "…" [icon <name>]` holds `button` lines. |
| `filters` | `filter` | `filter <id> title "…" [sets <Query>.<param>]` - a control that sets a parameter of the query the screen shows. |
| `tabs` | `tab` | `tab <id> title "…" [shows <Screen>]` - a screen shown in the slot the outlet is `in`. |
| `stats` | `stat` | `stat <id> title "…" [value from <Query>.<field>]` - a figure read from a single-result query. |
| `navigation` | `item` | `item <id> title "…" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]`, nesting sub-items. See [Navigation](navigation.md). |

An item of the wrong kind for its outlet is `PLAY0900`. The checks follow what each item names:

- A button's `slice` must be a State Change slice the document declares (`PLAY0761`, `PLAY0762`).
- A filter's parameter must be a `by` or `filter` parameter of the query (`PLAY0901`).
- A stat's query must return a single result, not a list (`PLAY0902`).
- A tab's screen must exist (`PLAY0197`).

A navigation outlet on a layout also carries the application's one `logo image "…" title "…"`.

### Dialog and drawer buttons

A dialog or drawer template declares its buttons as an actions outlet. The built-ins `runs close` and `runs submit` close the dialog and submit the screen's command, so the template needs no slice:

```screenplay
module Invoicing
  dialog template TaskDialog
    category Dialog
    body
    footer
    outlet actions of actions in footer
      default cancel title "Cancel" runs close
      default save title "Save" emphasis primary runs submit
```

`runs refresh` reloads the screen's data.

### Hooks

A `button` that names a State Change `slice` is a hook. It opens the slice's screen in its first fit the page has free, or where it says, and it can run the slice without a form:

```screenplay
module Catalog
  screen template ListPage
    category Lists
    main
    detail pane routed by sku optional
    outlet rowActions of actions for item

  feature Products
    outlet rowActions extends
      button price title "Set price" slice SetProductPrice opens in slot detail
      button withdraw title "Withdraw" slice WithdrawProduct
        runs
        enabled when item.status == "onSale"
        confirm "Withdraw this product?"

    slice StateChange SetProductPrice
      command SetProductPrice
        sku String identifier
        price Decimal
        produces PriceSet
          for sku
      event PriceSet
        sku String
      screen SetProductPrice
        fits pane, drawer
        action SetProductPrice for screen

    slice StateChange WithdrawProduct
      command WithdrawProduct
        sku String identifier
        produces ProductWithdrawn
          for sku
      event ProductWithdrawn
        sku String
```

- `opens dialog`, `opens drawer` and `opens in slot <name>` name where the slice's screen opens; without one it opens in the screen's first [fit](screens.md#placement) the page has free.
- `for item` or `for screen` on the button says what it acts on, overriding the outlet's.
- A `runs` line issues the command directly. A slice issued only by a `runs` hook needs no screen, and a hook counts as issuing its slice's commands for `PLAY0533`.
- `enabled when <condition>` and `confirm "<text>"` go under the button.

`PLAY0907`-`PLAY0911` and `PLAY0915` check hooks; see [Placement, hooks and bindings](diagnostics.md#placement-hooks-and-bindings).

## Change an outlet below the template

A module, feature or slice screen writes `outlet <name> inherits|extends|overrides`. It names an outlet the layout or template declares; an unknown outlet is `PLAY0903`. A level that mentions nothing keeps what it inherits.

| Mode | Shows |
| --- | --- |
| `inherits` | The inherited items, unchanged. It lists nothing of its own (`PLAY0897`). |
| `extends` | The inherited items, then the level's own. |
| `overrides` | The level's own items only. Overriding with none warns (`PLAY0752`). |

Under the outlet line:

- `hide <id>` drops an inherited item.
- `before <id>` or `after <id>` under an item places it next to an inherited item instead of after them.

Each must name an item the inherited outlet has (`PLAY0899`). An extending level's own item may not repeat an inherited id (`PLAY0905`).

```screenplay
module Catalog
  screen template ListPage
    category List
    header
    main
    outlet toolbar of actions in header
      default refresh title "Refresh" runs refresh
      default export title "Export"
    outlet filters of filters in header
    outlet stats of stats in header

  feature Products
    outlet toolbar extends
      hide export
      button add title "Add product" slice DraftProduct emphasis primary icon plus
        before refresh
      group more title "More" icon dots
        button withdraw title "Withdraw" slice WithdrawProduct
    outlet filters extends
      filter status title "Status" sets AllProducts.status
    outlet stats extends
      stat drafts title "Drafts" value from ProductStats.drafts

    slice StateChange DraftProduct
      command DraftProduct
        sku String identifier
        produces ProductDrafted
          for sku
      event ProductDrafted
        sku String
      screen DraftProduct
        action DraftProduct

    slice StateChange WithdrawProduct
      command WithdrawProduct
        sku String identifier
        produces ProductWithdrawn
          for sku
      event ProductWithdrawn
        sku String
      screen WithdrawProduct
        action WithdrawProduct

    slice StateView ProductCatalog
      readmodel ProductRow
        sku String
        status String
      readmodel ProductStats
        drafts Int
      query AllProducts => ProductRow[]
        filter status String
      query ProductStats => ProductStats
      screen ProductCatalog
        template ListPage
        outlet toolbar inherits
```

The levels apply in order - the template's defaults, then the module, the feature and its parent features, then the screen - so a screen sees what every level above it left.

## Add to an outlet from anywhere

`contribute to <outlet>` adds items to an outlet from any module or feature, without the level that owns the screens listing them. See [Contributions](contributions.md).

```text
module Invoicing
  feature Payments
    contribute to detailTabs
      tab payments title "Payments" shows InvoicePayments
```

[Contributions](contributions.md#example) shows the same contribution with the template that declares `detailTabs`.

## Checks and the TypeScript compiler

Both compilers read a module's, feature's and screen's outlets and report their form: `PLAY0896`, `PLAY0897`, `PLAY0898` and `PLAY0904`; a malformed hook part (`opens`, `for`, `runs`) is `PLAY0760` in both. Layouts, templates and contributions stay opaque to the TypeScript compiler, so the declarations and the checks that need them - `PLAY0895`, `PLAY0899`-`PLAY0903` and `PLAY0905` - are reported by the C# compiler. See [Outlets diagnostics](diagnostics.md#outlets).

Outlets are authoring syntax and are not part of the executable model, like every screen construct; the semantic binder reports a module's outlets as deferred.

## From Screenplay 5

Screenplay 6 turned three Screenplay 5 forms into outlets. Each old form is an error whose message names the replacement and `screenplay migrate --from 5 --to 6`:

| Screenplay 5 | Screenplay 6 | Code |
| --- | --- | --- |
| `navigation topbar\|sidebar` on a layout | `outlet topbar\|sidebar of navigation` with `default` items | `PLAY0894` |
| `navigation sidebar extends` on a module | `outlet sidebar extends` | `PLAY0894` |
| `<slot> contributes <Point>` | `outlet <Point> of navigation in <slot>` | `PLAY0895` |
| `toolbar <name>` on a feature or module | `outlet <name> extends` | `PLAY0896` |

## See also

- [Layouts and templates](templates.md) for slots and their roles.
- [Navigation](navigation.md) for navigation outlets.
- [Contributions](contributions.md) for `contribute to`.
- [Screens](screens.md#toolbar-buttons) for a screen's own toolbar buttons.
