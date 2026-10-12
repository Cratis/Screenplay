// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reading_query_filter_admission : Specification
{
    [Fact]
    void should_admit_caller_supplied_collection_filters()
    {
        var syntax = new ScreenplayCompiler().Parse(query_filter_source.Source).Value!;
        var query = syntax.Modules.Single().Features.Single().Slices.Single().Queries.First();
        new AuthoringReadiness(syntax).SyntaxOnly(query).ShouldBeFalse();
    }

    [Theory]
    [InlineData("filter status String", "filter status String from $context.identity.id")]
    [InlineData("Filtered => Item[]", "Filtered => Item optional")]
    void should_keep_other_filter_shapes_syntax_only(string original, string replacement)
    {
        var syntax = new ScreenplayCompiler().Parse(query_filter_source.Source.Replace(original, replacement, StringComparison.Ordinal)).Value!;
        var query = syntax.Modules.Single().Features.Single().Slices.Single().Queries.First();
        new AuthoringReadiness(syntax).SyntaxOnly(query).ShouldBeTrue();
    }
}
