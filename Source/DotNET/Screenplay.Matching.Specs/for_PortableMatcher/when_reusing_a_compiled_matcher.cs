// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_reusing_a_compiled_matcher : Specification
{
    PortableMatcher _matcher = null!;
    bool[] _results = null!;

    void Establish() => _matcher = PortableMatcher.Compile("^[A-Z]{3}$");

    void Because()
    {
        _results = new bool[1000];
        Parallel.For(0, _results.Length, index => _results[index] = _matcher.IsMatch(index % 2 == 0 ? "ABC" : "ABC\n"));
    }

    [Fact] void should_keep_per_call_state_independent() => _results.ShouldEqual(Enumerable.Range(0, 1000).Select(index => index % 2 == 0));
}
