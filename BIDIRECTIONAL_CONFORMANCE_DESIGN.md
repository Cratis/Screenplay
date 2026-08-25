<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Bidirectional conformance design

**Issue:** Screenplay #148

**Status:** Design only — **unimplemented and blocked**

**Decision owner:** Cratis/Screenplay

## Blocked status

Nothing in this document is implemented by the presence of this design. No current release may claim bidirectional conformance from it.

The first executable conformance pair is blocked on all of the following:

1. Generation specification recovery that retains complete success events, exact event-predicate values, read-model assertions, query/read steps, and all three supported rejection shapes;
2. Generation's deterministic realization-report fragments and report composition contract;
3. shared source placement and atomic adapter admission needed to rebind recovered evidence without guessing;
4. an explicitly approved, versioned CritterStack renderer whose generated realization is target-matched to the CritterStack source adapters; and
5. a generated-specification result reporter that emits the normalized outcomes defined here.

The released Stage Cratis renderer proves the forward `ESM → ArtifactRenderPlan` contract. It is not, by itself, evidence that a target-matched CritterStack render/recovery pair exists. A general Cratis target must not be silently treated as the CritterStack target.

## Purpose

Screenplay is the semantic authority in both directions:

```text
Screenplay source → ESM → target artifacts
implementation source → evidence → reviewed Screenplay proposal
```

Issue #148 proves that these directions meet at portable meaning. It does not require recovered `.play` text or generated source formatting to be byte-identical. For each admitted bidirectional capability, conformance compares:

- stable semantic identities;
- stable event-contract identities and revisions;
- portable semantic contracts and capability requirements;
- stable specification identities;
- normalized success and rejection outcomes; and
- explicit semantic loss, ambiguity, conflict, or unsupported behavior.

Target-only realization details are measured separately and never alter ESM meaning or its semantic revision.

## Non-goals

This increment does not:

- make code authoritative;
- decompile arbitrary code into invented domain meaning;
- automatically apply recovered evidence to Screenplay;
- require recovered source formatting, comments, file layout, or generated code style to match;
- admit frontend or deployment round trips;
- merge the renderer-target roster with the source-adapter roster;
- use a sidecar as a copy of the input ESM; or
- claim that two unmatched framework adapters form a conformance pair.

## Current contract baseline

### Screenplay semantic contracts

The current executable semantic model has these relevant guarantees:

- canonical ESM JSON uses schema `cratis.screenplay.esm`, schema version `1`;
- `ExecutableSemanticModel` carries language version, semantic version, semantic revision, and the immutable application graph;
- the semantic revision is computed over canonical semantic content without the revision field;
- every semantic declaration has a `SemanticId`;
- an event declaration has both its declaration `SemanticId` and a distinct persisted `EventContractId` plus `EventContractRevision`;
- specification identities are `SemanticSpecification.Id`, not source-adapter scenario subjects;
- specification values are attached to stable target-property identities;
- a rejection is exactly one of bare, message-only, or code-and-message;
- a rejection cannot coexist with success outcomes;
- a success has at least one expected event, read-model state, or query result; and
- the reference evaluator emits `Accepted`, `Rejected`, `Conflict`, or `Unsupported`, with non-accepted outcomes leaving the world unchanged.

The canonical ESM serializer remains the authority for ESM bytes. This design does not introduce a second ESM serializer.

### Stage rendering contracts

The released Stage rendering contract provides:

- `ArtifactRenderProfile` with exact target, target version, renderer, renderer version, and hashed resolved inputs;
- `ArtifactRenderRequest` with the ESM, matching execution plan, profile, and semantic scope;
- `ArtifactRenderPlan` schema version `1`;
- normalized relative paths, normalized text bytes, artifact SHA-256 values, deterministic ordering, and typed diagnostics;
- a semantic revision anchor on every render plan; and
- a pure planner that performs no publication.

`ArtifactRenderPlan.Success` means that no render diagnostic is an error. It does not mean that artifacts build, generated specifications pass, source recovery succeeds, or semantic conformance has been proven.

Stage does not currently emit the trusted rendered-bindings sidecar defined below.

### Generation recovery contracts

Generation currently separates adapter identity, source subject identity, evidence strength, neutral facts, diagnostics, scenario identity, ordered specification steps, and typed values. Those are necessary inputs, but they are not yet the complete conformance contract.

