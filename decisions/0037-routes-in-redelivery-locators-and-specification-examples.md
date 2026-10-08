---
id: 0037
title: State routes in redelivery locators and in typed specification examples with the existing route lines
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/**
---

## Context

Decision [0031](0031-event-source-and-stream-in-specifications.md) lets a specification event state its route with `stream Source.Stream`, a `streamId` (or [0033](0033-composite-event-stream-ids.md)'s part block), or `no stream`. Two places still cannot.

**Redelivery locators ([#490](https://github.com/Cratis/Screenplay/issues/490)).**
- [0030](0030-reaction-refusals-and-redelivery.md) gives `when redelivered <Event> to <Reaction>`, which locates exactly one `given` fact by `for` and stated values. `SpecificationRedeliveryValidator.cs:15-63` checks that every given of the event matches, and reports PLAY0543 unless exactly one does.
- Routes are never compared. Two givens that differ only in their stream make the locator ambiguous, and nothing can name the right one.
- A `stream` line under `when redelivered` reports PLAY0548 today, "A command occurrence cannot declare a route", which is the wrong message (`SpecificationParser.cs:759-765`; TypeScript `SpecificationParser.ts:239`).
- 0030 makes this a prerequisite for admitting #433 (0030:32).

**Typed examples ([#491](https://github.com/Cratis/Screenplay/issues/491)).**
- [0032](0032-expand-typed-specification-examples-in-the-front-end.md) lets a step reuse an `example` for its values and `for`.
- Route lines in an example are refused with PLAY0526 (`SpecificationParser.Examples.cs:61-66`), a deferral recorded at 0032:32.
- The refusal tests the first word, so a payload property named `streamId` is refused too, even though it is payload at step level (`specifications.md:179`).
- Expansion merges values and `for` only (`SpecificationExamples.cs:279-288`).
- MCP `find-fixtures` reports every route row as `authored` and says "Examples never contribute routes" (`McpFixtureQueries.cs:17,95-107`).

**Execution.** The admission of routes, decision [0036](0036-admit-event-sources-streams-and-command-routes.md), lists both as non-goals unless their own decision is accepted before its claim.

## Decision

Both places take **the same route lines a `then` event takes**. Nothing new is invented:
- no new ESM members;
- no new diagnostic codes;
- no new version.

### Redelivery locators

`when redelivered <Event> to <Reaction>` accepts these optional child lines, alongside `for` and values:
- `stream Source.Stream`, with a `streamId` or a part block;
- or `no stream`.

```play
specification Recovering_september
  given Approved
    for "inv-1"
    stream Invoice.Ledger
      streamId = 202609
  given Approved
    for "inv-1"
    stream Invoice.Ledger
      streamId = 202610
  when redelivered Approved to Claimer
    for "inv-1"
    stream Invoice.Ledger
      streamId = 202609
  then no events
```

**How the locator matches.** A locator is a pattern, like `then`.
- **Without a route line**, routes are not compared: the route is a wildcard, as today.
- **With `stream`**, only givens on that resolved source and stream match, and only when their canonical formatted id is equal.
- **With `no stream`**, only unrouted givens match. That is why `no stream` is allowed here, although 0031 refuses it on `given`: selecting a fact is not placing one.
- **`for`** stays optional and independent of the route.

**Formatting the id.**
- Comparison uses 0036's shared stream id formatter, in declaration-part order for composites.
- That covers UUID case, the integer bounds, NFC refusal and escaping.
- Existing equality of payload and `for` values is unchanged.

**Three-valued matching.**
- Each candidate is matched against the **effective**, example-expanded givens, keeping duplicate occurrences. The match is the conjunction of `for`, values and route, each true, false or unknown.
- A definite `false` on any of them rules the candidate out, even when another comparison is unknown.
- The locator succeeds only when exactly one candidate is definitely `true` and none is unknown. Otherwise PLAY0543 applies.

**Diagnostics**, all existing codes:

| Code | Covers |
| --- | --- |
| PLAY0547 | Malformed, repeated or conflicting route lines |
| PLAY0549 | A route that does not resolve, or a keyed/unkeyed or part mismatch. The locator takes 0031's route checks, except the "routed needs `for`" rule and the contradiction check. |
| PLAY0550 | A locator `for` incompatible with its route's source identifier, as for any routed occurrence |
| PLAY0543 | Zero or several matches, or a route comparison that cannot be decided. The hint becomes "use 'for', values, 'stream' or 'no stream'". |
| PLAY0548 | No longer fires for a locator |

**Binding and admission.**
- **Two separate refusals.** A routed locator is refused twice: PLAY0268 for redelivery (#433) on the node, and PLAY0268 for specification routes on its route lines. The walker visits the locator's route lines for that.
- **Redelivery stays refused independently.** It remains refused until 0030's own requirements are met: redelivery, refusal handling and `then no events`, admitted together. That holds even after 0036 admits specification routes.
- **No new ESM bytes.** At admission a locator still resolves to a given index (0030:73), so a routed locator adds none.
- **Admission evidence.** Equivalent locators, with and without a route that selects the same fact, must bind to the same index, and must redeliver that existing routed fact without appending it again.

### Routes in examples

An **event** example may state `stream …` (scalar or part block) or `no stream`:

```play
example September_deposit : FundsDeposited
  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  stream Account.Transactions
    streamId = 202609
  amount = 100

specification Moving_a_deposit
  given September_deposit
  then September_deposit amount = 40
    stream Account.Transactions
      streamId = 202610
```

**Rules**
- **One slot, replaced whole.** A step that writes `stream …` or `no stream` replaces the example's route **as a whole**: never only the `streamId`, never one part. This is 0032's rule for structured values.
  - A `then` may replace an inherited route with `no stream`.
  - A `given` may replace an inherited `no stream` with a valid route.
- **No way back to the wildcard.** Once an example states a route, a step cannot restore "route not compared", just as nothing removes an inherited value. Use another example for that.
- **Role checks use the effective route.** A `given` or `when append` whose effective route is `no stream` reports PLAY0547 at the step, naming the example, because 0031 allows `no stream` only on `then`.
- **`for` is separate, and typed by route.** A step's route never clears the example's `for`.
  - An event example's own `for` is validated against its own route's source identifier, using 0031's fallback, even when the example is unused.
  - The effective `for` is validated again against each step's effective route, with PLAY0550.
  - An override never hides an invalid declaration. This replaces deriving an example's destination type from event producers (`SemanticModelBinder.Examples.cs:34-68`), which refuses routed history with no producer or a differently typed one.
- **Command and read-model examples.** Route lines in those keep PLAY0526. The message narrows to "only event examples carry routes; a command's route comes from its declaration, and read models have none". The code is kept, and it still means an invalid example route, with fewer cases rejected.
- **Payload named `streamId`.** In an example body, a top-level `streamId = x` is a payload line, as at step level.

**Where diagnostics land**
- **Declaration checks run once, on the authored example**, with its own locations, even when it is unused:
  - route resolution;
  - literal checks;
  - the example's `for` against its route.
- **Contextual checks run on each effective step**, using the expansion's provenance, reported at the authored step and naming the example:
  - the "routed `given` needs `for`" rule;
  - `no stream` on `given` or `when append`;
  - the effective `for` against the effective route (PLAY0550);
  - a contradiction with the command under test (PLAY0551, `then` only).
- **No duplicates.** An inherited route's location still points at the example, so a declaration error is reported once however many steps use the example. A route that every step overrides is still checked at the example.
- **Both compilers.** The validators in both get an origin-aware pass over effective steps, instead of validating only the expanded syntax.

**Origin reporting**
- **.NET.** `EffectiveSpecificationStep` gains an init-only `Route` of type `EffectiveSpecificationRoute(Value, Origin, OverriddenValue)`. Its value is the stream or no-stream node, and `Origin` reuses `authored | example | override`. The public constructor is unchanged. A missing route has no entry, because a missing route means "not compared" or "unrouted", never a default value.
- **MCP `find-fixtures`.**
  - Its `…Stream`, `…StreamId`, part and `…NoStream` rows take the route's origin and example name.
  - `overriddenValue` holds the replaced route, as text in the authored spelling. That's `Source.Stream` plus the id, or the parts in declaration order, or `no stream`, at most one row per replaced route.
  - Redelivery locators get the role `whenRedeliveredEvent`, lower camel case like the existing `givenEvent`, `whenAppendedEvent` and `thenEvent`, and the role filter accepts it.
  - The coverage text no longer says examples never contribute routes.
  - The runner's failure provenance is source-bound: an ESM-only execution cannot recover origins, and reports none.
- **Readiness.** An example route that no step uses still reports PLAY0268, because the walker visits example routes. Unused examples are not a way around admission (0032).

**Syntax JSON.** The new nullable members on redelivery and example nodes are omitted when null, so every existing syntax vector stays byte-identical. The same rule holds in both compilers.

### Execution and versioning

- **Before admission.** These forms are syntax and front-end behavior only.
- **Example routes.** These expand in the front end, so they become executable wherever specification routes are admitted (0036). They need no admission of their own.
- **Locator routes.** These are admitted with #433's redelivery, under 0030's rules, never earlier.
- **Reference runner.** At admission, it reports route origins in failure provenance.

### Delivery

- **One pull request**, after #462's authoring surface merges (it brings the part blocks), covering every surface per the language-change rule:
  - both compilers and the printer;
  - syntax JSON and the effective-syntax API;
  - MCP, Monaco, the VS Code grammar and the board's redelivery text;
  - docs and conformance vectors.
- **Issues.** Corpus and downstream issues are filed for Cratis/AI, Stage (effective API), Studio's importer and Arc#2887 (preservation).

## Options considered

### Redelivery

- **Route child lines (chosen).** One notation everywhere, per 0031.
- **A header clause** (`… to Claimer in Invoice.Ledger 202609`). Rejected for the reasons 0031 rejected one-line routes: positional and harder to read.
- **Positional or labelled givens** (`given #2`). Rejected: fragile, and they leave 0030's value-based locator behind.

### Examples

- **Allowing route lines with whole-route replacement (chosen).**
- **Keep refusing them.** Rejected: it forces authors to repeat routes on every step, which is what examples exist to remove.
- **Named route declarations.** Rejected: a new construct for little gain.
- **Field-level merge**, where a step's `streamId` overrides only the id. Rejected: at step level `streamId =` is payload, and merging parts would mix schemas.
- **Refuse `no stream` in examples, to keep examples role-neutral.** Rejected: a `then`-only example is a real need. A role check at the step is enough.

## Default if unanswered

Two givens on different streams can never be told apart by a redelivery locator, so #433 stays unadmittable. Examples keep refusing routes, so every routed step repeats its route.

## Timeline and scope

**In scope:**
- route lines under `when redelivered` and in event examples;
- matching, expansion, provenance and diagnostics;
- the PLAY0526 narrowing and the PLAY0548 fix;
- every surface listed under Delivery.

**Out of scope:**
- removing an inherited route;
- partial route overrides;
- route lines on command steps;
- implicit fixture defaults (#492).

## Verification

**Done when** both compilers parse, validate, expand, print and round-trip the new forms identically on shared conformance vectors, and binding refuses them with PLAY0268. The vectors cover:
- **Redelivery**:
  - two givens differing only by stream (ambiguous without a route, located with one);
  - `no stream` selecting the unrouted given;
  - a composite locator, and one with a UUID written in another case;
  - an undecidable route, and a candidate ruled out by payload while its route is unknown (it must not block the match);
  - a locator compared through the shared formatter: reordered part mappings, escaped text, integers next to the bounds, non-NFC refusal;
  - a malformed route line, and the absence of PLAY0548.
- **Examples**:
  - an example route inherited, and one replaced by a step (origin `override`);
  - `no stream` in an example used on `then` (accepted) and on `given` (PLAY0547 at the step);
  - an unused routed example (PLAY0268);
  - route lines in a command example (PLAY0526, new message);
  - a payload property named `streamId` in an example;
  - a routed given from an example without `for`;
  - an example's `for` checked against its route when the example is unused, when its event has no producer, and when a producer has a different type;
  - a step whose route moves the example to another source, so its `for` is checked again;
  - an example used by several steps, reporting its declaration error once;
  - an invalid example route that every step overrides, still reported at the example;
  - a `given` replacing an inherited `no stream`, and a `then` replacing an inherited route with `no stream`.
- **MCP**: `find-fixtures` origin rows, and the readiness case for examples.
- **Syntax vectors**: every existing vector byte-identical.

## Consequences

- **Easier:**
  - Redelivery specifications can target one stream's fact, which unblocks #433.
  - Routed examples remove repeated route lines.
  - Tools can see where a route came from.
- **Harder:**
  - Example expansion and the redelivery validator carry routes.
  - Diagnostics must be attributed to steps.
- **Forecloses:**
  - partial route overrides;
  - a separate "any route" spelling inside examples.

## Verdict

The decider delegated this verdict to the orchestrating agent. The choices above were made under that delegation on 2026-10-08, after an independent cross-provider critique by GPT-6 Astra. That critique answered "accept with changes", and all its findings are folded in:
- redelivery admission stays separate;
- `for` is typed by route for examples;
- locator matching is three-valued;
- role checks use the effective route;
- diagnostics are attributed by origin, with no duplicates;
- the PLAY0550 and PLAY0551 roles are covered;
- the shared formatter is used;
- the MCP wire contract is pinned.

1. **Redelivery?** Route child lines under `when redelivered`, matched three-valued against effective givens, with `no stream` selecting the unrouted fact. They're admitted only with #433.
2. **Examples?** Event examples may carry a route, which steps replace as a whole. Role and `for` checks use the effective route. They're executable wherever specification routes are admitted.
3. **Codes?** No new codes. PLAY0526 narrows, and PLAY0548 no longer fires for locators.
