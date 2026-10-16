// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_enforcing_expanded_size : for_SemanticModelBinder.given.a_semantic_binder
{
    [Fact]
    void should_report_play0699_for_every_shared_over_budget_shape_before_building_a_matcher()
    {
        using var document = PortableMatchVectors.Read("portable-match-differential.json");
        var refused = document.RootElement.GetProperty("budgetCases").EnumerateArray().Where(vector => !vector.GetProperty("accepted").GetBoolean()).ToArray();
        refused.Length.ShouldBeGreaterThan(10);
        foreach (var vector in refused)
        {
            var pattern = vector.GetProperty("pattern").GetString()!;
            var parsed = SemanticMatchPattern.Parse(pattern);
            parsed.Accepted.ShouldBeFalse();
            parsed.Construct.ShouldEqual("expanded pattern size");
            parsed.Expression.ShouldBeNull();
            parsed.Offset.ShouldEqual(vector.GetProperty("offset").GetInt32());
            var result = Bind("concept Code : String\n  validate\n    matches \"" + pattern + "\"\n");
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect).ShouldBeTrue();
        }
    }
}