In particular, a `SubjectId` or `SpecificationScenarioKey` identifies recovered source. It must never replace the corresponding Screenplay `SemanticId`. The trusted sidecar bridges generated source subjects back to Screenplay identities; the adapters still have to recover the meaning independently.

## Conformance unit: a target-matched pair

A bidirectional conformance result is valid only for one reviewed `ConformancePair`:

```text
ConformancePair =
  target id + exact target profile version
  + renderer id + exact renderer version + exact renderer inputs
  + source roster id + exact roster version
  + ordered adapter ids and exact adapter versions
  + build/test driver id and exact version
```

The pair registry is static and reviewed. A vector names a registered pair; it cannot supply executable build commands, load arbitrary adapters, or invent a renderer/adapter association.

The renderer-target roster and source-adapter roster remain separate products and separate approval surfaces. Pairing is an explicit conformance assertion between one entry from each roster, not roster unification.

A pair is target-matched only when all of these are true:

1. the renderer emits the exact framework and source idioms the adapters claim to inspect;
2. renderer and adapters agree on target profile and package-major contract;
3. the pair has an approved build/test driver for that target;
4. the pair declares which portable capabilities are recoverable;
5. the pair emits and consumes the same rendered-bindings sidecar version; and
6. a mismatched target, renderer, roster, adapter, or version fails before source analysis.

The first intended pair is a CritterStack backend renderer with the CritterStack recovery roster. It remains **blocked until that renderer is approved and versioned**. Existing Vogen recovery can participate in the source roster where required, but Vogen is never a renderer target and runs at most once.

## Canonical conformance vector schema

Vectors are checked-in, synthetic, reviewable JSON documents. The schema name is:

```text
cratis.screenplay.bidirectional-conformance-vector
```

Schema version `1` has this logical shape:

```json
{
  "schema": "cratis.screenplay.bidirectional-conformance-vector",
  "schemaVersion": 1,
  "id": "register-project-success-and-rejection",
  "capabilities": [
    "command.validation.notEmpty",
    "command.produces.event",
    "projection.one",
    "query.snapshot.zeroOrOne",
    "specification.success",
    "specification.rejection.message"
  ],
  "source": {
    "esm": "input.esm.json",
    "esmSha256": "<lowercase-sha256>",
    "identityCatalog": "identity-catalog.json",
    "identityCatalogSha256": "<lowercase-sha256>"
  },
  "pair": {
    "id": "critter-stack-dotnet-v1",
    "target": "critter-stack",
    "targetVersion": "<exact-version>",
    "renderer": "<approved-renderer-id>",
    "rendererVersion": "<exact-version>",
    "sourceRoster": "critter-stack-dotnet",
    "sourceRosterVersion": "<exact-version>",
    "adapters": [
      {
        "id": "<adapter-id>",
        "version": "<exact-version>"
      }
    ],
    "driver": "critter-stack-dotnet",
    "driverVersion": "<exact-version>"
  },
  "scope": {
    "kind": "application",
    "semanticId": "sem1:<64-lowercase-hex>"
  },
  "expected": {
    "snapshot": "expected.snapshot.json",
    "snapshotSha256": "<lowercase-sha256>",
    "allowedRealizationLossCodes": []
  }
}
```

The physical files may be embedded resources, but logical names and bytes are fixed by the vector manifest. Paths are vector-root-relative, slash-separated, and cannot be absolute, empty, dot-segmented, or case-colliding.

### Vector canonicalization

Vector and snapshot JSON use a dedicated canonical writer with:

- UTF-8 without a byte-order mark;
- no insignificant whitespace;
- well-formed Unicode normalized to NFC;
- fixed schema property order;
- lowercase SHA-256 text;
- ordinal ordering for set-like arrays;
- preserved authored order where order is semantic, including produced events and specification steps; and
- strict rejection of unknown enum values, duplicate keys, duplicate identities, non-canonical numbers, malformed hashes, and unsupported schema versions.

The canonical ESM and identity catalog remain byte-for-byte outputs of their existing Screenplay serializers. The conformance writer references and hashes those bytes rather than reserializing their contents.

A vector contains no shell command. Build and test behavior comes from the reviewed driver named by the pair.

## Canonical semantic snapshot schema

The snapshot schema name is:

