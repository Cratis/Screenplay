// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

/// <summary>
/// Stands in for a node kind that did not exist when the walker was written - the language growing a new
/// expression form, or a sub-language contributing one of its own.
/// </summary>
public class when_walking_a_node_kind_the_walker_does_not_know : Specification
{
    given.a_counting_walker _walker;
    unknown_expression _unknown;
    TagSyntax _tag;
    Exception _error;

    void Establish()
    {
        _walker = new();
        _unknown = new(SourceLocation.Start);
        _tag = new(_unknown, SourceLocation.Start);
    }

    void Because() => _error = Catch.Exception(() => _walker.VisitTag(_tag));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_still_reach_the_node_it_does_know() => _walker.Nodes.ShouldContain(_tag);
    [Fact] void should_reach_the_node_it_does_not_know() => _walker.Nodes.ShouldContain(_unknown);

    record unknown_expression(SourceLocation Location) : ExpressionSyntax(Location);
}
