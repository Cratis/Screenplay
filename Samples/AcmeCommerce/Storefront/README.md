# Storefront (module `Shop`)

What a shopper sees and does: **Browse, Basket, Checkout, My orders**. The shop never reads another domain's data: everything it knows
about products, stock and order status arrives as a PUBLIC event and is translated into the shop's own words.

| Slice | Kind | What it shows |
| --- | --- | --- |
| `ListProduct`, `UpdateDisplayPrice`, `DelistProduct`, `UpdateAvailability`, `TrackOrderStatus` | Translate (inbound) | P1/P2/P3/P9/P6 -> `ProductListed`, `DisplayPriceChanged`, `ProductDelisted`, `AvailabilityChanged`, `MyOrderUpdated`; captures `key sku` (the shop addresses a product by SKU) |
| `PublishOrder` | Translate (outbound) | `CheckoutCompleted` -> public P4 `OrderPlaced` |
| `RegisterCustomer`, `CloseCustomerAccount` | StateChange | constraint `UniqueCustomerEmail`, concept rule on `EmailAddress` |
| `AddToBasket`, `ChangeBasketQuantity`, `RemoveFromBasket` | StateChange | `AddToBasket` reads the listing (`reads ListedProduct by sku`) and refuses a sold-out product |
| `EnterShippingAddress`, `ChooseDeliveryMethod`, `CheckOut` | StateChange | the three CommandForms of the Checkout wizard; `CheckOut` reads the basket size and the customer account |
| `EmptyBasketAfterCheckout` | Automation | `CheckoutCompleted` -> `ClearBasketContents` |
| `ProductBrowse`, `ProductPage`, `BasketView`, `CheckoutProgress`, `CustomerAccounts`, `MyOrders` | StateView | read models; `BasketView` uses `children`, `ProductBrowse` has a filtered search query |

Screens: `Browse` (own template `ProductGrid`: a card grid with a filters rail), `Basket`, `ProductPage` and `OrderTracking` (own template `SideSummary`: lines beside a summary),
`Checkout` (blueprint `WizardScreen`: a stepper is what a wizard is), `MyOrders` (`ListScreen`). Both own templates live in `model/templates.play` and carry content: the
stock components each slot is seeded with - the draft the template editor draws. A screen fills the slots it shows; a screen preview draws only what the screen filled.

Language notes (see the Acme Commerce epic): the basket carries `addedValue` (what its lines were worth when they were added: `add addedValue by unitPrice * quantity`);
`CheckOut` is not rejected for an out-of-stock *line* (a command cannot read per element of a collection) - `AddToBasket` refuses sold-out products instead.