```text
cratis.screenplay.semantic-conformance-snapshot
```

Schema version `1` records only portable comparison material:

```json
{
  "schema": "cratis.screenplay.semantic-conformance-snapshot",
  "schemaVersion": 1,
  "languageVersion": "1",
  "semanticVersion": "1",
  "semanticRevision": "rev1:<64-lowercase-hex>",
  "scope": {
    "kind": "application",
    "semanticId": "sem1:<64-lowercase-hex>"
  },
  "capabilityRequirements": [
    "command.validation.notEmpty"
  ],
  "semanticIdentities": [
    {
      "id": "sem1:<64-lowercase-hex>",
      "kind": "command",
      "contractSha256": "<lowercase-sha256>"
    }
  ],
  "eventContracts": [
    {
      "semanticId": "sem1:<64-lowercase-hex>",
      "contractId": "evt1:<64-lowercase-hex>",
      "revision": 1,
      "shapeSha256": "<lowercase-sha256>"
    }
  ],
  "specifications": [
    {
      "semanticId": "sem1:<64-lowercase-hex>",
      "definitionSha256": "<lowercase-sha256>",
      "referenceOutcome": {
        "kind": "accepted",
        "facts": [],
        "readModels": [],
        "queries": []
      }
    }
  ]
}
```

`contractSha256`, `shapeSha256`, and `definitionSha256` are hashes of versioned canonical fragments produced by Screenplay. They are not hashes of target source text.

The snapshot includes every semantic identity reachable from the vector scope and required capabilities. Comparison is not name-based. Display names remain inside the canonical contract fragment and therefore still participate in semantic equality where the ESM says they are semantic.

### Identity comparison

The comparator applies these rules:

- every expected `SemanticId` must occur exactly once in the recovered snapshot with the same semantic kind;
- no recovered identity may be substituted from a name, namespace, file path, source position, or Generation `SubjectId`;
- every event compares declaration `SemanticId`, `EventContractId`, `EventContractRevision`, and canonical shape;
- every specification compares `SemanticSpecification.Id` and its complete canonical Given/When/Then definition;
- property references and values compare by stable property `SemanticId`;
- a missing, duplicated, guessed, or conflicting identity is semantic loss and fails closed; and
- the recovered semantic revision must equal the rendered semantic revision after rebinding the complete vector scope.

A source-adapter subject can change when generated file placement changes without changing semantic identity. Such a subject change belongs in the realization report, not the semantic snapshot.

## Normalized specification outcomes

The reference evaluator and the rendered target must emit the same target-neutral outcome schema. A passing test-framework exit code alone is insufficient.

### Accepted outcome

An accepted outcome contains:

- `kind: accepted`;
- produced facts in occurrence order;
- each fact's event declaration `SemanticId`, `EventContractId`, destination value, and values keyed by property `SemanticId`;
- resulting asserted read-model instances keyed by read-model `SemanticId` plus canonical runtime key;
- requested query results keyed by query `SemanticId` plus canonical argument; and
- rows in deterministic comparison order.

Read-model collections that are semantically sets are sorted by read-model identity and canonical key. Event occurrence order and authored query result order are preserved where the current semantic contract makes order observable.

### Rejected outcome

A rejected outcome contains:

- `kind: rejected`;
- the portable rejection category when available;
- assertion shape: `bare`, `message`, or `codeAndMessage`;
- code and message only when asserted by that shape; and
- `worldUnchanged: true`.

A message-only rejection cannot be normalized to bare. A code-and-message rejection cannot be normalized to message-only. Additional unasserted framework exception text is realization data and must not be promoted into semantic equality.

`Conflict` and `Unsupported` are harness failures for a vector that expects accepted or rejected behavior. They are never normalized into rejection merely to make a comparison pass.

## Trusted rendered-bindings sidecar

Each conforming renderer emits one managed text artifact named:

```text
.cratis-screenplay-bindings.json
```

Its schema is `cratis.screenplay.rendered-bindings`, version `1`. It is separate from the CLI ownership manifest `.cratis-render.json`.

The sidecar contains:

