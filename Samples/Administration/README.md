# Administration

An application's administrator, built from Scene's **Administration blueprint** (`Cratis.Blueprint.Administration`, `@cratis/scene.blueprint.administration`). It is an application whose screens come from a
*domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with
the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
Administration/
  administration.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Administrator` | `IsAdministrator` | every screen |

Users hold roles, roles hold permissions and every change is written to an audit log. Every screen fills one of the blueprint's five screen templates or two dialogs: the user list and page, the roles beside their permission matrix, the settings and the audit log.

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.Administration <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author
  accepts. Where the model has to say something the template cannot know - which command a form or a button runs - the screen fills the template's slot with an `action`.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint ...`. No pack is declared in the document: the blueprint ships it, and the
  build resolves the reference against its packages. Each read model is named after the shape the pack supplies, so the same rows reach the screen in a preview and in a running application.
- **One row of settings.** `SaveSettings` writes the organization's settings and `SettingsView` shows them; the settings template is a form whose sections are saved one at a time.
- **A log nobody edits.** `AuditLog` is a read model the audit log template shows read only; entries arrive as `AuditEntryRecorded`.
- **An invitation is a registration.** `InviteUser` is the dialog `AdministrationInviteUser`; a user starts as *invited* and the list's role action (`ChangeUserRole`) gives them another role.

## Verify

```bash
screenplay --warnaserror Samples/Administration
screenplay test Samples/Administration
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Administration
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/administration-blueprint.md).
