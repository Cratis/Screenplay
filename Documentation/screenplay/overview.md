---
title: Language overview
description: The design principles of the Screenplay language, the top-level structure of a .play file, and a map of every construct.
---

Screenplay is a business-oriented language for specifying the desired functionality of an information system. It describes concepts, commands, facts, views, queries, automations, policies, and specifications without making a particular runtime authoritative. Screenplay now owns the versioned semantic foundation; Stage, Studio, and generated applications are being migrated to consume it capability by capability.

## Choose what you want to model

Suppose a librarian adds a book. Before choosing a database or UI framework, you can name the inputs, the librarian's permission, the ISBN rule, and the `BookAdded` fact. Those declarations are the model. Specifications make its acceptance and refusal cases checkable; views and screens describe how people use the resulting information.

This is an optional way to develop with the Cratis Platform, not a prerequisite for [Arc](/arc/) or [Chronicle](/chronicle/). If your handwritten implementation already makes the behavior clear, you do not need a `.play` model to keep using those components. [Why Screenplay](why-screenplay.md) discusses the trade-offs.

## From source to a consumer

| Step | What you have | What still needs checking |
| --- | --- | --- |
| Compile `.play` source | A syntax tree and source diagnostics | Whether the behavior binds to executable semantics |
| Bind supported behavior | A versioned executable semantic model (ESM) | Whether the chosen evaluator, renderer, or runtime admits its capabilities |
| Run reference specifications | Expected and actual outcomes for supported scenarios, evaluated in memory | Attached code, external systems, deployment, and consumer conformance |
| Render or run with a consumer | A consumer-specific realization | Its declared support, configuration, and application-level tests |

A board visualizes the declarations; it is not another execution step. Start with the [complete reference-tested model](getting-started.md), then [see its board](see-your-event-model.md). For the consumer contract, use [Interoperability](interoperability.md).

## File extension

Screenplay files use the `.play` extension. You may keep a small model in one file or split an application across [folders and imported files](folders.md); one physical file is not a limit on system size.

## Design principles

- **Indentation-based** — Python-style, no braces. Structure follows the offside rule: a construct owns everything indented beneath it.
- **Declarative first, bounded escape hatches** — behavior remains meaningful without implementation code. Selected implementation points can carry inline code or `file` references as realization attachments.
- **Slices are the atom of behavior** — commands, facts, and views live inside typed slices aligned with Event Modeling's vocabulary. Shared concepts, policies, and other application declarations can live outside them.
- **First-class sub-languages** — the Projection Declaration Language (PDL) and Change Data Capture Language (CDL) are built-in, independently consumable grammars. Inline language tags are extensible, while host construct keywords remain closed.
- **Concepts carry compliance** — value types declare PII and sensitivity attributes once, with the reason they are personal data; all usages inherit them.
- **`file` is never required** — a document must be expressible, and meaningful, before any code exists. Code pointers are realization metadata a slice gains once it is built.

## Top-level structure

```text
<domain>
<imports>
<application mode declarations>
<concepts and composite types>
<policies, personas, authentication, and onboarding>
<triggers>
<layouts, themes, and UI profiles>
<module>+
  <screen and dialog templates>
  <forms and contributions>
  <feature>+
    <feature>*          ← sub-features, arbitrarily deep
    <contributions>
    <slice>+
      <construct>+      ← commands, events, read models, queries, projections, specifications, reactions, captures, screens, constraints
<seeds>
```

The outline is a map, not a copyable file. The optional [domain](domain.md) names the business area; modules group features; each slice describes one behavior. [Concepts](concepts.md) name domain values such as `BookId`, rather than leaving every identifier as an interchangeable primitive. New models use `reaction batches atomic` and `command reads protected`; see [getting started](getting-started.md) for their placement and version requirement.

## Imports

An unquoted, qualified import names a cross-module contract. Imported types are available by their short name within the module:

```screenplay
import Customers.CustomerRegistered
import Customers.CustomerDetailsReadModel
```

These are declaration examples, not a complete application. Quoted imports instead load model source from a path or glob. Giving files the same domain does not load them; see [Imports](imports.md) for resolution rules.

## Comments

Line comments start with `//` and run to the end of the line.

## Language reference

| Topic | Page |
| --- | --- |
| The domain a file belongs to | [Domain](domain.md) |
| Formalized value types with compliance attributes | [Concepts](concepts.md) |
| Composite value types - the shapes events carry | [Types](types.md) |
| Named authorization rules | [Policies](policies.md) |
| Roles interacting with the application | [Personas](personas.md) |
| Modules, features, and the four slice types | [Modules, Features and Slices](slices.md) |
| Single-line and fenced multi-line descriptions | [Descriptions](slices.md#descriptions) |
| Event type declarations | [Events](events.md) |
| Commands, validation, the `produces` block, and handlers | [Commands](commands.md) |
| Read-side entry points, parameters and performers | [Queries](queries.md) |
| What a handler, a performer, a rule and a policy are given | [Contexts](context.md) |
| PDL-embedded projections | [Projections](projections/index.md) |
| CDL-embedded change data capture | [Captures](captures.md) |
| Events seeded per event source id | [Event seeding](seeding.md) |
| Server-side rules enforced before commit | [Constraints](constraints.md) |
| Reaction rules | [Reactions](reactions.md) |
| What sets a reaction off | [Triggers](triggers.md) |
| UI declarations at three abstraction levels | [Screens](screens.md) |
| Built-in sub-languages and extensible inline language tags | [Sub-languages and inline code](sub-languages.md) |
| The symbols Screenplay keeps on purpose, and why | [Notation conventions](notation.md) |
| The full EBNF grammar | [Grammar](grammar.md) |

## Tooling

The [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) NPM package provides a Monaco language service for `.play` files — syntax highlighting (including embedded C#/Java/TypeScript/React/HTML/SQL blocks and the PDL/CDL sub-languages), IntelliSense completions, hover documentation, and diagnostics. The [`screenplay-editor`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-editor) app hosts the editor standalone.

The [Screenplay VS Code extension](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/VSCodeExtension) brings the same language support to VS Code — a TextMate grammar with embedded-language and PDL/CDL highlighting, plus IntelliSense, hover, and diagnostics driven by the same shared language logic. `.play` files carry the Cratis icon.
