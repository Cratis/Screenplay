---
title: Check values against validation rules
description: Check semantic values against concept rules without creating an execution plan or copying the reference evaluator.
---

Use `SemanticValidationRules.Check` from the `Cratis.Screenplay` .NET library when you already have semantic model objects and need the same value-rule meaning as the reference evaluator. You do not need a command, query or execution plan. This API checks rules only: it does not validate an application's structure, authorize a caller, allocate generated values or execute opaque code.

## Overloads

All types below are in `Cratis.Screenplay.Semantics`; collections use `System.Collections.Immutable`.

| Call | Result |
| --- | --- |
| `Check(SemanticValidationRule rule, SemanticValue value)` | One `SemanticRuleCheck`. |
| `Check(SemanticConcept concept, SemanticValue value)` | An `ImmutableArray<SemanticRuleCheck>` in rule declaration order, with the concept identity on each result. |
| `Check(SemanticApplication application, SemanticTypeReference type, SemanticValue value)` | An `ImmutableArray<SemanticRuleCheck>` walking concept values through collections and composites. |

Pass contract-validated rules, types and values, such as those from a bound or deserialized executable semantic model. The application overload resolves concept and composite identities from that application. It traverses array elements and supplied composite properties in value order, then each concept's rules in declaration order. A primitive contributes no rules. An empty or absent collection, or an absent composite, contributes no values to check. An absent scalar concept still checks its rules.

This standalone example checks an invoice-number prefix:

```csharp
using Cratis.Screenplay.Semantics;

var rule = new SemanticValidationRule(
    default,
    SemanticValidationRuleKind.StartsWith,
    SemanticValue.Text("INV-"),
    "An invoice number must start with INV-.");
var check = SemanticValidationRules.Check(rule, SemanticValue.Text("PO-42"));
Console.WriteLine(check.Status);
Console.WriteLine(check.Message);
```

Output:

```text
Failed
An invoice number must start with INV-.
```

## Result contract

`SemanticRuleCheck` carries these members:

| Member | Meaning |
| --- | --- |
| `Status` | `NotEvaluable = 0` (also the default), `Satisfied` or `Failed`. Never treat `NotEvaluable` as a pass. |
| `Rule` | The original rule, including operand, severity, authored message and requirement identity. |
| `Message` | A non-nullable `string`: the authored message or the default rejection text, including on satisfied results. For `NotEvaluable`, the reason the rule cannot run. |
| `Concept` | The concept identity for a concept check or application walk; otherwise null. |
| `Pattern` | The `PortableMatchCheck` for a `Matches` rule with a text operand; otherwise null. |

Messages beginning with `$strings.` remain unresolved string keys. Resolve them in the consuming application's locale. Without an authored message, `NotEmpty` uses `A required value is empty.` for a single-rule check and `A required concept value is empty.` for a concept check or application walk. Other defaults describe the bound, comparison, pattern or checksum.

Rule severity is presentation data, not permission to ignore a failure. The reference evaluator rejects failed rules at every severity.

## Rule meanings

Use `SemanticValue.Null` for absence. Every **evaluable** rule except `NotEmpty` is satisfied by absence; presence needs its own rule. Unsupported rules and patterns remain `NotEvaluable` even for an absent value.

| `SemanticValidationRuleKind` | Meaning for a present value |
| --- | --- |
| `NotEmpty` | Rejects empty or Unicode-whitespace-only text and empty arrays. Numbers, booleans and composites are present. |
| `Maximum`, `Minimum` | Inclusive numeric bounds, or bounds on text length. |
| `Equal`, `NotEqual` | Semantic equality or inequality; text comparison is ordinal and case-sensitive. |
| `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` | Numeric comparison against the operand. |
| `Length` | Text length equals the numeric operand. |
| `AllGreaterThan`, `AllGreaterThanOrEqual` | Every collection element satisfies the numeric comparison. An empty collection passes. These rules belong on collection properties, not scalar concepts. |
| `Matches` | Searches using the [portable match dialect](commands.md#portable-match-dialect); `^` and `$` anchor to the whole input. |
| `Checksum` | Text ends in the exact check digits for `luhn`, `mod11` or `mod97`; see [patterns and checksums](generated-values/patterns.md). |
| `Contains`, `NotContains`, `StartsWith`, `EndsWith` | Ordinal, case-sensitive literal text comparisons. |
| `RulePredicate`, `CodeValidation`, `Unknown` | `NotEvaluable`; the reference implementation cannot execute these rules. |

Text comparisons use UTF-16 code units, without culture-dependent casing or Unicode normalization. Length counts code units too: a supplementary character represented by a surrogate pair has length two. Numeric values and operands use exact .NET `decimal`, not binary floating point. Empty text operands are valid: `Contains`, `StartsWith` and `EndsWith` pass for present text, while `NotContains` fails.

A value or operand incompatible with a rule's required kind throws `InvalidSemanticContract`, rather than returning `Failed`. For example, `Length` requires text and a numeric operand. Ordering requires numbers, not text lengths; quantified comparisons require every element to be numeric, even after an earlier comparison fails. Equality requires matching scalar text, number or boolean kinds, or a null operand. Operand kinds are checked even for absent values and empty collections. This is not a general type-validation API; validate the input contract before checking rules.

## Patterns and older models

`Matches` calls [the public pattern admission check](commands.md#checking-patterns-before-authoring) before matching. Admitted patterns run on Screenplay's deterministic linear matcher, with no platform regex or timeout. Invalid patterns and constructs outside the dialect return `NotEvaluable`; `Pattern` carries the diagnostic code, construct, offset and suggestion, and `Message` carries the admission explanation.

Recognized deprecated Studio lookaround shapes are evaluated as their first-class replacement, including in older ESM documents. `Rule` still contains the original `Matches` rule, while `Pattern.Status` is `Deprecated` and `Pattern.Replacement` describes the literal comparison. The default message describes that replacement, not a regex failure.

The API does not change ESM bytes or select a new ESM version. It is a .NET value-checking surface; the TypeScript compiler exposes pattern admission, not this semantic value evaluator. For execution ordering and where concept rules are enforced, see [concept validation](concepts.md#validation).
