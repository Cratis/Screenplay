# Golden vectors

These files are the checked-in canonical bytes of the ESM serialization contract:

| File | Source model |
| --- | --- |
| `full-esm-v1.json` | `canonical_serialization_golden_vectors.CreateSemanticModel()` |
| `full-expressions-v1.json` | `canonical_serialization_golden_vectors.CreateExpressions()` |
| `full-identity-catalog-v1.json` | `canonical_serialization_golden_vectors.CreateIdentityCatalog()` |

The source models live in `../given/canonical_serialization_golden_vectors.cs`. The specs in this project
compare the serialized source models with these bytes, and `Screenplay.CanonicalVectors.Specs` links the same
files and checks that they round-trip unchanged. Do not edit the files by hand.

## Regenerating

When a deliberate contract change alters the canonical bytes, change the source model first, then run:

```bash
SCREENPLAY_REGENERATE_GOLDEN=1 dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug
```

The run rewrites the three files from their source models and then fails on purpose with
`GoldenVectorsRegenerated`, so it can never pass silently in CI. Review the diff, then rebuild and rerun
without the variable - the bytes are embedded at build time.

Every change to the canonical bytes changes the revision hashes inside them. A change to canonical
serialization also changes the expected ESM bytes and semantic revision of the corpus in
`Screenplay.CanonicalCorpus`, which this mechanism does not rewrite. Stage compares its output against that
corpus, so `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` must be released together for Stage.