- the exact semantic revision;
- target, target version, renderer, and renderer version;
- source-roster compatibility id and version declared by the approved pair;
- the hash of the non-sidecar planned artifact set;
- each managed source artifact's normalized relative path and planned SHA-256;
- target subject/symbol anchors and optional generated source spans;
- the bound `SemanticId` and semantic kind;
- `EventContractId` and revision for event bindings; and
- specification `SemanticId` for generated specification bindings.

The sidecar deliberately does **not** contain:

- a serialized ESM or identity catalog;
- semantic property shapes or values;
- specification Given/When/Then steps or expected outcomes;
- recovered facts;
- source file content;
- physical checkout paths; or
- secrets, environment values, caller data, or runtime production data.

This prevents a tautological pass in which recovery simply reads the original model from renderer metadata. Adapters must recover contracts and behavior from the generated implementation. The sidecar is authoritative only for identity continuity and generated-source provenance.

### Sidecar trust rules

The harness trusts a binding only when:

1. the sidecar is a planned artifact from the approved renderer in the current render plan;
2. schema and pair versions are supported;
3. target, renderer, profile, source-roster compatibility, and semantic revision exactly match the vector;
4. the non-sidecar artifact-set hash matches the plan;
5. the bound artifact still has its planned hash after build;
6. the source anchor resolves exactly once inside that artifact; and
7. the binding kind is compatible with the recovered source fact.

Build outputs under `bin`, `obj`, generated caches, and test-results folders are not binding inputs. A build that rewrites a managed source artifact invalidates its binding.

A missing, stale, edited, duplicated, malformed, or mismatched sidecar is never repaired heuristically. Recovery may continue as ordinary hand-written-source evidence, but exact generated identity conformance fails unless another approved exact identity source exists.

The sidecar is input, not proof. It cannot turn heuristic source evidence into exact semantic evidence, hide an adapter conflict, or suppress an unsupported diagnostic.

## Render-build-test-recover-rebind-rerender pipeline

The conformance harness executes each vector in a fresh isolated workspace:

1. **Load** — validate canonical vector, ESM, identity catalog, expected snapshot, hashes, pair registration, and privacy policy.
2. **Compile** — compile or read the canonical ESM, verify its revision, compile the portable execution plan, and reject unsupported required capabilities.
3. **Reference** — run every vector specification through the reference evaluator and write normalized reference outcomes.
4. **Render** — call the approved pure Stage planner with the exact pair profile and application scope. Require a publishable deterministic plan and the sidecar.
5. **Materialize** — publish only planned artifacts into the isolated workspace using safe managed publication semantics.
6. **Build** — invoke the pair's static build driver with pinned toolchain/package inputs. Require zero errors and zero warnings.
7. **Test** — run generated specifications. Require passing tests and a canonical normalized-outcome file for every expected specification identity.
8. **Recover** — analyze the fixed post-build source snapshot with only the pair's ordered source roster. Consume trusted bindings under the rules above, resolve facts, and admit the complete application atomically.
9. **Report** — compose Screenplay, Generation, renderer, and harness fragments. Preserve semantic loss separately from realization-only loss.
10. **Rebind** — lower the admitted recovered graph to a reviewable Screenplay proposal, apply trusted identity assignments in memory, and compile it through the normal Screenplay binder. Do not mutate authored Screenplay.
11. **Compare** — compare rendered and recovered canonical semantic snapshots plus reference and target normalized outcomes.
12. **Rerender** — render the rebound ESM through the same exact pair profile. Require the same semantic revision and deterministic artifact paths, hashes, bytes, diagnostics, and sidecar bytes.
13. **Finalize** — emit a deterministic conformance verdict and report. Remove the isolated workspace without publishing recovered proposals.

Rerender equality is a determinism gate for this generated-source vector. It is not a general promise that arbitrary hand-written code can be reformatted into byte-identical generated code.

## Semantic loss and realization-only loss

The report has two disjoint collections.

### Semantic conformance findings

These affect the verdict and include:

- missing, changed, duplicated, or guessed semantic identities;
- missing or changed event-contract identities, revisions, or shapes;
- omitted or changed command, validation, event, projection, query, or specification meaning;
- changed accepted or rejected outcomes;
- unsupported required capabilities;
- ambiguous or conflicting recovery evidence; and
- partial scenario or partial application admission.

Any semantic error fails the vector. No allowlist can turn semantic loss into realization-only loss.

### Realization findings

These do not alter ESM or semantic revision and include only target choices such as:

