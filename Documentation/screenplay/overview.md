---
title: Language overview
description: The design principles of the Screenplay language, the top-level structure of a .play file, and a map of every construct.
---

Screenplay is a business-oriented language for specifying the desired functionality of an information system. It describes concepts, commands, facts, views, queries, automations, policies, and specifications without making a particular runtime authoritative. Screenplay now owns the versioned semantic foundation; Stage, Studio, and generated applications are being migrated to consume it capability by capability.

## File extension

Screenplay files use the `.play` extension.

## Design principles

- **Indentation-based** — Python-style, no braces. Structure follows the offside rule: a construct owns everything indented beneath it.
- **Declarative first, bounded escape hatches** — behavior remains meaningful without implementation code. Selected implementation points can carry inline code or `file` references as realization attachments.
- **Slices are the atom** — everything lives inside a typed slice aligned with Event Modeling's vocabulary.
- **First-class sub-languages** — the Projection Declaration Language (PDL) and Change Data Capture Language (CDL) are built-in, independently consumable grammars. Inline language tags are extensible, while host construct keywords remain closed.
- **Concepts carry compliance** — value types declare PII and sensitivity attributes once, with the reason they are personal data; all usages inherit them.
- **`file` is never required** — a document must be expressible, and meaningful, before any code exists. Code pointers are realization metadata a slice gains once it is built.

## Top-level structure

```text
<domain>
<imports>
<concepts and composite types>
<policies, personas, and authentication>
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

## Imports

Cross-module references. Imported types are available by their short name within the module.

```screenplay
import Customers.CustomerRegistered
import Customers.CustomerDetailsReadModel
```

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
| The full EBNF grammar | [Grammar](grammar.md) |

## Tooling

The [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) NPM package provides a Monaco language service for `.play` files — syntax highlighting (including embedded C#/TypeScript/React/HTML blocks and the PDL/CDL sub-languages), IntelliSense completions, hover documentation, and diagnostics. The [`screenplay-editor`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-editor) app hosts the editor standalone.

The [Screenplay VS Code extension](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/VSCodeExtension) brings the same language support to VS Code — a TextMate grammar with embedded-language and PDL/CDL highlighting, plus IntelliSense, hover, and diagnostics driven by the same shared language logic. `.play` files carry the Cratis icon.
