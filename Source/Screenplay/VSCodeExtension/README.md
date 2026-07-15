# Screenplay Language

Language support for the Cratis **Screenplay** DSL — the modeling language that describes a complete bounded context (events, commands, queries, projections, screens, automations, authorization, validation, constraints, and concepts) in a single declarative `.play` file.

## Features

- **Syntax highlighting** for all Screenplay constructs, slice types, concept attributes (`@pii`, `@sensitive`), and context variables (`$context.*`, `$env.*`).
- **Embedded language highlighting** — inline `csharp`, `typescript`, `react`, and `html` blocks between triple backticks are highlighted with their own grammars.
- **Sub-language highlighting** for the Projection Declaration Language (PDL) inside `projection` blocks and the Change Data Capture Language (CDL) inside `capture` blocks.
- **IntelliSense** — context-aware completions for constructs, clauses, and in-scope symbols: policies after `authorize`, events after `on` and `produces`, `file`/`csharp` after `handler`, concepts and primitives in type positions.
- **Hover documentation** for keywords, concepts, policies, and events.
- **Diagnostics** — unknown slice types, unknown primitive types, references to undeclared policies and events, tab indentation, and unclosed code blocks.
- `.play` files carry the Cratis icon in the explorer and editor tabs.

## Development

From the repository root:

```shell
yarn install
yarn build
```

Then press **F5** in VS Code to launch an Extension Development Host with the extension loaded.

To produce a `.vsix` package:

```shell
yarn workspace screenplay package
```
