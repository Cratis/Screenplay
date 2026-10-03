# VS Code extension

The Screenplay extension for Visual Studio Code (`cratis.screenplay`) opens a `.play` file on the **event model board**, the board Cratis Studio draws. The model reads as a timeline:

- modules and features across
- each slice as a column holding its command, the events it produces and the read model it builds
- the specifications of each slice beneath it
- the screens of each slice, drawn as a prototype in the **User** row above it

The text stays the source of truth. The board redraws as you edit, and the language support (highlighting, completion, hover and diagnostics) is one click away.

## Screens

Each slice that declares [screens](screens.md) gets a prototype in the board's **User** row. The prototype is a sketch of what the screen holds, laid out top to bottom:

- titles
- actions, side by side
- tables, summaries and inline code
- the slots of a template, the header and footer spanning and the others side by side

Data a table or summary presents is not drawn twice. A screen implemented in a file is drawn as one content area. The User row appears only when the model has a screen.

## View options

The **View** button in the upper right of the board offers the view options Cratis Studio has:

- **Detail level**: **Full** draws each slice whole. **Overview** leaves out the specifications and the properties.
- **Properties** shows the properties of commands, events and read models.
- **Visualization**: **Arrows** connects slices with arrows, **Lines** with lines.

The choice is kept for you: every board, and every later session, opens with the view you last chose.

The collapse button on a module, feature or slice header folds it away while you look at the rest. It changes only how the board is drawn, never the model.

## Move between the board and the text

A `.play` file opens on the board by default. Use either of these to reach the text:

- **Show Source** in the board's title bar opens the text beside the board, so you can edit and watch the board follow.
- **Reopen Editor With…** › **Text Editor** shows the text in place of the board.

From a text editor, **Open Event Model Board** in the title bar, or in the Command Palette, goes back.

To always open `.play` files as text, add this to your settings:

```json
"workbench.editorAssociations": {
    "*.play": "default"
}
```

## Folder applications

A [folder of `.play` files](folders.md) is one application, and a file in it holds only its share of the model. When the file is inside a folder application, the board compiles the whole application and draws the file's share of it:

- The application is every `.play` file beneath the nearest folder that holds an `application.play`, which is the file the compiler writes at the root when it expands an application into folders.
- The search stays inside the workspace folder.
- Changes count wherever they are made: unsaved edits to any file of the folder, and files saved, created or deleted on disk.

What the file's share is depends on what it holds:

| The open file | The board shows |
| --- | --- |
| `application.play` at the root of the folder | The whole application |
| A slice file | Its slices |
| A feature or module file | The slices of the files it [imports](imports.md), and the slices other files place in the modules and features it declares |
| A file with no slice of its own, such as one that only declares concepts, types or policies | The whole application |

Every slice is drawn against everything the application declares, so a slice file's board has the same personas and events as the whole one. The problems listed above the board are the ones in the files it draws.

A file that is not inside a folder application is shown on its own - unless it [imports](imports.md) other files, in which case it is the root of an application and the board shows everything it imports.

## One application in the editor

The editor validates a workspace folder's `.play` files as one application, not file by file. A name declared in another file - an event, a policy, a concept, a query - is not reported as unknown, and a file an [import](imports.md) places in a module or feature is checked in that placement, so a focused file that holds only a `slice` validates cleanly. Import problems - a pattern that matches nothing, a file placed in two modules, an import cycle - are reported on the import that causes them. Unsaved edits count, and the folder is recompiled once a burst of edits settles.

Inside the quotes of an `import`, completion offers the `.play` paths of the workspace folder.

## Inline events in the text editor

Completion offers `produces event` inside commands and `for`, typed mappings, tags, `description`, `documentation`, and rename-only `id` inside its body. Inline events appear in event-name completion, hover, Go to Definition, and the board just like standalone events. Extracting one into the same slice keeps its board identity.

Inlay hints show `for <identifier>` on an inline production that omits its destination. A legacy plain omission shows `for <new event source>` only when no production in the command resolves a destination. An explicit sibling can supply a legacy command-level default, so the editor suppresses the hint rather than guessing. Hints also disappear when destinations conflict or an inline identifier cannot be determined. VS Code's standard inlay-hint settings control their visibility. The Monaco language service uses the same destination analysis.

