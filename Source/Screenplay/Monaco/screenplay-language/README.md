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
- **Completions** — context-aware by enclosing construct (top level, module, feature, slice, command, produces, handler, screen, constraint, …), plus in-scope symbol names: policies after `authorize`, events after `on` and `produces`, concepts and primitives in type positions, and resolvable module/feature targets after `depends on`, nearest scope first, excluding targets already declared.
- **Hover** — keyword documentation, concept definitions (primitive + attributes), policy require expressions, and event property lists.
- **Diagnostics** — unknown slice types, unknown primitive types, references to undeclared policies and events, tab indentation, and unclosed code fences. Every one of them carries the compiler's own `PLAY` code (`diagnosticCodes`), so a squiggle and a CLI diagnostic for the same condition are the same code.

## Quick fixes

The registered code-action provider offers verified edits against the current buffer:

- `PLAY0479`: replace one legacy `?` suffix with `optional`, or migrate the document.
- `PLAY0471`: remove a redundant event `id` line. Trailing comments prevent removal.

Redundant id removal works in placed and multi-document applications.
Actions respect the requested diagnostic and kind, cache analysis per model version,
and pin edits to that version. Range requests return every eligible intersecting occurrence, without duplicate
actions. Independent recipes are verified together once per version to check the intended
syntax changes and diagnostic removal without reparsing per marker; no .NET process is
required. See [editor quick fixes](../../../../Documentation/screenplay/vscode.md#event-quick-fixes)
for the redundant id removal behavior.

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

The published runtime is an ES module graph under `dist/bundles/`, not a shared
prebuilt chunk. The root, capture, and projection entry paths are unchanged.
Private compiler modules ship as relative files under `dist/bundles/compiler/`;
consumers do not need to install `@cratis/screenplay-compiler`. Consumers can
now tree-shake and split these modules using their own bundler configuration.
Callable names, minification, and source maps are preserved.

`check-package.mjs` checks the packed tarball, resolves every public entry with
only declared peers installed, and prints the ten largest modules, total runtime
bytes, and the minified size of a consumer importing `register`, `languageId`, and
`screenplayDarkThemeName`. Runtime sizes exclude declarations and source maps.

### Runtime size budget

- Every published JavaScript file must be **≤ 500,000 bytes**. This is the consumer
  chunk budget and is never raised; split the module graph rather than growing a
  prebuilt chunk.
- The total runtime must be **≤ 1,000,000 bytes**. This ceiling approves language
  growth within that limit while guarding against vendored dependencies. Growth
  beyond it requires explicit approval after reviewing the module breakdown,
  not an automatic ceiling increase each release. Monaco remains external.
- A consumer's final chunks still depend on its bundler's splitting configuration;
  a module graph does not guarantee that a single bundled consumer stays under
  500,000 bytes.

The module-graph build initially emits **199 JavaScript files totaling 752,551
bytes**. Its ten largest modules are:

| Module (relative to `dist/bundles/`) | Bytes |
| --- | ---: |
| `completion-items.js` | 41,933 |
| `compiler/Syntax/SyntaxCollections.js` | 29,257 |
| `keyword-docs.js` | 23,414 |
| `compiler/Syntax/ScreenplaySyntaxWalker.js` | 20,450 |
| `compiler/Parsing/SpecificationParser.js` | 19,931 |
| `compiler/Diagnostics/DiagnosticCodes.js` | 14,235 |
| `sub-languages/projection/CompletionProvider.js` | 13,391 |
| `event-source-authoring.js` | 12,877 |
| `validation.js` | 12,806 |
| `compiler/Parsing/ScreenParser.js` | 12,351 |

The packed consumer smoke import bundles to **601,682 bytes** as a single
minified module with Monaco external. The breakdown contains no printer or
MCP-only provenance modules. Syntax serialization initialization is still pulled
in by compiler barrel exports; removing it requires a separate, behavior-checked
compiler import cleanup rather than dropping parser validators from the editor.

`yarn test:pack` also runs size-check specifications with a planted oversized
module. The guard must reject it and name its published path.
