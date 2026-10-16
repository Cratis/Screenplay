# Golden vectors

The `full-esm-*.json` files pin canonical executable semantic model (ESM) bytes, including each model's semantic revision.

Consume the released bytes through `Cratis.Screenplay.CanonicalCorpus`, rather than copying these files.
`EsmGoldenVectors.Versions` discovers the full vectors in ascending version order;
`EsmGoldenVectors.Get(version)` returns an `EsmGoldenVector` with `Version`, immutable `Bytes`, and
`SemanticRevision` verified by the strict ESM reader. Unknown versions throw `EsmGoldenVectorNotFound`.
`EsmGoldenVectors.PolicyNegationV7` exposes the separate policy-negation extension described below.
The package links these ESM files as embedded resources in every build configuration, without copies.
Adding a new `full-esm-v<N>.json` automatically includes it and exposes its version in the next package build.
Expression, identity-catalog and typed-context sidecars are separate contracts and are not part of this API.

The full vectors are built from these source models:

| File | Source model |
| --- | --- |
| `full-esm-v1.json` | `canonical_serialization_golden_vectors.CreateSemanticModel()` |
| `full-esm-v2.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV2()` — typed destination, explicit specification event sources, and an audit identity occurrence mapping |
| `full-esm-v3.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV3()` — the full v2 model plus a reducer-built read model with opaque transitions, a rule predicate, command code validation, and an opaque policy predicate |
| `full-esm-v4.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV4()` — mixed ordinary and three-generation events, with historical tags and v4 transition cardinality |
| `full-esm-v5.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV5()` — the v4 model plus one keyed read-model absence assertion |
| `full-esm-v6.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV6()` — the v5 model plus an application trigger, reactions covering every trigger kind, a capture covering every map operation, condition, child collection and nested record, and specifications that state, advance and fire clocks, triggers and capture records |

`full-esm-v7.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV7()`: the full v6 model plus a rule-free UUID concept, generated identifier and nonidentifier command properties, scalar and authored-order record responses, generation fixtures, and scalar/record return assertions including null. The C# binder and reference runner admit these constructs as ESM v7. Source-backed bytes and execution outcomes are pinned separately by `RegisterProjectCorpus.V7`; downstream read, execution, rendering and reverse-recovery admission remain explicit consumer decisions.

`policy-negation-v7.json` pins the byte-preserving v7 extension admitted by decision 0027. Its source is `PolicyNegationCorpus.SourceForms`: negated roles, claim matches, groups and repeated negation. `PolicyNegationCorpus.V7` also pins its revision and allowed/denied reference outcomes across single, split, reordered and relocated source forms. This separate vector leaves the original `full-esm-v1.json` through `full-esm-v7.json` unchanged.

`full-esm-v8.json` pins ESM v8 event routes under decision 0036. It is built on the full v7 model by `canonical_serialization_golden_vectors.CreateSemanticModelV8()`: pinned source and stream names, a source without an identifier type, keyed and unkeyed streams, UUID/text/integer routes (including adjacent values at both Double bounds), a literal key, and a command with both response and route. Separate builder methods add specification routes, `unrouted` expectations and composite identities; both conditional parts joined v8 at the claim.

`full-esm-v10.json` pins released ESM v10: reaction system identity under 0043, production route overrides under 0061, and observer filters under 0062 and 0063. V10 first shipped in Screenplay v4.125.0; the 0063 extension shipped in v4.129.0 and is part of frozen v10 under 0066's retroactive exception. `canonical_serialization_golden_vectors.CreateSemanticModelV10()` builds on the full v9 model and the reaction-identity fixture, adding scalar, composite and standalone production overrides, source-only and source/stream reaction filters, and a reducer filter. The shared golden retains reactions with ordinally sorted roles and with no roles. `ReactionIdentityCorpus.V10`, `EventRoutesCorpus.ProductionRoutesV10` and `EventRoutesCorpus.ObserverFiltersV10` separately pin source-backed bytes and outcomes across single-file, folder, reordered and relocated forms. A model selects v10 when it uses any of these three features. Models without them keep their earlier bytes.

