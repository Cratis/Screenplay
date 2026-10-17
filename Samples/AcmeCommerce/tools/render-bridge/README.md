# Render bridge fragments of Storefront and Sales (ecommerce-author-sales)

`tools/render-variant.sh` renders the *variant* of the sample, not the source. The fragments in this folder are what the Storefront and Sales
domains need in the variant, proven with `cratis 3.75.0`: `cratis render` -> `dotnet build` 0 warnings/0 errors -> `dotnet test <App>.Specs`
123/123 -> `npm run build` (Catalog + Storefront + Sales together). Apply them in this order after copying the sample and deleting `*/translation`:

| Step | File | Why |
| --- | --- | --- |
| 1 | `strip-protected-reads.py <variant-root>` | cratis/Stage 3.75 refuse `command reads protected` (STAGE-ESM-044) and `reaction batches atomic` (STAGE-ESM-043, Stage#422): reads/`require` rules are removed, a produced mapping `p = Read.prop` becomes a command input (table `EXTRA_INPUTS`: add your own domain's commands there), the decision-only `OrderState` view and the `ConfirmApprovedOrder` automation are not rendered |
| 2 | `drop-events-storefront-sales.txt` (`<file>\|<event>`) | a private event declared in a StateView slice gets no generated `[EventType]`; the bridge declares it in a StateChange slice |
| 3 | `drop-specifications-storefront-sales.txt` (`<file>\|<specification>`) | specifications naming public events, guarded by a protected read (removed) or cascading through a reaction (STAGE-ESM-011) |
| 4 | `bridge-storefront.play.txt`, `bridge-sales.play.txt` -> `RenderBridge/*.play` | a command per inbound private event, and the reaction `CheckoutCompleted -> ReceiveOrder` that carries an order from the shop to Sales |

Also removed by the variant (see `render-variant.sh`): `*/translation/`, the custom `Acme.Commerce` components unless `PACKAGE_SET=1`.
Rendering projections also needs `no automap` where an event carries the key property (STAGE-ESM-017 "Mappings cannot overwrite the key"); the
source already says so. Not rendered by Stage either: a projection keyed by an event *payload* property (so the Sales dashboard is per order),
composite values in a projection (so `OrderState` stays out of the variant).
