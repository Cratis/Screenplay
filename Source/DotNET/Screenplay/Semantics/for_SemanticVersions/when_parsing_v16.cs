// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v16 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("16.0").ShouldEqual(LanguageVersion.V16);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("16.0").ShouldEqual(SemanticVersion.V16);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV16Support.Supports(LanguageVersion.V16, SemanticVersion.V16).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV16Support.Supports(LanguageVersion.V15, SemanticVersion.V16).ShouldBeFalse();
    [Fact] void should_include_every_v15_pair() => EsmSchemaV15Support.LanguageVersions.Zip(EsmSchemaV15Support.SemanticVersions).All(pair => EsmSchemaV16Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v16_in_the_frozen_v15_contract() => EsmSchemaV15Support.Supports(LanguageVersion.V16, SemanticVersion.V16).ShouldBeFalse();
}