- namespace and file placement;
- generated type/member spelling that remains bound to the same semantic identity;
- framework attributes and registration glue;
- target package and toolchain versions already fixed by the profile;
- generated test class names; and
- source-subject identifiers and source spans.

Every realization finding has a stable producer-owned code, severity, evidence/provenance reference, and owner. A vector may allow specific non-error realization codes. Unknown, ambiguous, or error realization findings still fail the gate; they are not silently ignored.

Realization report fields never participate in semantic revision computation.

## Privacy and data minimization

Canonical vectors use synthetic values only. They must not be copied from customer repositories, production event logs, telemetry, user identities, or support bundles.

The harness and schemas enforce these rules:

- no absolute paths, home-directory names, machine names, repository credentials, environment values, or network endpoints in golden files;
- only normalized vector-relative or project-relative logical paths;
- no raw source text in snapshots or the composed public report;
- no build log in canonical output; diagnostics use stable codes and sanitized logical locations;
- no hashing of real low-entropy personal or secret values as a substitute for redaction;
- deterministic synthetic identifiers, names, dates, and messages in all first vectors;
- no network access during vector execution after dependencies are restored into the approved isolated cache;
- private detailed evidence, when needed locally, is a separate non-golden artifact with explicit retention policy; and
- sidecars are provenance metadata, not secret stores, and are minimized to identity bindings and hashes.

A privacy scan is a release gate. A vector that leaks a physical path or sentinel secret must fail before comparison.

## First green corpus

The first green corpus reuses the bounded RegisterProject behavior already exercised by Screenplay and Stage, but only after the blocked dependencies are complete. It contains:

1. **Register project success** — `ProjectId`, `ProjectName`, `RegisterProject`, `ProjectRegistered`, explicit destination, exact mappings, one-instance `ProjectSummary` projection, optional snapshot `ProjectById`, success event/read-model/query assertions, and stable identities.
2. **Empty name rejection** — `not empty` validation and exact message-only rejection, with unchanged world.
3. **Rejection shape controls** — separate bare and code-and-message vectors once the approved target can render and recover both without degradation.

Each capability is split into the smallest useful vector when a combined fixture would make a failure ambiguous.

## First red vectors

The first implementation must land red vectors before the matching green claim. Each red vector asserts the exact failing phase and stable diagnostic code.

1. renderer target and recovery roster do not form a registered pair;
2. target, renderer, adapter, driver, or schema version differs from the registered pair;
3. rendered-bindings sidecar is missing;
4. sidecar semantic revision or artifact-set hash is stale;
5. a managed source artifact changes after rendering;
6. a sidecar binding points outside its artifact, resolves twice, or names the wrong semantic kind;
7. recovery regenerates a `SemanticId` from a name or source position instead of preserving it;
8. event declaration `SemanticId` survives but `EventContractId` or revision changes;
9. event property shape or produced-event order changes;
10. success recovery drops an event predicate value;
11. success recovery drops a read-model assertion or query/read step;
12. message-only rejection degrades to bare, or code-and-message degrades to message-only;
13. one specification scenario is admitted partially;
14. two adapters contribute conflicting facts for one required subject;
15. an unsupported required capability is omitted rather than reported;
16. target-only namespace or placement data is incorrectly added to ESM and changes semantic revision;
17. a physical checkout path enters canonical output; and
18. a sentinel secret or personal value enters a vector, sidecar, snapshot, diagnostic, or public report.

A realization-only control also proves the opposite boundary: changing an allowlisted generated file placement while preserving trusted bindings and portable meaning leaves the semantic snapshot unchanged but produces a realization finding.

## Ownership

| Contract or work | Owner |
| --- | --- |
| Vector and snapshot schemas, canonicalization, semantic comparator, identity rules, normalized portable outcomes | Screenplay |
| Canonical ESM, identity catalog, semantic fragments, reference outcomes, and specification hashes | Screenplay |
| Pure renderer, target profile, artifact hashes/diagnostics, rendered-bindings emission, and rerender determinism | Stage and the approved target renderer |
| Source identities, adapter facts, trusted-binding consumption, derivation, atomic admission, recovered proposal, uncertainty, conflicts, and report fragments | Generation |
| CritterStack framework discovery and target-specific atomic source adapter behavior | Screenplay.CritterStack |
| Static pair registry, isolated build/test drivers, safe materialization, result envelope, and report assembly | CLI/conformance harness |
| Renderer approval and declared CritterStack target profile | Stage plus Screenplay.CritterStack maintainers, recorded explicitly before implementation |
| Visual review or application of a recovered proposal | Studio, in a later increment |

