// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_absent_dates : Specification
{
    SemanticRuleCheck[] _results;

    void Because() => _results = new[] { SemanticPrimitiveType.Date, SemanticPrimitiveType.DateTime }
        .Select(primitive => SemanticDateComparisons.Check(new(default, SemanticValidationRuleKind.GreaterThan, SemanticValue.Text(primitive == SemanticPrimitiveType.Date ? "2026-01-01" : "2026-01-01T00:00:00.0000000Z"), null), SemanticValue.Null, primitive)).ToArray();

    [Fact] void should_leave_presence_to_not_empty() => _results.ShouldEachConformTo(result => result.Status == SemanticRuleStatus.Satisfied);
}
