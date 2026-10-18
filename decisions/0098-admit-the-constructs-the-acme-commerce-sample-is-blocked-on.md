---
id: 0098
title: Admit the constructs the Acme Commerce sample is blocked on (exception to the freeze)
status: accepted
stage: none
decided: 2026-10-18
decider: Einar (owner, maintainer)
class: product
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
  - Samples/AcmeCommerce/**
---

## Context

Decision 0092 freezes the language at ESM v28. Item 4 allows a construct only when a real model is blocked and no
reasonable model-side alternative exists, named in the decider's own record. This record is the request. It does not
admit anything until the decider accepts it, item by item.

The model is `Samples/AcmeCommerce` (Cratis/Scene#96): five bounded contexts (Catalog, Storefront, Sales, Purchasing,
Warehouse) with public events between them. Building it surfaced eight gaps, each filed and each carrying a workaround
in the sample. The workaround is the alternative tried. Cratis/Screenplay#862 (a performer query leaves the application
unbound) is not in this list: it is a binder bug fixed without a construct (Cratis/Screenplay#920).

## Requests

Each item says what is blocked, the alternative tried, and why the alternative is or is not reasonable. A recommendation
follows each; the decider may accept any subset.

1. **#857 list payloads in captures (fan-out).** Blocked: a multi-line order cannot cross a context boundary in a form the
   evaluator and Stage run (`Unsupported`: list values in a capture). Tried: one scalar line per public event, keyed so
   each event is a new key. Not reasonable for a real order or receipt: the contract no longer matches the business fact
   and consumers cannot see an order as one unit. Recommend: accept.
2. **#858 generated values for a command a reaction invokes.** Blocked: an automation cannot mint an id and its
   specification ends `Unsupported(IdentityAllocation)`. Tried: derive the id from the triggering event. Reasonable when
   the trigger carries a unique id; not when it does not (alarms, schedules). Recommend: accept, smallest form
   (a specification step pins the generated value).
3. **#859 append to several event sources in one command.** Blocked: an atomic decision over two aggregates. Tried: an
   automation chain (command, event, reaction, command). Reasonable as an eventually consistent design, which is also what
   event-sourced practice recommends; atomic multi-source append is a consistency promise the event store may not keep.
   Recommend: decline; document the rule next to `for` in `commands.md`.
4. **#861 map one concept to another over the same primitive.** Blocked: an id crossing contexts forces one concept shared by
   both sides (`PLAY0273`). Tried: declare the consumer's own concept in the consumer's local copy of the contract. This
   is the anti-corruption layer and is reasonable. Recommend: decline; document the pattern. (An admission change here
   alters `SemanticTypeCompatibility`, which Stage and every host share, so it needs an ESM version.)
5. **#864 multiplication in projection arithmetic.** Blocked: a basket or order total cannot be a read model. Tried: the
   command takes the total as input. Not reasonable: a client-supplied total is a correctness and trust hole.
   Recommend: accept.
6. **#866 state the published event in a command specification.** Blocked: the accepted path of a publishing command
   cannot be specified on the command (`PLAY0285`, exact fact count). Tried: specify the accepted path in the outbound
   slice. Reasonable, the sample does it. Recommend: decline; document.
7. **#867 decimal places on a Decimal concept.** Blocked: `Money` cannot say it has two decimals. Tried: `>= 0` only.
   Weak: the rule is real and has to be hand-written in every target. It is also a rule the compilers, generators and
   Stage must all carry. Recommend: accept if consumers are ready; otherwise defer.
8. **#868 application-level toolbar buttons.** Blocked: buttons declared once for every screen. Tried: repeat the button on a
   screen of each domain. Reasonable (a presentation convenience, no business meaning). Recommend: decline.

## Decision

Accepted for all eight items (#857, #858, #859, #861, #864, #866, #867, #868), decided by the owner on 2026-10-18.
The freeze of decision 0092 was set for the maintainer who asked for it and does not bind the Acme Commerce work; this
record is the item 4 exception the freeze asks for. The recommendations above were advisory; the owner accepted
every item. Each item is implemented with a specification, documentation and AI corpus on every surface, and
lands in Stage, the CLI, Studio and the AI corpus under the catch-up rule of 0092 item 5. The ESM version
numbers each item takes are listed in a superseding note on the pull request that admits it.

## Options considered

- Accept all eight. Chosen by the owner.
- Decline all eight. Possible; the sample then keeps its workarounds, which are listed in its README.

## Related issues

Cratis/Screenplay#857, #858, #859, #861, #864, #866, #867, #868; decisions 0092; Cratis/Scene#96.
