---
id: 0115
title: A specification runs its own slice only, and states the commands its reactions invoke (ESM v34)
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-10-25
decider: Einar Ingebrigtsen
amends: 0022, 0098
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A slice is built in isolation and is self-contained. Its specifications are how it is proved, so they must concern
that slice and nothing else.

The reference execution did not work that way. [0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)
let every reaction in the model follow a specification's action, and compared `then` events against every new fact,
the action's and the reactions'. [0098](0098-admit-the-constructs-the-acme-commerce-sample-is-blocked-on.md) went
further and let a command's specification state the public event another slice, an outbound `Translate`, publishes.

The consequence surfaced in the Acme Commerce sample. `SetProductPrice` / `SettingAPriceIsPublished` stated
`ProductPriceChanged`, which is produced by the outbound slice `PublishPriceChange`, and passed. A specification that
left the event out failed with "Expected 1 fact(s), got 2". A slice's specification therefore had to specify, and
change with, behavior it does not own. An automation that invoked another slice's command proved itself by stating that
command's events.

This was never a deliberate contract; it was a mistake in the reference execution. The correction applies to every
model, at every version.

## Decision

1. **A specification runs only the slice that declares it.** Only that slice's reactions, captures and outbound event
   projections run. Other slices' reactions, captures and publications never run, so their facts cannot appear in
   `then`, read-model or query assertions. Read-model projections, queries and constraints stay in force: they describe
   state and contracts, not behavior.
2. **A command another slice owns is decided, not adopted.** When the slice's reaction invokes it, the command is
   evaluated against the world so the reaction's `on refused` branches and refusals behave as before, but the facts it
   would append are that slice's, and are not kept. A command the specification's own slice declares runs in full.
3. **The compile-time outcome check follows the same rule.** `PLAY0285` follows only reactions, invocations and
   publications declared in the specification's own slice. Stating another slice's event is refused as unreachable.
4. **`then invokes <Command>` states an invocation.** An automation proves what it does to another slice by stating the
   command it invokes and the values it passes:

   ```screenplay
   then invokes CloseInvoice
     invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
     reason = "paid"
   ```

   The run records every invocation the slice's reactions make, in order. When a specification states at least one
   `then invokes`, the recorded invocations must match them exactly in count and order (any order under
   `then events in any order`), comparing only the stated values. A specification that states none does not compare
   invocations. The statement binds into ESM as `thenInvocations`, written only when present, so it selects the new
   ESM v34 and leaves every earlier model's canonical bytes unchanged.
5. **Earlier pinned vectors are corrected, not preserved.** The canonical corpora whose specifications stated another
   slice's facts are restated and their bytes regenerated. This is the exception to
   [0066](0066-published-esm-versions-are-frozen.md) that a correction of wrong behavior warrants: the earlier vectors
   pinned the mistake.

## Options considered

- **Isolate every model (chosen).** The rule is about what a specification means, and old models deserve it as much as
  new ones.
- **Isolate only from a new ESM version.** Rejected: it keeps the mistake alive for every existing model and leaves two
  meanings of the same specification.
- **Isolate without `then invokes`.** Rejected: an automation whose effect is invoking another slice's command would
  have nothing it may state.

## Default if unanswered

Specifications keep requiring knowledge of other slices, and a specification that is right for its slice fails when a
neighbour gains a reaction or a publication.

## Timeline and scope

In force from 2026-10-25 until superseded. In scope: reference execution, the outcome check, the `then invokes`
statement and its ESM v34 binding, the canonical corpora, the Acme Commerce sample and the documentation. Consumers
(Stage, Studio) adopt the isolation through the reference execution they host, and admit ESM v34 or refuse it with a
typed refusal.

## Verification

**Done when:** a command specification that states another slice's event is refused by `PLAY0285` and fails at run
time; an automation specification passes with `then invokes` and fails on a different invoked value; the canonical
corpora and the Acme Commerce sample pass with every specification stating only its own slice.

**Verify by:** `for_SemanticSpecificationRunner/when_a_slice_publishes_what_another_slice_appends`,
`when_running_reactions/and_other_slices_react_to_the_command`, the `then invokes` runner and binder specifications, the
canonical vector suite, and `screenplay test Samples/AcmeCommerce`.

## Consequences

A slice's specifications change only when the slice changes. A reaction that sets off a chain across slices is proved
link by link: each slice's specification states its own link, and `then invokes` states the hand-over.

0022's "then compares every new fact, the action's and the reactions'" now reads "the facts of the specification's own
slice". 0098's statement of a public event in a command's specification no longer holds; the public event is stated in
the outbound `Translate` slice's specification. Both records keep their text, with a note pointing here.

## Related

Amends 0022 and 0098. Builds on 0004 and 0066.
