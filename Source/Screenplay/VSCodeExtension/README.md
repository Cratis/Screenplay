# Screenplay Language

Language support for the Cratis **Screenplay** DSL — the modeling language that describes a complete bounded context (events, commands, queries, projections, screens, automations, authorization, validation, constraints, and concepts) in a single declarative `.play` file.

![The Screenplay event model board beside PlaceOrder source, with Commerce command, event, screen, and specification cards](https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/images/vscode-source-and-board.png)

*The released extension in code-server, showing the Commerce sample.*

## Quickstart

1. Run `code --install-extension cratis.screenplay`.
2. Open your model's folder, then a `.play` file to see the board.
3. Choose **Show Source** to edit beside the board and watch it redraw.

[See your event model](https://cratis.io/screenplay/see-your-event-model/) ·
[VS Code guide](https://cratis.io/screenplay/vscode/) ·
[Language documentation](https://cratis.io/screenplay/)

The board visualizes the model; it does not execute commands or specifications.
Use the [compiler](https://cratis.io/screenplay/tool/) to validate your model;
a board that draws is not proof that every semantic check passes.

## Features

- **The event model board** — a `.play` file opens on the Cratis event model board, the board Cratis Studio draws, and redraws as you edit. A file inside a folder application shows the whole application. **Show Source** opens the text beside the board, and **Open Event Model Board** goes back.
- **Syntax highlighting** for all Screenplay constructs, slice types, concept attributes (`@pii`, `@sensitive`), and context variables (`$context.*`, `$env.*`).
- **Embedded language highlighting** — inline `csharp`, `typescript`, `react`, and `html` blocks between triple backticks are highlighted with their own grammars.
- **Sub-language highlighting** for the Projection Declaration Language (PDL) inside `projection` blocks and the Change Data Capture Language (CDL) inside `capture` blocks.
- **IntelliSense** — context-aware completions for constructs, clauses, and in-scope symbols: policies after `authorize`, events after `on` and `produces`, `file`/`csharp` after `handler`, concepts and primitives in type positions.
- **Hover documentation** for keywords, concepts, policies, and events.
- **Diagnostics** — unknown slice types, unknown primitive types, references to undeclared policies and events, tab indentation, and unclosed code blocks.
- `.play` files carry the Cratis icon in the explorer and editor tabs.

## Development

From the repository root, build the packages the extension bundles, then the extension:

```shell
yarn install
yarn workspace @cratis/screenplay-compiler build
yarn workspace @cratis/screenplay-event-models build
yarn workspace @cratis/screenplay-language build
yarn workspace screenplay build
```

Then press **F5** in VS Code to launch an Extension Development Host with the extension loaded.

To produce a `.vsix` package:

```shell
yarn workspace screenplay package
```
