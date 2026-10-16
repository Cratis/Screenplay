---
title: Check a concept declaration
description: Inspect generation options, rule proofs and exact support for one concept without compiling an application.
---

An editor can check a concept before it has an application or source text. Use `SemanticConceptCheck.Check` from the `Cratis.Screenplay` .NET library instead of copying generation rules or attributing compiler diagnostics by line number. This checks a declaration; use [value-rule checks](value-rule-checks.md) to check an actual value.

## Check syntax built by an editor

The syntax overload checks generation arguments, modifier applicability and values as well as binding and generation-rule consistency. It accepts nodes whose locations are all `SourceLocation.Start`:

```csharp
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

var concept = new ConceptSyntax("InviteCode", "String", [], [], SourceLocation.Start)
{
    Generation = new("nanoid", SourceLocation.Start) { Size = 8, Alphabet = "ab" }
};
var check = SemanticConceptCheck.Check(concept, utcAssessmentYear: 2026);
Console.WriteLine(check.Support!.Count);
Console.WriteLine(check.Support.MeetsIdentifierMinimum);
```

Output:

```text
256
False
```

The low support is data, not an error on the concept. `PLAY0683` and `PLAY0684` depend on the command property using it: whether it is an identifier and whether a unique constraint covers its mapped event property. This API does not invent a command or suppress those checks when you later bind the application.

## Overloads and assessment year

Both overloads live in `Cratis.Screenplay.Semantics` and return `SemanticConceptCheckResult`:

| Call | Input |
| --- | --- |
| `Check(ConceptSyntax concept, int? utcAssessmentYear, bool assumeGenerated = false)` | An authored or editor-built declaration. No source document is required. |
| `Check(SemanticConcept concept, int? utcAssessmentYear, bool assumeGenerated = false)` | A semantic concept, such as one read from an executable model. Checks generation contracts, pattern admission and generation-rule proofs; it does not rebind other semantic contracts. |

Pass the UTC assessment year explicitly for reproducible support counts and witnesses. `null` uses `DateTimeOffset.UtcNow.Year`. The year must be 1–9899, leaving room for the complete inclusive 101-year date horizon. Invalid years return `PLAY0673` with no assessment. Neither the year nor the results enter ESM bytes.

An explicit strategy is assessed automatically. Without a strategy, set `assumeGenerated: true` only when a Uuid concept is used by a generated property. Otherwise no generation support or proofs are produced for the implicit UUID default. This flag never invents UUID generation for an ordinary String or Int concept.

## Results and finding targets

| Member | Meaning |
| --- | --- |
| `Concept` | The inspected semantic concept, potentially partial when findings report errors; null when inspection could not bind it. It is not proof of admission. |
| `Findings` | Existing compiler codes, severity and messages, attributed to constructs rather than source locations. |
| `Proofs` | `RuleConsistencyResult` entries parallel to `Concept.Validations` when generation is assessable; otherwise empty. They describe Proven, Refuted or Unproven, with a witness or reason. |
| `Support` | Exact minimum conditioned `Count`, display `Bits`, nullable `MeetsIdentifierMinimum` and approximate `ValuesAtOnePercentCollision`. Null for invalid or non-generated declarations; opaque generation has a support object whose members are null. |
| `RequiredVersion` | The minimum ESM version required by this inspected concept alone, not the surrounding application. |

A `SemanticConceptFinding` carries `Code`, `Severity`, `Message` and `Target`. For `Option`, `Option` names the source option. For `Rule`, `RuleIndex` is zero-based across validation blocks in declaration order, counting each declarative rule, code-validation block and refused `require` clause. A semantic input uses its validation-array order. Generation-wide findings have neither index nor option. An unknown target is not a successful check.

`Pattern` carries a `PortableMatchCheck` for match-pattern findings, including the UTF-16 offset into the decoded pattern. Invalid syntax reports `PLAY0367`; refused portable constructs report `PLAY0699`; recognized deprecated Studio shapes keep their replacement information. There are no line or column coordinates to translate back into the editor's model.

Generation-rule refutations remain `PLAY0688` errors regardless of the authored rule severity. `PLAY0689` is an Unproven warning, never a refutation or a proof. Read [rule consistency](generated-values/strategies.md#rule-consistency) for proof limits. With malformed syntax, the inspected semantic rules can be only a subset of the authored rules; findings retain their original rule indices while proofs follow the inspected semantic array.

Keep `Count` as a `BigInteger`; do not round it through floating point. Identifier support compares the exact count to `2^GenerationStrategyCatalog.IdentifierMinimumBits`. Unknown entropy does not meet or fail that threshold: its result is null. The collision estimate can be positive infinity for enormous counts; use `GenerationEntropy.CollisionCountText(bits)` when finite JSON-number text is needed.

## Scope and readiness

Use the [public strategy catalog](generated-values/strategies.md#public-strategy-catalog) to discover allowed options before authoring. Use this check to explain a particular declaration. The syntax tree cannot represent repeated modifier tokens, so duplicate-modifier diagnostics still belong to source parsing.

The syntax overload assigns private assessment identities, not the eventual application's concept or implementation identities. The semantic overload preserves the supplied concept. Do not persist syntax-check identities as application contracts.

A check does not validate the enclosing application, its references, generated-property eligibility, uniqueness, authorization or implementation linkage. It never invokes a generation or validation body, never supplies a value, and never establishes runtime readiness. Bind and verify the full model before treating it as executable. A reserved `sequence` still reports `PLAY0682`; a generation rule still requires an implementation and has unknown entropy.
