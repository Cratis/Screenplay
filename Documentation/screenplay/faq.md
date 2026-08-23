---
title: Frequently asked questions
description: Common questions about Screenplay — its syntax, portable semantics, bounded code attachments, and downstream runtimes.
---

## Why indentation instead of braces?

Structure follows the offside rule: a construct owns everything indented beneath it, the way Python and YAML work. A `.play` file describes a nested model — modules contain features contain slices contain constructs — and indentation shows that nesting directly, without the visual noise of matching braces. The trade-off is that indentation is significant, so the language service flags tab indentation as a diagnostic; use spaces.

## What does the `.play` extension mean?

A Screenplay is a script for a production, so its file is a `.play`. The name is deliberate: a runtime or renderer performs the script without becoming the authority for what it means. Editors recognize `.play` files and give them the Screenplay icon and language support.

## Do I still write C#, TypeScript, or React?

Only at the implementation points that admit code. A command handler, query performer, rule predicate, reducer rule, reaction, constraint, or screen can carry an inline block or `file` reference where the declarative language cannot state the realization. Concepts, commands, events, queries, and specifications still carry the business meaning; code is an attachment rather than a second application model.

## What are Stage and Studio?

They are downstream consumers of a `.play` model. **Stage** is the runtime and rendering surface; **Studio** visualizes and edits the model. Screenplay remains the authority for portable meaning. A consumer must either preserve a declared capability or report it as unsupported — sharing a source file alone does not prove behavioral parity.

## How does Screenplay relate to Arc and Chronicle?

Screenplay describes portable command, fact, view, query, policy, and specification semantics. [Arc](/arc/) and [Chronicle](/chronicle/) are the first realization target for those semantics, but Screenplay does not depend on either product and does not copy their implementation vocabulary into the semantic authority. Realization profiles and renderers map the portable model to a framework.

## What are PDL and CDL?

They are the two built-in first-class sub-languages. **PDL**, the Projection Declaration Language, defines projection bodies. **CDL**, the Change Data Capture Language, defines capture transformations. Both have standalone compiler entry points as well as their host-language constructs. See [Projections](projections/index.md) and [Captures](captures.md).

## Can I add my own sub-language?

Not as a new Screenplay construct today. Host-language construct keywords are closed so the compiler cannot silently discard unknown behavior. You can register additional inline language tags, which the compiler carries as opaque text for the owning tool to interpret. Monaco can also register editor-only highlighting and completions, but that does not make the construct valid Screenplay syntax. See [Sub-languages and inline code](sub-languages.md).

## Where do highlighting and IntelliSense come from?

From the [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) package — a Monaco language service that provides highlighting (including the embedded code blocks and the PDL/CDL sub-languages), context-aware completions, hover documentation, and diagnostics. The standalone editor and the VS Code extension are both thin hosts over that one package, so they behave identically.

## Is Screenplay production-ready?

Screenplay is young and still evolving. The compiler and authoring tools are usable, while portable execution and rendering are being delivered capability by capability. Treat the reference as the description of accepted syntax, and require a runtime or renderer to declare support before relying on it to perform that syntax.
