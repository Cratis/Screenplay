# Screenplay

Screenplay is the modeling language for the Cratis platform. It describes a complete bounded context — events, commands, queries, projections, screens, automations, authorization, validation, constraints, and concepts — in a single declarative `.play` file. Stage interprets a Screenplay and runs it as a live application.

## Contents

| What | Where |
| --- | --- |
| Language documentation and full EBNF grammar | [Documentation/screenplay](Documentation/screenplay/index.md) |
| `@cratis/screenplay-language` — Monaco language service | [Source/Screenplay/Monaco/screenplay-language](Source/Screenplay/Monaco/screenplay-language) |
| `screenplay-editor` — standalone editor host | [Source/Screenplay/Monaco/screenplay-editor](Source/Screenplay/Monaco/screenplay-editor) |
| `screenplay` — VS Code extension | [Source/Screenplay/VSCodeExtension](Source/Screenplay/VSCodeExtension) |

## Getting started

```shell
yarn install
yarn build
yarn dev      # starts the editor on http://localhost:9200
```

To work on the VS Code extension, press **F5** — it builds the language service and the extension, then launches an Extension Development Host.
