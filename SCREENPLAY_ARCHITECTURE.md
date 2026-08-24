<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay architecture

**Status:** Accepted

**Vision basis:** 2026-08-06

**Decision owner:** Cratis/Screenplay

## Decision

Screenplay is the authoritative, declarative, executable semantic model of the desired functional state of an information system.

- People and AI author it.
- Studio visualizes and edits it.
- A reference evaluator and Stage execute it.
- Specifications verify it.
- Renderers produce code, configuration, tests, schemas, and other approved artifacts.
- Source adapters recover evidence and propose meaning without making code authoritative.

Code is one realization artifact and a means to an end. Stage and generated applications implement Screenplay semantics; neither defines them. The boundary is **portable model semantics versus replaceable realization choices**, not human-readable versus technical.

The first delivery program is backend-only. Existing Screenplay UI and Stage Scene behavior remain compatible. Frontend and deployment generation require separately approved profiles and are not implied by this decision.

## Bidirectional model and code flow

Screenplay supports two complementary directions around one semantic authority:

```text
Screenplay source → ESM → execution plan → artifacts/code
code → source evidence → reviewed semantic proposal → Screenplay source
```

The forward direction renders or executes the desired functional state. The recovery direction observes an existing implementation and proposes the meaning it can prove. Recovery never makes code authoritative, invents missing domain intent, or applies an uncertain proposal without review.

Bidirectional fidelity is measured at the semantic boundary, not by requiring generated code to reproduce identical source text. For an admitted capability, rendering and recovering the generated result must preserve the same semantic identities, contracts, and observable specification outcomes. Realization-only details and any loss or uncertainty remain explicit in the composable realization report.

This makes code useful in both directions: a replaceable realization produced from Screenplay, and evidence that can help create or update Screenplay. It does not turn Screenplay into a general-purpose source-code decompiler.

## Target architecture

```text
.play document set / later workspace
Studio edits
AI semantic patches
accepted Prologue candidates
source-recovery evidence
        │
        ▼
ApplicationSyntax
  compatible source/declaration AST
        │ bind, resolve, validate
        ▼
Executable Semantic Model (ESM)
  language + semantic version
  immutable semantic graph
  stable semantic and event-contract identities
  deterministic revision
        │
        ├──────────────► Studio / AI semantic snapshot
        │
        ├──────────────► portable compiled PDL/CDL plans
        │
        ▼
Portable Execution Plan
  decisions, facts, views, queries,
  reactions, effects, policies and specs
        │
    ┌───┴───────────────┬────────────────┐
    ▼                   ▼                ▼
reference evaluator     Stage            renderers
                                             │
                                             ▼
                                  ArtifactRenderPlan
                                  artifacts + hashes
                                  + typed diagnostics
                                             │
                                             ▼
                                    staged/journaled CLI
                                    artifact publication
```

`ArtifactRenderPlan` is deliberately named to avoid collision with the existing Stage Scene `RenderPlan`, which is preserved.

## Document set now, full workspace later

The initial compiler accepts one document or a document set representing one logical application. It provides:

- deterministic document ordering and compilation;
- document identities and original source spans;
- `ApplicationSyntax → ESM` binding;
- source-to-semantic maps and diagnostics;
- equivalent single-file and folder semantics.

This initial surface does **not** promise a lossless CST, trivia-preserving edits, transactional multi-file changes, or byte-identical workspace round trips.

A later `ScreenplayWorkspace` adds:

- immutable original bytes and source documents;
- lossless CST, comments, formatting, and embedded bodies;
- ownership maps and durable authoring metadata;
- revision-checked transactional edits;
- deterministic flatten, expand, and write plans.

File boundaries are authoring boundaries, not semantic boundaries. Workspace losslessness has three explicit levels: semantic, source, and workspace losslessness. Studio canvas layout is a Studio-owned sidecar and is not application behavior.

## Identity namespaces and durability

Identity domains remain independent:

