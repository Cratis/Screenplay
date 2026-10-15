// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_enforcing_expanded_size : for_SemanticModelBinder.given.a_semantic_binder
{
    [Theory]
    [InlineData("a{1500}", 1500)]
    [InlineData("a{1499,}", 1499)]
    [InlineData("^a{1498}$", 1498)]
    [InlineData("^(?:a{2}){749}$", 1498)]
    [InlineData("^a{1497,}$", 1497)]
    void should_evaluate_a_1500_position_pattern(string pattern, int length) =>
        SemanticMatchPattern.Create(pattern).IsMatch(new string('a', length)).ShouldBeTrue();

    [Theory]
    [InlineData("a{1501}")]
    [InlineData("a{1500,}")]
    [InlineData("^a{1499}$")]
    [InlineData("^(?:a{2}){749}a$")]
    [InlineData("^a{1498,}$")]
    void should_refuse_1501_positions_before_building_a_matcher(string pattern)
    {
        var parsed = SemanticMatchPattern.Parse(pattern);
        parsed.Accepted.ShouldBeFalse();
        parsed.Construct.ShouldEqual("expanded pattern size");
        parsed.Expression.ShouldBeNull();
        var result = Bind("concept Code : String\n  validate\n    matches \"" + pattern + "\"\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect).ShouldBeTrue();
    }
}