The editor reports mixed-source omissions, declaration collisions, forbidden inline generations and origins, reserved system metadata, malformed documentation and identity pins. Redundant pins are information diagnostics, not warnings. Keep `id` absent for new events. Highlighting and hover treat `id` and `documentation` as directives only at the start of an event-body line with directive syntax; properties named `id` and projection keys such as `key id` remain names. Trailing comments and fenced prose do not change destination analysis.

## Optional values and quick fixes

Write `optional` after the type, as in `note String optional` or
`lines InvoiceLine[] optional`. Completion suggests this spelling and hover explains
that it allows the whole value to be absent. A property or type named `optional`
remains a name, not a keyword.

The [compatibility spelling](types.md#compatibility-note) receives information
diagnostic `PLAY0479`, marked deprecated rather than a warning. Use the lightbulb
to migrate one occurrence, or the document action to migrate all occurrences.
The fixes change only the spelling, retaining comments and spacing. They reparse
the current buffer and verify that its meaning is unchanged before offering edits.
Nothing is saved automatically. Stale buffer versions are refused. Monaco provides
the same verified fixes; neither editor needs a .NET process.

## Event quick fixes

The lightbulb in VS Code and Monaco also offers these occurrence-only actions:

| Diagnostic | Action |
| --- | --- |
| `PLAY0471` | **Remove the redundant event id** deletes the complete `id` line, including its indentation and line ending, for inline or standalone events. A trailing comment prevents the action; move the comment to its own line first. |
| `PLAY0478` | **State the destination: for projectId** inserts `for <identifier>` before the production's tags and mappings. Choose it only when the event should address that identifier rather than a newly allocated identity. |

Destination fixes never participate in fix-all or save actions. They require a local,
unique event contract, one required scalar command identifier, no other omitted
production in that command, and explicit siblings targeting the same identifier.
The editor also requires proof that adding `for` cannot promote the model's version:
the event already has a payload property with that name, or a local typed destination
already establishes the newer routing rules. Imports, generation markers, and version
evidence requiring executable binding are conservatively refused. This means the
workspace repair workflow can offer repairs the editor cannot prove safe.

Both actions reparse the edited buffer and verify that only the intended syntax
changes and the diagnostic disappears. Analysis is cached for the current document
version; stale edits are refused. `PLAY0470` has no quick fix: state the intended
destinations explicitly and resolve the binding errors first.

## When the model has errors

The board draws everything that could be read, so a typo in one slice does not empty it. The errors are listed above the board. Select one to open the file at its line.

The board is drawn by the extension's own [TypeScript compiler](typescript-compiler.md), which parses but does not run the C# compiler's semantic checks. A model the board draws can still be one the [compiler](tool.md) rejects. The diagnostics in the text editor and the CLI remain the authority on whether a model is valid.

The text editor also reports `PLAY0478` as information when a plain production
omits `for` and its command has an identifier. This is advice, not a new routing
default. Monaco and VS Code use their own TypeScript validation and offer the
conservative [event quick fixes](#event-quick-fixes) above; they do not host the C#
workspace repair transaction. Use the [MCP repair workflow](mcp/authoring-tools.md#fix-a-diagnostic)
for executable-model verification or to declare a missing produced event, then
review and apply the typed proposal.

## Theme

The board is drawn in its dark theme whatever your color theme is. Its labels are colored for a dark surface, the same as in Studio's viewer, and would be unreadable on a light one.

## Develop the extension

From the repository root, build the packages the extension bundles, then the extension:

```shell
yarn install
yarn workspace @cratis/screenplay-compiler build
yarn workspace @cratis/screenplay-event-models build
yarn workspace @cratis/screenplay-language build
yarn workspace screenplay build
```

Press **F5** in VS Code to start an Extension Development Host with the extension loaded, and package a `.vsix` with `yarn workspace screenplay package`. The board's page is `Source/Screenplay/VSCodeExtension/Webview`, a React application bundled with the extension. It renders `@cratis/event-models` under a content security policy that allows no `eval`.
