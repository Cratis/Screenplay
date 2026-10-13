// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v15 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("15.0").ShouldEqual(LanguageVersion.V15);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("15.0").ShouldEqual(SemanticVersion.V15);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV15Support.Supports(LanguageVersion.V15, SemanticVersion.V15).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV15Support.Supports(LanguageVersion.V14, SemanticVersion.V15).ShouldBeFalse();
    [Fact] void should_include_every_v14_pair() => EsmSchemaV14Support.LanguageVersions.Zip(EsmSchemaV14Support.SemanticVersions).All(pair => EsmSchemaV15Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v15_in_the_frozen_v14_contract() => EsmSchemaV14Support.Supports(LanguageVersion.V15, SemanticVersion.V15).ShouldBeFalse();
}