| Identity                     | Owner and purpose                                                                  | Durable location                                                                                       | Bootstrap and migration limit                                                                                                     |
| ---------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| Screenplay document identity | Screenplay identity for a source document independent of display path              | Screenplay document-set/workspace metadata                                                             | Plain files receive provisional IDs; a path-only history cannot prove a rename                                                    |
| Generation source identity   | Stable project/file/symbol evidence across checkout and display-path changes       | Generation evidence catalog and realization-report source section                                      | Generation mappings cannot become semantic IDs implicitly                                                                         |
| Semantic identity            | Screenplay meaning across rename, move, file split, Studio, AI, and generated code | Screenplay semantic/workspace metadata and serialized ESM                                              | Plain legacy source gets deterministic provisional IDs until metadata is persisted; ambiguous renames require an explicit mapping |
| Event-contract identity      | Persisted-fact compatibility across type/display renames and revisions             | Screenplay event declaration or persisted contract-migration metadata, then ESM and artifact manifests | Legacy derivation is deterministic and persisted once; bootstrap cannot infer historical aliases without evidence                 |
| Studio authoring identity    | Canvas/editor object and layout continuity                                         | Studio-owned authoring/layout sidecar keyed to Screenplay identities                                   | It never defines semantic or event-contract identity                                                                              |
| Prologue candidate identity  | Observation, evidence, review, acceptance, and correction history                  | Prologue candidate store                                                                               | Acceptance proposes a revision-bound semantic patch; the candidate ID is not copied into the semantic model                       |
| Runtime identity             | Domain value used while executing commands, event sources, views, and effects      | Application data and event/runtime context                                                             | It is modeled data, not an authoring, source, or semantic-node ID                                                                 |

Names, paths, Roslyn symbols, candidates, canvas positions, and runtime values must not substitute for semantic identity.

The minimum event contract in ESM v1 contains a stable contract ID and initial revision. Existing event declarations receive a deterministic legacy-derived ID that is persisted on first migration. A type or display rename preserves that ID. Full predecessor graphs, transformations, and revision evolution remain owned by Screenplay issue #71 and are not duplicated by the initial kernel.

Artifact manifests bind semantic, specification, profile, emitter, identity-schema, and artifact-schema versions. Identity or artifact-schema changes require an explicit migration; deleting a manifest and guessing continuity is not a migration.

## Portable execution semantics

The ESM lowers to a versioned execution plan over an explicit world:

```text
World =
  event histories
  keyed read-model state
  pending business deadlines
  capture checkpoints
  pending external-effect intents
  logical clock and deterministic ID allocator
  caller, compliance-subject, event-source and execution-scope context
```

These contexts are distinct:

- **caller:** the principal initiating or authorizing an operation, with roles and claims;
- **compliance subject:** the person or legal subject to whom classified data relates;
- **event source:** the modeled domain identity whose ordered history receives or supplies facts;
- **execution scope:** the explicit tenant, namespace, application, or other isolation boundary for one execution.

Normalized outcomes are:

```text
Accepted(facts, effects, resulting state)
Rejected(category, code, details)
Conflict(category, reconsideration details)
Unsupported(capability, details)
```

No reachable unsupported construct may be silently omitted, approximated, or weakened.

The minimum ESM includes a deterministic evaluator, not only contracts. Its first shared behavioral vector executes a complete command → event → projection → query path and covers both acceptance and validation rejection. Full Stage parity remains later work.

## Specifications

Given/When/Then specifications are executable semantic assets. They support:

- given facts with source identity, scope, time, and ordering;
- given keyed read-model state;
- caller identity, roles, claims, logical time, and deterministic IDs;
- when command, query, occurrence, time passage, or external observation;
- then facts and destinations, view state, query results, effects, deadlines, authorization outcome, rejection, conflict, or unsupported capability;
- exact/subset and ordered/unordered assertions.

The same behavioral vectors run against every surface for which the capability is required: the reference evaluator, Stage, and rendered applications.

## Semantic admission rule

A construct enters Screenplay core only when it:

1. changes portable observable behavior or model promises;
2. is needed for equivalent execution, testing, Studio, or AI reasoning;
3. cannot already be expressed with existing building blocks;
4. has framework-neutral vocabulary;
5. has deterministic semantics;
6. is feasible in the reference evaluator and a renderer;
7. is demonstrated by real model evidence, not diagnostic frequency;
8. fails closed when unsupported;
9. is the smallest missing relationship or qualifier;
10. keeps realization and operations separate.

Every recovered fact has independent purpose and disposition:

- purpose: business meaning, realization, operations, uncertain;
- disposition: represented, report-only, not represented, unresolved, rejected.

Only a confirmed event-model gap can motivate language design.

## Compatibility and semantic versioning

Language syntax version and semantic execution version are explicit and independent. A new binder or evaluator must not silently strengthen an existing construct.

In particular, existing `reads` retains its existing meaning under the legacy semantic version. Decision-consistency semantics—declared decision inputs must remain current and all produced facts commit together—requires an explicit semantic-version opt-in or reviewed migration. Existing `ConcurrencySyntax` is classified as legacy realization-shaped concurrency metadata: it is preserved for compatibility and may be reported, but it does not imply portable decision consistency. A migration may bind a reviewed instance to portable decision inputs; otherwise it remains legacy or blocks a target that cannot preserve it.

