# Hr

The people side of a company, built from Scene's **Hr blueprint** (`Cratis.Blueprint.Hr`, `@cratis/scene.blueprint.hr`). It is an application whose screens come from a
*domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with
the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
Hr/
  hr.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `HrManager` | `IsHrManager` | every screen |

Employees sit in an org chart, ask for time off and are onboarded when they join. Every screen fills one of the blueprint's seven screen templates or two dialogs: the directory of cards, the employee page, the org chart, the time-off requests and balances, and the onboarding list and checklist. The demo data is the blueprint's own Norwegian pack.

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.Hr <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author
  accepts. Where the model has to say something the template cannot know - which command a form or a button runs - the screen fills the template's slot with an `action`.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint ...`. No pack is declared in the document: the blueprint ships it, and the
  build resolves the reference against its packages. Each read model is named after the shape the pack supplies, so the same rows reach the screen in a preview and in a running application.
- **An org chart from a view.** `ReportingLines` is a read model of who reports to whom, and the screen fills the `HrOrgChart` template; the tree is built from the rows.
- **Time off in two places.** `TimeOff` holds the requests and the `Balances` feature nested under it holds the leave balances, so the sidebar can show *Requests* and *Balances* beneath *Time off*.
- **A command from a list.** `DecideTimeOff` approves or declines a request; its screen fills the time-off list template and runs the command from the list's toolbar.

## Verify

```bash
screenplay --warnaserror Samples/Hr
screenplay test Samples/Hr
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Hr
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/hr-blueprint.md).
