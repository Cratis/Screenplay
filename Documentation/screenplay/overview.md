---
title: Language overview
description: The design principles of the Screenplay language, the top-level structure of a .play file, and a map of every construct.
---

Screenplay is the modeling language for the Cratis platform. It lets developers describe a complete bounded context — events, commands, queries, projections, screens, automations, authorization, validation, constraints, and concepts — in a single declarative file. Stage interprets a Screenplay and runs it as a live application; Studio uses the same contract to visualize and generate.

## File extension

Screenplay files use the `.play` extension.

## Design principles

- **Indentation-based** — Python-style, no braces. Structure follows the offside rule: a construct owns everything indented beneath it.
- **Declarative first, imperative escape hatch** — every construct has a declarative form; any construct can drop into C#, TypeScript, React, or HTML via inline code blocks or `file` references.
- **Slices are the atom** — everything lives inside a typed slice aligned with Event Modeling's vocabulary.
- **Sub-language pluggability** — the Projection Declaration Language (PDL) and Change Data Capture Language (CDL) are embedded sub-grammars. Additional sub-languages can be registered and parsed inside named constructs.
- **Concepts carry compliance** — value types declare PII and sensitivity attributes once; all usages inherit them.

## Top-level structure

```text
<domain>
<imports>
<concepts>
<policies>
<module>
  <layouts>
  <feature>+
    <feature>*          ← sub-features, arbitrarily deep
    <slice>+
      <construct>+      ← events, commands, queries, projections, captures, reactors, screens, constraints
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
| Named authorization rules | [Policies](policies.md) |
| Modules, features, and the four slice types | [Modules, Features and Slices](slices.md) |
| Event type declarations | [Events](events.md) |
| Commands, validation, the `produces` block, and handlers | [Commands](commands.md) |
| Read-side entry points | [Queries](queries.md) |
| PDL-embedded projections | [Projections](projections/index.md) |
| CDL-embedded change data capture | [Captures](captures.md) |
| Server-side rules enforced before commit | [Constraints](constraints.md) |
| Event reaction rules | [Reactors](reactors.md) |
| UI declarations at three abstraction levels | [Screens](screens.md) |
| Registering additional embedded sub-languages | [Sub-language Pluggability](sub-languages.md) |
| The full EBNF grammar | [Grammar](grammar.md) |

## Tooling

The [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) NPM package provides a Monaco language service for `.play` files — syntax highlighting (including embedded C#/TypeScript/React/HTML blocks and the PDL/CDL sub-languages), IntelliSense completions, hover documentation, and diagnostics. The [`screenplay-editor`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-editor) app hosts the editor standalone.

The [Screenplay VS Code extension](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/VSCodeExtension) brings the same language support to VS Code — a TextMate grammar with embedded-language and PDL/CDL highlighting, plus IntelliSense, hover, and diagnostics driven by the same shared language logic. `.play` files carry the Cratis icon.
