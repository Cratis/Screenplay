---
id: 0078
title: Opt into atomic reaction-production batches without changing existing models
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/EventModels/**
  - Source/Screenplay/McpApp/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[Screenplay#769](https://github.com/Cratis/Screenplay/issues/769) identifies a portable/runtime mismatch. A reaction's ordered productions append individually in `Source/DotNET/Screenplay/Semantics/Execution/SemanticReactionLoop.cs:183-189,259`; an invocation-refusal branch does the same at `:299-302`. A later rejection leaves the earlier facts and their projections accepted. Commands already materialize and validate their whole collection before one world commit (`SemanticCommandExecution.cs:298-353`).

Chronicle 19.65.2 validates a returned event collection with shared batch claims before storage. Any pre-append rejection prevents the entire collection from being appended (`Source/Kernel/Core/EventSequences/EventSequence.cs:340-402`). Chronicle queues accepted events only after durable append acknowledgment (`:670-719,945-978`); this does not promise atomic observer read-model updates or rollback after unknown infrastructure failures. Arc 22.88.0 appends direct events before invoking commands, and appends a selected refusal branch's events as a separate collection (`Source/DotNET/Chronicle/Reactors/EventsAndCommandsResultHandler.cs:34-42`, `RefusalEvents.cs:24-32`). [Arc#3093](https://github.com/Cratis/Arc/issues/3093) therefore withholds constrained reaction collections with SP0070 rather than emitting a false model.

Decisions 0001 and 0004 require runtime-aligned portable meaning, explicit admission and conformance. Decisions 0022 and 0030 preserve earlier accepted cascade facts and each command's atomicity. Decisions 0004 and 0066 protect existing models and published contracts; 0025 assigns a version at admission, not at design acceptance. Selecting a new version from an existing multi-production shape would change identical source on recompile. Selecting atomicity solely from a higher version would also make a later unrelated feature change old reactions.

Two implementation limits must remain visible: this checkout admits no closing constraint or scoped constraint beyond whole-sequence uniqueness (`Semantics/SemanticConstraints.cs:8-55`), and direct/branch reaction productions have no admitted stream route (`SemanticModelBinder.Reactions.cs:245-300`). The #616 phase-3 work concerns generation, not closure or direct-production routing. Atomicity must not be presented as admission of those missing constructs.

## Decision

An explicit authored application mode opts its reaction productions into atomic batches. Its spelling is a new root preamble, `reaction batches atomic`, and its portable representation is the optional final application member `reactionProductionMode: "atomic"`. One invocation of one reaction trigger appends its direct production list as one batch; a selected invocation-refusal branch appends its own production list as another batch. Invoked commands remain separate atomic command appends. Models without the mode retain their current bytes, revisions and per-fact outcomes regardless of production count or future feature-version selection. The mode is admitted in a new ESM version, numbered only at the release-ready checkpoint under 0025 and frozen under 0066.

The contract has these rules:

- **Selection.** The declaration belongs to the authored model root and applies to its composed application, including imported reactions. Duplicate or conflicting declarations reject. Binding selects the greater of the existing required feature version and the mode's admitting version only when this declaration exists. Absence never opts in automatically, and adding or removing the declaration is an explicit semantic model edit. The spelling and root/import behavior are implemented and pinned by syntax conformance at admission.
- **Default for new models.** Atomic batches are what Chronicle and Arc execute, so producers of new models — Screenplay's new-model scaffolding and samples, and Studio's new-application template and export — declare `reaction batches atomic` by default ([#771](https://github.com/Cratis/Screenplay/issues/771)). Documentation presents per-fact production as legacy behavior kept for byte stability. Existing models are never rewritten automatically.
- **Canonical form.** Write `reactionProductionMode` after the application's existing members, only with value `"atomic"`. Do not serialize a legacy value, null or false. Earlier schemas and programmatic validators reject the member; unknown values reject. Later versions may omit it and then retain legacy semantics. The version gates support; the member selects behavior. No batch identifier, delivery receipt or duplicate produced-event array is added.
- **Frozen contracts.** Preserve v1–v20 and the v21 contract when it lands; no released version is reinterpreted. Unchanged source without the declaration keeps its previous version and bytes. New versions without the member still use legacy append behavior. This record adds dated clarifications to 0022's per-fact production and budget clauses and 0030's ordinary refusal-branch append clause: only opted-in models use the new batch boundary. Preserve their original decision text and all older behavior.
- **Boundary.** A batch is one `Fire(reaction, trigger, occurrence)` call's direct productions, not all matching triggers or reactions for that occurrence. Another clock occurrence, another trigger, each invoked command, and each selected refusal branch is a separate unit. An acknowledge branch appends nothing. A realization must preserve these units rather than aggregate separate triggers into one return collection.
- **Preparation and validation.** Materialize mappings, values, destination/context, tags, occurrence and reaction origin into private ordered candidates. A materialization failure discards them all. Then validate append constraints in authored fact order against the original durable claims plus a private batch accumulator. Only successfully validated events contribute release or owned closure transitions. Keep property-value reservations, event-cycle claims and closure changes distinct; a full-history scratch replay is not an equivalent accumulator.
- **Runtime claims.** Mirror Chronicle's persisted-plus-reserved unique-property checks, including reservations that are not freed by a source changing its value within the batch. Mirror event-cycle release before a later claim. When closing constraints are admitted, a valid close prevents a subsequent covered append in the same batch, an exact owned reopening removes only its own closure, and other/manual closures remain. Resolve constraint scope from each candidate's admitted route; a route does not divide an otherwise single-store/namespace/sequence batch.
- **Current routing limits.** Preserve the existing destination/default-context rules and null direct-production routes; do not infer the triggering fact's stream. Multi-source `for` batches can execute now. Invoked commands retain their admitted per-production routes. Direct stream routing and closure source fixtures require their own admitted contracts before they can be claimed executable; unsupported forms fail closed, never run with guessed defaults.
- **Projection and adoption.** After all append checks pass, compute projections, pending children and derived publications in private state, in primary fact order. Adopt world and projection state only after the entire preparation succeeds. Enqueue accepted primary facts in order, then their derived publication list, following the command collection precedent. Drain reaction cascades only from adopted facts. Publications are derived effects, not silently included in returned-event constraint claims; unsupported publication combinations return Unsupported. Pin publication ordering separately in conformance.
- **Budgets.** Reserve the complete expanded result, including derived publications, against the existing 1,000-new-fact scenario budget before adoption or queue mutation. Given facts do not consume this budget. Exceeding it returns Unsupported and preserves the pre-batch world, read models, pending children and queue. Keep clock-occurrence limits and legacy-mode budgets unchanged.
- **Rejection.** A constraint or admitted closure violation returns the existing Rejected outcome with category Constraint against the exact pre-batch world. Report the first violating production in authored order and, within it, the first constraint in ordinal name order. Preserve existing code, message and string-key semantics. Carry a production index, event contract and route as additive execution diagnostic metadata rather than modifying authored error messages or exposing constrained values. Chronicle's aggregate validator diagnostics need not have identical ordering; do not claim they do.
- **Other failures.** Mapping/shape errors remain Contract rejections; opaque reducers, missing capabilities and projection failures remain Unsupported. None publishes a partial opted-in batch. A direct-batch failure prevents its invocations; a branch append failure is not another invocation refusal and cannot choose another branch. No new outcome kind or infrastructure-recovery promise is introduced.
- **Commands and cascades.** Append the complete direct batch before invoking commands. Each accepted command keeps its own disposition and is observed only after its command batch fits the budget. First matching handled refusal stops the remaining invocations, as in 0030 and Arc. A rejected command contributes no command facts. Later batch, command or cascade failure never undoes previously accepted units, including the initiating append. Preserve the separate public `SemanticReactionSettlement` live-host facade, which returns its input world on cascade failure; do not broaden that facade's behavior here.
- **Assertions and redelivery.** `then error` asserts rejection, not the absence of batch facts. Prove rollback with evaluator world/projection assertions and normalized corpus world counts. `then no events` retains 0074's accepted-zero-outcomes meaning and incompatibility with errors; use it for acknowledgement or filtered triggers. Redelivery runs the named reaction against the existing fact without reappending it, applies ordinary atomic validation again, and adds no deduplication, replay, retry or once-only guarantee.

## Options considered

1. **Explicit application mode with a portable member (chosen).** No unchanged model migrates, the choice survives serialization and source recovery, and future version selection cannot implicitly change reactions. Cost: new authoring syntax, AST/model support and every consuming surface must preserve the mode.
2. **Select from any trigger or branch producing more than one event.** Rejected: that shape already binds. Recompilation changes canonical bytes and can change constraint, projection, malformed-mapping or budget outcomes. Restricting selection to constrained collections still changes existing models and misses other observable failures.
3. **Version-only explicit compiler/API option.** Not recommended: it need not alter current source by default, but the choice can disappear on source recovery; later unrelated features selecting that version would imply atomicity unless another discriminator is introduced. A CLI default or dependency update must not be a migration mechanism.
4. **Per-trigger atomic marker.** A reasonable alternative if mixed modes are a product requirement. Not recommended for this increment: it multiplies boundary, branch and realization rules. The application mode keeps one explicit intent while retaining separate trigger transactions.
5. **Stage a full scratch world using current single-fact append.** Rejected as the validation definition: it can run projections too early and free unique-property claims Chronicle retains during batch validation. Use private projections after append checks and a runtime-aligned claim accumulator instead. Do not change frozen command semantics to fix an adjacent constraint discrepancy.
6. **Make the entire reaction/cascade transactional, including commands.** Rejected: Arc's returned-event batch, individual commands and refusal branch append are separate commits. External effects and prior accepted facts cannot be rolled back by this language decision.
7. **Admit closure and direct stream routing implicitly with atomicity.** Rejected: neither contract exists in the inspected model. They require separately accepted authoring, canonical and execution rules. Keep supported atomic batches independently implementable, but do not claim the complete requested vector matrix while those prerequisites are absent.
8. **Expose Chronicle's entire diagnostic list as the portable first error.** Not recommended: per-event validators run concurrently and follow runtime enumeration, whereas portable assertions need a stable rule. Keep deterministic first-error normalization and a typed fact locator; a portable aggregate diagnostic contract is separate work.

## Default if unanswered

Existing per-fact reference behavior remains. No version, syntax or canonical member is admitted and no source is automatically rewritten. Arc#3093 keeps constrained collections in code with SP0070. Stage and other consumers must not claim a runtime-equivalent declarative batch for these collections. The cost is that constrained batch extraction and render/recover coverage remain blocked rather than becoming silently incorrect.

## Timeline and scope

Settle the explicit opt-in, batch boundary and publication-order choices before implementation. Number only at admission against current main after the preceding admission, including #616, is released; a branch does not reserve a number. This design holds until superseded and adds no numbered version claim.

In scope: the explicit mode, direct and refusal-branch batch units, staged append validation, private projection state, budgets, typed first-error reporting, no-event/redelivery interactions, old-model stability, canonical and source-backed vectors, all in-repository authoring surfaces, and explicit consumer admission planning.

Out of scope: changing published models, changing the existing command contract, capture-wide transactions, adding closure or direct-route grammar by implication, protected reaction reads, cross-store/namespace transactions, observer snapshot atomicity, storage-provider crash guarantees, durable external deduplication and automatic recovery.

Scope split (accepted): #769 ships atomic batches for constructs admitted today (multi-source `for` productions, refusal-branch batches, uniqueness constraints). Closing-then-appending and routed direct-production vectors are tracked in [#770](https://github.com/Cratis/Screenplay/issues/770) and land when their constructs are admitted; until then those forms fail closed.

## Verification

**Done when:** explicit opted-in source binds to a newly admitted version with the canonical mode member; unchanged source without it keeps its historical bytes, revisions and outcomes, including existing multi-production models and the prior admission's vectors; earlier readers reject the mode and later models without it remain legacy; a later rejected production leaves zero batch facts and unchanged projections/pending children, while earlier accepted units remain; successful batches publish and cascade only after adoption in pinned order; expanded fact budgets reject before any adoption; refusal branches form independent batches and stop invocations correctly; redelivery adds no original fact; all in-repository surfaces preserve or explicitly refuse the mode; the closing and direct-routing cases are tracked in #770; each affected consumer explicitly admits the new contract or rejects it.

**Verify by:** later implementation runs binder, parser/printer/import round-trip, AST transport, strict-reader and programmatic-validator specifications in both compilers; evaluator and specification-runner cases for later-event rejection, inter/intra-batch uniqueness, same-source repetition, property-change reservation, release cycles, multi-source batches, refusal-branch rejection/success/acknowledgement, redelivery, clock/application triggers, publications and budget boundaries; low-level world/read-model/pending-child assertions on rejected results; new golden and `AtomicReactionBatchesCorpus` vectors with equivalent single/folder/reordered/relocated sources, exact ESM bytes/revision, outcomes/categories/world counts/routes; prior-version golden/corpus comparisons; the version-table check; MCP/CLI/editor/sample checks; Stage render/run/recover and Arc extraction conformance; Chronicle 19.65.2 or a deliberately reviewed newer runtime oracle for constraint, closure and route cases. Generated vectors are regenerated and reviewed, never hand-edited. The repository's full CI-equivalent gate applies to the implementation.

## Consequences

An opted-in model can express the returned-event transaction that Chronicle actually executes, without losing a valid prefix on a later rejection. Authors can distinguish a refused command from a failed branch append and reason about redelivery without claiming exactly-once effects.

The explicit mode costs language/model plumbing and requires consumers to preserve it through render and recovery. A new version alone is not sufficient evidence of atomic intent. Legacy models may still differ from the runtime, so Arc must keep SP0070 unless it proves equivalence or emits the explicit mode.

Private append claims and private projections are separate responsibilities. Current command/history-replay differences are not silently repaired under this issue. Closure and direct stream routing remain visible dependencies; all-or-none append acceptance does not manufacture either capability.

Consumer work must be tracked when implementation is authorized: Cratis/AI first (reaction, automation, consistency, specification, authoring and toolchain guidance), Arc#3093, Stage, Studio, cli, Screenplay.Generation, Prologue when its syntax emitter is affected, Screenplay.CritterStack and Chronicle conformance. Targets that cannot realize the mode reject it. Consumer issues are filed with the implementation.

## Status notes

**2026-10-15 — accepted.** Accepted by Sindre Alstad Wilting (delegated through the coordinator) with all six recommended choices: the application-level `reaction batches atomic` mode serialized as `reactionProductionMode: "atomic"` (absent keeps per-fact behavior); one trigger occurrence's direct productions and each refusal branch as separate batches, with invoked commands separate; a Chronicle-style append accumulator in the new mode only; shipping for constructs admitted today with closure and routed vectors as follow-ups (#770); first violated fact in authored order, then constraint-name order, with a typed locator; primary facts then publications, projections adopted all or none, rollback proven with world counts. Addition: new models declare the mode by default (#771). The ESM version is assigned at admission under 0025, after #616 phase 3.
