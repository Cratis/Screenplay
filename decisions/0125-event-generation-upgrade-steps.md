---
id: 0125
title: Upgrade an event one generation at a time with rename, default and drop steps
status: proposed
stage: none
class: contract
reversibility: costly
decider: direction by woksin on 2026-10-30; detailed contract proposed
amends: 0011, 0015
applies-to:
  - Source/DotNET/Screenplay/Syntax/EventSyntax.cs
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/events.md
---

## Context

[0011](0011-event-generations.md) declares every generation in full and leaves the transformation that maps generation
N−1 to N out of scope. [0015](0015-event-generations-in-the-executable-model.md) item 2 gives each revision its own
property identities and claims no correspondence between same-named properties; item 4 says a target that cannot realize
the evolution rejects it. Stage cannot derive upcasts from same-named properties for that reason (Cratis/Screenplay#71,
comment on Stage#204), so a generation-N event cannot register on a production Chronicle kernel.

Chronicle defines the meaning (0001):

- A migration is adjacent only: `To` must equal `From` + 1, else `InvalidMigrationGenerationGap`
  (Chronicle `EventTypeMigration.cs:56-62`). Each migration has an `Upcast` and a `Downcast` (`:78`, `:84`).
- The property operations are `Split`, `Combine`, `RenamedFrom`, `DefaultValue` and `MapValues`
  (`IEventMigrationPropertyBuilder.cs:24,32,39,46,58`). There is no drop operation: a property is dropped by not being in
  the target schema of a returned object. Unfiltered JSON, not that shaped object, is carried to the next hop
  (`EventTypeMigrations.cs:87-90,227-234`). A removed name reused later can therefore recover an older value.
  This is a source-inspection finding only; runtime reproduction remains unverified.
- A default fills a property only when it is absent from the payload; a present null counts as present
  (`EventTypeMigrations.cs`, default-value loop).
- Registration of generation N>1 needs a migration from every generation 1..N−1, else `MissingEventTypeMigrators`,
  `MissingFirstGenerationForEventType` or `MissingMigrationForEventTypeGeneration`
  (`EventTypeRegistrar.cs:234-251`). A migration's keys and expression sources are checked against the two schemas
  (`EventTypeRegistrar.cs:256-285`).

woksin's direction of 2026-10-30 covered direction only: simple step-by-step event upgrades. Stage#204 renders from the result.
The syntax, derived downcast and admission choices below are proposals, not woksin-approved details. These
records stay drafts until proven; this record does not amend an accepted contract yet.

Evidence revisions: Screenplay and Chronicle `origin/main` on 2026-10-30. The generation grammar and its diagnostics
`PLAY0446`–`PLAY0448` are shipped (`DiagnosticCodes.cs:2114-2120`); `PLAY0449` guards historical references.

## Decision

Proposed detailed contract, subject to proof and acceptance:

1. **Syntax and opt-in.** A generation N>1 declaration may end with one explicit `upgrade` block describing how a
   generation N−1 payload becomes generation N. Generation 1 has none. No upgrade clause means legacy compilation,
   ESM selection and canonical bytes remain unchanged (0011:40-43; 0015:48-56; 0004). Explicit clauses opt into separately
   admitted transformation semantics; only these clauses activate the step rules below. No default flip or major change
   is proposed. A deployment requiring migrations still fails closed downstream when coverage is absent (0015 item 4).

   ```screenplay
   event InvoiceRegistered generation 2
     invoiceId InvoiceId
     customerName String
     currency Currency
     upgrade from generation 1
       customerName renamed from customer
       currency default "NOK"
       drop legacyRef downcast default ""
   ```

   Grammar: `upgrade from generation <N-1>` then one or more lines, each `<target> renamed from <prior>`,
   `<target> default <literal>` or `drop <prior> [downcast default <literal>]`. The number after `from generation` must be
   N−1 and the prior generation must be declared in the document (0011 item 3).
2. **Carried properties.** A property of generation N with the same name and exactly the same type as a property of N−1,
   and named by no step, is carried unchanged. This is the only implicit correspondence, and it matches Chronicle copying
   unmentioned properties. Same name with a different type is an error: changing a type is out of scope.
3. **Accounting.** Every property of N−1 must be carried, the source of a `renamed from`, or named by `drop`. Every
   property of N must be carried, renamed or defaulted. A property that is neither is an error, so data is never lost or
   invented by omission.
4. **Typing.** `renamed from` requires the same type, including concept and its constraints, on both sides. A `default`
   literal must type-check against the target property under the existing literal rules and satisfy the concept's
   rules (for example a `not empty` text rejects `""`, 0071). Defaults are literals only: no expressions, `$context`,
   other properties or code bodies. A prior or target name is used by at most one step.
5. **Downcast is derived, never authored.** Generation N to N−1 inverts each step: a rename renames back; a default
   removes the property; a drop restores the prior property from its `downcast default` literal. `drop` without
   `downcast default` is an error when the dropped property is required, because the downcast could not produce a valid
   N−1 payload. The renderer emits Chronicle's `Upcast` and `Downcast` from the same block.
6. **Rendering.** The step maps to Chronicle `RenamedFrom` and `DefaultValue`. A drop renders to nothing in the upcast
   (the returned object's target schema omits the property) and to `DefaultValue` on the prior property in the downcast.
   **Initial restriction:** admission rejects reuse of any removed property name anywhere later in the complete
   generation chain, whether removed by `drop` or rename. It checks all declared revisions, not just adjacent pairs.
   Chronicle carries unfiltered JSON between hops (`EventTypeMigrations.cs:87-90,227-234`); a name removed in generation
   2 and reused in 3 could expose a generation-1 value. This is not a verified exploit or runtime result: source
   inspection only, runtime reproduction unverified. If the restriction cannot ensure parity, admission stays closed.
7. **ESM.** Only a revision with an explicit upgrade clause carries an ordered list of steps in the separately admitted
   representation of the event aggregate (0015 item 1). Steps reference
   revision-scoped property identities of revision N−1 and N (0015 item 2), so no step relies on name matching. The list
   is part of the canonical bytes. The ESM version is allocated at admission under
   [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md); this record claims none, and no
   document, diagnostic or product string names one before that checkpoint.
8. **Reference replay.** The reference evaluator gets one pure function: migrate a payload from revision g to revision h.
   It applies the upcast steps g→g+1→…→h or the derived downcast steps in reverse, one adjacent step at a time, never a
   shortcut. The result is deterministic and typed. A payload that does not satisfy its source revision's schema is
   refused, not coerced. A rename or carry keeps null as null; a default applies only when the property is absent
   (Chronicle parity).
9. **Fail closed.** Before new admission, binding refuses the explicit upgrade feature only, with a typed unsupported
   diagnostic; legacy generation models remain accepted as before. After admission, an explicit block with malformed or
   non-adjacent steps, an unaccounted property, a type or default mismatch, a duplicate step target, or a non-literal
   default is refused with typed diagnostics. An absent block is not a compilation error and grants no migration
   capability. A replay or deployment requiring a missing hop refuses it downstream, never inventing a transformation.
   A consumer that cannot realize explicit steps refuses their version under 0004. Codes are allocated at implementation.
10. **Immutability is separate.** Refusing edits to a released generation stays with Cratis/Screenplay#609.

**Done when:** the forms parse, print and round-trip in both compilers; every refusal in items 2–4 and 9 is a typed
diagnostic; the ESM carries and strictly reads steps; the evaluator migrates up and down; the generation-2
`ProjectRegistered` corpus vector (0015 item 9) states its drop of `projectId` as a step; Stage#204 renders from the ESM.

**Verify by:** parser, printer, validator and binder specs per refusal; canonical vectors that replay generation 1
payloads to 2 and back, covering rename, default, drop with downcast default, carry, absent versus null; strict-reader
rejection of malformed steps; a consumer check against a Chronicle migration rendered from the same steps. Admission
also requires a multi-hop conformance vector (1→2→3 and reverse) covering removed-name reuse rejection, including rename
sources, and proving allowed chains match Chronicle despite unfiltered intermediate JSON. Compatibility vectors must
also prove legacy generation declarations retain their compilation, ESM selection and exact canonical bytes, while an
explicit clause alone receives the pre-admission refusal. These vectors have not been run.

## Options considered

- **Rename, default and drop steps (proposed).** Mirrors Chronicle's operations and is what Stage#204 needs.
- **Same-name inference for every property.** Rejected: 0015 item 2 claims no correspondence; only identical name and
  type carries, and only because Chronicle does the same.
- **Authored downcast.** Rejected: a second block that can disagree with the first; derivation covers every step.
- **Split, combine and map-values steps.** Deferred, not rejected: Chronicle has them, but they need value rules this
  record does not design.
- **Expressions or code bodies in an upgrade.** Rejected for the first cut; no portable meaning yet.
- **Top-level `migration X 1 => 2` (the #71 sweep proposal).** Not taken: 0011 already declares prior generations in full,
  so the block sits with the generation it produces.

## Default if unanswered

Generation-N events stay unregistrable on a production kernel and Stage#204 stays blocked.

## Timeline and scope

Not in force: detailed acceptance waits for the proof above. Proposed scope: the `upgrade` block, its checks, ESM steps, reference replay, vectors and the
corpus. Out of scope: type changes, nested property paths, split, combine and map-values, historical-shape consumption in
authored behavior (0015 item 3), `[Tombstone]` and `[CompensationFor]`, resolved-schema immutability (#609), Chronicle
deployment conformance (Stage#204) and any Chronicle change.

## Consequences

Authors state each evolution once and a document can be replayed across generations. The cost is an optional block per
explicitly upgraded generation and a separate ESM admission; legacy declarations remain unchanged. If accepted, 0011's "transformation stays open" and 0015 item 2's "no correspondence"
would read through this record.

## Related

Proposes amendments to 0011 and 0015. Builds on 0001, 0004 and 0025. Cratis/Screenplay#71, #609; Stage#204, #168.
