---
title: Cratis Screenplay language overview
description: Cratis Screenplay organizes business behavior into typed slices, with shared concepts and policies; this overview maps the declarations and language reference.
---

Cratis Screenplay describes business behavior in a portable `.play` model that people and AI agents can read. Events record facts, commands express requests, and read models provide queryable views of those facts. Screens describe how people use the behavior; specifications give examples of the expected outcomes.

Use this page to understand how the declarations fit together and find their language reference. To write a small model first, follow [Getting started](getting-started.md). If you are deciding what to model, read [Why Screenplay](why-screenplay.mdx).

Screenplay modeling is language-neutral. A model does not select C# as its implementation language; rendering targets decide how to realize it, and code attachments must match the target that consumes them. See [Sub-languages and inline code](sub-languages.md) for the supported tags and target boundary.

The compiler binds declarations to an executable semantic model (ESM), a versioned description of their meaning. Where Cratis Chronicle supplies runtime behavior, the ESM follows it using portable names. Tools that consume the ESM must preserve that meaning and explicitly admit the versions and capabilities they support. A valid declaration does not establish that every target can execute it.

To check scenarios without a target runtime, use the [reference execution path](specifications.md#reference-execution). It runs admitted capabilities in memory without starting Arc, Chronicle, a database, or a network service. Its projection behavior still follows Chronicle.

## File extension

Save Screenplay source in `.play` files. A model can stay in one file or use imports to bring several files together.

## Design principles

Indentation defines ownership: a construct owns what is indented beneath it, without braces. Modules and features group typed slices, which organize behavior using Event Modeling's vocabulary. Shared declarations such as concepts and policies can sit outside those slices.

The model is meaningful before implementation code exists. Selected implementation points accept constrained inline code or a `file` reference when a target needs custom logic; these attachments do not replace the model's portable meaning.

Two built-in sub-languages handle specialized declarations: the Projection Declaration Language (PDL) describes how events build read models, and the Change Data Capture Language (CDL) describes capturing external changes. Both have independently consumable grammars. Inline code language tags can be extended; Screenplay's own construct keywords cannot.

Concepts define value types and their classifications once. Usages inherit personal-data and sensitivity attributes, including the reason a value is personal data. This carries the classification through the model, not a guarantee of application compliance.

## Top-level structure

Use this outline to decide where a declaration belongs. Shared types and configuration precede the modules; features group slices inside each module. Nested features let a larger model keep its business structure.

```text title="Structure of a .play file"
<domain>
<imports>
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

## Imports

When one module refers to types declared in another, import them to use their short names:

```screenplay title="Cross-module imports"
import Customers.CustomerRegistered
import Customers.CustomerDetailsReadModel
```

## Comments

Use `//` for a line comment. It runs to the end of the line and does not change the model's behavior.

## Language reference

Choose the declaration you need to add or understand. Each page explains its syntax and relevant execution limits.

| Topic | Page |
| --- | --- |
| The domain a file belongs to | [Domain](domain.md) |
| Composing a model across files | [Imports](imports.md) |
| Formalized value types with compliance attributes | [Concepts](concepts.md) |
| Composite value types - the shapes events carry | [Types](types.md) |
| Named authorization rules | [Policies](policies.md) |
| Roles interacting with the application | [Personas](personas.md) |
| Authentication and identity configuration | [Authentication](authentication.md) |
| Modules, features, and the four slice types | [Modules, Features and Slices](slices.md) |
| Single-line and fenced multi-line descriptions | [Descriptions](slices.md#descriptions) |
| Event type declarations | [Events](events.md) |
| Commands, validation, the `produces` block, and handlers | [Commands](commands.md) |
| External effects and compensation declarations | [Operations and external systems](operations.md) |
| Queryable state declarations | [Read models](readmodels.md) |
| Read-side entry points, parameters and performers | [Queries](queries.md) |
| What a handler, a performer, a rule and a policy are given | [Contexts](context.md) |
| PDL-embedded projections | [Projections](projections/index.md) |
| CDL-embedded change data capture | [Captures](captures.md) |
| Scenarios, execution scope, and syntax-only forms | [Specifications](specifications.md) |
| Events seeded per event source id | [Event seeding](seeding.md) |
| Server-side rules enforced before commit | [Constraints](constraints.md) |
| Reaction rules | [Reactions](reactions.md) |
| What sets a reaction off | [Triggers](triggers.md) |
| UI declarations at three abstraction levels | [Screens](screens.md) |
| Reusable screen structure | [Layouts and templates](templates.md) |
| Command input forms | [Forms](forms.md) |
| Navigation structure | [Navigation](navigation.md) |
| Item lists a template offers | [Outlets](outlets.md) |
| Built-in sub-languages and extensible inline language tags | [Sub-languages and inline code](sub-languages.md) |
| The symbols Screenplay keeps on purpose, and why | [Notation conventions](notation.md) |
| The full EBNF grammar | [Grammar](grammar.md) |

## Tooling

For editor feedback while writing a model, follow [Getting started](getting-started.md) to launch the standalone editor or the VS Code extension from source. Both provide highlighting, completions, hover documentation, and diagnostics. Highlighting covers the PDL/CDL sub-languages and embedded C#/Java/TypeScript/React/HTML/SQL blocks.

If you are embedding an editor, the [`@cratis/screenplay-language`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-language) package provides the shared Monaco language service. The [`screenplay-editor`](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/Monaco/screenplay-editor) app hosts it standalone. The [Screenplay VS Code extension](https://github.com/Cratis/Screenplay/tree/main/Source/Screenplay/VSCodeExtension) uses a TextMate grammar and the shared language logic; `.play` files carry the Cratis icon.

## See a model run

To try a supported model through its commands, queries, and screens, follow the [Cratis Stage sandbox walkthrough](/stage/docker/). Stage holds state in memory and loses it on a container restart. It is not a production host; check admission diagnostics and bind published ports to `127.0.0.1`.
