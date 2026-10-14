// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v20 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("20.0").ShouldEqual(LanguageVersion.V20);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("20.0").ShouldEqual(SemanticVersion.V20);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV20Support.Supports(LanguageVersion.V20, SemanticVersion.V20).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV20Support.Supports(LanguageVersion.V19, SemanticVersion.V20).ShouldBeFalse();
    [Fact] void should_include_every_v19_pair() => EsmSchemaV19Support.LanguageVersions.Zip(EsmSchemaV19Support.SemanticVersions).All(pair => EsmSchemaV20Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v20_in_the_frozen_v19_contract() => EsmSchemaV19Support.Supports(LanguageVersion.V20, SemanticVersion.V20).ShouldBeFalse();
}
