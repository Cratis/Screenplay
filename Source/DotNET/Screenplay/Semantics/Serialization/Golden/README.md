# Golden vectors

The `full-esm-*.json` files pin canonical executable semantic model (ESM) bytes, including each model's semantic revision:

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

`full-esm-v10.json` pins the single claimed, unreleased ESM v10: reaction system identity under 0043, production route overrides under 0054, and observer filters under 0055 and 0056. `canonical_serialization_golden_vectors.CreateSemanticModelV10()` builds on the full v9 model and the reaction-identity fixture, adding scalar, composite and standalone production overrides, source-only and source/stream reaction filters, and a reducer filter. The shared golden retains reactions with ordinally sorted roles and with no roles. `ReactionIdentityCorpus.V10`, `EventRoutesCorpus.ProductionRoutesV10` and `EventRoutesCorpus.ObserverFiltersV10` separately pin source-backed bytes and outcomes across single-file, folder, reordered and relocated forms. A model selects v10 when it uses any of these three features. Models without them keep their earlier bytes.

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
`full-esm-v8.json`, `full-esm-v9.json`, `full-esm-v10.json`, `policy-negation-v7.json`, `full-identity-catalog-v1.json`, `typed-contexts-v1.json`, and `unbound-handler-context-v1.json`.
It does not link `full-expressions-v1.json`. The vectors project checks ESM and identity-catalog
round trips and the presence of both descriptor sidecars. Do not edit the files by hand.

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

ESM vectors carry a semantic revision; the identity catalog carries its own catalog revision. Expression
and typed-context descriptor vectors are separate contracts, not ESM documents with semantic revisions.
An ESM serialization change can also change the expected ESM bytes and semantic revision of the corpus in
`Screenplay.CanonicalCorpus`, which this mechanism does not rewrite. Stage compares its output against that
corpus, so `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` must be released together for Stage.
