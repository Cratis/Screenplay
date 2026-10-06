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

This record is proposed. It records no acceptance by Einar Ingebrigtsen or Sindre Alstad Wilting. Choices that are Sindre Alstad Wilting's defaults in 0023 or Einar Ingebrigtsen's #303 approval, which implies approval of #300's dependent design as recorded in 0023, are not restated as new decisions.

## Decision

Generated values and command responses enter the ESM as v7 under 0004 and 0025, with the contract below. A model selects v7 only when it uses a generated property, a response, a generated fixture or a return expectation. Every other model keeps its version, bytes and revision.

### Three command-value shapes

A command's values exist in three shapes. Each layer uses exactly one.

| Shape | Contents | Used by |
| --- | --- | --- |
| **Request inputs** | One value per non-generated command property. | The request's exact-shape and type validation; authorization artifact; property and concept validation; declarative requirements; specification `when` values; reaction mappings. |
| **Generated fixture or allocation inputs** | A value for each generated property, supplied by a specification (`when … for` for a generated identifier, `when … generated` for any other generated value) or by the evaluator's generated-value channel (`SemanticExecutionRequest.GeneratedValues`, below). | Generation only. Never validated as request input. |
| **Complete post-generation values** | Request inputs plus generated values, one per command property. | Productions (destinations, payload mappings) and response computation. |

The partition applies in every layer together. The request shape is the command's non-generated properties exactly; a request value that targets a generated property is `Rejected(Contract)`. Specification `when` values validate against request inputs, and generated fixtures against generated properties. A reaction maps request inputs only, and a mapping into a generated property is invalid; the syntax-level check already exists (`GeneratedPropertySuppliedAsInput`, `SpecificationResponseValidator.cs:174`) and the ESM model validator enforces it too. Typed contexts and the model validator follow the same partition. Updating one layer without the others either rejects valid v7 models or indexes values that do not exist.

### Phase order

The common command phases retain their existing order. This decision does not change request-envelope checks, existing version-gated destination or occurrence checks, or their precedence. Generation is inserted after successful command validation and before productions.

Preserved as they are today ([`SemanticEvaluator.cs`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs)):

- Request-envelope checks that run before authorization (`:19-37`): a read-only request carrying command values, an unknown command (`Unsupported`) and a default `Queries` collection (`Rejected(Contract)`).
- The pre-v6 occurrence check (`:98-102`), which rejects a missing occurrence for any `$context` mapping before production conditions are tested, and the v6 and later production-time check (`:112-116`), which applies only to a production that is reached.
- The allocated-destination checks (`:75-80`, `:136-140`), version-gated as today.

For the command phases the order is, with generation added after validation and before productions:

1. Authorization (`:39-58`). The artifact and `subject` are built from whatever request values were supplied, as today.
2. Opaque-validation admission (`:60-68`): a command with an opaque validation predicate reports `Unsupported`.
3. Request shape and type validation over request inputs (`:70-73`).
4. Declarative validation, property and concept rules, and requirements, as 0023 orders them ("authorization, property rules, reads, derive/provide, read-backed requirements, then productions and responses").
5. Generation of generated values from fixtures or allocation.
6. Productions over complete values.
7. Response computed from complete values, after productions.
8. Constraint and projection evaluation.

Existing outcomes therefore stay as they are. Here "malformed" means malformed command input values on an otherwise valid request envelope. Malformed input values from an unauthorized caller stay `Unauthorized`. Malformed input values on a command with opaque validation stay `Unsupported`. Malformed input values on an authorized, admitted command are `Rejected(Contract)` before declarative validation. A request value that targets a generated property is malformed in the same sense and takes the same position. An invalid envelope, such as a default `Queries` collection, is still refused first, even for an unauthorized caller.

Nothing before step 5 may observe a generated value. A denial, an `Unsupported` admission, a contract rejection or a validation failure in steps 1 to 4 reports its own outcome and never reaches allocation.

**Failures.**