Before Program v1 freezes, every current `ApplicationSyntax` construct must appear in a compatibility disposition matrix with its syntax and semantic version, current behavior, target support, migration, and one explicit disposition:

- **bind** to equivalent portable semantics;
- **preserve legacy** without strengthening;
- **report-only** as realization evidence;
- **block** because safe execution/rendering is unavailable;
- **migrate** through an explicit reviewed transform;
- **defer** outside Program v1.

No construct may disappear merely because it is absent from the first ESM subset.

## Admitted portable semantics

### Decision consistency

For the opted-in semantic version, decision inputs state that a command decides from particular state and may commit only while every declared input remains current. A stale decision returns a typed conflict and must be reconsidered from fresh state. No stream, lock, sequence, version, or DCB terminology belongs in the portable model. Aliases distinguish multiple instances of the same view.

### View-driven automation and business due time

Screenplay represents:

```text
facts → view/todo item → automated reaction → command → facts
```

A reaction can operate for each current view item, optionally when its recorded business deadline is due. The model promises outstanding work and terminal facts, not queue or transport exactly-once behavior.

### Affected-view identity

Projection and reducer transitions state which read-model instance or instances an occurrence affects, with deterministic zero/one/many cardinality. Renderers cannot guess keys or collapse fan-out.

### Data-subject relationships

Concept classification says a value is personal. A use-site relationship states lineage—who the value is about—and enables capability checks, for example:

```screenplay
email EmailAddress about customerId
```

`about` does not, by itself, promise universal erasure or export behavior. Erasure, export, retention, anonymization, and legal-basis behavior require separate explicit semantics and conformance vectors. Data subject is also distinct from caller, event source, execution scope, tenancy, ownership, and authorization.

### Queries

A query contract includes result cardinality and optionality, snapshot/live delivery, typed arguments and sources, selection predicates, optional-filter behavior, deterministic ordering and tie-breakers, paging, allowed caller sorting, authorization/scope, and specifications.

C#, SQL, LINQ, MongoDB, EF, Arc, HTTP, SSE, and WebSockets are realizations.

### Policies

Policies are portable authorization decisions over typed caller, artifact, compliance subject, execution scope, claims, roles, and occurrence time. Logical grouping is exact. Unsupported policy semantics block rendering or execution; they never degrade to a weaker policy.

### Event contract evolution

A persisted fact has stable contract identity, immutable revisions, schema, predecessor lineage, and deterministic transformation into the current semantic form. Framework aliases, serializer generations, and upcaster classes are realization details. Beyond the ESM v1 identity and initial revision, this work remains in Screenplay #71.

## No Saga construct

Screenplay will not add Saga grammar, AST, semantic node, slice type, printer, compiler, lowerer, execution node, or Studio card.

Long-running processes use ordinary Event Modeling building blocks:

```text
facts → process/todo view → reaction → command → facts
```

Commands express intent, facts record progress, views represent process state, reactions request next steps, typed concepts correlate, and specifications define legal outcomes. Framework Saga metadata remains realization evidence and may not originate invented domain meaning.

## Constrained implementation bodies and standalone sublanguages

Inline and referenced C#, TypeScript/React, SQL, and future languages are constrained implementation bodies, not second applications. They fill explicit implementation requirements where the portable model cannot deterministically state the realization.

### Small common contract

Screenplay defines only a small language-neutral envelope:

```text
ImplementationRequirement
  stable identity
  owning SemanticId
  role
  context-contract version
  result-contract version
  required capabilities

ImplementationAttachment
  requirement identity
  language identity
  inline or file-backed source
  content revision/hash
  exact source map and provenance
```

Roles are specific—command handler, query performer, rule predicate, policy, reducer rule, reaction, and later screen implementation—because each receives a different context and returns a different result. Screenplay does not invent one universal code AST or a general-purpose portable function language. A target provider translates the versioned role contract into its own function signature, imports, framework APIs, and file layout.

The ESM records the implementation requirement and its observable contract, not raw target code as portable business meaning. Accepted source remains an attachment with its own revision. Artifact manifests bind the semantic revision, target profile, requirement identities, and attachment revisions, so changing code cannot pass unnoticed merely because the declarative semantic revision stayed the same.

### Inline and file forms

Inline and file-backed bodies have identical role, context, result, capability, and validation semantics. Their storage differs:

- inline source is held by the Screenplay workspace;
- a file reference is a portable repository-relative locator resolved by the authoring/rendering host, never by the pure semantic binder;
- file paths are provenance and placement, not semantic or attachment identity;
- after resolution, tooling validates the same virtual body against the same contract.

