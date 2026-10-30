---
id: 0121
title: Reaction-effect and snapshot-performer provider contracts on the body-execution seam
status: accepted
stage: none
class: contract
reversibility: reversible
decided: 2026-10-30
decider: woksin (delegated through L0)
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/**
---

## Context

Screenplay#1099 extends the opt-in body-execution provider (`ISemanticBodyExecutionProvider`) to every
implementation kind. Rule, validation and reducer bodies run today. S4 added policy predicates (decision 0005).
S5 added command handlers (decisions 0082 and 0117). `ReactionEffect` and `QueryPerformer` requirements already
carry context and result version 1 and capability `provider-defined`, but no record defines what a provider
receives or returns for them. The reference reaction loop refuses every reaction body ("requires a target
provider"). Performer queries stay outside the executable model (decisions 0097 and 0113); admitting them is a
separate major change that this record does not make.

## Decision

1. **Provider API.** `ISemanticBodyExecutionProvider` gains `ReactAsync` and `PerformAsync`. Both are default
   interface methods that answer Unsupported, so existing providers keep compiling and behaving as before.
2. **Reaction effect, version pair (1,1).**
   - Scope. A body runs only on a reaction with exactly one trigger, an event trigger that declares no `produces`, no `invokes` and no routing override, and only after
     `where` has passed. Clock triggers, application triggers, combined bodies and a missing provider stay
     Unsupported (capability `Reaction`). A body with `reads` stays a binding error under decision 0006.
   - Context. The context carries the event type, the typed event payload and the event source. It carries
     `Tenant`, `CausedBy`, `Causation` and `Occurred` exactly as the run supplies them, with null when the run
     does not supply them. It carries the reaction's `RunsAs`. It has no read access and no sequence number.
   - Result. The result is exactly one of Empty, events (event type and payload) or commands (command and
     payload). A missing result is Unsupported, never Empty. A mixed result is Unsupported.
   - Validation. Every target must be modeled and every payload must match its contract. A generated command
     property is never supplied. The whole batch is validated before anything settles.
   - Settlement. Events take the triggering event's source and context, the clock's `Occurred` and the
     reaction's origin. They honour decision 0078 atomic batches when the application opts in. Commands run
     through ordinary execution, including their own authorization and validation, with the reaction's origin
     and `RunsAs`. A refused command ends the reaction like `invokes` without `on refused`. Commands that
     already settled stay, so no cross-command atomicity is claimed.
   - Failure. An unverified or ambiguous body, or one whose hash, role, owner, source, member or version does not match the trigger, is Unsupported (`Reaction`), and so is a provider
     throw. A host time limit and a non-run cancellation surface as a throw. All diagnostics are sanitized.
     Cancellation of the run itself propagates.
3. **Snapshot performer, version pair (1,1), Many only.**
   - Context. The context carries typed arguments, `Tenant`, the caller identity and `ReceivedAt`, plus an
     immutable snapshot of rows of the declared read model only. It has no causation and no `Occurred`.
   - Result. The result is a non-null ordered array of rows, and an empty array is a success. A missing result
     is Unsupported, never an empty success.
   - Reach. The reference run does not call `PerformAsync` until a separate record admits performer queries to
     the executable model.
4. **Compatibility.** No ESM byte, version, DTO, grammar or canonical vector changes. `RequirementId` is already
   serialized on the trigger. Requirement pairs stay (1,1); live performers keep (1,2) under decision 0120.

## Alternatives and default

- *A body combined with declarative effects.* Making it a binding error was rejected because it would break
  models that bind today.
- *Mixed event and command batches in order.* Rejected because the order of a mixed batch is ambiguous and
  invites claims of atomicity.
- *Restricting targets to the reaction's own slice.* Not adopted. Body targets are as wide as declarative
  `produces` and `invokes`, which is reversible.
- *Deferring `PerformAsync` to the admission work.* Rejected so that hosts can implement one stable seam. The
  contract matches the documented Stage performer context.
- *Default.* Without a provider, behaviour is unchanged.

## Verification

Specs under `Source/DotNET/Screenplay/Semantics/Execution/for_SemanticBodyExecution/when_reacting/` and
`when_performing/`, `for_SemanticModelBinder/when_a_reaction_body_declares_reads`, and
`Screenplay.Mcp/for_ScreenplayMcpServer/when_supplying_a_reaction_effect_provider`. Canonical vectors are
unchanged. Package validation passes against the 4.0.0 baseline.

## Related

0005, 0006, 0078, 0082, 0097, 0113, 0117, 0119, 0120; Screenplay#1099.
