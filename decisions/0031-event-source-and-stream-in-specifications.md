---
id: 0031
title: State the event source and stream of specification events with the command route's own lines
status: proposed
stage: none
class: contract
reversibility: costly
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
- A `given` without `for` is read as belonging to the command's own source. A scenario that seeds history on another source silently changes meaning.
- The v2 `for` rule blocks history for a source that no command in the model produces, for example an event that only arrives through a capture or from another system.

The #457 comment adds a requirement: composite stream ids (for example project + period) are common, so the specification must accept a stream id value the same way the command route composes it.

Rules this record has to fit:

- Notation ([#81](https://github.com/Cratis/Screenplay/issues/81), [#350](https://github.com/Cratis/Screenplay/issues/350)): prefer an indented body to a call-like form, a qualified operand to new sub-clause keywords, and a statement about the system to an expression. Dotted paths and `=` are retained conventions.
- Specification blocks are a header line (`given <Event>`) followed by indented children: payload lines `property = value` and the context line `for <value>`.
- Command routes ([#302](https://github.com/Cratis/Screenplay/issues/302), [0023](0023-command-production-model.md#event-sources-streams-and-concurrency)) are a child line `stream Source.Stream` with a nested `streamId = <value>` exactly when the stream is keyed. A route never supplies `for`. #302 rejected one-line header clauses (`produces event X for Account accountId in Transactions month`) as too implicit.
- Stream ids use 0023's portable formatting: text unchanged, UUIDs lowercase hyphenated, integer concepts invariant decimal; no provider picks its own conversion. #407 adds that an empty text id is refused, because Chronicle stores it as `"Default"` and two keys would collide.
- New syntax may land before any ESM version admits it; the binder refuses it with `PLAY0268` until then ([0004](0004-admission-and-governance-of-portable-executable-semantics.md)). Version numbers are assigned at the release-ready claim, not in advance ([0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)).

## Decision

*Proposed, not decided.* Adopt option B below. An event in `given`, `when append` or `then` may state its route with the same child lines a command uses: `stream Source.Stream`, and for a keyed stream a nested `streamId = <value>` whose value is a concrete literal. When an event states a stream, its `for` value is typed by the source's declared `identifier`, not by the producing command. A `then` event without a `stream` line does not compare routes. A `given` or `when append` event without one is unrouted, as #407 already defines. The syntax lands first and is refused with `PLAY0268`; the reference runner places and compares routes in the ESM version that admits #407's routes, or the next one if that version is claimed first. The details below are part of the proposal.

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

- **Placement.** `stream` is a child line of the event block, beside `for` and the payload lines, in any order, at most once. `streamId` is nested under `stream`, never at event level. A payload property named `stream` or `streamId` stays a payload line (`stream = ...`), exactly as production payload lines stay payload under a routed command.
- **Reference.** `Source.Stream` resolves with the command route's rules: exact names, the complete compilation input, ambiguity reported rather than guessed.
- **Value.** The `streamId` value is a concrete literal of the stream's declared `streamId` type, checked like any specification value and formatted by 0023's portable rules. It is required exactly when the stream is keyed and refused when it is not. Empty text is refused, as in #407.
- **Composite stream ids.** The specification writes a stream id in the same shape the route maps it, with literals where the route has command properties. Today a stream id is one scalar, so it is one value. A composite id written as one pre-joined string such as `"p-1:2026-10"` is not accepted as a stand-in for parts: the joining format belongs to the portable formatter, and a specification that hard-codes it would break the day the formatter is decided. When routes gain composite stream ids (see *Timeline and scope*), specifications gain the same parts on the same terms, for example:

  ```screenplay
    given HoursLogged
      for "p-1"
      stream Project.Periods
        streamId
          project = "p-1"
          period = "2026-10"
  ```

  This record fixes the rule (mirror the route, literal parts, never a pre-joined string), not the composite route spelling itself.

### Which blocks take it

| Block | `stream` line | Meaning |
| --- | --- | --- |
| `given <Event>` | Allowed | The fact is placed on that source and stream. `for` is required when `stream` is stated. |
| `when append <Event>` | Allowed | The appended fact carries that route. `for` is required when `stream` is stated. |
| `then <Event>` | Allowed | The new fact must carry exactly that route. `for` stays optional. |
| `when <Command>` | Refused | The route comes from the command declaration. Assert it with a `then` event. |
| `given readmodel`, `then readmodel`, `then no readmodel`, `then query` | Not applicable | Read models have keys, not routes. |

There is no `then no <Event>` form today, so nothing applies there.

Source only (an event source type with no stream) is not offered. A command route always names `Source.Stream`, and #407's route always has a stream; the specification vocabulary stays no larger than the route vocabulary. A source with no declared stream is therefore not stateable in a specification until routes allow it.

### Values: `for` without a producer

- **No `stream` line: unchanged.** The v2 rule still applies, so every existing specification keeps its meaning, diagnostics and ESM bytes.
- **With a `stream` line:** `for` is a concrete literal of the source's declared `identifier` type. A producer is not needed, so history for a source nobody in the model produces can be stated.
  - If the event also has a producer whose destination type differs from the source's identifier type, the specification is refused. #407 refuses the same mismatch on a routed command, because the event would be filed under a source with a foreign identity.
  - If the source declares no `identifier`, the v2 producer rule supplies the type; with no producer either, the specification is refused with a message asking for an `identifier` on the source.
- `when <Command> for <value>` keeps its current meaning.

### How the runner uses it

- **`given`** records the fact with its route. **`when append`** appends with its route. In #407's admitted scope routes never change projections, reducers, reactions, constraints or queries, so in that version a route on `given` changes no outcome of the reference runner. It still matters: a renderer seeds the history in the stated stream, so a rendered test against Chronicle and Arc runs against the same history the specification describes.
- **`then`** compares the route as part of the fact, like the event type, payload and `for`. A stated route must match the fact's route exactly (source, stream and formatted stream id). An unrouted fact never matches a stated route. `then events in any order` includes the route in matching and keeps the exact count of new facts.
- **No `stream` on `then` means "not compared".** This mirrors `for`, which is compared only when written, and keeps every existing specification passing when its command later gains a route. There is no spelling for "this fact must be unrouted"; that can be added later without changing anything here.
- **Constraints and concurrency.** Screenplay's constraints are not scoped to streams yet (constraint scopes are deferred by 0023), and the reference runner does not execute concurrency. A `given` route therefore does not change which constraint values are claimed or which append conflicts. When constraint scopes, `from` filters on reactions and reducers, or concurrency execution are admitted, they read the routes this record places on `given` facts; the spelling does not change.

### Representation

- **Syntax and AST.** Specification event steps gain an optional route: source and stream reference with location, and an optional stream id value. Syntax JSON is additive. The printer prints `stream` after `for` and before payload lines; printing and reparsing gives the same syntax. Typed AST edits and MCP authoring add, replace and remove the route like other specification children; rename of a source or stream repairs these references like command routes.
- **ESM.** Each `given`, `when append` and `then` event fixture gains an optional `route` with the same source, stream and stream-id encoding as #407's command route, except that the stream id is a literal. A model selects the admitting version only when it uses the feature; every other model keeps its version, bytes and revision.
- **Both compilers.** The C# and TypeScript compilers, Monaco and VS Code accept, validate and print the same forms.

### Versioning

The syntax lands as soon as this record is accepted and is refused with `PLAY0268` ("not admitted by any supported executable model version yet", naming the feature, not a number). Admission joins #407's feature: one release-ready claim admits command routes and specification routes together, and takes the next unused number under 0025. If #407 is claimed before this work is ready, specification routes take the next number at their own claim instead of delaying #407.

## Options considered

### Spelling

- **A. Header clauses, as in the issue: `given AuthorRegistered for "other" in Author.Profile`.** Not recommended. `for` is a child line today, so the source id would get a second spelling. The stream id has no natural place on the line (`in Account.Transactions 202610` is positional), and a composite id cannot fit without a call-like `key(a, b)`, which #81 argues against. #302 rejected the same shape for productions as too implicit.
- **B. Child lines that reuse the command route (recommended).** `stream Source.Stream` with nested `streamId = <value>` under the event, next to `for`. One spelling for "where events land" in commands and specifications, indentation instead of parentheses, a qualified operand instead of new keywords, and composite ids follow the route automatically. Cost: three or four lines per routed event instead of one.
- **C. A one-line child with a new keyword: `in Account.Transactions 202610` or `on Account.Transactions`.** Not recommended. It adds a keyword the command side does not use, the stream id is positional again, and composite ids have nowhere to go.
- **D. A grouping block: `route` with `for`, `stream` and `streamId` beneath it.** Not recommended. It moves the existing `for` line (a breaking change to every v2 specification, or two places to write it), adds a keyword with no meaning of its own and one more level of indentation.
- **E. A default for the whole specification** (for example `given stream Account.Transactions` applying to the events after it). Not recommended. It is order-dependent and implicit, and one scenario often spans several streams.
- **F. Infer the route from the producing command, no syntax.** Not recommended as the answer. It cannot work for events with no producer, several producers with different routes, captured or external events, or `when append`, and it cannot supply a stream id value. It is what generators are forced into today.

### Composite stream ids

- **Mirror the route's parts with literals (recommended).** Specifications never encode a joining format, and the rule follows whatever route spelling is chosen.
- **A single pre-joined text value.** Not recommended. It works today without new syntax, but every specification would hard-code a separator and order that the portable formatter has not decided and a provider may not choose (0023).
- **A one-line JSON object, `streamId = {"project":"p-1","period":"2026-10"}`.** A reasonable variant: specifications already accept single-line JSON objects for composite `type` values. Choose it if composite routes are spelled as a composite `type`; otherwise the indented parts read closer to the route.

### `then` without a route

- **Not compared (recommended).** Matches `for`; existing specifications survive a command gaining a route.
- **Must be unrouted.** Rejected as the default: adding a route to a command would break every specification of it, and the failure would be about wiring, not behavior.

### Version

- **Join #407's version (recommended).** Consumers (Stage, the Arc generator) bump once and can round-trip specifications for exactly the models that use routes. Cost: #407's claim carries more evidence.
- **Always a later version.** Keeps #407 small, but every consumer adopts routes twice, and for one release routed models can only be specified with their routes left out.

## Default if unanswered

Specifications cannot state an event's source type, stream or stream id. #407 still ships routes on command facts and pins them in the canonical corpus, but authors cannot assert them. Arc's generator keeps dropping or flattening scenarios that use non-default streams, Stage renders specification tests on Chronicle's default stream, and seeded history on another source keeps depending on the producer rule or the command's own source. Nothing has to be migrated later, because options B, C and D are all additive. The cost is that every model using routes has specifications that describe less than its code, and generated round trips lose information.

## Timeline and scope

The decision holds until per-production route overrides, reaction and reducer `from` filters, constraint scopes or composite stream ids are designed. Those may extend it (for example a per-production override would be asserted the same way, on the `then` event); none should need to change its spelling. Syntax can start after acceptance; execution waits for #407's implementation.

In scope: the `stream` and `streamId` child lines on `given`, `when append` and `then` events; their checks; the refusal under `when <Command>`; the `for` typing rule for routed events; `then` comparison and `given` placement in the reference runner; the ESM fixture route; the version rule; both compilers, printer, typed AST, MCP, editors and documentation.

Out of scope: how a route composes a composite stream id and how a stream declares one (a separate decision that this record's mirror rule follows; it may sit with [#459](https://github.com/Cratis/Screenplay/issues/459), composite read-model keys); routes naming a source without a stream; asserting that a fact is unrouted; per-production overrides; `from` filters; constraint scopes; concurrency execution; Chronicle's stream completion; any change to `when <Command> for` or to specifications without a `stream` line.

## Verification

**Done when** an event in `given`, `when append` or `then` can state `stream Source.Stream` with `streamId` for a keyed stream, both compilers parse, validate, print and round-trip it identically, the binder refuses it with `PLAY0268` until the admitting version, and in that version the reference runner places routed `given` and `when append` facts and fails a `then` whose stated route differs from the fact's route, while every specification without a `stream` line keeps its bytes, revision and outcome.

**Verify by**:

- Parser, printer and validator specifications in C# and TypeScript for: a keyed and an unkeyed stream; missing and superfluous `streamId`; empty text and wrong-type stream ids; a payload property named `stream`; `stream` under `when <Command>` (refused); `stream` without `for` on `given` and `when append` (refused) and on `then` (accepted).
- `for` typing: an event with no producer seeded on a source with an `identifier` binds; the same event with a producer of a different destination type is refused; a specification without `stream` keeps every existing v2 diagnostic.
- Canonical vectors in the admitting version: routed `given`, `when append` and `then` fixtures with UUID-, text- and integer-keyed streams; a `then` that fails on a different stream id, a different stream and an unrouted fact; a `then` without a route that passes against a routed fact; `then events in any order` with routes. All v1–v7 goldens and corpora byte-identical.
- The `DepositFunds` sample above executes in the reference runner, and Arc's generator emits the `stream` lines for a Chronicle `EventLog.Append<TSource>(id, event, stream, streamId)` scenario.

## Consequences

- **Easier:** writing a specification for any routed command and reading where its history lives, generating and rendering specifications without losing the route, and seeding history for sources no command in the model produces.
- **Harder:** routed specifications are longer, by two to three lines per event. Tools that edit specification events (printer, AST, MCP, rename, folder layout) carry one more child. The `for` rule now has two branches, by whether `stream` is stated.
- **Forecloses:** a one-line header spelling for routes in specifications, and writing composite stream ids as pre-joined text. Once models use the lines, changing the spelling costs a migration, which is why this is costly to reverse.

## Questions for the decider

1. Is option B, reusing the command's `stream` / `streamId` lines as children of the event block, the spelling you want, rather than the one-line `given AuthorRegistered for "other" in Author.Profile` in the issue?
2. Should a `then` event without a `stream` line leave the route uncompared (recommended), or require the fact to be unrouted?
3. Should `for` be required when `given` or `when append` states a `stream` (recommended), or default to the command's identifier as an unrouted `given` does today?
4. When an event states a `stream`, is typing `for` by the source's `identifier`, and refusing a producer whose destination type differs, the right rule?
5. For composite stream ids, do you accept "mirror the route's parts with literal values, never a pre-joined string", and should the composite route spelling itself get its own issue and record?
6. Should specification routes join #407's ESM version (recommended), or always follow it in a later version?
