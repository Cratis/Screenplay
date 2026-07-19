---
title: Frequently asked questions
description: Common questions about the Screenplay language — indentation, the escape hatch to code, Stage and Studio, and how it relates to Arc and Chronicle.
---

## Why indentation instead of braces?

Structure follows the offside rule: a construct owns everything indented beneath it, the way Python and YAML work. A `.play` file describes a nested model — modules contain features contain slices contain constructs — and indentation shows that nesting directly, without the visual noise of matching braces. The trade-off is that indentation is significant, so the language service flags tab indentation as a diagnostic; use spaces.

## What does the `.play` extension mean?

A Screenplay is a script for a production, so its file is a `.play`. The name is deliberate: **Stage** performs the script as a running application, and the extension carries that metaphor. Editors recognize `.play` files and give them the Screenplay icon and language support.

## Do I still write C#, TypeScript, or React?

Only where you want to. Every construct has a declarative form that covers the common case, and any construct can drop into an inline `csharp`, `typescript`, `react`, or `html` block — or point at an external `file` — when a rule needs custom logic. The declarative form handles the routine 90%; the escape hatch is there for the rest, so you are never forced out of the model to express something hard.

## What are Stage and Studio?

They are the two consumers of a `.play` file. **Stage** interprets a Screenplay and runs it as a live application — it is the runtime. **Studio** reads the same file to visualize and generate. Both work from the identical source of truth, which is why the model and the running system cannot drift apart. This repository is the third piece: the language itself, its documentation, and the editing tools (a Monaco language service, a standalone editor, and a VS Code extension).

## How does Screenplay relate to Arc and Chronicle?

A `.play` file describes the same artifacts you would otherwise write by hand on the Cratis platform: [Arc](/arc/) commands, queries, validation, and authorization, and [Chronicle](/chronicle/) events, projections, constraints, and reactors. Screenplay is a modeling layer *over* those frameworks, not a replacement for them — nothing about the runtime is hidden. Because the targets are the same, a hand-written slice and a modeled `.play` slice coexist in one application without friction.

## What are PDL and CDL?

They are the two built-in sub-languages. **PDL**, the Projection Declaration Language, is the grammar inside a `projection` body. **CDL**, the Change Data Capture Language, is the grammar inside a `capture` body. The Screenplay parser delegates each construct's body to its registered sub-parser, so these are embedded grammars with their own highlighting and completions — not special-cased syntax. See [Projections](projections/index.md) and [Captures](captures.md).

## Can I add my own sub-language?

Yes. PDL and CDL are registered exactly the way an extension would be — they are the reference implementations of the pluggability model. You register a construct keyword, provide a parser for its indented body, and optionally supply Monaco token rules, completions, and hover text so highlighting and IntelliSense compose cleanly. See [Sub-language Pluggability](sub-languages.md).

## Where do highlighting and IntelliSense come from?

From the [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) package — a Monaco language service that provides highlighting (including the embedded code blocks and the PDL/CDL sub-languages), context-aware completions, hover documentation, and diagnostics. The standalone editor and the VS Code extension are both thin hosts over that one package, so they behave identically.

## Is Screenplay production-ready?

Screenplay is young and still evolving; the language and its tooling are under active development. Treat it accordingly — the reference documents what the language expresses today, and where a construct isn't covered yet, a hand-written Arc/Chronicle slice alongside your `.play` files is a perfectly good answer.
