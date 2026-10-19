---
id: 0101
title: A given without an event source belongs to the event source the specification acts on
status: accepted
stage: none
decided: 2026-10-19
decider: einari (owner)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/projections/semantic-model.md
---

## Context

A specification may state a `given` event without `for`. The reference evaluator accepts it and keeps a "legacy
null destination" for it, even in ESM v2 and later where `for` exists (Cratis/Screenplay#579). That phrase described
what the evaluator stores, not what the fact means, so a consumer that has to append the given to a real event
stream, such as Stage running specifications against Chronicle (Cratis/Stage#14, Cratis/Stage#15), had nothing to
reproduce.

The owner ruled on the issue
([comment](https://github.com/Cratis/Screenplay/issues/579#issuecomment-6262697157)): a given without an event
source id is fine. Specifications assert behavior, not event source ids, and a slice has, for the most part, one
event source, if it has one. Sourceless givens are not deprecated and get no diagnostic.

The language is frozen at ESM v28, so the definition must not need a grammar change, a new ESM version or different
executable bytes. It does not: the evaluator already attributes a sourceless fact to the event source of the fact a
constraint checks (`SemanticConstraintEnforcement.SourceOf`), and refuses a projection that keys on an event source
identity the fact does not carry (`SemanticScopedProjection.EventSource`).

## Decision

A `given` event without `for` is history of the event source the specification acts on. It is not on a stream of
its own, and it is not on a wildcard stream.

- **The action's event source.** For `when <Command>`, that is the event source each fact the command appends goes
  to. For `when append <Event> for <value>`, it is `<value>`. A sourceless `when append` and sourceless givens share
  one event source.
- **Constraints.** A unique-event constraint counts a sourceless given as already on the action's event source, so
  the action is refused. A unique-value constraint treats a sourceless given's value as the action's own claim, so
  the action may claim it again; a value held by a given with a different `for` is still refused.
- **No identity is invented.** Where the outcome needs the given's event source identity and the action does not
  supply one (no action, a query, a trigger, a clock or a redelivery), the reference evaluator reports the
  specification as `Unsupported` with capability `Projection` instead of choosing an identity. Givens whose event
  source nothing reads still establish state.
- **Consumers.** A consumer that must append the given to a concrete event source appends it to the action's event
  source. When the action appends to exactly one event source, which is the common case, the outcome is the
  reference outcome. When there is no action event source, it may use any one identity, the same for every
  sourceless fact in the specification and different from every identity the specification states; no reference
  outcome depends on that identity.
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

## Related issues

- Cratis/Screenplay#579 — this decision.
- Cratis/Stage#14, Cratis/Stage#15 — consumers that append specification givens to Chronicle.
- Decision [0031](0031-event-source-and-stream-in-specifications.md) — `for` and routes in specifications.
- The canonical vector `SourcelessGivensCorpus` (`Source/DotNET/Screenplay.CanonicalCorpus/Corpus/SourcelessGivens`)
  pins the definition.
