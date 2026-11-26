// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions.when_trying_to_parse_version_text;

public class with_a_misplaced_or_repeated_separator : Specification
{
    [Theory]
    [InlineData(".0")]
    [InlineData("1.")]
    [InlineData("1.0.0")]
    void should_reject_the_text_and_return_a_default_version(string text)
    {
        var parsed = LanguageVersion.TryParse(text, out var version);
        (parsed, version).ShouldEqual((false, default(LanguageVersion)));
    }
}
