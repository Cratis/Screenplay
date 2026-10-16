---
title: Check command date comparisons
description: Evaluate command Date and DateTime comparisons with the same chronological meaning as the reference evaluator.
---

Two timestamps can name the same instant without looking alike. If your host executes Screenplay commands itself, comparing their text can make it disagree with the reference evaluator. Use `SemanticDateComparisons` and `SemanticConditionEvaluator` from the `Cratis.Screenplay` .NET library to share that evaluator's date comparison and input-checking behavior instead of copying it.

These APIs take contract-validated semantic model objects, such as those from a bound or deserialized executable semantic model. They do not admit a model, authorize a caller or execute a command for you.

## Prepare the command inputs

First call `SemanticDateComparisons.CheckInputs(application, command, inputs)`, before running any rule or condition. It returns the first invalid participating `DateTime` text input as a `SemanticValidationFailure` with `Error` severity, or null when none is invalid. Properties are checked in command declaration order, not input order. Null entries, absent values and properties outside the comparisons are ignored; this does not replace general input type validation.

The failure message is exactly:

```text
DateTime command property '<property name>' requires an explicit offset or Z.
```

Reject this input even if a condition would short-circuit before comparing it. Check the original text before a deserializer can attach a local offset.

Next call `SemanticDateComparisons.Participants(application, command)`. This gives you a map from property identity to `Date` or `DateTime`, resolving concept types too. It includes non-read property operands of `require` and `produces when` comparisons, and subjects of comparison-kind command validation rules. A date property with only `not empty` is not a participant.

## Evaluate the comparisons

The comparison APIs and model types are in `Cratis.Screenplay.Semantics`; `SemanticValidationFailure` is in `Cratis.Screenplay.Semantics.Execution`.

| Member | Result |
| --- | --- |
| `SemanticDateComparisons.Participants(SemanticApplication application, SemanticCommand command)` | `IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>` identifying the date comparison participants. |
| `SemanticDateComparisons.TryParseInstant(string value, out DateTimeOffset instant)` | Whether the text has an accepted explicit-offset instant spelling; supplies the parsed instant on success. |
| `SemanticDateComparisons.Compare(SemanticValue left, SemanticValue right, SemanticPrimitiveType primitive)` | An `int`: negative, zero or positive for earlier, equal or later calendar dates or UTC instants. Throws `InvalidSemanticContract` for non-text or unparseable operands, or a non-date primitive. |
| `SemanticDateComparisons.Check(SemanticValidationRule rule, SemanticValue value, SemanticPrimitiveType primitive)` | A `SemanticRuleCheck`, chronological for the six comparison kinds on `Date` or `DateTime`; otherwise the ordinary value-rule check. |
| `SemanticDateComparisons.CheckInputs(SemanticApplication application, SemanticCommand command, IEnumerable<SemanticPropertyValue?> values)` | A `SemanticValidationFailure?` for the first invalid participating `DateTime` text input. |
| `SemanticConditionEvaluator.Evaluate(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes = null, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null)` | Whether the comparison or logical condition holds, using the optional named protected read values. |

Pass the `Participants` map as `dateTypes` to `Evaluate`: date dispatch uses the **left operand's property identity**. Without that map, text equality stays ordinal and ordering requires numeric operands. Logical `and` and `or` preserve grouping and short-circuit evaluation.

The comparison kinds are `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan` and `LessThanOrEqual`. `Date` compares calendar dates without a time zone, spelled `yyyy-MM-dd`. `DateTime` compares UTC instants at tick precision: seven fractional second digits, or 100 nanoseconds per tick. Different offsets naming the same instant are equal.

This standalone C# example shows why text comparison is not enough:

```csharp
using Cratis.Screenplay.Semantics;

var rule = new SemanticValidationRule(
    default,
    SemanticValidationRuleKind.GreaterThan,
    SemanticValue.Text("2026-01-01T00:00:00.0000000Z"),
    "Choose a later instant.");
var check = SemanticDateComparisons.Check(
    rule,
    SemanticValue.Text("2026-01-01T02:00:00+02:00"),
    SemanticPrimitiveType.DateTime);
Console.WriteLine(check.Status);
Console.WriteLine(check.Message);
```

The two values name the same instant, so the input is not later. Output:

```text
Failed
Choose a later instant.
```

An absent value (`SemanticValue.Null`) passes date comparison rules; use `NotEmpty` separately to require presence. `Check` preserves the authored message, including unresolved `$strings.` keys, or supplies the same default as the reference evaluator, even on satisfied results. Incompatible or invalid comparison operands throw `InvalidSemanticContract` instead of returning `Failed`. Unsupported condition nodes and operators also throw `InvalidSemanticContract`. Supply the command properties and named reads that the validated condition references.

## Accepted instant spellings

The accepted input subset of ISO 8601 uses a four-digit year and two-digit month, day, hour and minute, a literal uppercase `T`, and an explicit offset or uppercase `Z`. Write offsets as `+HH:mm` or `-HH:mm`; the parser also accepts `+HHmm` and a one-digit hour such as `+2:00`, but not an hour-only offset such as `+02`. A target that reimplements this check accepts the same spellings. Seconds are optional. When seconds are present, the fraction is optional and may have up to seven digits after a decimal point; fractional minutes are not accepted. These are the four forms:

| Form | Example |
| --- | --- |
| Seconds, optional fraction, explicit offset | `2026-01-01T02:00:00.1234567+02:00` |
| Seconds, optional fraction, Z | `2026-01-01T00:00:00.1234567Z` |
| Minutes, explicit offset | `2026-01-01T02:00+02:00` |
| Minutes, Z | `2026-01-01T00:00Z` |

Parsing is culture-independent. Calendar and offset values must be valid. Offset-less timestamps, date-only values, fractions beyond seven digits and unparseable text are rejected. Comparison constants in a validated model are already canonical: calendar dates for `Date`, or UTC with seven fractional digits and `Z` for `DateTime`. Input values may use any of the four forms.

## Keep the boundary narrow

Chronological equality applies only to admitted **command comparisons**. Specification `then` matching, read-model keys, constraints and other text equality remain ordinal. A target must preserve tick precision rather than round to milliseconds.

Do not use this API to enable date rules on concepts or date ordering in reaction `where`: those positions remain refused by the executable model. Admitted command comparisons use a command property on the left and a same-primitive command property or literal on the right; command validation rules use literal operands. Date comparisons with protected read paths, `$context`, `today`, or mixed `Date` and `DateTime` operands remain refused too.

For ordinary value and concept rules, use [Check values against validation rules](value-rule-checks.md). This host API does not change language admission or executable semantic model bytes.
