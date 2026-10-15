// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_translating_character_sets
{
    [Theory]
    [InlineData("[a-cb-d]", new[] { 97, 100 })]
    [InlineData(@"[\w\W]", new[] { 0, 65535 })]
    [InlineData(@"[^\w\W]", new int[0])]
    [InlineData(@"[^\D0]", new[] { 49, 57 })]
    [InlineData(@"[\u0000\uffff]", new[] { 0, 0, 65535, 65535 })]
    [InlineData(@"[^\u0000\uffff]", new[] { 1, 65534 })]
    [InlineData(@"[^\u0000-\uffff]", new int[0])]
    void should_keep_sorted_flat_ranges_without_host_character_classes(string pattern, int[] boundaries)
    {
        var expression = (MatchCharacterExpression)SemanticMatchPattern.Parse(pattern).Expression!;
        expression.Characters.Ranges.SelectMany(range => new[] { range.First, range.Last }).ShouldEqual(boundaries);
    }
}
