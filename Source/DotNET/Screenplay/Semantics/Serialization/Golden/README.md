# Golden vectors

These files are the checked-in canonical bytes of the ESM serialization contract:

| File | Source model |
| --- | --- |
| `full-esm-v1.json` | `canonical_serialization_golden_vectors.CreateSemanticModel()` |
| `full-esm-v2.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV2()` — typed destination, explicit specification event sources, and an audit identity occurrence mapping |
| `full-esm-v3.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV3()` — the full v2 model plus a reducer-built read model with opaque transitions, a rule predicate, command code validation, and an opaque policy predicate |
| `full-esm-v4.json` | `canonical_serialization_golden_vectors.CreateSemanticModelV4()` — mixed ordinary and three-generation events, with historical tags and v4 transition cardinality |
| `full-expressions-v1.json` | `canonical_serialization_golden_vectors.CreateExpressions()` |
| `full-identity-catalog-v1.json` | `canonical_serialization_golden_vectors.CreateIdentityCatalog()` |
| `typed-contexts-v1.json` | `when_describing_typed_contexts.GoldenBytes()` — bound rule, policy and reducer contexts |
| `unbound-handler-context-v1.json` | `when_describing_an_unbound_handler.GoldenBytes()` — unbound command handler context |

The ESM source models live in `../given/canonical_serialization_golden_vectors.cs`; the typed-context sources live in the binder specs. The specs in this project
compare serialized source models and sidecars with these bytes, and `Screenplay.CanonicalVectors.Specs` links the same
files for consumers and checks the ESM vectors round-trip unchanged. Do not edit the files by hand.

## Regenerating

When a deliberate contract change alters the canonical bytes, change the source model first, then run:

```bash
SCREENPLAY_REGENERATE_GOLDEN=1 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug
```

For a v3-only or v4-only regeneration, use `SCREENPLAY_REGENERATE_GOLDEN=3` or `SCREENPLAY_REGENERATE_GOLDEN=4` with the same command. The `1` setting rewrites all eight files; all modes fail on purpose with
`GoldenVectorsRegenerated`, so it can never pass silently in CI. Review the diff, then rebuild and rerun
without the variable - the bytes are embedded at build time.

Every change to the canonical bytes changes the revision hashes inside them. A change to canonical
serialization also changes the expected ESM bytes and semantic revision of the corpus in
`Screenplay.CanonicalCorpus`, which this mechanism does not rewrite. Stage compares its output against that
corpus, so `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` must be released together for Stage.
