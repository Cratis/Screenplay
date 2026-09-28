---
id: 0017
title: Map declared decision reads to Chronicle decision reads
status: accepted
stage: none
decided: 2026-09-28
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Syntax/CommandSyntax.cs
  - Source/DotNET/Screenplay/Semantics/Versions.cs
---

## Context

A command's `reads` declarations name the state it uses to decide. [Decision 0003](0003-decision-consistency-for-command-reads.md) chose event-source-keyed views, an explicit language-version migration and aliases, but proposed using the view's last-handled watermark as a concurrency boundary and treating any absent view as expecting no matching event. That runtime mapping is wrong after removal and cannot merge two reads of one key soundly: their last matching positions may differ. [Chronicle decision reads](https://github.com/Cratis/Chronicle/issues/4249) shipped in v19.9.0 with an opaque decision token whose boundary is the unfiltered event-log tail taken before folding the view. Screenplay must use that runtime meaning under [decision 0001](0001-chronicle-runtime-semantic-authority.md), while naming the dependency portably.

Chronicle's capability replaces the architectural precondition in 0003, not the remaining deployment work. Arc's [#2828](https://github.com/Cratis/Arc/issues/2828) realization is local and unreleased, and owner-safe rollback ([Chronicle#4292](https://github.com/Cratis/Chronicle/issues/4292)) is still local. Screenplay's binder still rejects legacy `reads`/`concurrency` meaning; neither [#129](https://github.com/Cratis/Screenplay/issues/129) nor [#209](https://github.com/Cratis/Screenplay/issues/209) is complete.

## Decision

Under an explicitly opted-in **new language version**, each `reads <View> [as <alias>] [by <property>]` declaration becomes a protected decision read of that view at the value of `property`, or at the command's declared identifier (the event-source key) if `by` is omitted. This deliberately reinterprets omitted `by` **only in the new version**: the existing [`commands.md`](../Documentation/screenplay/commands.md) and `ReadsSyntax` XML documentation describe it as an unkeyed, application-wide shared view in legacy syntax. Legacy documents retain that meaning; they are not silently promoted. In the new version, an omitted-`by` protected read without a declared command identifier must refuse with a diagnostic, never substitute a fresh or random UUID. An explicit `by` property can supply the key even when the command has no declared identifier, subject to the identity-key and routing admission rules below. A singleton/unkeyed view cannot be protected by this mapping and must refuse, whether or not `by` is written. The declared view instance is available for the decision; an absent instance is a valid state input, not permission to skip protection. An implementation obtains the runtime's protected read and enrolls its opaque dependency in the command transaction before deciding. It must not expose or synthesize sequence numbers in Screenplay.

For a supported view and key, the dependency guards the event log for that key against all event types the view handles, **including removal events**. The runtime captures the **unfiltered event-log tail `b` before folding** the instance. The filtered tail for the key and handled types is only a completeness diagnostic, never the concurrency boundary. No events folded means absent, even if default state exists; folding a removal can also mean absent. Only an **empty entire event log** uses `BeforeFirst`; an absent instance in a nonempty log uses the actual pre-fold boundary. Reads at the same key merge their handled-type sets and use the earliest pre-fold boundary. A change in either set after that boundary rejects the command; an irrelevant change need not. A fold extending beyond the boundary may yield a safe conflict rather than an unsafe acceptance.

Admission is fail-closed. Bind-time checks establish the *declared shape*, not that runtime definitions or deployment state are current: require an event-source-identity-keyed view over the event log with a finite, nonempty set of handled event types, including removals. Refuse reducers, joins (including removal joins), child/nested scopes, open/all-event subscriptions, derivatives and event-property routing. A finite `every` mapping is admissible only when the runtime limits it to the view's declared event types; it must not turn the guarded set into an open-ended subscription. Routing must resolve to the raw event-source string; refuse converting key types except plain strings, unformatted string concepts and canonical GUID keys. GUID keys require lowercase `D` form and the assumption that **all writers stored those event-source IDs canonically**; other casing or formats in existing history fall outside the guarantee. Runtime admission also refuses invalid keys and incompatible projection definitions. A definition-agreement check and a filtered-tail fold probe can detect drift or missed events but **do not certify** that executing and client definitions agree. Unsupported shapes fail with a reason; they do not fall back to an unguarded read.

All tokens must target the same event store, namespace and event sequence as the commit. A protected command has one owner for completion: application code cannot commit its protected transaction early, and an owner rollback must not allow a later unguarded immediate append. A successful protected command returning no facts still validates its dependencies; a rejected command does not commit or validate them. A stale dependency produces a `concurrencyViolation` response (HTTP 400 in the Arc realization), without sequence numbers and without appending the returned facts. The runtime does **not automatically retry the command**: the caller must re-read and deliberately resubmit.

The new language/ESM version is **not allocated by this record or chosen by the implementer**. Before the implementation merges, fix its number in a dated amendment to this accepted record or an accepted follow-up decision, with evidence for [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md)'s admission gate (runtime meaning, canonical form and golden vector, reference execution or typed unsupported outcome, source-backed conformance vectors, fail-closed behavior and per-model version activation). Preserve legacy documents and their existing `reads`/`concurrency` behavior without silent promotion. A command declaring both protected `reads` and `concurrency` receives a blocking ambiguity diagnostic. Preserve the 0003 alias rule: `reads <View> [as <alias>] [by <prop>]`, with an alias required for a second read of the same view. ESM v4 is assigned to event lineage, v5 is held for [#284](https://github.com/Cratis/Screenplay/issues/284), and v6 is proposed for [#285](https://github.com/Cratis/Screenplay/issues/285); none is allocated here for decision reads.

## Options considered

- **Use Chronicle's protected decision read (taken):** guards both presence and absence with the pre-fold event-log boundary and combines same-key reads without equating last matching positions.
- **Use 0003's view watermark or an absence-specific expectation:** not taken; removal leaves a matching event, and two views at one key need not share a last matching position.
- **Silently reuse legacy `reads` or promote `concurrency`:** not taken; changes the meaning of existing documents without an explicit version choice.
- **Admit every view and approximate its dependencies:** not taken; joins, children and nonidentity keys cannot be guarded exactly by the supported keyed event-type scope.

## Default if unanswered

The current language continues to reject binding of legacy `reads` and `concurrency`; `require` on read-model paths remains unsupported. No command acquires protection merely by spelling `reads` in an existing document.

## Timeline and scope

This decision supersedes **only** 0003's item 1 runtime mapping (including its watermark and absence rule). Its item 2 capability precondition has been **architecturally replaced** by shipped Chronicle decision reads, but Arc deployment and Screenplay integration remain pending. Items 3 (versioned migration and both-declared ambiguity) and 4 (alias grammar) remain in force.

The Arc realization requires explicit `[ProtectedDecision]` command mode, invocation-scoped read caching and enrollment, owner-safe rollback and refusal of **every discoverable validator** in protected commands, including parameterless validators, until [Arc#2831](https://github.com/Cratis/Arc/issues/2831) provides validator attestation. Therefore [#209](https://github.com/Cratis/Screenplay/issues/209)'s `require` over declared reads through validators is deferred, not implicitly allowed or skipped. Protected handler/`Provide` paths are the initial realization, contingent on Arc#2828 and Chronicle#4292 shipping. These framework names describe the implementation seam, not Screenplay syntax or ESM vocabulary.

Outside this guarantee: revise/redact (history changes without moving the matching tail), event-generation migrations, definition drift or cross-silo definition convergence, reads from other event sequences, clocks/external services and immediate external effects. The guard assumes the executing definition matches the client's and that Chronicle serializes and validates appends; duplicate activations, foreign writers or unresolved earlier storage outcomes can reach a collision-renumber retry without revalidation. An unrelated external side effect cannot be rolled back by a rejected command. No blanket claim of exactly-once execution or automatic retry follows from this record.

## Verification

**Done when:** A new explicitly selected language version binds admitted `reads` to protected dependencies without exposing sequence numbers; old versions remain unchanged; mixed `reads`/`concurrency` and missing aliases fail; unsupported shapes and incompatible keys refuse with reasons. The rendered target enrolls reads before decision, prevents unguarded completion after rollback, validates successful no-fact commands, and reports stale reads as `concurrencyViolation` without facts or numbers. `require` over declared reads remains deferred while protected validators are refused.

**Verify by:** Parser/binder and version-migration specs for aliases, conflicting declarations and every admitted/refused shape. Include vectors showing that legacy omitted-`by` syntax retains its unkeyed/shared-view meaning and is not promoted to protected binding; in the new version, omitted `by` with a declared identifier uses that identifier's event-source key, omitted `by` without one refuses instead of guarding a generated UUID, and a singleton/unkeyed view refuses. Cover explicit `by` without a command identifier when its property meets the key admission rules. Include runtime reference vectors for never-created absence in an empty and a nonempty log, absence after removal followed by recreation, present-state race, cross-source guarded read, fold past the captured boundary, irrelevant event changes, same-key union/earliest-boundary merge, canonical versus noncanonical GUID keys, no-fact validation, owner rollback and rejected commands. Check that invalid projection definitions or incomplete folds refuse, but do not describe diagnostic checks as certification. Confirm Arc's protected mode refuses discoverable validators before construction, even parameterless ones; run the integration vectors against the released Chronicle/Arc versions before calling #129 runtime work complete. Check grammar against Chronicle's pinned Screenplay package as in decision 0001.

## Consequences

Protected reads have one portable dependency meaning, including absence, without a view-watermark API. Only a restricted subset of views can participate, and deployments must coordinate definition changes, migrations and other writers. When the versioned syntax semantics ship, update the legacy omitted-`by` descriptions in `Documentation/screenplay/commands.md` and the `ReadsSyntax` XML documentation to distinguish both versions; this decision does not change current public guidance or runtime behavior. Versioned adoption and the Arc release/validator boundary delay full #129 and #209 delivery instead of silently weakening their guarantee.

## Related issues

Screenplay: [#129](https://github.com/Cratis/Screenplay/issues/129), [#209](https://github.com/Cratis/Screenplay/issues/209), [#284](https://github.com/Cratis/Screenplay/issues/284), [#285](https://github.com/Cratis/Screenplay/issues/285). Chronicle: [#4249](https://github.com/Cratis/Chronicle/issues/4249), [#4292](https://github.com/Cratis/Chronicle/issues/4292). Arc: [#2828](https://github.com/Cratis/Arc/issues/2828), [#2831](https://github.com/Cratis/Arc/issues/2831). Decisions: [0001](0001-chronicle-runtime-semantic-authority.md), [0003](0003-decision-consistency-for-command-reads.md).

## Status notes

**2026-09-28 — accepted, not implemented.** Sindre Alstad Wilting delegated the choice to the maintainer's orchestrating agent. Chronicle's client decision reads shipped in v19.9.0. Arc#2828 and Chronicle#4292 are local, not released; Screenplay's version gate, binding and rendering are not implemented. Validator attestation remains Arc#2831. Do not close #129 or #209 on this documentation decision.
