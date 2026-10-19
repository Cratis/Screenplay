// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules.given;

public class rule_examples : Specification
{
    protected (SemanticValidationRule Rule, SemanticValue Satisfied, SemanticValue Failed)[] Examples;

    void Establish() => Examples =
    [
        (Rule(SemanticValidationRuleKind.NotEmpty), SemanticValue.Text("present"), SemanticValue.Text(" \u0085\u00a0\u3000")),
        (Rule(SemanticValidationRuleKind.Maximum, SemanticValue.Number(2)), SemanticValue.Number(2), SemanticValue.Number(2.0000000000000000000000000001m)),
        (Rule(SemanticValidationRuleKind.Minimum, SemanticValue.Number(2)), SemanticValue.Number(2), SemanticValue.Number(1.9999999999999999999999999999m)),
        (Rule(SemanticValidationRuleKind.Equal, SemanticValue.Text("Ab")), SemanticValue.Text("Ab"), SemanticValue.Text("ab")),
        (Rule(SemanticValidationRuleKind.NotEqual, SemanticValue.Text("Ab")), SemanticValue.Text("ab"), SemanticValue.Text("Ab")),
        (Rule(SemanticValidationRuleKind.GreaterThan, SemanticValue.Number(2)), SemanticValue.Number(3), SemanticValue.Number(2)),
        (Rule(SemanticValidationRuleKind.GreaterThanOrEqual, SemanticValue.Number(2)), SemanticValue.Number(2), SemanticValue.Number(1)),
        (Rule(SemanticValidationRuleKind.LessThan, SemanticValue.Number(2)), SemanticValue.Number(1), SemanticValue.Number(2)),
        (Rule(SemanticValidationRuleKind.LessThanOrEqual, SemanticValue.Number(2)), SemanticValue.Number(2), SemanticValue.Number(3)),
        (Rule(SemanticValidationRuleKind.Length, SemanticValue.Number(2)), SemanticValue.Text("\ud83d\ude00"), SemanticValue.Text("a")),
        (Rule(SemanticValidationRuleKind.AllGreaterThan, SemanticValue.Number(2)), SemanticValue.Array([SemanticValue.Number(3)]), SemanticValue.Array([SemanticValue.Number(3), SemanticValue.Number(2)])),
        (Rule(SemanticValidationRuleKind.AllGreaterThanOrEqual, SemanticValue.Number(2)), SemanticValue.Array([SemanticValue.Number(2)]), SemanticValue.Array([SemanticValue.Number(2), SemanticValue.Number(1)])),
        (Rule(SemanticValidationRuleKind.Matches, SemanticValue.Text("^[A-Z]{3}$")), SemanticValue.Text("ABC"), SemanticValue.Text("ABC\n")),
        (Rule(SemanticValidationRuleKind.Checksum, SemanticValue.Text("luhn")), SemanticValue.Text("79927398713"), SemanticValue.Text("79927398714")),
        (Rule(SemanticValidationRuleKind.Contains, SemanticValue.Text("Ab")), SemanticValue.Text("xAbx"), SemanticValue.Text("xabx")),
        (Rule(SemanticValidationRuleKind.NotContains, SemanticValue.Text("Ab")), SemanticValue.Text("xabx"), SemanticValue.Text("xAbx")),
        (Rule(SemanticValidationRuleKind.StartsWith, SemanticValue.Text("Ab")), SemanticValue.Text("Abx"), SemanticValue.Text("xAb")),
        (Rule(SemanticValidationRuleKind.EndsWith, SemanticValue.Text("Ab")), SemanticValue.Text("xAb"), SemanticValue.Text("Abx")),
        (Rule(SemanticValidationRuleKind.DecimalPlaces, SemanticValue.Number(2)), SemanticValue.Number(19.99m), SemanticValue.Number(19.999m))
    ];

    protected static SemanticValidationRule Rule(SemanticValidationRuleKind kind, SemanticValue? operand = null) => new(default, kind, operand, null);
}
