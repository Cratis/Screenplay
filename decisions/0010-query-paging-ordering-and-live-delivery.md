---
id: 0010
title: "Queries: page-number paging, one sort field, change-set live delivery, unordered by default"
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/QuerySyntax.cs
  - Source/DotNET/Screenplay/Parsing/QueryParser.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.ReadModels.cs
  - Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay/Semantics/Serialization/**
  - Documentation/screenplay/queries.md
  - Documentation/screenplay/specifications.md
---

## Context

[#140](https://github.com/Cratis/Screenplay/issues/140) asks for queries as executable portable contracts: selection, ordering, paging, live delivery and query specifications. The triage left three choices open: ordering syntax and default determinism, the paging model, and what `live` promises.

Today the executable semantic model (ESM) admits one query shape: one caller-supplied `by` argument returning one optional read model. Observable queries, filters, scopes and performers fail binding, and so do collection results ([`SemanticModelBinder.ReadModels.cs:63-77`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.ReadModels.cs)). `SemanticQueryDelivery.Live` exists but is unreachable ([`SemanticBehaviors.cs:152`](../Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs)). Specifications compare repeated `result` entries in authored order ([`specifications.md`, "Query results"](../Documentation/screenplay/specifications.md#query-results)), while nothing in a query can declare an order. For events, `then events in any order` already compares without order.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md) the runtime defines the meaning. For queries the runtime is Arc:

- Paging is a page number and a page size ([`Paging.cs`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Queries/Paging.cs)). Arc has no cursor paging.
- Sorting is one field and one direction ([`Sorting.cs`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Queries/Sorting.cs)).
- An observable collection query delivers change sets of added, replaced and removed items between snapshots ([`ChangeSetComputor.cs:10-27`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Queries/ChangeSetComputor.cs)).

## Decision

Queries mirror Arc.

1. **Paging.** A query that pages takes a page number and a page size.
2. **Sorting.** The caller may select one sort field and a direction.
3. **Live.** A live query delivers the initial current answer, then change sets (added, replaced, removed) as the answer changes.
4. **Result comparison.** A specification compares a collection result without regard to order, unless the query declares `order by`. This is the query counterpart of `then events in any order`.
5. **First increment.** Collection queries with equality filters are admitted first. Ordering, paging and live delivery follow in later increments, under this record.

## Options considered

- **Mirror Arc (taken).** Every construct has a runtime counterpart that Stage can render onto.
- **Cursor paging.** Deterministic with a stable sort key, but not taken: Arc has no cursor paging, so the model would promise something the target cannot realize.
- **Ordering on the projection, or left to realization profiles.** Not taken: the order is part of what the caller observes, so it belongs on the query contract.
- **Compare results in authored order by default.** Not taken for collection results: it asserts an order no declaration guarantees, and a target that returns the same set in another order would fail.
- **Whole-result replacement or per-instance notifications for `live`.** Not taken: Arc delivers change sets, and a different promise would diverge from the runtime.
- **Never admit `live`.** Not taken: #140's criterion for live queries could then never be met.

## Default if unanswered

Every query beyond single-by-key stays outside the ESM, specifications keep asserting an undeclared order, and `live` stays unreachable. Stage's portable query rendering stays blocked on a contract it can render without inventing broader queries (Cratis/Stage#58).

## Timeline and scope

Settle before any #140 work, and keep it until superseded. Each increment is admitted under [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md), starting with collection queries with equality filters.

In scope: equality-filter selection and its lowering from existing `by` and same-name filters; collection results and unordered comparison; `order by`; caller sorting by one field; page-number paging; live change-set delivery; query specifications for absence, single, ordered collection, page and live update.

Out of scope: cursor paging; multi-field caller sorting; non-equality predicates; tie-breaking rules and how a specification asserts a page of an unordered query, which are settled with the paging increment; query performers, which wait on this record and [decision 0012](0012-typed-context-descriptor-and-command-handler-role.md); SQL, LINQ or transport syntax.

## Verification

**Done when (first increment):** A query returning a collection with equality filters binds to the ESM, and the reference evaluator returns every matching instance. A specification that lists the matching results in any order passes; one that lists a wrong or missing result fails. `queries.md` and `specifications.md` state the unordered comparison rule.

**Done when (whole record):** `order by`, caller sorting, paging and live delivery are each admitted with golden vectors and reference execution, and a specification with `order by` fails when results arrive in another order.

**Verify by:** Binder and evaluator specs per shape, specification-runner specs for ordered and unordered comparison, and golden vectors for each admitted form.

## Consequences

Query contracts match what Arc runs, so renderers realize them without invention. Specifications written against collection queries become order-independent unless the author declares an order, which removes a class of false failures and makes order an explicit decision. Cursor paging is closed off unless Arc gains it.

## Related issues

Screenplay: [#140](https://github.com/Cratis/Screenplay/issues/140), [#128](https://github.com/Cratis/Screenplay/issues/128). Stage: [#58](https://github.com/Cratis/Stage/issues/58).
