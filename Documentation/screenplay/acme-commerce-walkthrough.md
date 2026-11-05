---
title: "Cratis Screenplay: trace the Acme Commerce sample"
description: "Trace products, orders and stock across five contexts in the Acme Commerce Screenplay sample, then inspect its rules, specifications and screens."
---

<a id="acme-commerce-a-walkthrough"></a>

Acme Commerce is a Cratis Screenplay sample for following a business flow across five bounded contexts: Catalog, Storefront,
Sales, Purchasing and Warehouse. A bounded context owns its language and rules. The sample connects those contexts through
public events, letting you follow a product from approval to checkout, reservation and dispatch without treating every context
as one shared model.

Open [`Samples/AcmeCommerce`](https://github.com/Cratis/Screenplay/tree/main/Samples/AcmeCommerce) in a Screenplay checkout.
You can follow the source with a text editor; the [event model board](see-your-event-model.md) offers a visual route. The
sample's [README](https://github.com/Cratis/Screenplay/blob/main/Samples/AcmeCommerce/README.md) describes its layout and helper
scripts. You do not need a Studio account to read the model or use the Screenplay compiler.

Choose the part you want to understand:

- [Follow the public events](#the-public-events-p1-to-p12) to trace a product, order or replenishment request across contexts.
- [Read a domain's rules](#domain-by-domain) to find the command, state and specification behind a decision.
- [Inspect the screens](#screens) to see how forms and templates give a person access to those commands.
- [Verify or consume the model](#compile-test-package-render-and-run) to distinguish compiler checks, specification execution, generated code and a sandbox.
- [Use Studio for shared review](#what-each-studio-feature-shows) if you want to discuss the model with others.

| Context folder | Module | Start with this task |
| --- | --- | --- |
| `Catalog/` | `Merchandising` | Approve a product for sale and publish its contract |
| `Storefront/` | `Shop` | List a product, fill a basket and check out |
| `Sales/` | `Orders` | Decide whether an order can be approved and confirmed |
| `Warehouse/` | `Stock` | Reserve stock, plan a shipment and dispatch it |
| `Purchasing/` | `Buying` | Replenish stock and receive a purchase order |

## An order's lifecycle and the process it sets off

To study an order within Storefront, open `Storefront/model/lifecycle.play`. It declares two top-level lifecycle constructs
in version 36 of the executable semantic model (ESM): a [state machine](statemachines.md) for where the order is, and a
[workflow](workflows.md) for the work that follows.

`OrderLifecycle` follows the `OrderProgress` read model from Placed through Paid, Packed and Shipped to Delivered, with
Cancelled and Refunded outcomes. Entering Paid starts `FulfilOrder`; entering Shipped sends `NotifyCustomer`.

`FulfilOrder` attempts to reserve stock up to three times. It then chooses between creating a parcel and seeking backorder
approval from a `WarehouseClerk` within a day. It waits up to two days for the carrier and escalates a failure or expiry.
Read its specifications beside the workflow to compare those paths. The commands and events it names live in
`Storefront/model/fulfilment.play`.

These lifecycle declarations describe Storefront's process. They do not replace the public-event contracts between the five
contexts below. The [specification runner](#compile-and-run-specifications) checks their modeled expectations; a consumer must
separately support the constructs before it can execute them.

## Where to find it

All paths in this walkthrough are relative to `Samples/AcmeCommerce` unless a command starts at the repository root.
Keep the whole sample folder together: a domain's slices refer to declarations in `Shared/` and its other files.

| What you want to inspect | Where to open it |
| --- | --- |
| Application identity and execution declarations | `application.play` |
| Shared value types and access rules | `Shared/concepts.play`, `Shared/access.play`, `Shared/eventsources.play` |
| A context's commands, views and specifications | `<Domain>/model/` |
| Public-event publication and reception | `<Domain>/translation/` |
| A context's shell and design data | `<Domain>/shell/` |
| Packaging and rendering adaptations | `tools/domain-package.sh`, `tools/render-variant.sh` |

[Cratis Studio](https://app.cratis.studio/) is an optional commercial workspace for collaborative modeling. Its published
access route offers self-service registration and a 14-day trial. Use a project your account can access; this walkthrough does
not require membership in the Cratis organization or access to a hosted Acme Commerce project. It supplies no project or
application IDs for the account-specific routes `https://app.cratis.studio/projects/<projectId>` and
`https://app.cratis.studio/projects/<projectId>/application/<applicationId>`.

<a id="how-it-was-built"></a>

## Follow the source in order

Read from the shared declarations into the contexts that use them. `Shared/` establishes names and rules that the contexts
agree on. Catalog publishes the product contracts; Storefront translates them into listings and publishes an order after
checkout. Sales, Warehouse and Purchasing handle approval, fulfillment and replenishment.

Once you can trace that flow, inspect a domain's `shell/` folder and the screens in `model/` to see how people reach its
commands. The helper scripts package individual domains or adapt a copy for rendering. They are separate from the canonical
`.play` source, and their output is not evidence that every consumer supports the original model.

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

The root `application.play` contains the only `domain AcmeCommerce` declaration. Compiling the [folder](folders.md) composes
its `.play` files into one application, so `Shared/` needs no `..` import from each domain.

Events need distinct names within that application. The consumer therefore declares a local name such as
`ProductPublishedAtStorefront`; `from "Catalog"` records its origin. Reusing the publisher's event name on the consumer fails
binding (`PLAY0472`). The per-domain packaging script can restore the published name because each package is a separate tree.

A read model's first event initializes its required properties. If the specification runner reaches a projection it cannot
execute, it reports that scenario as unsupported, including scenarios that depend on the affected read model. Unrelated
specifications can still run.

## The shared kernel

Start in `Shared/concepts.play` to see which values the contexts agree on. A [concept](concepts.md) gives a value a business
name and validation rules. Commands, events and read models that use `Sku` share these rules:

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

`Shared/eventsources.play` declares the [event sources](event-sources.md): the identities whose histories the model records,
such as `Product`, `Customer`, `Basket`, `Order` and `Supplier`. Named streams distinguish histories where one identity has
more than one.

`Shared/access.play` declares the [policies](policies.md) (`IsShopper`, `IsSalesManager`, `CanCurateCatalog`, `CanBuy`,
`CanWorkWarehouse`, ...) and the personas that hold them: Shopper, Merchandiser, SalesRep, SalesManager, Buyer and
WarehouseClerk. These represent the people who work in the modeled shop. A module names the policy that admits them:

```text
policy CanCurateCatalog
  require role "Merchandiser"
```

The modules also name why they handle personal data through a [`purpose`](purposes.md): `ShopOrderProcessing` in Storefront,
`OrderFulfilmentProcessing` in Sales and `SupplierRelations` in Purchasing.

For the shared visual declarations, open `Shared/theme.play`. It declares the `Acme` theme and the packages it is compatible
with, including the custom package `Acme.Commerce`.

## The public events (P1 to P12)

Use this table as the route map between contexts. P1 to P12 are labels used in the walkthrough; the event names are the
contracts declared in the source. The modeled contexts exchange these public events, then translate them into private facts.

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

For a first pass, trace the catalogue flow (F-A). Open `Catalog/translation/publish.play`, find `ProductPublished`, then open
`Storefront/translation/listing.play` and find `ProductPublishedAtStorefront`. Follow its capture to `ProductListed` and the
listing read model. This connects the public contract to what a shopper can see.

Next follow checkout through P4, order confirmation through P5 and the reservation results P7 or P8. That order-to-dispatch
flow (F-B) returns status through P6 and shipment information through P12. To study replenishment (F-C), follow P10 from
Warehouse into Purchasing, then P11 back to Warehouse. A price change (F-D) follows P2 from Catalog to both Storefront and
Sales.

### Private versus public events

Private events record what a context has done in its own language: `ProductApprovedForSale`, `OrderConfirmationIssued` or
`ReservationMade`. The `Translate` slices publish the contracts other contexts consume. Follow those contracts across a
boundary, rather than reaching into the publisher's private events.

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

For a many-to-one example, read `Sales/translation/publish.play`. `PublishOrderStatus` maps the private approved, rejected,
on-hold, shipped, fulfilled and cancelled events to the same public `OrderStatusChanged` contract, setting `status` for each.

### Inbound translation: the anti-corruption layer

An inbound translation is an anti-corruption layer: it keeps the publisher's language from becoming the consumer's internal
model. Storefront declares the foreign event, then captures it into a private event in the shop's language. Its product
captures use `key sku` because the shop addresses a product by its SKU:

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

Pick the context whose decision you want to understand. Each section points to a starting file and the events that connect
it to the rest of the model.

### Catalog (`Merchandising`)

To understand why a product cannot yet be sold, open `Catalog/model/publication.play`. `ApproveProductForSale` reads
`ProductReadiness` and refuses with "Describe the product before approving it" or "Price the product before approving it".
Trace that state back to `DescribeProduct` and `SetProductPrice`, then compare the successful and refused specifications.

`UniqueSku` prevents duplicate SKUs on `ProductDrafted`. `OneApprovalPerProduct` prevents another `ProductApprovedForSale`
until `ProductWithdrawn` releases the rule. The translation slices `PublishProduct`, `PublishPriceChange` and
`PublishRetirement` publish P1, P2 and P3 so consumers need not know those internal decisions.

### Storefront (`Shop`)

Start in `Storefront/translation/listing.play` to follow `ListProduct`, `UpdateDisplayPrice`, `DelistProduct`,
`UpdateAvailability` and `TrackOrderStatus`. These consume P1, P2, P3, P9 and P6 in Storefront's language.

Then open `Storefront/model/basket.play`. `AddToBasket` reads the listing and refuses a sold-out product. In
`Storefront/model/checkout.play`, the wizard's three steps (`EnterShippingAddress`, `ChooseDeliveryMethod`, `CheckOut`) use
command [forms](forms.md). `CheckOut` reads `BasketLineCount` and refuses an empty basket. `PublishOrder` translates
`CheckoutCompleted` into P4 for Sales.

To inspect the shopper's pages, open `Storefront/model/templates.play`. `ProductGrid` and `SideSummary` give the shop its own
page shapes. `Storefront/model/module.play` overrides the application's topbar with Browse, Basket and My orders.

### Sales (`Orders`)

Follow P4 into `Sales/translation/receive.play`, then read `Sales/model/approval.play`. `SmallOrderAutoApproval` invokes
`ApproveOrder` when `quotedTotal < 500`; larger orders need a separate approval decision. Approval leads to `ConfirmOrder`,
whose private `OrderConfirmationIssued` becomes public `OrderConfirmed` (P5).

The approval commands still have access rules. `ApproveOrder`, `RejectOrder` and `ApplyLargeDiscount` require
`authorize IsSalesManager`; a SalesRep's `ApplyDiscount` stops at 15 percent. Compare their specifications to see the difference
between a rejected business decision and an authorization denial.

`OneOpenOrderPerBasket` constrains `OrderReceived`, and `OneConfirmationPerOrder` prevents a second confirmation. The former
lives outside the inbound slice because a constraint referring to an inbound-captured event cannot live there (`PLAY0616`).
For the return path, `Sales/translation/fulfilment.play` receives P7, P8 and P12; `PublishOrderStatus` publishes P6.

### Warehouse (`Stock`)

Open `Warehouse/translation/inbound.play` to see how a published product becomes a SKU, an order confirmation requests a
reservation and a purchasing receipt adds stock. In `Warehouse/model/reservations.play`, `ReserveStockReaction` invokes
`ReserveStockUnits`, which produces `ReservationMade` on success. If validation refuses the command, the reaction's
`on refused by validation` branch produces `ReservationFailed`. Follow that branch for P8; the successful reservation becomes P7 for Sales.

Follow the remaining automations to understand the physical flow. `PlanShipmentWhenReserved` plans a shipment;
`TakeStockWhenDispatched` removes dispatched stock. `WatchReorderPoint` raises `ReorderPointCrossed` once per crossing and
re-arms when stock returns. Its publication, P10, is Purchasing's signal to replenish. `UniqueLocationCode` and
`OneDispatchPerShipment` guard the corresponding commands.

### Purchasing (`Buying`)

Start with `Purchasing/translation/inbound.play`: `RecordDemand` consumes P5 and `RequestReplenishment` consumes P10.
In `Purchasing/model/replenishment.play`, `AutoReplenish` requests a one-line purchase order for the SKU's preferred supplier.
`OneOpenReplenishmentOrderPerSku` prevents another while one is open; goods received or cancellation releases it. Without a
preferred supplier the model records `ReplenishmentBlocked`, shown as status `blocked` in the replenishment read model.

Supplier offers use a generated `SupplierOfferId`, giving each offer its own event-source identity. Other constraints include
`UniqueSupplierTaxId`, `UniquePurchaseOrderNumber` and `OneSubmissionPerPurchaseOrder`. Follow `PublishGoodsReceived` in
`Purchasing/translation/publish.play` to P11, which returns the received goods to Warehouse's stock flow.

### Constraints

Read a [constraint](constraints.md) beside the command that appends the event it guards. For example, Catalog's SKU rule is:

```text
constraint UniqueSku
  unique sku on ProductDrafted
  message "That SKU is already in the catalogue"
```

`unique event X` makes the event happen once per event source; `released by Y` names another slice's event that lifts the rule
again.

### Specifications

Use the [specifications](specifications.md) beside a slice to check the decision you just traced. A command scenario can give
a caller (`authenticated`, `role "..."`) and prior events, then expect events or a refusal. A view scenario supplies events and
expects a read model.

A specification checks the slice that declares it. `SettingAPrice` expects `ProductPriceSet`; the outbound translation checks
`ProductPriceChanged` separately. For an automation, `then invokes` states the handoff to a command. That command's slice
checks what it appends. Here, the expected result is an invocation of `ApproveOrder`:

```screenplay
specification ASmallOrderApprovesItself
  when append OrderReceived
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
    orderId     = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
    customerId  = "c2a4e6f8-1b3d-4f5a-9c7e-2d4f6a8b0c01"
    email       = "ada@example.com"
    market      = "NO"
    quotedTotal = 98
  then invokes ApproveOrder
    orderId = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
```

Read the received, approved, confirmed and published scenarios together to follow the chain. Passing these slice-level
checks does not establish cross-context delivery or a working deployment.

## The shell

### One application per domain

The combined source is one application for tracing the whole model. Packaging a domain gives it a separate application root
and its own shell: Acme Shop, Acme Catalog, Acme Sales, Acme Purchasing or Acme Warehouse. Inspect `<Domain>/shell/` to compare
the logo, theme, navigation and sample data without changing the business flow.

The domains share `Cratis.Components` and use the foundation blueprint `Cratis.Blueprint.Default`. Their different shell
settings let the shopper's storefront and the warehouse work area present the tasks differently.

### The UI profile

`<Domain>/shell/shell.play` declares the theme and `ui profile`: the target, packages, root template and appearance. Catalog,
Sales, Purchasing and Warehouse reference the blueprint's `AppShell` directly, pinned at `^1`. Storefront names
`StorefrontShell`, declared in `Storefront/model/shell.play`, as its root template. Its topbar includes a basket indicator
bound to `AllBasketSizes.0.lineCount`, so you can trace a shell element back to the basket read model.

The following reduced example illustrates a direct blueprint root rather than Storefront's own shell. Use the checked-out
files when compiling the sample:

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
| Storefront | `#C2410C` | own shell | own shell | light |
| Catalog | `#4338CA` | `static` | light | light |
| Sales | `#1D4ED8` | `static` | dark | light |
| Purchasing | `#B45309` | `slimPlus` | light | light |
| Warehouse | `#0F766E` | `compact` | dark | dark |

The profile also carries `identity` and `appearance` declarations. For example, Storefront names the application "Acme Shop"
and sets round corners and `source-serif-4`. Read those alongside `navigation.play`, where the topbar supplies its logo.

### Sample data

`<Domain>/shell/sampledata.play` supplies [sample data](sample-data.md) packs such as `AcmeStorefront` and `AcmeCatalog`, with
locale `nb-NO`. Use these rows to inspect a screen's design without first producing every business event. They are excluded
from generated production artifacts. The CLI can separately opt a disposable sandbox into deriving seed events from supported
sample-data shapes with `--sample-data`; that does not seed production data. The Storefront pack authors `BasketContents.lines` as two nested rows of the declared `BasketLine` type, matching the cart indicator's two lines and total of 647. A bare `lines` field without rows means an explicit empty list; leaving it out means generated data. These collection values require matching Stage/Studio conversion support for preview.

### Navigation and the logo

Open `<Domain>/shell/navigation.play` for the [navigation regions](navigation.md). It declares the topbar and a sidebar grouped
by module and feature. The logo is an inline SVG data URI, `logo image "data:image/svg+xml;base64,..."`, so the model does not
need to load a separate logo file. `tools/embed-logos.py` derives those values from `assets/logos`.

A module can inherit or replace the application's navigation:

| Module | Statement | Effect |
| --- | --- | --- |
| `Shop` | `navigation topbar overrides` | the storefront has its own light topbar |
| `Merchandising`, `Orders`, `Buying`, `Stock` | none | inherit the application's navigation |

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

Compare the application-level button with the screen's button before adding another action: the first is available across
pages, while the second belongs to the product list.

## Screens

Screens fill [templates](templates.md). The sample uses the foundation blueprint templates by reference, for example
`template blueprint Cratis.Blueprint.Default ListScreen version "^1"`:

| Template | Task it presents |
| --- | --- |
| `QuickCreateDialog` | Enter command input, such as a new product or stock adjustment |
| `ConfirmDialog` | Confirm an action, such as approving a product or packing a parcel |
| `ListScreen` | Work through products, an approval inbox, purchase orders or shipments |
| `RecordPage` | Inspect one product, order, supplier or stock item |
| `DashboardScreen` | Read a domain's summary figures |
| `BoardScreen` | Review pricing, the orders pipeline or the picking queue |
| `FormScreen` | Describe a product or set its price |
| `WizardScreen` | Check out or create a purchase order in steps |
| `SideSheet` | Record goods received |
| `MasterDetailScreen` | Work with suppliers and their details |
| Own `ProductGrid`, `SideSummary` | Browse products or inspect a basket, product or order |

Command input is a [form](forms.md) declared once and found by its command (`form DescribeProductForm for DescribeProduct`),
with a two-column layout where it helps.

<a id="every-command-has-its-own-screen"></a>

### Connect a person's task to a command

Commands that a person runs have screens in their slices: usually a `QuickCreateDialog` for input or a `ConfirmDialog` for
confirmation. Commands invoked by automations, such as clearing the basket after checkout or reserving stock units, need no
person-facing screen.

To find how a person reaches a command, look for these surfaces:

| Surface | Example | Written as |
| --- | --- | --- |
| toolbar button that opens the command's dialog | Catalog "New product", Purchasing "New purchase order", "Register supplier", Warehouse "Register SKU" | `button new title "New product" slice Merchandising.Products.DraftProduct` |
| action on the selected row of a list | Warehouse "Adjust stock", "Count", "Dispatch"; Purchasing "Choose supplier" | `action AdjustStock label "Adjust stock"` under a `toolbar` |
| state-dependent action on the selected row | Catalog "Approve for sale" and "Withdraw" on the product lists; Warehouse "Pick order", "Pack", "Cancel shipment" | `action "Approve for sale"` with `when item.status == "draft" execute ApproveProductForSale` and `otherwise hidden` |
| state-dependent action on a record page | product page, order page ("Release hold" only while on hold, "Mark fulfilled" once shipped), purchase order ("Archive" once received or cancelled) | the same guarded `action`, beside the `data` of the one record |
| reaction | confirm an order after approval, clear the basket after checkout | `reaction ... invokes Command` |

### The domain's own templates

Storefront declares its own templates for the pages that define the shop. Open `Storefront/model/templates.play` to inspect
`ProductGrid` and `SideSummary`. Each template declares named slots, an arrangement and initial component content. This gives
the design a useful starting shape before a screen binds its own data:

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
      property rowActions
        item "Add to basket"
      ...
```

`ProductGrid` places a filters rail beside the product cards. Its filter builder narrows the list, which also supplies paging
and search. Compare it with `Browse`, where `header`, `filters` and `products` bind that shape to Storefront's data. The slot is
named `products`, not `grid`: in an arrangement, `grid` names a layout kind.

`SideSummary` puts the main content beside a narrow summary and rearranges for compact widths. `Basket`, `ProductPage` and
`OrderTracking` fill its four slots. Follow their query bindings to find the read models each screen needs; then use
[layout arrangement](layout-arrangement.md) to change the shape without changing those data contracts.

### Blueprint templates at the feature level

A feature can reference a blueprint template instead of declaring its own. In `Catalog/model/publication.play`, Publication
uses `ListScreen` from `Cratis.Blueprint.Default`, pinned at `^1`:

```text
feature Publication
  template blueprint Cratis.Blueprint.Default ListScreen version "^1"
```

| Domain | Feature | Template | Why it fits |
| --- | --- | --- | --- |
| Catalog | Publication | `ListScreen` | the approval list and its feature toolbar (Approve product, Withdraw product): `ListScreen` has the `toolbar` slot |
| Catalog | Pricing | `BoardScreen` | the pricing board |
| Catalog | Reporting | `DashboardScreen` | the catalogue dashboard |
| Purchasing | PurchaseOrders | `ListScreen` | the purchase orders list |
| Purchasing | Overview | `DashboardScreen` | the purchasing dashboard |

Check `fits slot` when nesting templates: the child must fit an outlet the parent exposes. The sample assigns foundation
templates to these features and supplies screen content bound to its own queries and commands. When choosing a different
blueprint, check its required slots and data contracts too. A template's appearance alone does not establish that its queries
can be answered by Acme Commerce's read models.

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

A host needs an approved package set before it can load a package it does not ship. This JSON configuration maps the
component declarations to an npm module, its export and styles. The sample's set is
`tools/packages/acme-commerce.package-set.json`; it names `@acme/scene.commerce`, `acmeCommerceRuntimePackage` and the style sheet.

`render-variant.sh` uses that set when `PACKAGE_SET=1`. For a sandbox, `cratis run` also accepts `--package-set` and `--packages`;
the latter must point to installed browser-package folders. Approval alone does not supply the package files. See the
[CLI's custom-package instructions](/cli/reference/run/#custom-packages) for the required layout.

## Compile, test, package, render and run

Choose the check that answers your question. Compilation checks the composed source. Specification execution checks modeled
expectations. Rendering generates implementation artifacts, while `cratis run` starts a disposable sandbox. Success on one
path does not establish support on another.

### Compile and run specifications

Run these commands from the repository root with the [Screenplay CLI](tool.md) installed:

```bash
# the whole application
screenplay --warnaserror Samples/AcmeCommerce

# every specification
screenplay test Samples/AcmeCommerce
```

`--warnaserror` makes warnings fail compilation. Read its diagnostics before moving on to `screenplay test`, whose output
separates passed, failed and unsupported scenarios. Unsupported means the runner could not execute the scenario; it is not a
pass. Use the current report rather than a specification count copied from this page. The [CLI guide](tool.md) explains exit
codes and the difference between validation and specification execution.

If you edit a domain file in isolation and see missing declarations, check that you are compiling `Samples/AcmeCommerce`,
not just that file. Its shared concepts, events and read models can live elsewhere in the tree.

### Packaging a domain for Studio

To prepare one context for an import, use `tools/domain-package.sh`. It assembles a standalone tree from the root declarations,
`Shared/` and the chosen domain, invokes compilation and specifications, then writes `<Domain>.zip` to the output directory.
The script replaces an existing package of the same name, so choose an output directory for disposable copies:

```bash
SCREENPLAY=screenplay Samples/AcmeCommerce/tools/domain-package.sh Catalog /tmp/acme-commerce-packages
```

`SCREENPLAY` names the compiler command; it defaults to `screenplay` on `PATH`. The script prints only the last line of each
check. A ZIP is not proof that compilation and all specifications passed: run the checks on the packaged folder separately
when you need the full results. For opening the model in Studio, continue with [the event-model viewing guide](see-your-event-model.md).

A standalone domain has no name clash, so the package gives every inbound contract its *published* name back
(`ProductPublishedAtStorefront` becomes `ProductPublished`); set `KEEP_LOCAL_NAMES=1` to keep the local names.

### Rendering it

`tools/render-variant.sh` copies selected domains into an adapted model, then invokes `cratis render`. It needs the Cratis CLI
and Python 3 on `PATH`. Review its header before using the result: the variant deliberately differs from the authored model.

The default selection is `DOMAINS="Catalog Storefront"`, but both commands below currently fail before reaching
`cratis render`. The script removes Sales, then unconditionally opens `Sales/model/approval.play` while processing
`tools/render-bridge/drop-specifications.txt`. `PACKAGE_SET=1` does not avoid this missing-file failure. The default needs a
separate script fix before it can be used.

These are the helper's invocation forms from the repository root, not working default rendering commands:

```bash
Samples/AcmeCommerce/tools/render-variant.sh                 # custom component blocks and package references removed
PACKAGE_SET=1 Samples/AcmeCommerce/tools/render-variant.sh   # with @acme/scene.commerce, approved by the package set
```

The script also accepts Sales, Purchasing and Warehouse through `DOMAINS`, including all five together; selecting them does
not establish render support. Its main adaptations are:

- Translation files are removed. Bridge commands stand in for captured private events; no connecting reactions are generated.
- The Purchasing dashboard's performer query becomes an event projection; the Storefront basket's calculated `addedValue`
  contribution is omitted.
- Listed specifications are dropped. Without `PACKAGE_SET=1`, custom component blocks and package references are removed; no replacement tables are generated.

The helper optionally copies `tools/render-bridge/bridge.play.txt`, but that file is absent from the sample. The generated
bridge commands require explicit wiring to connect contexts; the variant does not supply the cross-context flow traced above.

Protected command reads and `reaction batches atomic` are preserved by default. `STRIP_READS=1` selects an older adaptation
that removes reads and guards; do not treat that variant as equivalent to the decision rules you inspected above.

The script's removal of translations is not a current blanket Stage limit. Stage renders a supported subset of inbound
captures and outbound public-event projections. Unsupported shapes and specifications receive diagnostics. Review the
[renderer admission guide](/screenplay/stage/guides/build-renderer-target/) when deciding whether an adaptation is still
needed. Neither rendering nor isolated generated handler tests establish delivery between deployed contexts.

After rendering, check the generated application separately:

```bash
cd <render destination>
dotnet build
npm install
npm run build
dotnet test AcmeCommerce.Specs
```

These checks apply to the generated variant, not to the unmodified five-context model. Review the generated code and restore
or implement any behavior your application needs before considering deployment.

### Running on Stage

To try the model interactively, `cratis run [PATH]` starts a local Stage sandbox for a `.play` file or folder. Send a modeled
command and inspect the resulting state to compare the behavior with the specification you read. The sandbox runs supported
behavior and is not a production host.

The [CLI run reference](/cli/reference/run/) covers Docker setup, `--port`, approved custom packages and admission output. Inspect that
output for unsupported commands, queries or lifecycle constructs before relying on the session. The render-variant helper
and a sandbox have different execution paths; generating one variant does not establish that the full Acme Commerce source
can run unchanged.

<a id="what-each-studio-feature-shows"></a>

## Review the model in Cratis Studio

Use Studio when the next task is a shared review with developers, domain experts or product owners. Its event-model boards
bring commands, events, read models, specifications and screens into a collaborative workspace. Brainstorming boards and
notes let you record decisions, assumptions and risks alongside that discussion.

For Acme Commerce, choose one decision to review rather than starting with every context. Product approval is a useful first
case: compare the missing-description and missing-price refusals with the successful approval, then follow the published
contract into Storefront. Use the [event-model viewing guide](see-your-event-model.md) to open a model and the source paths
above to check the declarations behind it. This is a review route, not a promise that a hosted sample project contains every
board or note described by the source.

<a id="known-gaps"></a>

### Check the model before relying on it

A board shows declarations and design; it does not establish that specifications passed or that a deployed integration
works. Compare the model you opened with the `.play` source, especially at public-event boundaries and authorization rules.
Keep the compiler report, specification results and consumer admission report separate when deciding what has been checked.

For your next change, select one command and add or revise its successful and refused [specifications](specifications.md).
Then validate the whole sample folder. If the change crosses a context boundary, review the corresponding public contract and
both translations before moving on to implementation.

## Related

- [Folders](folders.md) and [imports](imports.md): composing many files into one application.
- [Reactions](reactions.md), [captures](captures.md) and [constraints](constraints.md).
- [Screens](screens.md), [templates](templates.md), [navigation regions](navigation.md) and [sample data](sample-data.md).
- [Compiler and CLI](tool.md).
