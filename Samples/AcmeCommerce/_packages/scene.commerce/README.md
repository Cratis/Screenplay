# @acme/scene.commerce

Acme Commerce's custom Scene runtime package (declared as `Acme.Commerce` in the sample's `ui profile`):

| Component | Authored as | Used by |
| --- | --- | --- |
| `productCard` | `component Acme.Commerce.productCard` | Storefront Browse (the product grid) |
| `stockBadge` | `component Acme.Commerce.stockBadge` | Warehouse StockLevels |
| `orderTimeline` | `component Acme.Commerce.orderTimeline` | Sales OrderPage (activity slot) |

It is plain ES modules with React left external (the host's own React is used), so it needs no build step. Approve it for
`cratis render` with `Samples/AcmeCommerce/tools/packages/acme-commerce.package-set.json`; see
`../../tools/render-variant.sh` (`PACKAGE_SET=1`) and the sample README.
