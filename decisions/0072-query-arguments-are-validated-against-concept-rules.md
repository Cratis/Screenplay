---
id: 0072
title: Query arguments are validated against their concept rules, and a violation is a rejection
status: accepted
stage: none
decided: 2026-10-13
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Parsing/**
  - Source/Screenplay/Compiler/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Documentation/screenplay/**
---

## Context

[#682](https://github.com/Cratis/Screenplay/issues/682) asks what happens when a query argument breaks the rules of its concept, for example `concept InvoiceId : String` with `validate min 2` and a lookup by `"A"`.

- **The reference evaluator checks shape only.** `ValidateQueryKey` and `ValidateQueryFilters` call `SemanticValueValidator`, which checks type, primitive and enumeration membership but never runs concept rules (`Semantics/Execution/SemanticEvaluator.Validation.cs:142-173`, `Semantics/SemanticValueValidator.cs:61`). An out-of-rule key is an ordinary lookup that finds nothing. A query can only be refused as `Contract` (a bad shape), `Unauthorized` or unsupported (`SemanticQueryRun.cs:37-117`); `Validation` is used only for commands (`SemanticCommandExecution.cs:138`).
- **Commands run concept rules** on their values (`ValidateConceptValues`, `SemanticEvaluator.Validation.cs:122-127`), and a violation is a `Validation` rejection.
- **Arc validates query arguments the same way as command properties.** `Arc.Core/Queries/Filters/FluentValidationFilter.cs:15-65` validates arguments "with the same traversal a command's properties go through, so a validator — a `ConceptValidator<T>` in particular — behaves identically". A null or missing argument is skipped. A failure is `QueryResult.ValidationResults` and HTTP 400 (`Arc/Queries/QueryHttpExtensions.cs:148-150`), checked after authorization (403).
- **Specifications cannot assert a query rejection** other than `then denied`. `when query` and `then query` bind as query outcomes, and `then error` without a command is refused (`SemanticModelBinder.Specifications.cs:59-97`, `SemanticModelValidator.Specifications.cs:40-47`).
- Stage works around the gap by rendering a validated query argument as its primitive, so the rendered application keeps the evaluator's "no instance" answer instead of Arc's 400 (Cratis/Stage#350).

Under [0001](0001-chronicle-runtime-semantic-authority.md) the runtime defines the meaning, and for queries the runtime is Arc.

## Decision

1. **A query argument is validated against its concept's rules**, both the `by` key and every supplied filter value ([0069](0069-admit-equality-query-filters-as-esm-v14.md)). An omitted optional filter is not validated, as in Arc.
2. **A violation rejects the query** with the `Validation` category, after authorization and before any instance is selected. Messages follow the concept rule's message, as for commands.
3. **The evaluator applies this in every ESM version.** The canonical bytes do not change; the reference outcome changes from "no instance" to a rejection, which is what the runtime already answers. This follows the precedent of the `$` fix in `matches` (0054 item 11) and of [0071](0071-whitespace-only-text-does-not-satisfy-not-empty.md).
4. **Specifications can assert the rejection.** `then error`, with or without a message, is allowed after `when query`, and after `then query … arguments` when the specification has no command, with no `result` and no `then no result`. It asserts exactly that the query was rejected with `Validation`. In the executable model this is a new specification shape: it is admitted in the next ESM version allocated under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) and [0066](0066-published-esm-versions-are-frozen.md), and only a model that uses it selects that version.
5. **Renderers apply the concept's validator to query arguments.** Stage renders concept-typed arguments so Arc's validator runs, and drops the primitive workaround.

## Options considered

- **A violation is a rejection (chosen).** It matches Arc, so the reference evaluator and a rendered application give the same answer. It treats an argument like the command property it often came from, and it lets a specification state the boundary.
- **A violation is a key that matches nothing.** It keeps today's evaluator outcome, but every Arc application answers 400 instead. Every renderer would have to bypass Arc's validator, as Stage does today, and a client could not tell a malformed key from a missing instance.
- **Validate only the `by` key, not filters.** Not taken: Arc validates every argument, and a filter is a caller-supplied value like the key.
- **Apply only from the next ESM version.** Not taken for the evaluator: the bytes do not change and the runtime already rejects in every version. Only the new specification form needs a version.

## Default if unanswered

The evaluator keeps answering "no instance" while Arc answers 400, Stage keeps rendering primitives to hide the difference, and specifications cannot state that a malformed key is refused.

## Timeline and scope

Proposed on 2026-10-12, for acceptance by the decider. Implementation follows acceptance, after [0071](0071-whitespace-only-text-does-not-satisfy-not-empty.md) because both define what a rule rejects.

In scope: evaluator validation of query keys and filter values, the `Validation` rejection for queries, the `then error` specification form for queries, its ESM admission, binder and both compilers' diagnostics, documentation and vectors.

Out of scope: validating read-model state, rules declared on query parameters themselves (there are none), and `from` context arguments (not admitted).

## Verification

**Done when:** a lookup by an out-of-rule key or filter value is rejected with `Validation` in every ESM version, an omitted optional filter is not validated, a specification asserts the rejection with `then error` after `when query`, and the docs describe both.

**Verify by:** evaluator specifications for key and filter violations, authorization ordering and omitted filters; binder and validator specifications for the new specification form; a golden vector in the allocated version; and the documentation.

## Consequences

The reference evaluator and Arc agree on malformed query arguments, and renderers no longer need to hide concept types on query arguments. Specifications that asserted "no result" for an out-of-rule key now fail and must assert `then error`. Consumers admit the next ESM version for the new specification form.

Consumer follow-ups: Stage renders concept-typed query arguments again, reversing the primitive workaround of [Stage#350](https://github.com/Cratis/Stage/issues/350), and its conformance harness maps the Validation rejection ([Stage#370](https://github.com/Cratis/Stage/issues/370)); Studio supports `then error` after `when query` and admits the version ([StudioIssues#628](https://github.com/Cratis/StudioIssues/issues/628)); Arc.TypeScript surfaces query validation results ([Arc#3225](https://github.com/Cratis/Arc/issues/3225)).

## Status notes

**2026-10-13 — accepted.** Accepted by Sindre Alstad Wilting (delegated through the coordinator after an independent review), with the consumer follow-ups named under Consequences.

## Related

Screenplay [#682](https://github.com/Cratis/Screenplay/issues/682). Stage [#350](https://github.com/Cratis/Stage/issues/350). Builds on 0001, 0025, 0066, 0069 and 0071.