- Failure before an individual command is accepted preserves that command's input world and produces no response.
- Once the initiating command or a reaction has been accepted, subsequent reaction or scenario-query failure retains already accepted facts and produces no response.
- Queries included in a direct evaluator request remain part of that request's atomic acceptance boundary.
- Projection inability stays `Unsupported(Projection)` (`SemanticEvaluator.cs:183-190,223-230`). It is not recast as `Rejected`. A constraint violation after generation is a rejection.

On the scenario path the initiating command is accepted first ([`SemanticScenario.cs:47-56`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticScenario.cs)), reactions settle and queries then run against the loop's world (`:88-89`). A failure there therefore keeps the accepted command and cascade facts.

Arc binds the response when the handler returns it and takes it back when the command does not succeed ([`CommandPipeline.cs:596-598,665-672`](https://github.com/Cratis/Arc/blob/v22.48.2/Source/DotNET/Arc.Core/Commands/CommandPipeline.cs), Arc v22.48.2). The reference runner mirrors that: a response exists only on acceptance and only when nothing after acceptance failed.

### Pre-generation references are refused

A generated property, including a generated identifier, is unavailable to anything that evaluates before generation. The binder, the model validator and the strict reader all refuse a reference to it, because a programmatically constructed or deserialized ESM is not checked by the binder alone. The refusal covers:

- Authorization policies referenced by the command, including inherited and composed policies, when the policy references a generated property or the subject of a generated identifier. A policy that references only input properties, the caller or other inputs stays usable.
- The implicit `subject`, when the command's identifier is generated. Policy evaluation takes the subject from the identifier's request value (`SemanticEvaluator.cs:44-47`), which does not exist for a generated identifier.
- Declarative requirements and property rules that reference a generated property. Rules and requirements over input properties alone stay usable on the same command.
- **Concept rules on a generated property's concept.** v7 refuses a generated property whose concept declares validation rules of any kind: declarative, named or code validations. There is no post-generation validation phase in v7 and rules are never silently dropped. Admitting such concepts later, with a post-generation check, is additive and would take a new decision.
- Opaque context contracts. Typed contexts for pre-generation roles publish **input-only** command shapes, mark a generated identifier subject `Unavailable` (the catalog already has that source kind, `SemanticTypedContextCatalog.cs:139-143`) and list no generated properties. A contract whose references cannot be inspected keeps the opaque `Unsupported` behavior it has today.
- Programmatic and strict ESM validation, as above.

**Diagnostics.** No new code is added. A pre-generation reference in source is `PLAY0273` (`InvalidSemanticBinding`) with an actionable message, for example: "Generated property 'Id' of command 'CreateOrder' cannot be used by authorization policy 'P'; generated values exist only after validation. Reference an input property or remove 'generated'." A deliberately unadmitted combination (for example a generated property on a concept with rules) is `PLAY0268` (`UnsupportedSemanticSyntax`). Reaction invocation of a command with generated properties is admitted. Only execution that reaches generation without supplied values returns `Unsupported(IdentityAllocation)`; it is not a bind-time `PLAY0268` refusal. `PLAY0485` (`GeneratedPropertySuppliedAsInput`) is used only when a generated property is supplied as request or form input, never for a policy or rule reference. The ESM validator and strict reader refuse a pre-generation reference in a programmatic or deserialized model through the existing malformed-contract failure (`InvalidSemanticContract`), not a source diagnostic.

**Positive vectors** prove the refusals are narrow: an unrelated policy on a command that has a generated property, an input-only rule or requirement on the same command, and a generated identifier on a concept without rules each bind and execute.

### Response representation

- **No response and a null response differ.** A command without a response block produces an accepted result with no response member. A command with a response whose optional source is absent produces a response whose value is `Null`. Establishing a world (`EstablishWorld`) carries no command response ([`semantic-model.md:75`](../Documentation/screenplay/projections/semantic-model.md)).
- **Scalar and record forms.** `returns <property>` is a scalar response with one source property. An unnamed `returns` block is a record response of named fields, each with a name, a source property and a type. The `Response` suffix and optional explicit types are Einar Ingebrigtsen's approval on #303, as recorded in 0023.
- **Order and names.** Record fields keep authored order, which is canonical and part of the revision. Names are non-empty and unique. Unknown, duplicate or malformed members are refused by the strict reader.
- **Sources.** A source is a direct property of the same command, by property id, and may be a generated property. A type must equal its source's type, collection shape and optionality; the model validator checks this. Collections and whole read models are refused ([`CommandResponseValidator.cs:55-69`](../Source/DotNET/Screenplay/Parsing/CommandResponseValidator.cs)). Ordinary composite-typed properties are allowed. An optional source may be absent and then yields `Null`.
- **Equality.** Response values compare with the semantic value rules ([`SemanticValueRules.AreEqual`, `SemanticValueValidator.cs:26-40`](../Source/DotNET/Screenplay/Semantics/SemanticValueValidator.cs)), not CLR record equality. Arrays compare in order. Composites compare by property id regardless of member order.
- **`then returns`.** A scalar expectation is equal to the response value. A record expectation asserts a non-empty subset of fields by name, each unique and known. Fields not asserted are ignored. A mismatch is reported deterministically: one entry per differing field, in response field order, and a single entry for a scalar.
- **A success outcome.** `then returns` counts as a success outcome, so a specification with `then returns` and no `then` events is valid ([`ExecutableSemanticModel.cs:958-973`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs) does not yet count it). It keeps the existing comparison of "no expected events": a command specification without `then` events still asserts that the command produced no facts, because the runner compares facts whenever no appended event is under test or events are expected ([`SemanticSpecificationRunner.cs:211-215`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticSpecificationRunner.cs)). `then returns` neither skips nor relaxes that comparison, and specifications without return expectations behave as before. It requires a command action and is invalid together with errors or a denial, as the syntax validator already enforces (`SpecificationResponseValidator.cs:43-46`).

### Generated identifiers

A generated identifier serves as the command's destination, as a payload source and as a response source. Inline productions with an implicit destination lower to the identifier as before. A plain `produces` with no `for` keeps 0023's legacy exception and uses an allocated identity, never silently retargeted to the generated identifier; the evaluator still reads it from the allocated-identity channel ([`SemanticEvaluator.cs:123-133`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs)). A command cannot combine an inline implicit destination with a plain legacy production: the existing check refuses it ([`SemanticModelBinder.Commands.cs:59-65`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Commands.cs), `ExplicitProducesTargetsRequired`). Authors use separate commands, and the combined form is a refusal vector. **Acknowledged limitation.** A source specification that combines a generated identifier with a reached plain legacy allocated production cannot supply both channels (`when … for` feeds only `GeneratedValues`), so it reaches `Unsupported(IdentityAllocation)`. A direct evaluator request can supply both. No previously executable model regresses. A generated property without a generated identifier is allowed. A command with a response and no productions is valid and records no facts.

**`when … for` on a command with a generated identifier.** Today `when … for` is typed from the command's destination (`SemanticModelBinder.Specifications.cs:92`, `ExecutableSemanticModel.cs:853-859`) and asserts that every fact the initiating command produced has that destination (`SemanticSpecificationRunner.cs:219-222`). For a command that has a generated identifier v7 changes this, and only for such commands:

- `for` is typed against the generated identifier property and supplies its generation fixture. It is stored as a `generatedValues` entry (see *Canonical form*), not as the legacy `eventSource` member.
- It does **not** assert that explicitly routed productions target that value. A command can return the identifier it generated and produce an event to a different identity through an explicit `for`. Routing is asserted by each expected event's own destination.
- The runner skips the "every produced fact has this destination" check for these specifications.
- It does **not** share the legacy allocated-destination channel. The generated fixture travels only in `GeneratedValues`; `AllocatedIdentities` and `AllocatedEventSourceType` keep serving plain legacy productions. One channel per purpose is the simplest consistent choice: a fixture meant for generation can never satisfy, or be consumed as, a legacy destination, and the typed-destination check at `SemanticEvaluator.cs:75-80` stays unchanged.
- For every other command (no generated identifier), `when … for` behaves exactly as in v6, with the same typing, channel and destination assertion.

A fixture's shape and its absence are separate questions. Shape (a compatible type, a generated target, a nonidentifier target for `generated`, a UUID-compatible identifier for `for`) is validated when the specification is built and refused if wrong. A missing fixture is not a shape error.

**UUID normalization.** Source fixtures accept several textual UUID forms, case-insensitively ([`ResponseValueTypes.cs:62-69`](../Source/DotNET/Screenplay/Parsing/ResponseValueTypes.cs)). Semantic UUID values must be lowercase hyphenated ([`SemanticValueValidator.cs:20`](../Source/DotNET/Screenplay/Semantics/SemanticValueValidator.cs)). For v7 generated fixtures and return expectations on generated UUID-backed values, the binder lowers an accepted form to lowercase hyphenated text and refuses one that cannot be normalized. No existing value, model or byte changes.

### Reference runner

- **Missing fixture.** A generated value that is reached and has no fixture returns `Unsupported(IdentityAllocation)`, the existing capability. This happens only after steps 1 to 4 pass, so an authorization denial or validation failure still reports its own outcome. A specification that reaches Unsupported never passes.
- **Evaluator generated-value channel.** `SemanticExecutionRequest` gains an optional init member `GeneratedValues` of `ImmutableArray<SemanticPropertyValue>`, beside `AllocatedIdentities` (`SemanticExecutionContracts.cs:204-208`). Each entry targets a generated property of the requested command and carries a semantic value of that property's type. Default or empty means none supplied. Existing constructors and callers are unchanged. Handling happens only when generation is reached: a null or malformed entry, an entry whose target is not a generated property of the command (foreign, or an input property), a value of the wrong type or a duplicate target returns `Rejected(Contract)`. When generation is reached, the entire supplied `GeneratedValues` collection is validated before completeness is checked: any malformed, foreign, mistyped or duplicate entry returns `Rejected(Contract)`, even when another generated property is missing. Only a well-formed collection with a missing required entry returns `Unsupported(IdentityAllocation)`. Entries for generated properties not reached are ignored.
- **Response.** Accepted results carry the response, computed from complete values after productions. The scenario path carries the initiating command's response onto the final result.
- **Failures.** As in *Phase order*: a failure before the command is accepted yields no response and leaves the input world unchanged; once accepted, later reaction or scenario-query failure keeps the accepted facts and yields no response.

### Reactions

- A reaction-invoked command that only returns a response runs, and its response is computed and discarded.
- Reaction invocation of a command with generated properties is admitted. A reaction-invoked command that reaches generation or allocation returns `Unsupported(IdentityAllocation)`. An invocation request has no fixture channel. A branch that is not reached is unaffected.
- Unmapped optional properties still get `Null`, as in v6. Generated properties are left out of the invocation's request values.
- A cascade failure retains the facts accepted before the failure, as v6 does ([`SemanticExecutionContracts.cs:262`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionContracts.cs); 0022). Clearing a response never implies rolling back facts. A later reaction failure after the initiating command was accepted yields a failure result with those facts and no response.
- A reaction cascade that binds is not thereby executable. A cascade with reached generation is `Unsupported`.

### Not guaranteed

Generated values give no idempotency, retry or deduplication guarantee. In production each acceptance generates fresh values. The reference runner's supplied fixtures are exempt: they are deterministic and reused by design. Protected-read and concurrency admission is unchanged ([`SemanticModelBinder.Commands.cs:24-36`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Commands.cs)).

### Names, identity and scope

- **D2.** Collection and whole-read-model responses stay deferred, as 0023 records. Ordinary composites are allowed.
- **D5. Fields are identified by name.** A response field's name is an external contract (it becomes a proxy member). It is recorded by name in authored order with the source property id, and gets no catalog identity. Adding `generated` to a property keeps its property id. Renaming a source property keeps the reference and does not rename the field. Renaming a field is a contract change and changes the revision, as do changes to a mapping, a type, or field order. Moving a file or reordering declarations changes neither identity nor revision.
- **D6. Continuations are outside the ESM.** Binding returned names in `on submit` and `on success` is syntax and editor work. It does not gate v7 and remains part of #300 and #303. v7 does not close them or deliver end-to-end navigation.
- **D7. Consumers.** The v7 release opens tracking issues in Stage, CLI, Studio and Generation, per 0004. Each consumer admits read (strict reader), reference execution, rendering and reverse recovery separately. A package bump is none of them. End-to-end Screenplay response delivery remains blocked; downstream implementation in Stage#175, Scene#53 and Arc#2885 can proceed independently.
- **No Chronicle conformance.** Chronicle has no response. The runtime meaning is Arc's command pipeline, cited above; where Chronicle has no meaning, 0001 has the proposal state it deterministically, which this record does. No Chronicle conformance issue is opened under 0004 clause 4.

### Canonical form

All additions appear only when present. Every example below is a fragment of the existing canonical model, uses the existing type-reference and value encodings, and shows members in the order the writer emits them. Member names are exact. The strict reader rejects unknown, duplicate or malformed members as it does today.

**Command property: `generated`.** The command property writer (a command-specific variant of `WriteProperty`, `SemanticModelCanonicalJson.cs:178-187`) writes `generated` after `identifier`, only when true. Properties stay ordered by id. Type, event and read-model properties never write it. It is never written as `false`.

A generated property is a required, non-collection reference to a declared UUID-backed concept. The binder, the programmatic model validator and the strict reader all enforce this, matching the existing source rule ([`CommandResponseValidator.cs:26-31`](../Source/DotNET/Screenplay/Parsing/CommandResponseValidator.cs), 0023). A primitive `uuid`, an optional or collection type, or a concept not backed by UUID is refused. `target` below is the concept's `sem1:` catalog address, as in the existing goldens (for example `Corpus/Reactions/v6/expected/esm-v6.json`).

```json
{ "id": "…", "name": "Id", "type": { "kind": "concept", "primitive": null, "target": "sem1:…", "collection": false, "optional": false }, "identifier": true, "generated": true }
```

**Command: `response`.** The last member of the command object, after `destination`. Omitted when the command has no response; never `null`. `kind` is `"scalar"` or `"record"`. `type` is the existing type reference and must equal the source property's type, including collection shape and optionality. `source` is a property id of the same command.

For a scalar response sourced from the generated property above, `type` is the same concept reference:

```json
"response": { "kind": "scalar", "source": "…", "type": { "kind": "concept", "primitive": null, "target": "sem1:…", "collection": false, "optional": false } }
```

```json
"response": { "kind": "record", "fields": [
  { "name": "orderId", "source": "…", "type": { … } },
  { "name": "note", "source": "…", "type": { … } } ] }
```

Record `fields` is non-empty and in authored order (not sorted; order is part of the revision). Names are unique and non-empty. Field members are written in the order `name`, `source`, `type`, and all three are mandatory.

**Specification `when`: `generatedValues`.** An array of the existing property-value shape (`targetProperty`, `value`, as `WritePropertyValue`), written after `values` and before `eventSource`, only when non-empty, sorted by `targetProperty` ordinal. Each target is a generated property of the command, with a value of its type. `values` targets only non-generated properties. A `when … for` on a command with a generated identifier is written as an entry here, and such a `when` has no `eventSource` member; the reader rejects both together.

```json
"when": { "command": "…", "values": [ … ],
  "generatedValues": [ { "targetProperty": "…", "value": { "kind": "string", "value": "0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11" } } ] }
```

Generated UUID fixtures are written as lowercase hyphenated `string` values (see *UUID normalization*).

**Specification `thenReturns`.** Written after `thenDenied` and before the automation members, only when a return expectation exists, never `null`. Scalar: `{ "kind": "scalar", "value": <value> }`, where `value` is mandatory. Record: `{ "kind": "record", "fields": [ { "name", "value" } ] }`, non-empty, sorted by `name` ordinal, names unique and known, `value` mandatory.

```json
"thenReturns": { "kind": "scalar", "value": { "kind": "null" } }
```

```json
"thenReturns": { "kind": "record", "fields": [ { "name": "note", "value": { "kind": "null" } } ] }
```

The first asserts a **null scalar response**. The second asserts a **record whose `note` field is null**. They differ by `kind`, and neither is the omission of `thenReturns` (no return assertion). A `value` is never JSON `null` and never omitted: the absence of a value is the semantic value `{ "kind": "null" }`.

**Readers and versions.** The strict reader accepts `generated` only inside command properties at schema version 7 or later with the value `true`, and `response`, `generatedValues` and `thenReturns` only at version 7 or later, on commands, specification `when` objects and specifications respectively. It keeps rejecting `numericMode` in v7 (0025 point 5). Exact-mode documents with these constructs keep failing binding until exact mode is admitted.

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

The binder keeps refusing generated values, responses and return expectations with `PLAY0268`. Authoring, printing and editors keep working, but no model with them can be executed, rendered or exported to the ESM. End-to-end Screenplay response delivery remains blocked; downstream implementation can proceed independently. Implementation would have no contract for the open questions above and each layer would choose its own.

## Timeline and scope

Must be accepted before any v7 implementation merges to `main`, and after 0025 is accepted. It holds until superseded.

In scope: the v7 ESM fields, readers and writers, all validators, binder and reaction rules, typed contexts, evaluator, scenario and runner behavior, vectors, MCP export and readiness, editors, documentation, samples, and consumer tracking.

Out of scope: UI continuations (D6); collections and whole-read-model responses; arbitrary returns from inline implementation code; operations, event sources, streams and reads; exact numbers; idempotency or retry; Chronicle runtime changes; and any change to v1–v6.

## Verification

**Done when:** a source-backed v7 corpus pins bytes, revision and normalized outcomes; v1–v6 bytes, revisions and outcomes are unchanged; and every behavior above has a vector that fails when it is wrong. The cross-feature vectors are:

- *Identity.* A generated identifier as destination, payload source and response source. A generated nonidentifier without a generated identifier. A response-only command with no facts. An inline implicit destination combined with a plain legacy production in one command, as a refusal vector, and the same two forms in separate commands as acceptance vectors. A command that returns its generated identifier while a production explicitly targets another identity: `when … for` supplies the identifier fixture, the returned value equals it, the production's own expected-event destination is asserted, and no destination mismatch is reported. The same specification shape on a command without a generated identifier keeps the v6 destination assertion.
- *Fixtures.* Fixture and type disagreement. Malformed and foreign fixture targets. UUID normalization and refusal.
- *Phase order.* Malformed input combined with an authorization denial (stays `Unauthorized`), with an opaque validation rule (stays `Unsupported`) and on an authorized admitted command (`Rejected(Contract)`), run at v6 and v7 with identical outcomes on a valid request envelope. A default `Queries` collection with an unauthorized caller stays `Rejected(Contract)` at every version. An unreached context-dependent production with no occurrence stays `Rejected(Contract)` before v6 and succeeds at v6 and later, unchanged by this decision. Authorization denial and validation failure before allocation is reached, with no `Unsupported`. Missing reached fixture giving `Unsupported(IdentityAllocation)` only after preconditions. Constraint rejection after generation with the world unchanged and no response. A scenario-query failure after acceptance keeping the accepted facts with no response, and a direct evaluator request query failure inside the atomic boundary. Projection inability reporting `Unsupported(Projection)`.
- *Pre-generation references.* An inherited or composed policy, the implicit `subject` on a generated identifier, a declarative requirement, a property rule, a concept rule on a generated concept, the input-only shape in typed contexts, and programmatic and strictly deserialized models. Each is refused with the pinned diagnostic (`PLAY0273`, `PLAY0268` or the malformed-contract failure) and an actionable message. Positive vectors prove an unrelated policy, an input-only rule or requirement and a generated identifier on a rule-free concept remain usable. A generated concept with a declarative, a named and a code validation is refused for each.
- *Canonical JSON.* The examples above as golden vectors, with the generated property and its scalar response using a UUID-backed concept reference, and refusal vectors for a primitive, optional, collection or non-UUID generated type in the binder, programmatic validator and strict reader: command property, scalar and record response, `generatedValues` including a generated identifier fixture, and scalar and record `thenReturns`. A null scalar against a record with a null field. Mandatory and omitted members, member order, field and fixture sort order, and reader rejection of every misplaced, duplicate or pre-v7 member.
- *Evaluator channel.* `GeneratedValues` supplied malformed, foreign, mistyped or duplicate gives `Rejected(Contract)` when generation is reached and is ignored when it is not. A missing entry gives `Unsupported(IdentityAllocation)`. One missing property combined with one malformed supplied entry gives `Rejected(Contract)`, with input and property ordering varied.
- *Responses.* No response against a null response. Scalar against record. Authored order. Duplicate, unknown and malformed fields. Optional and composite sources. Array-bearing equality. Deterministic mismatch reporting. `then returns` as the only success outcome, keeping the "no expected events" comparison, and invalid with errors or a denial.
- *Reactions.* Unreached branches. A successful response-only invocation. A reached allocation giving `Unsupported`. Preserved unmapped-optional behavior. Facts retained after a later cascade failure with no response.
- *Compatibility.* Old golden and corpus bytes and revisions unchanged, plus old authorization and reaction outcomes. An exact document with responses still fails binding. `numericMode` is rejected in v7.
- *Identity and revision.* Adding `generated` keeps the property id. A source rename keeps response references and does not rename the field. Response mapping, type, name and order changes change the revision. File relocation and declaration reordering do not.
- *Corpus.* Source rejection vectors for forbidden generated inputs, separate from programmatic evaluator tests for `Rejected(Contract)`: a source-backed model cannot reach that outcome, since the syntax validator refuses it first (`SpecificationResponseValidator.cs:26,79`). Source-backed vectors for acceptance with a response, missing-allocation `Unsupported` and a mismatched return.
- *Surfaces.* MCP export reconstruction across pages and a stale model revision. Readiness reports a response as admitted but a command executable only when every construct it uses is admitted. C# and TypeScript syntax conformance vectors regenerated for any sample change. `semantic-model.md` states that world establishment carries no command response.

**Verify by:** Run the golden, corpus, evaluator, runner, binder, validator, strict-reader and reaction specs listed above. Build both configurations. Open consumer tracking issues at release and check that each states which admission it covers. Check the `Decision: 0004, 0025, 0026` trailer on the merging pull request.

## Consequences

Authors can write a command that creates something and returns what was created, and Stage and Arc can render and type that response once they admit v7. Specifications can pin both events and what the caller receives.

Costs: v7 touches model, readers, writer, three validators, binder, typed contexts, evaluator, scenario, runner and surfaces together, so it ships as one release and not in parts. Generated values cannot be used in authorization, validation or requirements, and generated concepts cannot carry rules, until a later decision lifts that. Reaction cascades that reach generation without supplied values are `Unsupported`. Field names become a contract that authors must treat as one.

## Related issues

Screenplay: [#300](https://github.com/Cratis/Screenplay/issues/300), [#303](https://github.com/Cratis/Screenplay/issues/303), [#309](https://github.com/Cratis/Screenplay/issues/309), [#361](https://github.com/Cratis/Screenplay/pull/361). Stage: [#175](https://github.com/Cratis/Stage/issues/175). Scene: [#53](https://github.com/Cratis/Scene/issues/53). Arc: [#2885](https://github.com/Cratis/Arc/issues/2885).

## Status notes

**Proposed.** No decider and no acceptance date are recorded.
