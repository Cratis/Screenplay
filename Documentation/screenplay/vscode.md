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

A [folder of `.play` files](folders.md) is one application, and a file in it holds only its share of the model. When the file is inside a folder application, the board shows the whole application:

- The application is every `.play` file beneath the nearest folder that holds an `application.play`, which is the file the compiler writes at the root when it expands an application into folders.
- The search stays inside the workspace folder.
- Changes count wherever they are made: unsaved edits to any file of the folder, and files saved, created or deleted on disk.

A file that is not inside a folder application is shown on its own - unless it [imports](imports.md) other files, in which case it is the root of an application and the board shows everything it imports.

## One application in the editor

The editor validates a workspace folder's `.play` files as one application, not file by file. A name declared in another file - an event, a policy, a concept, a query - is not reported as unknown, and a file an [import](imports.md) places in a module or feature is checked in that placement, so a focused file that holds only a `slice` validates cleanly. Import problems - a pattern that matches nothing, a file placed in two modules, an import cycle - are reported on the import that causes them. Unsaved edits count, and the folder is recompiled once a burst of edits settles.

Inside the quotes of an `import`, completion offers the `.play` paths of the workspace folder.

## Inline events in the text editor

Completion offers `produces event` inside commands and `for`, typed mappings, tags, `description`, `documentation`, and rename-only `id` inside its body. Inline events appear in event-name completion, hover, Go to Definition, and the board just like standalone events. Extracting one into the same slice keeps its board identity.

Inlay hints show `for <identifier>` on an inline production that omits its destination. A legacy plain omission shows `for <new event source>` instead; it never pretends to have the inline default. Hints disappear when destinations conflict or an inline identifier cannot be determined. VS Code's standard inlay-hint settings control their visibility. The Monaco language service uses the same destination analysis.

The editor reports mixed-source omissions, declaration collisions, forbidden inline generations and origins, reserved system metadata, malformed documentation and identity pins. Redundant pins are information diagnostics, not warnings. Keep `id` absent for new events.

## When the model has errors

The board draws everything that could be read, so a typo in one slice does not empty it. The errors are listed above the board. Select one to open the file at its line.

The board is drawn by the extension's own [TypeScript compiler](typescript-compiler.md), which parses but does not run the C# compiler's semantic checks. A model the board draws can still be one the [compiler](tool.md) rejects. The diagnostics in the text editor and the CLI remain the authority on whether a model is valid.

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