`full-esm-v11.json` pins released ESM v11 (first shipped in Screenplay 4.135.0) under 0064, 0025 and 0066. `canonical_serialization_golden_vectors.CreateSemanticModelV11()` adds an event production carrying exact Double-mode integers and a shortest round-tripping fraction to the full v10 model. A retained non-route literal selects v11 only when its lowered decimal differs detectably from legacy conversion; unused examples, overridden values and already-lossless route inputs do not count. Programmatic v1–v10 models still accept precise decimals under their existing contracts. `NumberLiteralsCorpus.ExactLiteralsV11` separately pins source-backed bytes and accepted outcomes. The released v1–v10 goldens and corpus vectors remain unchanged.

`full-esm-v12.json` pins enum member numbers under 0065, 0025 and 0066. `canonical_serialization_golden_vectors.CreateSemanticModelV12()` adds sparse and negative int32 enum identities to the full v11 model, retaining exact literal lowering and all earlier contracts. `EnumMemberNumbersCorpus.V12` pins source-backed bytes, revision and name-based accepted outcomes across single-file, folder, reordered and relocated forms. Positional pins bind as absent numbers; only non-positional numbering selects v12. Released v1–v11 goldens and corpus vectors remain unchanged.

`full-esm-v13.json` pins concept compliance under 0068, 0025 and 0066. `canonical_serialization_golden_vectors.CreateSemanticModelV13()` adds personal health/criminal data, a namespace-scoped secret and combined personal/secret markers to the full v12 model. `ConceptComplianceCorpus.V13` pins source-backed bytes, revision and reference outcomes across single-file, folder, reordered and relocated forms. Only markers select v13; reasons and event subject marks add no bytes. Personal takes precedence over secret, and no secret scope is stored next to personal. Identity/enum compliance is refused; the binder also refuses personal or explicitly subject-scoped secret values in an event with an explicit subject mark. Released v1–v12 vectors remain unchanged.

`full-esm-v16.json` pins stored event type names under 0075. `canonical_serialization_golden_vectors.CreateSemanticModelV16()` adds one pinned contract to the full v15 model, retaining unpinned contracts. `StoredEventNamesCorpus.V16` pins a source-backed mixed pinned/unpinned rename and unchanged reference outcomes across equivalent forms. Existing v1–v15 golden bytes are unchanged. Only an event pin that differs from the display name selects v16; an equal-name pin omits `storedName` and preserves the earlier version and bytes.

`full-esm-v17.json` pins explicit no-event assertions and recovery redelivery under 0074. `canonical_serialization_golden_vectors.CreateSemanticModelV17()` adds both specification members to the full v16 model, retaining stored event names and every earlier contract. `RecoverySpecificationsCorpus.V17` pins source-backed bytes, revisions and reference outcomes across equivalent forms. Existing v1–v16 golden bytes are unchanged. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=17`, then rebuild and rerun without the variable.

`full-esm-v18.json` pins query validation rejection specifications under 0072. `canonical_serialization_golden_vectors.CreateSemanticModelV18()` adds actionless query requests with empty `results` and one `thenErrors` entry, both with and without a message, to the full v17 model. `QueryValidationCorpus.V18` pins key and filter rejections across equivalent source forms. Only this specification shape selects v18; concept validation of query arguments applies in every version without changing earlier bytes. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=18`, then rebuild and rerun without the variable. Released v1–v17 vectors remain unchanged.
`full-esm-v19.json` pins invocation refusal branches and String refusal expressions under 0030. `canonical_serialization_golden_vectors.CreateSemanticModelV19()` adds ordered selectors, a named constraint, acknowledgement and all three refusal members to the full v17 model. `InvocationRefusalsCorpus.V19` pins source-backed bytes, revision, branch cascades and acknowledgement with redelivery/no-event assertions across equivalent source forms. Prior golden and corpus bytes stay unchanged. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=19`, then rebuild and rerun without the variable.

The other files pin separate serialization contracts, not ESM bytes:

| File | Source |
| --- | --- |
| `full-expressions-v1.json` | `canonical_serialization_golden_vectors.CreateExpressions()` — expression variants |
| `full-identity-catalog-v1.json` | `canonical_serialization_golden_vectors.CreateIdentityCatalog()` — identity catalog with its own catalog revision |
| `typed-contexts-v1.json` | `when_describing_typed_contexts.GoldenBytes()` — typed-context descriptor sidecars for bound rule, policy, validation and reducer contexts |
| `unbound-handler-context-v1.json` | `when_describing_an_unbound_handler.GoldenBytes()` — typed-context descriptor sidecar for an unbound command handler |

The ESM, expression and identity-catalog sources live in `../given/canonical_serialization_golden_vectors*.cs`;
the typed-context sources live in the binder specs. Specs in this project compare the serialized sources
and descriptor sidecars with these bytes.

`Screenplay.CanonicalVectors.Specs.csproj` links the ESM and sidecar files as embedded resources:
`full-esm-v1.json`, `full-esm-v2.json`, `full-esm-v3.json`, `full-esm-v4.json`, `full-esm-v5.json`, `full-esm-v6.json`, `full-esm-v7.json`,
`full-esm-v8.json`, `full-esm-v9.json`, `full-esm-v10.json`, `full-esm-v11.json`, `full-esm-v12.json`, `full-esm-v13.json`, `policy-negation-v7.json`, `full-identity-catalog-v1.json`, `typed-contexts-v1.json`, and `unbound-handler-context-v1.json`.
It does not link `full-expressions-v1.json`. The vectors project checks ESM and identity-catalog
round trips and the presence of both descriptor sidecars. Do not edit the files by hand.

`full-esm-v22.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV22()` on the full v21 model, adding only the optional final application member `reactionProductionMode: "atomic"` under decision 0078. `AtomicReactionBatchesCorpus.V22` and its legacy control pin equivalent-source bytes and reference outcomes, including rejected-world counts, refusal branches, redelivery, application and clock triggers. Earlier golden/corpus bytes remain unchanged. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=22`; regenerate the source corpus with `SCREENPLAY_REGENERATE_ATOMIC_REACTION_BATCHES_CORPUS=1`. Both selectors fail deliberately; rebuild embedded resources before strict reruns.