Tooling creates a virtual document that wraps only the method body with the target provider's generated context/signature. Diagnostics and edits map back to the exact inline block or referenced file. Namespaces, classes, application entry points, unsafe paths, undeclared capabilities, and APIs outside the role contract are rejected. Completion filtering alone is not a sandbox; binder admission, renderer validation, and runtime execution enforce the same contract.

### Deterministic generation and implementation gaps

Rendering is deterministic first. A target renderer produces everything implied by portable semantics and returns typed implementation gaps for requirements it cannot satisfy from accepted attachments:

```text
ArtifactRenderPlan
  deterministic artifacts
  implementation gaps
  blocking diagnostics
  publishable: true | false
```

A missing body, unsupported language, stale attachment, or capability mismatch is a blocking gap. The renderer never emits a successful application containing `TODO`, `NotImplementedException`, empty success, or guessed behavior. Partial artifacts may be previewed, but the CLI cannot publish them as a complete application.

Target integrations are named, versioned, explicitly enabled by the realization profile, and deterministically ordered. They may contribute at declared generation sections or add files, but cannot arbitrarily post-process the model or redefine semantics. Generated files and user-owned attachment files remain separate; the CLI manifest owns only generated artifacts. This follows the generation-gap principle rather than protected regions inside files users edit.

### Optional AI assistance

AI is an optional gap resolver outside the pure renderer. It never runs inside `IArtifactRenderPlanner` and never writes application files directly.

```text
plan → typed gap → AI/user candidate → verify → review/accept → re-plan
```

A candidate is bound to the exact semantic revision, target profile/version, implementation requirement, and base attachment revision. It records provider/model provenance and input/output hashes. Compilation, static analysis, and the requirement's Screenplay specifications verify it. Acceptance is an explicit revision-checked workspace/semantic patch; only accepted source becomes an implementation attachment. Re-running the planner with the accepted attachment is deterministic.

AI may propose a missing target implementation or translate an accepted attachment to another target language. It may not invent domain events, policies, query behavior, or acceptance criteria to make generation succeed. If specifications and the role contract do not constrain a gap sufficiently, the candidate remains unresolved and the model needs clarification.

### Runtime and recovery

Stage executes an implementation body only through a registered provider that supports its language, role-contract version, and capabilities. Otherwise execution returns typed `Unsupported`; it does not interpret arbitrary source opportunistically.

Source recovery follows the inverse boundary. Generated manifests and stable requirement identities allow exact recovery of generated/accepted attachments. Hand-written code contributes located source evidence and a reviewable proposal. Recovery may preserve an opaque body and its role without claiming to understand all behavior. Loss, ambiguity, unsupported APIs, and target-only details remain explicit in the realization report.

