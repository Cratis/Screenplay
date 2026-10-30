// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_HostLanguageFragments;

public class when_finding_fragments
{
    [Theory]
    [InlineData("a && b", "&&", 2)]
    [InlineData("a == 1 || b", "||", 7)]
    [InlineData("Foo(x)", "Foo(", 0)]
    [InlineData("x == Math.Round(y)", "Math.Round(", 5)]
    [InlineData("\"a\" + calculate(x)", "calculate(", 6)]
    [InlineData("\"it\\\"s\" && b", "&&", 8)]
    public void should_find_the_fragment(string text, string fragment, int offset) =>
        HostLanguageFragments.Find(text).ShouldEqual((fragment, offset));

    [Theory]
    [InlineData("\"a && b\"")]
    [InlineData("'f(x)'")]
    [InlineData("`${a} && f(b)`")]
    [InlineData("{\"k\":\"a || b\",\"l\":[f(1)]}")]
    [InlineData("[\"a && b\", 'c || d']")]
    [InlineData("a != b")]
    [InlineData("a and (b or c)")]
    [InlineData("not (a == 1)")]
    [InlineData("a contains (b)")]
    [InlineData("Foo (x)")]
    [InlineData("a & b | c")]
    [InlineData("\"unterminated && b")]
    [InlineData("{ unterminated")]
    public void should_not_find_a_fragment(string text) => HostLanguageFragments.Find(text).ShouldBeNull();
}
