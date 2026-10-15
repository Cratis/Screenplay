// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_enforcing_expanded_size : for_SemanticModelBinder.given.a_semantic_binder
{
    [Theory]
    [InlineData("a{1500}")]
    [InlineData("a{1499,}")]
    [InlineData("^a{1498}$")]
    [InlineData("^(?:a{2}){749}$")]
    [InlineData("^a{1497,}$")]
    void should_construct_a_1500_position_pattern_with_the_real_linear_engine(string pattern)
    {
        var regex = SemanticMatchPattern.Create(pattern);
        regex.Options.ShouldEqual(RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);
        regex.MatchTimeout.ShouldEqual(Regex.InfiniteMatchTimeout);
    }

    [Theory]
    [InlineData("a{1501}")]
    [InlineData("a{1500,}")]
    [InlineData("^a{1499}$")]
    [InlineData("^(?:a{2}){749}a$")]
    [InlineData("^a{1498,}$")]
    void should_refuse_1501_positions_before_constructing_an_engine(string pattern)
    {
        var parsed = SemanticMatchPattern.Parse(pattern);
        parsed.Accepted.ShouldBeFalse();
        parsed.Construct.ShouldEqual("expanded pattern size");
        parsed.DotNetPattern.ShouldBeNull();
        var result = Bind("concept Code : String\n  validate\n    matches \"" + pattern + "\"\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect).ShouldBeTrue();
    }
}