The design deliberately applies three established patterns rather than inventing a universal code model: the [Generation Gap](https://martinfowler.com/dslCatalog/generationGap.html) separates generated and authored files; [Smithy codegen integrations](https://smithy.io/2.0/guides/building-codegen/making-codegen-pluggable.html) use named opt-in target hooks; and [VS Code embedded-language tooling](https://code.visualstudio.com/api/language-extensions/embedded-languages) uses virtual documents with explicit source mapping and known cross-language limitations.

PDL and CDL remain independently consumable first-class sublanguages. Their parsers, compilers, and portable compiled-plan contracts must not depend on constructing the full application ESM. The ESM may reference their versioned portable compiled plans. Workbench adoption is gated on standalone package conformance, existing API compatibility, and shared policy/constraint behavioral vectors; it is not an implicit consequence of ESM delivery.

## Realization profiles, adapters, and artifacts

A realization profile selects separately reviewed rosters:

- a **source adapter roster**, which recovers evidence;
- a **renderer target roster**, which produces artifacts.

The rosters are not interchangeable. Vogen belongs only to the source adapter roster and is never a renderer target.

A profile declares satisfied capabilities, target/framework/package choices, implementation attachments, and unsupported semantics. It cannot redefine model meaning. Frontend and deployment are separate optional profiles requiring their own approval and conformance scope.

Stage adds this API without renaming Scene types:

```text
IArtifactRenderPlanner.Plan(
  immutable semantic snapshot,
  immutable execution plan,
  immutable realization profile)
    → ArtifactRenderPlan(artifacts, hashes, typed diagnostics)
```

`IArtifactRenderPlanner` is deterministic, pure, and performs no file, process, network, clock, or ambient-environment I/O. Existing `IRenderer` remains available through a compatibility adapter, is marked for deprecation only after equivalent coverage exists, and is removed only under the normal breaking-change policy.

The CLI plans fully before writing, protects unmanaged or user-modified files, and removes only unchanged stale managed files. Publication uses a staging area and durable journal recording intended operations, prior manifest state, and completion. A crash can be resumed or rolled back deterministically, and the new manifest is published last. This is crash-recoverable staged publication, not a claim that an arbitrary multi-file filesystem update is absolutely atomic.

## Composable realization report

One deterministic report is composed from owned sections rather than assigned to one product:

- **Screenplay anchors:** semantic revision, identity anchors, language/semantic versions, capability requirements, and specification hashes;
- **Generation source evidence:** source identities, provenance, derivation, admission, uncertainty, and conflicts;
- **Stage/renderers:** capability disposition, target identity, artifact findings, hashes, and unsupported diagnostics;
- **CLI envelope:** selected static rosters/profiles, serialization version, invocation-safe paths, publication journal/result, and report assembly.

The report never changes model meaning. Each producer owns its schema fragment and version; the CLI preserves unknown versioned fragments when composing or re-serializing them.

## Product ownership

- **Screenplay:** language, initial document-set compiler, later workspace/CST, ESM, semantic/event-contract IDs, semantic snapshots/patches, execution plan, deterministic evaluator, specification semantics, constrained-language contracts, and Monaco integration.
- **Generation:** source evidence, source identity, atomic source adapters, admission, derivation, provenance, and source/spec/module recovery.
- **Stage:** reference-conformant runtime, specification runner, `IArtifactRenderPlanner`, Cratis renderer, capability/artifact report section, and Docker host.
- **Studio:** visual/collaborative authoring, authoring IDs, layout, workspace persistence, import/export, and semantic patch review.
- **Prologue:** observations, candidate identity, evidence/review history, and accepted-candidate patch proposals.
- **CLI:** trusted orchestration, distinct static source/target rosters, file/folder selection, diagnostics, report envelope/serialization, and crash-recoverable artifact publication.
- **Arc/Critter Stack:** atomic source adapters and realization mappings; never core language meaning.

Screenplay production packages must not depend on other Cratis products.

## Migration and compatibility

- Add document-set, ESM, execution, and report APIs alongside current syntax/visitor APIs.
- Preserve existing parsing, canonical printing, folder compilation, public UI, and Stage Scene compatibility.
- Complete the compatibility disposition matrix before freezing Program v1.
- Migrate Stage and renderers capability by capability; unsupported behavior becomes a blocking typed diagnostic.
- Preserve framework-shaped legacy syntax until a major release and automated migration are available.
- Keep compatibility facades while source recovery moves to atomic adapters.
- Source rewriting and identity/artifact migration are always explicit.
- Preserve reconciled Studio, Prologue, and Stage work; successors integrate it instead of reopening or duplicating it.

## Conformance criteria

The architecture is proven when:

- the frozen Program v1 capability set and semantic version are published;
- every existing `ApplicationSyntax` construct has an explicit compatibility disposition;
- file/folder forms compile to one semantic snapshot;
- the minimum evaluator and shared command → event → projection → query success/rejection vectors pass;
- later no-op workspace round trips preserve bytes and typed patches are revision checked;
- semantic hashes ignore formatting and relocation where identity metadata is unchanged;
- evaluator execution is deterministic under injected time, IDs, caller, scope, and effects;
- required shared vectors cover success, rejection, conflict, unsupported behavior, policies, queries, automation, deadlines, compliance, and evolution;
- Stage, the Cratis backend, and the named TypeScript/Node.js backend target pass every vector required for their declared capability set;
- unsupported semantics never become broader queries, weaker policies, guessed correlation, or omitted behavior;
- report composition is deterministic and does not alter semantic fingerprints;
- artifact publication is deterministic, journaled, crash-recoverable, and safe for user files;
- identity and artifact-schema bootstrap/migration behavior is tested;
- ledger cells are explicitly Required, Passed, Deferred, N/A, or Blocked;
- frontend and deployment remain excluded unless separately approved.

## Rejected alternatives

- `ApplicationSyntax` as universal syntax/semantic/runtime/render DTO;
- Stage, Arc, Chronicle, or a generated framework as semantic authority;
- one competing application model per product;
- language syntax driven directly by adapter diagnostics;
- a Saga construct;
- unrestricted embedded code;
- AI replacement of whole `.play` text;
- permanent IDs derived only from names or paths;
- silently strengthening legacy `reads` or `ConcurrencySyntax`;
- universal erasure behavior inferred from `about`;
- best-effort rendering or unverifiable absolute atomicity claims.
