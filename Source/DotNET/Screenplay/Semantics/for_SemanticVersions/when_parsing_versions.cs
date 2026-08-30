// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_versions : Specification
{
    LanguageVersion _languageVersion;
    SemanticVersion _semanticVersion;
    bool _generalLanguageParserAcceptedV2;
    bool _generalSemanticParserAcceptedV2;

    void Because()
    {
        _languageVersion = LanguageVersion.Parse("1.0");
        _semanticVersion = SemanticVersion.Parse("1.0");
        _generalLanguageParserAcceptedV2 = LanguageVersion.TryParse("2.0", out _);
        _generalSemanticParserAcceptedV2 = SemanticVersion.TryParse("2.0", out _);
    }

    [Fact] void should_parse_supported_language_version() => _languageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_parse_supported_semantic_version() => _semanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_keep_the_general_language_parser_on_v1() => _generalLanguageParserAcceptedV2.ShouldBeFalse();
    [Fact] void should_keep_the_general_semantic_parser_on_v1() => _generalSemanticParserAcceptedV2.ShouldBeFalse();
    [Fact] void should_expose_one_supported_language_version() => EsmSchemaV1Support.LanguageVersions.ShouldContainOnly(LanguageVersion.V1);
    [Fact] void should_expose_one_supported_semantic_version() => EsmSchemaV1Support.SemanticVersions.ShouldContainOnly(SemanticVersion.V1);
    [Fact] void should_expose_known_v2_language_versions() => EsmSchemaV2Support.LanguageVersions.ShouldContainOnly(LanguageVersion.V1, LanguageVersion.V2);
    [Fact] void should_expose_known_v2_semantic_versions() => EsmSchemaV2Support.SemanticVersions.ShouldContainOnly(SemanticVersion.V1, SemanticVersion.V2);
    [Fact] void should_admit_the_v1_pair_in_schema_v2() => EsmSchemaV2Support.Supports(LanguageVersion.V1, SemanticVersion.V1).ShouldBeTrue();
    [Fact] void should_admit_the_v2_pair_in_schema_v2() => EsmSchemaV2Support.Supports(LanguageVersion.V2, SemanticVersion.V2).ShouldBeTrue();
    [Fact] void should_reject_a_v1_language_with_v2_semantics() => EsmSchemaV2Support.Supports(LanguageVersion.V1, SemanticVersion.V2).ShouldBeFalse();
    [Fact] void should_reject_a_v2_language_with_v1_semantics() => EsmSchemaV2Support.Supports(LanguageVersion.V2, SemanticVersion.V1).ShouldBeFalse();
}
