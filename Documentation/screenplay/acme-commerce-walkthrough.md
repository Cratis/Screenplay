---
title: "Acme Commerce: a walkthrough"
description: "How the Acme Commerce sample was built: five bounded contexts in one Screenplay application, with public events, shell, screens and Studio views."
---

# Acme Commerce: a walkthrough

Acme Commerce is the large sample in this repository: a mid-size shop in five bounded contexts - **Catalog, Storefront, Sales,
Purchasing and Warehouse** - written as ONE Screenplay application in
[`Samples/AcmeCommerce`](https://github.com/Cratis/Screenplay/tree/main/Samples/AcmeCommerce). The contexts never share a
database. They talk only through twelve *public events*, and each context translates what it receives into its own words, so a
change in one context's language never leaks into another's.

This page tells how the sample was built and why each part looks the way it does. Every statement here is checked against the
sources of the sample; the sample's own [README](https://github.com/Cratis/Screenplay/blob/main/Samples/AcmeCommerce/README.md) is the
short reference. What the language can say is said in the `.play` files; only the brainstorming material and a few project settings live only in Studio.

At the time of writing the application is 56 `.play` files:

| Domain | Module | Slices (change / view / automation / translate) | Screens | Specifications |
| --- | --- | --- | --- | --- |
| Catalog | `Merchandising` | 5 / 5 / 0 / 3 | 10 | 18 |
| Storefront | `Shop` | 9 / 6 / 1 / 6 | 15 | 27 |
| Sales | `Orders` | 8 / 6 / 2 / 7 | 15 | 30 |
| Purchasing | `Buying` | 12 / 6 / 1 / 3 | 20 | 34 |
| Warehouse | `Stock` | 18 / 6 / 5 / 9 | 24 | 58 |

The shell adds the `Application` module (2 slices, 4 screens, 2 specifications). Together that is 169 specifications.

## Where to find it

| What | Where |
| --- | --- |
| The Screenplay source | `Samples/AcmeCommerce` in this repository |
| The Studio project | the project **Acme Commerce** with the application **Acme Commerce Shop**, in the **Cratis** tenant of [Cratis Studio](https://app.cratis.studio/) |
| The loader that fills the Studio project | `scripts/samples/acme-commerce` in the `Cratis/Studio` repository |

The Studio project is permanent: it is never deleted, and it is only refreshed through the loader. Sign in to Cratis Studio
with an account in the Cratis tenant and pick **Acme Commerce** in the project picker. A project's address has the shape
`https://app.cratis.studio/projects/<projectId>` and an application's
`https://app.cratis.studio/projects/<projectId>/application/<applicationId>`, so open the project from the picker rather than from
a copied link. The application is called "Acme Commerce Shop" rather than plain
"Acme Commerce" because Studio application names are unique per tenant and an early scratch application had already taken the
shorter one.

## How it was built

The sample was written in this order, and each step compiles on its own:

1. The **shared kernel** (`Shared/`): concepts, event sources, policies and personas.
2. The **shell** of each domain (`<Domain>/shell/`): its theme, UI profile, navigation with logo and sample data.
3. **Catalog** first, because it publishes the first contracts (P1 to P3).
4. **Storefront**, which only listens to Catalog and then publishes the first order (P4).
5. **Sales**, **Warehouse** and **Purchasing**, which close the loops: order to cash to dock, and replenishment.
6. The **custom component package**, the **sample data** and the **tooling** that packages a domain for Studio and builds a
   renderable variant.

### One application, five folders

```text
application.play                the ONLY `domain AcmeCommerce` line; `reaction batches atomic`, `command reads protected`
Shared/                         the shared kernel
<Domain>/shell/                 the domain's own theme, ui profile, navigation + logo, sample-data pack
assets/logos/                   the five logo SVGs
<Domain>/model/                 state changes, state views, automations, forms, screens, specifications
<Domain>/translation/           Translate slices: outbound publication, inbound anti-corruption
_packages/scene.commerce/       the custom component package @acme/scene.commerce
tools/                          domain-package.sh, render-variant.sh, render-bridge/, packages/
```

Four rules about composing a [folder](folders.md) into one application shaped the layout:

- **One `domain` line per tree.** Only `application.play` has it; the domain folders carry none.
- **One name per event in the application.** The same name on a publisher and on a consumer compiles but does not bind
  (`PLAY0472`). A consumer therefore declares a foreign public event under a *consumer-local* name,
  `<Event>At<Consumer>` - `ProductPublishedAtStorefront` - and `from "<Origin>"` keeps the lineage.
- **No `..` imports.** `Shared/` is a sibling folder; compiling a folder walks every `.play` file in it.
- **A read model's first event creates it.** Every required property is set there with a literal default. Specifications
  that reach an unadmitted projection or depend on its read model report "unsupported"; unrelated specifications still run.

## The shared kernel

`Shared/` holds what every context means the same way.

**Concepts with their rules.** Validation lives on the [concept](concepts.md), so the rule travels with the value into every
command, event and read model. `Sku` is the key every context knows a product by:

```text
concept Sku : String
  validate
    not empty            message "A SKU is required"
    min 7                message "A SKU has at least 7 characters"
    max 24               message "A SKU has at most 24 characters"
    starts with "ACM-"   message "A SKU starts with ACM-"
    not contains " "     message "A SKU has no spaces"
```

Other shared concepts are `Money` (a `Decimal`, `>= 0`, `decimals <= 2`), `Quantity` (an `Int`, `> 0`), `CurrencyCode` (three letters),
`PurchaseOrderNumber` (starts with `PO-`) and `EmailAddress`, which is marked `pii` with a stated reason. Each domain adds the
words only it uses: Storefront has `Slug`, `ShortDescription`, the enumeration concept `DeliveryMethod` (`standard`, `express`,
`pickup`) and the composite type `BasketLine`. Concepts that every context uses include the enumerations `OrderStatus`,
`ProductStatus` and `Carrier` and the composite types `PostalAddress` and `OrderLine`.

**Event sources.** Every [event source](event-sources.md) is declared once in `Shared/eventsources.play`, because a source is a
name several contexts may refer to: `Product`, `Customer`, `Basket`, `Order`, `Supplier`, and so on, with named streams where
one identity has more than one history.

**Policies and personas.** `Shared/access.play` declares the [policies](policies.md) (`IsShopper`, `IsSalesManager`,
`CanCurateCatalog`, `CanBuy`, `CanWorkWarehouse`, ...) and then the personas that hold them: Shopper, Merchandiser, SalesRep,
SalesManager, Buyer and WarehouseClerk: six personas, each a real person who works in the shop. A module admits its own persona(s):

```text
policy CanCurateCatalog
  require role "Merchandiser"
```

**Processing purposes.** Storefront (`ShopOrderProcessing`), Sales (`OrderFulfilmentProcessing`) and Purchasing
(`SupplierRelations`) each declare a [`purpose`](purposes.md) for the personal data they handle, and the module names it with
`purpose`.

**The theme.** `Shared/theme.play` declares the `Acme` theme and the packages it is compatible with, including the custom
package `Acme.Commerce`.

## The public events (P1 to P12)

These are the contracts. A public event is the only thing that crosses a context boundary.

| # | Public event | Published by | Consumed by |
| --- | --- | --- | --- |
| P1 | `ProductPublished` | Catalog | Storefront, Warehouse |
| P2 | `ProductPriceChanged` | Catalog | Storefront, Sales |
| P3 | `ProductRetired` | Catalog | Storefront, Warehouse |
| P4 | `OrderPlaced` | Storefront | Sales |
| P5 | `OrderConfirmed` | Sales | Warehouse, Purchasing |
| P6 | `OrderStatusChanged` | Sales | Storefront |
| P7 | `StockReserved` | Warehouse | Sales |
| P8 | `StockReservationFailed` | Warehouse | Sales |
| P9 | `StockLevelChanged` | Warehouse | Storefront |
| P10 | `StockBelowReorderPoint` | Warehouse | Purchasing |
| P11 | `PurchaseOrderReceived` | Purchasing | Warehouse |
| P12 | `ShipmentDispatched` | Warehouse | Sales |

The flows they carry: the catalogue (F-A: Catalog to Storefront and Warehouse), order to cash to dock (F-B: Storefront, Sales,
Warehouse and back), replenishment (F-C: Warehouse, Purchasing, Warehouse) and change of price (F-D: Catalog to Storefront and
Sales).

### Private versus public events

Everything else is private. A domain says what it has done in its own words (`ProductApprovedForSale`, `OrderConfirmationIssued`,
`ReservationMade`) and only the `Translate` slices turn a private event into a public one. A context never sees another's private
events.

### Outbound translation: one public contract per slice

Catalog's `PublishProduct` slice turns the private `ProductApprovedForSale` into the public `ProductPublished`. The approval
carries a snapshot of what was approved, so the public event is a plain projection of it:

```text
slice Translate PublishProduct
  description "Publishes ProductPublished when a product is approved for sale"
  direction outbound

  public event ProductPublished
    productId ProductId
    sku       Sku
    name      ProductName
    ...

  projection ProductPublisher => ProductPublished
    from ProductApprovedForSale
      productId = $eventSourceId
      sku       = sku
      name      = name
      ...
```

Sales' `PublishOrderStatus` is the many-to-one case: six private events (approved, rejected, on hold, shipped, fulfilled and
cancelled) become ONE public `OrderStatusChanged` with a `status`.

### Inbound translation: the anti-corruption layer

Storefront declares the foreign event in its own shape and then *captures* it into a private event in the shop's own language.
The shop addresses a product by its SKU, so every product capture uses `key sku`:

```text
slice Translate ListProduct
  direction inbound

  event ProductPublishedAtStorefront from "Catalog"
    productId ProductId
    sku       Sku
    ...

  capture ProductFeed
    source events
      from ProductPublishedAtStorefront
    key sku
    append ProductListed
      when added
        productId        = $.productId
        title            = $.name
        shortDescription = $.details
        slug             = $.sku
        ...
        available        = 0
```

Catalog's `name` becomes Storefront's `title`; the SKU becomes the slug and, through `key sku`, the event source id;
availability starts at zero until Warehouse says otherwise (P9). The sample declares 16 consumer-local events like this one,
one for each consumer of each public event in the table above. See [captures](captures.md).

### Reactions between the domains

[Reactions](reactions.md) are the automations inside a domain that hear a private event and invoke a command. Examples that
run across the whole flow:

- Storefront: `CheckoutCompleted` invokes `ClearBasketContents`, which empties the basket.
- Sales: an `OrderReceived` below 500 invokes `ApproveOrder` (`where quotedTotal < 500`), and an `OrderApproved` invokes
  `ConfirmOrder`, which is what produces P5.
- Warehouse: `ReservationRequested` invokes `ReserveStockUnits`; a drop in available stock invokes `RaiseReorderAlarm`, which is
  what produces P10; a reservation plans a shipment; a dispatch takes the stock off the shelf.
- Purchasing: `ReplenishmentRequested` (from P10) invokes `CreateReplenishmentPurchaseOrder`.

```text
reaction SmallOrderAutoApproval
  runs as system role "SalesManager"
  when OrderReceived
    orderId
    quotedTotal
    invokes ApproveOrder
      orderId = orderId
  where quotedTotal < 500
```

A reaction runs *as* a stated role (`runs as system role ...`), so the policies of the command it invokes still apply.

## Domain by domain

### Catalog (`Merchandising`)

The Merchandiser curates what is sold. Features: Products, Pricing, Publication.

- **Commands and events.** `DraftProduct` / `ProductDrafted`, `DescribeProduct` / `ProductDescribed`, `SetProductPrice` /
  `ProductPriceSet`, `ApproveProductForSale` / `ProductApprovedForSale`, `WithdrawProduct` / `ProductWithdrawn`.
- **Read models.** `ProductRow`, `ProductDetail`, `PricedProduct`, `ProductReadiness` and `CatalogFigure`.
- **A decision against state.** `ApproveProductForSale` reads `ProductReadiness` and refuses with "Describe the product before
  approving it" or "Price the product before approving it".
- **Constraints.** `UniqueSku` (`unique sku on ProductDrafted`) and `OneApprovalPerProduct` (`unique event
  ProductApprovedForSale`, `released by ProductWithdrawn` - a withdrawn product may be approved again).
- **Public events.** P1, P2, P3 through `PublishProduct`, `PublishPriceChange` and `PublishRetirement`.

### Storefront (`Shop`)

The Shopper browses, fills a basket and checks out. Features: Browse, Basket, Checkout, MyOrders.

- **Inbound.** `ListProduct` (P1), `UpdateDisplayPrice` (P2), `DelistProduct` (P3), `UpdateAvailability` (P9) and
  `TrackOrderStatus` (P6).
- **Outbound.** `PublishOrder` turns `CheckoutCompleted` into P4.
- **Rules.** `AddToBasket` reads the listing and refuses a sold-out product; `UniqueCustomerEmail`; `EmailAddress` must
  contain `@`.
- **A multi-step flow.** Checkout is a wizard whose three steps (`EnterShippingAddress`, `ChooseDeliveryMethod`, `CheckOut`) are
  command [forms](forms.md); `CheckOut` reads the basket size and the customer account and is refused for an empty basket.
- **Its own page shapes.** `ProductGrid` and `SideSummary` are screen templates the module declares itself (see below).
- **Navigation.** The module *overrides* the application's topbar with its own: Browse, Basket, My orders.

### Sales (`Orders`)

Order intake, approval, confirmation and status. Features: Intake, Approval, Fulfilment, Reporting.

- **Inbound.** `ReceiveOrder` (P4), `UpdatePriceList` (P2), `RecordReservation` (P7), `HoldOrderOnShortage` (P8) and
  `RecordShipment` (P12).
- **Outbound.** `PublishOrderConfirmed` (P5) and `PublishOrderStatus` (P6). The private event behind P5 is
  `OrderConfirmationIssued`; the public contract keeps the name `OrderConfirmed`.
- **Policy-driven automation.** Small orders approve themselves; larger ones wait in the manager's inbox.
- **Who may do what.** `ApproveOrder`, `RejectOrder`, `ApplyLargeDiscount` and the inbox are for the SalesManager
  (`authorize IsSalesManager`); a SalesRep's `ApplyDiscount` stops at 15 percent.
- **Constraints.** `OneOpenOrderPerBasket` (`unique event OrderReceived`; it lives with the consumer because a constraint that
  mentions an inbound-captured event cannot sit in the inbound slice, `PLAY0616`) and `OneConfirmationPerOrder`.
- **Navigation.** The module *extends* the application's sidebar with a Reports item.

### Warehouse (`Stock`)

Physical stock. Features: Levels, Reservations, Receiving, Dispatch, Overview. The largest domain, with nine translate slices
and five automations.

- **Inbound.** `RegisterSkuFromCatalog` (P1), the SKU block on retirement (P3), the reservation request from P5 and the stock
  receipt from P11.
- **Outbound.** P7, P8, P9, P10 and P12.
- **Automations.** `ReserveStock` (it either promises the stock with `ReservationMade` or says it could not with
  `ReservationFailed`), `WatchReorderPoint` (one `ReorderPointCrossed` per crossing; the alarm re-arms when stock comes back),
  `SnapshotStockOnMovement`, `PlanShipmentWhenReserved` and `TakeStockWhenDispatched`.
- **Constraints.** `UniqueLocationCode` and `OneDispatchPerShipment`.

### Purchasing (`Buying`)

Suppliers, replenishment and purchase orders. Features: Suppliers, Replenishment, PurchaseOrders, Demand, Overview.

- **Inbound.** `RecordDemand` (P5) and `RequestReplenishment` (P10). **Outbound.** `PublishGoodsReceived` (P11).
- **Automation.** `AutoReplenish` drafts a one-line purchase order with the SKU's preferred supplier. It skips when an open order
  already covers the SKU (the constraint `OneOpenReplenishmentOrderPerSku` is *released by* goods received or a cancellation),
  and when there is no preferred supplier it records `ReplenishmentBlocked`, which the replenishment read model shows as
  status `blocked`.
- **Single-value keys.** A supplier's offers are keyed by a generated `SupplierOfferId` (the offer is an event source of its own) instead of supplier + SKU: Stage refuses composite read-model keys (STAGE-ESM-048) until Chronicle#4265 ships, and the sample has no composite key anywhere. They return when Chronicle#4265 ships.
- **Constraints.** `UniqueSupplierTaxId`, `UniquePurchaseOrderNumber`, `OneSubmissionPerPurchaseOrder` and
  `OneOpenReplenishmentOrderPerSku`.

### Constraints

Eleven [constraints](constraints.md) in all. Most are uniqueness rules written where the command appends the event:

```text
constraint UniqueSku
  unique sku on ProductDrafted
  message "That SKU is already in the catalogue"
```

`unique event X` makes the event happen once per event source; `released by Y` names another slice's event that lifts the rule
again.

### Specifications

Every behaviour above has a [specification](specifications.md): a command is given a caller (`authenticated`, `role "..."`) and
prior events and then either produces events or is refused with a message; a view is given events and then shows a read model.
The accepted path of a command whose slice publishes a public event states that public event after the private ones (`then
ProductPriceChanged` after `then ProductPriceSet`), so the publication is proved where the command is; the translate slice keeps
its own specification for the event-driven path.

## The shell

### One application per domain

Scene keeps the logo and the UI profile at application level only (a module shows the application's logo locked), so a domain that has its own
logo, theme and shell is its own application: Acme Shop, Acme Catalog, Acme Sales, Acme Purchasing and Acme Warehouse. They share the platform
(`Cratis.Components` and the Default blueprint's `AppShell`, `^1`) and differ in logo, accent, colour scheme, menu mode and menu theme. Each
`<Domain>/shell/` folder holds three files, and a domain package (`tools/domain-package.sh`) carries them to Studio.

### The UI profile

`<Domain>/shell/shell.play` declares the domain's theme and its `ui profile`: the web target, the packages, an accent colour, the menu and a
**root template** taken from a blueprint - the `AppShell` of `Cratis.Blueprint.Default`, pinned at `^1`. Storefront, the customer-facing one:

```text
theme AcmeStorefront
  compatible with Cratis.Components
  compatible with Cratis.Blueprint.Default
  compatible with PrimeReact
  compatible with Acme.Commerce

ui profile Storefront
  target platform web
  target size regular
  theme AcmeStorefront

  packages
    Cratis.Components
    Cratis.Blueprint.Default

  root template blueprint Cratis.Blueprint.Default AppShell version "^1"
    option navigationMode horizontal
    option menuTheme light

  appearance
    color scheme light
    accent "#C2410C"

  sample data
    active AcmeStorefront
```

| Domain | Accent | Menu mode | Menu theme | Scheme |
| --- | --- | --- | --- | --- |
| Storefront | `#C2410C` | `horizontal` | light | light |
| Catalog | `#4338CA` | `static` | light | light |
| Sales | `#1D4ED8` | `static` | dark | light |
| Purchasing | `#B45309` | `slimPlus` | light | light |
| Warehouse | `#0F766E` | `compact` | dark | dark |

The name, logo image, corners and font that the designer's App and Look tabs show are not in the `ui profile` grammar yet; Studio's sample loader
saves them (`identity` stage) from `assets/logos`.

### Sample data

`<Domain>/shell/sampledata.play` is the domain's [sample data](sample-data.md) pack (`AcmeStorefront`, `AcmeCatalog`, ..., locale `nb-NO`): one
shape per read model that a screen shows. It is design and preview time only: a production build never sees it.

### Navigation and the logo

With a blueprint root template the layout carries only the [navigation regions](navigation.md). `<Domain>/shell/navigation.play` declares
the domain's layout (`StorefrontNavigation`, `CatalogNavigation`, ...): the topbar with the logo - `logo image "data:image/svg+xml;base64,..."`,
the SVG of `assets/logos` inlined by `tools/embed-logos.py`, because a document carries no file beside it and a data URI cannot 404 - and a
hierarchical sidebar, module then feature. A domain module says how it relates to the shell:

| Module | Statement | Effect |
| --- | --- | --- |
| `Shop` | `navigation topbar overrides` | the storefront has its own light topbar |
| `Orders` | `navigation sidebar extends` | adds a Reports item to the sidebar |
| `Merchandising`, `Buying`, `Stock` | none | inherit the application's navigation |

### Toolbars

The application's topbar declares buttons once, for every page (`Catalog/shell/navigation.play`), and a policy gates each:

```text
navigation topbar
  button newProduct title "New product" slice Merchandising.Products.DraftProduct emphasis primary icon plus
    authorize CanCurateCatalog
```

A screen's toolbar belongs to that screen. Catalog's product list carries a primary button that opens the slice's dialog:

```text
toolbar
  toolbar actions
    button new title "New product" slice Merchandising.Products.DraftProduct emphasis primary icon plus
```

There are 23 toolbar buttons across the domains, mostly in Purchasing (10) and Warehouse (7).

## Screens

Screens fill [templates](templates.md). The sample uses the foundation blueprint templates by reference, for example
`template blueprint Cratis.Blueprint.Default ListScreen version "^1"`:

| Template | Screens | Used for |
| --- | --- | --- |
| `QuickCreateDialog` | 30 | the input surface of a command: new product, add line, adjust stock, ... |
| `ConfirmDialog` | 13 | a command that needs only a yes or no: approve or withdraw a product, release a hold, fulfil an order, pick or pack a parcel |
| `ListScreen` | 12 | work lists: the product catalogue, the approval inbox, purchase orders, shipments, stock levels |
| `RecordPage` | 9 | one thing in full: product page, order page, purchase order page, supplier page, stock item |
| `DashboardScreen` | 4 | the dashboards of Catalog, Sales, Purchasing and Warehouse |
| `BoardScreen` | 3 | the pricing board, the orders pipeline and the picking queue |
| `FormScreen` | 2 | the Catalog set-price and describe-product forms |
| `WizardScreen` | 2 | Storefront checkout and Purchasing "New purchase order" |
| `SideSheet` | 1 | Purchasing goods receipt |
| `MasterDetailScreen` | 1 | Purchasing suppliers |
| own `ProductGrid`, `SideSummary` | 1 and 3 | Storefront browse; basket, product page and order tracking |

Command input is a [form](forms.md) declared once and found by its command (`form DescribeProductForm for DescribeProduct`),
with a two-column layout where it helps.

### Every command has its own screen

A command's slice owns the screen the person sees when they run it: a `QuickCreateDialog` for a command that asks for input, a `ConfirmDialog`
for one that only needs a yes. The board draws it under the persona who may run the command, and the slice previews and plays on its own.
Only the commands a reaction invokes on its own (confirm an order, clear a basket after checkout, take dispatched stock, snapshot the stock level,
raise the reorder alarm, reserve stock units, plan a shipment) have no screen: nobody runs them.

The screen where the persona works reaches the command, in the way that fits it:

| Surface | Example | Written as |
| --- | --- | --- |
| toolbar button that opens the command's dialog | Catalog "New product", Purchasing "New purchase order", "Register supplier", Warehouse "Register SKU" | `button new title "New product" slice Merchandising.Products.DraftProduct` |
| action on the selected row of a list | Warehouse "Adjust stock", "Count", "Dispatch"; Purchasing "Choose supplier" | `action AdjustStock label "Adjust stock"` under a `toolbar` |
| state-dependent action on the selected row | Catalog "Approve for sale" and "Withdraw" on the product lists; Warehouse "Pick order", "Pack", "Cancel shipment" | `action "Approve for sale"` with `when item.status == "draft" execute ApproveProductForSale` and `otherwise hidden` |
| state-dependent action on a record page | product page, order page ("Release hold" only while on hold, "Mark fulfilled" once shipped), purchase order ("Archive" once received or cancelled) | the same guarded `action`, beside the `data` of the one record |
| reaction | confirm an order after approval, clear the basket after checkout | `reaction ... invokes Command` |

### The domain's own templates

Storefront does not borrow a blueprint shape for the pages that define the shop. It declares its own templates, created from
the current screen and categorised under Shop. A template is a *shape with a designed first draft*: the slots it declares, how
they share the space, and the stock components (`Cratis.Blueprint.Default`) it seeds each slot with, so the template editor
draws a real product grid and a real summary layout - not an empty place:

```text
screen template ProductGrid
  display "Product grid"
  category Shop
  scopes slice stateview
  fits slot content
  outlet quickView

  header
  filters
  products

  arrangement flow
    ...

  content filters
    component Cratis.Blueprint.Default.filterBuilder gridFilters
      ...
  content products
    component Cratis.Blueprint.Default.dataList gridCards
      property variant = "cards"
      property rowActions = ["Add to basket"]
      ...
```

- `ProductGrid` is a responsive grid of product cards (picture, name, price, an add-to-basket button) with a filters rail beside it
  (its slot is not called `grid`: in an arrangement `grid` is the layout kind). The filter builder's value narrows the cards, and the list pages and
  searches itself. `Browse` uses it: a heading, the filter rail and the live cards.
- `SideSummary` is the main region with a narrow summary beside it: line items left, the totals and the one thing to do next right. It
  rearranges for a compact width. `Basket`, `ProductPage` and `OrderTracking` use it and fill all four slots.
- The template's content is the *designed first draft* the template editor draws. A screen that uses the template fills every slot it
  shows: what a screen puts in a slot is what is drawn there, and a screen preview draws only what the screen filled - a slot the screen
  leaves out is not drawn from the template's draft. `Browse` therefore fills `filters` (the same filter builder as the template's) as well as
  `header` and `products`, and `Basket`, `ProductPage` and `OrderTracking` fill all four slots of `SideSummary`.

See [layout arrangement](layout-arrangement.md).

## Custom components: `@acme/scene.commerce`

The `Acme.Commerce` package is plain ES modules with React left external, so the host's own React is used and there is no build
step. It declares three components, authored as `component Acme.Commerce.<name>`:

| Component | Used by |
| --- | --- |
| `productCard` | Storefront Browse (the product grid) and the Catalog product page |
| `stockBadge` | Warehouse stock levels |
| `orderTimeline` | the Sales order page |

```text
component Acme.Commerce.productCard card
  property name from query ProductById.name
  property sku from query ProductById.sku
  property price from query ProductById.price
```

The package declares itself in `index.js`: the export `acmeCommerceRuntimePackage` holds a manifest (`Acme.Commerce`, version
`1.0.0`, kind `ComponentLibrary`, components `productCard`, `stockBadge`, `orderTimeline`) and the three components keyed
`Acme.Commerce:<component>`.

Rendering a profile that names a package the host does not ship needs an *approved package set*: a JSON file that says which
npm module carries which components (the format is in the Scene documentation, "Approved package sets"). `render-variant.sh`
reads it from `Samples/AcmeCommerce/tools/packages/acme-commerce.package-set.json` when `PACKAGE_SET=1`; it names the npm module
`@acme/scene.commerce`, the export `acmeCommerceRuntimePackage` and the package's style sheet. With it, `cratis render` adds
`@acme/scene.commerce` to the generated application's dependencies and writes its approved packages file. The repository ignores
every `packages` folder (`**/packages/*` in `.gitignore`), so this one file is added with `git add -f`; a fresh checkout has it.

## Compile, test, package, render and run

```bash
# the whole application: 56 files, 0 errors, 0 warnings
screenplay --warnaserror Samples/AcmeCommerce

# every specification
screenplay test Samples/AcmeCommerce
```

`--warnaserror` turns warnings into failures; the sample is held at zero. The compiler reports 19 informational
diagnostics (`PLAY0469`: an event copies a command identifier into its payload although that identifier already identifies the
event source), which are not warnings. `screenplay test` ran 169 specifications: 168 pass and 1 is reported *unsupported* -
`ReceivingAnOrderFromTheStorefront` in Sales, because the reference evaluator cannot use the composite capture field `lines` as a
scalar. The exit code of `test` is therefore 3, not 0. `cratis screenplay validate Samples/AcmeCommerce` reports the model valid as well.

### Packaging a domain for Studio

Studio imports one event model per domain. `tools/domain-package.sh` assembles a single domain as a standalone model - the
`domain` line, `Shared/` and the domain folder - proves that it compiles with
`--warnaserror` and passes its specifications, and writes `<Domain>.zip`:

```bash
SCREENPLAY=screenplay Samples/AcmeCommerce/tools/domain-package.sh Catalog /tmp/acme-commerce-packages
```

For Catalog that is 11 files compiled with 0 errors and 0 warnings, 20 specifications passed, and a ZIP of 16 files. `SCREENPLAY`
names the compiler command (the default is `screenplay` on the path).

A standalone domain has no name clash, so the package gives every inbound contract its *published* name back
(`ProductPublishedAtStorefront` becomes `ProductPublished`); set `KEEP_LOCAL_NAMES=1` to keep the local names.

### Rendering it

`tools/render-variant.sh` builds a renderable variant and runs `cratis render`:

```bash
Samples/AcmeCommerce/tools/render-variant.sh                 # custom components replaced by core tables
PACKAGE_SET=1 Samples/AcmeCommerce/tools/render-variant.sh   # with @acme/scene.commerce, approved by the package set
```

It exists because Stage cannot render everything the language says yet. The variant is **Catalog and Storefront** by default
(`DOMAINS="Catalog Storefront"`); the header of the script lists every difference from the source and why:

- Stage renders state-change, state-view and automation slices, not `Translate` slices or public events. The variant turns
  each inbound private event into a bridge command, then deletes `translation/`.
- Stage refuses `reaction batches atomic` and protected `reads`; the variant strips them and drops the specifications that
  exercised a removed guard.
- Without `PACKAGE_SET=1` the three custom components are replaced by core tables and the package is removed from the profile.

For Catalog and Storefront this was taken all the way (with `cratis` 3.75.0, as recorded in the sample README):
`cratis render --package-set`, `dotnet build`, `npm run build`, the generated `dotnet test` (90 of 90), and a browser walk of the
screens with the hierarchical sidebar, the Shop topbar override and the toolbar button. Sales, Purchasing and Warehouse are not in the rendered variant: a reaction that invokes a command which
gained inputs from a removed `reads` cannot be satisfied from its trigger payload.

### Running on Stage

The rendered application is built and tested like any other application:

```bash
cd <render destination>
dotnet build
npm install
npm run build
dotnet test AcmeCommerce.Specs
```

`cratis run [PATH]` starts a local Stage sandbox for a `.play` file or a folder; its options include `--port`, `--package-set`
and `--packages` for approved custom packages. The sample README records that a live Stage cannot load a custom package that
was not built in, and the renderer limits above apply to Stage too: it does not run `Translate` slices or public events. This
page therefore makes no claim that the whole five-domain application runs on Stage; what is proven is the Catalog and Storefront
variant above.

## What each Studio feature shows

The Studio project is filled in two ways. The Screenplay **import** brings in what the `.play` files say, one event model per
domain. The **loader** adds what only Studio holds: the project skeleton, personas, brainstorming boards and sticky notes. See
[Share a public model with Studio](see-your-event-model.md) for the import and export of a model.

| Studio feature | What the sample shows |
| --- | --- |
| **Event model board** (one per domain) | the slices of the domain as an event model: commands, events, read models and screens, with the specifications and the sample data behind the screens |
| **Public events** | the twelve contracts, each in its publisher and, under its consumer-local name, in every consumer |
| **Concepts board** | the shared concepts and the domain's own, with their validation rules; the loader adds sticky notes saying why the rules exist |
| **Constraints board** | the eleven constraints of the sample, with notes on why the rules exist |
| **Event sources** | the shared event sources and their streams |
| **Personas and policies** | the seven personas with their application roles; the policies each persona holds |
| **UI profile** | the shell: the AppShell root template, the Acme theme, the packages |
| **Brainstorming boards** | "Acme big picture" (Storefront domain) and "How we got to <Domain>" for each domain: a chaos storm on the left, then a column per lane, the public events, the rules and why they exist, what became the event model, and what was parked on purpose |
| **Sticky notes** | the open questions, risks, decisions and assumptions on those boards, and the explanations on the concept and constraint boards |
| **Chats** | one conversation per board, in which the personas debate a rule (for instance the SalesRep and SalesManager on a discount over the rep's limit) |

Sticky notes in Studio have text only, no colour. The loader therefore prefixes each note with its kind - `[QUESTION]`,
`[RISK]`, `[DECISION]`, `[ASSUMPTION]` or `[NOTE]` - so every note reads in plain text.

The Studio project is loaded in stages (skeleton, import, studio-only, verify), and the loader's `verify` stage reads back what
is present and what is missing. Some of what the table lists depends on importer changes that are still in review (next
section), so a page in Studio can show less than the table says; the `.play` source is the reference.

### Known gaps

Not every construct comes through the Studio import yet. Each gap is tracked in `Cratis/StudioIssues`; the state below is the
state of the issue and of its pull request when this page was written.

| Gap | Issue | State |
| --- | --- | --- |
| Public events, event origins and Translate slices (direction, captures, outbound projections) | StudioIssues #758 | open; the import pull request is open |
| Automation reactions, triggers, schedules and what they produce | StudioIssues #760 | open; the import pull request is open |
| Personas, `authorize` use sites and `given caller` | StudioIssues #762 | open; the persona and the access parts of the import are merged |
| The UI profile, theme, root template and own templates | StudioIssues #763 | open; the pull request for theme and packages is open |
| Import hardening: enum and Int concepts, keyed stream routes, placed-file imports, `.strings` | StudioIssues #764 | open; placed files, `.strings` and the latest event generation are merged, whole-number and enum concepts are open |
| Cross-domain screen navigation and several event models in one Play session | StudioIssues #765 | open |
| Brainstorming board tools for regions, chats and layout; sticky notes on persona and event-source boards | StudioIssues #766 | open |

The loader works around what it must until these land: it adds the personas and application roles itself, and it lays out the
concept and constraint boards (an imported card is placed at the origin, StudioIssues #775). Look at a Studio page before you rely
on it: where the import does not carry a construct yet, the `.play` source is the truth.

## Related

- [Folders](folders.md) and [imports](imports.md): composing many files into one application.
- [Reactions](reactions.md), [captures](captures.md) and [constraints](constraints.md).
- [Screens](screens.md), [templates](templates.md), [navigation regions](navigation.md) and [sample data](sample-data.md).
- [Compiler and CLI](tool.md).
