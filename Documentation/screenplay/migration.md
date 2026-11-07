---
title: "Migrate a Screenplay model to a new version"
description: "Rewrite .play files written for an earlier Screenplay version with screenplay migrate, the MCP migrate tool or the VS Code command, read what needs review, and migrate a Screenplay 5 model to Screenplay 6."
---

# Migrate a model to a new version

A Screenplay major version can retire a form. The compiler does not keep the old form working for a while: it reports
it as an error whose message names the replacement and the migration that writes it, for example
`run 'screenplay migrate --from 5 --to 6' to rewrite the model (slots and outlets)`. The migration rewrites the files
for you and tells you what it could not decide alone.

```bash
screenplay migrate Samples/Shop --from 5
```

```text
Migrating 3 files from Screenplay 5 to 6 (1 step: 5→6 slots-and-outlets)
shell.play
  8:3 navigation topbar → outlet topbar of navigation + outlet topbarActions of actions
  13:3 navigation sidebar → outlet sidebar of navigation
orders.play
  4:5 toolbar actions → outlet actions extends
3 files rewritten · 0 need review · 0 refused
```

A file does not say which version it was written for, so `--from` is required. Migrations only move forward.

## Run it from the command line

`screenplay migrate <file.play|folder> --from <version> [--to <version>] [--dry-run] [--report <file>]`

| Option | Meaning |
| --- | --- |
| `<file.play\|folder>` | A folder migrates every `.play` file beneath it as one application. A file migrates that file; the other files of its folder give it its place in the application and are compiled with it, unchanged. |
| `--from <version>` | The version the files are written in, as a major: `5`, `5.11` or `v5.11.0` all mean 5. |
| `--to <version>` | The version to end at. Defaults to the version the compiler reads. |
| `--dry-run` | Print what would change and write nothing. |
| `--report <file>` | Also write the result as JSON: the steps, and for every file its outcome, changes, reviews and the compiler's remaining errors and warnings, each with its line. |

| Exit code | Meaning |
| --- | --- |
| `0` | Every file was migrated and compiles with the target version without errors. |
| `2` | A file needs review: the migration left something for you to decide, or the result still has compiler errors. Every change it could make is written. |
| `1` | The migration could not run: an invalid argument, no `.play` files, no chain of steps between the versions, or a file a step could not rewrite. A file that could not be rewritten is left as it was. |

Run it on a clean working tree and read the diff before committing, as with any generated change.

## How the rewrite works

The migration never edits text with search and replace. Each step reads the file with the compiler - which still
understands every retired form of the version it migrates from, and keeps what it read in the syntax tree while it
reports the form - rewrites the syntax tree, and prints the changed declarations back with the
[printer](printing.md). Only the lines a step changes are rewritten. Every other line stays as it was, byte for byte:
comments, blank lines, alignment and the comment at the end of a rewritten line.

When the steps have run, the current compiler compiles the result and reports any errors and warnings with the file
and line, so you see straight away what still needs attention. It reports what a normal compile reports, the checks
that are on by default included - such as `PLAY0533` for a State Change command nothing issues. A warning is reported
but does not make a file need review; an error does.

Steps chain by version. Migrating from 5 to 7 would run the 5 → 6 step, then the 6 → 7 step, each reading the output
of the one before. The runner refuses a gap (no step leaves a version on the way) and an overlap (two steps cover the
same version) instead of guessing.

## Migrate from VS Code

