---
title: "Screenplay sub-languages and inline code"
description: "Extend Screenplay inline language recognition or Monaco editor support, and understand what registration changes in parsing, binding and execution."
---

# Sub-languages and inline code

Screenplay supports embedded declaration languages and inline code, but extending code recognition is different from adding editor support. Register an inline language to retain a tagged body in the syntax tree; register a Monaco sub-language to add highlighting and completion for a construct's body. Neither registration adds executable semantics.

The [Projection Declaration Language](projections/index.md) (PDL) describes how events build read models. The [Change Data Capture Language](captures.md) (CDL) describes how changes in source data become events. Both are built into the compiler. Inline code carries an implementation where a declaration alone is not enough.

## What the compiler can be extended with

Both compilers and the editors recognize six built-in inline tags: `csharp`, `java`, `typescript`, `react`, `html` and `sql`. The body is opaque to Screenplay: the parser keeps its text rather than interpreting the language inside it. To recognize another tag in the C# compiler, supply a registry:

```csharp
var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["python", "kotlin"]));
```

This compiler instance retains `python` and `kotlin` bodies alongside the built-in tags. Your integration must interpret or render those bodies separately; adding the tag does not supply an implementation. `new ScreenplayCompiler()` still recognizes only the six built-in inline languages.

Use a tagged fence for Java, just like the other built-in inline languages:

````screenplay
policy InvoiceAccess
  ```java
  return true;
  ```
````

The printer preserves `java` and the body text. Semantic implementation requirements retain the language tag, but reference execution reports Unsupported when it needs that predicate. A target must separately admit the attachment's language and role before rendering it; recognizing Java does not make it executable C#.

Construct keywords such as `projection`, `capture` and `command` are fixed in the compiler. Adding another construct to the grammar requires a compiler change. The [Monaco registration API](#the-monaco-registration-api) below extends editor tooling, not that grammar.

## Code the model needs

The executable semantic compiler records implementation requirements; it does not run inline code or file-backed implementations. The executable semantic model (ESM) represents bodied reducer transitions, named rule predicates, command and concept code validations, and inline or file-backed policy predicates as opaque v3 attachments. Reference execution that needs their outcomes returns Unsupported rather than guessing a result.

Resolved nonempty file/inline command handlers with unconditional `produces` bind in ESM v23 as language-tagged, hashed CommandHandler attachments; other attached forms retain `PLAY0800`. `PLAY0035` is retired. A pending command handler intent with unconditional `produces` binds in ESM v23 as a Pending CommandHandler requirement (information `PLAY0695`). Its `produces` declaration supplies result contract v1; the eventual body owns payload values. Reaching that body returns Unsupported (`ImplementationBody`). Production-less or conditional pending commands are refused with `PLAY0696`/`PLAY0697`; realization is deferred.

Inspect `ImplementationRequirements` to find an attachment's RequirementId, owning semantic address and role (with a member discriminator where needed), context and result contract versions, RequiredCapability, AttachmentResolution, inline language or authored file path, and source-map location. A SHA-256 content hash is present only for inline code or file contents supplied by the host. An authored path is not a content hash: the binder does not open files, so attachments without host-supplied contents remain unresolved.

Hosts with a trusted model root can use `AttachmentFiles.Load(root, documents)` and supply the result to `SemanticDocumentSet.Create` or an in-memory workspace's `WithAttachmentContents`. The loader reads only implementation attachments, confines access and reports warning diagnostics for refusals ([#244](https://github.com/Cratis/Screenplay/issues/244)). `PlayFileCompiler` and the standalone Tool compile syntax only and do not read attachments.

Use the MCP `read-workspace` view `implementation-requirements` to page these gaps at a pinned revision. This is an inventory for tooling, not an implementation contract or an indication that the reference executor can run the code. See [MCP](mcp/reference.md) for the read arguments.

A host editor can put the dedented inline body into a virtual document. In the
C# `BodyLines` API, each zero-based body line index gives the original one-based
line and the column of its first character; add the zero-based UTF-16 character
offset within the virtual line to `bodyLines[i].column` when mapping completion,
hover, diagnostics or edits back to the authored document. The MCP view encodes
consecutive lines sharing a column as runs; expand each `{line, column, count}`
entry before using a body line index. Check the result against `bodySpan` before applying
edits, particularly across lines or near fences. Tabs count as one column, not a
visual tab stop; CRLF takes two source offsets but one line break. A file
attachment instead maps to its entire file from line 1, column 1 and has no
per-line dedent map. Unresolved files have no known body span. Screenplay emits
this mapping data; the host owns the virtual document and language service.

## Plugging a new sub-language into the editor

Register a construct keyword with its token rules, completions and hover documentation when your editor needs to help readers work inside that construct.

The editor can highlight the registered construct's indented body as a sub-language. This does not add a production to the [Screenplay grammar](grammar.md).

A construct keyword the compiler does not know inside a slice, including one registered only in the editor, produces warning PLAY0029, and the parser skips its body so later declarations still parse; the document still compiles, but the construct contributes nothing to the model. Unknown words at the top level or in a module or feature body are errors. Editor registration is a reading aid, not compiler acceptance. The TypeScript compiler also skips some *known, shipped* constructs that it does not yet model; those are not extension keywords.

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

PDL and CDL use this API when `register(monaco)` runs. See `sub-languages/pdl.ts` and `sub-languages/cdl.ts` in the package for the reference implementations.

If your next step is analysis rather than editor support, [walk the parsed syntax tree](visitors.md). To turn an executable model into target artifacts, use the [rendering contract](interoperability.md#level-3-screenplay-to-artifact-rendering) instead of treating a registered tag as target support.
