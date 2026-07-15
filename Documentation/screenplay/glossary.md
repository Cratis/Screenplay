---
title: Glossary
description: One precise line per Screenplay term — the structure of a .play file, its constructs, its sub-languages, and the tools that run and visualize it.
---

The vocabulary of the Screenplay language, defined once. For the underlying event-sourcing terms these build on — event, event source, read model, projection, reducer, reactor, observer — see the shared [Cratis glossary](/glossary/); this page defines what is specific to Screenplay.

## Structure

- **`.play` file** — a Screenplay source file. Describes one bounded context, top to bottom, in one declarative document.
- **Module** — the top-level namespace of a `.play` file; maps to a bounded context. One module per file by convention.
- **Feature** — a vertical grouping of related slices inside a module. Nests arbitrarily deep for sub-features.
- **Slice** — the atomic unit of behavior, aligned with Event Modeling. Has a type and a name and contains the constructs that implement one behavior.
- **Construct** — a declaration inside a slice: `event`, `command`, `query`, `projection`, `capture`, `constraint`, `reactor`, or `screen`.
- **Offside rule** — the indentation rule that defines structure: a construct owns everything indented beneath it. There are no braces.

## Slice types

- **`StateChange`** — a command → events flow; something that changes the system.
- **`StateView`** — a query + projection + screen; something that reads the system.
- **`Automation`** — a reactor or reducer; something that reacts to events.
- **`Translate`** — a capture; converts external data into events.

## Constructs

- **Concept** — a formalized value type that wraps a primitive (`concept InvoiceId : Uuid`) and carries compliance attributes; every usage inherits them.
- **Policy** — a named authorization rule (role-based, claim-based, or custom) that commands and queries reference by name via `authorize`.
- **Command** — an imperative intent with properties, `authorize`, `validate`, and a `produces` block declaring the events it appends.
- **Event** — a past-tense fact declaration: a named type and its properties.
- **Query** — a read-side entry point mapping identifying and filter parameters to a read-model return type (`=> ReadModel[]`).
- **Projection** — a declaration, written in PDL, that builds a read model by folding events (`from EventType key ...`).
- **Capture** — a declaration, written in CDL, that turns polled or pushed external data into events.
- **Constraint** — a server-side invariant (such as uniqueness) enforced in the Chronicle kernel before an event is committed.
- **Reactor** — a rule that observes events and produces side effects — notifications, follow-up events, or commands.
- **Screen** — a UI declaration inside a `StateView` slice, expressible at three levels from pure intent to layout with inline React.
- **Layout** — a reusable screen template with named slots, declared at module level and referenced by screens.

## Sub-languages

- **Sub-language** — a named grammar embedded inside a construct's body, parsed by a registered sub-parser. PDL and CDL are the built-ins; more can be registered.
- **PDL (Projection Declaration Language)** — the embedded sub-language for `projection` bodies.
- **CDL (Change Data Capture Language)** — the embedded sub-language for `capture` bodies.
- **Embedded code block** — an inline `csharp`, `typescript`, `react`, or `html` block (between triple backticks) or a `file` reference — the escape hatch any construct can drop into.
- **Context variables** — values the runtime supplies inside expressions: `$context` (event context), `$env` (environment), `$eventContext`, and `$.` (the current capture item).

## Tools and runtime

- **Stage** — the runtime that interprets a `.play` file and runs it as a live application.
- **Studio** — the tool that reads the same `.play` file to visualize and generate.
- **`@cratis/screenplay-language`** — the Monaco language service: highlighting, completions, hover, and diagnostics for `.play` files.
- **Screenplay editor** — the standalone browser editor host that embeds the language service.
- **Screenplay VS Code extension** — the extension bringing the same language support to VS Code.