`full-esm-v25.json` pins the protected command reads contract under decision 0085. `canonical_serialization_golden_vectors.CreateSemanticModelV25()` binds a required read over a finite event-source-keyed projection with removal handling, a read-backed requirement and a produced-event read-path mapping. The final application member is `commandReadMode: "protected"`; each read writes `name`, `readModel`, `key` and `presence: "required"`. Read-path expressions write `kind: "readPath"`, `read` and `property`; condition property operands add `read` for a view property. `ProtectedCommandReadsCorpus.V25` pins equivalent single, folder, reordered and relocated source forms and their reference outcomes, including absence precedence, read paths, removals/recreation and invoked-command reads. Its identical-source legacy control removes only the preamble and pins refusal diagnostics rather than fabricating an executable model. Evaluator vectors separately cover decide/commit races and `Conflict` / `DecisionStateChanged`. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=25`; regenerate the source corpus and legacy diagnostics with `SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS_V25=1`. Both selectors fail deliberately; review, rebuild embedded resources and rerun strictly without them. Released v1–v24 bytes remain unchanged.
`full-esm-v23.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV23()` on the full v22 model, adding a described command with `handler: { requirementId, resultVersion }` and an unmapped required event property under decision 0082. Existing models without a described handler keep their version and bytes. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=23`; the run fails deliberately, then embedded resources must be rebuilt before a strict rerun.

`full-esm-v24.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV24()` on the full v23 model, adding all four first-class ordinal text-rule discriminators. The allocation is held in `TextRuleVersion.Major`. `TextRulesCorpus.SourceForms` separately compares deprecated Studio lookaround source with first-class source, including identical canonical bytes and accepted/rejected reference specifications. Prior golden bytes remain unchanged. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=24`; rebuild and rerun without the switch after reviewing the intentional failure.

`full-esm-v27.json` pins flat composite read-model identity under decision 0088: several identifier properties, sorted query `keyParts`, untyped projection keys of kind `parts`, fixture identities and keyed absence. `canonical_serialization_golden_vectors.CreateSemanticModelV27()` binds a source fixture with lookup, removal and recreation specifications. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=27`, review the deliberate failure and diff, rebuild embedded resources and rerun strictly. The working allocation is `CompositeReadModelKeyVersion.Major`; the interoperability row is added only at admission after the preceding version lands.

`CompositeReadModelKeysCorpus.V27` pins Double-mode source forms, including reordered key declarations and query parts. Its same-source exact-mode control remains refused with `PLAY0268` naming #285. Regenerate its expected bytes and revision with `SCREENPLAY_REGENERATE_COMPOSITE_READ_MODEL_KEYS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug --filter FullyQualifiedName~when_loading_composite_read_model_keys`. The run deliberately fails; review, rebuild and rerun without the variable. Neither switch permits editing an existing golden by hand.

