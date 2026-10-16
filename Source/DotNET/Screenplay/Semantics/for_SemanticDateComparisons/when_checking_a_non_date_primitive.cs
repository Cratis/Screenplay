// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_a_non_date_primitive : Specification
{
    SemanticRuleCheck _result;

    void Because() => _result = SemanticDateComparisons.Check(new(default, SemanticValidationRuleKind.Equal, SemanticValue.Text("2026-01-01T00:00:00Z"), null), SemanticValue.Text("2026-01-01T02:00:00+02:00"), SemanticPrimitiveType.Text);

    [Fact] void should_keep_text_equality_ordinal() => _result.Status.ShouldEqual(SemanticRuleStatus.Failed);
}