**Screenplay: Migrate document** migrates the open, saved `.play` document. It asks for the version the document is
written in, runs the migration in the C# compiler over the same bridge as the
[C# repairs and checks](vscode.md), and replaces the editor text with the result. The change is an ordinary edit:
review it, undo it, or save it. What changed and what needs review are written to the **Screenplay migration**
output. The other files of the root are not touched.

## Migrate through MCP

The [MCP server](mcp/reference.md)'s `migrate` tool takes `from`, an optional `to` and an optional root-relative
`document`. It is read-only: it returns the same report as `--report` and, when you name a document, its migrated
source for you to write. An agent can use it to migrate one document, check the reviews and the remaining diagnostics,
and write the result through its usual file tools.

## Migrating from 5

Screenplay 6 split what Screenplay 5 called outlets into two things. A **slot** is a region content is placed in. An
**[outlet](outlets.md)** is a list of items a layout or template offers - buttons, filters, tabs, stats or
destinations - that modules, features and screens change without copying the template. Run:

```bash
screenplay migrate <folder> --from 5 --to 6
```

The `slots-and-outlets` step rewrites:

| Screenplay 5 | Screenplay 6 | Code |
| --- | --- | --- |
| `outlet <name>` in a component | `slot <name>` | `PLAY0890` |
| `destination outlet <name>` in a contribution | `destination slot <name>` | `PLAY0891` |
| `outlet <name>` under a `navigate to` | `slot <name>` | `PLAY0892` |
| `outlet <name>` on a layout or template | `slot <name>` | `PLAY0893` |
| `navigation topbar\|sidebar` on a layout or template | `outlet topbar\|sidebar of navigation`, its items as `default` items and its logo kept; its buttons move to a new `outlet topbarActions\|sidebarActions of actions` | `PLAY0894` |
| `navigation topbar\|sidebar extends\|inherits\|overrides` on a module | `outlet topbar\|sidebar extends\|inherits\|overrides`, with its items; its buttons move to `outlet topbarActions\|sidebarActions` with the same mode, the actions outlet the layout's own region buttons moved to | `PLAY0894` |
| `<slot> contributes <Point>` | `<slot>` and `outlet <Point> of navigation in <slot>` | `PLAY0895` |
| `toolbar <name>` on a feature or module | `outlet <name> extends`, with its buttons | `PLAY0896` |
| a template `toolbar` slot holding a `toolbar <name>` or buttons | the `toolbar` slot and `outlet <name> of actions in toolbar`, its buttons as `default` items | `PLAY0026` |

It leaves for review, with the file and line:

- A template slot that holds both items and something else, such as a component. The items move to the outlet; the
  rest stays under the slot, which still does not compile, so you can place it with a screen's `fits` or remove it.
- A toolbar `item <name> action|navigate|dialog <Target>`. An outlet holds buttons that name a slice; write it as
  `button <name> title "…" slice <Slice>` (or `default` in a template) or remove it.
- A `logo` in a module's navigation region. Only a layout declares the logo.
- Buttons in a module's navigation region when no layout in the same file offers `outlet topbarActions|sidebarActions of actions`.
  The buttons still move to that outlet, out of the navigation outlet; check that the layout offers it, or move them to
  the actions outlet it does offer.
- A mode on a layout's navigation region, and an outlet the template already declares under the same name.
- A line in a navigation region that is neither an item, a button nor a logo.

Every sample as it was in Screenplay 5.11 migrates to warning-free Screenplay 6 with nothing to review; the
specifications in `Source/DotNET/Screenplay/Migrations` hold the migration to that and to a before-and-after fixture
for every row above.

The [TypeScript compiler](typescript-compiler.md) reports the Screenplay 5 forms with the same messages, but does not
migrate: the migration runs in the C# compiler, behind the CLI, the MCP tool and the VS Code command.

## Write a migration step

A step is a class in the compiler that implements `IScreenplayMigration` and has a public parameterless constructor;
the runner finds it by convention.

```csharp
public class SlotsAndOutlets : IScreenplayMigration
{
    public ScreenplayVersion From => 5;
    public ScreenplayVersion To => 6;
    public MigrationName Name => "slots-and-outlets";

    public MigrationResult Apply(DocumentSyntax document)
    {
        // Read document.Syntax - retired forms included - and return the rewritten tree,
        // a MigrationChange per change with the edits that print it, and a MigrationReview
        // for anything an author has to decide.
    }
}
```

A change carries edits that say which source lines the rewritten nodes replace: `ReplaceLine`, `InsertAfterBlock`,
`MoveBlock` and `RemoveLine`. The runner prints the nodes and applies the edits, refusing two edits to one line. To run
migrations from your own code, use `ScreenplayMigrator`: `Plan(from, to)` chains the steps, `MigratePath` and `Migrate`
return a `MigrationReport` without writing anything.

## See also

- [Compiler and CLI](tool.md)
- [Outlets](outlets.md) and [Layouts and templates](templates.md) for what Screenplay 6 writes.
- [Diagnostics](diagnostics.md) for the codes above.
