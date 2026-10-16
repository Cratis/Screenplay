# Crm

A sales team's CRM in one file, built from Scene's **CRM blueprint** (`Cratis.Blueprint.Crm`, `@cratis/scene.blueprint.crm`). It is the
canonical example of an application whose screens come from a *domain blueprint* instead of being drawn by hand: every screen fills one
of the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data is the
blueprint's own sample-data pack.

```text
Crm/
  crm.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module with six features
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `SalesRep` | `IsSalesRep` | every screen |

## What to read it for

- **Screens from a blueprint.** `template blueprint Crm CrmLeadList version "^1.0"` names a template of the blueprint package and the
  range of its version the author accepts. The screen fills no slot by hand; the template is the dashboard, the list with its filters and
  bulk actions, the pipeline board, the record page, the create and edit forms and the quick-create dialog. Where the model has to say
  something the template cannot know - which command a button runs - the screen fills the template's slot (`actions`, `toolbar`) with an
  `action`.
- **Navigation as regions.** The layout declares the topbar and the sidebar, with Deals holding the Pipeline and the List; the module
  needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's Norwegian pack with `active blueprint crm-nordic-smb`.
  No pack is declared in the document: the blueprint ships them, and the build resolves the reference against its packages.
- **A read model for each thing a list shows.** `Leads`, `Contacts`, `Companies`, `Deals` and `Activities` are named after the shapes
  the blueprint's pack supplies, so the same rows reach the screen in a preview and in a running application. `DealRows` is a second
  view of the deals for the grouped list, and `SalesFigures` is one row, keyed `global`, that adds up the pipeline as deals are registered.
- **A quick-create dialog and a full form for one command.** `RegisterLead` has two screens: `NewLead` (the page) and `QuickNewLead`
  (the dialog opened from the list's header).

## Verify

```bash
screenplay --warnaserror Samples/Crm
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Crm
```

The blueprint itself - its eighteen screen templates, five dialogs, two sample-data packs and its navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/crm-blueprint.md).
