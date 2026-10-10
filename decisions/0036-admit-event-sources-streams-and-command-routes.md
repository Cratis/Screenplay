---
id: 0036
title: Admit event sources, streams and command routes into the executable model, with specification routes and composite stream ids
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

**Authoring works; binding refuses.** Since v4.62.0, these parse, validate and print, and appear in the editors and MCP:
- `eventsource Name`, with `identifier`;
- nested `stream Name`, with an optional `streamId`;
- the command route `stream Source.Stream`, with its `streamId` mapping.

Decision [0031](0031-event-source-and-stream-in-specifications.md) added the same route lines to specification events, plus `no stream`. Decision [0033](0033-composite-event-stream-ids.md) added composite stream ids made of named parts.

The binder refuses all of them with PLAY0268 (`Semantics/SemanticModelBinder.CommandProductions.cs:42-55`). No executable model, specification or renderer sees a route, so every rendered append uses Chronicle's defaults: `Default`, `All`, `"Default"`.

**What the runtime provides** (Chronicle 19.32.0, Arc 22.50.5).
- **Chronicle.** An appended `EventForEventSourceId` carries `EventSourceType`, `EventStreamType` and `EventStreamId`. An empty stream id is stored as `"Default"` (`EventSequence.cs:406-407,773-781`). Duplicate source names, and duplicate stream names under one source, are refused (`EventSources.cs:30-36,86-90`).
- **Arc.** `[EventSource<TSource>(stream)]` on a command puts the source and stream into the command context. The stream id comes from `ICanProvideEventStreamId` or `[EventStreamId]`.
- **When Arc computes the stream id.** `BuildContextValues` runs at `CommandPipeline.cs:481`, before command filters and argument resolution. So Arc's own ordering puts stream id computation ahead of authorization and validation.

