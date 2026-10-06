---
id: 0023
title: Model command productions and allocate their ESM versions
status: accepted
stage: none
decided: 2026-10-02
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
supersedes: 0021
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay.Contexts/Contexts/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

> **2026-10-06 — ESM allocation superseded in part by [decision 0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md).** 0025 replaces the v7–v11 rows of the *ESM allocation* table and the allocation-dependent sentences in *ESM allocation*, *Default if unanswered*, *Timeline and scope* and *Verification*. ESM v7 is allocated to generated values and responses; admission remains subject to accepted 0026 and 0004's gates. Later versions, including exact numbers, are numbered at a serialized release-ready admission checkpoint instead of in a fixed order. The v6 row, numeric-mode independence, cumulative contracts as redefined in 0025 and every other section of this record remain in force.

## Context

A command needs to describe what it records, returns, reads and asks other systems to do. [0021](0021-commands-produce-events-operations-and-responses.md) established that scope, but its named responses, `append` block and interface-based operations no longer match the approved model. [#309](https://github.com/Cratis/Screenplay/issues/309) brings the revised proposals together. This record replaces 0021. It records Einar Ingebrigtsen's idea approvals and Sindre Alstad Wilting's subsequent allocation, compatibility and conservative-default decisions, all made on October 2, 2026. Each section names its decider. Sindre Alstad Wilting's defaults are binding until revisited through an accepted follow-up, except the proposed read-absence rule, which requires an accepted 0017 amendment. Acceptance does not mean implementation.

## Decision

**Decider: Einar Ingebrigtsen — 2026-10-02, idea approvals linked below.** Commands describe events, responses and intent-first operations in technology-neutral terms. Each construct is optional: existing models retain their meaning and canonical bytes. Providers map the model to their platforms, publish capabilities and reject behavior they cannot honor. A provider limitation does not narrow the language. Specifications remain the acceptance criteria, and builds never call AI. Sindre Alstad Wilting's decisions below settle the remaining delivery defaults.

### Events and identity

**Decider: Einar Ingebrigtsen — 2026-10-02.** His [“Affirmative” on #299](https://github.com/Cratis/Screenplay/issues/299#issuecomment-5951068741) approves the issue together with Sindre Alstad Wilting's scope comments on [metadata and reserved fields](https://github.com/Cratis/Screenplay/issues/299#issuecomment-5885607080), [rename-only identity](https://github.com/Cratis/Screenplay/issues/299#issuecomment-5888409211) and [multi-source destinations and inlay hints](https://github.com/Cratis/Screenplay/issues/299#issuecomment-5894374014). [#305's approval](https://github.com/Cratis/Screenplay/issues/305#issuecomment-5951714737) covers the legacy-production repair:

- `produces event <Name>` declares an event with typed mappings. Plain `produces <Name>` references an existing declaration; a typo never declares one. Inline events lower to a standalone event plus a production with the same canonical bytes. Declarations remain order-independent, per-file compilable and printer-invertible; collisions with declared, imported or other inline events are errors.
- An omitted `for` on an inline production means the command's `identifier` only when every production targets the same event source. Once any production targets another source, **all** productions must state `for` explicitly. A diagnostic and typed repair enforce this rule; an inlay hint shows each implicit destination. Lowering supplies the explicit identifier destination.
- **Plain `produces` keeps its legacy exception:** omitting `for` uses an allocated identity, not the command's identifier. It is never silently retargeted. [#305's approved repair](https://github.com/Cratis/Screenplay/issues/305#issuecomment-5951714737) offers an explicit `for <identifier>` when available, with documentation examples checked against that behavior.
- Event source identity belongs in the destination, not duplicated in a same-source payload. `EventSourceIdInPayload` is a warning for inline events and information for declared events. Repairs move identity to the destination for unpersisted contracts or propose generation 2 without it for persisted contracts.
- `id "<old name>"` is **rename-only**: it preserves the identity of an event that already has stored events. New events omit it. A rename repair adds the pin when needed; an information diagnostic and removal repair flag a pin equal to the current name.
- Inline events are generation 1 and have no external `origin`. `generation` is reserved; a typed extraction repair makes an event standalone before it evolves. Inline `tag` belongs to the event type; plain productions retain per-production tags.
- `description` and `documentation` describe declarations without adding ESM bytes. A description accepts one line or a text/Markdown fence; documentation accepts fenced Markdown. Editors, workspace indexes and MCP recognize nested event declarations.

The [undeclared-event repair in #298](https://github.com/Cratis/Screenplay/issues/298) remains useful alongside inline declarations, as #299 states.

**Decider: Sindre Alstad Wilting — 2026-10-02; revisitable defaults:** Inline events remain disallowed in reactions for now; whether to lift that limit now that 0022 admits reactions is open. For #298, infer only known mapping types, preserving concepts, and refuse uncertain, imported, cross-file or conflicting declarations. All repairs follow [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md): typed proposals, revision checks, preview and explicit acceptance, never automatic compiler edits.

### Generated values and responses

**Decider: Einar Ingebrigtsen — 2026-10-02.** Approval of [#300](https://github.com/Cratis/Screenplay/issues/300) is implied by his [approval of #303](https://github.com/Cratis/Screenplay/issues/303#issuecomment-5951185350), which depends on it; his [#300 comment](https://github.com/Cratis/Screenplay/issues/300#issuecomment-5951130617) is a future-scope note, not a separate approval. `generated` applies only to command properties whose type is a concept over `Uuid`. `generated identifier` names a generated event source id; `generated` alone names another generated value. Neither is request input or a form field. Specifications supply identifiers through `when … for` and other generated values through `when … generated`.

A command has at most one unconditional response, sent only on acceptance and cleared on failure. `returns <property>` returns one value; an unnamed `returns` block returns several named fields. [#303's approval](https://github.com/Cratis/Screenplay/issues/303#issuecomment-5951185350) settles its open decisions 1–2:

- Use the `Response` suffix, yielding `<Command>Response`. Renderers favor an official generated type that proxy generators can discover.
- Infer field types from their sources; explicit types are optional.

Forms and interactions bind returned names in `on submit` and `on success`, never `on failure`. Specifications use `then returns`, which cannot accompany an error or denial. Arbitrary values returned by inline implementation code are future scope, as [Einar Ingebrigtsen noted on #300](https://github.com/Cratis/Screenplay/issues/300#issuecomment-5951130617); this decision does not limit that future response channel to the forms above.

**Decider: Sindre Alstad Wilting — 2026-10-02; revisitable defaults for #303.3–4:** Defer collections and whole-read-model responses, including `returns` of a whole view read by the command, for reconsideration with reads.

### Operations and external systems

**Decider: Einar Ingebrigtsen — 2026-10-02.** [#301's approval](https://github.com/Cratis/Screenplay/issues/301#issuecomment-5951238337) settles open decisions 1–2, approves the body's lean toward rejecting operations in reactions in 4, and accepts the proposed operation/attachment shape in 5:

- `system` names an external system. Operations declare `uses <System>`, never provider interface names. System abilities are future scope.
- `produces operation <Name>` declares an inline operation. Authors can promote it to a reusable standalone `operation <Name>` referenced through `produces <Name>`. Event and operation names are unique within a slice.
- Inputs, description and optional compensation express intent. Code is optional in the model; execution targets reject an unimplemented operation instead of generating a stub. Operations use command operation semantics and an implementation attachment role.
- Operations belong to the command transaction; operations in reactions are errors.
- The reference runner checks requested operations by value, including intent-only operations. Success is the default; specifications explicitly request failures with `given operation … fails` and assert compensation with `then compensated`.

**Decider: Sindre Alstad Wilting — 2026-10-02; revisitable defaults for #301.3 and reuse:** Operations run in authored order after events are enrolled and before commit. Providers honor declared compensation according to commit disposition and reject the model if they cannot honor this contract. Standalone operations can be referenced from any slice in the same model or its imports, like other declarations.

### Event sources, streams and concurrency

**Decider: Einar Ingebrigtsen — 2026-10-02.** [#302's approval](https://github.com/Cratis/Screenplay/issues/302#issuecomment-5951569440) chooses named streams instead of 0021's `append` block and settles open decisions 1, 6 and 7:

- Use `eventsource` and nested `stream` declarations, command-level `stream` references, and `from` filters for reactions and reducers. Names must resolve. Command routing covers all its events, including those returned by handler code; explicit per-production overrides are part of the model.
- Keep `concurrency`: it describes what to compare within the routed scope, not where to append. It is **not deprecated**. Omitting it does not mean unchecked appends; the provider's default concurrency behavior still applies.
- For legacy concurrency values, offer a repair that drops a value matching the stream, reject a contradicting value, and offer a repair creating the stream reference when none exists. Providers reject scopes they cannot honor, including unsupported exclusion of `eventSource` or `events …` scopes.
- Deliver declarations and command routing first, then reaction/reducer filters, per-event overrides and finally constraint scopes. Constraint scopes are deferred, not dropped.

Explicit `for` supports atomic multi-source production. Optional per-production `occurred at` is for imports and replays. Namespace, sequence, correlation, causation and caused-by remain system-assigned; a command cannot route to another tenant. Unsupported routing or filtering fails closed.

**Decider: Sindre Alstad Wilting — 2026-10-02; revisitable defaults for #302.2–5:**

- Cross-file declarations follow the existing [import](../Documentation/screenplay/imports.md) and [folder](../Documentation/screenplay/folders.md) rules. Files compiled together share declarations; quoted imports include files, and contract imports retain their existing meaning. No implicit file discovery or order-dependent declaration is added.
- Event-source and stream declarations support rename-only `id` pins. New declarations omit them; a rename preserves the old stored identity, and redundant pins receive an information diagnostic and typed removal repair.
- Serialize the source classification as `SourceKind`, distinct from the identifier's value type.
- Stream ids use a portable, culture-independent subset: text and text-backed concepts pass through unchanged; UUIDs and UUID-backed concepts use lowercase hyphenated canonical form; integer-backed concepts use invariant decimal form. Other non-text values require a later formatting construct and are rejected for now. A provider cannot choose a different conversion.

### Implementation lifecycle

**Decider: Einar Ingebrigtsen — 2026-10-02.** [#307's approval](https://github.com/Cratis/Screenplay/issues/307#issuecomment-5951377649) settles open decision 1 and the committed JSON lock approach in 3, not the unanswered hint-structure and multi-target questions:

- Use `implementation` with hints and an optional `file` reference. Keep existing file and fenced-code forms. The same lifecycle serves operations, provisioning, rules, handlers, reaction effects and query performers.
- A developer or an explicitly requested AI action realizes intent as ordinary team-owned source. An AI action proposes the attachment through a typed workspace edit; acceptance requires the relevant rendered specifications to pass. AI is optional and is never invoked by builds or automatic regeneration.
- Keep fingerprints in a committed JSON lock file outside the ESM. Contract drift fails through compilation of the generated half against the attached implementation, not through the lock. Changed description or hints warn while retaining the implementation. Authors confirm or deliberately reimplement it. Code edits remain ordinary source changes, checked by specifications.

**Decider: Sindre Alstad Wilting — 2026-10-02; revisitable defaults for #307.2–6:**

- Keep `hint` lines free text; structured hints are deferred.
- Commit the lock at the application root as `.screenplay/implementations.json` and document that path. Its versioned JSON schema starts at `schemaVersion: 1`, with an `implementations` array keyed uniquely by stable `requirementId`, the identity of the model construct needing an implementation, independent of its file path. Each entry holds the model's relative `file` link and `contractHash`, `intentHash` and `specificationsHash`: SHA-256 fingerprints of canonical contract, description/hints and relevant specification content. Paths are links, never identity. Entries are ordered by requirement id; malformed locks and unsupported schema versions are rejected.
- Missing or stale lock data warns locally. CI fails on it only when enforcement is explicitly enabled. This does not turn an incompatible contract or unsupported implementation into a warning: those still fail admission or compilation. The model's attachment remains authoritative; a lock never selects different code.
- Confirmation reruns the relevant rendered specifications and updates fingerprints only after they pass. A failed or unsupported test cannot confirm intent. Specification changes change the fingerprint and require review. Nothing regenerates or calls AI automatically.
- Keep one file attachment per requirement. Multi-target attachment selection is deferred: no language/provider fallback or inferred choice is added. A target that cannot use the selected attachment rejects it.

### Reads, derived values and provisioning

**Decider: Einar Ingebrigtsen — 2026-10-02.** [#308's approval and syntax direction](https://github.com/Cratis/Screenplay/issues/308#issuecomment-5951683391) settle open decisions 2, 5 and 6, `advisory` in 3, and new semantic nodes in 7:

- Commands read state on the server; callers do not supply trusted state. Chained read keys remain protected. Protection is the default, and `advisory` explicitly opts out. Providers cannot silently weaken a protected read.
- Use `derive` for command-local computation and `provide` for intent-first provisioning, with `rejects` declaring failure outcomes. Specifications can supply provided values. Reads and derived values enter rule contexts, subject to [0017's admission guarantees](0017-map-declared-decision-reads-to-chronicle-decision-reads.md).
- Order execution as authorization, property rules, reads, derive/provide, read-backed requirements, then productions and responses. Uniqueness belongs in append-time constraints, not a read followed by a requirement.
- Add semantic nodes for reads, derived values and providers only through [0004's admission process](0004-admission-and-governance-of-portable-executable-semantics.md).

**Decider: Einar Ingebrigtsen — 2026-10-02, same approval.** Use the explicit word `optional`, not `?`, for optionality. Keep language vocabulary human-readable and consistent; do not introduce C#/TypeScript notation into the language.

**Decider: Sindre Alstad Wilting — 2026-10-02; compatibility and revisitable #308.3–4 defaults, with a proposal for #308.1:**

- `Type optional` is canonical everywhere, including properties, query results and filters, and `reads Reservation optional as existing`. Existing `Type?`, supported today by [`PropertyLineParser.cs`](../Source/DotNET/Screenplay/Parsing/PropertyLineParser.cs), remains accepted for compatibility, with an information diagnostic and typed repair to `optional`. Printers, documentation, samples and editors use `optional`. The ESM's `IsOptional` does not change.
- **Proposed, dependent on an accepted 0017 amendment:** An optional read exposes absence for `exists`/`no` tests and still protects that absent state unless `advisory`. A missing required read rejects the command before derived values or productions run. Until that amendment is accepted, [0017's existing absence rule](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) remains in force. Its protection, admitted-shape and explicit-opt-in guarantees also remain; legacy documents are not silently promoted, and combined protected `reads` and `concurrency` remains a blocking ambiguity.
- A provider unable to protect a read rejects it unless the model marks it `advisory`. Publishing a capability limitation does not authorize a weaker rendering.
- Defer `via query` reads and reject them at admission. `advisory` alone does not admit an otherwise unsupported query shape.
- Initially, `derive` accepts only value forms and operators already admitted by the portable expression language. [`ExpressionParser.cs`](../Source/DotNET/Screenplay/Parsing/ExpressionParser.cs) currently has no general binary arithmetic: literals and typed value paths can be reused, but the multiplication in #308's example is deferred. Raw expressions are not executable expressions. No new arithmetic, implicit rounding, unit conversion or money/currency behavior is inferred from an example; unsupported computations require an explicit `provide` implementation or a later accepted extension. Projection arithmetic and its rounding rules do not change.

### Specification time

**Decider: Einar Ingebrigtsen — 2026-10-02.** [#304's approval](https://github.com/Cratis/Screenplay/issues/304#issuecomment-5951705711) establishes controlled occurrence time. [0022](https://github.com/Cratis/Screenplay/blob/4fc16226d66b4e8ae8bc222e568c31f8023972ee/decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md) is being implemented as ESM v6 on its own branch, `origin/esm-v6-reactions-clocks-triggers-captures`, using `given clock`; it remains proposed on main.

**Decider: Sindre Alstad Wilting — 2026-10-02.** Follow the newer record: #304's occurrence fixture is delivered by 0022's `given clock`. Do not add a `given time` alias. Remaining #304 work is a regression specification for `$context.occurred` and Stage rendering, with explicit rejection where rendering is unsupported. This record leaves 0022 and its branch-owned implementation unchanged.

### ESM allocation

**Decider: Sindre Alstad Wilting — 2026-10-02**, except v6, which belongs to **Einar Ingebrigtsen's 2026-10-02 decision in 0022 on its own branch**:

| ESM version | Allocation | Decider |
| --- | --- | --- |
| Unchanged | #299 inline lowering, #298 repairs and #305 repair/documentation work | Sindre Alstad Wilting |
| v6 | 0022: reactions, clocks, triggers, captures and occurrence time | Einar Ingebrigtsen |
| v7 | #285: introduce a preamble-selected exact-number mode, recorded in the ESM | Sindre Alstad Wilting |
| v8 | #300 generated values and #303 response blocks together | Sindre Alstad Wilting |
| v9 | #301 operations and any #307 implementation role that changes ESM | Sindre Alstad Wilting |
| v10 | #302 event sources and streams | Sindre Alstad Wilting |
| v11 | #308 reads, derived values and provisioning, including #129 decision reads | Sindre Alstad Wilting |

#307's lifecycle metadata and lock stay outside the ESM. These allocations resolve the former competing v6 claims from #285 and 0022. Versions stay cumulative: each admits the earlier versions' features and ships only after the previous one. If an earlier allocation is withdrawn, renumber later allocations through an accepted follow-up before shipping them.

Numeric mode is separate from feature version. v7 introduces exact numbers as a preamble-selected mode; it does not make them mandatory. Replace [#285's version-only preamble proposal](https://github.com/Cratis/Screenplay/issues/285) with an explicit numeric-mode choice, recorded in the ESM independently of its version. A v8–v11 document keeps Double numbers unless its preamble opts into exact numbers; `schemaVersion: 8`, for example, admits v8 features but does not select a numeric mode. `returns` requires at least v8 in either mode. Declaration-bearing files opt in together; mixed exact/legacy documents fail. Existing models retain their numeric behavior and bytes. Every batch still meets [0004](0004-admission-and-governance-of-portable-executable-semantics.md).

v11 is also the language/ESM allocation [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) requires for [#129 decision reads](https://github.com/Cratis/Screenplay/issues/129), subject to acceptance of the required 0017 amendment for the proposed required/optional absence rule. The allocation does not waive 0017's admission or explicit-opt-in requirements.

## Options considered

- **Optional, explicit productions and named declarations (chosen).** They keep existing models valid and make destinations, responses and external effects readable and checkable.
- **Infer a declaration from the first plain production.** Rejected: typos become contracts, compilation depends on declaration order and command edits can silently change event schemas.
- **Keep 0021's named responses, `append` and interface-based dependencies.** Rejected: response names resemble events, free-form routing repeats unvalidated names, and interfaces tie the model to a provider. Named streams do not replace concurrency scope.
- **Require code while modeling, or regenerate it on every build.** Rejected: the first hides intent and the second makes builds nondeterministic. Intent-only reference specifications do not claim to execute an implementation.
- **Treat idea approval as allocation of every detail.** Rejected: Einar Ingebrigtsen's idea approvals and Sindre Alstad Wilting's subsequent defaults have separate attribution. Conservative initial limits keep unsupported behavior explicit without claiming Einar Ingebrigtsen chose each detail.

## Default if unanswered

Sindre Alstad Wilting's decided defaults apply without waiting for further answers; the proposed absence rule still needs an accepted 0017 amendment. Existing models keep their behavior and bytes. Deferred or unsupported constructs remain rejected at admission; providers do not guess, silently downgrade protection or report unexecuted behavior as passing. Pending answers, inline events stay disallowed in reactions, providers reject capability gaps and existing collection/arrow spelling stays unchanged. The cost is narrower initial expressiveness and target support: some commands need explicit implementations or must wait, and v11 reads cannot ship with the proposed absence rule until 0017 is amended.

## Timeline and scope

This decision governs #309 and its linked work from October 2, 2026 until superseded. The allocation above fixes delivery order; it does not waive strict-reader admission, golden and source-backed conformance vectors, reference execution or typed unsupported outcomes, or consumer tracking under 0004. Optionality spelling preserves existing semantics and ESM bytes. New read behavior requires the allocated v11 admission; the proposed required/optional absence distinction requires an accepted 0017 amendment before that implementation merges.

In scope: command events, generated values, responses, operations, routing, reads, derivation, provisioning, their ESM allocation and numeric-mode compatibility, authoring tools and specifications. System abilities, collection and whole-view responses, constraint scopes and arbitrary inline-code returns are later work. Deployment mechanisms and provider API names do not belong in language syntax.

## Verification

**Done when:** Inline and equivalent explicit productions have identical canonical bytes; legacy plain productions retain their allocated destination. Identity, routing and repair rules are visible in diagnostics and tooling. Semantic batches follow the allocated order and require explicit provider admission. The ESM records numeric mode separately from cumulative feature version; later features do not opt into exact numbers. `optional` prints canonically while `?` retains compatibility and unchanged optionality bytes. #304 uses `given clock` only. Implementation realization and confirmation require passing specifications, locks follow the decided schema and enforcement policy, builds never invoke AI, and existing models retain their meaning and bytes.

**Verify by:** Parser/printer and declaration-order specifications; inline/explicit corpus comparisons; legacy-byte golden comparisons; workspace/MCP repair tests for preview, refusal and stale revisions; response and generated-input tests; operation order, failure and compensation specifications; cross-file visibility, rename continuity, canonical stream-id and unsupported-provider tests; protected-read and `given clock` occurrence vectors. Cover optionality across properties, reads, queries, printers and editors, and numeric-mode recording and exact-number opt-in independently of cumulative feature activation. Include v8–v11 Double and exact-mode vectors, cross-slice operation references, integer-backed stream ids, and rejection of unprotected reads unless explicitly advisory. Test missing/stale locks locally and in opted-in CI, failed confirmation, specification fingerprint drift and unsupported targets. Verify realization against rendered specifications and a build with no AI service. Carry these criteria to implementation issues; documentation checks alone do not prove implementation.

## Consequences

Authors can describe a complete command before choosing its implementation. Typed responses support callers and proxy generation; reusable operations expose side effects to specifications. Providers must reject unsupported behavior and publish their capabilities. Delivery requires coordinated language, tooling and consumer changes, with migrations where existing syntax or contracts differ.

## Open questions

1. **Provider capability publication** (#309): where profiles are published and how editors discover them.
2. **Existing notation audit:** whether and how existing `[]` and `=>` should change. Audit them separately; this record neither adds new C#/TypeScript notation nor silently removes existing spelling.
3. **Inline events in reactions:** whether to lift #299's restriction now that 0022 admits reactions into ESM v6.
