// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Matching.for_PortableMatcher;

public class when_matching_an_anchored_literal : Specification
{
    PortableMatcher _matcher = null!;
    bool _matches;

    void Establish() => _matcher = PortableMatcher.Compile("^ab$");
    void Because() => _matches = _matcher.IsMatch("ab");

    [Fact] void should_match_the_anchored_literal() => _matches.ShouldBeTrue();
}
