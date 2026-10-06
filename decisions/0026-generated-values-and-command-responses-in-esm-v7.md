---
id: 0026
title: Admit generated values and command responses as ESM v7
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
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

## Context

[0023](0023-command-production-model.md) approves `generated` command properties (#300) and response blocks (#303), and [#361](https://github.com/Cratis/Screenplay/pull/361) shipped their authoring. The binder refuses all of it, so none of it can be executed or rendered: generated properties, responses and return expectations at [`SemanticModelBinder.CommandProductions.cs:40-43`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.CommandProductions.cs), generated fixtures at `:50-53`. Operations and event-source or stream refusals sit beside them at `:26-39` and stay. An exact-mode document is refused earlier, at [`SemanticModelBinder.cs:23`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.cs).

[0004](0004-admission-and-governance-of-portable-executable-semantics.md) requires an accepted record, canonical bytes, reference execution or a typed unsupported outcome, source-backed vectors and activation only when used before a construct enters the ESM. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) proposes ESM v7 for these two features. This record is the v7 admission contract. It pins what 0023 left open, because admitting the construct is more than removing a refusal: every layer that today treats "all command properties" as one shape has to learn that a generated property exists only after the request has been validated.

Where the code assumes a single shape today:

- The request must carry exactly one value per command property ([`SemanticEvaluator.cs:474-486`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs)), and concept validation indexes every property's request value (`:578-583`).
- Authorization builds its artifact from the request values and takes `subject` from the identifier's request value (`:39-47`).
- A specification's `when` values are validated against all command properties ([`ExecutableSemanticModel.cs:862`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs)).
- A reaction must map every required command property ([`SemanticModelBinder.Reactions.cs:303-306`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Reactions.cs), [`SemanticModelValidator.Automation.cs:147-151`](../Source/DotNET/Screenplay/Semantics/SemanticModelValidator.Automation.cs)), and the reaction loop builds a value for every property, `Null` when unmapped ([`SemanticReactionLoop.cs:249-263`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticReactionLoop.cs)).
- Typed contexts for policies and rules publish every command property and the identifier as the subject ([`SemanticTypedContextCatalog.cs:87-94,139-143`](../Source/DotNET/Screenplay/Semantics/SemanticTypedContextCatalog.cs)).
- The property serializer is shared by types, events, commands and read models ([`SemanticModelCanonicalJson.cs:118,232,243,256,328`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelCanonicalJson.cs) call `WriteProperty`, `:178-187`).
- The scenario path that every v6 or later command specification takes returns the final query result, not the command's own result ([`SemanticScenario.cs:47-56,88-89`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticScenario.cs); [`SemanticSpecificationRunner.cs:96-104`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticSpecificationRunner.cs)). A response added to the accepted result would be lost there.

This record is proposed. It records no acceptance by Einar Ingebrigtsen or Sindre Alstad Wilting. Choices that are Sindre Alstad Wilting's defaults in 0023 or Einar Ingebrigtsen's approvals on #300 and #303 are not restated as new decisions.

## Decision

Generated values and command responses enter the ESM as v7 under 0004 and 0025, with the contract below. A model selects v7 only when it uses a generated property, a response, a generated fixture or a return expectation. Every other model keeps its version, bytes and revision.

### Three command-value shapes

A command's values exist in three shapes. Each layer uses exactly one.

| Shape | Contents | Used by |
| --- | --- | --- |
| **Request inputs** | One value per non-generated command property. | The request's exact-shape and type validation; authorization artifact; property and concept validation; declarative requirements; specification `when` values; reaction mappings. |
| **Generated fixture or allocation inputs** | A value for each generated property, supplied by a specification (`when … for` for a generated identifier, `when … generated` for any other generated value) or by the evaluator's allocation channel. | Generation only. Never validated as request input. |
| **Complete post-generation values** | Request inputs plus generated values, one per command property. | Productions (destinations, payload mappings) and response computation. |

