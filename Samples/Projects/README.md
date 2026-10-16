# Projects

A team tracking its work, built from Scene's **Project and Task Tracker blueprint** (`Cratis.Blueprint.ProjectTracker`, `@cratis/scene.blueprint.project-tracker`). It is an application whose screens come from a
*domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with
the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
Projects/
  projects.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `TeamMember` | `IsTeamMember` | every screen |

Projects hold tasks and tasks move from to do to done. Every screen fills one of the blueprint's five screen templates or three dialogs: the projects as cards, the project page, my tasks grouped by due date, the board by status and by person, and the task side sheet.

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.ProjectTracker <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author
  accepts. Where the model has to say something the template cannot know - which command a form or a button runs - the screen fills the template's slot with an `action`.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint ...`. No pack is declared in the document: the blueprint ships it, and the
  build resolves the reference against its packages. Each read model is named after the shape the pack supplies, so the same rows reach the screen in a preview and in a running application.
- **One set of events, three views.** Moving a task (`MoveTask`) updates the board's cards and the lanes' cards from the same `TaskMoved` event; the *By status* and *By person* entries of the sidebar target `Projects.Board` and `Projects.Board.ByPerson`.
- **A side sheet as a screen.** `TaskSheet` fills the `ProjectTaskSheet` template - a drawer from the side that shows one task - from the task list's view.
- **Cards with a progress bar.** The `ProjectList` view feeds the projects' cards; the progress is a number the template draws as a bar.
- **A schedule from the same projects.** `ProjectTimelineItems` is a read model projected from `ProjectRegistered` (the owner is the lane, the due date the end of the bar), and the `Timeline` feature's screen fills the `ProjectTimeline` template.

## Verify

```bash
screenplay --warnaserror Samples/Projects
screenplay test Samples/Projects
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Projects
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/project-tracker-blueprint.md).
