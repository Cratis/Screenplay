---
id: 0101
title: A given without an event source belongs to the event source the specification acts on
status: accepted
stage: implemented
decided: 2026-10-19
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/projections/semantic-model.md
---

## Context

A specification may state a `given` event without `for`. The reference evaluator accepts it and keeps a "legacy
null destination" for it, even in ESM v2 and later where `for` exists (Cratis/Screenplay#579). That phrase described
what the evaluator stores, not what the fact means, so a consumer that has to append the given to a real event
stream, such as Stage running specifications against Chronicle (Cratis/Stage#14, Cratis/Stage#15), had nothing to
reproduce.

The owner, Einar Ingebrigtsen, ruled on the issue
([comment](https://github.com/Cratis/Screenplay/issues/579#issuecomment-6262697157)): a given without an event
source id is fine. Specifications assert behavior, not event source ids, and a slice has, for the most part, one
event source, if it has one. Sourceless givens are not deprecated and get no diagnostic. That comment is the
ruling; the detailed rules below follow from it and were delegated.

The language is frozen at ESM v28, so the definition must not need a grammar change, a new ESM version or different
executable bytes. It does not: the evaluator already attributes a sourceless fact to the event source of the fact a
constraint checks (`SemanticConstraintEnforcement.SourceOf`), and refuses a projection that keys on an event source
identity the fact does not carry (`SemanticScopedProjection.EventSource`). Givens are projected while the
specification's world is established, before any action runs (`SemanticSpecificationRunner.EstablishWorld`), so that
refusal does not depend on the action.

## Decision

A `given` event without `for` is history of the event source the specification acts on. It is not on a stream of
its own, and it is not on a wildcard stream.

- **The action's event source.** For `when <Command>`, that is the event source each fact the command appends goes
  to. For `when append <Event> for <value>`, it is `<value>`. A sourceless `when append` and sourceless givens share
  one event source.
- **Constraints.** A unique-event constraint counts a sourceless given as already on the action's event source, so
  the action is refused. A unique-value constraint treats a sourceless given's value as the action's own claim, so
  the action may claim it again; a value held by a given with a different `for` is still refused.
- **No identity is invented.** Whenever a projection needs the identity of a sourceless given, regardless of the
  action, the reference evaluator reports the specification as `Unsupported` with capability `Projection` instead of
  choosing an identity. That is a projection keyed on the event source, including the default key. It holds for
  `when <Command>` and `when append` as much as for no action, a query, a trigger, a clock or a redelivery, because
  givens are projected before the action runs. Givens whose event source no projection needs still establish state.
- **Consumers.** A consumer that must append the given to a concrete event source appends it to the action's event
  source. When the action appends to exactly one event source, which is the common case, and no projection needs the
  given's identity, the outcome is the reference outcome. When a projection needs it, the reference outcome is
  `Unsupported`, so there is nothing to match and a consumer may report the outcome it computes. When there is no
  action event source, it may use any one identity, the same for every sourceless fact in the specification and
  different from every identity the specification states; no accepted or rejected reference outcome depends on that
  identity.
- **Known divergence: `$eventSourceId` from a sourceless given.** A projection keyed on a payload property that
  maps `$eventSourceId` from a sourceless given reads `null` in the reference evaluator, while a consumer that
  appended the given to the action's event source reads that identity. Following the ruling that specifications
  assert behavior rather than event source identities, a specification should not assert that value; state `for`
  on the given when it matters. The reference evaluator keeps its behavior.
- **Recommendation, not a rule.** When the action appends to more than one event source, or an assertion depends on
  the given's event source, state `for` on the given. No diagnostic enforces this.

Nothing in the ESM changes: a sourceless given is still carried without an event source, as before.

## Options considered

- **Deprecate sourceless givens** with a diagnostic and require `for`. Rejected by the owner: specifications assert
  behavior, and the common slice has one event source.
- **A fixed default event source** shared by all sourceless givens, independent of the action. Rejected: a
  unique-event constraint would then not see the given as prior history of the command's event source, which is
  what an author writing the given without `for` means and what the evaluator already does.
- **Change the evaluator to project sourceless givens onto the action's identity.** Not needed for the ruling and
  would change reference outcomes under the freeze; the `Unsupported` refusal keeps consumers free to do better.

## Default if unanswered

The reference evaluator keeps its behavior: sourceless givens are carried without an event source, constraints
attribute them to the action's event source, and a projection that needs their identity is `Unsupported`. Consumers
that append to real streams have no stated rule and may diverge from each other.

## Timeline and scope

Takes effect now, within the language freeze (0092). In scope: the meaning of a sourceless `given`, the
specification and semantic-model documentation, and the canonical vector. Out of scope: grammar, ESM versions and
bytes, diagnostics and evaluator behavior. Projecting sourceless givens onto the action's identity would change
reference outcomes and needs its own record once the freeze ends.

## Verification

Done when `SourcelessGivensCorpus.V2` pins the four source forms, the canonical bytes and revision, and seven
specification outcomes: a unique-event refusal and a different-source acceptance, a unique-value reclaim and a
cross-source refusal, an actionless projection keyed on a payload property, and two `Unsupported` (`Projection`)
outcomes for a projection keyed on the event source, one without an action and one with `when <Command>`.

Verify by running `dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs --filter
FullyQualifiedName~for_SourcelessGivensCorpus` and seeing it pass.

## Consequences

- Sourceless givens stay valid and undeprecated; authors state `for` only when it matters.
- Consumers such as Stage have one rule for where to append a sourceless given.
- A specification whose sourceless given feeds a projection keyed on the event source cannot pass in the reference
  evaluator; authors who need it to pass state `for` on the given.
- The `$eventSourceId` divergence stays documented rather than fixed.

## Related issues

- Cratis/Screenplay#579 — this decision.
- Cratis/Stage#14, Cratis/Stage#15 — consumers that append specification givens to Chronicle.
- Decision [0031](0031-event-source-and-stream-in-specifications.md) — `for` and routes in specifications.
- The canonical vector `SourcelessGivensCorpus.V2` (`Source/DotNET/Screenplay.CanonicalCorpus/Corpus/SourcelessGivens`)
  pins the definition.
