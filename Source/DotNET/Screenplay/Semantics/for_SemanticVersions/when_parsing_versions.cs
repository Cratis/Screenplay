// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_versions : Specification
{
    LanguageVersion _languageVersion;
    SemanticVersion _semanticVersion;

    void Because()
    {
        _languageVersion = LanguageVersion.Parse("12.34");
        _semanticVersion = SemanticVersion.Parse("5.6");
    }

    [Fact] void should_parse_language_major() => _languageVersion.Major.ShouldEqual(12u);
    [Fact] void should_parse_language_minor() => _languageVersion.Minor.ShouldEqual(34u);
    [Fact] void should_parse_semantic_major() => _semanticVersion.Major.ShouldEqual(5u);
    [Fact] void should_parse_semantic_minor() => _semanticVersion.Minor.ShouldEqual(6u);
    [Fact] void should_write_language_version_canonically() => _languageVersion.ToString().ShouldEqual("12.34");
    [Fact] void should_write_semantic_version_canonically() => _semanticVersion.ToString().ShouldEqual("5.6");
}
