# Sub-languages and inline code

Screenplay carries languages it does not itself read. The [Projection Declaration Language](projections/index.md) (PDL) and the [Change Data Capture Language](captures.md) (CDL) are the two built-in ones, and an inline block carries a general purpose language for the parts a declaration cannot express.

Two of those sets are open, and they are open in different places. Read this section before assuming which.

## What the compiler can be extended with

**Inline languages are open.** The five an inline block ships with — `csharp`, `typescript`, `react`, `html`, `sql` — are what the surrounding tooling understands end to end: a Stage renders them, an editor highlights them. Hand the compiler a registry and it recognizes more:

```csharp
var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["python", "kotlin"]));
```

A registered block parses, carries its language tag, and holds its text in the syntax tree. The compiler does not read it — whoever registered the language is what makes sense of it, and that boundary is exactly what lets the set be open. Nothing else changes: `new ScreenplayCompiler()` still recognizes the built-in five and nothing more.

**Construct keywords are not open.** The words that introduce a construct inside a slice — `projection`, `capture`, `command`, and the rest — are fixed in the compiler. PDL and CDL are built in rather than registered, and a third construct keyword means changing the compiler. The editor is a different story, and the rest of this page is about that.

## Plugging a new sub-language into the editor

An editor extension registers a construct keyword together with its token rules, completions and hover documentation, so highlighting and IntelliSense compose cleanly.

The construct's body is opaque to the Screenplay grammar: from the host's perspective it is `ExtensionConstruct = Ident, Ident, NL, [ INDENT, { AnyLine }, DEDENT ]` (see [Grammar](grammar.md)). The block ends where the indentation returns to the level of the construct keyword.

Be aware of what this does and does not buy: a construct registered here highlights correctly and is then **discarded by the compiler**, because the compiler's set of construct keywords is closed. Until that set opens too, an editor extension is a reading aid rather than a language extension.

## The Monaco registration API

The `@cratis/screenplay-language` package has its own registry for editor tooling. A sub-language registers its Monarch token rules and IntelliSense under a construct keyword:

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
