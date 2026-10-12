// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v14 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("14.0").ShouldEqual(LanguageVersion.V14);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("14.0").ShouldEqual(SemanticVersion.V14);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV14Support.Supports(LanguageVersion.V14, SemanticVersion.V14).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV14Support.Supports(LanguageVersion.V13, SemanticVersion.V14).ShouldBeFalse();
    [Fact] void should_include_every_v13_pair() => EsmSchemaV13Support.LanguageVersions.Zip(EsmSchemaV13Support.SemanticVersions).All(pair => EsmSchemaV14Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v14_in_the_frozen_v13_contract() => EsmSchemaV13Support.Supports(LanguageVersion.V14, SemanticVersion.V14).ShouldBeFalse();
}
