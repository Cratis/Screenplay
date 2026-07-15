# @cratis/screenplay-language

Monaco language service for the Cratis **Screenplay** DSL (`.play` files) — syntax highlighting, IntelliSense completions, hover documentation, and diagnostics.

## Usage

```typescript
import * as monaco from 'monaco-editor';
import { register } from '@cratis/screenplay-language';

register(monaco);
```

`register` is the single entry point. It registers the `screenplay` language (`.play` extension), the Monarch tokenizer, the completion and hover providers, the diagnostics watcher, and the `screenplay-dark` / `screenplay-light` themes on the Monaco instance you pass in. `monaco-editor` is a peer dependency — the package never bundles Monaco.

```typescript
import { languageId, screenplayDarkThemeName, screenplayLightThemeName } from '@cratis/screenplay-language';

monaco.editor.create(element, {
    language: languageId,
    theme: screenplayDarkThemeName,
});
```

## What is covered

- **Highlighting** — all construct and clause keywords, slice types, concept attributes (`@pii`, `@sensitive`), context variables (`$context.*`, `$env.*`, `$eventContext.*`, `$.\*`), strings, numbers, and comments.
- **Embedded code blocks** — `csharp`, `typescript`, `react`, and `html` blocks between triple backticks are highlighted with Monaco's own language grammars.
- **Sub-languages** — the PDL (`projection`) and CDL (`capture`) bodies are tokenized by their own registered rules using Monarch's state stack.
- **Completions** — context-aware by enclosing construct (top level, module, feature, slice, command, produces, screen, constraint, …), plus in-scope symbol names: policies after `authorize`, events after `on` and `produces`, concepts and primitives in type positions.
- **Hover** — keyword documentation, concept definitions (primitive + attributes), policy require expressions, and event property lists.
- **Diagnostics** — unknown slice types, unknown primitive types, references to undeclared policies and events, tab indentation, and unclosed code fences.

## Extending with new sub-languages

```typescript
import { registerSubLanguage } from '@cratis/screenplay-language';

registerSubLanguage('workflow', {
    tokens: [[/\b(?:start|step|end)\b/, 'keyword']],
    completions: [{ label: 'step', insertText: 'step ${1:Name}', documentation: 'A workflow step.' }],
    hovers: { step: 'Workflow — a step executed in sequence.' },
});
```

Registration works before or after `register(monaco)`; the tokenizer recomposes on the fly. See the [sub-language documentation](../../../../Documentation/screenplay/sub-languages.md) for the full design.

## Building

```shell
yarn build
```
