# Sub-language Pluggability

The Screenplay parser is designed as a **registry of named sub-language parsers**. When the parser encounters a construct keyword (`projection`, `capture`, or any registered extension), it delegates to the registered sub-parser for that construct's indented body.

The [Projection Declaration Language](projections.md) (PDL) and the [Change Data Capture Language](captures.md) (CDL) are the two built-in sub-languages, registered exactly the way an extension would be — they are the reference implementations of the pluggability model.

## Plugging in a new sub-language

Additional domain-specific languages plug in by:

1. **Registering a construct keyword** — the word that introduces the construct inside a slice.
2. **Providing a parser** that consumes the construct's indented block.
3. **Optionally providing Monaco language service extensions** for the sub-language — token rules, completions, and hover documentation — so highlighting and IntelliSense compose cleanly.

The construct's body is opaque to the Screenplay grammar: from the host's perspective it is `ExtensionConstruct = Ident, Ident, NL, [ INDENT, { AnyLine }, DEDENT ]` (see [Grammar](grammar.md)). The block ends where the indentation returns to the level of the construct keyword.

## The Monaco registration API

The `@cratis/screenplay-language` package mirrors the parser's registry so editor tooling composes the same way. A sub-language registers its Monarch token rules and IntelliSense under a construct keyword:

```typescript
import { registerSubLanguage } from '@cratis/screenplay-language';

registerSubLanguage('workflow', {
    tokens: [
        [/\b(?:start|step|branch|join|end)\b/, 'keyword'],
    ],
    completions: [
        {
            label: 'step',
            insertText: 'step ${1:Name}',
            documentation: 'A step in the workflow.',
        },
    ],
    hovers: {
        step: 'Workflow — a step executed in sequence.',
    },
});
```

| Member | Purpose |
| --- | --- |
| `tokens` | Monarch token rules applied inside the construct's indented body. The host tokenizer switches into these rules when the keyword is encountered (via Monarch's `@push`/`@pop` state stack) and switches back out when a Screenplay construct keyword starts a line. Strings, numbers, operators, context variables, and comments are inherited from the host — a sub-language only declares what is specific to it. |
| `completions` | Completion items offered when the cursor is inside the construct. `insertText` supports Monaco snippet syntax. |
| `hovers` | Keyword → one-line documentation, shown on hover inside the construct. |

Registration is dynamic: `registerSubLanguage` can be called before or after `register(monaco)` — the tokenizer is recomposed on the fly, and every Monaco instance the language service was registered on picks up the new sub-language immediately.

PDL and CDL are registered through exactly this API when `register(monaco)` runs — see `sub-languages/pdl.ts` and `sub-languages/cdl.ts` in the package for the reference implementations.
