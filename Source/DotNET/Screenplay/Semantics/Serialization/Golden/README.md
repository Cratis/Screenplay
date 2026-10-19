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

Full goldens below the current ESM version are frozen: v1–v27 are checked-in contracts, not rebuilt
from shared fixtures. Their version specs deserialize the frozen bytes, check the selected version and
feature-specific assertions, and serialize back to identical bytes. Only v28 is compared with its builder.
The historical fixture descriptions below explain what each vector covers; retained builders serve
other specs, not regeneration of older full goldens.

`given/frozen_esm_goldens.cs` records the SHA-256 of each frozen file's exact bytes.
`when_checking_frozen_full_goldens` requires exactly v1 through v(current−1), checks that every file exists,
and hashes the source files without JSON normalization. It reports 27 checked files at v28; even a
one-byte edit fails. Restore changed frozen bytes rather than updating their hashes.

These source models originally produced the full vectors:

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

`full-esm-v17.json` pins explicit no-event assertions and recovery redelivery under 0074. `canonical_serialization_golden_vectors.CreateSemanticModelV17()` adds both specification members to the full v16 model, retaining stored event names and every earlier contract. `RecoverySpecificationsCorpus.V17` pins source-backed bytes, revisions and reference outcomes across equivalent forms. Existing v1–v16 golden bytes are unchanged.

`full-esm-v18.json` pins query validation rejection specifications under 0072. The frozen v18 model adds actionless query requests with empty `results` and one `thenErrors` entry, both with and without a message, to the full v17 model. `QueryValidationCorpus.V18` pins key and filter rejections across equivalent source forms. Only this specification shape selects v18; concept validation of query arguments applies in every version without changing earlier bytes. Released v1–v17 vectors remain unchanged.
`full-esm-v19.json` pins invocation refusal branches and String refusal expressions under 0030. `canonical_serialization_golden_vectors.CreateSemanticModelV19()` adds ordered selectors, a named constraint, acknowledgement and all three refusal members to the full v17 model. `InvocationRefusalsCorpus.V19` pins source-backed bytes, revision, branch cascades and acknowledgement with redelivery/no-event assertions across equivalent source forms. Prior golden and corpus bytes stay unchanged.

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

`full-esm-v22.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV22()` on the full v21 model, adding only the optional final application member `reactionProductionMode: "atomic"` under decision 0078. `AtomicReactionBatchesCorpus.V22` and its legacy control pin equivalent-source bytes and reference outcomes, including rejected-world counts, refusal branches, redelivery, application and clock triggers. Earlier golden/corpus bytes remain unchanged. The full v22 golden is frozen. Regenerate the separate source corpus with `SCREENPLAY_REGENERATE_ATOMIC_REACTION_BATCHES_CORPUS=1`; it fails deliberately, so rebuild embedded resources before strict reruns.

`full-esm-v25.json` pins the protected command reads contract under decision 0085. `canonical_serialization_golden_vectors.CreateSemanticModelV25()` binds a required read over a finite event-source-keyed projection with removal handling, a read-backed requirement and a produced-event read-path mapping. The final application member is `commandReadMode: "protected"`; each read writes `name`, `readModel`, `key` and `presence: "required"`. Read-path expressions write `kind: "readPath"`, `read` and `property`; condition property operands add `read` for a view property. `ProtectedCommandReadsCorpus.V25` pins equivalent single, folder, reordered and relocated source forms and their reference outcomes, including absence precedence, read paths, removals/recreation and invoked-command reads. Its identical-source legacy control removes only the preamble and pins refusal diagnostics rather than fabricating an executable model. Evaluator vectors separately cover decide/commit races and `Conflict` / `DecisionStateChanged`. The full v25 golden is frozen. Regenerate the separate source corpus and legacy diagnostics with `SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS_V25=1`; it fails deliberately, so review, rebuild embedded resources and rerun strictly without it. Released v1–v24 bytes remain unchanged.
`full-esm-v23.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV23()` on the full v22 model, adding a described command with `handler: { requirementId, resultVersion }` and an unmapped required event property under decision 0082. Existing models without a described handler keep their version and bytes. The full v23 golden is frozen.

`full-esm-v24.json` was built on the full v23 model, adding all four first-class ordinal text-rule discriminators. The allocation is held in `TextRuleVersion.Major`. `TextRulesCorpus.SourceForms` separately compares deprecated Studio lookaround source with first-class source, including identical canonical bytes and accepted/rejected reference specifications. Prior golden bytes remain unchanged. The full v24 golden is frozen.

`full-esm-v28.json` pins command Uuid equality under decision 0090: `==` and `!=` between Uuid command properties, concept-typed properties and a protected read path in `require`, a canonical lowercase hyphenated literal that stays a text constant typed from the other operand, and a `produces when` comparison. `canonical_serialization_golden_vectors.CreateSemanticModelV28()` binds `a_uuid_comparison.GoldenSource`. `UuidComparisonsCorpus.V28` separately pins equivalent file/folder forms and the reference specification outcomes. Regenerate only this golden with `SCREENPLAY_REGENERATE_GOLDEN=28`, review the deliberate failure and diff, rebuild embedded resources and rerun strictly; the corpus expected bytes and revision are rewritten only by `SCREENPLAY_REGENERATE_UUID_COMPARISONS_CORPUS_V28=1` on `when_loading_v28_source`. v28 includes released v27 and is selected only when a model uses an admitted Uuid comparison; every earlier golden stays frozen.