`full-esm-v26.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV26()` under decision 0087. It pins command Date/DateTime conditions and validation rules, with text literals typed from the property and canonicalized to calendar dates or UTC tick-precision instants. `DateComparisonsCorpus.V26` separately pins equivalent file/folder forms, UTC ordering across offsets, instant equality, distinct ticks and calendar production guards. Other equality and all earlier golden bytes stay unchanged. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=26`; regenerate the source corpus with `SCREENPLAY_REGENERATE_DATE_COMPARISONS_CORPUS_V26=1`. Both fail deliberately; review, rebuild and rerun strictly without them.

## Regenerating

When a deliberate contract change alters the canonical bytes, change the source model first, then run:

```bash
SCREENPLAY_REGENERATE_GOLDEN=1 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug
```

For a v3-, v4-, v5-, v6-, v7- or v8-only regeneration, use `SCREENPLAY_REGENERATE_GOLDEN=3`, `4`, `5`, `6`, `7` or `8` with the same command. Use `8` for v8 changes so the v1–v7 files remain untouched. The `1` setting rewrites all eleven files; all modes fail on purpose with
`GoldenVectorsRegenerated`, so it can never pass silently in CI. Review the diff, then rebuild and rerun
without the variable - the bytes are embedded at build time.

Regenerate the policy-negation extension independently of the original goldens and corpora:

```bash
SCREENPLAY_REGENERATE_POLICY_NEGATION=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_PolicyNegationCorpus
```

This writes the extension golden and its corpus bytes/revision, then fails deliberately. Review, rebuild and rerun without the variable; existing fixtures must remain unchanged.

Regenerate only the event routes golden, leaving released goldens and corpora untouched:

```bash
SCREENPLAY_REGENERATE_EVENT_ROUTES=1 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug --filter FullyQualifiedName~when_round_tripping_the_event_routes_golden
```

This deliberately fails with `GoldenVectorsRegenerated`. Review the generated bytes, rebuild, and rerun without the variable. The v8 golden is embedded by the compiler's Debug build; the vectors project links it separately.

Regenerate the shared v10 golden or the reaction identity corpus separately, preserving released vectors:

```bash
SCREENPLAY_REGENERATE_GOLDEN=10 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug --filter FullyQualifiedName~when_checking_for_a_regeneration_request
SCREENPLAY_REGENERATE_REACTION_IDENTITY_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_ReactionIdentityCorpus
```

Each command deliberately fails after rewriting its owned vectors. Review the bytes, rebuild, then rerun without the variables.

Regenerate only the v11 golden or exact-number corpus, leaving released v1–v10 vectors untouched:

```bash
SCREENPLAY_REGENERATE_GOLDEN=11 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug --filter FullyQualifiedName~when_checking_for_a_regeneration_request
SCREENPLAY_REGENERATE_NUMBER_LITERALS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_NumberLiteralsCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

Regenerate only the v12 golden or enum member numbers corpus, leaving released v1–v11 vectors untouched:

```bash
SCREENPLAY_REGENERATE_GOLDEN=12 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug --filter FullyQualifiedName~when_checking_for_a_regeneration_request
SCREENPLAY_REGENERATE_ENUM_MEMBER_NUMBERS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_EnumMemberNumbersCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

Regenerate only the v13 golden or concept compliance corpus, leaving released v1–v12 vectors untouched:

```bash
SCREENPLAY_REGENERATE_GOLDEN=13 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug --filter FullyQualifiedName~when_checking_for_a_regeneration_request
SCREENPLAY_REGENERATE_CONCEPT_COMPLIANCE_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug --filter FullyQualifiedName~for_ConceptComplianceCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

ESM vectors carry a semantic revision; the identity catalog carries its own catalog revision. Expression
and typed-context descriptor vectors are separate contracts, not ESM documents with semantic revisions.
An ESM serialization change can also change the expected ESM bytes and semantic revision of the corpus in
`Screenplay.CanonicalCorpus`, which this mechanism does not rewrite. Stage compares its output against that
corpus, so `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` must be released together for Stage.
