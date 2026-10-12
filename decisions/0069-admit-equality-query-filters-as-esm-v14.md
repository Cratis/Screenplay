---
id: 0069
title: Admit caller-supplied equality query filters into the executable model as ESM v14
status: accepted
stage: implemented
decided: 2026-10-12
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[0010](0010-query-paging-ordering-and-live-delivery.md) makes collection queries with equality filters the first increment of [#140](https://github.com/Cratis/Screenplay/issues/140). Collection queries are already admitted: unkeyed lists and lists narrowed by one caller-supplied `by` key, including their `observable` form. A query with any `filter` still fails binding with `PLAY0268` (`SemanticModelBinder.ReadModels.cs:114-116`), so 0010's first increment is not done.

Facts that shape the proposal:

- A filter is declared `filter <name> <Type> [from <source>]` (`grammar.md`, `FilterClause`). The type may be optional (`T optional`, or the legacy `T?`), which `queries.md` documents as "the caller may omit the filter". `from` fills the value from the query context instead of the caller.
- `by` on a collection query already means equality: the reference evaluator keeps every instance whose key property equals the supplied value (`SemanticQueryRun.cs:56-59`).
- The specification runner already matches result rows by read model and key, not by position (`SemanticSpecificationRunner.cs:410-425`). The documentation says rows compare in authored order, which is wrong against the code and against 0010 item 4.
- A query call is identified today by its query and its `by` key (`SemanticExecutionContracts.cs:189-200`), and the validator refuses two expectations with the same query and key (`SemanticModelValidator.SpecificationStates.cs:179-193`). Every unkeyed call has a null key, so two calls that differ only by filter values would collide.
- ESM v10 to v13 are published and frozen ([0066](0066-published-esm-versions-are-frozen.md)). [0004](0004-admission-and-governance-of-portable-executable-semantics.md) would let a previously refused construct join the highest version when no binding model changes bytes, but this record takes a new number instead (see Options). The next free version is v14, checked against `origin/main` at v4.141.0 and rechecked at admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md).

## Decision

Admit caller-supplied equality filters on collection queries as ESM v14.

### Meaning

- A filter narrows the result to the read-model instances whose property **with the same name** equals the supplied value. Equality is the evaluator's value equality, the same as for `by`.
- An instance without that property does not match a supplied value.
- `by` and every supplied filter combine with AND.
- A filter with an optional type may be omitted by the caller. An omitted filter does not narrow. A required filter must be supplied.
- A supplied `null` is refused in v14. The model could tell an omitted filter from a supplied null, but a rendered query parameter usually cannot: a nullable parameter commonly means "do not filter". Refusing null now keeps "match instances whose value is null" free for an explicit spelling later.

### Admitted shape

A filter is admitted when all of these hold; otherwise binding reports `PLAY0268`, naming the query, the filter and the reason:

- the query returns a read-model collection;
- the filter has no `from` source;
- the read model has a property with the filter's name, that property is not a collection and is a primitive, concept or enum;
- the filter's type equals the property's type, ignoring optionality;
- filter names are distinct and none equals the `by` name.

`scoped to`, `performer` and `from` keep `PLAY0268`, each with its own message instead of today's combined one. Filters on single-result queries stay refused; loosening later is cheap.

### Model shape

A query gains an optional `filters` array, written after `keyProperty` in declaration order and only when the query has filters:

```json
"filters": [
  { "id": "…", "name": "customerId", "type": { …, "optional": false }, "property": "…" },
  { "id": "…", "name": "status", "type": { …, "optional": true }, "property": "…" }
]
```

- The array follows the `argument` and `keyProperty` group (or `readModel` when the query is unkeyed) and precedes `cardinality`.
- The filter's own type reference carries optionality, as every other type reference does. There is no second flag: an optional type means the caller may omit the filter.

A query call is identified by its query, its key and its supplied filter values. The specification query result, the query request the evaluator answers and the query result it returns each gain an optional `filters` array after `key`, holding the supplied values only, in the query's filter order:

```json
"filters": [ { "filter": "…", "value": … } ]
```

The validator's duplicate check uses the whole call identity, so two calls of one unkeyed query with different filter values are distinct.

