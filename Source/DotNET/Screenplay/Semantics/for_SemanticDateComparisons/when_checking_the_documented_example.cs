// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_the_documented_example : Specification
{
    SemanticRuleCheck _check;

    void Because()
    {
        var rule = new SemanticValidationRule(
            default,
            SemanticValidationRuleKind.GreaterThan,
            SemanticValue.Text("2026-01-01T00:00:00.0000000Z"),
            "Choose a later instant.");
        var check = SemanticDateComparisons.Check(
            rule,
            SemanticValue.Text("2026-01-01T02:00:00+02:00"),
            SemanticPrimitiveType.DateTime);
        _check = check;
    }

    [Fact] void should_report_the_documented_status() => _check.Status.ShouldEqual(SemanticRuleStatus.Failed);
    [Fact] void should_report_the_documented_message() => _check.Message.ShouldEqual("Choose a later instant.");
}
