# Customer portal

A self-service portal for customers: an account home, their orders and invoices, a support inbox and their own profile. Built from Scene's **Customer portal blueprint** (`Cratis.Blueprint.CustomerPortal`, `@cratis/scene.blueprint.customer-portal`). It is an application whose screens come from a *domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
CustomerPortal/
  customer-portal.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Customer` | `IsCustomer` | every screen |

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.CustomerPortal <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author accepts.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint customer-portal-en`. No pack is declared in the document: the blueprint ships it, and the build resolves the reference against its packages.
- **Events feeding several views.** Each entity's registration event feeds its list, and the screens that show the same events differently - `PortalHome`, `PortalOrdersList`, `PortalInvoicesList`, `PortalSupport`, `PortalProfile` - project their own read models from it.

## Verify

```bash
screenplay --warnaserror Samples/CustomerPortal
screenplay test Samples/CustomerPortal
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/CustomerPortal
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/customer-portal-blueprint.md).
