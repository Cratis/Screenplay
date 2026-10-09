---
id: 0031
title: State the event source and stream of specification events with the command route's own lines
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-07
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A specification has to say what the code it describes says. For events, Chronicle and Arc say more than Screenplay can today.

**What a specification can state now.** An event in `given`, `when append` or `then` can carry one child line, `for <value>`, naming the event source *id* it belongs to ([specifications](../Documentation/screenplay/specifications.md#syntax)). Since ESM v2 that value must be of the producing command's destination type. Nothing else about where the event lives can be written.

**What the runtime records.** Every appended Chronicle event carries an event source type, an event stream type and an event stream id, defaulting to `Default`, `All` and `"Default"` (Chronicle `EventForEventSourceId.cs`, `EventStreamId.Default`). Chronicle's own test scenarios append with them, for example `EventLog.Append<WarehouseEventSource>(source, event, "Receiving", "dock-3")` stores source type `Warehouse`, stream type `Receiving` and stream id `dock-3` (Chronicle `Testing.Specs/.../and_the_stream_is_declared.cs`). Arc puts all three on a command with `[EventSource<T>(stream)]` or `[EventSourceType]`/`[EventStreamType]`, and the stream id comes from `[EventStreamId]` or `ICanProvideEventStreamId`, a method on the command that returns one string (Arc `EventStreamIdValuesProvider.cs`). That method is where applications build composite stream ids today, such as a project and a period joined into one value.

**Why it matters.** Screenplay already lets a model declare `eventsource Account` with `stream Transactions` and route a command with `stream Account.Transactions` / `streamId = month` ([event sources](../Documentation/screenplay/event-sources.md)). [#407](https://github.com/Cratis/Screenplay/issues/407) makes those routes part of the executable model (ESM), so a routed command's facts carry a route. But #407 deliberately leaves `given` events without a route, and its choice table answers "route assertions in specifications?" with *"Not yet — no syntax exists"*. [#457](https://github.com/Cratis/Screenplay/issues/457) asks for that syntax. Without it:

- Arc's Screenplay generator ([Cratis/Arc#2887](https://github.com/Cratis/Arc/issues/2887)), which already turns `EventScenario` code into `when append`, has to either drop a scenario that seeds or expects events in a non-default stream, or write one that means something else.
- Stage renders specification tests with Chronicle's defaults, so a rendered test cannot seed history where the routed command will look for it.
- A `given` without `for` carries no source at all: the reference runner keeps a null destination and asserts nothing about where the event lives. A generator that renders such a fact has to pick a source, usually the command's own, so a scenario that seeds history on another source silently changes meaning.
- The v2 `for` rule blocks history for a source that no command in the model produces, for example an event that only arrives through a capture or from another system.

The #457 comment adds a requirement: composite stream ids (for example project + period) are common, so the specification must accept a stream id value the same way the command route composes it. Command routes cannot compose a stream id yet: a route takes one `streamId = <source>` mapping with a scalar source, and a stream's `streamId` type must be a non-optional scalar (`Parsing/EventSourceParser.cs`, `Parsing/EventSourceValidator.cs`).

Rules this record has to fit:

- Notation ([#81](https://github.com/Cratis/Screenplay/issues/81), [#350](https://github.com/Cratis/Screenplay/issues/350)): prefer an indented body to a call-like form, a qualified operand to new sub-clause keywords, and a statement about the system to an expression. Dotted paths and `=` are retained conventions.
- Specification blocks are a header line (`given <Event>`) followed by indented children: payload lines `property = value` and the context line `for <value>`.
- Command routes ([#302](https://github.com/Cratis/Screenplay/issues/302), [0023](0023-command-production-model.md#event-sources-streams-and-concurrency)) are a child line `stream Source.Stream` with a nested `streamId = <value>` exactly when the stream is keyed. A route never supplies `for`. #302 rejected one-line header clauses (`produces event X for Account accountId in Transactions month`) as too implicit.
- Stream ids use 0023's portable formatting: text unchanged, UUIDs lowercase hyphenated, integer concepts invariant decimal; no provider picks its own conversion. #407 adds that an empty text id is refused, because Chronicle stores it as `"Default"` and two keys would collide.
- New syntax may land before any ESM version admits it; the binder refuses it with `PLAY0268` until then ([0004](0004-admission-and-governance-of-portable-executable-semantics.md)). Version numbers are assigned at the release-ready claim, not in advance ([0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)).

## Decision

Adopt option B. An event in `given`, `when append` or `then` states its route with the child lines a command route uses: `stream Source.Stream` and, for a keyed stream, a nested `streamId = <literal>`. `for` stays its own child line. A `then` event can also state `no stream` to assert that the fact is unrouted. A `then` event with neither line does not compare the route. Stream ids are scalar; composite stream ids are deferred to [#462](https://github.com/Cratis/Screenplay/issues/462). Renderers and generators preserve every route statement or report what they cannot express. The syntax lands first and is refused with `PLAY0268` until an ESM version admits it.

### Spelling

```screenplay
specification DepositingInAnotherMonthDoesNotTouchSeptember
  given FundsDeposited
    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    stream Account.Transactions
      streamId = 202609
    amount = 100
  when DepositFunds
    accountId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    month = 202610
    amount = 40
  then FundsDeposited
    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    stream Account.Transactions
      streamId = 202610
    amount = 40
```

The issue's example, an event seeded on another source, reads:

```screenplay
  given AuthorRegistered
    for "other"
    stream Author.Profile
```

An expectation that a fact carries no route reads:

```screenplay
  then ReminderScheduled
    no stream
    dueOn = "2026-10-31"
```

- **Placement.** `stream` and `no stream` are child lines of the event block, beside `for` and the payload lines, in any order. An event takes at most one of them, once. `streamId` is nested under `stream`, never at event level.
- **Routing lines are not payload.** A routing line has no `=`; a payload line always has one. A payload property named `stream` or `streamId` stays a payload line (`stream = ...`), exactly as production payload lines stay payload under a routed command.
- **Reference.** `Source.Stream` resolves with the command route's rules: exact names, the complete compilation input, ambiguity reported rather than guessed.
- **Value.** The `streamId` value is a concrete scalar literal of the stream's declared `streamId` type, checked like any specification value and formatted by 0023's portable rules. It is required exactly when the stream is keyed and refused when it is not. Empty text is refused, as in #407.
- **Opaque text ids.** A text stream id such as `"p-1:2026-10"` is one opaque value. It is valid and passes through unchanged (0023); Screenplay never splits or reinterprets it.

### Which blocks take it

| Block | Routing line | Meaning |
| --- | --- | --- |
| `given <Event>` | `stream` | The fact is placed on that source and stream. `for` is required. |
| `when append <Event>` | `stream` | The appended fact carries that route. `for` is required. |
| `then <Event>` | `stream` or `no stream` | The new fact must carry exactly that route, or no route. `for` is optional. |
| `when <Command>` | Refused | The route comes from the command declaration. Assert it with a `then` event. |
| `given readmodel`, `then readmodel`, `then no readmodel`, `then query` | Not applicable | Read models have keys, not routes; keys belong to [#459](https://github.com/Cratis/Screenplay/issues/459). |

`no stream` on `given` or `when append` is refused: an event without a `stream` line is already unrouted there, as #407 defines. There is no `then no <Event>` form today, so nothing applies there.

### Unrouted facts

An unrouted fact carries no route in the executable model's facts and outcomes: the ESM field is absent, not a route with default values. When a provider stores an unrouted fact in Chronicle it gets Chronicle's defaults, source type `Default`, stream type `All` and stream id `"Default"`. Generators and renderers map in both directions on that basis:

- An unrouted fact is appended through the plain path (`Append(eventSourceId, event)` or `Given.ForEventSource(id)`), which writes those defaults.
- Reading Chronicle code or data back, only the complete default triple means unrouted. Missing, partial or unreadable routing metadata is not "unrouted"; it is a loss to report. A `then` written from it states nothing about the route rather than `no stream`.

### Source-only routes

Arc's `[EventSource<T>]` without a stream and Chronicle's `Append<TSource>(id, event)` route through a source with no stream (stream type `All`). Specifications mirror exactly the route vocabulary that #407 admits for commands. If #407 admits source-only routes, specifications get the same form on the same terms. Until then there is no source-only spelling, and a generator meeting a source-only append must report a classified loss or refusal; it never invents a stream.

### Composite stream ids

Command routes cannot compose a stream id today, so #457 delivers scalar stream ids only. Composite stream ids are deferred to [#462](https://github.com/Cratis/Screenplay/issues/462), which needs its own decision on how a stream declares parts and how a route maps them. When #462 defines them, specifications mirror the route's parts with literal values, never a joined string standing in for declared parts. The "never pre-joined" rule applies only to declared composite keys; a scalar text id stays opaque as above. #462 should align its spelling with composite read-model keys ([#459](https://github.com/Cratis/Screenplay/issues/459)) where that is sensible; the two are separate decisions.

### Values: `for` and the source's identifier

- **No routing line: unchanged.** The v2 rule still applies, so every existing specification keeps its meaning, diagnostics and ESM bytes.
- **`given` and `when append` with `stream`:** `for` is required and is a concrete literal of the named source's declared `identifier` type. It never defaults to the command's identifier. A producer is not needed, so history for a source nobody in the model produces can be stated.
- **`then` with `stream`:** `for` is optional and typed the same way when written. Leaving it out deliberately weakens the comparison to the route alone.
- **Routing belongs to occurrences, not event types.** A `given` or `when append` fixture is not refused because some command produces the same event with a different destination type, or through a different source. The fixture describes one occurrence.
- **Contradiction with the command under test.** A specification is refused only when a `then` event states a route that the command under test's own route or destination contradicts, for example a different source, or a `for` type the command's destination cannot produce, and only when that event can come from nothing but the command under test. When a reaction reachable from the scenario, or a command it invokes, could also produce the event, the binder defers to the run-time comparison, as specification outcome checks already do for reactions. This is a modeling error the binder can see; a failed comparison at run time is left for everything else.
- **Source without `identifier`.** A routed fixture needs the source to declare an `identifier` type. Where it does not, the v2 producer rule supplies the type only when it is unambiguous: exactly one destination type across the event's producers. Otherwise the specification is refused with a message asking for an `identifier` on the source.
- `when <Command> for <value>` keeps its current meaning.

### How the runner uses it

- **`given`** records the fact with its route. **`when append`** appends with its route. In #407's admitted scope routes never change projections, reducers, reactions, constraints or queries, so a route on `given` changes no outcome of the reference runner. It still matters: a renderer seeds the history in the stated stream, so a rendered test against Chronicle and Arc runs against the same history the specification describes.
- **`then` with `stream`** compares the route as part of the fact, like the event type, payload and `for`: source, stream and formatted stream id must match exactly. An unrouted fact never matches.
- **`then` with `no stream`** matches only an unrouted fact.
- **`then` with neither** does not compare the route. This mirrors `for`, which is compared only when written, and keeps existing specifications passing when their command later gains a route.
- **Reactions.** A command invoked by a reaction carries its declared route like any routed command; a reaction's direct productions stay unrouted under #407.
- **`then events in any order`** keeps the exact count of new facts and passes when some one-to-one assignment of expected events to facts exists. Because wildcard (no routing line) and exact expectations can match the same fact, the matcher must search for an assignment (bipartite matching), not take the first match for each expectation in order. Today's matcher (`SemanticSpecificationRunner.CompareFacts`) is greedy. Optional `for` already lets expectations differ in strictness, and routes make that common, so the matcher changes with this work.
- **Constraints and concurrency.** Constraints are not scoped to streams yet (deferred by 0023), and the reference runner does not execute concurrency. A `given` route therefore does not change which constraint values are claimed or which append conflicts. When constraint scopes, `from` filters or concurrency execution are admitted, they read the routes this record places on `given` facts; the spelling does not change.

### Preservation by renderers and generators

Every tool that emits specifications or code from them (Stage, Arc's Screenplay generator, MCP export) must preserve each explicit `stream` statement and `no stream` assertion, or report an explicit diagnostic naming what it cannot express. It never silently drops, defaults or changes a route. The Chronicle APIs to use:

| Specification | Chronicle test code |
| --- | --- |
| Unrouted `given` / `when append` | `scenario.Given.ForEventSource(id).Events(...)` / `scenario.When.ForEventSource(id).Events(...)`. These append through the default path and carry only the id, so they cannot express a route. |
| Routed `given` / `when append` | `scenario.EventLog.Append<TSource>(id, event, stream, streamId)` (`EventSequenceEventSourceExtensions`), which resolves source type and stream type from the event source definition (`ResolvedEventRouting`) and stores the stream id. |
| Routed command | Arc `[EventSource<TSource>(stream)]` on the command, with the stream id from `[EventStreamId]` or `ICanProvideEventStreamId`. |
| `then` with `stream` or `no stream` | Assert the appended event's event source type, event stream type and event stream id; for `no stream`, assert the default triple. |

### Representation

- **Syntax and AST.** Specification event steps gain an optional route (source and stream reference with location, optional stream id value) or an unrouted marker on `then`. Syntax JSON is additive. The printer prints `stream` or `no stream` after `for` and before payload lines; printing and reparsing gives the same syntax. Typed AST edits and MCP authoring add, replace and remove them like other specification children; renaming a source or stream repairs these references like command routes.
- **ESM.** Each `given`, `when append` and `then` event fixture gains an optional `route` with the same source, stream and stream-id encoding as #407's command route, except that the stream id is a literal. A `then` fixture can instead state that the fact is unrouted. A model selects the admitting version only when it uses the feature; every other model keeps its version, bytes and revision.
- **Both compilers.** The C# and TypeScript compilers, Monaco and VS Code accept, validate and print the same forms.

### Versioning

The syntax lands when this record is accepted and is refused with `PLAY0268` ("not admitted by any supported executable model version yet", naming the feature, not a number). If specification routes meet the admission evidence together with #407, they share #407's version. Otherwise they take the next claimed version, and #407 is not delayed for them. No number is assigned now (0025).

## Options considered

### Spelling

- **A. Header clauses, as in the issue: `given AuthorRegistered for "other" in Author.Profile`.** Rejected. `for` is a child line today, so the source id would get a second spelling. The stream id has no natural place on the line (`in Account.Transactions 202610` is positional). #302 rejected the same shape for productions as too implicit.
- **B. Child lines that reuse the command route.** Chosen. One spelling for "where events land" in commands and specifications, indentation instead of parentheses, a qualified operand instead of new keywords. Cost: two or three more lines per routed event.
- **C. A one-line child with a new keyword: `in Account.Transactions 202610`.** Rejected. It adds a keyword the command side does not use and the stream id is positional again.
- **D. A grouping block: `route` with `for`, `stream` and `streamId` beneath it.** Rejected. It moves the existing `for` line (breaking every v2 specification, or two places to write it) and adds a keyword with no meaning of its own.
- **E. A default for the whole specification** (`given stream Account.Transactions` applying to later events). Rejected. Order-dependent and implicit, and one scenario often spans several streams.
- **F. Infer the route from the producing command, no syntax.** Rejected. It fails for events with no producer, several producers with different routes, captured or external events, and `when append`, and it cannot supply a stream id value.

### `then` without a route

- **Not compared, with an explicit `no stream` assertion.** Chosen. Matches `for`, survives a command gaining a route, and still lets a specification pin that a fact is unrouted.
- **Must be unrouted by default.** Rejected: adding a route to a command would break every specification of it, and the failure would be about wiring, not behavior.
- **Not compared, with no unrouted assertion.** Rejected: generators could not round-trip an explicitly default-routed event, and "unrouted" would stay unstateable.

### `for` typing

- **Refuse any fixture whose event has a producer with a different destination type.** Rejected. Routing belongs to occurrences: the same event can legitimately land on different sources, and the rule would block valid history.
- **Type by the named source's identifier; refuse only contradictions with the command under test.** Chosen.

### Composite stream ids

- **Specify them now, ahead of routes.** Rejected. Specifications would get a shape routes do not have, and the joining format belongs to the portable formatter.
- **Defer to #462 and mirror its route parts with literals.** Chosen.
- **A single pre-joined text value for declared parts.** Rejected for declared composite keys: it hard-codes a separator and order no one has decided. A scalar text id that happens to contain a separator is unaffected.

### Version

- **Share #407's version when the evidence is ready together; otherwise the next claimed one.** Chosen. Consumers bump once when possible, and #407 is never held back.
- **Always join #407.** Rejected: it could delay #407.
- **Always a later version.** Rejected: consumers would adopt routes twice even when both were ready.

## Default if unanswered

Specifications cannot state an event's source type, stream or stream id. #407 still ships routes on command facts, but authors cannot assert them. Arc's generator keeps dropping or flattening scenarios that use non-default streams, Stage renders specification tests on Chronicle's default stream, and seeded history on another source keeps depending on the producer rule or the command's own source.

## Timeline and scope

The decision holds until per-production route overrides, reaction and reducer `from` filters, constraint scopes, source-only routes or composite stream ids are designed. Those extend it (a per-production override is asserted the same way, on the `then` event); none should need to change its spelling. Syntax can start now; execution waits for #407's implementation.

In scope: `stream`, `streamId` and `no stream` child lines on specification events with their checks; the refusal under `when <Command>`; the `for` typing rule for routed events; the definition of unrouted facts and their Chronicle mapping; `then` comparison, `given` placement and assignment-based any-order matching in the reference runner; the ESM fixture route; the preservation contract; the version rule; both compilers, printer, typed AST, MCP, editors and documentation.

Out of scope: composite stream ids ([#462](https://github.com/Cratis/Screenplay/issues/462)); source-only routes until #407 admits them for commands; read-model keys ([#459](https://github.com/Cratis/Screenplay/issues/459)); per-production overrides; `from` filters; constraint scopes; concurrency execution; Chronicle's stream completion; any change to `when <Command> for` or to specifications without a routing line.

## Verification

**Done when** an event in `given`, `when append` or `then` can state `stream Source.Stream` with `streamId` for a keyed stream, a `then` event can state `no stream`, both compilers parse, validate, print and round-trip these identically, the binder refuses them with `PLAY0268` until the admitting version, and in that version the reference runner places routed `given` and `when append` facts and compares `then` routes as defined above, while every specification without a routing line keeps its bytes, revision and outcome.

**Verify by**:

- Parser, printer and validator specifications in C# and TypeScript for: a keyed and an unkeyed stream; missing and superfluous `streamId`; empty text and wrong-type stream ids; an opaque text id such as `"p-1:2026-10"` passing through unchanged; a payload property named `stream`; both `stream` and `no stream` on one event (refused); `no stream` on `given` and `when append` (refused); a routing line under `when <Command>` (refused); `stream` without `for` on `given` and `when append` (refused) and on `then` (accepted).
- `for` typing: a fixture for an event with no producer binds on a source with an `identifier`; a fixture whose event has a producer with a different destination type is accepted; a `then` that contradicts the command under test's route or destination is refused; a source without `identifier` falls back only to a single unambiguous producer type; specifications without a routing line keep every existing v2 diagnostic.
- Canonical vectors in the admitting version: routed `given`, `when append` and `then` fixtures with UUID-, text- and integer-keyed streams; a `then` that fails on a different stream id, a different stream and an unrouted fact; a `then` without a routing line passing against a routed fact; `no stream` passing against an unrouted fact and failing against a routed one; a reaction-invoked routed command whose fact carries its route and a reaction's direct production that stays unrouted; a `when A` whose reaction invokes command B routed to another source, with a `then` stating B's route, which binds and passes; `then events in any order` mixing a wildcard and an exact routed expectation for the same event type, ordered so that greedy matching fails while an assignment exists, which must pass. All earlier goldens and corpora stay byte-identical.
- Preservation: Stage and Arc's generator emit `stream` lines for a Chronicle `EventLog.Append<TSource>(id, event, stream, streamId)` scenario, emit `no stream` or report a loss as defined above, and report a classified loss for a source-only append; none drops a route silently.
- The `DepositFunds` sample above executes in the reference runner.

## Consequences

- **Easier:** writing a specification for any routed command and reading where its history lives, generating and rendering specifications without losing the route, seeding history for sources no command in the model produces, and pinning that a fact is unrouted.
- **Harder:** routed specifications are two to three lines longer per event. Tools that edit specification events (printer, AST, MCP, rename) carry one more child. The `for` rule has two branches. The any-order matcher becomes an assignment search. Every renderer and generator takes on the preservation contract.
- **Forecloses:** a one-line header spelling for routes in specifications, and pre-joined text for declared composite stream ids. Once models use the lines, changing the spelling costs a migration, which is why this is costly to reverse.

## Verdict

The decider delegated this verdict to the orchestrating agent on 2026-10-07; the choices below were made under that delegation after an independent cross-provider critique.

1. **Spelling?** B, `stream Source.Stream` with a nested `streamId = <literal>` as child lines, `for` kept separate: one notation for where events land, in commands and specifications.
2. **`then` without a `stream` line?** The route is not compared, and `no stream` is added now to assert an unrouted fact: compatibility like optional `for`, without leaving "unrouted" unstateable.
3. **Is `for` required with `stream`?** Required on `given` and `when append`, optional on `then`: seeded history must name its instance, while an expectation may deliberately compare less.
4. **How is `for` typed?** By the named source's identifier, refused only when the command under test contradicts it: routing belongs to occurrences, not event types.
5. **Composite stream ids?** Deferred to #462 with its own decision; #457 delivers scalar ids, and specifications will mirror #462's route parts with literals: routes cannot compose ids yet, and opaque text ids stay valid.
6. **Which ESM version?** #407's if both meet the admission evidence together, otherwise the next claimed one, no number now: consumers bump once when possible and #407 is never delayed.

## Status notes

**2026-10-09.** The executable half of this decision joined claimed ESM v8 under [0036](0036-admit-event-sources-streams-and-command-routes.md).
