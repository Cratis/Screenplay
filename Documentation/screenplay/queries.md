---
title: "Queries"
description: "Declare Screenplay queries, the read-side entry points: return type, one-shot or observable, parameter sources and performance."
---

# Queries

Queries are read-side entry points. A query maps to a return type — a read model, or a collection of read models with `[]` — and says what it is for, whether it answers once or keeps answering, where each of its parameters comes from, and, when it needs to, how it is performed.

## Syntax

```screenplay
query <Name> => [observable] <ReturnType>[[]] [optional]
  [description "<text>"]
  [by <paramName> <Type> [from <source>]]
  [filter <paramName> <Type> [optional] [from <source>]]
  [scoped to <scope>]
  [authorize <PolicyName> [or <PolicyName>]*]
  [performer
    file <Path>
    | ```csharp
        <C# returning the result>
        ```
    | ```sql
        <SQL returning the result>
        ```]
```

| Clause | Meaning |
| --- | --- |
| `observable` | The query is a live read — it keeps pushing as the read model changes, instead of answering once. |
| `description` | What the query is trying to accomplish, in prose. |
| `by` | The identifying parameter — the query returns the instance it identifies. |
| `filter` | A caller-supplied equality parameter narrowing the result set. Write `optional` after the type when the caller may omit the filter; otherwise it is required. |
| `from` | Fills the parameter from the query context instead of the caller. |
| `authorize` | The [policies](policies.md) that must pass. |
| `performer` | The code that performs the query — an external file, or an inline block. |

## Description

A query's `description` says what the read is *for*, in the words of the domain. It is the first body line, at most one per query, and takes either the quoted single-line form or a fenced block — the same shape as a [command description](commands.md#description):

```screenplay
query GetOverdueInvoices => OverdueInvoicesReadModel[]
  description "Invoices past their due date, oldest first, for the collections worklist"
  authorize IsAccountant
```

That sentence is what a reader — human or LLM — has to work from when generating or reviewing an implementation, so write the intent, not a restatement of the name.

## Observable queries

Some reads answer once — a report a user asked for, a lookup a screen does when it opens. Others are meant to stay current: a worklist that gains a row the moment an invoice goes overdue, a status a second user changes while the first is looking at it. Those are two different promises to the caller, and without saying which one a query makes, a document leaves the reader — and the implementation — to guess.

`observable` qualifies the return type and settles it:

```screenplay
query LiveOverdueInvoices => observable OverdueInvoicesReadModel[]
  description "Invoices past their due date, kept current on the collections board"
  authorize IsAccountant
```

Read it as the query yields *an observable list of overdue invoices*. Without the marker, the query is one-shot — the default, and what most reads are:

```screenplay
query GetInvoice => InvoiceDetailsReadModel
  description "One invoice with its lines, customer and shipping status"
  by invoiceId InvoiceId
```

The marker qualifies only *how* the result arrives, so everything else about the query is unchanged: `observable` composes with `[]` and `optional` (`observable InvoiceDetailsReadModel optional`), with `by` and `filter` parameters, with `authorize`, and with a `performer`. A [screen](screens.md) binds to a live query exactly the way it binds to a one-shot one — `data <ReadModel> via query <QueryName>` — and gets the updates for free.

The result `=> observable optional` means an observable result whose type is named `optional`, not an optional result of a type named `observable`. The leading `observable` is read first. To make that result optional, write `=> observable optional optional`.

A one-shot query returning an optional scalar type named `observable` must keep `query Q => observable?`. This is the one place the `?` spelling remains canonical: replacing it with `optional` changes the meaning. It produces no `PLAY0479`, and quick fixes and document migrations leave it unchanged. Collections (`=> observable[] optional`) and live queries (`=> observable observable optional`) still use the keyword.

## What the caller sees

