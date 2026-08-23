// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_versions : Specification
{
    LanguageVersion _languageVersion;
    SemanticVersion _semanticVersion;

    void Because()
    {
        _languageVersion = LanguageVersion.Parse("1.0");
        _semanticVersion = SemanticVersion.Parse("1.0");
    }

    [Fact] void should_parse_supported_language_version() => _languageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_parse_supported_semantic_version() => _semanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_expose_one_supported_language_version() => EsmSchemaV1Support.LanguageVersions.ShouldContainOnly(LanguageVersion.V1);
    [Fact] void should_expose_one_supported_semantic_version() => EsmSchemaV1Support.SemanticVersions.ShouldContainOnly(SemanticVersion.V1);
}
