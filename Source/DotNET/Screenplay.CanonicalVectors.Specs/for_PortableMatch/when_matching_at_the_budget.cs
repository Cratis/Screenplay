// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Semantics;
using Xunit.Abstractions;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_at_the_budget(ITestOutputHelper output)
{
    [Theory]
    [InlineData("a{2999}b", false)]
    [InlineData("^a{2998}$", false)]
    [InlineData("^(?:a{2}){1499}$", false)]
    [InlineData("^a{2996,}$", true)]
    [InlineData("^(?:a?){1499}$", false)]
    void should_complete_a_10000_code_unit_input_at_3000_nodes(string pattern, bool expected)
    {
        var parsed = SemanticMatchPattern.Parse(pattern);
        var matcher = SemanticMatchPattern.Create(parsed);
        matcher.StateCount.ShouldBeLessThanOrEqual(parsed.ExpandedSize + 1);
        matcher.StateCount.ShouldBeLessThanOrEqual(2 * parsed.ExpandedSize);
        var input = new string('a', 10000);
        var watch = Stopwatch.StartNew();
        var matched = matcher.IsMatch(input);
        watch.Stop();
        output.WriteLine($"pattern={pattern}; input={input.Length}; states={matcher.StateCount}; elapsedMs={watch.Elapsed.TotalMilliseconds:F3}");
        matched.ShouldEqual(expected);
    }
}
