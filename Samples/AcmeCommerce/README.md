# Acme Commerce (sample)

A mid-size shop in five bounded contexts - **Catalog, Storefront, Sales, Purchasing, Warehouse** - in ONE Screenplay application.
The contexts never share a database; they talk through *public events*, and every context translates what it receives into
its own language.

```bash
screenplay --warnaserror Samples/AcmeCommerce
screenplay test Samples/AcmeCommerce
```

## Layout

```
application.play                the ONLY `domain AcmeCommerce` line
Shared/                         concepts (with their validation rules), enums with pinned numbers, composite types,
                                event sources, policies and personas, the Acme theme
Shell/                          the ui profile (blueprint AppShell root, theme, packages, the one sample-data pack),
                                the application navigation (hierarchical sidebar, topbar), sample data
Application/                    the Forbidden / NotFound / Error screens
<Domain>/model/                 state changes, state views, automations, forms, screens, specifications
<Domain>/translation/           Translate slices: outbound publication, inbound anti-corruption
```

Status: Catalog is complete; the other four domains are filled in by their own pull requests (see the pull requests of
the Acme Commerce epic, Cratis/Scene#96). This README grows with them.
