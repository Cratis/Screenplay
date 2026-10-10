// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_matching_literal_dollars : Specification
{
    bool _escaped;
    bool _inClass;
    bool _negatedClass;
    bool _escapedBackslash;
    bool _backslashBeforeNewline;
    bool _escapedClosingBracket;
    bool _leadingClosingBracket;

    void Because()
    {
        _escaped = SemanticMatchPattern.Create(@"^\$$").IsMatch("$");
        _inClass = SemanticMatchPattern.Create("^[$]$").IsMatch("$");
        _negatedClass = SemanticMatchPattern.Create("^[^$]$").IsMatch("A");
        _escapedBackslash = SemanticMatchPattern.Create(@"^\\$").IsMatch(@"\");
        _backslashBeforeNewline = SemanticMatchPattern.Create(@"^\\$").IsMatch("\\\n");
        _escapedClosingBracket = SemanticMatchPattern.Create(@"^[\]$]+$").IsMatch("]$");
        _leadingClosingBracket = SemanticMatchPattern.Create("^[]$]+$").IsMatch("]$");
    }

    [Fact] void should_preserve_an_escaped_dollar() => _escaped.ShouldBeTrue();
    [Fact] void should_preserve_a_dollar_in_a_character_class() => _inClass.ShouldBeTrue();
    [Fact] void should_preserve_a_negated_dollar_class() => _negatedClass.ShouldBeTrue();
    [Fact] void should_anchor_after_an_escaped_backslash() => _escapedBackslash.ShouldBeTrue();
    [Fact] void should_reject_a_newline_after_an_escaped_backslash() => _backslashBeforeNewline.ShouldBeFalse();
    [Fact] void should_preserve_a_dollar_after_an_escaped_closing_bracket() => _escapedClosingBracket.ShouldBeTrue();
    [Fact] void should_preserve_a_dollar_after_a_literal_leading_closing_bracket() => _leadingClosingBracket.ShouldBeTrue();
}
