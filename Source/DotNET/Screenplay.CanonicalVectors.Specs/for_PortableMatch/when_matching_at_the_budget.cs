// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Semantics;
using Xunit.Abstractions;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_at_the_budget(ITestOutputHelper output)
{
    [Theory]
    [InlineData("a{1499}b", false)]
    [InlineData("^a{1498}$", false)]
    [InlineData("^(?:a{2}){749}$", false)]
    [InlineData("^a{1497,}$", true)]
    [InlineData("^(?:a?){1498}$", false)]
    void should_complete_a_10000_code_unit_input_at_1500_positions(string pattern, bool expected)
    {
        var matcher = SemanticMatchPattern.Create(pattern);
        var input = new string('a', 10000);
        var watch = Stopwatch.StartNew();
        var matched = matcher.IsMatch(input);
        watch.Stop();
        output.WriteLine($"pattern={pattern}; input={input.Length}; states={matcher.StateCount}; elapsedMs={watch.Elapsed.TotalMilliseconds:F3}");
        matched.ShouldEqual(expected);
    }
}
