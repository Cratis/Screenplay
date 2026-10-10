// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_matching_end_of_input : Specification
{
    Regex _pattern;

    void Because() => _pattern = SemanticMatchPattern.Create("^[A-Z]{3}$");

    [Fact] void should_match_the_whole_input() => _pattern.IsMatch("ABC").ShouldBeTrue();
    [Fact] void should_reject_a_trailing_newline() => _pattern.IsMatch("ABC\n").ShouldBeFalse();
    [Fact] void should_reject_a_trailing_carriage_return() => _pattern.IsMatch("ABC\r").ShouldBeFalse();
    [Fact] void should_reject_a_trailing_line_separator() => _pattern.IsMatch("ABC\u2028").ShouldBeFalse();
}
