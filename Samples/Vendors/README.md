# Vendors

Vendors for a purchasing team: a vendor list, a vendor page with its contacts, orders and certificates, an onboarding board and a performance dashboard. Built from Scene's **Vendors blueprint** (`Cratis.Blueprint.Vendors`, `@cratis/scene.blueprint.vendors`). It is an application whose screens come from a *domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
Vendors/
  vendors.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `PurchasingManager` | `IsPurchasingManager` | every screen |

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.Vendors <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author accepts.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint vendors-en`. No pack is declared in the document: the blueprint ships it, and the build resolves the reference against its packages.
- **Events feeding several views.** Each entity's registration event feeds its list, and the screens that show the same events differently - `VendorsList`, `VendorsOnboardingBoard`, `VendorsPerformance` - project their own read models from it.

## Verify

```bash
screenplay --warnaserror Samples/Vendors
screenplay test Samples/Vendors
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Vendors
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/vendors-blueprint.md).
