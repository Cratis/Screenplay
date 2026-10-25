# Semantic conformance test support

`Cratis.Screenplay.Conformance` is a prerelease, source-only NuGet package for
renderer and source-adapter specification projects. It compiles internal types
into the consuming project; it has no runtime assembly or public binary API.
It is test support, not a Screenplay identity or wire-format replacement.

```xml
<PackageReference Include="Cratis.Screenplay.Conformance" Version="VERSION-preview" PrivateAssets="all" />
```

Use the same Screenplay version as the renderer under test. The sources require
C# 12 or later, nullable annotations, and .NET 8 or later. No test framework is
required by the packaged sources.

## Compare meaning rather than assigned identities

Compile the original and recovered `.play` sources to executable semantic models,
then compare them:

```csharp
using Cratis.Screenplay.Conformance;

var comparison = SemanticModelEquivalence.Compare(originalModel, recoveredModel);
Assert.True(comparison.IsEquivalent, string.Join("\n", comparison.Differences));
```

The equivalence rules are:

- N1: only canonical executable semantic model JSON is compared. Source spans,
  documents, display paths, the catalog and source map are outside that model.
  No generated backend source is normalized or compared.
- N2: drop the top-level `revision` and catalog-revision (`catrev1:`) values.
  Keep schema and language/semantic versions, contract revisions, predecessors,
  prior revisions and event `origin`. The latter is the foreign store name of a
  public event, not identity-assignment provenance.
- N3: use two passes. First map application/semantic `id` definitions to structural
  keys (`/application`, then parent, collection and name); map `contractId` to
  `ownerKey#contract`. Then replace identity references with `@key` and requirement
  identities (`requirementId` and `requirement`) with `ownerKey#requirement`.
  An unmapped identity fails with its JSON path.
- N4: re-sort set collections by structural key after replacing identities;
  preserve the order of ordered collections.
- N5: compare every other scalar exactly, including case, code, number format
  and whitespace inside strings. There is no fuzzy matching.

Declaration collections are sets, sorted by stable structural keys. Ordered
collections, including command `produces`, projection transitions and
specification steps, retain their order. `SemanticArrayClassification` is an
explicit table of owner/property pairs; an unknown array fails rather than
silently being treated as a set. The complete owner/property classification is
in [SemanticArrayClassification](https://github.com/Cratis/Screenplay/blob/main/Source/DotNET/Screenplay.Conformance/SemanticArrayClassification.cs).
This includes value collections keyed by the referenced `targetProperty`, reducer
transitions keyed by `eventContract`, and key parts keyed by `property`; these
reference-keyed sets are sorted again after identity replacement.
Unnamed or duplicate siblings receive `~n` keys ranked
by identity-stripped canonical content; `FallbackKeys` exposes every use. The
canonical corpus guard currently requires no fallbacks.

Differences are `Missing`, `Unexpected` or `Changed`. Paths use `/` for members,
`[name]` for set elements and `[n]` for ordered elements. For example:

```text
/application/modules[Projects]/features[Registration]/slices[Register]/commands[Register]/properties[name]
```

## Reconcile known losses

A target owns its JSON ledger and supplies observations; this package does not
render, invoke an adapter, compile recovered sources or choose fixture eligibility.
A minimal ledger is:

```json
{
  "schema": "cratis.screenplay.loss-ledger/v1",
  "target": "cratis-arc",
  "corpusVersion": "4.241.0",
  "minimumFixtureCount": 25,
  "budget": {
    "maximumEntries": 20,
    "maximumByDesign": 5,
    "maximumExclusions": 2
  },
  "excludedFixtures": {},
  "losses": []
}
```

Each loss has a `fixture`, `change` (`missing`, `unexpected`, `changed`,
`uncompilable` or `diagnostic`), exact `path`, `direction` (`render` or `adapter`),
and `diagnostics` array. It must carry exactly one of `issue` (an `owner/repo#123`
reference) or a nonempty `byDesign` reason. Wildcard paths, unknown members,
duplicate entries and malformed issue references are rejected. Silent losses
must carry an issue, never `byDesign`.

```csharp
using var stream = File.OpenRead("semantic-loss-ledger.json");
var ledger = SemanticLossLedger.Load(stream);
var verdict = ledger.Reconcile(observations, requireCompleteRun: true, eligibleFixtures);
Assert.True(verdict.IsClean, string.Join("\n", verdict.Findings));
```

An observation records its fixture, comparison (null when recovery failed to
compile), direction-scoped renderer/adapter diagnostics, and recovery compiler
codes. Diagnostic severity uses `RoundTripDiagnosticSeverity`. A verdict reports
all findings, not just the first:

- R1: differences must match fixture, change kind and exact path.
- R2: an entry matching nothing is stale and must be removed.
- R3: every cited diagnostic must have occurred for its direction. Compiler codes
  explain only `uncompilable` entries.
- R4: every warning or error must be cited; informational diagnostics need not be.
- R5: malformed entries fail, including silent losses accepted by design.
- R6: entry, by-design and exclusion counts may not exceed explicit budgets.
- R7: even a partial run must execute a fixture. A complete run must also meet its
  minimum, compare at least one recovered model and supply the eligible fixture
  set. Unobserved eligible fixtures and orphaned entries fail. Exclusions need a
  reason; obsolete exclusions and exclusions outside eligibility fail.

Partial runs allow focused debugging without applying the full-run minimum or
requiring unrelated fixtures. They are not a substitute for the complete gate.
The ledger's `Problems` and the verdict's `Malformed` findings keep parse and
shape failures visible; check `IsClean` rather than only counting differences.