`full-esm-v29.json` pins the Acme Commerce admissions of decision 0098: a `decimalPlaces` rule (`decimals <= N`) on a concept and a command property, a projection `add` whose operand is the product of two event values (the optional mapping `factor`), and an event property mapped from one concept to another over the same primitive. It is built by `canonical_serialization_golden_vectors.CreateSemanticModelV29()` and regenerated with `SCREENPLAY_REGENERATE_GOLDEN=29`. `AcmeAdmissionsCorpus.V29` pins the same constructs through source-backed execution. The v28 golden above is frozen.

`full-esm-v30.json` pins the second Acme Commerce admissions: a command whose second production appends to a source named by a non-identifier command property, and a specification `givenGenerated` pinning the generated identifier a reaction-invoked command takes. It is built by `canonical_serialization_golden_vectors.CreateSemanticModelV30()` and regenerated with `SCREENPLAY_REGENERATE_GOLDEN=30`. `AcmeSecondAdmissionsCorpus.V30` pins the same constructs through source-backed execution. The v29 golden above is frozen.

`full-esm-v27.json` pins flat composite read-model identity under decision 0088: several identifier properties, sorted query `keyParts`, untyped projection keys of kind `parts`, fixture identities and keyed absence. The frozen v27 model comes from a source fixture with lookup, removal and recreation specifications. The full v27 golden is frozen. The allocation is `CompositeReadModelKeyVersion.Major`; v27 includes released v26 and shipped in v4.204.0.

`CompositeReadModelKeysCorpus.V27` pins Double-mode source forms, including reordered key declarations and query parts. Its same-source exact-mode control remains refused with `PLAY0268` naming #285. Regenerate its expected bytes and revision with `SCREENPLAY_REGENERATE_COMPOSITE_READ_MODEL_KEYS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug --filter FullyQualifiedName~when_loading_composite_read_model_keys`. The run deliberately fails; review, rebuild and rerun without the variable. Corpus regeneration does not permit editing the frozen full v27 golden.

`full-esm-v26.json` is built by `canonical_serialization_golden_vectors.CreateSemanticModelV26()` under decision 0087. It pins command Date/DateTime conditions and validation rules, with text literals typed from the property and canonicalized to calendar dates or UTC tick-precision instants. `DateComparisonsCorpus.V26` separately pins equivalent file/folder forms, UTC ordering across offsets, instant equality, distinct ticks and calendar production guards. Other equality and all earlier golden bytes stay unchanged. The full v26 golden is frozen. Regenerate the separate source corpus with `SCREENPLAY_REGENERATE_DATE_COMPARISONS_CORPUS_V26=1`; it fails deliberately, so review, rebuild and rerun strictly without it.

## Regenerating

From a source checkout, update the current version's source model only for an intentional contract change, then run:

```bash
SCREENPLAY_REGENERATE_GOLDEN=1 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug
```

`1` rewrites only the current full ESM golden (`full-esm-v28.json`) plus the four non-ESM sidecars:
expressions, identity catalog, typed contexts and unbound handler context. Use `SCREENPLAY_REGENERATE_GOLDEN=28`
to rewrite only the current full ESM golden. Older version selectors fail with a frozen-golden message
before writing anything. The legacy `SCREENPLAY_REGENERATE_EVENT_ROUTES=1` switch also fails because v8 is frozen.
Regeneration deliberately throws `GoldenVectorsRegenerated`; review the diff, rebuild embedded resources,
and rerun without the variable. None of these switches rewrites the frozen SHA-256 table or source corpora.

When a new ESM version is added:

1. Preserve the previous current golden byte for byte and add its SHA-256 to `given/frozen_esm_goldens.cs`.
   Never replace an existing pin to accept a changed file.
2. Add the new full golden and current builder, and point regeneration at that builder. The helper derives
   the current version from the declared `SemanticVersion` fields and refuses a builder selecting an older version.
3. Switch the previous current spec to frozen-byte round trips, keep its feature assertions, and remove only
   builders with no remaining consumers. The manifest check must cover exactly v1..v(new current−1).
4. Run the compiler and canonical-vector specs. Keep all full goldens and published corpus bytes available.

Regenerate the policy-negation extension independently of the original goldens and corpora:

```bash
SCREENPLAY_REGENERATE_POLICY_NEGATION=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_PolicyNegationCorpus
```

This writes the extension golden and its corpus bytes/revision, then fails deliberately. Review, rebuild and rerun without the variable; existing fixtures must remain unchanged.

The source-backed corpora have independent regeneration switches; they do not rewrite frozen full goldens.
For the reaction identity corpus:

```bash
SCREENPLAY_REGENERATE_REACTION_IDENTITY_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_ReactionIdentityCorpus
```

Each command deliberately fails after rewriting its owned vectors. Review the bytes, rebuild, then rerun without the variables.

For the exact-number corpus:

```bash
SCREENPLAY_REGENERATE_NUMBER_LITERALS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_NumberLiteralsCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

For the enum member numbers corpus:

```bash
SCREENPLAY_REGENERATE_ENUM_MEMBER_NUMBERS_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug -f net10.0 --filter FullyQualifiedName~for_EnumMemberNumbersCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

For the concept compliance corpus:

```bash
SCREENPLAY_REGENERATE_CONCEPT_COMPLIANCE_CORPUS=1 dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj -c Debug --filter FullyQualifiedName~for_ConceptComplianceCorpus
```

Each command deliberately fails after regeneration. Review the bytes, rebuild and rerun without the variables.

ESM vectors carry a semantic revision; the identity catalog carries its own catalog revision. Expression
and typed-context descriptor vectors are separate contracts, not ESM documents with semantic revisions.
An ESM serialization change can also change the expected ESM bytes and semantic revision of the corpus in
`Screenplay.CanonicalCorpus`, which this mechanism does not rewrite. Stage compares its output against that
corpus, so `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` must be released together for Stage.
