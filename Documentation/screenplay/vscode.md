# VS Code extension

The Screenplay extension for Visual Studio Code (`cratis.screenplay`) opens a `.play` file on the **event model board**, the board Cratis Studio draws. The model reads as a timeline:

- modules and features across
- each slice as a column holding its command, the events it produces and the read model it builds
- the specifications of each slice beneath it

The text stays the source of truth. The board redraws as you edit, and the language support (highlighting, completion, hover and diagnostics) is one click away.

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

A file that is not inside a folder application is shown on its own.

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