`authorize` says *who may call* a query. In the executable semantic model, declarative gates on optional keyed lookups, unkeyed list queries and keyed list queries run before the lookup. A keyed query compares a supplied caller against the `by` argument; an unkeyed list query has no query-key artifact. A missing caller or failed gate yields `Unauthorized`; module and feature gates also apply by AND. If evaluation reaches an opaque ESM v3 policy predicate, the reference evaluator returns `SemanticUnsupported` instead of guessing a decision. Caller-supplied equality filters on collection queries are admitted by ESM v14. Policies still see only the `by` argument, not filters. A policy that names a query filter reports `PLAY0273`; declare that argument with `by` instead to authorize by it. Explicit scopes and context-sourced filters remain authoring syntax and are not admitted into the reference evaluator. A performer-backed query is an opaque read (see [below](#a-query-a-performer-answers-is-an-opaque-read)). It says nothing about *what they get back* — and in a real application those are different questions. `All` and `Mine` may admit exactly the same callers and return entirely different rows, and that difference is the access model a reader needs.

`scoped to` states it:

```screenplay
query Mine => Timesheet[]
  scoped to identity
  authorize Consultant

query Everyones => Timesheet[]
  scoped to global
  authorize Administrator
```

Without it, a query returning only the caller's own rows reads as one returning everyone's — the opposite of what it does.

### The tenant is the default, and stays unstated

A query is scoped to the tenant it runs for unless it says otherwise. That is the overwhelmingly common case, so writing it down would be noise on almost every query in a document:

```screenplay
query ForTenant => Timesheet[]
```

That query is already scoped to the current tenant. Nothing states it, because nothing needs to.

`scoped to global` is how a query *opts out* and reaches past the tenant. Making the narrow case the default means the dangerous case is the one you have to write down, rather than the one you get by forgetting.

### The scope is a name, not a closed set

`identity` and `global` are the two the language documents, but the grammar accepts any name. What scopes exist follows the identity model of whatever runs the document — Screenplay states that a query is scoped and to what, and leaves enforcing it to the runtime. A query declares at most one scope; results are narrowed one way.

## Invalid arguments are rejected

A malformed lookup is not a missing instance. After authorization and argument shape checks, the reference evaluator validates the `by` key and every supplied filter value against the concept's rules, just as it validates command inputs. A rule violation returns `Validation`, carrying the first failure's message (including `$strings` keys) and every validation failure. Failure order is the key first, supplied filters in declaration order, and each concept's rules in declaration order. This applies in every ESM version without changing model bytes.

An omitted optional filter is not validated. A supplied value with the wrong shape, including a null filter, still rejects as `Contract`. Authorization denial remains `Unauthorized` and happens first. An opaque concept rule returns `SemanticUnsupported` with the `Query` capability rather than inventing a result. A valid argument matching no instance still returns no result.

From ESM v18, use one actionless query and `then error` to [specify a Validation rejection](specifications.md#query-validation-rejections); do not use `then no result` for a malformed concept value.

## Composite lookups

A bare `by` header declares at least two `name Type [from source]` children. It cannot combine with the single `by name Type` form. Names are read-model key property names. From ESM v27, an optional single-instance query supplies every flat key part with its required compatible nominal type and caller-supplied value. Collection queries use equality filters or one scalar `by name Type` line, not a `by` block. See the complete example in [Read-model keys](readmodels.md#keys).

Collection `by` blocks, context-sourced parts and non-optional single-instance returns remain refused with `PLAY0268`. Missing parts use `PLAY0661`; invalid shapes and parts use `PLAY0662`. Queries have no key-completion repair.

## Parameters

`by` names the identifying parameter; `filter` narrows the result set. Both are supplied by the caller — a screen, an API client — unless they declare a source with `from`:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus optional
  filter customerId CustomerId optional
  filter tenantId   TenantId from $context.tenant
  authorize IsAuthenticated
```

`status` and `customerId` come from the UI. `tenantId` comes from the [query context](context.md) — the caller never chooses it, and the document says so once instead of every implementation remembering to. Any [mapping source](commands.md#mapping-sources) works as a `from` source, so `$env.` and constants are available too.

### Executable equality filters

In ESM v14 each supplied filter matches the read-model property with the same name using value equality. The property must be a scalar primitive, concept or enum, and the filter must have the same nominal type, ignoring optionality. Names must be distinct and cannot duplicate `by`. Filters on single-result queries and filters with a `from` source are refused with `PLAY0268`, naming the query, filter and reason.

`by` and every supplied filter combine with AND. An instance missing the filtered property does not match. A required filter must be supplied; an optional filter may be omitted and then does not narrow. Supplied `null` is refused, even for an optional filter. `T optional` is the canonical spelling; `T?` is legacy.

Query specifications name `by` and filters in their arguments. Unknown, repeated or missing required arguments report `PLAY0468`. Collection rows compare without regard to order, matched by read model and instance key, with exact row count. See [Query results](specifications.md#query-results).

## Examples

A single-instance query identified by a parameter:

```screenplay
query GetInvoice => InvoiceDetailsReadModel
  description "One invoice with its lines, customer and shipping status"
  by invoiceId InvoiceId
  authorize IsAuthenticated
```

An observable list query for a whole screen:

```screenplay
query AllWorkItems => observable WorkItemSummary[]
  description "Every work item on the board, kept current as work changes"
```

An observable list narrowed by a caller-supplied key:

```screenplay
query CommentsForWorkItem => observable CommentView[]
  description "All comments for the selected work item, kept live while the detail screen is open"
  by workItemId WorkItemId
```

A collection query with optional filters:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus optional
  filter customerId CustomerId optional
  authorize IsAuthenticated
```

The executable semantic model admits unfiltered lists, lists narrowed by one caller-supplied `by` key, and caller-supplied equality filters on collections, including their `observable` forms. A model with filters selects ESM v14; models without filters keep their existing bytes, version and revision. `scoped to`, `by ... from <source>` and `filter ... from <source>` remain unadmitted (`PLAY0268`), each with its own message. A `performer` query is not refused: it is an opaque read. The reference evaluator answers a live query with a snapshot; live change sets, paging and ordering are not part of v14.

## Executable query shape

The executable semantic model serializes the admitted collection shapes explicitly so Stage can generate the same query, projection and handler routes in every host:

```json
{
  "name": "AllWorkItems",
  "readModel": "<WorkItemSummary semantic id>",
  "cardinality": "many",
  "delivery": "live"
}
```

An unkeyed list query has no `argument` and no `keyProperty`. It routes to the whole read-model collection.

```json
{
  "name": "CommentsForWorkItem",
  "readModel": "<CommentView semantic id>",
  "argument": {
    "name": "workItemId",
    "type": { "kind": "concept", "concept": "<WorkItemId semantic id>" }
  },
  "keyProperty": "<CommentView.workItemId semantic id>",
  "cardinality": "many",
  "delivery": "live"
}
```

A keyed list query routes through its caller-supplied `argument` and matches that value against `keyProperty` on the read model. Stage should map `delivery: "live"` to the native observable query lifecycle. `delivery: "snapshot"` uses the same shape without live updates.

A v14 filtered query writes its `filters` after the `argument`/`keyProperty` group, or after `readModel` when unkeyed, and before `cardinality`. This excerpt shows a required status filter and an optional customer filter:

```json
"filters": [
  { "id": "<status argument id>", "name": "status", "type": { "kind": "concept", "primitive": null, "target": "<InvoiceStatus id>", "collection": false, "optional": false }, "property": "<status property id>" },
  { "id": "<customer argument id>", "name": "customerId", "type": { "kind": "concept", "primitive": null, "target": "<CustomerId id>", "collection": false, "optional": true }, "property": "<customerId property id>" }
]
```

There is no separate optional flag on the filter: its type reference carries optionality. Query requests, returned results and specification query results carry only supplied values, after `key`, in query filter declaration order:

```json
"filters": [
  { "filter": "<status argument id>", "value": { "kind": "string", "value": "issued" } }
]
```

A query call is identified by the query, its `by` key and its supplied filter values. Two calls of one unkeyed query with different filters are distinct. Both arrays are omitted when empty. Consumers must explicitly admit v14 or refuse the model rather than drop its filters.

## The `performer` block

Most queries need nothing more than their return type — the read model a [projection](projections/index.md) already builds is the answer. When a query does need its own logic, `performer` is where it goes. It is the query's counterpart to a command's [handler](commands.md#the-handler-block), and it takes the same two shapes: a `file` reference, or an inline block.

Delegating to a file:

```screenplay
query GetInvoiceSummary => InvoiceSummaryReadModel
  description "The counters behind the dashboard header"
  performer
    file Queries/InvoiceSummaryPerformer.cs
```

Inline, in a top-level language:

````screenplay
query GetOverdueInvoices => OverdueInvoicesReadModel[]
  description "Invoices past their due date, oldest first"
  performer
    ```csharp
      return readModels
          .Where(invoice => invoice.Status == InvoiceStatus.Overdue)
          .Where(invoice => invoice.TenantId == context.Tenant)
          .OrderBy(invoice => invoice.DueDate);
      ```
````

Or in SQL, when the read is a query against a relational store:

````screenplay
query ListLineItems => InvoiceLineReportReadModel[]
  description "Every invoice line, priced, for the accounting line report"
  filter tenantId TenantId from $context.tenant
  performer
    ```sql
      select   InvoiceId, LineNumber, Quantity, UnitPrice
      from     InvoiceLineReport
      where    TenantId = @tenantId
      order by InvoiceId, LineNumber
      ```
````

Inside a performer, `context` is the [`QueryContext`](context.md) — the query's arguments, the tenant, the caller, the identity recorded as having caused it, the causation, and when the query was received. A `file` reference compiles against the same type, so moving a block out to a file changes nothing about what it can see.

### A query a performer answers is an opaque read

The code decides what a performer returns, so no declarative shape can be executed or specified. Binding does not refuse the application for it. The query is listed as code the model needs (`QueryPerformer`), reported as `PLAY0270` information (`Query '<name>' is performed by code`), and left out of the executable model: it takes no part in the plan and no specification can assert it. A specification whose `then query` names a performer query is left out of the executable specifications with the same information; every other declaration and specification of the application keeps running with `screenplay test`. Everything else about the query — its read model, the screen bound to it, its authorization — is unchanged.

A read model a performer composes therefore sits next to projected read models without making the application `unbound`. Specify the declarative part (the events that feed it) as usual; the performer's own logic is realization metadata that a test of the real code covers.

A query is complete without a performer. The `performer` is realization metadata, not a precondition — see [Declarative first](grammar.md#declarative-first--file-is-never-required).

## Guidance

- Name queries as descriptive reads: `GetInvoice`, `ListInvoices`, `GetOverdueInvoices`.
- Mark a read `observable` when the caller is meant to see changes without asking again; leave it off for a one-shot read.
- The return type is a read model built by a [projection](projections/index.md) in the same or another `StateView` slice.
- Anything the caller must not be able to choose — the tenant, the caller's own subject — belongs on a `from` parameter, not on a `filter` the UI supplies.
- [Screens](screens.md) bind to queries with `data <ReadModel> via query <QueryName>`.