The partition applies in every layer together. The request shape is the command's non-generated properties exactly; a request value that targets a generated property is `Rejected(Contract)`. Specification `when` values validate against request inputs, and generated fixtures against generated properties. A reaction maps request inputs only, and a mapping into a generated property is invalid; the syntax-level check already exists (`GeneratedPropertySuppliedAsInput`, `SpecificationResponseValidator.cs:174`) and the ESM model validator enforces it too. Typed contexts and the model validator follow the same partition. Updating one layer without the others either rejects valid v7 models or indexes values that do not exist.

### Phase order

For one command execution, in this order:

1. Request shape and type validation over request inputs.
2. Authorization, property and concept validation, declarative requirements, as 0023 orders them ("authorization, property rules, reads, derive/provide, read-backed requirements, then productions and responses").
3. Generation of generated values from fixtures or allocation.
4. Productions over complete values.
5. Response computed from complete values, after productions.
6. Constraint and projection evaluation. A failure here rejects the command and leaves the input world unchanged. No response is produced.

Nothing before phase 3 may observe a generated value. A denial or validation failure in phases 1 and 2 reports its own outcome and never reaches allocation. Arc binds the response when the handler returns it and takes it back when the command does not succeed ([`CommandPipeline.cs:596-598,665-672`](https://github.com/Cratis/Arc/blob/v22.48.2/Source/DotNET/Arc.Core/Commands/CommandPipeline.cs), Arc v22.48.2). The reference runner mirrors that: a response exists only on acceptance.

### Pre-generation references are refused

A generated property, including a generated identifier, is unavailable to anything that evaluates before generation. The binder, the model validator and the strict reader all refuse a reference to it, because a programmatically constructed or deserialized ESM is not checked by the binder alone. The refusal covers:

- Authorization policies referenced by the command, including inherited and composed policies.
- The implicit `subject`, when the command's identifier is generated. Policy evaluation takes the subject from the identifier's request value (`SemanticEvaluator.cs:44-47`), which does not exist for a generated identifier.
- Declarative requirements and property rules.
- **Concept rules on a generated property's concept.** v7 refuses a generated property whose concept declares validation rules. There is no post-generation validation phase in v7 and rules are never silently dropped. Admitting such concepts later, with a post-generation check, is additive and would take a new decision.
- Opaque context contracts. Typed contexts for pre-generation roles publish **input-only** command shapes, mark a generated identifier subject `Unavailable` (the catalog already has that source kind, `SemanticTypedContextCatalog.cs:139-143`) and list no generated properties. A contract whose references cannot be inspected keeps the opaque `Unsupported` behavior it has today.
- Programmatic and strict ESM validation, as above.

### Response representation

- **No response and a null response differ.** A command without a response block produces an accepted result with no response member. A command with a response whose optional source is absent produces a response whose value is `Null`. Establishing a world (`EstablishWorld`) carries no command response ([`semantic-model.md:75`](../Documentation/screenplay/projections/semantic-model.md)).
- **Scalar and record forms.** `returns <property>` is a scalar response with one source property. An unnamed `returns` block is a record response of named fields, each with a name, a source property and a type. The `Response` suffix and optional explicit types are Einar Ingebrigtsen's approval on #303, as recorded in 0023.
- **Order and names.** Record fields keep authored order, which is canonical and part of the revision. Names are non-empty and unique. Unknown, duplicate or malformed members are refused by the strict reader.
- **Sources.** A source is a direct property of the same command, by property id, and may be a generated property. A type must equal its source's type, collection shape and optionality; the model validator checks this. Collections and whole read models are refused ([`CommandResponseValidator.cs:55-69`](../Source/DotNET/Screenplay/Parsing/CommandResponseValidator.cs)). Ordinary composite-typed properties are allowed. An optional source may be absent and then yields `Null`.
- **Equality.** Response values compare with the semantic value rules ([`SemanticValueValidator.AreEqual`, `:26-40`](../Source/DotNET/Screenplay/Semantics/SemanticValueValidator.cs)), not CLR record equality. Arrays compare in order. Composites compare by property id regardless of member order.
- **`then returns`.** A scalar expectation is equal to the response value. A record expectation asserts a non-empty subset of fields by name, each unique and known. Fields not asserted are ignored. A mismatch is reported deterministically: one entry per differing field, in response field order, and a single entry for a scalar.
- **A success outcome.** `then returns` counts as a success outcome, so a response-only success specification is valid ([`ExecutableSemanticModel.cs:958-973`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs) does not yet count it). It requires a command action and is invalid together with errors or a denial, as the syntax validator already enforces (`SpecificationResponseValidator.cs:43-46`).

### Generated identifiers

A generated identifier serves as the command's destination, as a payload source and as a response source. Inline productions with an implicit destination lower to the identifier as before. A plain `produces` with no `for` keeps 0023's legacy exception and uses an allocated identity, never silently retargeted to the generated identifier; the evaluator still reads it from the allocated-identity channel ([`SemanticEvaluator.cs:123-133`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs)). A generated property without a generated identifier is allowed. A command with a response and no productions is valid and records no facts.

A fixture's shape and its absence are separate questions. Shape (a compatible type, a generated target, a nonidentifier target for `generated`, a UUID-compatible identifier for `for`) is validated when the specification is built and refused if wrong. A missing fixture is not a shape error.

**UUID normalization.** Source fixtures accept several textual UUID forms, case-insensitively ([`ResponseValueTypes.cs:62-69`](../Source/DotNET/Screenplay/Parsing/ResponseValueTypes.cs)). Semantic UUID values must be lowercase hyphenated ([`SemanticValueValidator.cs:20`](../Source/DotNET/Screenplay/Semantics/SemanticValueValidator.cs)). For v7 generated fixtures and return expectations on generated UUID-backed values, the binder lowers an accepted form to lowercase hyphenated text and refuses one that cannot be normalized. No existing value, model or byte changes.

### Reference runner

- **Missing fixture.** A generated value that is reached and has no fixture returns `Unsupported(IdentityAllocation)`, the existing capability. This happens only after phases 1 and 2 pass, so an authorization denial or validation failure still reports its own outcome. A specification that reaches Unsupported never passes.
- **Response.** Accepted results carry the response, computed from complete values after productions. The scenario path carries the initiating command's response onto the final result.
- **Failures.** A constraint rejection after generation, or a projection or query failure, leaves the command world unchanged and yields no response. No earlier response is retained.

### Reactions

- A reaction-invoked command that only returns a response runs, and its response is computed and discarded.
- A reaction-invoked command that reaches an allocation returns `Unsupported(IdentityAllocation)`. An invocation request has no fixture channel. A branch that is not reached is unaffected.
- Unmapped optional properties still get `Null`, as in v6. Generated properties are left out of the invocation's request values.
- A cascade failure retains the facts accepted before the failure, as v6 does ([`SemanticExecutionContracts.cs:262`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionContracts.cs); 0022). Clearing a response never implies rolling back facts. A later reaction failure after the initiating command committed yields a failure result with those facts and no response.
- A reaction cascade that binds is not thereby executable. A cascade with reached generation is `Unsupported`.

### Not guaranteed

Generated values give no idempotency, retry or deduplication guarantee. Each acceptance generates fresh values. Protected-read and concurrency admission is unchanged ([`SemanticModelBinder.Commands.cs:24-36`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Commands.cs)).

### Names, identity and scope

- **D2.** Collection and whole-read-model responses stay deferred, as 0023 records. Ordinary composites are allowed.
- **D5. Fields are identified by name.** A response field's name is an external contract (it becomes a proxy member). It is recorded by name in authored order with the source property id, and gets no catalog identity. Adding `generated` to a property keeps its property id. Renaming a source property keeps the reference and does not rename the field. Renaming a field is a contract change and changes the revision, as do changes to a mapping, a type, or field order. Moving a file or reordering declarations changes neither identity nor revision.
- **D6. Continuations are outside the ESM.** Binding returned names in `on submit` and `on success` is syntax and editor work. It does not gate v7 and remains part of #300 and #303. v7 does not close them or deliver end-to-end navigation.
- **D7. Consumers.** The v7 release opens tracking issues in Stage, CLI, Studio and Generation, per 0004. Each consumer admits read (strict reader), reference execution, rendering and reverse recovery separately. A package bump is none of them. Stage#175, Scene#53 and Arc#2885 remain open dependencies for what users see.
- **No Chronicle conformance.** Chronicle has no response. The runtime meaning is Arc's command pipeline, cited above; where Chronicle has no meaning, 0001 has the proposal state it deterministically, which this record does. No Chronicle conformance issue is opened under 0004 clause 4.

### Canonical form

The new fields appear only when present, and only on commands: `"generated": true` on a command property (never `false`, never on type, event or read-model properties); `response` on a command (scalar: a source property id; record: ordered fields with name, type and source); `generatedValues` on a specification command, ordered by target property id; `thenReturns` on a specification, record assertions ordered by field name. The strict reader accepts `generated` only inside command properties at schema version 7 or later with the value `true`, and `response`, `generatedValues` and `thenReturns` only at version 7 or later. It keeps rejecting `numericMode` in v7 (0025 point 5). Exact-mode documents with these constructs keep failing binding until exact mode is admitted.

**Preserved for v1–v6:** canonical bytes and revisions of every model that does not use a v7 construct, and normalized execution outcomes including authorization and reaction behavior. Nothing writes `generated: false`, an absent response or an empty fixture collection. No existing property or catalog address, shared destination lowering or value normalization changes.

## Options considered

- **Pin three value shapes (chosen).** It is the only way authorization, validation and reactions keep seeing exactly the values that exist when they run.
- **Treat generated properties as ordinary request values with a flag.** Not taken: layers would index values that do not exist yet, and a generated property could be supplied as input.
- **Generate before authorization and validation.** Not taken: it allocates before a denial, contradicts 0023's order and has no support in Arc's pipeline, which binds responses after the handler.
- **Check concept rules after generation.** Not taken for v7: it adds a phase and a failure category. Refusing is additive to lift.
- **Missing fixture allocates a deterministic placeholder and compares relationally.** Not taken: new outcome semantics need their own vectors. `Unsupported(IdentityAllocation)` matches existing allocated-identity behavior. Relational assertions stay a consumer concern and can be added later.
- **Response fields with semantic ids.** Not taken: a response is a transient contract named for callers, and ids would add catalog entries and rename semantics it does not need. The cost is that field names are an external contract, stated above.
- **Collections and whole read models now.** Not taken, per 0023: they need reads or handler returns that do not bind.
- **Reaction cascades roll back on failure.** Not taken: v6 keeps accepted facts, and changing it would alter existing outcomes.
- **Exclude reaction-invoked commands wholesale.** Not taken: response-only invocations are safe and common.

## Default if unanswered

The binder keeps refusing generated values, responses and return expectations with `PLAY0268`. Authoring, printing and editors keep working, but no model with them can be executed, rendered or exported to the ESM. Stage#175, Scene#53 and Arc#2885 stay blocked. Implementation would have no contract for the open questions above and each layer would choose its own.

## Timeline and scope

Must be accepted before any v7 implementation merges to `main`, and after 0025 is accepted. It holds until superseded.

In scope: the v7 ESM fields, readers and writers, all validators, binder and reaction rules, typed contexts, evaluator, scenario and runner behavior, vectors, MCP export and readiness, editors, documentation, samples, and consumer tracking.

Out of scope: UI continuations (D6); collections and whole-read-model responses; arbitrary returns from inline implementation code; operations, event sources, streams and reads; exact numbers; idempotency or retry; Chronicle runtime changes; and any change to v1–v6.

## Verification

**Done when:** a source-backed v7 corpus pins bytes, revision and normalized outcomes; v1–v6 bytes, revisions and outcomes are unchanged; and every behavior above has a vector that fails when it is wrong. The cross-feature vectors are:

- *Identity.* A generated identifier as destination, payload source and response source. A generated nonidentifier without a generated identifier. A response-only command with no facts. An inline implicit destination beside a plain legacy allocated destination.
- *Fixtures.* Fixture and type disagreement. Malformed and foreign fixture targets. UUID normalization and refusal.
- *Phase order.* Authorization denial and validation failure before allocation is reached, with no `Unsupported`. Missing reached fixture giving `Unsupported(IdentityAllocation)` only after preconditions. Constraint rejection after generation with the world unchanged and no response. Query or projection failure with no retained response.
- *Pre-generation references.* An inherited or composed policy, the implicit `subject` on a generated identifier, a declarative requirement, a property rule, a concept rule on a generated concept, the input-only shape in typed contexts, and programmatic and strictly deserialized models. Each is refused.
- *Responses.* No response against a null response. Scalar against record. Authored order. Duplicate, unknown and malformed fields. Optional and composite sources. Array-bearing equality. Deterministic mismatch reporting. `then returns` as the only success outcome, and invalid with errors or a denial.
- *Reactions.* Unreached branches. A successful response-only invocation. A reached allocation giving `Unsupported`. Preserved unmapped-optional behavior. Facts retained after a later cascade failure with no response.
- *Compatibility.* Old golden and corpus bytes and revisions unchanged, plus old authorization and reaction outcomes. An exact document with responses still fails binding. `numericMode` is rejected in v7.
- *Identity and revision.* Adding `generated` keeps the property id. A source rename keeps response references and does not rename the field. Response mapping, type, name and order changes change the revision. File relocation and declaration reordering do not.
- *Corpus.* Source rejection vectors for forbidden generated inputs, separate from programmatic evaluator tests for `Rejected(Contract)`: a source-backed model cannot reach that outcome, since the syntax validator refuses it first (`SpecificationResponseValidator.cs:26,79`). Source-backed vectors for acceptance with a response, missing-allocation `Unsupported` and a mismatched return.
- *Surfaces.* MCP export reconstruction across pages and a stale model revision. Readiness reports a response as admitted but a command executable only when every construct it uses is admitted. C# and TypeScript syntax conformance vectors regenerated for any sample change. `semantic-model.md` states that world establishment carries no command response.

**Verify by:** Run the golden, corpus, evaluator, runner, binder, validator, strict-reader and reaction specs listed above. Build both configurations. Open consumer tracking issues at release and check that each states which admission it covers. Check the `Decision: 0004, 0025, 0026` trailer on the merging pull request.

## Consequences

Authors can write a command that creates something and returns what was created, and Stage and Arc can render and type that response once they admit v7. Specifications can pin both events and what the caller receives.

Costs: v7 touches model, readers, writer, three validators, binder, typed contexts, evaluator, scenario, runner and surfaces together, so it ships as one release and not in parts. Generated values cannot be used in authorization, validation or requirements, and generated concepts cannot carry rules, until a later decision lifts that. Reaction cascades that need generation are `Unsupported`. Field names become a contract that authors must treat as one.

## Related issues

Screenplay: [#300](https://github.com/Cratis/Screenplay/issues/300), [#303](https://github.com/Cratis/Screenplay/issues/303), [#309](https://github.com/Cratis/Screenplay/issues/309), [#361](https://github.com/Cratis/Screenplay/pull/361). Stage: [#175](https://github.com/Cratis/Stage/issues/175). Scene: [#53](https://github.com/Cratis/Scene/issues/53). Arc: [#2885](https://github.com/Cratis/Arc/issues/2885).

## Status notes

**Proposed.** No decider and no acceptance date are recorded.
