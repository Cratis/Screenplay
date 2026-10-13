// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_checking_whether_a_value_is_empty;

public class with_unicode_whitespace : Specification
{
    [Theory]
    [InlineData("\u0009")]
    [InlineData("\u000a")]
    [InlineData("\u000b")]
    [InlineData("\u000c")]
    [InlineData("\u000d")]
    [InlineData("\u0020")]
    [InlineData("\u0085")]
    [InlineData("\u00a0")]
    [InlineData("\u1680")]
    [InlineData("\u2000")]
    [InlineData("\u2001")]
    [InlineData("\u2002")]
    [InlineData("\u2003")]
    [InlineData("\u2004")]
    [InlineData("\u2005")]
    [InlineData("\u2006")]
    [InlineData("\u2007")]
    [InlineData("\u2008")]
    [InlineData("\u2009")]
    [InlineData("\u200a")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\u202f")]
    [InlineData("\u205f")]
    [InlineData("\u3000")]
    void should_treat_every_unicode_white_space_character_as_empty(string text) => SemanticEvaluator.IsEmpty(SemanticValue.Text(text)).ShouldBeTrue();
}
