---
id: 0119
title: Admit command handlers that declare a set of possible events (handler result contract v2, ESM v35)
status: proposed
stage: none
class: contract
reversibility: costly
amends: 0082, 0117
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Result contract v1 (0082, 0117) gives the command all the control and the body only the payloads.
- The command's unconditional `produces` lines fix the event types, the count (exactly one per line), the order, the destinations, the tags and the response.
- The body supplies only the payload values.
- `produces when`, repeated productions and a cardinality chosen by the body are refused with `PLAY0800`.
- A body cannot reject.

Many real handlers decide *which* events to emit and *how many*. For example, `ProcessInvoiceBatch` in the Invoicing sample emits:
1. a start event;
2. one sent-or-cancelled event per invoice, each on that invoice's event source;
3. a completion event.

It cannot be expressed today. Inferring the output from the body would let renderers, routing, protected reads and the runtime guess, which 0082, 0117 and 0096 forbid. ([Cratis/Screenplay#1102](https://github.com/Cratis/Screenplay/issues/1102); background in [#1068](https://github.com/Cratis/Screenplay/issues/1068) and [#309](https://github.com/Cratis/Screenplay/issues/309).)

## Decision

### 1. Syntax

The handler declares the events it may produce in a `may produce` block. The block is the first child of `handler`, before the body source (a fence, `file` or `implementation`); a pending intent may declare one too. The command's properties, `authorize` and `validate` are unchanged.

```screenplay
  handler
    may produce
      InvoiceBatchProcessingStarted   for batchId
      InvoiceSent                     for one of invoiceIds
      InvoiceCancelled                for one of invoiceIds
      InvoiceBatchProcessingCompleted for batchId
    ```csharp
    result.Add(new InvoiceBatchProcessingStarted(InvoiceIds.Count, context.Occurred));
    foreach (var id in InvoiceIds)
    {
        if (Action == "send") result.Add(id, new InvoiceSent(context.Occurred, null));
        else result.Add(id, new InvoiceCancelled("Batch", context.Occurred, context.Identity.Id, "Batch"));
    }
    result.Add(new InvoiceBatchProcessingCompleted(InvoiceIds.Count, context.Occurred));
    ```
```

Rules:
- **Entry form:** `<Event> [for <property> | for one of <collection property>]`.
  - An entry may carry route and tag children.
  - `when` and mappings are refused.
  - An event type appears at most once in the set.
  - An empty block is an error.
- **Placement:** the block is valid only inside `handler`. Command lines fall through to property parsing, so a command-level keyword would be ambiguous.
- **One contract per command:** `may produce` beside command-level `produces` is an error. `produces when` beside a handler keeps `PLAY0800`, so `when` keeps a single meaning.
- **Single sequence:** a set contains only event-log events or only published public events. A mixed set is an error, and foreign events are refused as for any production.
- **Legacy:** models without the block keep their bytes and their meaning.

### 2. Cardinality and order
- The body produces an ordered sequence of declared events, and that order is the append order.
- Zero events is a valid, fact-free success.
- An entry may occur any number of times, in any order.
- A host may set a maximum count; exceeding it is a contract violation.

### 3. Destinations (0096)
- **`for <property>`:** a scalar destination taken from the command. The body cannot override it.
- **`for one of <collection>`:** the body names the destination for each item.
  - The destination must equal an element of that command collection (canonical value equality).
  - The element type must be valid as the event's source identity.
  - Duplicate destinations are allowed.
  - An absent, null or empty collection allows no destinations.
- **No `for`:** the command's default destination applies, with the same refusal as ordinary productions when there is none.
- Named routes stay declarative per entry.

### 4. Tags, metadata, generated values and the response
- Tags resolve declaratively for each emitted event, exactly as for an ordinary production.
- All facts share one command occurrence.
- Generated values exist before the body runs.
- The response is unchanged from v1.

### 5. Context, result and refusal
**The typed context stays at contract version 1.**
- Result contract v2 adds a separate, generated result parameter: in C#, `result`, a per-handler type with one `Add` per scalar or default entry and one `Add(destination, …)` per `one of` entry.
- The body returns nothing.

**The body cannot refuse.**
- Rejection belongs to `authorize`, `validate` and code validations, which run before the body.
- A body failure is the existing typed body-fault outcome (faulted, timed out, memory exceeded or cancelled), and it produces no facts.

### 6. Protected reads (0003, 0017, 0085)
**Protection depends on the declared reads, not on the output.**
- Commit validates the dependency set whatever the body returns, including zero events.
- A conflict returns `DecisionStateChanged` with no facts.

**Every producer traversal treats `possibleEvents` exactly like `produces`.** This includes the routed-producer check for protected views in both the binder and the semantic validator.

v2 is admitted together with protected reads exactly where v1 is.

### 7. ESM v35

The handler result contract v2 is allocated **ESM v35** under 0025. v34 (0115) was released in Screenplay v4.246.0, and v35 is the next unused number. The change is a new required member with a new meaning, so it is a new version, not an extension.

`command.handler` writes `resultVersion: 2` and a `possibleEvents` array.
- Each entry follows the produced-event member order:
  1. `eventContract`;
  2. an optional `destination` expression with a `kind`;
  3. optional `tags`;
  4. an optional `route`.
- `one of` is a new expression kind, `oneOf`, carrying `property`. It is valid only inside `possibleEvents`.
- `command.produces` is empty for v2.

**Writer gate:** the canonical writer refuses `resultVersion` other than 1, or a non-empty `possibleEvents`, below v35.

**Strict readers** for v1–v34 refuse the new shape.

**Sidecar:** the requirement carries `resultVersion: 2` and `contextVersion: 1`. Linkage requires the ESM and sidecar result versions to match, and the requirement identity is unchanged.

**Public API:** extended only through non-positional `init` members.
- the command-handler model gains `PossibleEvents`;
- the body-execution result gains `Items` (an ordered event contract, payload and optional destination), with a separately named v2 factory.

Synthesized equality, hashing, `ToString` and reflection-based serialization of those records include the new members. Release notes state this.

### 8. How consumers that have not admitted v2 refuse
- **Not admitting v35:** the consumer refuses the **whole model**. This covers a strict reader, `cratis render` with CLI-RENDER-004, and the Stage, Stage.Axon, Arc.Screenplay, CritterStack.Screenplay and Screenplay.Generation packages.
- **Admitting v35 but not result v2:** refusal is **per command**, as in 0117. The command is never crashed and never falls back to declarative mappings.
- **The reference evaluator without a provider:** reports `Unsupported(ImplementationBody)`.

### 9. Runtime contract

A host that executes v2 must:
1. **Run in order:** authorize → validate → generate values → run the body → validate the result → append.
2. **Validate the result, before anything is appended, with the shared Screenplay validator.** It checks that:
   - each item's event is declared;
   - each payload type-checks against the event's current revision;
   - scalar and default entries carry no destination;
   - each `one of` destination is in the snapshotted collection;
   - the count is within the bound.

   Any failure rejects the whole result with no facts, as a contract-violation fault naming the item.
3. **Append the result as one group:**
   - in body order, with one occurrence, against the expected tail;
   - possibly across many event sources, but always in one sequence;
   - accepted or rejected as a whole, so a rejected event source fails the group.

   A host that cannot do this refuses v2 as a capability.
4. **Run the body at most once per request.**
   - If the world changes after the body has run (refresh or tail conflict), the host returns the conflict and does not decide again.
   - An indeterminate append is reported as indeterminate and is not retried.

## Options considered

- **Treat `produces X when cond` as declared eligibility, with the body choosing.** Not chosen.
  - `when` would mean a gate in declarative commands and eligibility beside a handler, with two deciders.
  - It cannot express one event per element to different event sources.
  - It contradicts 0082.
- **A new `may produce` set inside `handler`.** Chosen.
  - It is additive, keeps a single decider within a declared range, and gives every event an explicit destination.
  - It costs an ESM version, parser work in both compilers, and renderer and runtime work.
- **Infer the set from the body's return type.** Rejected.
  - Bodies are opaque, so routing, tags and protection could not be checked statically.
  - Destinations would be guessed.
  - The result would differ per language.
- **A command-level `may produce` keyword.** Rejected, because it collides with property declarations.
- **A flag on `command.produces`.** Rejected, because it would mix two meanings in one array.
- **Declarative fan-out (one event per collection element).** A complement for cases without body choice, left to [#309](https://github.com/Cratis/Screenplay/issues/309). `for each` stays reserved by 0006.

**Default without this record:** handlers with a variable output remain refused with `PLAY0800`.

## Consequences

**Not a major change** for the Screenplay package or for ESM consumers:
- v35 is opt-in by syntax;
- v1–v34 bytes, hashes and digests are unchanged;
- older readers refuse rather than misread;
- public records gain only `init` members.

**Until a transactional cross-sequence append exists,** a v2 set cannot mix event-log and published events.

**Admission arrives in one step.** Parsing, binding and execution may merge earlier, but public binding keeps `PLAY0800` for the v2 forms until the change that allocates v35 also lifts the refusal. No release can carry v2 under v34.

**Other affected surfaces:**
- Studio's export must preserve the `may produce` block.
- The editor grammar and completions add `may produce` and `for one of`.
- Renderers and the Stage ledger must admit or refuse the new members explicitly.

## Verification

**Done when:**
- both compilers parse, print and bind the block;
- `ProcessInvoiceBatch` binds;
- the decision-read routed-producer check covers `possibleEvents`;
- a mixed private/published set is refused;
- the writer refuses v2 below v35;
- `full-esm-v35.json` round-trips, and v1–v34 vectors are byte-identical;
- older readers refuse a v35 model;
- the shared result validator and the reference evaluator agree on these cases:
  - zero, repeated and ordered output;
  - a destination outside the collection;
  - an absent collection;
  - an undeclared event;
  - a destination on a scalar entry;
  - a body fault;
  - protected reads with zero output.

**Verify by:**
- compiler and canonical-vector specs;
- corpus vectors for every case above;
- TypeScript compiler specs;
- a Stage regression showing that a v35 model is refused per command before execution support lands;
- runtime specs proving:
  - an atomic multi-source append;
  - no facts after a contract violation;
  - exactly one body invocation on both the refresh and tail-conflict paths.

**The documentation fix ships with this record:** `Documentation/screenplay/interoperability.md` moves ESM v34 into the released table (released in Screenplay v4.246.0), and the "Claimed, unreleased version" block is removed.
