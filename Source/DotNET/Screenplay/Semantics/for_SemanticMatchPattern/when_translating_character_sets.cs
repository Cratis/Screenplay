// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_translating_character_sets
{
    [Theory]
    [InlineData("[a-cb-d]", @"[\u0061-\u0064]")]
    [InlineData(@"[\w\W]", @"[\u0000-\uffff]")]
    [InlineData(@"[^\w\W]", @"[^\u0000-\uffff]")]
    [InlineData(@"[^\D0]", @"[\u0031-\u0039]")]
    [InlineData(@"[\u0000\uffff]", @"[\u0000\uffff]")]
    [InlineData(@"[^\u0000\uffff]", @"[\u0001-\ufffe]")]
    [InlineData(@"[^\u0000-\uffff]", @"[^\u0000-\uffff]")]
    void should_emit_sorted_flat_ranges_without_host_character_classes(string pattern, string translated) =>
        SemanticMatchPattern.Parse(pattern).DotNetPattern.ShouldEqual(translated);
}