- Only a model with at least one query filter selects v14 (language, semantic and `schemaVersion` 14). A model without filters keeps its bytes and revision.
- v14 includes the v11 exact-number lowering, the v12 enum member numbers and the v13 concept compliance contracts.
- The model validator refuses filters below v14 and a v14 model without any filter.
- The strict reader refuses `filters` below v14, a value for an unknown filter, a repeated filter value, a missing required filter and a null value.
- The evaluator refuses the same for a query request built in memory, and a value whose type does not match the filter, before it selects anything.

### Reference evaluator and specifications

- The evaluator applies `by` and then every supplied filter. Live delivery still answers with a snapshot; change sets are a later increment under 0010.
- A specification's query arguments (`then query … arguments` and `when query`) name the `by` argument and filters by name. Both spellings report an unknown, repeated or missing required argument with `PLAY0468`.
- Policies on a query keep seeing only the `by` argument. Letting a policy read a filter is a later decision; an omitted filter must never become a value a policy can read.
- A specification compares a collection result without regard to order: each expected row is matched by read model and key. `specifications.md` and `queries.md` state this rule.

### Test vectors

- `full-esm-v14.json`: the v13 vector plus a collection query with one required and one optional filter, and a specification that supplies only the required one.
- `QueryFiltersCorpus.V14`: single-file and folder forms that pin bytes and revision.

### Consumers

These adapt after admission, each tracked in its own repository: Stage admits v14 explicitly and refuses filtered queries until it renders them as query parameters with equality selection ([Cratis/Stage#58](https://github.com/Cratis/Stage/issues/58)); the Arc Screenplay generator extracts them; Studio and the CLI pick up the new package; the AI corpus teaches the admitted shape. A consumer that does not support v14 refuses filtered models instead of dropping the filters.

## Options considered

- **Equality on the same-named property, caller-supplied, collections only (chosen).** It is 0010's first increment as written, it reuses the meaning `by` already has, and Arc can realize it without invention.
- **Name the target property explicitly (`filter status on currentStatus`).** Not taken: it adds syntax nobody has asked for, and the same-name rule is what `by` already uses.
- **Admit `from` context filters now.** Not taken: their values come from the typed context, which waits on [0012](0012-typed-context-descriptor-and-command-handler-role.md)'s descriptor for queries and on how a specification states a caller's context.
- **Treat a supplied null as "match null".** Not taken in v14: it would contradict Arc's null-parameter convention. It can be added later with an explicit spelling.
- **Join v13 as a byte-preserving extension.** Allowed by 0004 and 0066, because no model that binds today contains a filter. Not taken: a consumer that admitted v13 from an earlier package would then receive v13 models with a member it never saw, and the number would no longer identify one contract. A new number makes admission explicit for every consumer, at the cost of one more version.
- **A separate `optional` flag on the filter.** Not taken: the type reference already carries optionality, and two flags could disagree.

## Default if unanswered

Every query with a filter keeps failing with `PLAY0268`, 0010's first increment stays incomplete, and Stage cannot render filtered queries from the executable model.

## Timeline and scope

Proposed and accepted on 2026-10-12 by Sindre Alstad Wilting. It takes effect at admission against current `main`, with the version number rechecked at merge under 0025 and 0066.

In scope: the v14 model shape, binding, canonical writer and strict reader, validator refusals, reference evaluation, specification arguments and unordered comparison, vectors, MCP readiness text and documentation.

Out of scope: `order by`, caller sorting, paging and live change sets (later increments under 0010), `from` context filters, non-equality predicates, filters on single-result queries and null matching.

## Verification

**Done when:** a collection query with equality filters binds, writes and reads as v14 with the shape above; the evaluator returns every matching instance; a specification that lists the matching rows in any order passes and one with a wrong or missing row fails; models without filters keep their bytes and revision; each refusal fires; and `queries.md`, `specifications.md` and `interoperability.md` describe v14.

**Verify by:** the `full-esm-v14.json` vector, `QueryFiltersCorpus.V14`, binder, reader and validator specifications for each refusal, evaluator and runner specifications for matching, omission and order, and a byte comparison of filter-free models against v13.

## Consequences

Filtered list queries, the most common query shape in the samples after keyed lookups, become executable. Specifications on collection queries are order-independent, which matches what the runner already does. Queries that filter from the context stay unexecutable until context sources are admitted.

## Related

Builds on 0004, 0010, 0012, 0025 and 0066. Screenplay [#140](https://github.com/Cratis/Screenplay/issues/140), Stage [#58](https://github.com/Cratis/Stage/issues/58).
