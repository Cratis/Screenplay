# Samples

Hand-written Screenplay applications, each in its own folder. A folder compiles as one application, so every
sample can be verified on its own:

```bash
screenplay --warnaserror Samples/<Sample>
```

| Sample | Size | Shows |
| --- | --- | --- |
| [Library](Library) | 1 file, 8 slices | The smallest useful application: commands, events, a projection, a view no event builds, a reaction, screens and specifications. Start here. |
| [Invoicing](Invoicing) | 1 file, 30 slices | Every construct except the [preview constructs](../.cratis/ai/rules/project/samples.md#preview-constructs), used for something an invoicing system needs, with English and Norwegian `.strings` files. |
| [Approvals](Approvals) | 1 file, 6 slices | A third application built from a domain blueprint: an inbox whose cards decide, a request wizard, a request page with its approval chain and an administrator's request types. |
| [Ticketing](Ticketing) | 1 file, 12 slices | A second application built from a domain blueprint: a help desk whose dialogs and board issue commands, a public portal form and a knowledge base. |
| [Crm](Crm) | 1 file, 18 slices | An application built from a domain blueprint: screens that fill the CRM blueprint's templates (`template blueprint Crm ...`), the navigation regions, and a blueprint's sample-data pack as the preview data. |
| [Inventory](Inventory) | 1 file, 11 slices | An application built from the Inventory and Assets blueprint: a warehouse's items, locations, movements and suppliers. |
| [Hr](Hr) | 1 file, 10 slices | An application built from the HR blueprint: a directory, an org chart, time off and onboarding, with the blueprint's Norwegian pack. |
| [Projects](Projects) | 1 file, 7 slices | An application built from the Project and Task Tracker blueprint: projects, my tasks and a board by status and by person. |
| [Orders](Orders) | 1 file, 5 slices | An application built from the Orders blueprint: orders, a fulfilment board and a delivery calendar. |
| [Catalog](Catalog) | 1 file, 5 slices | An application built from the Catalog blueprint: a catalog overview, products with their variants and price history, and categories. |
| [Vendors](Vendors) | 1 file, 5 slices | An application built from the Vendors blueprint: vendors with their contacts and orders, an onboarding board and a performance dashboard. |
| [Billing](Billing) | 1 file, 6 slices | An application built from the Invoicing blueprint: a receivables overview, invoices with their lines and payments, and a due-date calendar. |
| [CustomerPortal](CustomerPortal) | 1 file, 7 slices | An application built from the Customer portal blueprint: a customer's account home, orders, invoices, support inbox and profile. |
| [Contracts](Contracts) | 1 file, 4 slices | An application built from the Contracts blueprint: contracts with their parties and obligations, a terms timeline and a review inbox. |
| [WorkOrders](WorkOrders) | 1 file, 5 slices | An application built from the Work orders blueprint: work orders with their tasks, parts and time, a board and a visit schedule. |
| [Bookings](Bookings) | 1 file, 6 slices | An application built from the Bookings blueprint: a booking calendar, bookings, resources as cards and a new-booking wizard. |
| [Administration](Administration) | 1 file, 9 slices | An application built from the Administration blueprint: users, roles and permissions, settings and an audit log. |
| [AcmeCommerce](AcmeCommerce) | many small files | A full multi-domain application - Catalog, Storefront, Sales, Purchasing, Warehouse - in one folder tree: shared kernel, per-domain shell (own logo, theme, menu) with hierarchical navigation, public events between contexts, blueprint screens and a custom component package. |
| [Commerce](Commerce) | many small files | An application composed with [imports](../Documentation/screenplay/imports.md) at every level: a root file that reads like a table of contents, a file per module and per feature that imports its own folder, and one file per slice that holds only the slice. |
| [TimeTracking](TimeTracking) | many small files | An application composed from one root glob. Module files declare their features inline and import each feature's folder, and each step of the story is a file of its own. |

## What every sample keeps to

- Every state change and state view slice has a screen. Whatever gates it is satisfied by the policies of the
  persona who uses it, so the event model board draws the screen in that persona's row.
- Every slice has Given/When/Then specifications.
- Views do not have to be built from events. Each sample beyond the smallest has at least one read model that
  a query's `performer` composes from somewhere else.
- No sample uses deprecated syntax, and each compiles with zero errors and zero warnings.
- A folder sample has no large file. Each file tells one part of the story, and the imports in its root reach every
  file in the folder.

`dotnet test` checks all of this in `Source/DotNET/Screenplay/for_Samples`, and the samples change in the same
pull request as the language does. See [the samples rule](../.cratis/ai/rules/project/samples.md).