No producer may write another producer's report fields. Unknown versioned report fragments are preserved by the composing host.

## Dependency order

Implementation proceeds in this order:

1. **Screenplay #148 contracts** — freeze vector, snapshot, normalized outcome, comparator, and neutral rendered-binding schemas with red parser/canonicalization vectors.
2. **Generation #26** — release shared source structure/placement and complete downstream adapter adoption needed for deterministic source subjects.
3. **Generation #25** — release complete generated-style success/event-predicate/read-model/query recovery and all rejection shapes with atomic fail-closed admission.
4. **Generation #24** — release deterministic evidence, provenance, derivation, admission, uncertainty, conflict, and realization-report fragments.
5. **CritterStack #44 and adapter work** — release the atomic CritterStack source roster against public Generation packages.
6. **Approved CritterStack renderer** — approve, implement, version, and release the matching Stage target renderer and sidecar emission. Do not substitute the existing generic Cratis renderer without an explicit target-profile decision and matching vectors.
7. **Outcome reporter** — make generated specifications emit canonical normalized outcomes keyed by Screenplay specification identity.
8. **Harness integration** — register the exact pair and implement the isolated render-build-test-recover-rebind-rerender driver.
9. **Red corpus** — land and pass every required failure vector.
10. **First green corpus** — land RegisterProject success/rejection vectors and publish the first scoped conformance result.
11. **Capability expansion** — add one shared vector per newly admitted bidirectional capability before changing its ledger cell to passed.

A later TypeScript/Node.js backend uses the same schemas but a different registered pair. Its success is required for the final portability program, not for the first CritterStack pair.

## Gates

### Contract gates

- canonical vector and snapshot round trips preserve exact bytes across supported target frameworks;
- malformed, non-canonical, duplicate, unknown-version, traversal, and case-collision inputs fail closed;
- snapshot fragments cover every identity reachable from the declared scope and capabilities;
- comparator diagnostics are stable and deterministic; and
- the sidecar cannot carry semantic payload or specification outcomes.

### Pair and render gates

- the exact renderer/adapter/driver pair is approved and registered;
- all versions and renderer inputs are exact and hashed;
- repeated Stage plans have identical paths, hashes, bytes, diagnostics, and sidecar;
- the plan has no blocking diagnostic; and
- unmanaged or modified-file publication protections remain intact.

### Build and behavior gates

- generated Debug and Release builds complete with zero errors and zero warnings under the pair's declared profiles;
- all generated specifications pass;
- every expected specification emits exactly one canonical normalized outcome;
- reference and rendered accepted/rejected outcomes compare equal; and
- no expected outcome is represented by `Conflict`, `Unsupported`, crash, timeout, or missing result.

### Recovery and semantic gates

- every required adapter contribution is deterministic under reversed discovery order;
- required facts use admissible evidence and complete scenarios/applications are admitted atomically;
- no required semantic fact is heuristic, ambiguous, conflicted, or unsupported;
- rendered bindings pass all trust checks;
- rebound ESM has the same semantic revision;
- semantic identities, event-contract identities/revisions/shapes, capability requirements, specification identities/definitions, and normalized outcomes compare exactly; and
- rerender produces identical deterministic artifacts.

### Loss, privacy, and release gates

- semantic loss collection is empty;
- realization findings contain only declared allowlisted non-error codes;
- privacy red vectors pass and secret/path scanning is clean;
- affected repositories build warning-free and all relevant specs pass;
- integrations are verified against released public package versions rather than unpublished local packages;
- CI is green in every owning repository; and
- the conformance ledger changes from `Required` to passed only for the exact capabilities and pair demonstrated by published vectors.

## Completion boundary

Screenplay #148's first milestone is complete only when the first approved CritterStack target-matched pair passes the full green corpus and every first red vector through the complete pipeline, with public released dependencies and deterministic reports.

That milestone proves only the listed capabilities for that exact pair. It does not complete all source recovery, all Stage rendering, Studio round trips, the second backend, or the full Screenplay program.
