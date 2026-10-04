# VS Code extension

The Screenplay extension for Visual Studio Code (`cratis.screenplay`) opens a `.play` file on the **event model board**, the board Cratis Studio draws. The model reads as a timeline:

- modules and features across
- each slice as a column holding its command, the events it produces and the read model it builds
- the specifications of each slice beneath it
- the screens of each slice, drawn as a prototype in the **User** row above it

The text stays the source of truth. The board redraws as you edit, and the language support (highlighting, completion, hover and diagnostics) is one click away.

## Generated values and responses (syntax-only)

The editor recognizes [generated command values and response contracts](commands.md#generated-values-and-responses-syntax-only), including fixture and assertion fields, inferred response types and generated-not-input hints. Completion uses the current source, including unsaved edits. Compiler diagnostics validate the syntax; acceptance does not enable execution. These constructs remain unavailable until ESM v8, and binding reports `PLAY0268` without a semantic model.

The board leaves generated values out of command request schemas and lists generated values and returns in command details. It does not create response events or emit official response types. TextMate highlighting treats ambiguous two-token `returns` lines conservatively; `returns @name` makes response intent explicit.

## Operation and system intent (syntax-only)

Both Monaco and VS Code recognize [systems and operations](operations.md), inline/standalone declarations and operation specification steps. Assistance resolves explicit declaration kinds over the assembled application and uses the current typed source for command inputs, including unsaved and import-placed files. Input/source suggestions retain concept, composite, optional and collection shapes; ambiguous references are not linked to an arbitrary declaration. Phase hover distinguishes pending, file and inline sources and ordered hints. Existing attachment navigation applies to phase files.

Operations remain unavailable until ESM v9 (`PLAY0268`). Board command details describe system/operation intent and authored production order; there are no operation event cards, fabricated event identities or passing operation assertion states. Unknown or ambiguous production context receives no guessed destination hint. Keyword-named inputs, comments and fenced source remain their original content.

## Handler implementation intent

Both Monaco and VS Code offer `implementation` beneath a command handler, then ordered `hint` lines, `file` or existing tagged fences within its wrapper. Completion and hover describe pending intent and unavailable handler execution. New words stay ordinary property names outside those contexts. Parser diagnostics `PLAY0492`–`PLAY0494` retain original source locations; fenced code is isolated from DSL analysis and completion.

The board does not show handler execution outputs or confirmation status. This authoring feature adds no AI, lock or confirmation command. See [supported owners](commands.md#implementation-intent-handlers-only).

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

The lightbulb in VS Code and Monaco also offers this occurrence-only action:

| Diagnostic | Action |
| --- | --- |
| `PLAY0471` | **Remove the redundant event id** deletes the complete `id` line, including its indentation and line ending, for inline or standalone events. A trailing comment prevents the action; move the comment to its own line first. |

Redundant id removal works in placed and multi-document applications.
It reparses the edited buffer and verifies that only the intended syntax changes
and the diagnostic disappears. Analysis is cached for the current document version;
range requests return every eligible intersecting occurrence, verifying the
independent recipes together rather than reparsing per marker. Stale edits are refused.

## When the model has errors

The board draws everything that could be read, so a typo in one slice does not empty it. The errors are listed above the board. Select one to open the file at its line.

The board is drawn by the extension's own [TypeScript compiler](typescript-compiler.md), which parses but does not run the C# compiler's semantic checks. A model the board draws can still be one the [compiler](tool.md) rejects. The diagnostics in the text editor and the CLI remain the authority on whether a model is valid.

The text editor also reports `PLAY0478` as information when a plain production
omits `for` and its command has an identifier. This is advice, not a new routing
default. Monaco keeps this advice-only behavior. VS Code can optionally use the
C# repair transaction below; neither editor tries to prove routing safety in
TypeScript. The [MCP repair workflow](mcp/authoring-tools.md#fix-a-diagnostic)
remains available separately.

## Preview saved-file C# repairs

This **experimental, opt-in** bridge offers only `PLAY0166` (declare a missing
produced event) and `PLAY0478` (explicitly route to the command identifier).
`PLAY0478` changes routing; it is not cleanup. C# decides eligibility across the
physical application. Extraction, `PLAY0469`, rename and a Monaco host bridge are
not included. Local `PLAY0471` and `PLAY0479` fixes remain unchanged.

Install a compatible server yourself using the [repair server setup](mcp/install.md#repair-capable-server-setup).
The server must expose repair contract v1, including pinned evidence and structured
failures. Older servers are refused, not downgraded. A CLI version alone does not
prove compatibility: capability support must be released in the server and included
in the CLI or bundle you install before that distribution is compatible.

In **User settings**, configure the absolute executable and one existing physical
model directory inside your trusted filesystem workspace. For a complete unpacked
native bundle, replace these illustrative paths with your installation:

```json
{
    "screenplay.repairs.enabled": true,
    "screenplay.repairs.executable": "/Users/you/tools/screenplay/server/Cratis.Screenplay.Tool",
    "screenplay.repairs.arguments": ["mcp"],
    "screenplay.repairs.modelRoot": "/Users/you/work/shop/specifications"
}
```

On Windows, use the binary's `.exe` path with JSON-escaped backslashes. For an
installed `screenplay` tool, use its absolute executable path and `["mcp"]`. For
`cratis`, use its absolute executable path and `["screenplay", "mcp"]`. The model
root is appended as **one argument**; do not put it in the prefix or add shell
quotes. Workspace/folder overrides are refused even if they match User settings.
A remote extension host needs its own compatible installation and paths. Browser
and virtual workspaces cannot run this bridge. The VSIX contains JavaScript only;
it does not download a server, install .NET, invoke Docker or run an AI agent.

1. Save or discard changed buffers yourself, including sibling `.play` files,
   attachments and `.screenplay` metadata. The extension never autosaves.
2. Run **Screenplay: Discover Saved-File C# Repairs**. This explicit command also
   explains trust, executable, dirty-buffer and contract refusals. Saved-source
   C# diagnostics use a separate collection; dirty-buffer local diagnostics remain.
3. Select the C# lightbulb action at the server-attributed occurrence. Consent to
   canonical formatting for every touched document, then wait for the complete
   read-only source **and identity-state** previews. Discovery and proposals write
   nothing. Incomplete or oversized review disables Apply (16 MiB combined bytes,
   at most 64 changed source documents). Metadata views are bounded to 10,000
   items and 16 MiB each.
4. Review every diff and the summary: routing consequences, byte hashes/BOM/line
   endings, authoring diagnostics and executable readiness. Readiness describes
   the compiler's executable subset, not implementation execution or runtime
   confirmation. Select **Apply reviewed repair**, then confirm **Apply**.

When C# returns a refusal, **Inspect conflict details** opens its structured
failure, conflict kinds and diagnostics in a read-only view. A refused operation
has no Apply authority.

Each root has one persistent connection. Root/configuration changes close the old
connection; file creation, imports, attachments and identity changes invalidate
outstanding selections. C# also checks exact source, catalog, state and frozen
base/candidate evidence. These actions are never preferred, fix-all or on-save.

Apply writes outside the editor and is **not normal editor Undo**. Use an exclusive
writer while applying. Journaled rollback does not guarantee crash-atomic visibility
across files. The extension awaits ordinary saved-buffer reloads; if you type after
Apply dispatch, it preserves your buffer and asks you to reconcile it with disk.
Further repairs stay blocked until affected buffers are synchronized, reconciled
or closed. It never force-reverts or replays source edits.

Cancellation discards queued reads or drains an in-flight read within its deadline.
Once Apply is dispatched, a timeout, disconnect or unrecognized failure means the
outcome is **unknown**, not “cancelled without changes.” Never retry automatically.
Use **Screenplay: Inspect C# Repair Identity and Recovery State**, inspect disk, and
follow the [separate recovery workflow](mcp/recovery.md) with explicit consent.
Inspection does not recover or authorize another apply.

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

The ordinary `yarn workspace screenplay test` suite uses a VS Code stub for local
editor tests. Build the C# tool, then run `yarn workspace screenplay test:repair-process`
for the real subprocess gate; it fails rather than skips when the tool is missing.
Set `SCREENPLAY_REPAIR_SERVER` to an absolute compatible binary if you are not using
`Source/DotNET/Tool/bin/Debug/net10.0/Cratis.Screenplay.Tool` (`.exe` on Windows).

For native extension-host tests, run `yarn workspace screenplay build:test-host`,
then `yarn workspace screenplay test:host` with `VSCODE_EXECUTABLE_PATH` pointing to
your installed native VS Code executable. The standard `@vscode/test-electron`
harness uses isolated settings and models under `.ai-work/`; it never downloads a
runtime. These tests exercise real read-only diffs, dirty attachment refusal,
exact-byte installation and saved-buffer reload. Modal click-through and
post-dispatch typing races require additional manual/native coverage; mocks are
not a substitute. Run on each native platform before claiming platform coverage.
