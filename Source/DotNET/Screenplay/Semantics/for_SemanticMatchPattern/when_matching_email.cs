// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_matching_email : Specification
{
    bool _email;
    bool _newline;
    bool _noBreakSpace;

    void Because()
    {
        _email = SemanticMatchPattern.Create(SemanticMatchPattern.Email).IsMatch("a@b.co");
        _newline = SemanticMatchPattern.Create(SemanticMatchPattern.Email).IsMatch("a@b.co\n");
        _noBreakSpace = SemanticMatchPattern.Create(SemanticMatchPattern.Email).IsMatch("a\u00a0@b.co");
    }

    [Fact] void should_accept_an_email() => _email.ShouldBeTrue();
    [Fact] void should_reject_a_trailing_newline() => _newline.ShouldBeFalse();
    [Fact] void should_reject_a_no_break_space_like_javascript() => _noBreakSpace.ShouldBeFalse();
}
