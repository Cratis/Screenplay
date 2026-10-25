// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Xunit;

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_exceeding_the_platform_automaton_limit(ITestOutputHelper output) : Specification
{
    const string Pattern = "^(a{2996}b)+$";
    PortableMatcher _matcher = null!;
    Exception? _error;

    void Establish() => _matcher = PortableMatcher.Compile(Pattern);

    void Because()
    {
        var translated = new PortableMatchParser(Pattern).Parse().NormalizedDotNetPattern;
        _error = Catch.Exception(() => _ = new Regex(translated, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant));
        output.WriteLine($"Runtime={Environment.Version}; platform result={_error}");
    }

    [Fact] void should_exceed_the_default_platform_limit() => _error.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_match_two_repetitions() => _matcher.IsMatch(string.Concat(Enumerable.Repeat(new string('a', 2996) + "b", 2))).ShouldBeTrue();
    [Fact] void should_reject_a_near_miss() => _matcher.IsMatch(new string('a', 2996) + "b" + new string('a', 2995) + "b").ShouldBeFalse();
}
