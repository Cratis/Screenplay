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
- **Construct** — a declaration inside a slice: `event`, `command`, `query`, `projection`, `capture`, `constraint`, `reaction`, or `screen`.
- **Offside rule** — the indentation rule that defines structure: a construct owns everything indented beneath it. There are no braces.

## Slice types

- **`StateChange`** — a command → events flow; something that changes the system.
- **`StateView`** — a query + projection + screen; something that reads the system.
- **`Automation`** — a reaction or reducer; something that runs when something happens.
- **`Translate`** — a capture; converts external data into events.

## Constructs

- **Concept** — a formalized value type that wraps a primitive (`concept InvoiceId : Uuid`) and carries compliance attributes, optionally with the reason each one applies; every usage inherits them.
- **Type** — a composite value type: a named shape built from several properties (`type InvoiceLine`), referenced by events, commands and other types.
- **Identifier** — the `identifier` modifier on a command property, marking the value a runtime resolves the event source id from. At most one per command.
- **Policy** — a named authorization rule (role-based, claim-based, or custom) that commands and queries reference by name via `authorize`.
- **Command** — an imperative intent with properties, `authorize`, `validate`, and a `produces` block declaring the events it appends.
- **Event** — a past-tense fact declaration: a named type and its properties.
- **Query** — a read-side entry point mapping identifying and filter parameters to a read-model return type (`=> ReadModel[]`), optionally with a `performer` that performs it.
- **Observable query** — a query whose return type is qualified with `observable` (`=> observable ReadModel[]`): a live read that keeps pushing as the read model changes, rather than answering once.
- **Performer** — the code that performs a query — an external `file` or an inline `csharp`/`sql` block. The query's counterpart to a command's `handler`.
- **Projection** — a declaration, written in PDL, that builds a read model by folding events (`from EventType key ...`).
- **Capture** — a declaration, written in CDL, that turns polled or pushed external data into events.
- **Constraint** — a server-side invariant (such as uniqueness) enforced in the Chronicle kernel before an event is committed.
- **Reaction** — behavior that runs when something happens, producing side effects: notifications, follow-up events, or commands. Chronicle's *reactor* is one thing that can perform one.
- **Application trigger** — a declared top-level signal with a payload shape, consumed by `reaction ... when`. `Startup` and `Shutdown` are built in. Written `trigger <Name>`; the bare word *trigger* in a backend context means this one. Not to be confused with an **interaction trigger** below.
- **Trigger data** — the values one occurrence of an application trigger hands the reaction.
- **Screen** — an instance: a UI declaration inside a `StateView` slice that names the template it fills and provides the content, expressible at three levels from pure intent to inline React.
- **Layout** — the application's base navigational look: the shell with its top bar, navigation, content and footer. An application has one, declared at the top level and selected by a `ui profile`.
- **Screen template** — a reusable shape with named slots that goes inside the shell, declared at module level and referenced by screens. An application has many; `fits slot` says which slot of its parent each one fills — matched by slot name, to any depth.
- **Dialog template** — a screen template for content that opens over the application. It declares no `fits slot`, because it occupies no slot.
- **Slot** — a named region a layout or template declares. One parent fills it, or it opens to many by declaring `contributes <ContributionPoint>`.
- **Arrangement** — how a layout or template positions the slots it declares: responsive `flow` or pixel-precise `freeform`.
- **Contribution point** — a named many-to-one extension point declared on a slot. `Navigation` is the first user of the mechanism.
- **Contribution** — one piece of content targeting a contribution point, declared with `contribute to`. It attaches to the nearest enclosing structure declaring a matching point.
- **UI profile** — a build's selections: `target platform`, `target size`, `layout`, `theme`, `packages`, `blueprint` and `start screen`. Names artifacts; does not contain them.
- **Package** — a named set of components a profile draws from, in override-priority order. `core` is the final fallback.
- **Blueprint** — a shipped bundle selected by a `ui profile`: layouts, shell chrome, a template set and theme tokens. Selected by name, like a theme; never declared in the document.
- **Size class** — `compact`, `regular` or `expanded`, on the width and height axes. A class, not a pixel breakpoint.

### Interaction

- **Interaction trigger** — what starts an interaction: the `on <thing>` clause of a behavior binding — `on click`, `on submit`, `on enter`, `on event <Event>`, `on interval <duration>`, or `on <ApplicationTrigger>`. Never declared, and never a top-level construct; it exists only inside a behavior. The counterpart to an **application trigger**, which is declared.
- **Action** — one declarative effect: `execute`, `navigate to`, `navigate back`, `open dialog`, `close dialog`, `refresh`, `set`, `notify`, `confirm`, `raise`. Every operand is a model reference, so an action that names nothing real is a diagnostic rather than a dead control.
- **Continuation** — the `on success` / `on failure` / `on result` block of an action, holding the actions that run after it. Only actions that can fail carry one.
- **Behavior** — a bundle of interaction-trigger-to-action bindings, attachable to an element, form, screen, template, layout, module or feature. Written inline as an anonymous `on` block, or declared as `behavior <Name>` with `parameter`s and attached with `uses`. Attachments are additive: a template's behavior and an element's both run.
- **Route** — the concrete address a renderer gives a screen. The document says `navigate to <Screen>`; the renderer decides that means `/invoicing/invoices`. Parameters come from the screen's `accepts` declarations.
- **Screen state** — a screen's declared values. `accepts` is route-backed, so it is shareable and reload-safe; `state` is transient and screen-local. Both are writable by `set`; nothing undeclared exists.

## Sub-languages

- **Sub-language** — a named grammar embedded inside a construct's body, parsed by a registered sub-parser. PDL and CDL are the built-ins; more can be registered.
- **PDL (Projection Declaration Language)** — the embedded sub-language for `projection` bodies.
- **CDL (Change Data Capture Language)** — the embedded sub-language for `capture` bodies.
- **Embedded code block** — an inline `csharp`, `typescript`, `react`, `html`, or `sql` block (between triple backticks) or a `file` reference — the escape hatch any construct can drop into.
- **Realization metadata** — a `file` reference or inline code block attached to a construct once it is implemented. Always optional: a document must be meaningful with none of it.
- **Context variables** — values the runtime supplies inside expressions: `$context` (the command or query context), `$env` (environment), `$eventContext`, and `$.` (the current capture item).
- **Context** — what a block of inline or file-referenced code is given, in scope as `context`. There is one per job: a **command context** and a **query context** (the command or arguments, the tenant, the caller, the causation, and when it was received — reachable declaratively through `$context.`), a **rule context** (what is under validation and who is calling), and a **policy context** (the caller and what the decision is about).
- **Identity** — the authorization view of the caller: identifier, display name, user name, whether authenticated, roles and claims. What a policy decides on.
- **Caused by** — the audit view of the same caller: subject, name and user name — the three values that travel with an appended event and that a projection reads through `$causedBy`.

## Tools and runtime

- **Stage** — the runtime that interprets a `.play` file and runs it as a live application.
- **Studio** — the tool that reads the same `.play` file to visualize and generate.
- **`@cratis/screenplay-language`** — the Monaco language service: highlighting, completions, hover, and diagnostics for `.play` files.
- **Screenplay editor** — the standalone browser editor host that embeds the language service.
- **Screenplay VS Code extension** — the extension bringing the same language support to VS Code.