**What has to happen first.** [#407](https://github.com/Cratis/Screenplay/issues/407) lays out the design and leaves choices a–g to the decider. Decision [0004](0004-admission-and-governance-of-portable-executable-semantics.md) requires an accepted admission record before a new ESM version. Decision [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) numbers a version when its release-ready claim merges. This record therefore names the feature, **event routes**, not a version number.

## Decision

**What is admitted.** One new ESM version, the "event routes" version, admits:
- application-level event sources and their streams;
- one command-level route with its scalar stream id.

**Two conditional parts.** 0031's specification routes and 0033's composite stream ids join the same version **if their evidence is ready at the claim** (see Delivery).
- Every rule below for fixture routes, `unrouted`, `streamIdParts`, the composite encoder and the assignment matcher applies only to the parts that join.
- A part that misses the claim stays refused with PLAY0268 and is admitted in a later version under this record's rules.

**Which models select it.** A model selects this version only when it declares a source, routes a command or states a specification route. Every other model keeps its version, bytes, revision and outcomes.

**What a route never does.** A route classifies every event its command produces. It never supplies `for`, and never changes allocation, authorization, validation, constraints, projections, reducers, reactions or queries.

### ESM shape

**Application `eventSources`.** Written after `triggers`, only when non-empty, sorted by id.
- A source has `id`, `name`, `sourceKind`, an optional `identifierType`, and `streams`.
- A stream (sorted by id) has `id`, `name`, `streamKind`, and exactly one of:
  - nothing (unkeyed);
  - `streamIdType` (scalar);
  - `streamIdParts: [{ "name": …, "type": … }]` (composite: two or more parts, in declaration order).

**Command `route`.** The command's last member, after `response`:

```json
"route": { "source": "<source id>", "stream": "<stream id>", "streamId": <expression> }
"route": { "source": "<source id>", "stream": "<stream id>", "streamIdParts": [{ "part": "projectId", "value": <expression> }, …] }
```

- **Keyed stream:** carries exactly one of `streamId` or `streamIdParts`, matching the stream's shape. Composite parts are written in declaration order and cover every part exactly once.
- **Unkeyed stream:** carries neither.

**Specification fixtures.** A given, when-append or then fixture may carry `route` as its last member. Literals use the existing semantic value object (`{ "kind": …, "value": … }`), as fixture `values` do:

```json
"route": { "source": "<source id>", "stream": "<stream id>", "streamId": { "kind": "string", "value": "2026-10" } }
"route": { "source": "<source id>", "stream": "<stream id>", "streamIdParts": [{ "part": "projectId", "value": { "kind": "string", "value": "3fa85f64-…" } }, …] }
```

A command route's `streamId` and part `value` use the existing expression encoding (a property reference or a literal expression). A fact's route is shown below.
- A `then` fixture may instead carry `"unrouted": true`, the ESM spelling of `no stream`. It is true-only, `then`-only, and omitted otherwise.
- `route` and `unrouted` are mutually exclusive.
- A fixture with neither asserts nothing about its route, as 0031 decides.

**Facts and normalized outcomes** carry `route` as their last member: `{ "sourceKind", "streamKind", "streamId"? }`.
- `streamId` is the canonical encoded string: 0023's scalar rules for a scalar stream, 0033's encoding for a composite one.
- It is mandatory for a keyed stream and omitted for an unkeyed one.
- Consumers recover parts only from the uniquely resolved stream declaration, never by guessing from the string.

**Strict reader and model validator.** The strict reader accepts these members only at this version's `schemaVersion`. It keeps rejecting unknown later versions, as today. It also rejects:
- null, empty or mixed forms;
- a stream id on an unkeyed stream;
- a missing part or an extra part;
- `unrouted` outside `then`;
- a route naming a foreign stream.

The model validator enforces the same rules for models built in code (`InvalidSemanticContract`).

### Choices from #407

| | Choice | Decision |
|---|---|---|
| a | Stored name | **One member.** `sourceKind`/`streamKind` hold the `id` pin if present, otherwise the name. |
| b | Identity | **Catalog addresses**, with new `SemanticKind`s appended after the existing ones. The catalog stays at schema v1. Its bytes are unchanged for models without sources, and older catalog readers reject the new kinds. |
| c | Descriptions | **Report only** (PLAY0270). |
| d | Routed command whose identifier or production destination type differs from the source's identifier type | **Refuse.** See "Authoring changes" below. |
| e | Unrouted facts | **No route.** Chronicle's defaults are never materialized. |
| f | Route assertions in specifications | **Superseded by 0031**, whose spelling is admitted here. |
| g | Integer stream ids in Double mode | **Invariant decimal within ±(2^53−1), lowered losslessly** (see k). The bound applies only in Double mode. Exact-number mode decides its own bound when it is admitted. |

### Further choices

**h. Source-only routes are not admitted.** There is no syntax for them.

**i. What a route mapping may read.**
- A direct, required, non-collection, non-generated command property of the declared type, or a literal. The same applies to each composite part.
- Property paths, which authoring and 0033 accept, are refused with PLAY0268 in this version.
- Generated properties are refused with PLAY0273, because the route is resolved before generation (j).

This direct-member rule is a choice of portable subset. It is not an Arc limit: `ICanProvideEventStreamId` can compute anything.

**j. Phase and failure.**

*Phase.*
- The stream id is resolved once per accepted command, after declarative validation and requirements (0026's step 4), and before generation and productions.
- A reaction-invoked command resolves its route in the same phase.
- This is the **portable** phase. Arc computes the stream id earlier than authorization, so Arc's ordering is not equivalent.
- A renderer must preserve this precedence. For example, it can defer formatting failures until after validation, or validate the inputs the stream id reads before formatting.
- Stage's conformance tests pin a command that is both unauthorized and carries an unformattable stream id. Authorization must win.

*Failure.*
- **Existing contract failures stay where they are.** A non-NFC or ill-formed direct input value is already `Rejected(Contract)` at request type validation (0026's step 3, `SemanticEvaluator.cs:71-73`), before declarative rules. That precedence does not change.
- **Formatting failures come later.** Failures only the formatter can detect are `Rejected(Contract)` at the route phase, after step 4, for the failing command. Examples: empty text, an integer outside the Double bound, a malformed composite.
- **Facts carry the encoded id.** Example: `"route": { "sourceKind": "Project", "streamKind": "Ledger", "streamId": "3fa85f64-…|2026-10" }`.
- The failing command allocates nothing, appends nothing and has no response.
- Facts already accepted earlier in the cascade are kept, as 0026 defines for later failures.
- An invalid **literal** is already refused by authoring diagnostics: PLAY0504 for routes, PLAY0549 for specifications. Binding adds PLAY0273 only where authoring cannot know the problem, such as a pin collision or a generated source.

**k. Lossless values.**
- **Integers.** Route-bearing integer values, both literals and command inputs that feed a route, are lowered without passing through a `decimal` or `double` that rounds. Today `SemanticModelBinder.Expressions.cs:39-46` uses `Convert.ToDecimal`, and that is wrong for 16-digit keys. Adjacent values near both bounds must survive the whole path: source, ESM, execution, encoded id.
- **Text.** ESM text is NFC (`SemanticValueValidator.cs:233-235`, `CanonicalJson.cs:63-93`). Stream id text, scalar or a composite part, is therefore **required** to be NFC in this version. A non-NFC literal is refused at authoring, and a non-NFC run-time value is `Rejected(Contract)`. Nothing is normalized silently.
- **Amendment to 0033.** This narrows 0033's text domain: 0033 said normalization-equivalent strings stay distinct. Decomposed text cannot be expressed at all, which keeps identities distinct without ever rewriting one. 0033 gets a status note.

**l. One formatter.**
- `SemanticStreamIdFormatter` implements 0023's scalar rules and 0033's composite encoder and strict decoder.
- A TypeScript twin in the compiler package uses the same rules.
- Both are held to `Compiler/Conformance/stream-id-codec.json`. The composite rules are exercised by vectors now; they are admitted for execution only when 0033's part joins. It holds 0033's vector list, the ±(2^53−1) bounds with adjacent values, and NFC refusals.
- The authoring validators in both compilers switch to it.

**m. Reference runner.**
- Routed `given` and `when append` facts are placed with their route.
- `then` compares the route when one is stated, and requires an unrouted fact when `unrouted` is stated.
- **Any-order matching is version-gated.** At this version and later, `then events in any order` uses assignment (bipartite) matching, as 0031 requires. Below it, the greedy matcher stays, so the outcomes of released versions don't change.
- **Event source typing follows 0031.** Routed fixtures are typed by the named source's identifier type, with 0031's fallback to a single unambiguous destination type across producers. That applies in the binder, the model validator, the strict reader and world establishment. It replaces deriving the type from event producers, which refuses history with no producer or a differently typed one.
- Supplied fixture routes are validated against the declarations, and formatted canonically, before projections observe them.

**n. Stored names and the unrouted triple.**
- **Unique stored names.** Stored source names are unique application-wide, and stored stream names are unique within their source. Comparison is ordinal and includes pin-versus-name collisions. Binding reports PLAY0273; programmatic and read contracts report `InvalidSemanticContract`.
- **Reserved source name.** A source whose stored name is `Default` is refused with PLAY0273. Otherwise an explicit route could produce Chronicle's unrouted triple (`Default`, `All`, `"Default"`), and 0031's reverse mapping would read it as unrouted.

**o. Catalog identity versus stored identity.** A source or stream keeps its catalog identity across renames and moves.
- **Stored name.** An unpinned rename still changes `sourceKind`/`streamKind`, which is the stored name. The docs state this, and recommend pinning `id` before renaming a source that has stored events.
- **Tooling.** Once sources are admitted, `propose-rename` migrates catalog entries atomically, including a source's owned streams, the same way it does for other admitted declarations.

### Authoring changes (not version-gated)

These change authoring diagnostics for every model. They are compatible with released ESM versions, because routed models never bound before. Both compilers change together, with parity vectors.
- PLAY0504 becomes an **error** for a routed command whose identifier or production destination type differs from the source's identifier type. Unrouted commands are unaffected.
- Stream id literals are checked by the shared formatter: the Double bound, NFC, and lone surrogates.

### Delivery

- **One batch.** Everything this record admits lands on `main` in one merge, from `batch/esm-event-routes`. Sub-pull requests merge into the batch under feature names. The claim pull request renames feature identifiers to the version number, regenerates the bytes and adds the "claimed, unreleased" table row. This is the fixed release contract chosen here: 0025 permits byte-preserving extensions of a released version, but routes are not one.
- **0031 and 0033 join on readiness.** Their executable halves join this version if their evidence is ready at the claim. If either isn't, it is admitted later, and this version is never held back, as their records say.
- **What may merge to `main` early.** The formatter (l) and the authoring changes may merge to `main` earlier, because they change no ESM bytes or outcomes. The version-gated matcher (m) merges with the batch.

### Not admitted

These stay refused:
- per-production route overrides, and per-production `occurred at`;
- reaction and reducer `from` filters, and route inheritance;
- constraint scopes and first-append rules (#407's comment);
- new concurrency flags, and legacy `concurrency` (PLAY0271);
- handler commands (PLAY0268);
- Chronicle stream completion;
- route lines in specification examples (#491). Under [0037](0037-routes-in-redelivery-locators-and-specification-examples.md) they expand in the front end, so they become executable wherever specification routes are admitted. No separate admission is needed.
- redelivery locators by stream (#490). Redelivery stays refused until 0030's own admission requirements are met, even after specification routes are admitted.

Captures and a reaction's direct productions remain unrouted.

## Options considered

- **Arc's ordering (stream id before authorization).** Rejected. A stream id failure would mask an authorization denial and leak which inputs are well formed. The portable phase puts it after validation, and renderers must preserve that.
- **An unconditional any-order matcher fix.** Rejected: it changes released outcomes.
- **Admitting non-NFC text by changing ESM text rules.** Rejected: it would widen the whole text contract for one feature.
- **Mirroring Chronicle's `""` → `"Default"`.** Rejected: it aliases two keys.
- **Stream ids from generated values.** Rejected: the route is resolved before generation.
- **Separate name and stored-name members.** Rejected: every consumer would reimplement the fallback.
- **Admitting property paths now.** Deferred: they add a nullability case the runner would have to define.
- **Joining released v7.** Rejected: routes are not a byte-preserving extension.

## Default if unanswered

- Routes stay authoring-only.
- Stage keeps rendering every append on Chronicle's default stream.
- Specifications cannot seed or assert stream placement.
- #433's redelivery admission stays blocked behind #490.

## Timeline and scope

**Order.** Accept this record first. Merge the formatter and the authoring changes to `main` once they are ready. Build the rest on `batch/esm-event-routes`, and release it with the claim pull request.

**Consumers.** Before the claim:
- Stage, the CLI, Studio and Screenplay.Generation each have a tracking issue for adopting or refusing the version.
- The Cratis/AI corpus has an issue.
- A Chronicle conformance issue is linked to Cratis/Chronicle#4130.

Until each consumer admits the version, it refuses it explicitly.

## Verification

**Done when** decision 0004's evidence exists for this version:

1. **Runtime meaning.** Specs cite the Chronicle and Arc sources above. The Chronicle conformance issue exists. Stage pins the precedence case from j.
2. **Canonical form.** A golden on top of v7 covers:
   - keyed and unkeyed streams, and composite streams (if 0033's part joins);
   - pinned sources and streams, and a source without an identifier type;
   - UUID-, text- and integer-keyed routes, with integers adjacent to both bounds;
   - a literal stream id, and a command with both `response` and `route`;
   - given, when-append and then fixture routes, and `unrouted` (if 0031's part joins).
3. **Reference execution.**
   - routed facts;
   - a reaction-invoked routed command, and a reaction's direct production staying unrouted;
   - `Rejected(Contract)` for each formatting failure, with cascade facts kept;
   - precedence against authorization, validation and requirements;
   - routed `given` history with no producer, and with a conflicting producer (if 0031's part joins);
   - `then` route, stream-id and `unrouted` comparison (if 0031's part joins);
   - a composite `then` that differs in one part, and one that passes with a UUID in another case (if both 0031's and 0033's parts join);
   - an any-order case where greedy matching fails but an assignment exists, passing only at this version (if 0031's part joins).

   Items marked "if … joins" are required only for a part that joins. A part that misses the claim needs a corpus vector showing it is still refused with PLAY0268.
4. **Corpus.** Single-file, folder, reordered and relocated forms, with routes in outcomes. Rejection vectors for:
   - PLAY0268: paths and overrides;
   - PLAY0271;
   - PLAY0273: generated sources, stored-name collisions, the reserved `Default` source;
   - PLAY0504.
5. **Fails closed.** The strict reader and validator reject every misplaced, pre-version, mixed-shape or foreign-stream member.
6. **Activated only when used.** The v1–v7 goldens, corpora and **normalized outcomes** are identical, and the version-table check passes.
7. **Identity.** Renames with and without a pin, a source rename carrying its streams with catalog migration, and relocation.
8. **MCP.** Export round-trips, and readiness reports routes as admitted.
9. **Shared vectors.** The formatter vectors pass in C# on net8, net9 and net10, and in TypeScript. End-to-end vectors cover source to encoded id, for integers near the bounds and for NFC refusal.
10. **Release.** The release note names the version, the authoring changes, and the consumers that still refuse it.

## Consequences

- **Easier:**
  - Stage can render Chronicle event sources and Arc routes.
  - Specifications can seed and assert stream placement.
  - Composite stream ids become executable.
  - #433 gets the routes #490 needs.
- **Harder:**
  - Another ESM member family to keep byte-stable.
  - Renderers must implement the formatter and the precedence exactly.
  - The batch branch must be kept current with `main` until the claim.
  - Stream id text must be NFC.
- **Forecloses:**
  - a separate stored-name member;
  - materialized default routes;
  - stream ids from generated values;
  - a source stored as `Default`.

## Verdict

The decider delegated this verdict to the orchestrating agent. The choices above were made under that delegation on 2026-10-08, after an independent cross-provider critique by GPT-6 Astra. The critique answered "accept with changes", with five blocking findings; all are folded in:
- the portable phase is stated without claiming Arc equivalence, and Stage must preserve it;
- the any-order matcher is version-gated;
- stream id text must be NFC;
- integers are lowered losslessly;
- the `Default` source name is reserved.

1. **Scope?** Sources, streams and scalar command routes in one version, named by feature until the claim. Specification routes and composite ids join it if they're ready at the claim, and are admitted later under these rules if not.
2. **#407 a–g?** As in the table: one stored-name member, catalog identity, report-only descriptions, refusing mismatched routed identifiers, no materialized defaults, 0031's spelling, Double bound with lossless lowering.
3. **Mappings?** Direct, required, non-generated properties or literals. Paths are refused for now.
4. **Phase?** After validation and requirements, before generation. Failure is atomic for the failing command.
5. **Delivery?** One batch branch and one claim. The formatter and authoring changes may merge earlier. 0031 and 0033 join only when ready.

## Status notes

**2026-10-09.** Event routes claim ESM v8 at the release-ready admission checkpoint under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md). The executable halves of [0031](0031-event-source-and-stream-in-specifications.md) and [0033](0033-composite-event-stream-ids.md) joined v8.

**2026-10-09.** ESM v8 was released in Screenplay 4.101.0 (PR #566).

**2026-10-09.** [0053](0053-per-production-route-overrides.md), [0054](0054-observer-source-and-stream-filters.md) and [0055](0055-admit-production-routes-and-observer-filters-as-esm-v10.md) propose the next #302 increment: complete per-production route replacement and observer filters as ESM v10. Reaction direct-production routes, occurred-at routing, concurrency flags, constraint scopes and stream closing remain outside this increment. This note does not change the accepted v8 contract.
