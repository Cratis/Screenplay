# Acme Commerce (sample)

A mid-size shop in five bounded contexts - **Catalog, Storefront, Sales, Purchasing, Warehouse** - in ONE Screenplay
application. The contexts never share a database. They talk only through *public events*, and every context translates
what it receives into its own language (anti-corruption), so a change in one language never leaks into another.

The full story - how it was built, domain by domain, and what each Studio feature shows - is in the
[walkthrough](../../Documentation/screenplay/acme-commerce-walkthrough.md). The Studio project is the permanent keeper project
**Acme Commerce** (application **Acme Commerce Shop**) in the Cratis tenant of Cratis Studio, filled by the loader in
`scripts/samples/acme-commerce` of the `Cratis/Studio` repository.

It is the epic sample for Studio, Screenplay, Scene and Stage: everything the language says about concepts, constraints,
personas, policies, event sources, public events, screens from blueprint templates, a UI profile and sample data is said
here, in text. Nothing lives only in Studio (rule REN2).

```bash
screenplay --warnaserror Samples/AcmeCommerce      # the whole application: 0 errors, 0 warnings
screenplay test Samples/AcmeCommerce               # every specification
```

## Layout

```
application.play                the ONLY `domain AcmeCommerce` line; `reaction batches atomic`, `command reads protected`
Shared/                         the shared kernel: concepts with their validation rules, enums with pinned numbers,
                                composite types, ALL event sources, policies and personas, the Acme theme
Shell/                          the ui profile (blueprint AppShell root pinned ^1, theme, packages, the one sample-data
                                pack), the application navigation (hierarchical sidebar module -> feature, topbar)
Application/                    the Forbidden / NotFound / Error screens and the client-error report behind them
<Domain>/model/                 state changes, state views, automations, forms, screens, specifications;
                                module.play is the skeleton (module description, authorize, features, navigation mode)
<Domain>/translation/           Translate slices: outbound publication, inbound anti-corruption
_packages/scene.commerce/       the custom component package @acme/scene.commerce (productCard, stockBadge, orderTimeline)
tools/                          domain-package.sh, render-variant.sh, render-bridge/, packages/ (the approved package set)
```

| Domain | Module | Persona | Owns |
| --- | --- | --- | --- |
| Catalog | `Merchandising` | Merchandiser | products, descriptions, price list, lifecycle |
| Storefront | `Shop` | Shopper | browse, basket, checkout, my orders |
| Sales | `Orders` | SalesRep, SalesManager | order intake, approval, confirmation, status |
| Purchasing | `Buying` | Buyer | suppliers, replenishment, purchase orders |
| Warehouse | `Stock` | WarehouseClerk | reservation, receiving, picking, dispatch |

OpsAdmin sees administration and dashboards in every domain.

## The one-application rules (all verified)

- **One `domain` line per tree** (PLAY0172), so the domain folders carry none.
- **One name per event in the application.** A foreign public event is declared in its consumer with a *consumer-local*
  name, `<Event>At<Consumer>` (`ProductPublishedAtStorefront`), and `from "<Origin>"` keeps the lineage. The same name on
  publisher and consumer compiles but does not bind (PLAY0472).
- **No `..` imports.** `Shared/` is simply a sibling folder; a folder compile walks every `.play` file.
- **Constraints live in slices.** A command that reads state (`reads X by id`) needs the application preamble, a read model
  with a `key`, a `remove with` handler and events on the default stream.
- **A read model's first event creates it.** Every required property is set there (literal defaults); otherwise every
  specification of the application becomes "unsupported".

- **Single-value keys only.** No read model has a composite key (`key` with several parts): Stage refuses those (STAGE-ESM-048) until
  Chronicle ships composite lookups (Chronicle#4265), and they block rendering and Play. Purchasing's supplier offers are keyed by a
  generated `SupplierOfferId` instead of supplier + SKU (see `Purchasing/README.md`); composite keys return when Chronicle#4265 ships.

## Public events (the contracts)

P1 `ProductPublished`, P2 `ProductPriceChanged`, P3 `ProductRetired` (Catalog); P4 `OrderPlaced` (Storefront); P5
`OrderConfirmed`, P6 `OrderStatusChanged` (Sales); P7 `StockReserved`, P8 `StockReservationFailed`, P9 `StockLevelChanged`,
P10 `StockBelowReorderPoint`, P12 `ShipmentDispatched` (Warehouse); P11 `PurchaseOrderReceived` (Purchasing). With who
consumes what, they are tabulated in the walkthrough. The flows are catalogue (F-A), order to cash to dock (F-B),
replenishment (F-C) and change of price (F-D).

## Tooling

- `tools/domain-package.sh <Domain>` assembles `domain <X>` + `Shared/` + the domain folder, proves it compiles with
  `--warnaserror` and passes its specifications, and writes `<Domain>.zip` - the unit Studio imports (one ZIP per event
  model). Inbound contracts get their published names back in the package (`KEEP_LOCAL_NAMES=1` keeps the local ones).
- `tools/render-variant.sh` builds the renderable variant and runs `cratis render` (`DOMAINS="Catalog Storefront"` by default).
  Stage renders neither Translate slices nor public events, nor `reaction batches atomic` (Stage#422), nor protected decision
  reads (Stage#503). The variant therefore moves each inbound private event into a bridge command (`render-bridge/bridge_translations.py`),
  deletes `translation/`, strips the preamble and the `reads` (`render-bridge/strip_reads.py`) and drops the specifications that
  need them. Every difference is documented in the script header. A reaction that invokes a command which gained inputs cannot be
  satisfied from its trigger payload, so Sales, Purchasing and Warehouse are not rendered yet. Proven for Catalog + Storefront (cratis
  3.75.0): `cratis render --package-set`, `dotnet build`, `npm run build`, generated `dotnet test` 90/90, headless Chromium
  walk of the screens with the hierarchical sidebar, the Shop topbar override and the toolbar button. Then:
  `dotnet build`, `npm install`, `npm run build`, `dotnet test AcmeCommerce.Specs`.
- `_packages/scene.commerce` is plain ES modules with React external. `cratis render --package-set
  tools/packages/acme-commerce.package-set.json` approves it (see `Scene/Documentation/package-hosting.md`). A live Stage
  (`cratis run`) cannot load a custom package that was not built in.

## What the language could not say (honest list)

Nothing in the first list remains: the language admitted each (Screenplay decision 0098, ESM v29 and v30) and the sample uses it.

- `Money` carries `decimals <= 2` ([#867](https://github.com/Cratis/Screenplay/issues/867)), and the basket's `addedValue` is `add addedValue by unitPrice * quantity` ([#864](https://github.com/Cratis/Screenplay/issues/864)).
- The shell has application-level toolbar buttons in `navigation topbar` (`Shell/navigation.play`, [#868](https://github.com/Cratis/Screenplay/issues/868)).
- Command specifications state the public event their slice publishes ([#866](https://github.com/Cratis/Screenplay/issues/866)).
- Orders cross the boundary whole and fan out per line ([#857](https://github.com/Cratis/Screenplay/issues/857)); a replenishment order's id is generated and pinned in its specifications ([#858](https://github.com/Cratis/Screenplay/issues/858)); that command appends to two event sources ([#859](https://github.com/Cratis/Screenplay/issues/859)); an id keeps its own concept on each side of a boundary ([#861](https://github.com/Cratis/Screenplay/issues/861)).

Still open:

- `$eventContext.*` in an outbound projection makes every specification "unsupported".
